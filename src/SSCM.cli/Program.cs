﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.CommandLine;
using SSCM.Core;

namespace SSCM.cli;

class Program
{
    private static bool ShowDebugOutput = false;

    static async Task<int> Main(string[] args)
    {
        var host = CreateDefaultBuilder().Build();

        var managers = CreateManagers(host);
        var root = BuildRootCommand(managers);
        return await root.InvokeAsync(args);
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

    private static Command BuildRootCommand(List<IControlManager> managers)
    {
        var debugOption = new Option<bool>(
            aliases: new [] { "--debug", "-d" },
            description: "Display debug output"
        );

        var root = new RootCommand("Space Sim Control Manager");
        root.Name = "sscm";
        root.AddGlobalOption(debugOption);
        managers.ForEach(m => AddCommands(m, root, debugOption));
        return root;
    }

    private static void AddCommands(IControlManager manager, RootCommand root, Option<bool> debugOption)
    {
        var mgr = new Command(manager.CommandAlias, $"Manage {manager.GameType} mappings");
        mgr.AddCommand(BuildImportCommand(manager, debugOption));
        mgr.AddCommand(BuildReportCommand(manager));
        mgr.AddCommand(BuildEditCommand(manager));
        mgr.AddCommand(BuildEditGameCommand(manager));
        mgr.AddCommand(BuildExportCommand(manager, debugOption));
        mgr.AddCommand(BuildBackupCommand(manager, debugOption));
        mgr.AddCommand(BuildRestoreCommand(manager, debugOption));
        // mgr.AddCommand(BuildConfigCommand(manager));
        root.AddCommand(mgr);
    }

    private static Command BuildImportCommand(IControlManager manager, Option<bool> debugOption)
    {
        var cmd = new Command("import", $"Imports the {manager.GameType} mappings and saves it locally.");
        cmd.Add(debugOption);
        cmd.SetHandler(async (debug) => {
                if (debug) ShowDebugOutput = true;
                await manager.Import(mode: ImportMode.Default);
            },
            debugOption);

        var merge = new Command("merge", $"Merges the latest {manager.GameType} mappings into the saved mappings.");
        merge.SetHandler(async (debug) => {
                if (debug) ShowDebugOutput = true;
                await manager.Import(mode: ImportMode.Merge);
            },
            debugOption);
        cmd.AddCommand(merge);

        var overwrite = new Command("overwrite", $"Overwrites the saved {manager.GameType} mappings with the latest mappings.");
        overwrite.SetHandler(async (debug) => {
                if (debug) ShowDebugOutput = true;
                await manager.Import(mode: ImportMode.Overwrite);
            },
            debugOption);
        cmd.AddCommand(overwrite);

        var interactive = new Command("interactive", $"Performs an interactive merge of saved {manager.GameType} mappings with the latest mappings.");
        interactive.SetHandler(async (debug) => {
                if (debug) ShowDebugOutput = true;
                await manager.Import(mode: ImportMode.Interactive);
            },
            debugOption);
        cmd.AddCommand(interactive);

        return cmd;
    }

    private static Command BuildReportCommand(IControlManager manager)
    {
        var preservedOnlyOption = new Option<bool>(
            aliases: new [] { "--preserved", "-p" },
            description: "Only output mappings marked for preservation"
        );

        var headersOnlyOption = new Option<bool>(
            aliases: new [] { "--names", "-n" },
            description: "Only output mapping names, not values"
        );

        var formatOption = new Option<string>(
            aliases: new [] { "--format", "-f" },
            description: "Output in a specific format",
            getDefaultValue: () => "md"
        ).FromAmong("md", "csv", "json");

        var cmd = new Command("report", "Outputs saved mappings in text format.");
        cmd.AddOption(preservedOnlyOption);
        cmd.AddOption(headersOnlyOption);
        cmd.AddOption(formatOption);
        cmd.SetHandler(async (preservedOnly, headersOnly, format) => {
            var options = new ReportingOptions {
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
            Console.WriteLine(await manager.Report(options));
        },
        preservedOnlyOption, headersOnlyOption, formatOption);

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

    private static Command BuildEditCommand(IControlManager manager)
    {
        var cmd = new Command("edit", "Opens the mappings JSON file in the system default editor. Edit the \"Preserve\" property to affect the export behavior.");
        cmd.AddAlias("open");
        cmd.SetHandler(() => {
            manager.Open();
        });
        return cmd;
    }

    private static Command BuildEditGameCommand(IControlManager manager)
    {
        var cmd = new Command("editgame", $"Opens the {manager.GameType} mappings file in the system default editor.");
        cmd.AddAlias("opengame");
        cmd.SetHandler(() => {
            manager.OpenGameConfig();
        });
        return cmd;
    }

    private static Command BuildExportCommand(IControlManager manager, Option<bool> debugOption)
    {
        var cmd = new Command("export", $"Previews updates to the {manager.GameType} mappings based on the locally saved mappings file.");
        cmd.Add(debugOption);
        cmd.SetHandler(async (debug) => {
                if (debug) ShowDebugOutput = true;
                await manager.ExportPreview();
            },
            debugOption);

        var apply = new Command("apply", $"Updates {manager.GameType} mappings based on the locally saved mappings file.");
        apply.SetHandler(async (debug) => {
                if (debug) ShowDebugOutput = true;
                await manager.ExportApply();
            },
            debugOption);
        cmd.AddCommand(apply);

        return cmd;
    }

    private static Command BuildBackupCommand(IControlManager manager, Option<bool> debugOption)
    {
        var cmd = new Command("backup", $"Makes a local copy of the {manager.GameType} mappings file which can be restored later.");
        cmd.Add(debugOption);
        cmd.SetHandler((debug) => {
            if (debug) ShowDebugOutput = true;
            manager.Backup();
        },
        debugOption);
        return cmd;
    }

    private static Command BuildRestoreCommand(IControlManager manager, Option<bool> debugOption)
    {
        var cmd = new Command("restore", $"Restores the latest local backup of the {manager.GameType} mappings file.");
        cmd.SetHandler((debug) => {
            if (debug) ShowDebugOutput = true;
            manager.Restore();
        },
        debugOption);
        return cmd;
    }

    // private static Command BuildConfigCommand(IControlManager manager)
    // {
    //     // TODO implement
    // }
}