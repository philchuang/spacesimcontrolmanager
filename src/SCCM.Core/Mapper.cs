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

        // TODO load current mappings.json
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

        var updated = await reader.Read();
        if (this._data != null)
        {
            PreviewMerge(this._data, updated);
            // TODO merge
        }
        else
        {
            this._data = updated;
        }
    }

    private void PreviewMerge(MappingData previous, MappingData updated)
    {
        // capture differences
        var inputDiffs = ComparisonHelper.Compare(
            previous.Inputs, updated.Inputs,
            i => $"{i.Type}-{i.Product}",
            (p, c) => p.Instance != c.Instance ||
                ComparisonHelper.DictionariesAreDifferent(
                    p.Settings.ToDictionary(s => s.Name), 
                    c.Settings.ToDictionary(s => s.Name))
            );
        var mappingDiffs = ComparisonHelper.Compare(
            previous.Mappings, updated.Mappings,
            m => $"{m.ActionMap}-{m.Action}",
            (p, c) => p.Input != c.Input || p.MultiTap != c.MultiTap);
        // TODO report differences
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
        var updater = CreateUpdater();
        var backup = updater.RestoreLatest();
        this.StandardOutput($"actionmaps.xml restored from [{backup}].");
    }

    public async Task Open()
    {
        // TODO write test
        this._platform.Open(this.GetSccmMappingsJsonPath());
        this.StandardOutput($"Opening [{this.GetSccmMappingsJsonPath()}] in the default editor, change the Preserve property to choose which settings are overwritten.");
    }
}