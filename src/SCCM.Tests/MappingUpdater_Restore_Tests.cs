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
        this._folders = new FoldersForTest(sccmDir: System.IO.Directory.GetCurrentDirectory());
        this._updater = new MappingUpdater(this._platform, this._folders, Samples.GetActionMapsXmlPath());
    }

    [Test]
    public async Task Backup_Creates_Copy()
    {
        var expected = System.IO.Path.Combine(this._folders.SccmDir, $"actionmaps.xml.{this._platform.UtcNow.ToLocalTime().ToString("yyyyMMddHHmmss")}.bak");
        var actual = this._updater.Backup();

        Assert.AreEqual(expected, actual);
        Assert.True(System.IO.File.Exists(actual));

        System.IO.File.Delete(actual);
    }
}