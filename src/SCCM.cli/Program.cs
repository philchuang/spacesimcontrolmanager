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
        root.AddCommand(BuildReadCommand(mapper, debugOption));
        root.AddCommand(BuildEditCommand(mapper));
        root.AddCommand(BuildUpdateCommand(mapper, debugOption));
        root.AddCommand(BuildBackupCommand(mapper, debugOption));
        root.AddCommand(BuildRestoreCommand(mapper, debugOption));
        return root;
    }

    private static Command BuildReadCommand(SCCM.Core.Mapper mapper, Option<bool> debugOption)
    {
        var cmd = new Command("read", "Reads the current Star Citizen control mappings and saves it locally.");
        cmd.Add(debugOption);
        cmd.SetHandler(async (debug) => {
            if (debug) ShowDebugOutput = true;
            await mapper.ImportAndSave();
        },
        debugOption);
        return cmd;
    }

    private static Command BuildEditCommand(SCCM.Core.Mapper mapper)
    {
        var cmd = new Command("edit", "Opens the imported Star Citizen control mappings JSON file.");
        cmd.SetHandler(async () => {
            await mapper.Open();
        });
        return cmd;
    }

    private static Command BuildUpdateCommand(SCCM.Core.Mapper mapper, Option<bool> debugOption)
    {
        var cmd = new Command("update", "Updates the Star Citizen control mappings from the local copy.");
        cmd.Add(debugOption);
        cmd.SetHandler(async (debug) => {
            if (debug) ShowDebugOutput = true;
            await mapper.LoadAndUpdate();
        },
        debugOption);
        return cmd;
    }

    private static Command BuildBackupCommand(SCCM.Core.Mapper mapper, Option<bool> debugOption)
    {
        var cmd = new Command("backup", "Backs up the current Star Citizen control mappings.");
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
        var cmd = new Command("restore", "Restores the Star Citizen control mappings from the last backup.");
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