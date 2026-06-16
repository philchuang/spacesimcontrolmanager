using NUnit.Framework;
using SSCM.Core;
using SSCM.Tests.Mocks;

namespace SSCM.Tests;

[TestFixture]
public class ControlManagerBase_Tests
{
    [Test]
    public async Task Import_Overwrite_Saves_Imported_Data_When_No_Current_Data()
    {
        // Arrange
        var manager = new FakeControlManager();
        manager.Repository.LoadResult = null;
        manager.Importer.ReadResult = new ControlManagerTestData("imported");

        // Act
        await manager.Import(ImportMode.Overwrite);

        // Assert
        Assert.AreSame(manager.Importer.ReadResult, manager.Repository.SavedData.Single());
        Assert.AreEqual(0, manager.Repository.BackupCount);
    }

    [Test]
    public async Task Import_Overwrite_Warns_When_Replacing_Current_Data()
    {
        // Arrange
        var warnings = new List<string>();
        var manager = new FakeControlManager();
        manager.WarningOutput += warnings.Add;
        manager.Repository.LoadResult = new ControlManagerTestData("current");

        // Act
        await manager.Import(ImportMode.Overwrite);

        // Assert
        Assert.That(warnings.Single(), Does.Contain("Overwriting existing mappings data"));
        Assert.AreSame(manager.Importer.ReadResult, manager.Repository.SavedData.Single());
    }

    [Test]
    public async Task Import_Merge_Backs_Up_Before_Saving_Merged_Data()
    {
        // Arrange
        var manager = new FakeControlManager();
        manager.Repository.LoadResult = new ControlManagerTestData("current");
        manager.Merger.MergeResult = new ControlManagerTestData("merged");

        // Act
        await manager.Import(ImportMode.Merge);

        // Assert
        Assert.AreEqual(1, manager.Repository.BackupCount);
        Assert.AreSame(manager.Merger.MergeResult, manager.Repository.SavedData.Single());
    }

    [Test]
    public async Task Import_Preview_Does_Not_Save()
    {
        // Arrange
        var output = new List<string>();
        var manager = new FakeControlManager();
        manager.StandardOutput += output.Add;
        manager.Repository.LoadResult = new ControlManagerTestData("current");
        manager.Merger.PreviewResult = true;
        manager.Merger.Result.MergeActions.Add(new MappingMergeAction(MappingMergeActionMode.Add, "x"));

        // Act
        await manager.Import(ImportMode.Preview);

        // Assert
        Assert.IsEmpty(manager.Repository.SavedData);
        Assert.AreEqual(0, manager.Repository.BackupCount);
        Assert.That(output.Last(), Does.Contain("1 changes NOT saved"));
    }

    [Test]
    public async Task Import_Serial_Reports_Cancellation_And_Does_Not_Save()
    {
        // Arrange
        var output = new List<string>();
        var manager = new FakeControlManager();
        manager.StandardOutput += output.Add;
        manager.Repository.LoadResult = new ControlManagerTestData("current");
        manager.Merger.ThrowOnMergeInteractive = true;

        // Act
        await manager.Import(ImportMode.Serial);

        // Assert
        Assert.IsEmpty(manager.Repository.SavedData);
        Assert.AreEqual(1, manager.Repository.BackupCount);
        Assert.That(output.Last(), Does.Contain("Merge cancelled"));
    }

    [Test]
    public async Task Import_Serial_Saves_When_Interactive_Merge_Returns_Data()
    {
        // Arrange
        var manager = new FakeControlManager();
        manager.Repository.LoadResult = new ControlManagerTestData("current");
        manager.Merger.MergeResult = new ControlManagerTestData("merged");

        // Act
        await manager.Import(ImportMode.Serial);

        // Assert
        Assert.AreEqual(1, manager.Repository.BackupCount);
        Assert.AreSame(manager.Merger.MergeResult, manager.Repository.SavedData.Single());
    }

    [Test]
    public void Import_Tui_Throws_When_Selector_Is_Null()
    {
        // Arrange
        var manager = new FakeControlManager();
        manager.Repository.LoadResult = new ControlManagerTestData("current");

        // Act/Assert
        Assert.ThrowsAsync<ArgumentNullException>(() => manager.Import(ImportMode.Tui));
    }

    [Test]
    public async Task Import_Tui_Does_Not_Backup_Or_Save_When_No_Rows()
    {
        // Arrange
        var output = new List<string>();
        var manager = new FakeControlManager();
        manager.StandardOutput += output.Add;
        manager.Repository.LoadResult = new ControlManagerTestData("current");
        manager.Merger.InteractiveSession = new InteractiveChangeSession([]);

        // Act
        await manager.Import(ImportMode.Tui, null, new FakeInteractiveChangeSelector(true));

        // Assert
        Assert.AreEqual(0, manager.Repository.BackupCount);
        Assert.IsEmpty(manager.Repository.SavedData);
        Assert.That(output.Last(), Does.Contain("No changes to make"));
    }

    [Test]
    public async Task Import_Tui_Backs_Up_And_Saves_When_Selector_Applies()
    {
        // Arrange
        var manager = new FakeControlManager();
        manager.Repository.LoadResult = new ControlManagerTestData("current");
        manager.Merger.InteractiveSession = CreateSession();

        // Act
        await manager.Import(ImportMode.Tui, null, new FakeInteractiveChangeSelector(true));

        // Assert
        Assert.AreEqual(1, manager.Repository.BackupCount);
        Assert.AreSame(manager.Repository.LoadResult, manager.Repository.SavedData.Single());
    }

    [Test]
    public async Task Upgrade_Apply_Backs_Up_Before_Saving_Upgraded_Data()
    {
        // Arrange
        var manager = new FakeControlManager();
        manager.Repository.LoadResult = new ControlManagerTestData("current");
        manager.Upgrader.UpgradeResult = new ControlManagerTestData("upgraded");

        // Act
        await manager.Upgrade(UpgradeMode.Apply);

        // Assert
        Assert.AreEqual(1, manager.Repository.BackupCount);
        Assert.AreSame(manager.Upgrader.UpgradeResult, manager.Repository.SavedData.Single());
    }

    [Test]
    public async Task Upgrade_Preview_Does_Not_Backup_Or_Save()
    {
        // Arrange
        var manager = new FakeControlManager();
        manager.Repository.LoadResult = new ControlManagerTestData("current");
        manager.Upgrader.PreviewResult = true;
        manager.Upgrader.Result.MergeActions.Add(new MappingMergeAction(MappingMergeActionMode.Add, "x"));

        // Act
        await manager.Upgrade(UpgradeMode.Preview);

        // Assert
        Assert.AreEqual(0, manager.Repository.BackupCount);
        Assert.IsEmpty(manager.Repository.SavedData);
    }

    [Test]
    public async Task Upgrade_Exits_Quietly_When_Load_Returns_Null()
    {
        // Arrange
        var manager = new FakeControlManager();
        manager.Repository.LoadResult = null;

        // Act
        await manager.Upgrade(UpgradeMode.Apply);

        // Assert
        Assert.AreEqual(0, manager.Repository.BackupCount);
        Assert.IsEmpty(manager.Repository.SavedData);
    }

    [Test]
    public async Task Export_Apply_Backs_Up_Before_Update()
    {
        // Arrange
        var manager = new FakeControlManager();
        manager.Repository.LoadResult = new ControlManagerTestData("current");

        // Act
        await manager.Export(ExportMode.Apply, new ExportOptions { OnlyMatches = true });

        // Assert
        Assert.AreEqual(1, manager.Exporter.BackupCount);
        Assert.AreSame(manager.Repository.LoadResult, manager.Exporter.UpdatedData.Single());
        Assert.True(manager.Exporter.ExportOptions.OnlyMatches);
    }

    [Test]
    public async Task Export_Preview_Does_Not_Backup_Or_Update()
    {
        // Arrange
        var manager = new FakeControlManager();
        manager.Repository.LoadResult = new ControlManagerTestData("current");

        // Act
        await manager.Export(ExportMode.Preview, ExportOptions.Default);

        // Assert
        Assert.AreEqual(0, manager.Exporter.BackupCount);
        Assert.IsEmpty(manager.Exporter.UpdatedData);
        Assert.AreSame(manager.Repository.LoadResult, manager.Exporter.PreviewedData.Single());
    }

    [Test]
    public async Task Export_Tui_Does_Not_Backup_Or_Save_When_No_Rows()
    {
        // Arrange
        var manager = new FakeControlManager();
        manager.Repository.LoadResult = new ControlManagerTestData("current");
        manager.Exporter.InteractiveSession = new InteractiveChangeSession([]);

        // Act
        await manager.Export(ExportMode.Tui, ExportOptions.Default, null, new FakeInteractiveChangeSelector(true));

        // Assert
        Assert.AreEqual(0, manager.Exporter.BackupCount);
        Assert.AreEqual(0, manager.Exporter.SaveInteractiveCount);
    }

    [Test]
    public async Task Export_Tui_Backs_Up_And_Saves_When_Selector_Applies()
    {
        // Arrange
        var manager = new FakeControlManager();
        manager.Repository.LoadResult = new ControlManagerTestData("current");
        manager.Exporter.InteractiveSession = CreateSession();

        // Act
        await manager.Export(ExportMode.Tui, ExportOptions.Default, null, new FakeInteractiveChangeSelector(true));

        // Assert
        Assert.AreEqual(1, manager.Exporter.BackupCount);
        Assert.AreEqual(1, manager.Exporter.SaveInteractiveCount);
    }

    [Test]
    public async Task Export_Serial_Reports_No_Changes_When_Exporter_Returns_False()
    {
        // Arrange
        var output = new List<string>();
        var manager = new FakeControlManager();
        manager.StandardOutput += output.Add;
        manager.Repository.LoadResult = new ControlManagerTestData("current");
        manager.Exporter.UpdateInteractiveResult = false;

        // Act
        await manager.Export(ExportMode.Serial, ExportOptions.Default);

        // Assert
        Assert.AreEqual(1, manager.Exporter.BackupCount);
        Assert.That(output.Last(), Does.Contain("No changes necessary"));
    }

    [Test]
    public async Task Export_Serial_Reports_Update_When_Exporter_Returns_True()
    {
        // Arrange
        var output = new List<string>();
        var manager = new FakeControlManager();
        manager.StandardOutput += output.Add;
        manager.Repository.LoadResult = new ControlManagerTestData("current");
        manager.Exporter.UpdateInteractiveResult = true;

        // Act
        await manager.Export(ExportMode.Serial, ExportOptions.Default);

        // Assert
        Assert.AreEqual(1, manager.Exporter.BackupCount);
        Assert.That(output.Last(), Does.Contain("CONFIGURATION UPDATED"));
    }

    [Test]
    public async Task Export_Serial_Reports_Cancellation()
    {
        // Arrange
        var output = new List<string>();
        var manager = new FakeControlManager();
        manager.StandardOutput += output.Add;
        manager.Repository.LoadResult = new ControlManagerTestData("current");
        manager.Exporter.ThrowOnUpdateInteractive = true;

        // Act
        await manager.Export(ExportMode.Serial, ExportOptions.Default);

        // Assert
        Assert.AreEqual(1, manager.Exporter.BackupCount);
        Assert.That(output.Last(), Does.Contain("Export cancelled"));
    }

    [Test]
    public async Task Report_Uses_CreateNew_When_Load_Returns_Null()
    {
        // Arrange
        var manager = new FakeControlManager();
        manager.Repository.LoadResult = null;

        // Act
        var actual = await manager.Report(new ReportingOptions());

        // Assert
        Assert.AreEqual("new", manager.Reporter.ReportedData.Single().Name);
        Assert.AreEqual("reported:new", actual);
    }

    [Test]
    public async Task Report_Uses_Repository_Data_When_Available()
    {
        // Arrange
        var manager = new FakeControlManager();
        manager.Repository.LoadResult = new ControlManagerTestData("current");

        // Act
        var actual = await manager.Report(new ReportingOptions());

        // Assert
        Assert.AreSame(manager.Repository.LoadResult, manager.Reporter.ReportedData.Single());
        Assert.AreEqual("reported:current", actual);
    }

    [Test]
    public void Backup_Delegates_To_Exporter()
    {
        // Arrange
        var output = new List<string>();
        var manager = new FakeControlManager();
        manager.StandardOutput += output.Add;

        // Act
        manager.Backup(new Dictionary<string, string>());

        // Assert
        Assert.AreEqual(1, manager.Exporter.BackupCount);
        Assert.That(output.Single(), Does.Contain("backup-path"));
    }

    [Test]
    public void Restore_Delegates_To_Exporter()
    {
        // Arrange
        var output = new List<string>();
        var manager = new FakeControlManager();
        manager.StandardOutput += output.Add;

        // Act
        manager.Restore(new Dictionary<string, string>());

        // Assert
        Assert.AreEqual(1, manager.Exporter.RestoreLatestCount);
        Assert.That(output.Single(), Does.Contain("restore-path"));
    }

    [Test]
    public void Open_Calls_Platform_With_Mapping_Data_Path()
    {
        // Arrange
        var opened = new List<string>();
        var manager = new FakeControlManager(new PlatformForTest(openMock: opened.Add));

        // Act
        manager.Open(new Dictionary<string, string>());

        // Assert
        Assert.AreEqual("mapping-data.json", opened.Single());
    }

    [Test]
    public void Open_Reports_When_Mapping_Data_File_Is_Missing()
    {
        // Arrange
        var output = new List<string>();
        var manager = new FakeControlManager(new PlatformForTest(openMock: _ => throw new FileNotFoundException()));
        manager.StandardOutput += output.Add;

        // Act
        manager.Open(new Dictionary<string, string>());

        // Assert
        Assert.That(output.Single(), Does.Contain("Could not find mapping file"));
        Assert.That(output.Single(), Does.Contain("mapping-data.json"));
    }

    [Test]
    public void OpenGameConfig_Calls_Platform_With_Game_Config_Path()
    {
        // Arrange
        var opened = new List<string>();
        var manager = new FakeControlManager(new PlatformForTest(openMock: opened.Add));

        // Act
        manager.OpenGameConfig(new Dictionary<string, string>());

        // Assert
        Assert.AreEqual("game-config.xml", opened.Single());
    }

    [Test]
    public void OpenGameConfig_Reports_When_Game_Config_File_Is_Missing()
    {
        // Arrange
        var output = new List<string>();
        var manager = new FakeControlManager(new PlatformForTest(openMock: _ => throw new FileNotFoundException()));
        manager.StandardOutput += output.Add;

        // Act
        manager.OpenGameConfig(new Dictionary<string, string>());

        // Assert
        Assert.That(output.Single(), Does.Contain("Could not find mapping file"));
        Assert.That(output.Single(), Does.Contain("game-config.xml"));
    }

    [Test]
    public async Task Import_Allows_Null_Options()
    {
        // Arrange
        var manager = new FakeControlManager();
        manager.Repository.LoadResult = null;

        // Act
        await manager.Import(ImportMode.Overwrite, null);

        // Assert
        Assert.IsEmpty(manager.AppliedOptions.Single());
    }

    [Test]
    public async Task Upgrade_Allows_Null_Options()
    {
        // Arrange
        var manager = new FakeControlManager();
        manager.Repository.LoadResult = null;

        // Act
        await manager.Upgrade(UpgradeMode.Apply, null);

        // Assert
        Assert.IsEmpty(manager.AppliedOptions.Single());
    }

    [Test]
    public async Task Export_Allows_Null_Options()
    {
        // Arrange
        var manager = new FakeControlManager();
        manager.Repository.LoadResult = new ControlManagerTestData("current");

        // Act
        await manager.Export(ExportMode.Preview, ExportOptions.Default, null);

        // Assert
        Assert.IsEmpty(manager.AppliedOptions.Single());
    }

    [Test]
    public async Task Report_Allows_Null_Options()
    {
        // Arrange
        var manager = new FakeControlManager();

        // Act
        await manager.Report(new ReportingOptions(), null);

        // Assert
        Assert.IsEmpty(manager.AppliedOptions.Single());
    }

    [Test]
    public void Backup_Allows_Null_Options()
    {
        // Arrange
        var manager = new FakeControlManager();

        // Act
        manager.Backup(null);

        // Assert
        Assert.IsEmpty(manager.AppliedOptions.Single());
    }

    [Test]
    public void Restore_Allows_Null_Options()
    {
        // Arrange
        var manager = new FakeControlManager();

        // Act
        manager.Restore(null);

        // Assert
        Assert.IsEmpty(manager.AppliedOptions.Single());
    }

    [Test]
    public void Open_Allows_Null_Options()
    {
        // Arrange
        var manager = new FakeControlManager();

        // Act
        manager.Open(null);

        // Assert
        Assert.IsEmpty(manager.AppliedOptions.Single());
    }

    [Test]
    public void OpenGameConfig_Allows_Null_Options()
    {
        // Arrange
        var manager = new FakeControlManager();

        // Act
        manager.OpenGameConfig(null);

        // Assert
        Assert.IsEmpty(manager.AppliedOptions.Single());
    }

    private static InteractiveChangeSession CreateSession()
    {
        return new InteractiveChangeSession([
            new InteractiveChangeRow("1", "Update", "item", "old", "new", true, () => true),
        ]);
    }
}
