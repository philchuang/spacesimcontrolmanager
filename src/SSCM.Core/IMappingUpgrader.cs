namespace SSCM.Core;

public interface IMappingUpgrader<TData>
{
    event Action<string> StandardOutput;
    event Action<string> WarningOutput;
    event Action<string> DebugOutput;

    MappingMergeResultBase<TData> Result { get; }

    Task<bool> Preview(TData current);

    Task<TData> Upgrade(TData current);
}
