namespace SSCM.Core;

// TODO write tests for this class

public interface IControlManager
{
    event Action<string> StandardOutput;
    event Action<string> WarningOutput;
    event Action<string> DebugOutput;

    string GameType { get; }
    string GameConfigLocation { get; set; }
    string AppSaveLocation { get; set; }

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
    public string GameConfigLocation { get; set; } = string.Empty;
    public string AppSaveLocation { get; set; } = string.Empty;

    protected abstract string GameConfigPath { get; }
    protected abstract string MappingDataSavePath { get; }
    protected IPlatform Platform { get; init; }
    private readonly Lazy<IMappingDataRepository> _lazyMappingDataRepository;
    protected IMappingDataRepository MappingDataRepository => this._lazyMappingDataRepository.Value;

    protected ControlManagerBase(IPlatform platform)
    {
        this.Platform = platform;
        this._lazyMappingDataRepository = new Lazy<IMappingDataRepository>(() => this.CreateMappingDataRepository());
    }

    protected abstract IMappingDataRepository CreateMappingDataRepository();
    protected abstract IMappingImporter CreateImporter();
    protected abstract IMappingImportMerger CreateMerger();
    protected abstract IMappingExporter CreateExporter();

    public async Task Import(ImportMode mode)
    {
        var importer = this.CreateImporter();

        var updatedData = await importer.Read();
        
        var currentData = await this.MappingDataRepository.Load();
        if (currentData == null || mode == ImportMode.Overwrite)
        {
            if (currentData == null) WriteLineDebug($"currentData is null");
            if (mode == ImportMode.Overwrite) 
            {
                WriteLineDebug($"mode is overwrite");
                if (currentData != null) WriteLineWarning("Overwriting existing mappings data!");
            }
            await this.MappingDataRepository.Save(updatedData);
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
                this.WriteLineStandard("No changes to make.");
            }
            return;
        }

        if (mode == ImportMode.Merge)
        {
            this.MappingDataRepository.Backup();
            WriteLineDebug($"Merging...");
            var mergedData = merger.Merge(currentData, updatedData);
            await this.MappingDataRepository.Save(mergedData);
        }
    }

    public async Task ExportPreview()
    {
        var data = await this.MappingDataRepository.Load();
        if (data == null) throw new Exception("Could not load saved mappings!");

        var exporter = this.CreateExporter();
        WriteLineStandard($"PREVIEWING EXPORT:");
        await exporter.Preview(data);
        WriteLineStandard($"CONFIGURATION NOT UPDATED: Execute \"export apply\" to apply these changes.");
    }

    public async Task ExportApply()
    {
        var data = await this.MappingDataRepository.Load();
        if (data == null) throw new Exception("Could not load saved mappings!");

        var exporter = this.CreateExporter();
        exporter.Backup();
        await exporter.Update(data);
        WriteLineStandard($"CONFIGURATION UPDATED: Changes applied to [{exporter.GameConfigPath}].");
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

    public void Open()
    {
        // TODO write simple test
        try
        {
            this.Platform.Open(this.MappingDataSavePath);
            WriteLineStandard($"Opening [{this.MappingDataSavePath}] in the default editor, change the Preserve property to choose which settings are overwritten.");
        }
        catch (FileNotFoundException)
        {
            this.StandardOutput($"Could not find mapping file at [{this.MappingDataSavePath}].");
        }
    }

    public void OpenGameConfig()
    {
        // TODO write simple test
        try
        {
            this.Platform.Open(this.GameConfigPath);
            WriteLineStandard($"Opening [{this.GameConfigPath}] in the default editor.");
        }
        catch (FileNotFoundException)
        {
            this.StandardOutput($"Could not find mapping file at [{this.GameConfigPath}].");
        }
    }
}
