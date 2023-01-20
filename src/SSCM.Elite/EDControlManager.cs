using SSCM.Core;

namespace SSCM.Elite;

// TODO write tests for this class

public class EDControlManager : ControlManagerBase<MappingData>
{
    protected override string GameConfigPath => System.IO.Path.Combine(this.GameConfigLocation, "TODO");
    protected override string MappingDataSavePath => System.IO.Path.Combine(this.AppSaveLocation, "TODO");

    public override string CommandAlias => "ed";
    public override string GameType => "Elite: Dangerous";

    public EDControlManager(IPlatform platform) : base(platform)
    {
        Initialize();
    }

    private void Initialize()
    {
        // TODO
    }

    protected override IMappingDataRepository<MappingData> CreateMappingDataRepository()
    {
        var repo = new MappingDataRepositoryDefault<MappingData>(this.Platform, this.MappingDataSavePath, "edmappings.{0}.json.bak");
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
        var exporter = new MappingExporter(this.Platform, GameConfigPath);
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