using NUnit.Framework;
using SCCM.Core;
using SCCM.Tests.Mocks;

namespace SCCM.Tests;

[TestFixture]
public class MappingUpdater_Restore_Tests
{
    private readonly MappingUpdater _updater;
    private readonly IPlatform _platform;
    private readonly IFolders _folders;

    public MappingUpdater_Restore_Tests()
    {
        this._platform = new PlatformForTest(DateTime.UtcNow, programFilesDir: System.IO.Directory.GetCurrentDirectory());
        this._folders = new FoldersForTest(actionmapsDir: System.IO.Directory.GetCurrentDirectory(), sccmDir: System.IO.Directory.GetCurrentDirectory());
        this._updater = new MappingUpdater(this._platform, this._folders, System.IO.Path.Combine(this._folders.ActionMapsDir, "actionmaps.xml"));
    }

    [Test]
    public async Task Restore_Overwrites_Actionmapsxml()
    {
        var filesToCleanup = new List<string>();

        // create dummy actionmaps
        var actionmapsxmlpath = this._updater.ActionMapsXmlPath;
        await System.IO.File.WriteAllTextAsync(actionmapsxmlpath, $"{nameof(Restore_Overwrites_Actionmapsxml)}-{Guid.NewGuid().ToString()}");
        filesToCleanup.Add(actionmapsxmlpath);

        // clean out old test data
        foreach (var f in System.IO.Directory.GetFiles(this._folders.SccmDir, "actionmaps.xml.*.bak"))
        {
            System.IO.File.Delete(f);
        }

        // create multiple backups
        var lastBackupPath = string.Empty;
        var lastContents = string.Empty;
        for (var i = 1; i <= 12; i++)
        {
            var backupTime = new DateTime(2022, i, 1, i, i*2, i*3);
            lastContents = $"{nameof(Restore_Overwrites_Actionmapsxml)}-{Guid.NewGuid().ToString()}";
            lastBackupPath = System.IO.Path.Combine(this._folders.SccmDir, $"actionmaps.xml.{backupTime.ToString("yyyyMMddHHmmss")}.bak");
            await System.IO.File.WriteAllTextAsync(lastBackupPath, lastContents);
            filesToCleanup.Add(lastBackupPath);
        }

        var actual = this._updater.RestoreLatest();

        Assert.AreEqual(lastBackupPath, actual);
        Assert.True(System.IO.File.Exists(actual));

        Assert.AreEqual(lastContents, await System.IO.File.ReadAllTextAsync(lastBackupPath));
        
        foreach (var f in filesToCleanup)
        {
            System.IO.File.Delete(f);
        }
    }
}