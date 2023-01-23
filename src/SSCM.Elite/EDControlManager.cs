using SSCM.Core;

namespace SSCM.Elite;

// TODO write tests for this class

public class EDControlManager : ControlManagerBase<EDMappingData>
{
    protected override string GameConfigPath => _folders.GameConfigPath;
    protected override string MappingDataSavePath => Path.Combine(_folders.EliteDataDir, "edmappings.json");

    public override string CommandAlias => "ed";
    public override string GameType => "Elite: Dangerous";

    private readonly IPlatform _platform;
    private readonly IEDFolders _folders;

    public EDControlManager(IPlatform platform, IEDFolders folders) : base(platform)
    {
        this._platform = platform;
        this._folders = folders;
    }

    protected override IMappingDataRepository<EDMappingData> CreateMappingDataRepository()
    {
        var repo = new MappingDataRepositoryDefault<EDMappingData>(this.Platform, this.MappingDataSavePath, "edmappings.{0}.json.bak");
        repo.StandardOutput += WriteLineStandard;
        repo.WarningOutput += WriteLineWarning;
        repo.DebugOutput += WriteLineDebug;
        return repo;
    }

    protected override IMappingImporter<EDMappingData> CreateImporter()
    {
        var importer = new MappingImporter(this.Platform, this.GameConfigPath);
        importer.StandardOutput += WriteLineStandard;
        importer.WarningOutput += WriteLineWarning;
        importer.DebugOutput += WriteLineDebug;
        return importer;
    }

    protected override IMappingImportMerger<EDMappingData> CreateMerger()
    {
        var merger = new MappingImportMerger();
        merger.StandardOutput += WriteLineStandard;
        merger.WarningOutput += WriteLineWarning;
        merger.DebugOutput += WriteLineDebug;
        return merger;
    }

    protected override IMappingExporter<EDMappingData> CreateExporter()
    {
        var exporter = new MappingExporter(this.Platform, GameConfigPath);
        exporter.StandardOutput += WriteLineStandard;
        exporter.WarningOutput += WriteLineWarning;
        exporter.DebugOutput += WriteLineDebug;
        return exporter;
    }

    protected override IMappingReporter<EDMappingData> CreateReporter()
    {
        var exporter = new MappingReporter();
        return exporter;
    }
}