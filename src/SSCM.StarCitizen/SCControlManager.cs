using SSCM.Core;

namespace SSCM.StarCitizen;

// TODO write tests for this class

public class SCControlManager : ControlManagerBase<MappingData>
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

    protected override IMappingDataRepository<MappingData> CreateMappingDataRepository()
    {
        var repo = new MappingDataRepositoryDefault<MappingData>(this.Platform, this.MappingDataSavePath, "scmappings.{0}.json.bak");
        repo.StandardOutput += WriteLineStandard;
        repo.WarningOutput += WriteLineWarning;
        repo.DebugOutput += WriteLineDebug;
        return repo;
    }

    protected override IMappingImporter<MappingData> CreateImporter()
    {
        var importer = new MappingImporter(this.Platform, this.GameConfigPath);
        importer.StandardOutput += WriteLineStandard;
        importer.WarningOutput += WriteLineWarning;
        importer.DebugOutput += WriteLineDebug;
        return importer;
    }

    protected override IMappingImportMerger<MappingData> CreateMerger()
    {
        var merger = new MappingImportMerger();
        merger.StandardOutput += WriteLineStandard;
        merger.WarningOutput += WriteLineWarning;
        merger.DebugOutput += WriteLineDebug;
        return merger;
    }

    protected override IMappingExporter<MappingData> CreateExporter()
    {
        var exporter = new MappingExporter(this.Platform, this._folders, GameConfigPath);
        exporter.StandardOutput += WriteLineStandard;
        exporter.WarningOutput += WriteLineWarning;
        exporter.DebugOutput += WriteLineDebug;
        return exporter;
    }

    protected override IMappingReporter<MappingData> CreateReporter()
    {
        var exporter = new MappingReporter();
        return exporter;
    }
}