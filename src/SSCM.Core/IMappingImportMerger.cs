namespace SSCM.Core;

public interface IMappingImportMerger
{
    event Action<string> StandardOutput;
    event Action<string> WarningOutput;
    event Action<string> DebugOutput;

    MappingMergeResult Result { get; }

    bool Preview(MappingData current, MappingData updated);
    
    MappingData Merge(MappingData current, MappingData updated);
}
