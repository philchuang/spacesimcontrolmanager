using NUnit.Framework;
using SSCM.Core;
using SSCM.Tests.Mocks;

namespace SSCM.Tests;

[TestFixture]
public class ResourceFile_Tests
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
    public void Create_UsesTheWorkingDirectoryThenTheExecutableDirectory()
    {
        // Arrange
        var work = this.MakeDir("work");
        var bin = this.MakeDir("bin");

        // Act
        var file = ResourceFile.Create(new PlatformForTest(workingDir: work), "thing.txt", bin);

        // Assert
        Assert.AreEqual("thing.txt", file.FileName);
        Assert.AreEqual(Path.Combine(work, "thing.txt"), file.Path);
        Assert.AreEqual(Path.Combine(bin, "thing.txt"), file.FallbackPath);
    }

    [Test]
    public void Create_DefaultsTheFallbackToTheExecutableDirectory()
    {
        // Act
        var file = ResourceFile.Create(new PlatformForTest(workingDir: this.MakeDir("work")), "thing.txt");

        // Assert
        Assert.AreEqual(Path.Combine(AppContext.BaseDirectory, "thing.txt"), file.FallbackPath);
    }

    [Test]
    public void FindExistingPath_PrefersThePrimaryDirectory()
    {
        // Arrange
        var work = this.MakeDir("work", "thing.txt");
        var bin = this.MakeDir("bin", "thing.txt");
        var file = new ResourceFile("thing.txt", work, bin);

        // Act & Assert
        Assert.AreEqual(Path.Combine(work, "thing.txt"), file.FindExistingPath());
    }

    [Test]
    public void FindExistingPath_FallsBackWhenThePrimaryDirectoryHasNoFile()
    {
        // Arrange
        var bin = this.MakeDir("bin", "thing.txt");
        var file = new ResourceFile("thing.txt", this.MakeDir("work"), bin);

        // Act & Assert
        Assert.AreEqual(Path.Combine(bin, "thing.txt"), file.FindExistingPath());
    }

    [Test]
    public void FindExistingPath_IsNullWhenNeitherDirectoryHasTheFile()
    {
        // Arrange
        var file = new ResourceFile("thing.txt", this.MakeDir("work"), this.MakeDir("bin"));

        // Act & Assert
        Assert.IsNull(file.FindExistingPath());
    }

    [Test]
    public void FindExistingPath_WorksWithoutAFallback()
    {
        // Arrange
        var work = this.MakeDir("work", "thing.txt");

        // Act & Assert
        Assert.AreEqual(Path.Combine(work, "thing.txt"), new ResourceFile("thing.txt", work).FindExistingPath());
        Assert.IsNull(new ResourceFile("thing.txt", this.MakeDir("empty")).FindExistingPath());
    }

    [Test]
    public void FallbackPath_IsNullWhenThereIsNoFallbackOrItIsTheSameFile()
    {
        // Act & Assert
        Assert.IsNull(new ResourceFile("thing.txt", this._root).FallbackPath);
        Assert.IsNull(new ResourceFile("thing.txt", this._root, this._root).FallbackPath);
        Assert.IsNull(new ResourceFile("thing.txt", this._root, this._root.ToUpperInvariant() + Path.DirectorySeparatorChar).FallbackPath);
    }

    [Test]
    public void Searched_ListsEveryLocation()
    {
        // Arrange
        var work = this.MakeDir("work");
        var bin = this.MakeDir("bin");

        // Act & Assert
        Assert.AreEqual($"[{Path.Combine(work, "thing.txt")}] or [{Path.Combine(bin, "thing.txt")}]", new ResourceFile("thing.txt", work, bin).Searched);
        Assert.AreEqual($"[{Path.Combine(work, "thing.txt")}]", new ResourceFile("thing.txt", work).Searched);
    }

    [Test]
    public void Require_ReturnsTheExistingPath()
    {
        // Arrange
        var bin = this.MakeDir("bin", "thing.txt");
        var file = new ResourceFile("thing.txt", this.MakeDir("work"), bin);

        // Act & Assert
        Assert.AreEqual(Path.Combine(bin, "thing.txt"), file.Require());
    }

    [Test]
    public void Require_ThrowsNamingEveryLocationAndTheHint()
    {
        // Arrange
        var file = new ResourceFile("thing.txt", this.MakeDir("work"), this.MakeDir("bin"));

        // Act & Assert
        var ex = Assert.Throws<FileNotFoundException>(() => file.Require("Run the thing to create it."));
        Assert.That(ex!.Message, Does.Contain("thing.txt").And.Contain(file.Path).And.Contain(file.FallbackPath!).And.EndWith("Run the thing to create it."));
        Assert.AreEqual(file.Path, ex.FileName);
    }

    [Test]
    public void Require_ThrowsWithoutAHintWhenNoneIsGiven()
    {
        // Arrange
        var file = new ResourceFile("thing.txt", this.MakeDir("work"));

        // Act & Assert
        var ex = Assert.Throws<FileNotFoundException>(() => file.Require());
        Assert.That(ex!.Message, Does.EndWith("]."));
    }

    /// <summary>Creates a subdirectory, optionally with a file in it.</summary>
    private string MakeDir(string name, string? fileName = null)
    {
        var dir = Path.Combine(this._root, name);
        Directory.CreateDirectory(dir);
        if (fileName != null) File.WriteAllText(Path.Combine(dir, fileName), "x");
        return dir;
    }
}
