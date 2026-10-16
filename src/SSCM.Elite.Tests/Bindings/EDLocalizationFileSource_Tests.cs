using NUnit.Framework;
using SSCM.Elite.Bindings;
using SSCM.Tests.Mocks;

namespace SSCM.Elite.Tests.Bindings;

public class EDLocalizationFileSource_Tests
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
    public void ParseLocalization_SplitsOnTheFirstEqualsAndSkipsComments()
    {
        // Act
        var map = EDLocalizationFileSource.ParseLocalization(
        [
            "YawLeftButton=Yaw Left",
            "; comment",
            "",
            "Equals=one=two",
            "NoSeparator",
        ]);

        // Assert
        Assert.AreEqual("Yaw Left", map["YawLeftButton"]);
        Assert.AreEqual("one=two", map["Equals"]);
        Assert.IsFalse(map.ContainsKey("NoSeparator"));
    }

    [Test]
    public async Task Load_ReturnsNullWhenNeitherLocationHasAFile()
    {
        // Arrange
        var source = EDLocalizationFileSource.Create(new PlatformForTest(workingDir: this._root), this._root);

        // Act
        var map = await source.Load();

        // Assert
        Assert.IsNull(map);
        Assert.IsNull(source.FindExistingPath());
    }

    [Test]
    public async Task Load_PrefersTheWorkingDirectoryOverTheExecutableDirectory()
    {
        // Arrange
        var work = Path.Combine(this._root, "work");
        var bin = Path.Combine(this._root, "bin");
        Directory.CreateDirectory(work);
        Directory.CreateDirectory(bin);
        File.WriteAllText(Path.Combine(work, EDLocalizationFileSource.FileName), "YawLeftButton=Working\n");
        File.WriteAllText(Path.Combine(bin, EDLocalizationFileSource.FileName), "YawLeftButton=Shipped\n");
        var source = EDLocalizationFileSource.Create(new PlatformForTest(workingDir: work), bin);

        // Act
        var map = await source.Load();

        // Assert
        Assert.AreEqual("Working", map!["YawLeftButton"]);
    }
}
