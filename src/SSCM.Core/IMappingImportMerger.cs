namespace SSCM.Core;

public interface IMappingImportMerger<TData>
{
    event Action<string> StandardOutput;
    event Action<string> WarningOutput;
    event Action<string> DebugOutput;

    MappingMergeResultBase<TData> Result { get; }

    bool Preview(TData current, TData updated);
    
    TData Merge(TData current, TData updated);
}
