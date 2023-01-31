using NUnit.Framework;
using SSCM.Core;
using SSCM.Tests;
using SSCM.Tests.Mocks;

namespace SSCM.Tests;

#pragma warning disable CS8604

[TestFixture]
public abstract class MappingExporter_BackupRestore_TestBase<TData> : TestBase
{
    protected readonly IPlatform _platform;

    protected MappingExporter_BackupRestore_TestBase()
    {
        this._platform = new PlatformForTest(DateTime.UtcNow);
    }

    protected abstract IMappingExporter<TData> CreateExporter();

    protected abstract string BackupDir { get; }

    protected async Task<string> CreateDummyFile(string path)
    {
        // create dummy file
        if (File.Exists(path)) File.Delete(path);
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        var contents = $"{nameof(MappingExporter_BackupRestore_TestBase<TData>)}-{Guid.NewGuid().ToString()}";
        await File.WriteAllTextAsync(path, contents);
        return contents;
    }

    protected async Task Backup_Creates_Copy(string targetPath, string expectedPath)
    {
        // Arrange
        await this.CreateDummyFile(targetPath);

        // Act
        var actual = this.CreateExporter().Backup();

        // Assert
        Assert.AreEqual(expectedPath, actual);
        Assert.True(File.Exists(actual));
    }

    protected async Task RestoreLatest_Overwrites_File(string targetPath, string backupFilter, Func<DateTime, string> backupFilenameGenerator)
    {
        await this.CreateDummyFile(targetPath);

        // clean out old test data
        if (Directory.Exists(this.BackupDir))
        {
            foreach (var f in Directory.GetFiles(this.BackupDir, backupFilter))
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
            lastBackupPath = backupFilenameGenerator(backupTime);
            lastContents = await this.CreateDummyFile(backupFilenameGenerator(backupTime));
        }

        var actual = this.CreateExporter().RestoreLatest();

        Assert.AreEqual(lastBackupPath, actual);
        Assert.True(File.Exists(actual));

        Assert.AreEqual(lastContents, await File.ReadAllTextAsync(lastBackupPath));
    }

    // TODO test gameconfig doesn't exist
}