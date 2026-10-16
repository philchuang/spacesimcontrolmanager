using NUnit.Framework;
using SSCM.Elite;

namespace SSCM.Elite.Tests;

public class CustomBindsFile_Tests
{
    private string _root = string.Empty;

    [SetUp]
    public void SetUp()
    {
        this._root = Path.Combine(Path.GetTempPath(), "sscm-tests", TestContext.CurrentContext.Test.ID);
        if (Directory.Exists(this._root)) Directory.Delete(this._root, recursive: true);
        Directory.CreateDirectory(this._root);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(this._root)) Directory.Delete(this._root, recursive: true);
    }

    [Test]
    public void FindExistingPath_ChoosesTheHighestSemanticVersion()
    {
        // Arrange
        this.Write("Custom.3.0.binds", "Custom.4.0.binds", "Custom.4.1.binds", "Custom.4.2.binds");

        // Act
        var path = new CustomBindsFile(this._root).FindExistingPath();

        // Assert
        Assert.AreEqual(Path.Combine(this._root, "Custom.4.2.binds"), path);
    }

    [Test]
    public void FindExistingPath_ComparesMinorVersionsNumerically()
    {
        // Arrange
        this.Write("Custom.4.2.binds", "Custom.4.10.binds");

        // Act
        var path = new CustomBindsFile(this._root).FindExistingPath();

        // Assert
        Assert.AreEqual(Path.Combine(this._root, "Custom.4.10.binds"), path);
    }

    [Test]
    public void FindExistingPath_UsesTheOnlyMatchingFile()
    {
        // Arrange
        this.Write("Custom.3.0.binds");

        // Act & Assert
        Assert.AreEqual(Path.Combine(this._root, "Custom.3.0.binds"), new CustomBindsFile(this._root).FindExistingPath());
    }

    [Test]
    public void FindExistingPath_IgnoresFilesThatAreNotCustomMajorMinorBinds()
    {
        // Arrange
        this.Write(
            "StartPreset.start",
            "Custom.binds",
            "Custom.4.binds",
            "Custom.4.0.0.binds",
            "Custom.4.0.binds.bak",
            "Other.4.0.binds",
            "Custom.4.2.binds");

        // Act
        var path = new CustomBindsFile(this._root).FindExistingPath();

        // Assert
        Assert.AreEqual(Path.Combine(this._root, "Custom.4.2.binds"), path);
    }

    [Test]
    public void FindExistingPath_MatchesCaseInsensitively()
    {
        // Arrange
        this.Write("custom.4.2.binds");

        // Act
        var path = new CustomBindsFile(this._root).FindExistingPath();

        // Assert
        Assert.AreEqual(Path.Combine(this._root, "custom.4.2.binds"), path);
    }

    [Test]
    public void FindExistingPath_IsNullWhenTheDirectoryHasNoMatchingFile()
    {
        // Arrange
        this.Write("StartPreset.binds");

        // Act & Assert
        Assert.IsNull(new CustomBindsFile(this._root).FindExistingPath());
    }

    [Test]
    public void FindExistingPath_IsNullWhenTheDirectoryDoesNotExist()
    {
        // Act & Assert
        Assert.IsNull(new CustomBindsFile(Path.Combine(this._root, "missing")).FindExistingPath());
    }

    [Test]
    public void Require_ReturnsTheHighestVersion()
    {
        // Arrange
        this.Write("Custom.4.0.binds", "Custom.4.2.binds");

        // Act & Assert
        Assert.AreEqual(Path.Combine(this._root, "Custom.4.2.binds"), new CustomBindsFile(this._root).Require());
    }

    [Test]
    public void Require_ThrowsNamingTheDirectoryAndTheHint()
    {
        // Arrange
        var file = new CustomBindsFile(this._root);

        // Act & Assert
        var ex = Assert.Throws<FileNotFoundException>(() => file.Require("Start the game once so it creates one."));
        Assert.That(ex!.Message, Does.Contain("Custom.{major}.{minor}.binds").And.Contain(this._root).And.EndWith("Start the game once so it creates one."));
        Assert.AreEqual(this._root, ex.FileName);
    }

    [Test]
    public void Searched_ListsTheDirectory()
    {
        // Act & Assert
        Assert.AreEqual($"[{this._root}]", new CustomBindsFile(this._root).Searched);
    }

    private void Write(params string[] fileNames)
    {
        foreach (var name in fileNames) File.WriteAllText(Path.Combine(this._root, name), "x");
    }
}
