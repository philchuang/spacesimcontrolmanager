using System.Text;
using System.Xml.Linq;
using NUnit.Framework;
using SSCM.StarCitizen.Bindings;
using SSCM.StarCitizen.Tests.Mocks;
using static SSCM.Tests.P4k.CryXmlConverter_Tests;
using static SSCM.Tests.P4k.P4kArchive_Tests;

namespace SSCM.StarCitizen.Tests.Bindings;

public class SCP4kGameDataSource_Tests
{
    private string _environmentDir = string.Empty;

    [SetUp]
    public void SetUp()
    {
        this._environmentDir = Path.Combine(Path.GetTempPath(), "sscm-tests", TestContext.CurrentContext.Test.ID, "LIVE");
        Directory.CreateDirectory(this._environmentDir);
    }

    [TearDown]
    public void TearDown()
    {
        var root = Directory.GetParent(this._environmentDir)!.FullName;
        if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
    }

    [Test]
    public void Normalize_RemovesUnforgeArtifactsIncludingTheTrailingWhitespace()
    {
        // Arrange
        var doc = new XDocument(new XElement("profile",
            new XElement("actionmap", new XAttribute("name", "a"), "/>" + new string(' ', 200)),
            new XElement("actionmap", new XAttribute("name", "b"), ">"),
            new XElement("actionmap", new XAttribute("name", "c"), new XCData("  />  "))));

        // Act
        SCP4kGameDataSource.Normalize(doc);

        // Assert
        Assert.AreEqual(0, doc.DescendantNodes().OfType<XText>().Count());
        Assert.AreEqual(3, doc.Root!.Elements("actionmap").Count());
    }

    [Test]
    public void Normalize_KeepsRealTextAndCData()
    {
        // Arrange
        var doc = new XDocument(new XElement("profile",
            new XElement("a", "some text />"),
            new XElement("b", "x > y"),
            new XElement("c", new XCData("   "))));

        // Act
        SCP4kGameDataSource.Normalize(doc);

        // Assert
        Assert.AreEqual("some text />", doc.Root!.Element("a")!.Value);
        Assert.AreEqual("x > y", doc.Root.Element("b")!.Value);
        Assert.AreEqual("   ", doc.Root.Element("c")!.Value, "whitespace in CDATA is significant and is kept");
    }

    [Test]
    public void Normalize_DropsWhitespaceOnlyTextButKeepsAttributes()
    {
        // Arrange
        var doc = new XDocument(new XElement("profile",
            "\n  ",
            new XElement("action", new XAttribute("joystick", " "), " \t\r\n"),
            "\n"));

        // Act
        SCP4kGameDataSource.Normalize(doc);

        // Assert
        Assert.AreEqual(0, doc.DescendantNodes().OfType<XText>().Count());
        Assert.AreEqual(" ", (string?) doc.Root!.Element("action")!.Attribute("joystick"), "attribute whitespace is data, not formatting");
    }

    [Test]
    public async Task Load_ExtractsConvertsAndNormalizesTheProfileFromDataP4k()
    {
        // Arrange
        var binary = CryXmlBuilder.Build(new CryXmlBuilder.Node("profile", [("version", "1")], "",
        [
            new CryXmlBuilder.Node("actionmap", [("name", "seat")], "/>" + new string(' ', 120),
            [
                new CryXmlBuilder.Node("action", [("name", "fire"), ("joystick", " ")], "", []),
            ]),
        ]));
        var archive = ArchiveBuilder.Build(true,
            new ArchiveBuilder.Entry("Data/Other.txt", 0, Encoding.UTF8.GetBytes("x"), Encoding.UTF8.GetBytes("x")),
            new ArchiveBuilder.Entry(@"Data\Libs\Config\defaultProfile.xml", 100, ArchiveBuilder.ZstdBytes(binary), binary));
        File.WriteAllBytes(Path.Combine(this._environmentDir, SCP4kGameDataSource.P4kFileName), archive);
        var source = new SCP4kGameDataSource(new SCFoldersForTest { GameEnvironmentDir = this._environmentDir });

        // Act
        var doc = await source.Load();

        // Assert
        Assert.AreEqual("profile", doc.Root!.Name.LocalName);
        var actionMap = doc.Root.Element("actionmap")!;
        Assert.AreEqual("seat", (string?) actionMap.Attribute("name"));
        Assert.AreEqual(0, doc.DescendantNodes().OfType<XText>().Count(), "the unforge artifact must be gone");
        Assert.AreEqual(" ", (string?) actionMap.Element("action")!.Attribute("joystick"));
    }

    [TestCase("LIVE", "LIVE")]
    [TestCase("ptu", "PTU")]
    public async Task Load_StartsWithACommentNamingTheEnvironmentAndArchiveDate(string environment, string expectedName)
    {
        // Arrange
        var binary = CryXmlBuilder.Build(new CryXmlBuilder.Node("profile", [], "", []));
        var archive = ArchiveBuilder.Build(true,
            new ArchiveBuilder.Entry(@"Data\Libs\Config\defaultProfile.xml", 100, ArchiveBuilder.ZstdBytes(binary), binary));
        var p4kPath = Path.Combine(this._environmentDir, SCP4kGameDataSource.P4kFileName);
        File.WriteAllBytes(p4kPath, archive);
        File.SetLastWriteTime(p4kPath, new DateTime(2026, 3, 9, 23, 59, 0, DateTimeKind.Local));
        var source = new SCP4kGameDataSource(new SCFoldersForTest { Environment = environment, GameEnvironmentDir = this._environmentDir });

        // Act
        var doc = await source.Load();

        // Assert
        var comment = doc.Nodes().First() as XComment;
        Assert.IsNotNull(comment, "the comment is the first node of the document");
        Assert.AreEqual($" extracted from {expectedName} Data.p4k dated 2026-03-09 ", comment!.Value);
        Assert.AreEqual("profile", doc.Root!.Name.LocalName);
    }

    [Test]
    public async Task Extract_ReturnsTheLocalizationBytesAlongsideTheProfileWhenAsked()
    {
        // Arrange
        var localization = Encoding.UTF8.GetBytes("ui_seat=Seat Controls\r\nui_fire=Fire\r\n");
        this.WriteArchive(localization);
        var source = this.CreateSource();

        // Act
        var data = await source.Extract(includeLocalization: true);

        // Assert
        Assert.AreEqual("profile", data.DefaultProfile.Root!.Name.LocalName);
        Assert.AreEqual(localization, data.Localization, "the file is returned exactly as stored");
    }

    [Test]
    public async Task Extract_LeavesOutTheLocalizationWhenNotAsked()
    {
        // Arrange
        this.WriteArchive(Encoding.UTF8.GetBytes("ui_seat=Seat Controls\n"));
        var source = this.CreateSource();

        // Act
        var data = await source.Extract(includeLocalization: false);

        // Assert
        Assert.IsNull(data.Localization);
        Assert.AreEqual("profile", data.DefaultProfile.Root!.Name.LocalName);
    }

    [Test]
    public async Task Extract_ReturnsNullLocalizationWhenTheArchiveHasNone()
    {
        // Arrange
        this.WriteArchive(localization: null);
        var source = this.CreateSource();

        // Act
        var data = await source.Extract(includeLocalization: true);

        // Assert
        Assert.IsNull(data.Localization);
        Assert.AreEqual("profile", data.DefaultProfile.Root!.Name.LocalName);
    }

    [Test]
    public void Extract_ThrowsWhenTheArchiveHasNoProfile()
    {
        // Arrange
        var data = Encoding.UTF8.GetBytes("ui_seat=Seat Controls\n");
        var archive = ArchiveBuilder.Build(true, new ArchiveBuilder.Entry(@"Data\Localization\english\global.ini", 100, ArchiveBuilder.ZstdBytes(data), data));
        File.WriteAllBytes(Path.Combine(this._environmentDir, SCP4kGameDataSource.P4kFileName), archive);
        var source = this.CreateSource();

        // Act & Assert
        var ex = Assert.ThrowsAsync<FileNotFoundException>(() => source.Extract(includeLocalization: true));
        Assert.That(ex!.Message, Does.Contain("defaultProfile.xml"));
    }

    private SCP4kGameDataSource CreateSource() =>
        new(new SCFoldersForTest { GameEnvironmentDir = this._environmentDir });

    private void WriteArchive(byte[]? localization)
    {
        var binary = CryXmlBuilder.Build(new CryXmlBuilder.Node("profile", [], "", []));
        var entries = new List<ArchiveBuilder.Entry>
        {
            new(@"Data\Libs\Config\defaultProfile.xml", 100, ArchiveBuilder.ZstdBytes(binary), binary),
        };
        if (localization != null) entries.Add(new(@"Data\Localization\english\global.ini", 100, ArchiveBuilder.ZstdBytes(localization), localization));

        File.WriteAllBytes(Path.Combine(this._environmentDir, SCP4kGameDataSource.P4kFileName), ArchiveBuilder.Build(true, entries.ToArray()));
    }
}
