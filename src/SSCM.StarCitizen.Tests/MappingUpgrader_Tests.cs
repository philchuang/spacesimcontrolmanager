using NUnit.Framework;
using SSCM.StarCitizen.Bindings;
using SSCM.StarCitizen.Tests.Mocks;
using SSCM.Tests.Mocks;

namespace SSCM.StarCitizen.Tests;

public class MappingUpgrader_Tests
{
    private string _root = string.Empty;
    private string _workingDir = string.Empty;
    private string _executableDir = string.Empty;

    [SetUp]
    public void SetUp()
    {
        this._root = Path.Combine(Path.GetTempPath(), "sscm-tests", TestContext.CurrentContext.Test.ID);
        if (Directory.Exists(this._root)) Directory.Delete(this._root, recursive: true);

        this._workingDir = Path.Combine(this._root, "work");
        this._executableDir = Path.Combine(this._root, "bin");
        Directory.CreateDirectory(this._workingDir);
        Directory.CreateDirectory(this._executableDir);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(this._root)) Directory.Delete(this._root, recursive: true);
    }

    [Test]
    public async Task Upgrade_ReadsUpgradeMappingsFromTheWorkingDirectoryFirst()
    {
        // Arrange
        WriteUpgradeMappings(this._workingDir, "null");
        WriteUpgradeMappings(this._executableDir, "\"seat-boost\"");
        var upgrader = this.CreateUpgrader();

        // Act
        var updated = await upgrader.Upgrade(Data("seat", "fire"));

        // Assert
        Assert.IsEmpty(updated.Mappings, "the working directory file removes the mapping, the executable directory file would rename it");
    }

    [Test]
    public async Task Upgrade_FallsBackToTheExecutableDirectory()
    {
        // Arrange
        WriteUpgradeMappings(this._executableDir, "\"seat-boost\"");
        var upgrader = this.CreateUpgrader();

        // Act
        var updated = await upgrader.Upgrade(Data("seat", "fire"));

        // Assert
        Assert.AreEqual(1, updated.Mappings.Count);
        Assert.AreEqual("seat", updated.Mappings[0].ActionMap);
        Assert.AreEqual("boost", updated.Mappings[0].Action);
    }

    [Test]
    public async Task Upgrade_ChangesNothingWhenNeitherDirectoryHasUpgradeMappings()
    {
        // Arrange
        var upgrader = this.CreateUpgrader();

        // Act
        var updated = await upgrader.Upgrade(Data("seat", "fire"));

        // Assert
        Assert.AreEqual(1, updated.Mappings.Count);
        Assert.AreEqual("fire", updated.Mappings[0].Action);
        Assert.IsFalse(upgrader.Result.HasDifferences);
    }

    private MappingUpgrader CreateUpgrader()
    {
        var platform = new PlatformForTest(workingDir: this._workingDir);
        var profile = new SCDefaultProfileFileSource(this._workingDir, this._executableDir);
        return new MappingUpgrader(platform, new SCFoldersForTest(), profile, this._executableDir);
    }

    private static SCMappingData Data(string actionMap, string action) => new()
    {
        Mappings = new List<SCMapping> { new() { ActionMap = actionMap, Action = action } },
    };

    /// <summary>Writes a one-entry upgrade file that renames (or, for <c>null</c>, removes) seat-fire.</summary>
    private static void WriteUpgradeMappings(string dir, string target) =>
        File.WriteAllText(Path.Combine(dir, "SCUpgradeMappings.jsonc"), $$"""
            [
                // comments are allowed
                { "type": "mapping", "source": "seat-fire", "target": {{target}}, "comment": "test" }
            ]
            """);
}
