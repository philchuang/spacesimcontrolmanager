using SSCM.Core;

namespace SSCM.Tests.Mocks;

internal static class ControlManagerFakeSessions
{
    public static InteractiveChangeSession CreateSession()
    {
        return new InteractiveChangeSession([
            new InteractiveChangeRow("1", "Update", "item", "old", "new", true, () => true),
        ]);
    }
}

internal class FakeControlManager : ControlManagerBase<ControlManagerTestData>
{
    public readonly FakeRepository Repository = new();
    public readonly FakeImporter Importer = new();
    public readonly FakeMerger Merger = new();
    public readonly FakeUpgrader Upgrader = new();
    public readonly FakeExporter Exporter = new();
    public readonly FakeReporter Reporter = new();
    public readonly List<Dictionary<string, string>> AppliedOptions = new();

    public FakeControlManager(IPlatform? platform = null)
        : base(platform ?? new PlatformForTest(), new DefaultUserInput())
    {
    }

    public override string CommandAlias => "fake";
    public override string GameType => "Fake Game";
    protected override string GameConfigPath => "game-config.xml";
    protected override string MappingDataSavePath => "mapping-data.json";

    protected override IMappingDataRepository<ControlManagerTestData> CreateMappingDataRepository() => this.Repository;
    protected override IMappingImporter<ControlManagerTestData> CreateImporter() => this.Importer;
    protected override IMappingImportMerger<ControlManagerTestData> CreateMerger() => this.Merger;
    protected override IMappingUpgrader<ControlManagerTestData> CreateUpgrader() => this.Upgrader;
    protected override IMappingExporter<ControlManagerTestData> CreateExporter() => this.Exporter;
    protected override IMappingReporter<ControlManagerTestData> CreateReporter() => this.Reporter;

    protected override void ApplyOptions(Dictionary<string, string> options)
    {
        this.AppliedOptions.Add(options);
    }
}

internal class FakeRepository : IMappingDataRepository<ControlManagerTestData>
{
    public event Action<string> StandardOutput = delegate {};
    public event Action<string> WarningOutput = delegate {};
    public event Action<string> DebugOutput = delegate {};

    public string MappingDataSavePath { get; set; } = "mapping-data.json";
    public ControlManagerTestData? LoadResult { get; set; } = new("current");
    public List<ControlManagerTestData> SavedData { get; } = new();
    public int BackupCount { get; private set; }

    public ControlManagerTestData CreateNew() => new("new");
    public Task<ControlManagerTestData?> Load(string? saveFilePath = null) => Task.FromResult(this.LoadResult);
    public Task Save(ControlManagerTestData data, string? saveFilePath = null)
    {
        this.SavedData.Add(data);
        return Task.CompletedTask;
    }
    public string? Backup()
    {
        this.BackupCount++;
        return "repo-backup";
    }
    public string? RestoreLatest() => "repo-restore";
}

internal class FakeImporter : IMappingImporter<ControlManagerTestData>
{
    public event Action<string> StandardOutput = delegate {};
    public event Action<string> WarningOutput = delegate {};
    public event Action<string> DebugOutput = delegate {};
    public ControlManagerTestData ReadResult { get; set; } = new("imported");
    public Task<ControlManagerTestData> Read() => Task.FromResult(this.ReadResult);
}

internal class FakeMerger : IMappingImportMerger<ControlManagerTestData>
{
    public event Action<string> StandardOutput = delegate {};
    public event Action<string> WarningOutput = delegate {};
    public event Action<string> DebugOutput = delegate {};
    public MappingMergeResultBase<ControlManagerTestData> Result { get; set; } = new FakeMergeResult(new("current"), new("updated"));
    public bool PreviewResult { get; set; }
    public ControlManagerTestData MergeResult { get; set; } = new("merged");
    public bool ThrowOnMergeInteractive { get; set; }
    public InteractiveChangeSession InteractiveSession { get; set; } = ControlManagerFakeSessions.CreateSession();

    public bool Preview(ControlManagerTestData current, ControlManagerTestData updated) => this.PreviewResult;
    public ControlManagerTestData Merge(ControlManagerTestData current, ControlManagerTestData updated) => this.MergeResult;
    public ControlManagerTestData MergeInteractive(ControlManagerTestData current, ControlManagerTestData updated, IUserInput userInput)
    {
        if (this.ThrowOnMergeInteractive) throw new UserInputCancelledException();
        return this.MergeResult;
    }
    public InteractiveChangeSession CreateInteractiveSession(ControlManagerTestData current, ControlManagerTestData updated) => this.InteractiveSession;
}

internal class FakeUpgrader : IMappingUpgrader<ControlManagerTestData>
{
    public event Action<string> StandardOutput = delegate {};
    public event Action<string> WarningOutput = delegate {};
    public event Action<string> DebugOutput = delegate {};
    public MappingMergeResultBase<ControlManagerTestData> Result { get; } = new FakeMergeResult(new("current"), new("updated"));
    public bool PreviewResult { get; set; }
    public ControlManagerTestData UpgradeResult { get; set; } = new("upgraded");
    public Task<bool> Preview(ControlManagerTestData current) => Task.FromResult(this.PreviewResult);
    public Task<ControlManagerTestData> Upgrade(ControlManagerTestData current) => Task.FromResult(this.UpgradeResult);
}

internal class FakeExporter : IMappingExporter<ControlManagerTestData>
{
    public event Action<string> StandardOutput = delegate {};
    public event Action<string> WarningOutput = delegate {};
    public event Action<string> DebugOutput = delegate {};
    public ExportOptions ExportOptions { get; set; } = ExportOptions.Default;
    public int BackupCount { get; private set; }
    public int RestoreLatestCount { get; private set; }
    public int SaveInteractiveCount { get; private set; }
    public List<ControlManagerTestData> PreviewedData { get; } = new();
    public List<ControlManagerTestData> UpdatedData { get; } = new();
    public InteractiveChangeSession InteractiveSession { get; set; } = ControlManagerFakeSessions.CreateSession();
    public bool UpdateInteractiveResult { get; set; } = true;
    public bool ThrowOnUpdateInteractive { get; set; }

    public string Backup()
    {
        this.BackupCount++;
        return "backup-path";
    }
    public string RestoreLatest()
    {
        this.RestoreLatestCount++;
        return "restore-path";
    }
    public Task<bool> Preview(ControlManagerTestData source)
    {
        this.PreviewedData.Add(source);
        return Task.FromResult(true);
    }
    public Task<bool> Update(ControlManagerTestData source)
    {
        this.UpdatedData.Add(source);
        return Task.FromResult(true);
    }
    public Task<bool> UpdateInteractive(ControlManagerTestData source, IUserInput userInput)
    {
        if (this.ThrowOnUpdateInteractive) throw new UserInputCancelledException();
        return Task.FromResult(this.UpdateInteractiveResult);
    }
    public Task<InteractiveChangeSession> CreateInteractiveSession(ControlManagerTestData source) => Task.FromResult(this.InteractiveSession);
    public Task SaveInteractive()
    {
        this.SaveInteractiveCount++;
        return Task.CompletedTask;
    }
}

internal class FakeReporter : IMappingReporter<ControlManagerTestData>
{
    public event Action<string> StandardOutput = delegate {};
    public event Action<string> WarningOutput = delegate {};
    public event Action<string> DebugOutput = delegate {};
    public List<ControlManagerTestData> ReportedData { get; } = new();
    public string Report(ControlManagerTestData data, ReportingOptions options)
    {
        this.ReportedData.Add(data);
        return $"reported:{data.Name}";
    }
}

internal class FakeInteractiveChangeSelector : IInteractiveChangeSelector
{
    private readonly bool _result;
    public FakeInteractiveChangeSelector(bool result) => this._result = result;
    public bool SelectAndApply(InteractiveChangeSession session) => this._result;
}

internal class FakeMergeResult : MappingMergeResultBase<ControlManagerTestData>
{
    public FakeMergeResult(ControlManagerTestData current, ControlManagerTestData updated) : base(current, updated)
    {
    }
}

internal record ControlManagerTestData(string Name);
