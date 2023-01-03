using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.CommandLine;
using SCCM.Core;
using SCCM.Core.SC;

namespace SCCM.cli;

class Program
{
    private static bool ShowDebugOutput = false;

    static async Task<int> Main(string[] args)
    {
        var host = CreateDefaultBuilder().Build();

        var manager = CreateManager(host);
        var root = BuildRootCommand(manager);
        return await root.InvokeAsync(args);
    }

    static IHostBuilder CreateDefaultBuilder()
    {
        IConfigurationRoot? config = null;
        return Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration(app =>
            {
                app.AddJsonFile("appsettings.json");
                config = app.Build();
            })
            .ConfigureServices(services =>
            {
                services.AddSingleton<IConfiguration>(s => config!);
                services.AddSingleton<IPlatform, Platform>();
                services.AddSingleton<ISCFolders, SCFolders>();
                services.AddTransient<IControlManager>(s => new ControlManager(s.GetService<IPlatform>()!, s.GetService<ISCFolders>()!));
            });
    }

    private static IControlManager CreateManager(IHost host)
    {
        var manager = host.Services.GetRequiredService<IControlManager>();
        manager.StandardOutput += Console.WriteLine;
        manager.WarningOutput += Console.WriteLine;
        manager.DebugOutput += s => { if (ShowDebugOutput) Console.WriteLine(s); };
        return manager;
    }

    private static Command BuildRootCommand(IControlManager manager)
    {
        var debugOption = new Option<bool>(
            aliases: new [] { "--debug", "-d" },
            description: "Display debug output"
        );

        var root = new RootCommand("Star Citizen Control Manager");
        root.AddGlobalOption(debugOption);
        root.AddCommand(BuildImportCommand(manager, debugOption));
        root.AddCommand(BuildEditCommand(manager));
        root.AddCommand(BuildEditSCCommand(manager));
        root.AddCommand(BuildExportCommand(manager, debugOption));
        root.AddCommand(BuildBackupCommand(manager, debugOption));
        root.AddCommand(BuildRestoreCommand(manager, debugOption));
        return root;
    }

    private static Command BuildImportCommand(IControlManager manager, Option<bool> debugOption)
    {
        var cmd = new Command("import", "Imports the Star Citizen actionmaps.xml and saves it locally in a mappings JSON file.");
        cmd.Add(debugOption);
        cmd.SetHandler(async (debug) => {
                if (debug) ShowDebugOutput = true;
                await manager.Import(mode: ImportMode.Default);
            },
            debugOption);

        var merge = new Command("merge", "Merges the latest mappings into the saved mappings.");
        merge.SetHandler(async (debug) => {
                if (debug) ShowDebugOutput = true;
                await manager.Import(mode: ImportMode.Merge);
            },
            debugOption);
        cmd.AddCommand(merge);

        var overwrite = new Command("overwrite", "Overwrites the saved mappings with the latest mappings.");
        overwrite.SetHandler(async (debug) => {
                if (debug) ShowDebugOutput = true;
                await manager.Import(mode: ImportMode.Overwrite);
            },
            debugOption);
        cmd.AddCommand(overwrite);

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

    private static Command BuildEditSCCommand(IControlManager manager)
    {
        var cmd = new Command("editsc", "Opens the Star Citizen actionmaps.xml in the system default editor.");
        cmd.AddAlias("opensc");
        cmd.SetHandler(() => {
            manager.OpenGameConfig();
        });
        return cmd;
    }

    private static Command BuildExportCommand(IControlManager manager, Option<bool> debugOption)
    {
        var cmd = new Command("export", "Previews updates to the Star Citizen bindings based on the locally saved mappings file.");
        cmd.Add(debugOption);
        cmd.SetHandler(async (debug) => {
                if (debug) ShowDebugOutput = true;
                await manager.ExportPreview();
            },
            debugOption);

        var apply = new Command("apply", "Updates the Star Citizen bindings based on the locally saved mappings file.");
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
        var cmd = new Command("backup", "Makes a local copy of the Star Citizen actionmaps.xml which can be restored later.");
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
        var cmd = new Command("restore", "Restores the latest local backup of the Star Citizen actionmaps.xml.");
        cmd.SetHandler((debug) => {
            if (debug) ShowDebugOutput = true;
            manager.Restore();
        },
        debugOption);
        return cmd;
    }
}