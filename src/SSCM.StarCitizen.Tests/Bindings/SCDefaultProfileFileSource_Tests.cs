using System.Xml.Linq;
using NUnit.Framework;
using SSCM.StarCitizen.Bindings;
using SSCM.Tests.Mocks;

namespace SSCM.StarCitizen.Tests.Bindings;

public class SCDefaultProfileFileSource_Tests
{
    private string _dir = string.Empty;

    [SetUp]
    public void SetUp()
    {
        this._dir = Path.Combine(Path.GetTempPath(), "sscm-tests", TestContext.CurrentContext.Test.ID);
        if (Directory.Exists(this._dir)) Directory.Delete(this._dir, recursive: true);
        Directory.CreateDirectory(this._dir);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(this._dir)) Directory.Delete(this._dir, recursive: true);
    }

    private static string Profile(string actionMap, string action) =>
        $"<profile><actionmap name=\"{actionMap}\"><action name=\"{action}\" /></actionmap></profile>";

    /// <summary>Creates a subdirectory, optionally with a defaultProfile.xml in it.</summary>
    private string MakeDir(string name, string? profileXml = null)
    {
        var dir = System.IO.Path.Combine(this._dir, name);
        Directory.CreateDirectory(dir);
        if (profileXml != null) File.WriteAllText(System.IO.Path.Combine(dir, "defaultProfile.xml"), profileXml);
        return dir;
    }

    [Test]
    public void Path_IsDefaultProfileXmlInTheGivenDirectory()
    {
        // Act
        var source = new SCDefaultProfileFileSource(this._dir);

        // Assert
        Assert.AreEqual(System.IO.Path.Combine(this._dir, "defaultProfile.xml"), source.Path);
    }

    [Test]
    public void Save_WritesTheFormatTheScriptProduces()
    {
        // Arrange
        var source = new SCDefaultProfileFileSource(this._dir);
        var document = new XDocument(new XElement("profile", new XAttribute("version", "1"),
            new XElement("actionmap", new XAttribute("name", "seat"),
                new XElement("action", new XAttribute("name", "fire"), new XAttribute("joystick", " ")))));

        // Act
        source.Save(document);

        // Assert
        var bytes = File.ReadAllBytes(source.Path);
        Assert.AreNotEqual(new byte[] { 0xEF, 0xBB, 0xBF }, bytes.Take(3).ToArray(), "no byte order mark");
        var expected = string.Join("\n",
            "<profile version=\"1\">",
            "  <actionmap name=\"seat\">",
            "    <action name=\"fire\" joystick=\" \" />",
            "  </actionmap>",
            "</profile>");
        Assert.AreEqual(expected, System.Text.Encoding.UTF8.GetString(bytes), "two-space indent, LF only, no XML declaration, no trailing newline");
    }

    [Test]
    public async Task Save_KeepsTheExtractionCommentOnTheFirstLine()
    {
        // Arrange
        var source = new SCDefaultProfileFileSource(this._dir);
        var document = new XDocument(
            new XComment(" extracted from LIVE Data.p4k dated 2026-03-09 "),
            new XElement("profile", new XElement("actionmap", new XAttribute("name", "seat"))));

        // Act
        source.Save(document);

        // Assert
        var lines = File.ReadAllText(source.Path).Split('\n');
        Assert.AreEqual("<!-- extracted from LIVE Data.p4k dated 2026-03-09 -->", lines[0]);
        Assert.AreEqual("<profile>", lines[1]);

        var loaded = await source.Load();
        Assert.AreEqual("seat", (string?) loaded.Root!.Element("actionmap")!.Attribute("name"));
    }

    [Test]
    public async Task Save_ThenLoad_RoundTripsAndOverwritesTheExistingFile()
    {
        // Arrange
        var source = new SCDefaultProfileFileSource(this._dir);
        File.WriteAllText(source.Path, "<old />");

        // Act
        source.Save(XDocument.Parse("<profile><actionmap name=\"seat\" /></profile>"));
        var loaded = await source.Load();

        // Assert
        Assert.AreEqual("profile", loaded.Root!.Name.LocalName);
        Assert.AreEqual("seat", (string?) loaded.Root.Element("actionmap")!.Attribute("name"));
        Assert.AreEqual(new[] { "defaultProfile.xml" }, Directory.GetFiles(this._dir).Select(Path.GetFileName), "no temp file is left behind");
    }

    [Test]
    public async Task Load_PrefersThePrimaryDirectoryOverTheFallback()
    {
        // Arrange
        var primary = this.MakeDir("primary", "<profile><actionmap name=\"primary\" /></profile>");
        var fallback = this.MakeDir("fallback", "<profile><actionmap name=\"fallback\" /></profile>");
        var source = new SCDefaultProfileFileSource(primary, fallback);

        // Act
        var loaded = await source.Load();

        // Assert
        Assert.AreEqual("primary", (string?) loaded.Root!.Element("actionmap")!.Attribute("name"));
        Assert.AreEqual(source.Path, source.FindExistingPath());
    }

    [Test]
    public async Task Load_FallsBackWhenThePrimaryDirectoryHasNoFile()
    {
        // Arrange
        var primary = this.MakeDir("primary");
        var fallback = this.MakeDir("fallback", "<profile><actionmap name=\"fallback\" /></profile>");
        var source = new SCDefaultProfileFileSource(primary, fallback);

        // Act
        var loaded = await source.Load();

        // Assert
        Assert.AreEqual("fallback", (string?) loaded.Root!.Element("actionmap")!.Attribute("name"));
        Assert.AreEqual(System.IO.Path.Combine(fallback, "defaultProfile.xml"), source.FindExistingPath());
    }

    [Test]
    public void FindExistingPath_IsNullWhenNeitherDirectoryHasAFile()
    {
        // Arrange
        var source = new SCDefaultProfileFileSource(this.MakeDir("primary"), this.MakeDir("fallback"));

        // Act & Assert
        Assert.IsNull(source.FindExistingPath());
    }

    [Test]
    public void FallbackPath_IsNullWhenThereIsNoFallbackOrItIsTheSameFile()
    {
        // Act & Assert
        Assert.IsNull(new SCDefaultProfileFileSource(this._dir).FallbackPath);
        Assert.IsNull(new SCDefaultProfileFileSource(this._dir, this._dir).FallbackPath);
        Assert.IsNull(new SCDefaultProfileFileSource(this._dir, this._dir.ToUpperInvariant() + System.IO.Path.DirectorySeparatorChar).FallbackPath);
    }

    [Test]
    public void Save_WritesToThePrimaryDirectoryAndLeavesTheFallbackAlone()
    {
        // Arrange
        var primary = System.IO.Path.Combine(this._dir, "does-not-exist-yet");
        var fallback = this.MakeDir("fallback", "<profile><actionmap name=\"fallback\" /></profile>");
        var source = new SCDefaultProfileFileSource(primary, fallback);

        // Act
        source.Save(XDocument.Parse("<profile><actionmap name=\"saved\" /></profile>"));

        // Assert
        Assert.IsTrue(File.Exists(System.IO.Path.Combine(primary, "defaultProfile.xml")), "the directory is created when needed");
        Assert.That(File.ReadAllText(System.IO.Path.Combine(fallback, "defaultProfile.xml")), Does.Contain("fallback"));
    }

    [Test]
    public void Create_UsesTheWorkingDirectoryThenTheExecutableDirectory()
    {
        // Arrange
        var work = this.MakeDir("work");
        var bin = this.MakeDir("bin");

        // Act
        var source = SCDefaultProfileFileSource.Create(new PlatformForTest(workingDir: work), bin);

        // Assert
        Assert.AreEqual(System.IO.Path.Combine(work, "defaultProfile.xml"), source.Path);
        Assert.AreEqual(System.IO.Path.Combine(bin, "defaultProfile.xml"), source.FallbackPath);
        Assert.AreEqual($"[{source.Path}] or [{source.FallbackPath}]", source.Searched);
    }

    [Test]
    public void Create_DefaultsTheFallbackToTheExecutableDirectory()
    {
        // Act
        var source = SCDefaultProfileFileSource.Create(new PlatformForTest(workingDir: this.MakeDir("work")));

        // Assert
        Assert.AreEqual(System.IO.Path.Combine(AppContext.BaseDirectory, "defaultProfile.xml"), source.FallbackPath);
    }

    [Test]
    public async Task LoadMappings_ReadsFromTheWorkingDirectoryFirst()
    {
        // Arrange
        var work = this.MakeDir("work", Profile("refreshed", "boost"));
        var bin = this.MakeDir("bin", Profile("shipped", "fire"));
        var source = new SCDefaultProfileFileSource(work, bin);

        // Act
        var mappings = await source.LoadMappings();

        // Assert
        Assert.AreEqual(1, mappings.Count);
        Assert.AreEqual("refreshed", mappings[0].ActionMap);
        Assert.AreEqual("boost", mappings[0].Action);
    }

    [Test]
    public async Task LoadMappings_FallsBackToTheExecutableDirectory()
    {
        // Arrange
        var source = new SCDefaultProfileFileSource(this.MakeDir("work"), this.MakeDir("bin", Profile("shipped", "fire")));

        // Act
        var mappings = await source.LoadMappings();

        // Assert
        Assert.AreEqual(1, mappings.Count);
        Assert.AreEqual("shipped", mappings[0].ActionMap);
        Assert.AreEqual("fire", mappings[0].Action);
    }

    [Test]
    public async Task LoadMappings_ReturnsNothingWhenNeitherDirectoryHasAProfile()
    {
        // Arrange
        var source = new SCDefaultProfileFileSource(this.MakeDir("work"), this.MakeDir("bin"));

        // Act
        var mappings = await source.LoadMappings();

        // Assert
        Assert.IsEmpty(mappings);
    }

    [Test]
    public async Task LoadMappings_SkipsActionMapsAndActionsWithoutNames()
    {
        // Arrange
        var source = new SCDefaultProfileFileSource(this.MakeDir("work", """
            <profile>
              <actionmap><action name="orphan" /></actionmap>
              <actionmap name="seat">
                <action name="fire" />
                <action />
              </actionmap>
            </profile>
            """));

        // Act
        var mappings = await source.LoadMappings();

        // Assert
        Assert.AreEqual(1, mappings.Count);
        Assert.AreEqual("seat", mappings[0].ActionMap);
        Assert.AreEqual("fire", mappings[0].Action);
    }

    [Test]
    public async Task LoadMappings_ListsEveryActionInProfileOrder()
    {
        // Arrange
        var source = new SCDefaultProfileFileSource(this.MakeDir("work", """
            <profile>
              <actionmap name="seat"><action name="fire" /><action name="boost" /></actionmap>
              <actionmap name="flight"><action name="fire" /></actionmap>
            </profile>
            """));

        // Act
        var mappings = await source.LoadMappings();

        // Assert
        Assert.AreEqual(new[] { "seat-fire", "seat-boost", "flight-fire" }, mappings.Select(m => $"{m.ActionMap}-{m.Action}"));
    }

    [Test]
    public void Load_ThrowsNamingBothLocationsWhenNeitherHasAFile()
    {
        // Arrange
        var source = new SCDefaultProfileFileSource(this.MakeDir("primary"), this.MakeDir("fallback"));

        // Act & Assert
        var ex = Assert.ThrowsAsync<FileNotFoundException>(() => source.Load());
        Assert.That(ex!.Message, Does.Contain(source.Path).And.Contain(source.FallbackPath!).And.Contain("--refresh-defaults"));
    }

    [Test]
    public void Load_ThrowsWithRefreshHintWhenTheFileIsMissing()
    {
        // Arrange
        var source = new SCDefaultProfileFileSource(this._dir);

        // Act & Assert
        var ex = Assert.ThrowsAsync<FileNotFoundException>(() => source.Load());
        Assert.That(ex!.Message, Does.Contain(source.Path).And.Contain("--refresh-defaults"));
    }
}
