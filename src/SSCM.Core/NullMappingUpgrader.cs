namespace SSCM.Core;

public class NullMappingUpgrader<TData> : MappingUpgraderBase<TData>
{
    public override MappingMergeResultBase<TData> Result
    {
        get => null;
        set { }
    }

    public NullMappingUpgrader(IPlatform platform) : base(platform)
    {
    }

    public override async Task<TData> Upgrade(TData current)
    {
        return default(TData);
    }
}