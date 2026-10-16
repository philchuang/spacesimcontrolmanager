using System.Text;
using NUnit.Framework;
using SSCM.StarCitizen.Bindings;
using SSCM.StarCitizen.Tests.Mocks;

namespace SSCM.StarCitizen.Tests.Bindings;

public class SCLocalizationFileSource_Tests
{
    private string _root = string.Empty;
    private string _gameDir = string.Empty;
    private string _workDir = string.Empty;

    [SetUp]
    public void SetUp()
    {
        this._root = Path.Combine(Path.GetTempPath(), "sscm-tests", TestContext.CurrentContext.Test.ID);
        if (Directory.Exists(this._root)) Directory.Delete(this._root, recursive: true);

        this._gameDir = Path.Combine(this._root, "LIVE", "data", "Localization", "english");
        this._workDir = Path.Combine(this._root, "work");
        Directory.CreateDirectory(this._workDir);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(this._root)) Directory.Delete(this._root, recursive: true);
    }

    [Test]
    public void WorkingPath_IsGlobalIniInTheWorkingDirectory()
    {
        // Act
        var source = this.CreateSource();

        // Assert
        Assert.AreEqual(Path.Combine(this._workDir, "global.ini"), source.WorkingPath);
    }

    [Test]
    public void GamePath_ComesFromTheFolders()
    {
        // Arrange
        var expected = Path.Combine(this._root, "elsewhere", "global.ini");
        var source = this.CreateSource(gamePath: expected);

        // Assert
        Assert.AreEqual(expected, source.GamePath);
    }

    [Test]
    public void FindExistingPath_PrefersTheGameCopyOverTheWorkingDirectoryOne()
    {
        // Arrange
        Directory.CreateDirectory(this._gameDir);
        File.WriteAllText(this.GamePath, "game=1\n");
        File.WriteAllText(this.WorkingPath, "work=1\n");
        var source = this.CreateSource();

        // Act & Assert
        Assert.AreEqual(this.GamePath, source.FindExistingPath());
    }

    [Test]
    public void FindExistingPath_FallsBackToTheWorkingDirectoryWhenTheGameHasNone()
    {
        // Arrange
        File.WriteAllText(this.WorkingPath, "work=1\n");
        var source = this.CreateSource();

        // Act & Assert
        Assert.AreEqual(this.WorkingPath, source.FindExistingPath());
    }

    [Test]
    public void FindExistingPath_ReturnsNullWhenNeitherLocationHasAFile()
    {
        // Act & Assert
        Assert.IsNull(this.CreateSource().FindExistingPath());
    }

    [Test]
    public void NeedsExtractionFromArchive_IsTrueOnlyWhenTheGameHasNoCopy()
    {
        // Arrange
        var source = this.CreateSource();

        // Act & Assert
        Assert.IsTrue(source.NeedsExtractionFromArchive);
        Directory.CreateDirectory(this._gameDir);
        File.WriteAllText(this.GamePath, "x=1\n");
        Assert.IsFalse(source.NeedsExtractionFromArchive);
    }

    [Test]
    public void ParseLocalization_SplitsOnFirstEqualsAndSkipsCommentsAndBlankLines()
    {
        // Arrange
        var lines = new[]
        {
            "; a comment",
            "   ; an indented comment",
            "",
            "no separator",
            "ui_a=Alpha",
            "ui_b=Bravo=Charlie",
            "ui_a=Alpha2",
        };

        // Act
        var map = SCLocalizationFileSource.ParseLocalization(lines);

        // Assert
        Assert.AreEqual(2, map.Count);
        Assert.AreEqual("Alpha2", map["ui_a"], "later duplicates win");
        Assert.AreEqual("Bravo=Charlie", map["ui_b"]);
    }

    [Test]
    public async Task Load_ParsesTheFileThatWouldBeRead()
    {
        // Arrange
        Directory.CreateDirectory(this._gameDir);
        File.WriteAllText(this.GamePath, "ui_a=Alpha\n;comment\n\nui_b=Bravo\n");
        var source = this.CreateSource();

        // Act
        var map = await source.Load();

        // Assert
        Assert.AreEqual("Alpha", map!["ui_a"]);
        Assert.AreEqual("Bravo", map["ui_b"]);
    }

    [Test]
    public async Task Load_ReturnsNullWhenThereIsNoFile()
    {
        // Act & Assert
        Assert.IsNull(await this.CreateSource().Load());
    }

    [Test]
    public void Save_WritesToTheWorkingDirectoryWithoutTouchingTheGameFolder()
    {
        // Arrange
        var content = Encoding.UTF8.GetBytes("ui_seat=Saved\n");
        var source = this.CreateSource();

        // Act
        source.Save(content);

        // Assert
        Assert.AreEqual(content, File.ReadAllBytes(source.WorkingPath));
        Assert.IsFalse(File.Exists(this.GamePath));
        Assert.IsFalse(File.Exists(source.WorkingPath + ".tmp"));
    }

    [Test]
    public void Save_OverwritesAnExistingWorkingDirectoryCopy()
    {
        // Arrange
        File.WriteAllText(this.WorkingPath, "stale=1\n");
        var source = this.CreateSource();

        // Act
        source.Save(Encoding.UTF8.GetBytes("fresh=1\n"));

        // Assert
        Assert.That(File.ReadAllText(this.WorkingPath), Does.Contain("fresh=1").And.Not.Contain("stale"));
    }

    private string GamePath => Path.Combine(this._gameDir, "global.ini");

    private string WorkingPath => Path.Combine(this._workDir, "global.ini");

    private SCLocalizationFileSource CreateSource(string? gamePath = null) =>
        new(new SCFoldersForTest { GameLocalizationPath = gamePath ?? this.GamePath }, this._workDir);
}
