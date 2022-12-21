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
        root.AddCommand(BuildWriteCommand(mapper, debugOption));
        return root;
    }

    private static Command BuildReadCommand(SCCM.Core.Mapper mapper, Option<bool> debugOption)
    {
        var cmd = new Command("copy", "Reads in the current Star Citizen control mappings and saves it locally.");
        cmd.Add(debugOption);
        cmd.SetHandler(async (debug) => {
            if (debug) ShowDebugOutput = true;
            await mapper.ReadAndSave();
        },
        debugOption);
        return cmd;
    }

    private static Command BuildWriteCommand(SCCM.Core.Mapper mapper, Option<bool> debugOption)
    {
        var cmd = new Command("paste", "Updates the Star Citizen control mappings from the local copy.");
        cmd.SetHandler(async (debug) => {
            await mapper.Restore();
        });
        return cmd;
    }

    private static Mapper CreateMapper()
    {
        var mapper = new Mapper();
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