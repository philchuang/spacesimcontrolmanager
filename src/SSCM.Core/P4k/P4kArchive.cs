using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;
using ZstdSharp;

namespace SSCM.Core.P4k;

/// <summary>
/// Minimal read-only access to a p4k archive.
/// A p4k is a ZIP64 archive that uses a nonstandard local header signature
/// (<c>0x14034b50</c> as well as the standard <c>0x04034b50</c>) and zstd (method 100)
/// compression, alongside stored and deflate entries. Only unencrypted entries are supported.
/// </summary>
public static class P4kArchive
{
    private const uint EocdSignature = 0x06054b50;
    private const uint Zip64LocatorSignature = 0x07064b50;
    private const uint Zip64EocdSignature = 0x06064b50;
    private const uint CentralDirectorySignature = 0x02014b50;
    private const uint LocalHeaderSignature = 0x04034b50;
    private const uint P4kLocalHeaderSignature = 0x14034b50;

    private const ushort MethodStore = 0;
    private const ushort MethodDeflate = 8;
    private const ushort MethodZstd = 100;

    private const int CentralDirectoryFixedSize = 46;
    private const int LocalHeaderFixedSize = 30;
    private const int CancellationCheckInterval = 256;

    // A corrupt directory entry can declare 0xFFFFFFFF. Cap the allocation so that fails
    // with a clear error instead of reserving gigabytes.
    internal const long MaxEntryBytes = 64L * 1024 * 1024;

    private static readonly uint[] CrcTable = CreateCrcTable();

    private sealed record Entry(ushort Flags, ushort Method, uint Crc32, long CompressedSize, long UncompressedSize, long LocalHeaderOffset);

    /// <summary>Raised inside a decoder so a short or long result is not reported as a codec failure.</summary>
    private sealed class EntryLengthException : Exception
    {
        public EntryLengthException(string message) : base(message) { }
    }

    /// <summary>Reads the named entry (case-insensitive, '/' and '\' are equivalent), or throws <see cref="FileNotFoundException"/>.</summary>
    public static byte[] ReadEntry(string archivePath, string entryName)
    {
        using var stream = new FileStream(archivePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 1 << 20, FileOptions.RandomAccess);
        return ReadEntry(stream, entryName);
    }

    public static byte[] ReadEntry(Stream stream, string entryName)
    {
        return ReadEntries(stream, [entryName]).TryGetValue(entryName, out var data)
            ? data
            : throw new FileNotFoundException($"Entry [{entryName}] was not found in the archive.", entryName);
    }

    /// <summary>
    /// Reads several entries with a single scan of the central directory, which is the slow part of a large archive.
    /// Names match the same way as <see cref="ReadEntry(string, string)"/>. The result is keyed by the name as requested
    /// (case-insensitively) and leaves out any entry that is not in the archive, so callers decide which ones are required.
    /// </summary>
    public static Dictionary<string, byte[]> ReadEntries(string archivePath, IReadOnlyCollection<string> entryNames, CancellationToken cancellationToken = default)
    {
        using var stream = new FileStream(archivePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 1 << 20, FileOptions.RandomAccess);
        return ReadEntries(stream, entryNames, cancellationToken);
    }

    public static Dictionary<string, byte[]> ReadEntries(Stream stream, IReadOnlyCollection<string> entryNames, CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
        foreach (var (name, entry) in FindEntries(stream, entryNames, cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            result[name] = ReadData(stream, entry, name);
        }

        return result;
    }

    private static byte[] ReadData(Stream stream, Entry entry, string entryName)
    {
        if ((entry.Flags & 1) != 0) throw new NotSupportedException($"Entry [{entryName}] is encrypted, which is not supported.");
        EnsureDeclaredRangeFits(stream, entry, entryName);

        stream.Seek(entry.LocalHeaderOffset, SeekOrigin.Begin);
        var header = new byte[LocalHeaderFixedSize];
        stream.ReadExactly(header);
        var signature = BinaryPrimitives.ReadUInt32LittleEndian(header);
        if (signature != LocalHeaderSignature && signature != P4kLocalHeaderSignature)
        {
            throw new InvalidDataException($"Unexpected local header signature 0x{signature:X8} for entry [{entryName}].");
        }

        var nameLength = BinaryPrimitives.ReadUInt16LittleEndian(header.AsSpan(26));
        var extraLength = BinaryPrimitives.ReadUInt16LittleEndian(header.AsSpan(28));
        var preamble = (long) LocalHeaderFixedSize + nameLength + extraLength;
        if (preamble > stream.Length - entry.LocalHeaderOffset)
        {
            throw new InvalidDataException($"Entry [{entryName}] extends past the end of the archive.");
        }

        var dataOffset = entry.LocalHeaderOffset + preamble;
        if (entry.CompressedSize > stream.Length - dataOffset)
        {
            throw new InvalidDataException($"Entry [{entryName}] extends past the end of the archive.");
        }

        stream.Seek(dataOffset, SeekOrigin.Begin);
        var compressed = new byte[checked((int) entry.CompressedSize)];
        stream.ReadExactly(compressed);
        return Decompress(entry, compressed, entryName);
    }

    private static void EnsureDeclaredRangeFits(Stream stream, Entry entry, string entryName)
    {
        if (entry.CompressedSize < 0 || entry.UncompressedSize < 0 || entry.LocalHeaderOffset < 0)
        {
            throw new InvalidDataException($"Entry [{entryName}] has a negative size or offset.");
        }

        if (entry.CompressedSize > MaxEntryBytes || entry.UncompressedSize > MaxEntryBytes)
        {
            throw new InvalidDataException($"Entry [{entryName}] declares a size above the {MaxEntryBytes}-byte limit.");
        }

        // LocalHeaderOffset + CompressedSize <= length, without adding the two values (a corrupt pair can overflow).
        if (stream.Length < LocalHeaderFixedSize
            || entry.LocalHeaderOffset > stream.Length - LocalHeaderFixedSize
            || entry.CompressedSize > stream.Length - entry.LocalHeaderOffset)
        {
            throw new InvalidDataException($"Entry [{entryName}] extends past the end of the archive.");
        }
    }

    private static byte[] Decompress(Entry entry, byte[] compressed, string entryName)
    {
        var output = entry.Method switch
        {
            MethodStore => compressed,
            MethodDeflate => Decode(entryName, () =>
            {
                using var input = new MemoryStream(compressed, writable: false);
                using var deflate = new DeflateStream(input, CompressionMode.Decompress);
                return ReadBounded(deflate, checked((int) entry.UncompressedSize), entryName);
            }),
            MethodZstd => Decode(entryName, () =>
            {
                using var input = new MemoryStream(compressed, writable: false);
                using var zstd = new DecompressionStream(input);
                return ReadBounded(zstd, checked((int) entry.UncompressedSize), entryName);
            }),
            _ => throw new NotSupportedException($"Entry [{entryName}] uses unsupported compression method {entry.Method}."),
        };

        if (output.LongLength != entry.UncompressedSize)
        {
            throw new InvalidDataException($"Entry [{entryName}] decompressed to {output.LongLength} bytes, but the archive declares {entry.UncompressedSize}.");
        }

        if (Crc32(output) != entry.Crc32)
        {
            throw new InvalidDataException($"Entry [{entryName}] failed its CRC32 check.");
        }

        return output;
    }

    /// <summary>
    /// Encryption is not always flagged with general-purpose bit 0, so a protected entry shows up as a zstd or deflate failure.
    /// Name the entry instead of letting that codec error through on its own.
    /// </summary>
    private static byte[] Decode(string entryName, Func<byte[]> read)
    {
        try
        {
            return read();
        }
        catch (EntryLengthException ex)
        {
            throw new InvalidDataException(ex.Message, ex);
        }
        catch (Exception ex)
        {
            throw new InvalidDataException($"Failed to decode entry [{entryName}]. It may be encrypted or corrupt.", ex);
        }
    }

    private static byte[] ReadBounded(Stream decoder, int expected, string entryName)
    {
        var buffer = expected == 0 ? [] : new byte[expected];
        var offset = 0;
        while (offset < buffer.Length)
        {
            var read = decoder.Read(buffer, offset, buffer.Length - offset);
            if (read == 0)
            {
                throw new EntryLengthException($"Entry [{entryName}] decompressed to {offset} bytes, but the archive declares {expected}.");
            }

            offset += read;
        }

        if (decoder.ReadByte() >= 0)
        {
            throw new EntryLengthException($"Entry [{entryName}] decompressed to more than the declared {expected} bytes.");
        }

        return buffer;
    }

    /// <summary>Scans the directory once for all the names, stopping early when every one has been found. Entries that are absent are left out.</summary>
    private static Dictionary<string, Entry> FindEntries(Stream stream, IReadOnlyCollection<string> entryNames, CancellationToken cancellationToken)
    {
        var targets = entryNames.Distinct(StringComparer.OrdinalIgnoreCase).Select(name => (Name: name, Bytes: Encoding.UTF8.GetBytes(name))).ToList();
        var found = new Dictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);
        if (targets.Count == 0) return found;

        var (entryCount, directoryOffset) = ReadDirectoryLocation(stream);
        stream.Seek(directoryOffset, SeekOrigin.Begin);

        // Buffering is local to the scan. Nothing rewinds the stream; ReadData seeks to each local header itself.
        var buffered = new BufferedStream(stream, 1 << 20);
        var fixedPart = new byte[CentralDirectoryFixedSize];
        var variablePart = new byte[ushort.MaxValue * 3];

        for (long i = 0; i < entryCount && found.Count < targets.Count; i++)
        {
            if ((i & (CancellationCheckInterval - 1)) == 0) cancellationToken.ThrowIfCancellationRequested();
            buffered.ReadExactly(fixedPart);
            if (BinaryPrimitives.ReadUInt32LittleEndian(fixedPart) != CentralDirectorySignature)
            {
                throw new InvalidDataException($"Bad central directory signature at entry {i}.");
            }

            var nameLength = BinaryPrimitives.ReadUInt16LittleEndian(fixedPart.AsSpan(28));
            var extraLength = BinaryPrimitives.ReadUInt16LittleEndian(fixedPart.AsSpan(30));
            var commentLength = BinaryPrimitives.ReadUInt16LittleEndian(fixedPart.AsSpan(32));
            var variableLength = nameLength + extraLength + commentLength;
            buffered.ReadExactly(variablePart.AsSpan(0, variableLength));

            // A plain loop: this runs once per directory entry, and a real archive has hundreds of thousands.
            string? matchedName = null;
            for (var t = 0; t < targets.Count; t++)
            {
                if (found.ContainsKey(targets[t].Name) || !NameEquals(variablePart.AsSpan(0, nameLength), targets[t].Bytes)) continue;
                matchedName = targets[t].Name;
                break;
            }

            if (matchedName == null) continue;

            var compressedSize = (long) BinaryPrimitives.ReadUInt32LittleEndian(fixedPart.AsSpan(20));
            var uncompressedSize = (long) BinaryPrimitives.ReadUInt32LittleEndian(fixedPart.AsSpan(24));
            var localHeaderOffset = (long) BinaryPrimitives.ReadUInt32LittleEndian(fixedPart.AsSpan(42));
            ApplyZip64Extra(variablePart.AsSpan(nameLength, extraLength), ref uncompressedSize, ref compressedSize, ref localHeaderOffset, matchedName);

            found[matchedName] = new Entry(
                BinaryPrimitives.ReadUInt16LittleEndian(fixedPart.AsSpan(8)),
                BinaryPrimitives.ReadUInt16LittleEndian(fixedPart.AsSpan(10)),
                BinaryPrimitives.ReadUInt32LittleEndian(fixedPart.AsSpan(16)),
                compressedSize,
                uncompressedSize,
                localHeaderOffset);
        }

        return found;
    }

    private static bool NameEquals(ReadOnlySpan<byte> name, byte[] target)
    {
        if (name.Length != target.Length) return false;
        for (var i = 0; i < name.Length; i++)
        {
            var a = name[i];
            var b = target[i];
            if (a == b) continue;
            if (a is >= (byte) 'A' and <= (byte) 'Z') a += 32;
            if (b is >= (byte) 'A' and <= (byte) 'Z') b += 32;
            if (a == (byte) '\\') a = (byte) '/';
            if (b == (byte) '\\') b = (byte) '/';
            if (a != b) return false;
        }

        return true;
    }

    // ZIP64 extra field (id 0x0001) holds, in order, only the values whose 32-bit field is 0xFFFFFFFF.
    private static void ApplyZip64Extra(ReadOnlySpan<byte> extra, ref long uncompressedSize, ref long compressedSize, ref long localHeaderOffset, string entryName)
    {
        var needUncompressed = uncompressedSize == uint.MaxValue;
        var needCompressed = compressedSize == uint.MaxValue;
        var needOffset = localHeaderOffset == uint.MaxValue;
        if (!needUncompressed && !needCompressed && !needOffset) return;

        while (extra.Length >= 4)
        {
            var id = BinaryPrimitives.ReadUInt16LittleEndian(extra);
            var size = BinaryPrimitives.ReadUInt16LittleEndian(extra.Slice(2));
            if (extra.Length < 4 + size) break;

            if (id == 0x0001)
            {
                var data = extra.Slice(4, size);
                var position = 0;
                if (needUncompressed && position + 8 <= data.Length)
                {
                    uncompressedSize = (long) BinaryPrimitives.ReadUInt64LittleEndian(data.Slice(position));
                    position += 8;
                    needUncompressed = false;
                }

                if (needCompressed && position + 8 <= data.Length)
                {
                    compressedSize = (long) BinaryPrimitives.ReadUInt64LittleEndian(data.Slice(position));
                    position += 8;
                    needCompressed = false;
                }

                if (needOffset && position + 8 <= data.Length)
                {
                    localHeaderOffset = (long) BinaryPrimitives.ReadUInt64LittleEndian(data.Slice(position));
                    needOffset = false;
                }

                break;
            }

            extra = extra.Slice(4 + size);
        }

        if (needUncompressed || needCompressed || needOffset)
        {
            throw new InvalidDataException($"Entry [{entryName}] has a size or offset of 0xFFFFFFFF but its ZIP64 extra field is missing or too short.");
        }
    }

    private static (long EntryCount, long DirectoryOffset) ReadDirectoryLocation(Stream stream)
    {
        // The end-of-central-directory record is 22 bytes plus an optional comment.
        var tailLength = (int) Math.Min(stream.Length, 22 + 20 + ushort.MaxValue);
        var tail = new byte[tailLength];
        stream.Seek(stream.Length - tailLength, SeekOrigin.Begin);
        stream.ReadExactly(tail);

        var eocd = -1;
        for (var i = tail.Length - 22; i >= 0; i--)
        {
            if (BinaryPrimitives.ReadUInt32LittleEndian(tail.AsSpan(i)) == EocdSignature) { eocd = i; break; }
        }

        if (eocd < 0) throw new InvalidDataException("Could not find the end of central directory record.");

        var entryCount = (long) BinaryPrimitives.ReadUInt16LittleEndian(tail.AsSpan(eocd + 10));
        var directoryOffset = (long) BinaryPrimitives.ReadUInt32LittleEndian(tail.AsSpan(eocd + 16));
        var countIsSentinel = entryCount == ushort.MaxValue;
        var offsetIsSentinel = directoryOffset == uint.MaxValue;
        if (!countIsSentinel && !offsetIsSentinel) return (entryCount, directoryOffset);

        // The 20-byte ZIP64 locator sits immediately before the EOCD. A plain archive can still hold exactly 65535 entries.
        var locatorPresent = eocd >= 20 && BinaryPrimitives.ReadUInt32LittleEndian(tail.AsSpan(eocd - 20)) == Zip64LocatorSignature;
        if (!locatorPresent)
        {
            if (!offsetIsSentinel) return (entryCount, directoryOffset);
            throw new InvalidDataException("Could not find the ZIP64 end of central directory locator.");
        }

        var zip64Offset = (long) BinaryPrimitives.ReadUInt64LittleEndian(tail.AsSpan(eocd - 20 + 8));
        var record = new byte[56];
        stream.Seek(zip64Offset, SeekOrigin.Begin);
        stream.ReadExactly(record);
        if (BinaryPrimitives.ReadUInt32LittleEndian(record) != Zip64EocdSignature)
        {
            throw new InvalidDataException("Bad ZIP64 end of central directory record signature.");
        }

        return ((long) BinaryPrimitives.ReadUInt64LittleEndian(record.AsSpan(32)), (long) BinaryPrimitives.ReadUInt64LittleEndian(record.AsSpan(48)));
    }

    internal static uint Crc32(ReadOnlySpan<byte> data)
    {
        var crc = 0xFFFFFFFFu;
        foreach (var b in data) crc = CrcTable[(crc ^ b) & 0xFF] ^ (crc >> 8);
        return ~crc;
    }

    private static uint[] CreateCrcTable()
    {
        var table = new uint[256];
        for (uint i = 0; i < table.Length; i++)
        {
            var crc = i;
            for (var bit = 0; bit < 8; bit++) crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320u : crc >> 1;
            table[i] = crc;
        }

        return table;
    }
}
