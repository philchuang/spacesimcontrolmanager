using SSCM.Core;

namespace SSCM.StarCitizen;

// TODO write tests for this class

public class SCControlManager : ControlManagerBase<SCMappingData>
{
    // TODO move logic to SCFolders
    protected override string GameConfigPath => System.IO.Path.Combine(this._folders.GameConfigDir, Constants.SC_ACTIONMAPS_XML_NAME);
    protected override string MappingDataSavePath => System.IO.Path.Combine(this._folders.ScDataDir, Constants.SSCM_SCMAPPINGS_JSON_NAME);

    public override string CommandAlias => "sc";
    public override string GameType => "Star Citizen";

    private readonly ISCFolders _folders;

    public SCControlManager(IPlatform platform, ISCFolders folders) : base(platform)
    {
        this._folders = folders;
    }

    protected override IMappingDataRepository<SCMappingData> CreateMappingDataRepository()
    {
        var repo = new MappingDataRepositoryDefault<SCMappingData>(this.Platform, this.MappingDataSavePath, "scmappings.{0}.json.bak");
        repo.StandardOutput += WriteLineStandard;
        repo.WarningOutput += WriteLineWarning;
        repo.DebugOutput += WriteLineDebug;
        return repo;
    }

    protected override IMappingImporter<SCMappingData> CreateImporter()
    {
        var importer = new MappingImporter(this.Platform, this.GameConfigPath);
        importer.StandardOutput += WriteLineStandard;
        importer.WarningOutput += WriteLineWarning;
        importer.DebugOutput += WriteLineDebug;
        return importer;
    }

    protected override IMappingImportMerger<SCMappingData> CreateMerger()
    {
        var merger = new MappingImportMerger();
        merger.StandardOutput += WriteLineStandard;
        merger.WarningOutput += WriteLineWarning;
        merger.DebugOutput += WriteLineDebug;
        return merger;
    }

    protected override IMappingExporter<SCMappingData> CreateExporter()
    {
        var exporter = new MappingExporter(this.Platform, this._folders, GameConfigPath);
        exporter.StandardOutput += WriteLineStandard;
        exporter.WarningOutput += WriteLineWarning;
        exporter.DebugOutput += WriteLineDebug;
        return exporter;
    }

    protected override IMappingReporter<SCMappingData> CreateReporter()
    {
        var exporter = new MappingReporter();
        return exporter;
    }

    public async Task<string> ReportInputs(bool preservedOnly = false)
    {
        var reporter = this.CreateReporter();
        var data = await this.MappingDataRepository.Load();
        return ((MappingReporter) reporter).ReportInputs(data ?? this.MappingDataRepository.CreateNew(), preservedOnly);
    }

    public async Task<string> ReportMappings(bool preservedOnly = false)
    {
        var reporter = this.CreateReporter();
        var data = await this.MappingDataRepository.Load();
        return ((MappingReporter) reporter).ReportMappings(data ?? this.MappingDataRepository.CreateNew(), preservedOnly);
    }
}