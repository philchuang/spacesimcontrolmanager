using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;
using ZstdSharp;

namespace SSCM.StarCitizen.P4k;

/// <summary>
/// Minimal read-only access to a Star Citizen Data.p4k archive.
/// A p4k is a ZIP64 archive that uses a nonstandard local header signature and
/// zstd (method 100) compression. Only unencrypted entries are supported.
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

    private sealed record Entry(ushort Flags, ushort Method, long CompressedSize, long UncompressedSize, long LocalHeaderOffset);

    /// <summary>Reads the named entry (case-insensitive, '/' and '\' are equivalent), or throws <see cref="FileNotFoundException"/>.</summary>
    public static byte[] ReadEntry(string p4kPath, string entryName)
    {
        using var stream = new FileStream(p4kPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 1 << 20, FileOptions.RandomAccess);
        return ReadEntry(stream, entryName);
    }

    public static byte[] ReadEntry(Stream stream, string entryName)
    {
        return ReadEntries(stream, [entryName]).TryGetValue(entryName, out var data)
            ? data
            : throw new FileNotFoundException($"Entry [{entryName}] was not found in the archive.", entryName);
    }

    /// <summary>
    /// Reads several entries with a single scan of the archive's directory, which is the slow part of a p4k.
    /// Names match the same way as <see cref="ReadEntry(string, string)"/>. The result is keyed by the name as requested
    /// (case-insensitively) and leaves out any entry that is not in the archive, so callers decide which ones are required.
    /// </summary>
    public static Dictionary<string, byte[]> ReadEntries(string p4kPath, IReadOnlyCollection<string> entryNames)
    {
        using var stream = new FileStream(p4kPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 1 << 20, FileOptions.RandomAccess);
        return ReadEntries(stream, entryNames);
    }

    public static Dictionary<string, byte[]> ReadEntries(Stream stream, IReadOnlyCollection<string> entryNames)
    {
        var result = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
        foreach (var (name, entry) in FindEntries(stream, entryNames))
        {
            result[name] = ReadData(stream, entry, name);
        }

        return result;
    }

    private static byte[] ReadData(Stream stream, Entry entry, string entryName)
    {
        if ((entry.Flags & 1) != 0) throw new NotSupportedException($"Entry [{entryName}] is encrypted, which is not supported.");

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
        stream.Seek(entry.LocalHeaderOffset + LocalHeaderFixedSize + nameLength + extraLength, SeekOrigin.Begin);

        var compressed = new byte[entry.CompressedSize];
        stream.ReadExactly(compressed);
        return Decompress(entry, compressed, entryName);
    }

    private static byte[] Decompress(Entry entry, byte[] compressed, string entryName)
    {
        switch (entry.Method)
        {
            case MethodStore:
                return compressed;
            case MethodDeflate:
            {
                using var input = new MemoryStream(compressed);
                using var deflate = new DeflateStream(input, CompressionMode.Decompress);
                using var output = new MemoryStream();
                deflate.CopyTo(output);
                return output.ToArray();
            }
            case MethodZstd:
            {
                using var input = new MemoryStream(compressed);
                using var zstd = new DecompressionStream(input);
                using var output = new MemoryStream((int) Math.Min(entry.UncompressedSize, int.MaxValue));
                zstd.CopyTo(output);
                return output.ToArray();
            }
            default:
                throw new NotSupportedException($"Entry [{entryName}] uses unsupported compression method {entry.Method}.");
        }
    }

    /// <summary>Scans the directory once for all the names, stopping early when every one has been found. Entries that are absent are left out.</summary>
    private static Dictionary<string, Entry> FindEntries(Stream stream, IReadOnlyCollection<string> entryNames)
    {
        var targets = entryNames.Distinct(StringComparer.OrdinalIgnoreCase).Select(name => (Name: name, Bytes: Encoding.UTF8.GetBytes(name))).ToList();
        var found = new Dictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);
        if (targets.Count == 0) return found;

        var (entryCount, directoryOffset) = ReadDirectoryLocation(stream);
        stream.Seek(directoryOffset, SeekOrigin.Begin);

        // Buffering is local to the scan. The caller's stream stays open and is repositioned afterwards.
        var buffered = new BufferedStream(stream, 1 << 20);
        var fixedPart = new byte[CentralDirectoryFixedSize];
        var variablePart = new byte[ushort.MaxValue * 3];

        for (long i = 0; i < entryCount && found.Count < targets.Count; i++)
        {
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
            ApplyZip64Extra(variablePart.AsSpan(nameLength, extraLength), ref uncompressedSize, ref compressedSize, ref localHeaderOffset);

            found[matchedName] = new Entry(
                BinaryPrimitives.ReadUInt16LittleEndian(fixedPart.AsSpan(8)),
                BinaryPrimitives.ReadUInt16LittleEndian(fixedPart.AsSpan(10)),
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
    private static void ApplyZip64Extra(ReadOnlySpan<byte> extra, ref long uncompressedSize, ref long compressedSize, ref long localHeaderOffset)
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
                if (needUncompressed && position + 8 <= data.Length) { uncompressedSize = (long) BinaryPrimitives.ReadUInt64LittleEndian(data.Slice(position)); position += 8; }
                if (needCompressed && position + 8 <= data.Length) { compressedSize = (long) BinaryPrimitives.ReadUInt64LittleEndian(data.Slice(position)); position += 8; }
                if (needOffset && position + 8 <= data.Length) { localHeaderOffset = (long) BinaryPrimitives.ReadUInt64LittleEndian(data.Slice(position)); }
                return;
            }

            extra = extra.Slice(4 + size);
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
        var needsZip64 = entryCount == ushort.MaxValue || directoryOffset == uint.MaxValue;
        if (!needsZip64 || eocd < 20) return (entryCount, directoryOffset);

        if (BinaryPrimitives.ReadUInt32LittleEndian(tail.AsSpan(eocd - 20)) != Zip64LocatorSignature)
        {
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
}
