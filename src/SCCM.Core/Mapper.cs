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
        this.ReadLocation = System.IO.Path.Combine(this._platform.ProgramFilesDir, @"Roberts Space Industries\StarCitizen\LIVE\USER\Client\0\Profiles\default");
        if (!System.IO.Directory.Exists(this.ReadLocation))
        {
            throw new DirectoryNotFoundException($"Could not find the Star Citizen directory at [{this.ReadLocation}]!");
        }

        this.SaveLocation = this._platform.SccmDir;
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

        System.IO.Directory.CreateDirectory(this.SaveLocation);
        var serializer = new DataSerializer(this.GetSccmMappingsJsonPath());
        await serializer.Write(this._data);
        this.StandardOutput($"Mappings backed up to [{this.GetSccmMappingsJsonPath()}].");
    }

    public async Task LoadAndUpdate()
    {
        await Load();
        await Update();
    }

    public async Task Load()
    {
        var serializer = new DataSerializer(this.GetSccmMappingsJsonPath());
        this._data = await serializer.Read();
    }

    public async Task Update()
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
        updater.Backup();
        await updater.Update(this._data);
        this.StandardOutput($"Mappings restored to [{actionmapsxml}].");
    }

    public async Task Backup()
    {
        // TODO write test
        var actionmapsxml = GetStarCitizenActionmapsXmlPath();
        var updater = new MappingUpdater(this._platform, actionmapsxml);
        updater.StandardOutput += this.StandardOutput;
        updater.WarningOutput += this.WarningOutput;
        updater.DebugOutput += this.DebugOutput;
        var backup = updater.Backup();
        this.StandardOutput($"Actionmaps.xml backed up to [{backup}].");
    }

    public async Task Restore()
    {
        // TODO write test
        // TODO implement
    }
}