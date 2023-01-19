using NUnit.Framework;
using SSCM.Core;
using SSCM.Core.StarCitizen;
using SSCM.Tests.Mocks;

namespace SSCM.Tests;

[TestFixture]
public class MappingExporter_Backup_Tests
{
    private readonly MappingExporter _updater;
    private readonly IPlatform _platform;
    private readonly ISCFolders _folders;

    public MappingExporter_Backup_Tests()
    {
        this._platform = new PlatformForTest(DateTime.UtcNow);
        this._folders = new SCFoldersForTest(sscmDataDir: System.IO.Directory.GetCurrentDirectory());
        this._updater = new MappingExporter(this._platform, this._folders, Samples.GetActionMapsXmlPath());
    }

    [Test]
    public void Backup_Creates_Copy()
    {
        var expected = System.IO.Path.Combine(this._folders.SscmDataDir, $"actionmaps.xml.{this._platform.UtcNow.ToLocalTime().ToString("yyyyMMddHHmmss")}.bak");
        var actual = this._updater.Backup();

        Assert.AreEqual(expected, actual);
        Assert.True(System.IO.File.Exists(actual));

        System.IO.File.Delete(actual);
    }
}