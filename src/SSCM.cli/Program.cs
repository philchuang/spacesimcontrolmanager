using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.CommandLine;
using SSCM.Core;

namespace SSCM.cli;

internal class Program
{
    private static bool ShowDebugOutput = false;

    static async Task<int> Main(string[] args)
    {
        var host = CreateDefaultBuilder().Build();

        var managers = CreateManagers(host);
        var root = BuildRootCommand(managers);
        return await root.Parse(args).InvokeAsync(new InvocationConfiguration());
    }

    // TODO working on a config subcommand where the user can set variables (dirs, etc.)

    static IHostBuilder CreateDefaultBuilder()
    {
        IConfigurationRoot? config = null;
        return Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration(app =>
            {
                app.AddJsonFile("appsettings.json", true);
                app.AddJsonFile("appsettings.local.json", true);
                config = app.Build();
            })
            .ConfigureServices(services =>
            {
                services.AddSingleton<IConfiguration>(s => config!);
                services.AddSingleton<IPlatform, Platform>();
                services.AddSingleton<IUserInput, CliUserInput>();
                services.AddSingleton<ISscmFolders, SscmFolders>();
                services.AddSingleton<SSCM.StarCitizen.ISCFolders, SSCM.StarCitizen.SCFolders>();
                services.AddSingleton<SSCM.Elite.IEDFolders, SSCM.Elite.EDFolders>();
                // TODO adapt to read this in dynamically based on DLLs
                services.AddTransient<IControlManager, SSCM.StarCitizen.SCControlManager>();
                services.AddTransient<IControlManager, SSCM.Elite.EDControlManager>();
            });
    }

    private static List<IControlManager> CreateManagers(IHost host)
    {
        var managers = host.Services.GetServices<IControlManager>().ToList();
        
        foreach (var manager in managers)
        {
            manager.StandardOutput += Console.WriteLine;
            manager.WarningOutput += s => Console.WriteLine($"[WARN ] {s}");
            manager.DebugOutput += s => { if (ShowDebugOutput) Console.WriteLine($"[DEBUG] {s}"); };
        }

        return managers;
    }

    internal static Command BuildRootCommand(List<IControlManager> managers)
    {
        var debugOption = new Option<bool>("--debug", "-d")
        {
            Description = "Display debug output",
            Recursive = true
        };

        var root = new RootCommand("Space Sim Control Manager");
        root.Add(debugOption);
        managers.ForEach(m => AddCommands(m, root, debugOption));
        return root;
    }

    private static void AddCommands(IControlManager manager, RootCommand root, Option<bool> debugOption)
    {
        var mgr = new Command(manager.CommandAlias, $"Manage {manager.GameType} mappings");
        var globalOptions = AddGlobalOptions(manager, mgr);

        foreach (var command in new[] {
            BuildImportCommand(manager, debugOption, globalOptions),
            BuildUpgradeCommand(manager, debugOption, globalOptions),
            BuildReportCommand(manager, globalOptions),
            BuildEditCommand(manager, globalOptions),
            BuildEditGameCommand(manager, globalOptions),
            BuildExportCommand(manager, debugOption, globalOptions),
            BuildBackupCommand(manager, debugOption, globalOptions),
            BuildRestoreCommand(manager, debugOption, globalOptions)})
        {
            mgr.Add(command);
        }

        // mgr.AddCommand(BuildConfigCommand(manager));
        root.Add(mgr);
    }

    private static IList<Option<string>> AddGlobalOptions(IControlManager manager, Command command)
    {
        var options = new List<Option<string>> ();
        foreach (var commandOption in manager.GlobalOptions)
        {
            var aliases = string.IsNullOrWhiteSpace(commandOption.ShortName) ? [] : new[] { $"-{commandOption.ShortName.ToLowerInvariant()}" };
            var cliOption = new Option<string>($"--{commandOption.Name.ToLowerInvariant()}", aliases)
            {
                Description = commandOption.Description,
                DefaultValueFactory = _ => commandOption.DefaultValue ?? string.Empty,
                Recursive = true
            };
            command.Add(cliOption);
            options.Add(cliOption);
        }

        return options;
    }

    private static Dictionary<string, string> PrepareOptions(ParseResult parseResult, Option<bool>? debugOption, IList<Option<string>> globalOptions)
    {
        var options = new Dictionary<string, string>();
        foreach (var cliOption in globalOptions)
        {
            var value = parseResult.GetValue(cliOption);
            if (value != null) options[cliOption.Name.TrimStart('-')] = value;
        }

        var debug = debugOption != null && parseResult.GetValue(debugOption);
        if (debug) ShowDebugOutput = true;
        return options;
    }

    private static Command BuildImportCommand(IControlManager manager, Option<bool> debugOption, IList<Option<string>> globalOptions)
    {
        Task Import(ParseResult parseResult, ImportMode mode, bool useTuiSelector = false)
        {
            var options = PrepareOptions(parseResult, debugOption, globalOptions);
            var selector = useTuiSelector ? new SpectreInteractiveChangeSelector(() => manager.GameTypeTitle) : null;
            return manager.Import(mode, options, selector);
        }

        Command CreateImportModeCommand(string name, string description, ImportMode mode, bool useTuiSelector = false)
        {
            var modeCommand = new Command(name, description);
            modeCommand.SetAction(parseResult => Import(parseResult, mode, useTuiSelector));
            return modeCommand;
        }

        var cmd = new Command("import", $"Imports the {manager.GameType} mappings and saves it locally.");
        cmd.SetAction(parseResult => Import(parseResult, ImportMode.Tui, useTuiSelector: true));

        cmd.Add(CreateImportModeCommand("preview", $"Previews the latest {manager.GameType} mappings without saving changes.", ImportMode.Preview));
        cmd.Add(CreateImportModeCommand("merge", $"Merges the latest {manager.GameType} mappings into the saved mappings.", ImportMode.Merge));
        cmd.Add(CreateImportModeCommand("overwrite", $"Overwrites the saved {manager.GameType} mappings with the latest mappings.", ImportMode.Overwrite));
        cmd.Add(CreateImportModeCommand("serial", $"Performs a serial merge of saved {manager.GameType} mappings with the latest mappings.", ImportMode.Serial));
        cmd.Add(CreateImportModeCommand("tui", $"Selects {manager.GameType} import changes from a terminal UI.", ImportMode.Tui, useTuiSelector: true));

        return cmd;
    }

    private static Command BuildUpgradeCommand(IControlManager manager, Option<bool> debugOption, IList<Option<string>> globalOptions)
    {
        var cmd = new Command("upgrade", $"Upgrades the saved {manager.GameType} mappings.");
        cmd.SetAction(async (parseResult) => {
                var options = PrepareOptions(parseResult, debugOption, globalOptions);
                await manager.Upgrade(mode: UpgradeMode.Preview, options);
            });

        var apply = new Command("apply", $"Upgrades the saved {manager.GameType} mappings.");
        apply.SetAction(async (parseResult) => {
                var options = PrepareOptions(parseResult, debugOption, globalOptions);
                await manager.Upgrade(mode: UpgradeMode.Apply, options);
            });
        cmd.Add(apply);

        return cmd;
    }

    private static Command BuildReportCommand(IControlManager manager, IList<Option<string>> globalOptions)
    {
        var preservedOnlyOption = new Option<bool>("--preserved", "-p")
        {
            Description = "Only output mappings marked for preservation"
        };

        var headersOnlyOption = new Option<bool>("--names", "-n")
        {
            Description = "Only output mapping names, not values"
        };

        var formatOption = new Option<string>("--format", "-f")
        {
            Description = "Output in a specific format",
            DefaultValueFactory = _ => "md"
        };
        formatOption.AcceptOnlyFromAmong("md", "csv", "json");

        var cmd = new Command("report", "Outputs saved mappings in text format.");
        cmd.Add(preservedOnlyOption);
        cmd.Add(headersOnlyOption);
        cmd.Add(formatOption);
        cmd.SetAction(async (parseResult) => {
            var managerOptions = PrepareOptions(parseResult, null, globalOptions);
            var preservedOnly = parseResult.GetValue(preservedOnlyOption);
            var headersOnly = parseResult.GetValue(headersOnlyOption);
            var format = parseResult.GetValue(formatOption) ?? "md";
            var reportingOptions = new ReportingOptions {
                Format = format switch {
                    "md" => ReportingFormat.Markdown,
                    "markdown" => ReportingFormat.Markdown,
                    "csv" => ReportingFormat.Csv,
                    "json" => ReportingFormat.Json,
                    _ => throw new ArgumentOutOfRangeException(format),
                },
                HeadersOnly = headersOnly,
                PreservedOnly = preservedOnly,
            };
            Console.WriteLine(await manager.Report(reportingOptions, managerOptions));
        });

        // TODO re-add, maybe have module-specific CLI configuration logic
        // ONLY FOR SC 
        // var inputsCmd = new Command("inputs", "Outputs input data in CSV format.");
        // inputsCmd.AddOption(preservedOnlyOption);
        // inputsCmd.SetHandler(async(preservedOnly) => {
        //     Console.WriteLine(await manager.ReportInputs(preservedOnly: preservedOnly));
        // },
        // preservedOnlyOption);
        // cmd.AddCommand(inputsCmd);

        // var mappingsCmd = new Command("mappings", "Outputs mappings data in CSV format.");
        // mappingsCmd.AddOption(preservedOnlyOption);
        // mappingsCmd.SetHandler(async(preservedOnly) => {
        //     Console.WriteLine(await manager.ReportMappings(preservedOnly: preservedOnly));
        // },
        // preservedOnlyOption);
        // cmd.AddCommand(mappingsCmd);

        return cmd;
    }

    private static Command BuildEditCommand(IControlManager manager, IList<Option<string>> globalOptions)
    {
        var cmd = new Command("edit", "Opens the mappings JSON file in the system default editor. Edit the \"Preserve\" property to affect the export behavior.");
        cmd.Aliases.Add("open");
        cmd.SetAction((parseResult) => {
            var options = PrepareOptions(parseResult, null, globalOptions);
            manager.Open(options);
        });
        return cmd;
    }

    private static Command BuildEditGameCommand(IControlManager manager, IList<Option<string>> globalOptions)
    {
        var cmd = new Command("editgame", $"Opens the {manager.GameType} mappings file in the system default editor.");
        cmd.Aliases.Add("opengame");
        cmd.SetAction((parseResult) => {
            var options = PrepareOptions(parseResult, null, globalOptions);
            manager.OpenGameConfig(options);
        });
        return cmd;
    }

    private static Command BuildExportCommand(IControlManager manager, Option<bool> debugOption, IList<Option<string>> globalOptions)
    {
        var onlyMatchesOption = new Option<bool>("--matches", "-m")
        {
            Description = "Only export settings that are already mapped.",
            Recursive = true
        };

        Task Export(ParseResult parseResult, ExportMode mode, bool useTuiSelector = false)
        {
            var managerOptions = PrepareOptions(parseResult, debugOption, globalOptions);
            var exportOptions = new ExportOptions
            {
                OnlyMatches = parseResult.GetValue(onlyMatchesOption),
            };
            var selector = useTuiSelector ? new SpectreInteractiveChangeSelector(() => manager.GameTypeTitle) : null;
            return manager.Export(mode, exportOptions, managerOptions, selector);
        }

        Command CreateExportModeCommand(string name, string description, ExportMode mode, bool useTuiSelector = false)
        {
            var modeCommand = new Command(name, description);
            modeCommand.SetAction(parseResult => Export(parseResult, mode, useTuiSelector));
            return modeCommand;
        }

        var cmd = new Command("export", $"Updates {manager.GameType} mappings from the locally saved mappings file using a terminal UI.");
        cmd.Add(onlyMatchesOption);
        cmd.SetAction(parseResult => Export(parseResult, ExportMode.Tui, useTuiSelector: true));

        cmd.Add(CreateExportModeCommand("preview", $"Previews updates to the {manager.GameType} mappings based on the locally saved mappings file.", ExportMode.Preview));
        cmd.Add(CreateExportModeCommand("apply", $"Updates {manager.GameType} mappings based on the locally saved mappings file.", ExportMode.Apply));
        cmd.Add(CreateExportModeCommand("serial", $"Performs a serial update of {manager.GameType} mappings based on the locally saved mappings file.", ExportMode.Serial));
        cmd.Add(CreateExportModeCommand("tui", $"Selects {manager.GameType} export changes from a terminal UI.", ExportMode.Tui, useTuiSelector: true));

        return cmd;
    }

    private static Command BuildBackupCommand(IControlManager manager, Option<bool> debugOption, IList<Option<string>> globalOptions)
    {
        var cmd = new Command("backup", $"Makes a local copy of the {manager.GameType} mappings file which can be restored later.");
        cmd.SetAction((parseResult) => {
            var options = PrepareOptions(parseResult, debugOption, globalOptions);
            manager.Backup(options);
        });
        return cmd;
    }

    private static Command BuildRestoreCommand(IControlManager manager, Option<bool> debugOption, IList<Option<string>> globalOptions)
    {
        var cmd = new Command("restore", $"Restores the latest local backup of the {manager.GameType} mappings file.");
        cmd.SetAction((parseResult) => {
            var options = PrepareOptions(parseResult, debugOption, globalOptions);
            manager.Restore(options);
        });
        return cmd;
    }

    // private static Command BuildConfigCommand(IControlManager manager)
    // {
    //     // TODO implement
    // }
}
