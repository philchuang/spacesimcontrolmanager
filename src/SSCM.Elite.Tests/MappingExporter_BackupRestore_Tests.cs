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

    private string GameConfigDir => Path.Combine(base.TestTempDir, "game");

    protected override string GameConfigPath => Path.Combine(this.GameConfigDir, "custom.4.0.binds");

    protected override string BackupDir => Path.Combine(base.TestTempDir, "backup");

    protected override string GetBackupFilePath(DateTime dt) => Path.Combine(this.BackupDir, $"custom.4.0.binds.{dt.ToString("yyyyMMddHHmmss")}.bak");

    protected override string BackupFileFilter => "custom.4.0.binds.*.bak";

    protected override IMappingExporter<EDMappingData> CreateExporter()
    {
        this._folders = new EDFoldersForTest {
            GameConfigDir = this.GameConfigDir,
            GameConfigPath = this.GameConfigPath, 
            EliteDataDir = this.BackupDir
        };
        var exporter = new MappingExporter(base._platform, this._folders);
        return exporter;
    }

    [Test]
    public override Task Backup_Creates_Copy() => base.Backup_Creates_Copy();

    [Test]
    public override Task RestoreLatest_Overwrites_GameConfig() => base.RestoreLatest_Overwrites_GameConfig();
}