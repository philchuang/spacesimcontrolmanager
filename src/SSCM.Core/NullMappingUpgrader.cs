namespace SSCM.Core;

public class NullMappingUpgrader<TData> : MappingUpgraderBase<TData>
{
    private MappingMergeResultBase<TData>? _result;

    public override MappingMergeResultBase<TData> Result
    {
        get => this._result ?? throw new InvalidOperationException("No upgrade result has been created.");
        set { }
    }

    public NullMappingUpgrader(IPlatform platform) : base(platform)
    {
    }

    public override Task<TData> Upgrade(TData current)
    {
        this._result = new NullMappingMergeResult<TData>(current);
        return Task.FromResult(current);
    }

    private sealed class NullMappingMergeResult<T>(T current) : MappingMergeResultBase<T>(current, current)
    {
    }
}
