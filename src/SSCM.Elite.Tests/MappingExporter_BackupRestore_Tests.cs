using NUnit.Framework;
using SSCM.Core;
using SSCM.Elite;
using SSCM.Elite.Tests.Mocks;
using SSCM.Tests;
using SSCM.Tests.Mocks;

namespace SSCM.Elite.Tests;

[TestFixture]
public class MappingExporter_BackupRestore_Tests : MappingExporter_BackupRestore_TestBase<EDMappingData>
{
    private EDFoldersForTest? _folders;

    public MappingExporter_BackupRestore_Tests()
    {
    }

    protected override string BackupDir => Path.Combine(base.TestTempDir, "backup");
    
    private string GameConfigDir => Path.Combine(base.TestTempDir, "game");
    private string GameMappingsPath => Path.Combine(this.GameConfigDir, "custom.4.0.binds");

    protected override IMappingExporter<EDMappingData> CreateExporter()
    {
        this._folders = new EDFoldersForTest {
            GameConfigDir = this.GameConfigDir,
            GameConfigPath = this.GameMappingsPath, 
            EliteDataDir = this.BackupDir
        };
        var exporter = new MappingExporter(base._platform, this._folders);
        return exporter;
    }

    [Test]
    public async Task Backup_Creates_Copy()
    {
        // Arrange
        var dt = base._platform.UtcNow.ToLocalTime();
        await this.CreateDummyFile(this.GameMappingsPath);
        var expected = Path.Combine(this.BackupDir, $"{Path.GetFileName(this.GameMappingsPath)}.{dt.ToString("yyyyMMddHHmmss")}.bak");

        // Act
        var actual = this.CreateExporter().Backup();

        // Assert
        Assert.AreEqual(expected, actual);
        Assert.True(File.Exists(actual));
    }

    [Test]
    public async Task RestoreLatest_Overwrites_GameConfig()
    {
        await this.CreateDummyFile(this.GameMappingsPath);

        // clean out old test data
        if (Directory.Exists(this.BackupDir))
        {
            foreach (var f in Directory.GetFiles(this.BackupDir, "*.bak"))
            {
                File.Delete(f);
            }
        }
        else
        {
            Directory.CreateDirectory(this.BackupDir);
        }

        // create multiple backups
        var lastBackupPath = string.Empty;
        var lastContent = string.Empty;
        for (var i = 1; i <= 12; i++)
        {
            var backupTime = new DateTime(2022, i, 1, i, i*2, i*3);
            lastBackupPath = Path.Combine(this.BackupDir, $"{Path.GetFileName(this.GameMappingsPath)}.{backupTime.ToString("yyyyMMddHHmmss")}.bak");
            lastContent = await this.CreateDummyFile(lastBackupPath);
        }

        var actual = this.CreateExporter().RestoreLatest();

        Assert.AreEqual(lastBackupPath, actual);
        Assert.True(File.Exists(actual), actual);

        Assert.AreEqual(lastContent, File.ReadAllTextAsync(actual).Result);
    }
}