namespace SCCM.Core;

public class Mapper
{
    public event Action<string> StandardOutput = delegate {};
    public event Action<string> WarningOutput = delegate {};
    public event Action<string> DebugOutput = delegate {};

    public string ReadLocation { get; set; } = string.Empty;
    public string SaveLocation { get; set; } = string.Empty;

    private readonly IPlatform _platform;

    private MappingData? _data = null;

    public Mapper(IPlatform platform)
    {
        this._platform = platform;
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

        this.SaveLocation = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments), "SCCM");
    }

    private string GetStarCitizenActionmapsXmlPath()
    {
        return System.IO.Path.Combine(this.ReadLocation, "actionmaps.xml");
    }

    private string GetSccmMappingsJsonPath()
    {
        return System.IO.Path.Combine(this.SaveLocation, "mappings.json");
    }

    public async Task ImportAndSave()
    {
        await this.Import();
        await this.Save();
    }

    public async Task Import()
    {
        var actionmapsxml = GetStarCitizenActionmapsXmlPath();
        // read-in XML file
        var reader = new MappingImporter(this._platform, actionmapsxml);
        reader.StandardOutput += this.StandardOutput;
        reader.WarningOutput += this.WarningOutput;
        reader.DebugOutput += this.DebugOutput;
        this._data = await reader.Read();
    }

    public async Task Save()
    {
        if (this._data == null)
        {
            throw new ArgumentNullException(nameof(_data));
        }

        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(this.SaveLocation));
        var serializer = new DataSerializer(this.SaveLocation);
        await serializer.Write(this._data);
        this.StandardOutput($"Mappings backed up to [{this.SaveLocation}].");
    }

    public async Task LoadAndRestore()
    {
        await Load();
        await Restore();
    }

    public async Task Load()
    {
        var serializer = new DataSerializer(this.GetSccmMappingsJsonPath());
        this._data = await serializer.Read();
    }

    public async Task Restore()
    {
        if (this._data == null)
        {
            throw new ArgumentNullException(nameof(_data));
        }

        var actionmapsxml = GetStarCitizenActionmapsXmlPath();
        var updater = new MappingUpdater(this._platform, actionmapsxml);
        updater.StandardOutput += this.StandardOutput;
        updater.WarningOutput += this.WarningOutput;
        updater.DebugOutput += this.DebugOutput;
        await updater.Backup();
        await updater.Update(this._data);
        this.StandardOutput($"Mappings restored to [{actionmapsxml}].");
    }
}