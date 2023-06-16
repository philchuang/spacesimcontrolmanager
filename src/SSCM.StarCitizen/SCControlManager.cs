using SSCM.Core;

namespace SSCM.StarCitizen;

// TODO write tests for this class

public class SCControlManager : ControlManagerBase<SCMappingData>
{
    protected override string GameConfigPath => System.IO.Path.Combine(this._folders.GameConfigDir, Constants.SC_ACTIONMAPS_XML_NAME);
    protected override string MappingDataSavePath => this._folders.MappingDataSavePath;

    public override string CommandAlias => "sc";
    public override string GameType => "Star Citizen";

    private readonly ISCFolders _folders;

    public SCControlManager(IPlatform platform, ISCFolders folders, IUserInput userInput) : base(platform, userInput)
    {
        this._folders = folders;
    }

    protected override IMappingDataRepository<SCMappingData> CreateMappingDataRepository()
    {
        var repo = new MappingDataRepositoryDefault<SCMappingData>(this.Platform, this._folders.MappingDataSavePath, "scmappings.{0}.json.bak");
        repo.StandardOutput += WriteLineStandard;
        repo.WarningOutput += WriteLineWarning;
        repo.DebugOutput += WriteLineDebug;
        return repo;
    }

    protected override IMappingImporter<SCMappingData> CreateImporter()
    {
        var importer = new MappingImporter(this.Platform, this._folders);
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
        var exporter = new MappingExporter(this.Platform, this._folders);
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

    public async Task<string> ReportInputs(ReportingOptions options)
    {
        var reporter = this.CreateReporter();
        var data = await this.MappingDataRepository.Load();
        return ((MappingReporter) reporter).ReportInputs(data ?? this.MappingDataRepository.CreateNew(), options);
    }

    public async Task<string> ReportMappings(ReportingOptions options)
    {
        var reporter = this.CreateReporter();
        var data = await this.MappingDataRepository.Load();
        return ((MappingReporter) reporter).ReportMappings(data ?? this.MappingDataRepository.CreateNew(), options);
    }
}