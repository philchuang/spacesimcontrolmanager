using NUnit.Framework;
using SSCM.Tests.Mocks;

namespace SSCM.Elite.Tests;

[TestFixture]
public class MappingImporter_Config_Tests
{
    private string _root = string.Empty;
    private string _workingDir = string.Empty;
    private string _executableDir = string.Empty;
    private string _gameConfigPath = string.Empty;

    [SetUp]
    public void SetUp()
    {
        this._root = Path.Combine(Path.GetTempPath(), "sscm-tests", TestContext.CurrentContext.Test.ID);
        if (Directory.Exists(this._root)) Directory.Delete(this._root, recursive: true);

        this._workingDir = Path.Combine(this._root, "work");
        this._executableDir = Path.Combine(this._root, "bin");
        Directory.CreateDirectory(this._workingDir);
        Directory.CreateDirectory(this._executableDir);

        this._gameConfigPath = Path.Combine(this._root, "custom.binds");
        File.WriteAllText(this._gameConfigPath, "<Root><Alpha Value=\"1\" /><Beta Value=\"2\" /></Root>");
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(this._root)) Directory.Delete(this._root, recursive: true);
    }

    [Test]
    public async Task Read_UsesTheConfigInTheWorkingDirectoryFirst()
    {
        // Arrange
        WriteConfig(this._workingDir, "^Alpha$");
        WriteConfig(this._executableDir, "^Beta$");

        // Act
        var skipped = await this.ReadAndCollectSkipped();

        // Assert
        Assert.AreEqual(new[] { "Alpha" }, skipped);
    }

    [Test]
    public async Task Read_FallsBackToTheConfigInTheExecutableDirectory()
    {
        // Arrange
        WriteConfig(this._executableDir, "^Beta$");

        // Act
        var skipped = await this.ReadAndCollectSkipped();

        // Assert
        Assert.AreEqual(new[] { "Beta" }, skipped);
    }

    [Test]
    public void Read_ThrowsNamingBothLocationsWhenNeitherHasAConfig()
    {
        // Arrange
        var importer = this.CreateImporter();

        // Act & Assert
        var ex = Assert.ThrowsAsync<FileNotFoundException>(() => importer.Read());
        Assert.That(ex!.Message, Does.Contain("EDMappingConfig.yml")
            .And.Contain(Path.Combine(this._workingDir, "EDMappingConfig.yml"))
            .And.Contain(Path.Combine(this._executableDir, "EDMappingConfig.yml")));
    }

    private async Task<List<string>> ReadAndCollectSkipped()
    {
        var importer = this.CreateImporter();
        var skipped = new List<string>();
        importer.DebugOutput += s =>
        {
            if (s.StartsWith("Skipped [", StringComparison.Ordinal)) skipped.Add(s["Skipped [".Length..s.IndexOf(']')]);
        };

        await importer.Read();
        return skipped;
    }

    private MappingImporter CreateImporter() =>
        new(new PlatformForTest(workingDir: this._workingDir), this._gameConfigPath, this._executableDir);

    private static void WriteConfig(string dir, string ignoreRegex) =>
        File.WriteAllText(Path.Combine(dir, "EDMappingConfig.yml"), $$"""
            GroupMappings: {}
            IgnoreList:
            - {{ignoreRegex}}
            """);
}
