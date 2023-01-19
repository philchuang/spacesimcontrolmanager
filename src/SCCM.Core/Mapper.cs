namespace SCCM.Core;

public class Mapper
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

    private async Task<MappingData?> LoadMappingData()
    {
        var serializer = new DataSerializer(this.SccmMappingsJsonPath);
        return await serializer.Read();
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

        if (mode == ImportMode.Default)
        {
            this.DebugOutput($"Previewing merge...");
            if (this.PreviewMerge(currentData, updatedData))
            {
                this.StandardOutput("Changes NOT saved! Run in merge or overwrite modes to save changes.");
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
            var mergedData = this.Merge(currentData, updatedData);
            await this.Save(mergedData);
        }
    }

    private bool PreviewMerge(MappingData current, MappingData updated)
    {
        // capture differences
        var inputDiffs = ComparisonHelper.Compare(
            current.Inputs, updated.Inputs,
            i => $"{i.Type}-{i.Product}",
            (p, c) => p.Instance != c.Instance ||
                ComparisonHelper.DictionariesAreDifferent(
                    p.Settings.ToDictionary(s => s.Name), 
                    c.Settings.ToDictionary(s => s.Name))
            );
        var mappingDiffs = ComparisonHelper.Compare(
            current.Mappings, updated.Mappings,
            m => $"{m.ActionMap}-{m.Action}",
            (p, c) => p.Input != c.Input || p.MultiTap != c.MultiTap);

        if (inputDiffs.Any()) this.PrintDiffs(inputDiffs, "inputs");
        if (mappingDiffs.Any()) this.PrintDiffs(mappingDiffs, "mappings");
        return inputDiffs.Any() && mappingDiffs.Any();
    }

    private void PrintDiffs<T>(ComparisonResult<T> comp, string type)
    {
        if (comp.AddedKeys.Any())
        {
            this.StandardOutput($"The following {type} were added: [{string.Join(", ", comp.AddedKeys)}]");
        }
        if (comp.RemovedKeys.Any())
        {
            this.StandardOutput($"The following {type} were removed: [{string.Join(", ", comp.RemovedKeys)}]");
        }
        if (comp.ChangedPairs.Any())
        {
            this.StandardOutput($"The following {type} were modified:");
            foreach (var changed in comp.ChangedPairs)
            {
                this.StandardOutput($"CURRENT [{changed.Key}] = {changed.Current}");
                this.StandardOutput($"UPDATED [{changed.Key}] = {changed.Updated}");
            }
        }
    }

    private MappingData Merge(MappingData current, MappingData updated)
    {
        // TODO implement
        return current;
    }

    public async Task Save(MappingData data)
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

    public async Task Export()
    {
        var data = await this.LoadMappingData();
        if (data == null) throw new Exception("Could not load saved mappings!");

        var exporter = this.CreateExporter();
        exporter.Backup();
        await exporter.Update(data);
        this.StandardOutput($"Mappings restored to [{exporter.ActionMapsXmlPath}].");
    }

    public async Task Backup()
    {
        var exporter = this.CreateExporter();
        var backup = exporter.Backup();
        this.StandardOutput($"actionmaps.xml backed up to [{backup}].");
    }

    public async Task Restore()
    {
        var exporter = this.CreateExporter();
        var backup = exporter.RestoreLatest();
        this.StandardOutput($"actionmaps.xml restored from [{backup}].");
    }

    public async Task Open()
    {
        // TODO write test
        this._platform.Open(this.SccmMappingsJsonPath);
        this.StandardOutput($"Opening [{this.SccmMappingsJsonPath}] in the default editor, change the Preserve property to choose which settings are overwritten.");
    }
}