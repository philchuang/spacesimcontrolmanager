using NUnit.Framework;
using SSCM.Core;
using System.CommandLine;

namespace SSCM.cli.Tests;

[TestFixture]
public class Program_CommandBinding_Tests
{
    [TestCase(new[] { "fake", "import" }, ImportMode.Tui, true)]
    [TestCase(new[] { "fake", "import", "preview" }, ImportMode.Preview, false)]
    [TestCase(new[] { "fake", "import", "merge" }, ImportMode.Merge, false)]
    [TestCase(new[] { "fake", "import", "overwrite" }, ImportMode.Overwrite, false)]
    [TestCase(new[] { "fake", "import", "serial" }, ImportMode.Serial, false)]
    [TestCase(new[] { "fake", "import", "tui" }, ImportMode.Tui, true)]
    public async Task Import_Commands_Call_Expected_Mode(string[] args, ImportMode expectedMode, bool expectedSelector)
    {
        // Arrange
        var manager = new FakeControlManager();

        // Act
        var exitCode = await Invoke(manager, args);

        // Assert
        Assert.AreEqual(0, exitCode);
        Assert.AreEqual(expectedMode, manager.ImportCalls.Single().Mode);
        Assert.AreEqual(expectedSelector, manager.ImportCalls.Single().SelectorProvided);
    }

    [TestCase(new[] { "fake", "export" }, ExportMode.Tui, true)]
    [TestCase(new[] { "fake", "export", "preview" }, ExportMode.Preview, false)]
    [TestCase(new[] { "fake", "export", "apply" }, ExportMode.Apply, false)]
    [TestCase(new[] { "fake", "export", "serial" }, ExportMode.Serial, false)]
    [TestCase(new[] { "fake", "export", "tui" }, ExportMode.Tui, true)]
    public async Task Export_Commands_Call_Expected_Mode(string[] args, ExportMode expectedMode, bool expectedSelector)
    {
        // Arrange
        var manager = new FakeControlManager();

        // Act
        var exitCode = await Invoke(manager, args);

        // Assert
        Assert.AreEqual(0, exitCode);
        Assert.AreEqual(expectedMode, manager.ExportCalls.Single().Mode);
        Assert.AreEqual(expectedSelector, manager.ExportCalls.Single().SelectorProvided);
    }

    [Test]
    public async Task Export_Matches_Option_Sets_OnlyMatches()
    {
        // Arrange
        var manager = new FakeControlManager();

        // Act
        var exitCode = await Invoke(manager, "fake", "export", "preview", "--matches");

        // Assert
        Assert.AreEqual(0, exitCode);
        Assert.True(manager.ExportCalls.Single().ExportOptions.OnlyMatches);
    }

    [TestCase(new[] { "fake", "upgrade" }, UpgradeMode.Preview)]
    [TestCase(new[] { "fake", "upgrade", "apply" }, UpgradeMode.Apply)]
    public async Task Upgrade_Commands_Call_Expected_Mode(string[] args, UpgradeMode expectedMode)
    {
        // Arrange
        var manager = new FakeControlManager();

        // Act
        var exitCode = await Invoke(manager, args);

        // Assert
        Assert.AreEqual(0, exitCode);
        Assert.AreEqual(expectedMode, manager.UpgradeCalls.Single().Mode);
    }

    [TestCase("--environment", "PTU")]
    [TestCase("-e", "EPTU")]
    public async Task Global_Options_Are_Passed_To_Manager(string option, string value)
    {
        // Arrange
        var manager = new FakeControlManager();

        // Act
        var exitCode = await Invoke(manager, "fake", option, value, "import", "preview");

        // Assert
        Assert.AreEqual(0, exitCode);
        Assert.AreEqual(value, manager.ImportCalls.Single().Options["environment"]);
    }

    [TestCase("md", ReportingFormat.Markdown)]
    [TestCase("csv", ReportingFormat.Csv)]
    [TestCase("json", ReportingFormat.Json)]
    public async Task Report_Format_Maps_To_ReportingOptions(string format, ReportingFormat expected)
    {
        // Arrange
        var manager = new FakeControlManager();

        // Act
        var exitCode = await Invoke(manager, "fake", "report", "--format", format);

        // Assert
        Assert.AreEqual(0, exitCode);
        Assert.AreEqual(expected, manager.ReportCalls.Single().ReportingOptions.Format);
    }

    [Test]
    public async Task Report_Flags_Map_To_ReportingOptions()
    {
        // Arrange
        var manager = new FakeControlManager();

        // Act
        var exitCode = await Invoke(manager, "fake", "report", "--preserved", "--names");

        // Assert
        Assert.AreEqual(0, exitCode);
        Assert.True(manager.ReportCalls.Single().ReportingOptions.PreservedOnly);
        Assert.True(manager.ReportCalls.Single().ReportingOptions.HeadersOnly);
    }

    [TestCase("backup", nameof(FakeControlManager.BackupCount))]
    [TestCase("restore", nameof(FakeControlManager.RestoreCount))]
    [TestCase("edit", nameof(FakeControlManager.OpenCount))]
    [TestCase("open", nameof(FakeControlManager.OpenCount))]
    [TestCase("editgame", nameof(FakeControlManager.OpenGameConfigCount))]
    [TestCase("opengame", nameof(FakeControlManager.OpenGameConfigCount))]
    public async Task Simple_Commands_Delegate_To_Manager(string command, string counterName)
    {
        // Arrange
        var manager = new FakeControlManager();

        // Act
        var exitCode = await Invoke(manager, "fake", command);

        // Assert
        Assert.AreEqual(0, exitCode);
        Assert.AreEqual(1, manager.GetCounter(counterName));
    }

    private static Task<int> Invoke(FakeControlManager manager, params string[] args)
    {
        var root = Program.BuildRootCommand([manager]);
        return root.Parse(args).InvokeAsync(new InvocationConfiguration());
    }

    private class FakeControlManager : IControlManager
    {
        public event Action<string> StandardOutput = delegate {};
        public event Action<string> WarningOutput = delegate {};
        public event Action<string> DebugOutput = delegate {};

        public string CommandAlias => "fake";
        public string GameType => "Fake Game";
        public string GameTypeTitle => "Fake Game";
        public List<CommandOption> GlobalOptions { get; } =
        [
            new CommandOption
            {
                Name = "environment",
                ShortName = "e",
                Description = "Environment",
                DefaultValue = "LIVE",
            }
        ];

        public List<ImportCall> ImportCalls { get; } = new();
        public List<ExportCall> ExportCalls { get; } = new();
        public List<UpgradeCall> UpgradeCalls { get; } = new();
        public List<ReportCall> ReportCalls { get; } = new();
        public int BackupCount { get; private set; }
        public int RestoreCount { get; private set; }
        public int OpenCount { get; private set; }
        public int OpenGameConfigCount { get; private set; }

        public Task Import(ImportMode mode, Dictionary<string, string>? options, IInteractiveChangeSelector? selector = null)
        {
            this.ImportCalls.Add(new ImportCall(mode, options ?? new Dictionary<string, string>(), selector != null));
            return Task.CompletedTask;
        }

        public Task Upgrade(UpgradeMode mode, Dictionary<string, string>? options)
        {
            this.UpgradeCalls.Add(new UpgradeCall(mode, options ?? new Dictionary<string, string>()));
            return Task.CompletedTask;
        }

        public Task Export(ExportMode mode, ExportOptions exportOptions, Dictionary<string, string>? options, IInteractiveChangeSelector? selector = null)
        {
            this.ExportCalls.Add(new ExportCall(mode, exportOptions, options ?? new Dictionary<string, string>(), selector != null));
            return Task.CompletedTask;
        }

        public Task<string> Report(ReportingOptions reportingOptions, Dictionary<string, string>? options)
        {
            this.ReportCalls.Add(new ReportCall(reportingOptions, options ?? new Dictionary<string, string>()));
            return Task.FromResult("report");
        }

        public void Backup(Dictionary<string, string>? options) => this.BackupCount++;
        public void Restore(Dictionary<string, string>? options) => this.RestoreCount++;
        public void Open(Dictionary<string, string>? options) => this.OpenCount++;
        public void OpenGameConfig(Dictionary<string, string>? options) => this.OpenGameConfigCount++;

        public int GetCounter(string name) => name switch
        {
            nameof(this.BackupCount) => this.BackupCount,
            nameof(this.RestoreCount) => this.RestoreCount,
            nameof(this.OpenCount) => this.OpenCount,
            nameof(this.OpenGameConfigCount) => this.OpenGameConfigCount,
            _ => throw new ArgumentOutOfRangeException(nameof(name), name, null),
        };
    }

    private record ImportCall(ImportMode Mode, Dictionary<string, string> Options, bool SelectorProvided);
    private record ExportCall(ExportMode Mode, ExportOptions ExportOptions, Dictionary<string, string> Options, bool SelectorProvided);
    private record UpgradeCall(UpgradeMode Mode, Dictionary<string, string> Options);
    private record ReportCall(ReportingOptions ReportingOptions, Dictionary<string, string> Options);
}
