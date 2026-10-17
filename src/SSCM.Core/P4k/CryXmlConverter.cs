using System.Buffers.Binary;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace SSCM.Core.P4k;

/// <summary>
/// Converts CryEngine binary XML ("CryXmlB") into an <see cref="XDocument"/>.
/// Plain-text XML is passed through unchanged.
///
/// Layout (little endian): 8-byte magic, then nine uint32 header fields:
/// file length, node table offset, node count, attribute table offset, attribute count,
/// child table offset, child count, string table offset, string table length.
/// Nodes are 28 bytes, attributes 8 bytes, children 4 bytes (node indexes).
/// </summary>
public static class CryXmlConverter
{
    private static readonly byte[] Magic = "CryXmlB\0"u8.ToArray();

    private const int HeaderSize = 44;
    private const int NodeSize = 28;
    private const int AttributeSize = 8;
    private const int ChildSize = 4;

    public static XDocument ToXDocument(byte[] data)
    {
        if (!IsCryXml(data))
        {
            using var stream = new MemoryStream(data);
            using var reader = XmlReader.Create(stream, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit });
            return XDocument.Load(reader);
        }

        var header = ReadHeader(data);
        if (header.NodeCount == 0) throw new InvalidDataException("CryXml contains no nodes.");
        var strings = new StringTable(data, header.StringOffset, header.StringLength);

        var elements = new XElement[header.NodeCount];
        for (var i = 0; i < header.NodeCount; i++)
        {
            var node = ReadNode(data, header, i);
            var element = new XElement(ToName(strings.Get(node.NameOffset)));

            for (var a = 0; a < node.AttributeCount; a++)
            {
                var attributeOffset = header.AttributeOffset + (long) (node.FirstAttributeIndex + a) * AttributeSize;
                EnsureRange(data, attributeOffset, AttributeSize, "attribute");
                var key = strings.Get(BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan((int) attributeOffset)));
                var value = strings.Get(BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan((int) attributeOffset + 4)));
                element.SetAttributeValue(ToName(key), value);
            }

            var content = strings.Get(node.ContentOffset);
            if (content.Length > 0) element.Add(content);

            elements[i] = element;
        }

        // XElement.Add clones a node that already has a parent, so a repeated index or a cycle would copy the subtree instead of failing.
        var attached = new HashSet<int>();
        var parentOf = new int[header.NodeCount];
        Array.Fill(parentOf, -1);
        for (var i = 0; i < header.NodeCount; i++)
        {
            var node = ReadNode(data, header, i);
            for (var c = 0; c < node.ChildCount; c++)
            {
                var childOffset = header.ChildOffset + (long) (node.FirstChildIndex + c) * ChildSize;
                EnsureRange(data, childOffset, ChildSize, "child");
                var childIndex = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan((int) childOffset));
                if (childIndex >= header.NodeCount) throw new InvalidDataException($"Child index {childIndex} is out of range.");

                var index = (int) childIndex;
                if (IsAncestor(parentOf, i, index)) throw new InvalidDataException($"Child index {childIndex} creates a cycle.");
                if (!attached.Add(index)) throw new InvalidDataException($"Child index {childIndex} is already in the tree.");
                parentOf[index] = i;
                elements[i].Add(elements[index]);
            }
        }

        return new XDocument(elements[0]);
    }

    private static bool IsAncestor(int[] parentOf, int node, int child)
    {
        for (var ancestor = node; ancestor >= 0; ancestor = parentOf[ancestor])
        {
            if (ancestor == child) return true;
        }

        return false;
    }

    public static bool IsCryXml(byte[] data) => data.Length >= HeaderSize && data.AsSpan(0, Magic.Length).SequenceEqual(Magic);

    private static XName ToName(string name) => System.Xml.XmlConvert.EncodeLocalName(name);

    private readonly record struct Header(
        long NodeOffset, uint NodeCount, long AttributeOffset, uint AttributeCount,
        long ChildOffset, uint ChildCount, long StringOffset, uint StringLength);

    private readonly record struct Node(
        uint NameOffset, uint ContentOffset, ushort AttributeCount, ushort ChildCount,
        uint FirstAttributeIndex, uint FirstChildIndex);

    private static Header ReadHeader(byte[] data)
    {
        uint Field(int index) => BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(8 + index * 4));

        var header = new Header(Field(1), Field(2), Field(3), Field(4), Field(5), Field(6), Field(7), Field(8));
        EnsureRange(data, header.NodeOffset, (long) header.NodeCount * NodeSize, "node table");
        EnsureRange(data, header.AttributeOffset, (long) header.AttributeCount * AttributeSize, "attribute table");
        EnsureRange(data, header.ChildOffset, (long) header.ChildCount * ChildSize, "child table");
        EnsureRange(data, header.StringOffset, header.StringLength, "string table");
        return header;
    }

    private static Node ReadNode(byte[] data, Header header, int index)
    {
        var span = data.AsSpan((int) (header.NodeOffset + (long) index * NodeSize), NodeSize);
        return new Node(
            BinaryPrimitives.ReadUInt32LittleEndian(span),
            BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(4)),
            BinaryPrimitives.ReadUInt16LittleEndian(span.Slice(8)),
            BinaryPrimitives.ReadUInt16LittleEndian(span.Slice(10)),
            BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(16)),
            BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(20)));
    }

    private static void EnsureRange(byte[] data, long offset, long length, string what)
    {
        if (offset < 0 || length < 0 || offset + length > data.Length)
        {
            throw new InvalidDataException($"CryXml {what} is outside of the file bounds.");
        }
    }

    private sealed class StringTable
    {
        private readonly byte[] _data;
        private readonly int _start;
        private readonly int _length;
        private readonly Dictionary<uint, string> _cache = new();

        public StringTable(byte[] data, long start, long length)
        {
            this._data = data;
            this._start = (int) start;
            this._length = (int) length;
        }

        public string Get(uint offset)
        {
            if (this._cache.TryGetValue(offset, out var cached)) return cached;
            if (offset >= this._length) throw new InvalidDataException($"String offset {offset} is out of range.");

            var span = this._data.AsSpan(this._start + (int) offset, this._length - (int) offset);
            var end = span.IndexOf((byte) 0);
            var value = Encoding.UTF8.GetString(end < 0 ? span : span.Slice(0, end));
            this._cache[offset] = value;
            return value;
        }
    }
}
