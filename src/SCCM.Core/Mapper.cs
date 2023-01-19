namespace SCCM.Core;

public class Mapper
{
    public event Action<string> StandardOutput = delegate {};
    public event Action<string> DebugOutput = delegate {};

    public string ReadLocation { get; set; } = string.Empty;
    public string SaveLocation { get; set; } = string.Empty;

    public Mapper()
    {
        Initialize();
    }

    private void Initialize()
    {
        var scpath1 = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"Roberts Space Industries\StarCitizen\LIVE\USER\Client\0\Profiles\default");
        var scpath2 = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), @"Roberts Space Industries\StarCitizen\LIVE\USER\Client\0\Profiles\default");
        if (System.IO.Directory.Exists(scpath1))
        {
            this.ReadLocation = scpath1;
        }
        else
        {
            if (!System.IO.Directory.Exists(scpath2))
            {
                throw new DirectoryNotFoundException($"Could not find the Star Citizen directory in [{scpath1}] or [{scpath2}]!");
            }
            this.ReadLocation = scpath2;
        }

        this.SaveLocation = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "SCCM.json");
    }

    private string GetActionMapsXmlPath()
    {
        var actionmapsxml = System.IO.Path.Combine(this.ReadLocation, "actionmaps.xml");
        if (!System.IO.File.Exists(actionmapsxml))
        {
            throw new FileNotFoundException($"Could not find the Star Citizen mappings file at [{actionmapsxml}]!");
        }
        return actionmapsxml;
    }

    public async Task Read()
    {
        // check for SC mapping file
        var actionmapsxml = GetActionMapsXmlPath();

        // TODO read-in XML file
        var reader = new Reader(actionmapsxml);
        var mappings = await reader.Read();
        // TODO save joystick instance data
        // TODO save actionmap-action-rebind data in Mapping class
    }

    public async Task Write()
    {
        // TODO check for SC mapping file
        var actionmapsxml = GetActionMapsXmlPath();
        // TODO back it up
        // TODO load local copy
        // TODO iterate over Mappings and apply to XML
        // TODO svae
    }
}