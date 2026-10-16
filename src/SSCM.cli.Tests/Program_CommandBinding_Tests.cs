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

    [TestCase(new[] { "fake", "print" }, null)]
    [TestCase(new[] { "fake", "print", "markdown" }, "markdown")]
    [TestCase(new[] { "fake", "print", "tsv" }, "tsv")]
    [TestCase(new[] { "fake", "print", "html" }, "html")]
    public async Task Print_Commands_Call_Expected_Format(string[] args, string? expectedFormat)
    {
        // Arrange
        var manager = new FakePrintControlManager();

        // Act
        var exitCode = await Invoke(manager, args);

        // Assert
        Assert.AreEqual(0, exitCode);
        Assert.AreEqual(expectedFormat, manager.PrintCalls.Single().Format);
        Assert.IsEmpty(manager.PrintCalls.Single().PrintOptions.Flags, "no switch is on unless it is asked for");
    }

    [TestCase(new[] { "fake", "print", "--refresh-defaults" }, null)]
    [TestCase(new[] { "fake", "print", "--refresh-defaults", "html" }, "html")]
    [TestCase(new[] { "fake", "print", "tsv", "--refresh-defaults" }, "tsv")]
    public async Task Print_Flag_Is_Set_Before_Or_After_The_Format(string[] args, string? expectedFormat)
    {
        // Arrange
        var manager = new FakePrintControlManager();

        // Act
        var exitCode = await Invoke(manager, args);

        // Assert
        Assert.AreEqual(0, exitCode);
        Assert.AreEqual(expectedFormat, manager.PrintCalls.Single().Format);
        Assert.AreEqual(new[] { "refresh-defaults" }, manager.PrintCalls.Single().PrintOptions.Flags);
    }

    [Test]
    public void Print_Subcommands_And_Options_Come_From_The_Implementation()
    {
        // Arrange
        var manager = new FakePrintControlManager
        {
            PrintFormats = [new BindingPrintFormat("json", "Prints JSON."), new BindingPrintFormat("csv", "Prints CSV.")],
            PrintFlags = [new BindingPrintFlag("verbose", "Be chatty."), new BindingPrintFlag("Include-Unbound", "Keep unbound rows.")],
        };

        // Act
        var print = FindPrintCommand(manager);

        // Assert
        Assert.AreEqual(new[] { "json", "csv" }, print.Subcommands.Select(c => c.Name));
        Assert.AreEqual(new[] { "Prints JSON.", "Prints CSV." }, print.Subcommands.Select(c => c.Description));
        Assert.That(print.Options.Select(o => o.Name), Is.SupersetOf(new[] { "--verbose", "--include-unbound" }));
        Assert.That(print.Options.Select(o => o.Name), Does.Not.Contain("--refresh-defaults"));
        Assert.AreEqual("Prints the Fake Game bindings as json and csv.", print.Description);
    }

    [Test]
    public async Task Print_Passes_The_Chosen_Format_And_Flags_To_The_Implementation()
    {
        // Arrange
        var manager = new FakePrintControlManager
        {
            PrintFormats = [new BindingPrintFormat("json", "Prints JSON."), new BindingPrintFormat("csv", "Prints CSV.")],
            PrintFlags = [new BindingPrintFlag("verbose", "Be chatty."), new BindingPrintFlag("compact", "Be brief.")],
        };

        // Act
        var exitCode = await Invoke(manager, "fake", "print", "csv", "--compact");

        // Assert
        Assert.AreEqual(0, exitCode);
        Assert.AreEqual("csv", manager.PrintCalls.Single().Format);
        Assert.AreEqual(new[] { "compact" }, manager.PrintCalls.Single().PrintOptions.Flags);
    }

    [Test]
    public async Task Print_Rejects_A_Format_The_Implementation_Does_Not_Offer()
    {
        // Arrange
        var manager = new FakePrintControlManager { PrintFormats = [new BindingPrintFormat("json", "Prints JSON.")] };

        // Act
        var exitCode = await Invoke(manager, "fake", "print", "markdown");

        // Assert
        Assert.AreNotEqual(0, exitCode);
        Assert.IsEmpty(manager.PrintCalls);
    }

    [TestCase("--refresh-defaults")]
    public async Task Print_Rejects_A_Flag_The_Implementation_Does_Not_Offer(string flag)
    {
        // Arrange
        var manager = new FakePrintControlManager { PrintFlags = [] };

        // Act
        var exitCode = await Invoke(manager, "fake", "print", flag);

        // Assert
        Assert.AreNotEqual(0, exitCode);
        Assert.IsEmpty(manager.PrintCalls);
    }

    [TestCase(1, "Prints the Fake Game bindings as a.")]
    [TestCase(2, "Prints the Fake Game bindings as a and b.")]
    [TestCase(3, "Prints the Fake Game bindings as a, b, and c.")]
    public void Print_Description_Lists_The_Available_Formats(int count, string expected)
    {
        // Arrange
        var manager = new FakePrintControlManager
        {
            PrintFormats = new[] { "a", "b", "c" }.Take(count).Select(n => new BindingPrintFormat(n, n)).ToList(),
        };

        // Act & Assert
        Assert.AreEqual(expected, FindPrintCommand(manager).Description);
    }

    [Test]
    public void Print_Has_No_Subcommands_Or_Options_When_The_Implementation_Offers_None()
    {
        // Arrange
        var manager = new FakePrintControlManager { PrintFormats = [], PrintFlags = [] };

        // Act
        var print = FindPrintCommand(manager);

        // Assert
        Assert.IsEmpty(print.Subcommands);
        Assert.That(print.Options.Select(o => o.Name), Does.Not.Contain("--refresh-defaults"));
    }

    [TestCase("--environment", "PTU")]
    [TestCase("-e", "EPTU")]
    public async Task Print_Passes_Global_Options_To_Generator(string option, string value)
    {
        // Arrange
        var manager = new FakePrintControlManager();

        // Act
        var exitCode = await Invoke(manager, "fake", option, value, "print", "tsv");

        // Assert
        Assert.AreEqual(0, exitCode);
        Assert.AreEqual(value, manager.PrintCalls.Single().Options["environment"]);
    }

    [Test]
    public void Print_AlwaysOffersOutputDir()
    {
        // Arrange
        var manager = new FakePrintControlManager { PrintFormats = [], PrintFlags = [] };

        // Act
        var print = FindPrintCommand(manager);

        // Assert
        Assert.That(print.Options.Select(o => o.Name), Does.Contain("--outputDir"));
    }

    [TestCase(new[] { "fake", "print", "--outputDir", @"C:\out" }, null, @"C:\out")]
    [TestCase(new[] { "fake", "print", "--outputDir", "relative/out", "markdown" }, "markdown", "relative/out")]
    [TestCase(new[] { "fake", "print", "html", "--outputDir", "after" }, "html", "after")]
    public async Task Print_Passes_OutputDir_To_The_Implementation(string[] args, string? expectedFormat, string expectedOutputDir)
    {
        // Arrange
        var manager = new FakePrintControlManager();

        // Act
        var exitCode = await Invoke(manager, args);

        // Assert
        Assert.AreEqual(0, exitCode);
        Assert.AreEqual(expectedFormat, manager.PrintCalls.Single().Format);
        Assert.AreEqual(expectedOutputDir, manager.PrintCalls.Single().PrintOptions.OutputDir);
    }

    [Test]
    public async Task Print_Leaves_OutputDir_Unset_When_The_Option_Is_Omitted()
    {
        // Arrange
        var manager = new FakePrintControlManager();

        // Act
        var exitCode = await Invoke(manager, "fake", "print");

        // Assert
        Assert.AreEqual(0, exitCode);
        Assert.IsNull(manager.PrintCalls.Single().PrintOptions.OutputDir);
    }

    [TestCase("print")]
    [TestCase("print", "html")]
    [TestCase("print", "--refresh-defaults")]
    public async Task Print_Shows_Friendly_Error_And_Fails_When_The_Generator_Reports_One(params string[] printArgs)
    {
        // Arrange
        var manager = new FakePrintControlManager { Exception = new SscmException("Star Citizen environment [PTU] was not found.") };
        var originalError = Console.Error;
        var error = new StringWriter();
        Console.SetError(error);

        // Act
        int exitCode;
        try
        {
            exitCode = await Invoke(manager, new[] { "fake" }.Concat(printArgs).ToArray());
        }
        finally
        {
            Console.SetError(originalError);
        }

        // Assert
        Assert.AreEqual(1, exitCode);
        Assert.AreEqual("Error: Star Citizen environment [PTU] was not found." + Environment.NewLine, error.ToString());
    }

    [Test]
    public async Task Print_Leaves_Unexpected_Errors_To_The_Default_Handler()
    {
        // Arrange
        var manager = new FakePrintControlManager { Exception = new InvalidOperationException("boom") };
        var originalError = Console.Error;
        var error = new StringWriter();
        Console.SetError(error);

        // Act
        int exitCode;
        try
        {
            exitCode = await Invoke(manager, "fake", "print");
        }
        finally
        {
            Console.SetError(originalError);
        }

        // Assert
        Assert.AreNotEqual(0, exitCode);
        Assert.That(error.ToString(), Does.Not.Contain("Error: boom"), "only SscmException gets the friendly message");
    }

    [Test]
    public async Task Print_Command_Is_Only_Registered_For_Managers_That_Can_Print()
    {
        // Arrange
        var manager = new FakeControlManager();

        // Act
        var exitCode = await Invoke(manager, "fake", "print");

        // Assert
        Assert.AreNotEqual(0, exitCode);
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

    private static Command FindPrintCommand(FakeControlManager manager) =>
        Program.BuildRootCommand([manager]).Subcommands.Single(c => c.Name == "fake").Subcommands.Single(c => c.Name == "print");

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

    private class FakePrintControlManager : FakeControlManager, IBindingDocsGenerator
    {
        public List<PrintCall> PrintCalls { get; } = new();
        public Exception? Exception { get; init; }

        // Defaults mimic Star Citizen; tests override them to show the CLI follows the implementation.
        public IReadOnlyList<BindingPrintFormat> PrintFormats { get; init; } =
        [
            new BindingPrintFormat("markdown", "Prints markdown."),
            new BindingPrintFormat("tsv", "Prints TSV."),
            new BindingPrintFormat("html", "Prints HTML."),
        ];

        public IReadOnlyList<BindingPrintFlag> PrintFlags { get; init; } =
        [
            new BindingPrintFlag("refresh-defaults", "Refreshes the defaults."),
        ];

        public Task<BindingPrintResult> Print(string? format, BindingPrintOptions printOptions, Dictionary<string, string>? options = null, CancellationToken cancellationToken = default)
        {
            if (this.Exception != null) throw this.Exception;
            this.PrintCalls.Add(new PrintCall(format, printOptions, options ?? new Dictionary<string, string>()));
            return Task.FromResult(new BindingPrintResult());
        }
    }

    private record PrintCall(string? Format, BindingPrintOptions PrintOptions, Dictionary<string, string> Options);
    private record ImportCall(ImportMode Mode, Dictionary<string, string> Options, bool SelectorProvided);
    private record ExportCall(ExportMode Mode, ExportOptions ExportOptions, Dictionary<string, string> Options, bool SelectorProvided);
    private record UpgradeCall(UpgradeMode Mode, Dictionary<string, string> Options);
    private record ReportCall(ReportingOptions ReportingOptions, Dictionary<string, string> Options);
}
