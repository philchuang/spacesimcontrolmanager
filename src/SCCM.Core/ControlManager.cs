namespace SCCM.Core;

public class ControlManager
{
    public event Action<string> StandardOutput = delegate {};
    public event Action<string> WarningOutput = delegate {};
    public event Action<string> DebugOutput = delegate {};

    public string ReadLocation { get; set; } = string.Empty;
    public string SaveLocation { get; set; } = string.Empty;

    public string StarCitizenActionmapsXmlPath { get => System.IO.Path.Combine(this.ReadLocation, Constants.SC_ACTIONMAPS_XML_NAME); }
    public string SccmMappingsJsonPath { get => System.IO.Path.Combine(this.SaveLocation, Constants.SCCM_SCMAPPINGS_JSON_NAME); }

    private readonly IPlatform _platform;
    private readonly IFolders _folders;

    public ControlManager(IPlatform platform, IFolders folders)
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

    private MappingImporter CreateImporter()
    {
        var importer = new MappingImporter(this._platform, this.StarCitizenActionmapsXmlPath);
        importer.StandardOutput += this.StandardOutput;
        importer.WarningOutput += this.WarningOutput;
        importer.DebugOutput += this.DebugOutput;
        return importer;
    }

    private MappingExporter CreateExporter()
    {
        var exporter = new MappingExporter(this._platform, this._folders, StarCitizenActionmapsXmlPath);
        exporter.StandardOutput += this.StandardOutput;
        exporter.WarningOutput += this.WarningOutput;
        exporter.DebugOutput += this.DebugOutput;
        return exporter;
    }

    private MappingImportMerger CreateMerger()
    {
        var merger = new MappingImportMerger();
        merger.StandardOutput += this.StandardOutput;
        merger.WarningOutput += this.WarningOutput;
        merger.DebugOutput += this.DebugOutput;
        return merger;
    }

    public async Task Import(ImportMode mode)
    {
        var importer = this.CreateImporter();

        var updatedData = await importer.Read();
        
        var currentData = await this.LoadMappingData();
        if (currentData == null || mode == ImportMode.Overwrite)
        {
            if (currentData == null) this.DebugOutput($"currentData is null");
            if (mode == ImportMode.Overwrite) this.DebugOutput($"mode is overwrite");
            await this.Save(updatedData);
            return;
        }
        this.StandardOutput("");
        
        var merger = this.CreateMerger();

        if (mode == ImportMode.Default)
        {
            this.DebugOutput($"Previewing merge...");
            if (merger.Preview(currentData, updatedData))
            {
                this.StandardOutput($"{merger.ChangesCount} changes NOT saved! Run in merge or overwrite modes to save changes.");
            }
            else
            {
                this.StandardOutput("No changes detected.");
            }
            return;
        }

        if (mode == ImportMode.Merge)
        {
            this.DebugOutput($"Merging...");
            var mergedData = merger.Merge(currentData, updatedData);
            await this.Save(mergedData);
        }
    }

    public async Task ExportPreview()
    {
        var data = await this.LoadMappingData();
        if (data == null) throw new Exception("Could not load saved mappings!");

        var exporter = this.CreateExporter();
        await exporter.Preview(data);
        this.StandardOutput($"Execute \"export apply\" to apply these changes.");
    }

    public async Task ExportApply()
    {
        var data = await this.LoadMappingData();
        if (data == null) throw new Exception("Could not load saved mappings!");

        var exporter = this.CreateExporter();
        exporter.Backup();
        await exporter.Update(data);
        this.StandardOutput($"Mappings applied to [{exporter.ActionMapsXmlPath}].");
    }

    public void Backup()
    {
        var exporter = this.CreateExporter();
        var backup = exporter.Backup();
        this.StandardOutput($"actionmaps.xml backed up to [{backup}].");
    }

    public void Restore()
    {
        var exporter = this.CreateExporter();
        var backup = exporter.RestoreLatest();
        this.StandardOutput($"actionmaps.xml restored from [{backup}].");
    }

    public void Open()
    {
        // TODO write simple test
        this._platform.Open(this.SccmMappingsJsonPath);
        this.StandardOutput($"Opening [{this.SccmMappingsJsonPath}] in the default editor, change the Preserve property to choose which settings are overwritten.");
    }
    
    public void OpenScXml()
    {
        // TODO write simple test
        this._platform.Open(this.StarCitizenActionmapsXmlPath);
        this.StandardOutput($"Opening [{this.StarCitizenActionmapsXmlPath}] in the default editor.");
    }

    private async Task<MappingData?> LoadMappingData()
    {
        var serializer = new DataSerializer(this.SccmMappingsJsonPath);
        return await serializer.Read();
    }

    private async Task Save(MappingData data)
    {
        if (data == null)
        {
            throw new ArgumentNullException(nameof(data));
        }

        System.IO.Directory.CreateDirectory(this.SaveLocation);
        var serializer = new DataSerializer(this.SccmMappingsJsonPath);
        await serializer.Write(data);
        this.StandardOutput($"Mappings backed up to [{this.SccmMappingsJsonPath}].");
    }
}