using NUnit.Framework;
using SSCM.Core;
using SSCM.Tests;
using SSCM.Tests.Mocks;

namespace SSCM.Tests;

[TestFixture]
public abstract class MappingExporter_BackupRestore_TestBase<TData> : TestBase
{
    protected readonly IPlatform _platform;

    protected MappingExporter_BackupRestore_TestBase()
    {
        this._platform = new PlatformForTest(DateTime.UtcNow);
    }


    protected abstract IMappingExporter<TData> CreateExporter();

    protected abstract string GameConfigPath { get; }

    protected abstract string BackupDir { get; }

    protected abstract string GetBackupFilePath(DateTime dt);

    protected abstract string BackupFileFilter { get; }

    protected async Task CreateDummyGameConfig()
    {
        // create dummy gameconfig
        if (File.Exists(this.GameConfigPath)) File.Delete(this.GameConfigPath);
        Directory.CreateDirectory(Path.GetDirectoryName(this.GameConfigPath));
        await File.WriteAllTextAsync(this.GameConfigPath, $"{nameof(RestoreLatest_Overwrites_GameConfig)}-{Guid.NewGuid().ToString()}");
    }

    public virtual async Task Backup_Creates_Copy()
    {
        // Arrange
        await this.CreateDummyGameConfig();
        var expected = this.GetBackupFilePath(this._platform.UtcNow.ToLocalTime());

        // Act
        var actual = this.CreateExporter().Backup();

        // Assert
        Assert.AreEqual(expected, actual);
        Assert.True(File.Exists(actual));
    }

    public virtual async Task RestoreLatest_Overwrites_GameConfig()
    {
        await this.CreateDummyGameConfig();

        // clean out old test data
        if (Directory.Exists(this.BackupDir))
        {
            foreach (var f in Directory.GetFiles(this.BackupDir, this.BackupFileFilter))
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
        var lastContents = string.Empty;
        for (var i = 1; i <= 12; i++)
        {
            var backupTime = new DateTime(2022, i, 1, i, i*2, i*3);
            lastContents = $"{nameof(RestoreLatest_Overwrites_GameConfig)}-{Guid.NewGuid().ToString()}";
            lastBackupPath = this.GetBackupFilePath(backupTime);
            await File.WriteAllTextAsync(lastBackupPath, lastContents);
        }

        var actual = this.CreateExporter().RestoreLatest();

        Assert.AreEqual(lastBackupPath, actual);
        Assert.True(File.Exists(actual));

        Assert.AreEqual(lastContents, await File.ReadAllTextAsync(lastBackupPath));
    }

    // TODO test gameconfig doesn't exist
}