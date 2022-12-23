using System;
using System.CommandLine;
using SCCM.Core;

namespace SCCM.cli;

class Program
{
    private static bool ShowDebugOutput = false;

    private static Command BuildRootCommand(SCCM.Core.Mapper mapper)
    {
        var debugOption = new Option<bool>(
            aliases: new [] { "--debug", "-d" },
            description: "Display debug output"
        );

        var root = new RootCommand("Star Citizen Control Mapper Tool");
        root.AddGlobalOption(debugOption);
        root.AddCommand(BuildImportCommand(mapper, debugOption));
        root.AddCommand(BuildEditCommand(mapper));
        root.AddCommand(BuildExportCommand(mapper, debugOption));
        root.AddCommand(BuildBackupCommand(mapper, debugOption));
        root.AddCommand(BuildRestoreCommand(mapper, debugOption));
        return root;
    }

    private static Command BuildImportCommand(SCCM.Core.Mapper mapper, Option<bool> debugOption)
    {
        var cmd = new Command("import", "Imports the Star Citizen actionmaps.xml and saves it locally in a mappings JSON file.");
        cmd.Add(debugOption);
        cmd.SetHandler(async (debug) => {
                if (debug) ShowDebugOutput = true;
                await mapper.Import(mode: ImportMode.Default);
            },
            debugOption);

        var merge = new Command("merge", "Merges the latest mappings into the saved mappings.");
        merge.SetHandler(async (debug) => {
                if (debug) ShowDebugOutput = true;
                await mapper.Import(mode: ImportMode.Merge);
            },
            debugOption);
        cmd.AddCommand(merge);

        var overwrite = new Command("overwrite", "Overwrites the saved mappings with the latest mappings.");
        overwrite.SetHandler(async (debug) => {
                if (debug) ShowDebugOutput = true;
                await mapper.Import(mode: ImportMode.Overwrite);
            },
            debugOption);
        cmd.AddCommand(overwrite);

        return cmd;
    }

    private static Command BuildEditCommand(SCCM.Core.Mapper mapper)
    {
        var cmd = new Command("edit", "Opens the mappings JSON file in the system default editor. Edit the \"Preserve\" property to affect the export behavior.");
        cmd.SetHandler(async () => {
            await mapper.Open();
        });
        return cmd;
    }

    private static Command BuildExportCommand(SCCM.Core.Mapper mapper, Option<bool> debugOption)
    {
        var cmd = new Command("export", "Updates the Star Citizen bindings based on the locally saved mappings file.");
        cmd.Add(debugOption);
        cmd.SetHandler(async (debug) => {
            if (debug) ShowDebugOutput = true;
            await mapper.Export();
        },
        debugOption);
        return cmd;
    }

    private static Command BuildBackupCommand(SCCM.Core.Mapper mapper, Option<bool> debugOption)
    {
        var cmd = new Command("backup", "Makes a local copy of the Star Citizen actionmaps.xml which can be restored later.");
        cmd.Add(debugOption);
        cmd.SetHandler(async (debug) => {
            if (debug) ShowDebugOutput = true;
            await mapper.Backup();
        },
        debugOption);
        return cmd;
    }

    private static Command BuildRestoreCommand(SCCM.Core.Mapper mapper, Option<bool> debugOption)
    {
        var cmd = new Command("restore", "Restores the latest local backup of the Star Citizen actionmaps.xml.");
        cmd.SetHandler(async (debug) => {
            if (debug) ShowDebugOutput = true;
            await mapper.Restore();
        },
        debugOption);
        return cmd;
    }

    private static Mapper CreateMapper()
    {
        var platform = new Platform();
        var folders = new Folders(platform);
        var mapper = new Mapper(platform, folders);
        mapper.StandardOutput += Console.WriteLine;
        mapper.WarningOutput += Console.WriteLine;
        mapper.DebugOutput += s => { if (ShowDebugOutput) Console.WriteLine(s); };
        return mapper;
    }

    static async Task<int> Main(string[] args)
    {
        var mapper = CreateMapper();
        var root = BuildRootCommand(mapper);
        return await root.InvokeAsync(args);
    }
}