using NUnit.Framework;
using SCCM.Core;
using SCCM.Tests.Mocks;

namespace SCCM.Tests;

[TestFixture]
public class MappingUpdater_Backup_Tests
{
    private readonly MappingUpdater _updater;
    private readonly IPlatform _platform;

    public MappingUpdater_Backup_Tests()
    {
        this._platform = new PlatformForTest(DateTime.UtcNow, sccmDir: System.IO.Directory.GetCurrentDirectory());
        this._updater = new MappingUpdater(this._platform, Samples.GetActionMapsXmlPath());
    }

    [Test]
    public async Task Backup_Creates_Copy()
    {
        var expected = System.IO.Path.Combine(this._platform.SccmDir, $"actionmaps.xml.{this._platform.UtcNow.ToLocalTime().ToString("yyyyMMddHHmmss")}.bak");
        var actual = this._updater.Backup();

        Assert.AreEqual(expected, actual);
        Assert.True(System.IO.File.Exists(actual));

        System.IO.File.Delete(actual);
    }
}