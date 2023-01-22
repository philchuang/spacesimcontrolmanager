using SSCM.Core;

namespace SSCM.Elite;

public class MappingImportMerger : IMappingImportMerger<EDMappingData>
{
    public event Action<string> StandardOutput = delegate {};
    public event Action<string> WarningOutput = delegate {};
    public event Action<string> DebugOutput = delegate {};

    public MappingMergeResult ResultED {
        get;
        set;
    } = new MappingMergeResult(new EDMappingData(), new EDMappingData());

    public MappingMergeResultBase<EDMappingData> Result {
        get => this.ResultED;
        set => this.ResultED = (MappingMergeResult) value;
    }

    public MappingImportMerger()
    {
    }

    public bool Preview(EDMappingData current, EDMappingData updated)
    {
        throw new NotImplementedException();
    }

    public EDMappingData Merge(EDMappingData current, EDMappingData updated)
    {
        throw new NotImplementedException();
    }
}