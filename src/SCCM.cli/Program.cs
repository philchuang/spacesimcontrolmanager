using System;
using System.CommandLine;
using SCCM.Core;

namespace SCCM.cli;

class Program
{
    private static Command BuildRootCommand(SCCM.Core.Mapper mapper)
    {
        var root = new RootCommand("Star Citizen Control Mapper Tool");
        root.AddCommand(BuildReadCommand(mapper));
        root.AddCommand(BuildWriteCommand(mapper));
        return root;
    }

    private static Command BuildReadCommand(SCCM.Core.Mapper mapper)
    {
        var cmd = new Command("copy", "Reads in the current Star Citizen control mappings and saves it locally.");
        cmd.SetHandler(async () => {
            await mapper.Read();
        });
        return cmd;
    }

    private static Command BuildWriteCommand(SCCM.Core.Mapper mapper)
    {
        var cmd = new Command("paste", "Updates the Star Citizen control mappings from the local copy.");
        cmd.SetHandler(async () => {
            await mapper.Write();
        });
        return cmd;
    }

    static async Task<int> Main(string[] args)
    {
        var mapper = new Mapper();
        var root = BuildRootCommand(mapper);
        return await root.InvokeAsync(args);
    }
}