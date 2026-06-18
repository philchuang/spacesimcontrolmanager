using NUnit.Framework;
using SSCM.Core;
using SSCM.Tests.Mocks;

namespace SSCM.Tests;

[TestFixture]
public class MappingDataRepositoryDefault_Tests : TestBase
{
    private string SavePath => Path.Combine(base.TestTempDir, "data", "mappings.json");
    private string CustomSavePath => Path.Combine(base.TestTempDir, "custom", "mappings.json");

    [SetUp]
    public void Init()
    {
        if (Directory.Exists(base.TestTempDir))
        {
            Directory.Delete(base.TestTempDir, true);
        }
    }

    [Test]
    public async Task Load_Reads_Default_Save_Path()
    {
        // Arrange
        var expected = new RepositoryTestData { Name = "default" };
        var repo = this.CreateRepository();
        await repo.Save(expected);

        // Act
        var actual = await repo.Load();

        // Assert
        Assert.NotNull(actual);
        Assert.AreEqual(expected.Name, actual!.Name);
    }

    [Test]
    public async Task Load_Reads_Custom_Save_Path()
    {
        // Arrange
        var expected = new RepositoryTestData { Name = "custom" };
        var repo = this.CreateRepository();
        await repo.Save(expected, this.CustomSavePath);

        // Act
        var actual = await repo.Load(this.CustomSavePath);

        // Assert
        Assert.NotNull(actual);
        Assert.AreEqual(expected.Name, actual!.Name);
    }

    [Test]
    public async Task Load_Returns_Null_And_Warns_When_File_Missing()
    {
        // Arrange
        var warnings = new List<string>();
        var debugs = new List<string>();
        var repo = this.CreateRepository();
        repo.WarningOutput += warnings.Add;
        repo.DebugOutput += debugs.Add;

        // Act
        var actual = await repo.Load();

        // Assert
        Assert.Null(actual);
        Assert.That(warnings.Single(), Does.Contain(this.SavePath));
        Assert.IsEmpty(debugs);
    }

    [Test]
    public async Task Load_Returns_Null_And_Warns_When_Json_Malformed()
    {
        // Arrange
        Directory.CreateDirectory(Path.GetDirectoryName(this.SavePath)!);
        await File.WriteAllTextAsync(this.SavePath, "{ nope");
        var warnings = new List<string>();
        var debugs = new List<string>();
        var repo = this.CreateRepository();
        repo.WarningOutput += warnings.Add;
        repo.DebugOutput += debugs.Add;

        // Act
        var actual = await repo.Load();

        // Assert
        Assert.Null(actual);
        Assert.That(warnings.Single(), Does.Contain("Exception occurred while loading"));
        Assert.That(debugs.Single(), Does.Contain(nameof(SscmException)));
    }

    [Test]
    public async Task Save_Creates_Parent_Directories()
    {
        // Arrange
        var repo = this.CreateRepository();

        // Act
        await repo.Save(new RepositoryTestData { Name = "saved" });

        // Assert
        Assert.True(File.Exists(this.SavePath));
    }

    [Test]
    public async Task Save_Writes_Custom_Path()
    {
        // Arrange
        var repo = this.CreateRepository();

        // Act
        await repo.Save(new RepositoryTestData { Name = "custom" }, this.CustomSavePath);

        // Assert
        Assert.True(File.Exists(this.CustomSavePath));
    }

    [Test]
    public void Save_Throws_When_Data_Null()
    {
        // Arrange
        var repo = this.CreateRepository();

        // Act/Assert
        Assert.ThrowsAsync<ArgumentNullException>(() => repo.Save(null!));
    }

    [Test]
    public async Task Backup_Copies_Current_File_Using_Platform_Timestamp()
    {
        // Arrange
        var platform = new PlatformForTest(new DateTime(2026, 06, 15, 12, 34, 56, DateTimeKind.Utc));
        var repo = this.CreateRepository(platform);
        await repo.Save(new RepositoryTestData { Name = "backup" });
        var expected = Path.Combine(
            Path.GetDirectoryName(this.SavePath)!,
            $"mappings.{platform.UtcNow.ToLocalTime().ToString("yyyyMMddHHmmss")}.json.bak");

        // Act
        var actual = repo.Backup();

        // Assert
        Assert.AreEqual(expected, actual);
        Assert.True(File.Exists(actual));
    }

    [Test]
    public void Backup_Returns_Null_And_Warns_When_File_Missing()
    {
        // Arrange
        var warnings = new List<string>();
        var repo = this.CreateRepository();
        repo.WarningOutput += warnings.Add;

        // Act
        var actual = repo.Backup();

        // Assert
        Assert.Null(actual);
        Assert.That(warnings.Single(), Does.Contain("Nothing to back up"));
    }

    [Test]
    public async Task RestoreLatest_Restores_Latest_Timestamped_Backup()
    {
        // Arrange
        Directory.CreateDirectory(Path.GetDirectoryName(this.SavePath)!);
        await File.WriteAllTextAsync(this.SavePath, "current");
        var older = Path.Combine(Path.GetDirectoryName(this.SavePath)!, "mappings.20250101010101.json.bak");
        var latest = Path.Combine(Path.GetDirectoryName(this.SavePath)!, "mappings.20260101010101.json.bak");
        await File.WriteAllTextAsync(older, "older");
        await File.WriteAllTextAsync(latest, "latest");
        var repo = this.CreateRepository();

        // Act
        var actual = repo.RestoreLatest();

        // Assert
        Assert.AreEqual(latest, actual);
        Assert.AreEqual("latest", await File.ReadAllTextAsync(this.SavePath));
    }

    [Test]
    public void RestoreLatest_Returns_Null_And_Warns_When_No_Backups_Exist()
    {
        // Arrange
        Directory.CreateDirectory(Path.GetDirectoryName(this.SavePath)!);
        var warnings = new List<string>();
        var repo = this.CreateRepository();
        repo.WarningOutput += warnings.Add;

        // Act
        var actual = repo.RestoreLatest();

        // Assert
        Assert.Null(actual);
        Assert.That(warnings.Single(), Does.Contain("Could not find any backup files"));
    }

    private MappingDataRepositoryDefault<RepositoryTestData> CreateRepository(IPlatform? platform = null)
    {
        return new MappingDataRepositoryDefault<RepositoryTestData>(
            platform ?? new PlatformForTest(),
            this.SavePath,
            "mappings.{0}.json.bak");
    }

    public class RepositoryTestData
    {
        public string Name { get; set; } = string.Empty;
    }
}
