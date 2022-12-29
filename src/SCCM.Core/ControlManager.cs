namespace SCCM.Core;

// TODO write tests for this class

public interface IControlManager
{
    event Action<string> StandardOutput;
    event Action<string> WarningOutput;
    event Action<string> DebugOutput;

    string GameType { get; }
    string ReadLocation { get; set; }
    string SaveLocation { get; set; }

    Task Import(ImportMode mode);
    Task ExportPreview();
    Task ExportApply();
    void Backup();
    void Restore();
    void Open();
    void OpenGameConfig();
}

public abstract class ControlManagerBase : IControlManager
{
    public event Action<string> StandardOutput = delegate {};
    protected void WriteLineStandard(string s) => this.StandardOutput(s);
    public event Action<string> WarningOutput = delegate {};
    protected void WriteLineWarning(string s) => this.WarningOutput(s);
    public event Action<string> DebugOutput = delegate {};
    protected void WriteLineDebug(string s) => this.DebugOutput(s);

    public abstract string GameType { get; }
    public string ReadLocation { get; set; } = string.Empty;
    public string SaveLocation { get; set; } = string.Empty;

    protected abstract IMappingImporter CreateImporter();
    protected abstract IMappingImportMerger CreateMerger();
    protected abstract IMappingExporter CreateExporter();

    protected abstract Task<MappingData?> LoadMappingData();
    protected abstract Task SaveMappingData(MappingData data);

    public async Task Import(ImportMode mode)
    {
        var importer = this.CreateImporter();

        var updatedData = await importer.Read();
        
        var currentData = await this.LoadMappingData();
        if (currentData == null || mode == ImportMode.Overwrite)
        {
            if (currentData == null) WriteLineDebug($"currentData is null");
            if (mode == ImportMode.Overwrite) WriteLineDebug($"mode is overwrite");
            await this.SaveMappingData(updatedData);
            return;
        }
        this.WriteLineStandard("");
        
        var merger = this.CreateMerger();

        if (mode == ImportMode.Default)
        {
            WriteLineDebug($"Previewing merge...");
            if (merger.Preview(currentData, updatedData))
            {
                this.WriteLineStandard($"{merger.Result.MergeActions.Count} changes NOT saved! Run in merge or overwrite modes to save changes.");
            }
            else
            {
                this.WriteLineStandard("No changes detected.");
            }
            return;
        }

        if (mode == ImportMode.Merge)
        {
            WriteLineDebug($"Merging...");
            var mergedData = merger.Merge(currentData, updatedData);
            await this.SaveMappingData(mergedData);
        }
    }

    public async Task ExportPreview()
    {
        var data = await this.LoadMappingData();
        if (data == null) throw new Exception("Could not load saved mappings!");

        var exporter = this.CreateExporter();
        await exporter.Preview(data);
        WriteLineStandard($"Execute \"export apply\" to apply these changes.");
    }

    public async Task ExportApply()
    {
        var data = await this.LoadMappingData();
        if (data == null) throw new Exception("Could not load saved mappings!");

        var exporter = this.CreateExporter();
        exporter.Backup();
        await exporter.Update(data);
        WriteLineStandard($"Mappings applied to [{exporter.GameConfigPath}].");
    }

    public void Backup()
    {
        var exporter = this.CreateExporter();
        var backup = exporter.Backup();
        WriteLineStandard($"{this.GameType} config backed up to [{backup}].");
    }

    public void Restore()
    {
        var exporter = this.CreateExporter();
        var backup = exporter.RestoreLatest();
        WriteLineStandard($"{this.GameType} config restored from [{backup}].");
    }

    public abstract void Open();

    public abstract void OpenGameConfig();
}

public class ControlManager : ControlManagerBase
{
    public string StarCitizenActionmapsXmlPath { get => System.IO.Path.Combine(this.ReadLocation, Constants.SC_ACTIONMAPS_XML_NAME); }
    public string SccmMappingsJsonPath { get => System.IO.Path.Combine(this.SaveLocation, Constants.SCCM_SCMAPPINGS_JSON_NAME); }

    public override string GameType => "Star Citizen";

    private readonly IPlatform _platform;
    private readonly ISCFolders _folders;

    public ControlManager(IPlatform platform, ISCFolders folders)
    {
        this._platform = platform;
        this._folders = folders;
        Initialize();
    }

    private void Initialize()
    {
        this.ReadLocation = this._folders.ActionMapsDir;
        this.SaveLocation = this._folders.SccmDir;
    }

    protected override MappingImporter CreateImporter()
    {
        var importer = new MappingImporter(this._platform, this.StarCitizenActionmapsXmlPath);
        importer.StandardOutput += WriteLineStandard;
        importer.WarningOutput += WriteLineWarning;
        importer.DebugOutput += WriteLineDebug;
        return importer;
    }

    protected override MappingImportMerger CreateMerger()
    {
        var merger = new MappingImportMerger();
        merger.StandardOutput += WriteLineStandard;
        merger.WarningOutput += WriteLineWarning;
        merger.DebugOutput += WriteLineDebug;
        return merger;
    }

    protected override MappingExporter CreateExporter()
    {
        var exporter = new MappingExporter(this._platform, this._folders, StarCitizenActionmapsXmlPath);
        exporter.StandardOutput += WriteLineStandard;
        exporter.WarningOutput += WriteLineWarning;
        exporter.DebugOutput += WriteLineDebug;
        return exporter;
    }

    protected override async Task<MappingData?> LoadMappingData()
    {
        var serializer = new DataSerializer(this.SccmMappingsJsonPath);
        return await serializer.Read();
    }

    protected override async Task SaveMappingData(MappingData data)
    {
        if (data == null)
        {
            throw new ArgumentNullException(nameof(data));
        }

        System.IO.Directory.CreateDirectory(this.SaveLocation);
        var serializer = new DataSerializer(this.SccmMappingsJsonPath);
        await serializer.Write(data);
        WriteLineStandard($"Mappings backed up to [{this.SccmMappingsJsonPath}].");
    }

    public override void Open()
    {
        // TODO write simple test
        this._platform.Open(this.SccmMappingsJsonPath);
        WriteLineStandard($"Opening [{this.SccmMappingsJsonPath}] in the default editor, change the Preserve property to choose which settings are overwritten.");
    }
    
    public override void OpenGameConfig()
    {
        // TODO write simple test
        this._platform.Open(this.StarCitizenActionmapsXmlPath);
        WriteLineStandard($"Opening [{this.StarCitizenActionmapsXmlPath}] in the default editor.");
    }
}