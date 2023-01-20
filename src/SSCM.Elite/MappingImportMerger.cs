using SSCM.Core;

namespace SSCM.Elite;

public class MappingImportMerger : IMappingImportMerger<MappingData>
{
    public event Action<string> StandardOutput = delegate {};
    public event Action<string> WarningOutput = delegate {};
    public event Action<string> DebugOutput = delegate {};

    public MappingMergeResult ResultED {
        get;
        set;
    } = new MappingMergeResult(new MappingData(), new MappingData());

    public MappingMergeResultBase<MappingData> Result {
        get => this.ResultED;
        set => this.ResultED = (MappingMergeResult) value;
    }

    public MappingImportMerger()
    {
    }

    public bool Preview(MappingData current, MappingData updated)
    {
        throw new NotImplementedException();
    }

    public MappingData Merge(MappingData current, MappingData updated)
    {
        throw new NotImplementedException();
    }
}