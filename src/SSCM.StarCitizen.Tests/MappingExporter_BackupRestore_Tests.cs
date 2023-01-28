using NUnit.Framework;
using SSCM.Core;
using SSCM.StarCitizen;
using SSCM.StarCitizen.Tests.Mocks;
using SSCM.Tests;
using SSCM.Tests.Mocks;

namespace SSCM.StarCitizen.Tests;

[TestFixture]
public class MappingExporter_BackupRestore_Tests : MappingExporter_BackupRestore_TestBase<SCMappingData>
{
    private SCFoldersForTest? _folders;

    public MappingExporter_BackupRestore_Tests()
    {
    }

    private string GameConfigDir => Path.Combine(base.TestTempDir, "game");

    protected override string GameConfigPath => Path.Combine(this.GameConfigDir, "actionmaps.xml");

    protected override string BackupDir => Path.Combine(base.TestTempDir, "backup");

    protected override string GetBackupFilePath(DateTime dt) => Path.Combine(this.BackupDir, $"actionmaps.xml.{dt.ToString("yyyyMMddHHmmss")}.bak");

    protected override string BackupFileFilter => "actionmaps.xml.*.bak";

    protected override IMappingExporter<SCMappingData> CreateExporter()
    {
        this._folders = new SCFoldersForTest(this.GameConfigDir, this.BackupDir);
        var exporter = new MappingExporter(base._platform, this._folders, this.GameConfigPath);
        return exporter;
    }

    [Test]
    public override Task Backup_Creates_Copy() => base.Backup_Creates_Copy();

    [Test]
    public override Task RestoreLatest_Overwrites_GameConfig() => base.RestoreLatest_Overwrites_GameConfig();
}