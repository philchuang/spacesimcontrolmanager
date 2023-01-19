namespace SCCM.Core;

public class Mapper
{
    public event Action<string> StandardOutput = delegate {};
    public event Action<string> WarningOutput = delegate {};
    public event Action<string> DebugOutput = delegate {};

    public string ReadLocation { get; set; } = string.Empty;
    public string SaveLocation { get; set; } = string.Empty;

    private MappingData _data = new MappingData();

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

        this.SaveLocation = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments), "SCCM", "mappings.json");
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

    public async Task ReadAndSave()
    {
        await this.Read();
        await this.Save();
    }

    public async Task Read()
    {
        // check for SC mapping file
        var actionmapsxml = GetActionMapsXmlPath();

        // read-in XML file
        var reader = new DataReader(actionmapsxml);
        reader.StandardOutput += this.StandardOutput;
        reader.WarningOutput += this.WarningOutput;
        reader.DebugOutput += this.DebugOutput;
        this._data = await reader.Read();
    }

    public async Task Save()
    {
        System.IO.Directory.CreateDirectory(this.SaveLocation);
        var writer = new DataWriter(this.SaveLocation);
        await writer.Write(this._data);
    }

    public async Task Restore()
    {
        // TODO check for SC mapping file
        var actionmapsxml = GetActionMapsXmlPath();
        // TODO back it up
        // TODO load local copy
        // TODO iterate over Mappings and apply to XML
        // TODO svae
    }
}