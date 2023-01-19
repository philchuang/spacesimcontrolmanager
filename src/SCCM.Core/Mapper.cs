namespace SCCM.Core;

public class Mapper
{
    public event Action<string> StandardOutput = delegate {};
    public event Action<string> WarningOutput = delegate {};
    public event Action<string> DebugOutput = delegate {};

    public string ReadLocation { get; set; } = string.Empty;
    public string SaveLocation { get; set; } = string.Empty;

    private readonly IPlatform _platform;
    private readonly IFolders _folders;

    private MappingData? _data = null;

    public Mapper(IPlatform platform, IFolders folders)
    {
        this._platform = platform;
        this._folders = folders;
        Initialize();
    }

    private void Initialize()
    {
        this.ReadLocation = this._folders.ActionMapsDir;
        if (!System.IO.Directory.Exists(this.ReadLocation))
        {
            throw new DirectoryNotFoundException($"Could not find the Star Citizen directory at [{this.ReadLocation}]!");
        }

        this.SaveLocation = this._folders.SccmDir;
    }

    private MappingUpdater CreateUpdater()
    {
        var updater = new MappingUpdater(this._platform, this._folders, GetStarCitizenActionmapsXmlPath());
        updater.StandardOutput += this.StandardOutput;
        updater.WarningOutput += this.WarningOutput;
        updater.DebugOutput += this.DebugOutput;
        return updater;
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

        var updater = CreateUpdater();
        updater.Backup();
        await updater.Update(this._data);
        this.StandardOutput($"Mappings restored to [{updater.ActionMapsXmlPath}].");
    }

    public async Task Backup()
    {
        var updater = CreateUpdater();
        var backup = updater.Backup();
        this.StandardOutput($"actionmaps.xml backed up to [{backup}].");
    }

    public async Task Restore()
    {
        // TODO write test
        var updater = CreateUpdater();
        var backup = updater.RestoreLatest();
        this.StandardOutput($"actionmaps.xml restored from [{backup}].");
    }
}