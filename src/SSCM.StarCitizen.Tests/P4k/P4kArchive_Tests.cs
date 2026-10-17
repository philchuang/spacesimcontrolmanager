using System.IO.Compression;
using System.Text;
using NUnit.Framework;
using SSCM.StarCitizen.P4k;

namespace SSCM.StarCitizen.Tests.P4k;

public class P4kArchive_Tests
{
    private const ushort Store = 0;
    private const ushort Deflate = 8;
    private const ushort Zstd = 100;

    [TestCase(false)]
    [TestCase(true)]
    public void ReadEntry_ReadsStoredDeflatedAndZstdEntries(bool zip64)
    {
        // Arrange
        var stored = Encoding.UTF8.GetBytes("stored content");
        var deflated = Encoding.UTF8.GetBytes(new string('d', 500));
        var zstd = Encoding.UTF8.GetBytes(new string('z', 500));
        var archive = ArchiveBuilder.Build(zip64,
            new ArchiveBuilder.Entry("Data/a.txt", Store, stored, stored),
            new ArchiveBuilder.Entry("Data/b.txt", Deflate, ArchiveBuilder.DeflateBytes(deflated), deflated),
            new ArchiveBuilder.Entry("Data/c.txt", Zstd, ArchiveBuilder.ZstdBytes(zstd), zstd));

        // Act
        using var stream = new MemoryStream(archive);

        // Assert
        Assert.AreEqual(stored, P4kArchive.ReadEntry(stream, "Data/a.txt"));
        Assert.AreEqual(deflated, P4kArchive.ReadEntry(stream, "Data/b.txt"));
        Assert.AreEqual(zstd, P4kArchive.ReadEntry(stream, "Data/c.txt"));
    }

    [Test]
    public void ReadEntry_MatchesNamesIgnoringCaseAndSeparatorStyle()
    {
        // Arrange
        var data = Encoding.UTF8.GetBytes("profile");
        var archive = ArchiveBuilder.Build(false, new ArchiveBuilder.Entry(@"Data\Libs\Config\defaultProfile.xml", Zstd, ArchiveBuilder.ZstdBytes(data), data));

        // Act
        using var stream = new MemoryStream(archive);
        var actual = P4kArchive.ReadEntry(stream, "data/libs/config/DEFAULTPROFILE.XML");

        // Assert
        Assert.AreEqual(data, actual);
    }

    [Test]
    public void ReadEntry_AcceptsStandardLocalHeaderSignature()
    {
        // Arrange
        var data = Encoding.UTF8.GetBytes("standard");
        var archive = ArchiveBuilder.Build(false, localSignature: 0x04034b50, new ArchiveBuilder.Entry("a.txt", Store, data, data));

        // Act
        using var stream = new MemoryStream(archive);

        // Assert
        Assert.AreEqual(data, P4kArchive.ReadEntry(stream, "a.txt"));
    }

    [Test]
    public void ReadEntry_ThrowsWhenEntryIsMissing()
    {
        // Arrange
        var data = Encoding.UTF8.GetBytes("x");
        var archive = ArchiveBuilder.Build(false, new ArchiveBuilder.Entry("a.txt", Store, data, data));

        // Act & Assert
        using var stream = new MemoryStream(archive);
        var ex = Assert.Throws<FileNotFoundException>(() => P4kArchive.ReadEntry(stream, "missing.txt"));
        Assert.That(ex!.Message, Does.Contain("missing.txt"));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ReadEntries_ReadsSeveralEntriesInOnePassAndLeavesOutMissingOnes(bool zip64)
    {
        // Arrange
        var first = Encoding.UTF8.GetBytes("first");
        var second = Encoding.UTF8.GetBytes(new string('s', 400));
        var other = Encoding.UTF8.GetBytes("other");
        var archive = ArchiveBuilder.Build(zip64,
            new ArchiveBuilder.Entry(@"Data\Libs\a.xml", Zstd, ArchiveBuilder.ZstdBytes(first), first),
            new ArchiveBuilder.Entry("Data/other.txt", Store, other, other),
            new ArchiveBuilder.Entry("Data/Localization/english/global.ini", Deflate, ArchiveBuilder.DeflateBytes(second), second));

        // Act
        using var stream = new MemoryStream(archive);
        var entries = P4kArchive.ReadEntries(stream, ["data/libs/A.XML", "Data/Localization/english/global.ini", "Data/missing.txt"]);

        // Assert
        Assert.AreEqual(2, entries.Count);
        Assert.AreEqual(first, entries["data/libs/A.XML"], "keyed by the name as requested");
        Assert.AreEqual(second, entries["Data/Localization/english/global.ini"]);
        Assert.IsFalse(entries.ContainsKey("Data/missing.txt"));
    }

    [Test]
    public void ReadEntries_StopsScanningOnceEveryRequestedEntryIsFound()
    {
        // Arrange
        var data = Encoding.UTF8.GetBytes("a");
        var archive = ArchiveBuilder.Build(false,
            new ArchiveBuilder.Entry("a.txt", Store, data, data),
            new ArchiveBuilder.Entry("b.txt", Store, data, data));
        using var stream = new MemoryStream(archive);

        // Act
        var entries = P4kArchive.ReadEntries(stream, ["a.txt", "A.TXT"]);

        // Assert
        Assert.AreEqual(1, entries.Count, "the same name asked for twice is one entry");
        Assert.AreEqual(data, entries["a.txt"]);
    }

    [Test]
    public void ReadEntries_ReturnsNothingWhenNoNamesAreRequested()
    {
        // Arrange
        using var stream = new MemoryStream(new byte[100]);

        // Act & Assert
        Assert.IsEmpty(P4kArchive.ReadEntries(stream, []));
    }

    [Test]
    public void ReadEntry_ThrowsWhenEntryIsEncrypted()
    {
        // Arrange
        var data = Encoding.UTF8.GetBytes("secret");
        var archive = ArchiveBuilder.Build(false, new ArchiveBuilder.Entry("a.txt", Store, data, data, Flags: 1));

        // Act & Assert
        using var stream = new MemoryStream(archive);
        Assert.Throws<NotSupportedException>(() => P4kArchive.ReadEntry(stream, "a.txt"));
    }

    [Test]
    public void ReadEntry_ThrowsForUnsupportedCompressionMethod()
    {
        // Arrange
        var data = Encoding.UTF8.GetBytes("odd");
        var archive = ArchiveBuilder.Build(false, new ArchiveBuilder.Entry("a.txt", 99, data, data));

        // Act & Assert
        using var stream = new MemoryStream(archive);
        Assert.Throws<NotSupportedException>(() => P4kArchive.ReadEntry(stream, "a.txt"));
    }

    [Test]
    public void ReadEntry_ThrowsWhenNotAnArchive()
    {
        // Arrange
        using var stream = new MemoryStream(new byte[100]);

        // Act & Assert
        Assert.Throws<InvalidDataException>(() => P4kArchive.ReadEntry(stream, "a.txt"));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ReadEntry_ThrowsWhenZip64SizeHasNoUsableExtraField(bool shortExtra)
    {
        // Arrange
        var data = Encoding.UTF8.GetBytes("profile");
        var archive = ArchiveBuilder.Build(new ArchiveBuilder.BuildOptions(SentinelSizes: true, ShortZip64Extra: shortExtra),
            new ArchiveBuilder.Entry("a.txt", Store, data, data));

        // Act & Assert
        using var stream = new MemoryStream(archive);
        var ex = Assert.Throws<InvalidDataException>(() => P4kArchive.ReadEntry(stream, "a.txt"));
        Assert.That(ex!.Message, Does.Contain("a.txt").And.Contain("ZIP64"));
    }

    [Test]
    public void ReadEntry_ThrowsWhenEntryExtendsPastTheArchive()
    {
        // Arrange
        var data = Encoding.UTF8.GetBytes("short");
        var archive = ArchiveBuilder.Build(new ArchiveBuilder.BuildOptions(CompressedSize: 100_000),
            new ArchiveBuilder.Entry("a.txt", Store, data, data));

        // Act & Assert
        using var stream = new MemoryStream(archive);
        var ex = Assert.Throws<InvalidDataException>(() => P4kArchive.ReadEntry(stream, "a.txt"));
        Assert.That(ex!.Message, Does.Contain("a.txt").And.Contain("end of the archive"));
    }

    [Test]
    public void ReadEntry_ThrowsWhenDecompressedLengthDoesNotMatch()
    {
        // Arrange
        var payload = Encoding.UTF8.GetBytes(new string('d', 50));
        var declared = new byte[payload.Length + 10];
        var archive = ArchiveBuilder.Build(false, new ArchiveBuilder.Entry("a.txt", Deflate, ArchiveBuilder.DeflateBytes(payload), declared));

        // Act & Assert
        using var stream = new MemoryStream(archive);
        var ex = Assert.Throws<InvalidDataException>(() => P4kArchive.ReadEntry(stream, "a.txt"));
        Assert.That(ex!.Message, Does.Contain("a.txt").And.Contain("decompressed to"));
    }

    [Test]
    public void ReadEntry_ThrowsWhenCrcDoesNotMatch()
    {
        // Arrange
        var original = new byte[] { 1, 2, 3, 4 };
        var stored = new byte[] { 1, 2, 3, 5 };
        var archive = ArchiveBuilder.Build(false, new ArchiveBuilder.Entry("a.txt", Store, stored, original));

        // Act & Assert
        using var stream = new MemoryStream(archive);
        var ex = Assert.Throws<InvalidDataException>(() => P4kArchive.ReadEntry(stream, "a.txt"));
        Assert.That(ex!.Message, Does.Contain("a.txt").And.Contain("CRC32"));
    }

    [Test]
    public void ReadEntry_ThrowsWhenDecodeFails()
    {
        // Arrange
        var declared = new byte[32];
        var archive = ArchiveBuilder.Build(false, new ArchiveBuilder.Entry("secret.txt", Zstd, new byte[16], declared));

        // Act & Assert
        using var stream = new MemoryStream(archive);
        var ex = Assert.Throws<InvalidDataException>(() => P4kArchive.ReadEntry(stream, "secret.txt"));
        Assert.That(ex!.Message, Does.Contain("secret.txt").And.Contain("decode"));
    }

    [Test]
    public void ReadEntry_ReadsPlainArchiveThatReportsExactly65535Entries()
    {
        // Arrange
        var data = Encoding.UTF8.GetBytes("only entry");
        var archive = ArchiveBuilder.Build(new ArchiveBuilder.BuildOptions(EntryCount: ushort.MaxValue),
            new ArchiveBuilder.Entry("a.txt", Store, data, data));

        // Act
        using var stream = new MemoryStream(archive);
        var actual = P4kArchive.ReadEntry(stream, "a.txt");

        // Assert
        Assert.AreEqual(data, actual);
    }

    [Test]
    public void ReadEntry_ThrowsWhenZip64OffsetHasNoLocator()
    {
        // Arrange: an EOCD whose directory offset is the ZIP64 sentinel, with no room for a locator in front of it.
        var eocd = new byte[22];
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(eocd, 0x06054b50);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt16LittleEndian(eocd.AsSpan(10), 1);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(eocd.AsSpan(16), uint.MaxValue);

        // Act & Assert
        using var stream = new MemoryStream(eocd);
        var ex = Assert.Throws<InvalidDataException>(() => P4kArchive.ReadEntry(stream, "a.txt"));
        Assert.That(ex!.Message, Does.Contain("ZIP64"));
    }

    [Test]
    public void ReadEntry_ThrowsWhenEntryExceedsSizeCap()
    {
        // Arrange
        var data = Encoding.UTF8.GetBytes("tiny");
        var archive = ArchiveBuilder.Build(new ArchiveBuilder.BuildOptions(UncompressedSize: (uint) (P4kArchive.MaxEntryBytes + 1)),
            new ArchiveBuilder.Entry("a.txt", Store, data, data));

        // Act & Assert
        using var stream = new MemoryStream(archive);
        var ex = Assert.Throws<InvalidDataException>(() => P4kArchive.ReadEntry(stream, "a.txt"));
        Assert.That(ex!.Message, Does.Contain("a.txt").And.Contain(P4kArchive.MaxEntryBytes.ToString()));
    }

    internal static class ArchiveBuilder
    {
        public record Entry(string Name, ushort Method, byte[] Stored, byte[] Original, ushort Flags = 0);

        public static byte[] DeflateBytes(byte[] data)
        {
            using var output = new MemoryStream();
            using (var deflate = new DeflateStream(output, CompressionLevel.Optimal, leaveOpen: true)) deflate.Write(data);
            return output.ToArray();
        }

        public static byte[] ZstdBytes(byte[] data)
        {
            using var compressor = new ZstdSharp.Compressor();
            return compressor.Wrap(data).ToArray();
        }

        public sealed record BuildOptions(
            bool Zip64 = false,
            uint LocalSignature = 0x14034b50,
            bool SentinelSizes = false,
            bool ShortZip64Extra = false,
            uint? CompressedSize = null,
            uint? UncompressedSize = null,
            ushort? EntryCount = null);

        public static byte[] Build(bool zip64, params Entry[] entries) => Build(zip64, 0x14034b50, entries);

        public static byte[] Build(bool zip64, uint localSignature, params Entry[] entries) =>
            Build(new BuildOptions(zip64, localSignature), entries);

        public static byte[] Build(BuildOptions options, params Entry[] entries)
        {
            var output = new MemoryStream();
            var writer = new BinaryWriter(output);
            var offsets = new List<long>();
            var useSentinel = options.Zip64 || options.SentinelSizes;

            foreach (var entry in entries)
            {
                offsets.Add(output.Position);
                var name = Encoding.UTF8.GetBytes(entry.Name);
                writer.Write(options.LocalSignature);
                writer.Write((ushort) 45);
                writer.Write(entry.Flags);
                writer.Write(entry.Method);
                writer.Write((ushort) 0);
                writer.Write((ushort) 0);
                writer.Write(P4kArchive.Crc32(entry.Original));
                writer.Write((uint) entry.Stored.Length);
                writer.Write((uint) entry.Original.Length);
                writer.Write((ushort) name.Length);
                writer.Write((ushort) 0);
                writer.Write(name);
                writer.Write(entry.Stored);
            }

            var directoryOffset = output.Position;
            for (var i = 0; i < entries.Length; i++)
            {
                var entry = entries[i];
                var name = Encoding.UTF8.GetBytes(entry.Name);
                var extra = Array.Empty<byte>();
                if (options.Zip64)
                {
                    using var extraStream = new MemoryStream();
                    var extraWriter = new BinaryWriter(extraStream);
                    extraWriter.Write((ushort) 1);
                    extraWriter.Write((ushort) 24);
                    extraWriter.Write((ulong) entry.Original.Length);
                    extraWriter.Write((ulong) entry.Stored.Length);
                    extraWriter.Write((ulong) offsets[i]);
                    extra = extraStream.ToArray();
                }
                else if (options.ShortZip64Extra)
                {
                    using var extraStream = new MemoryStream();
                    var extraWriter = new BinaryWriter(extraStream);
                    extraWriter.Write((ushort) 1);
                    extraWriter.Write((ushort) 4);
                    extraWriter.Write(0u);
                    extra = extraStream.ToArray();
                }

                writer.Write(0x02014b50u);
                writer.Write((ushort) 45);
                writer.Write((ushort) 45);
                writer.Write(entry.Flags);
                writer.Write(entry.Method);
                writer.Write((ushort) 0);
                writer.Write((ushort) 0);
                writer.Write(P4kArchive.Crc32(entry.Original));
                writer.Write(useSentinel ? uint.MaxValue : options.CompressedSize ?? (uint) entry.Stored.Length);
                writer.Write(useSentinel ? uint.MaxValue : options.UncompressedSize ?? (uint) entry.Original.Length);
                writer.Write((ushort) name.Length);
                writer.Write((ushort) extra.Length);
                writer.Write((ushort) 0);
                writer.Write((ushort) 0);
                writer.Write((ushort) 0);
                writer.Write(0u);
                writer.Write(useSentinel ? uint.MaxValue : (uint) offsets[i]);
                writer.Write(name);
                writer.Write(extra);
            }

            var directorySize = output.Position - directoryOffset;

            if (options.Zip64)
            {
                var zip64RecordOffset = output.Position;
                writer.Write(0x06064b50u);
                writer.Write(44ul);
                writer.Write((ushort) 45);
                writer.Write((ushort) 45);
                writer.Write(0u);
                writer.Write(0u);
                writer.Write((ulong) entries.Length);
                writer.Write((ulong) entries.Length);
                writer.Write((ulong) directorySize);
                writer.Write((ulong) directoryOffset);

                writer.Write(0x07064b50u);
                writer.Write(0u);
                writer.Write((ulong) zip64RecordOffset);
                writer.Write(1u);
            }

            var reportedCount = options.Zip64 ? ushort.MaxValue : options.EntryCount ?? (ushort) entries.Length;
            writer.Write(0x06054b50u);
            writer.Write((ushort) 0);
            writer.Write((ushort) 0);
            writer.Write(reportedCount);
            writer.Write(reportedCount);
            writer.Write((uint) directorySize);
            writer.Write(options.Zip64 ? uint.MaxValue : (uint) directoryOffset);
            writer.Write((ushort) 0);

            return output.ToArray();
        }
    }
}
