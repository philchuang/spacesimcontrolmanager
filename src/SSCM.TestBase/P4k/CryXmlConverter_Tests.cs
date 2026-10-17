using System.Buffers.Binary;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using NUnit.Framework;
using SSCM.Core.P4k;

namespace SSCM.Tests.P4k;

public class CryXmlConverter_Tests
{
    [Test]
    public void ToXDocument_ConvertsNodesAttributesTextAndChildren()
    {
        // Arrange
        var data = CryXmlBuilder.Build(new CryXmlBuilder.Node("profile", [("version", "1")], "",
        [
            new CryXmlBuilder.Node("actionmap", [("name", "seat"), ("UILabel", "@ui_seat")], "",
            [
                new CryXmlBuilder.Node("action", [("name", "fire"), ("joystick", " ")], "", []),
                new CryXmlBuilder.Node("action", [("name", "look")], "some text", []),
            ]),
            new CryXmlBuilder.Node("actionmap", [("name", "flight")], "", []),
        ]));

        // Act
        var doc = CryXmlConverter.ToXDocument(data);

        // Assert
        Assert.AreEqual("profile", doc.Root!.Name.LocalName);
        Assert.AreEqual("1", (string?) doc.Root.Attribute("version"));
        var maps = doc.Root.Elements("actionmap").ToList();
        Assert.AreEqual(new[] { "seat", "flight" }, maps.Select(m => (string?) m.Attribute("name")));
        Assert.AreEqual("@ui_seat", (string?) maps[0].Attribute("UILabel"));

        var actions = maps[0].Elements("action").ToList();
        Assert.AreEqual(2, actions.Count);
        Assert.AreEqual(" ", (string?) actions[0].Attribute("joystick"), "whitespace-only values must be preserved");
        Assert.AreEqual("some text", actions[1].Value);
        Assert.AreEqual(0, maps[1].Elements().Count());
    }

    [Test]
    public void ToXDocument_PassesPlainXmlThrough()
    {
        // Arrange
        var data = Encoding.UTF8.GetBytes("<profile><actionmap name=\"seat\" /></profile>");

        // Act
        var doc = CryXmlConverter.ToXDocument(data);

        // Assert
        Assert.AreEqual("seat", (string?) doc.Root!.Element("actionmap")!.Attribute("name"));
    }

    [Test]
    public void IsCryXml_DetectsMagic()
    {
        // Arrange
        var binary = CryXmlBuilder.Build(new CryXmlBuilder.Node("profile", [], "", []));

        // Act & Assert
        Assert.IsTrue(CryXmlConverter.IsCryXml(binary));
        Assert.IsFalse(CryXmlConverter.IsCryXml(Encoding.UTF8.GetBytes("<profile />")));
    }

    [Test]
    public void ToXDocument_ThrowsWhenAChildIndexIsReused()
    {
        // Arrange
        var data = CryXmlBuilder.Build(new CryXmlBuilder.Node("profile", [], "",
        [
            new CryXmlBuilder.Node("a", [], "", []),
            new CryXmlBuilder.Node("b", [], "", []),
        ]));
        WriteChildIndex(data, 1, 1);

        // Act & Assert
        var ex = Assert.Throws<InvalidDataException>(() => CryXmlConverter.ToXDocument(data));
        Assert.That(ex!.Message, Does.Contain("already in the tree"));
    }

    [Test]
    public void ToXDocument_ThrowsWhenANodeIsItsOwnChild()
    {
        // Arrange
        var data = CryXmlBuilder.Build(new CryXmlBuilder.Node("profile", [], "",
        [
            new CryXmlBuilder.Node("a", [], "", []),
        ]));
        WriteChildIndex(data, 0, 0);

        // Act & Assert
        var ex = Assert.Throws<InvalidDataException>(() => CryXmlConverter.ToXDocument(data));
        Assert.That(ex!.Message, Does.Contain("cycle"));
    }

    [Test]
    public void ToXDocument_ThrowsWhenAChildIndexEqualsTheNodeCount()
    {
        // Arrange
        var data = CryXmlBuilder.Build(new CryXmlBuilder.Node("profile", [], "",
        [
            new CryXmlBuilder.Node("a", [], "", []),
        ]));
        WriteChildIndex(data, 0, 2);

        // Act & Assert
        var ex = Assert.Throws<InvalidDataException>(() => CryXmlConverter.ToXDocument(data));
        Assert.That(ex!.Message, Does.Contain("out of range"));
    }

    [Test]
    public void ToXDocument_ThrowsWhenThereAreNoNodes()
    {
        // Arrange
        var data = new byte[44];
        Encoding.ASCII.GetBytes("CryXmlB\0").CopyTo(data, 0);

        // Act & Assert
        var ex = Assert.Throws<InvalidDataException>(() => CryXmlConverter.ToXDocument(data));
        Assert.That(ex!.Message, Does.Contain("no nodes"));
    }

    [Test]
    public void ToXDocument_RejectsAPlainXmlDtd()
    {
        // Arrange
        var data = Encoding.UTF8.GetBytes("<!DOCTYPE profile [<!ENTITY x \"secret\">]><profile>&x;</profile>");

        // Act & Assert
        Assert.Throws<XmlException>(() => CryXmlConverter.ToXDocument(data));
    }

    [Test]
    public void ToXDocument_ThrowsWhenTablesAreOutsideTheFile()
    {
        // Arrange
        var data = CryXmlBuilder.Build(new CryXmlBuilder.Node("profile", [("a", "b")], "", []));
        var truncated = data[..(data.Length - 12)];

        // Act & Assert
        Assert.Throws<InvalidDataException>(() => CryXmlConverter.ToXDocument(truncated));
    }

    private static void WriteChildIndex(byte[] data, int childNumber, uint index)
    {
        var childOffset = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(28));
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan((int) childOffset + childNumber * 4), index);
    }

    /// <summary>Writes the CryXmlB layout: header, nodes, children, attributes, then the string table.</summary>
    internal static class CryXmlBuilder
    {
        public record Node(string Name, (string Key, string Value)[] Attributes, string Text, Node[] Children);

        public static byte[] Build(Node root)
        {
            var strings = new MemoryStream();
            var stringOffsets = new Dictionary<string, uint>();
            uint Str(string value)
            {
                if (stringOffsets.TryGetValue(value, out var existing)) return existing;
                var offset = (uint) strings.Length;
                strings.Write(Encoding.UTF8.GetBytes(value));
                strings.WriteByte(0);
                stringOffsets[value] = offset;
                return offset;
            }

            var nodes = new List<Node>();
            var parents = new List<int>();
            var indexes = new Dictionary<Node, int>(ReferenceEqualityComparer.Instance);
            void Flatten(Node node, int parent)
            {
                indexes[node] = nodes.Count;
                nodes.Add(node);
                parents.Add(parent);
                foreach (var child in node.Children) Flatten(child, indexes[node]);
            }
            Flatten(root, -1);

            var nodeTable = new MemoryStream();
            var childTable = new MemoryStream();
            var attributeTable = new MemoryStream();
            var nodeWriter = new BinaryWriter(nodeTable);
            var childWriter = new BinaryWriter(childTable);
            var attributeWriter = new BinaryWriter(attributeTable);
            uint childCount = 0, attributeCount = 0;

            for (var i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                nodeWriter.Write(Str(node.Name));
                nodeWriter.Write(Str(node.Text));
                nodeWriter.Write((ushort) node.Attributes.Length);
                nodeWriter.Write((ushort) node.Children.Length);
                nodeWriter.Write(parents[i] < 0 ? uint.MaxValue : (uint) parents[i]);
                nodeWriter.Write(attributeCount);
                nodeWriter.Write(childCount);
                nodeWriter.Write(0u);

                foreach (var (key, value) in node.Attributes)
                {
                    attributeWriter.Write(Str(key));
                    attributeWriter.Write(Str(value));
                    attributeCount++;
                }

                foreach (var child in node.Children)
                {
                    childWriter.Write((uint) indexes[child]);
                    childCount++;
                }
            }

            const int headerSize = 44;
            var nodeOffset = headerSize;
            var childOffset = nodeOffset + (int) nodeTable.Length;
            var attributeOffset = childOffset + (int) childTable.Length;
            var stringOffset = attributeOffset + (int) attributeTable.Length;
            var total = stringOffset + (int) strings.Length;

            var output = new MemoryStream();
            var writer = new BinaryWriter(output);
            writer.Write(Encoding.ASCII.GetBytes("CryXmlB\0"));
            writer.Write((uint) total);
            writer.Write((uint) nodeOffset);
            writer.Write((uint) nodes.Count);
            writer.Write((uint) attributeOffset);
            writer.Write(attributeCount);
            writer.Write((uint) childOffset);
            writer.Write(childCount);
            writer.Write((uint) stringOffset);
            writer.Write((uint) strings.Length);
            writer.Write(nodeTable.ToArray());
            writer.Write(childTable.ToArray());
            writer.Write(attributeTable.ToArray());
            writer.Write(strings.ToArray());
            return output.ToArray();
        }
    }
}
