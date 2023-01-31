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

    protected override string BackupDir => Path.Combine(base.TestTempDir, "backup");

    private string GameConfigDir => Path.Combine(base.TestTempDir, "game");
    private string GameAttributesPath => Path.Combine(this.GameConfigDir, "attributes.xml");
    private string GameMappingsPath => Path.Combine(this.GameConfigDir, "actionmaps.xml");

    protected override IMappingExporter<SCMappingData> CreateExporter()
    {
        this._folders = new SCFoldersForTest(this.GameConfigDir, this.BackupDir) { GameMappingsPath = this.GameMappingsPath, GameAttributesPath = this.GameAttributesPath };
        var exporter = new MappingExporter(base._platform, this._folders);
        return exporter;
    }

    [Test]
    public async Task Backup_Creates_Copy()
    {
        // Arrange
        var dt = this._platform.UtcNow.ToLocalTime();
        await this.CreateDummyFile(this.GameMappingsPath);
        var mappings = Path.Combine(this.BackupDir, $"{Path.GetFileName(this.GameMappingsPath)}.{dt.ToString("yyyyMMddHHmmss")}.bak");
        await this.CreateDummyFile(this.GameAttributesPath);
        var attributes = Path.Combine(this.BackupDir, $"{Path.GetFileName(this.GameAttributesPath)}.{dt.ToString("yyyyMMddHHmmss")}.bak");
        var expected = $"{mappings},{attributes}";

        // Act
        var actual = this.CreateExporter().Backup();

        // Assert
        Assert.AreEqual(expected, actual);

        var actualPaths = actual.Split(",").ToList();
        actualPaths.ForEach(f => Assert.True(File.Exists(f), f));
    }

    [Test]
    public async Task RestoreLatest_Overwrites_GameConfig()
    {
        await this.CreateDummyFile(this.GameMappingsPath);
        await this.CreateDummyFile(this.GameAttributesPath);

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
        var lastBackupPaths = new List<string>();
        var lastContents = new List<string>();
        foreach (var file in new[] { "actionmaps.xml", "attributes.xml" })
        {
            var lastBackupPath = string.Empty;
            var lastContent = string.Empty;
            for (var i = 1; i <= 12; i++)
            {
                var backupTime = new DateTime(2022, i, 1, i, i*2, i*3);
                lastBackupPath = Path.Combine(this.BackupDir, $"{file}.{backupTime.ToString("yyyyMMddHHmmss")}.bak");
                lastContent = await this.CreateDummyFile(lastBackupPath);
            }
            lastBackupPaths.Add(lastBackupPath);
            lastContents.Add(lastContent);
        }

        var actual = this.CreateExporter().RestoreLatest();

        Assert.AreEqual(string.Join(",", lastBackupPaths), actual);
        var actualPaths = actual.Split(",").ToList();
        actualPaths.ForEach(f => Assert.True(File.Exists(f), f));

        lastContents.Select((c, i) => (content: c, idx: i)).ToList().ForEach(t => Assert.AreEqual(t.content, File.ReadAllTextAsync(actualPaths[t.idx]).Result));
    }
}