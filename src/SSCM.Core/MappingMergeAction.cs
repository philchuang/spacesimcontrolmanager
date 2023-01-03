namespace SCCM.Core;

public class MappingMergeAction
{
    public MappingMergeActionMode Mode { get; private set; }

    public object Value { get; private set; }
    
    public MappingMergeAction(MappingMergeActionMode mode, object value)
    {
        if (mode == MappingMergeActionMode.None) throw new ArgumentOutOfRangeException(mode.ToString());
        this.Mode = mode;
        this.Value = value;
    }
}

public enum MappingMergeActionMode
{
    None = 0,
    Add = 1,
    Remove = 2,
    Replace = 3,
}