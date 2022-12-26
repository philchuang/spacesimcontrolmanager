namespace SCCM.Core;

public class MappingMergeAction
{
    public object? Parent { get; private set; } 

    public MappingMergeActionMode Mode { get; private set; }

    public object Value { get; private set; }
    
    public MappingMergeAction(object? parent, MappingMergeActionMode mode, object value)
    {
        if (mode == MappingMergeActionMode.None) throw new ArgumentOutOfRangeException(mode.ToString());
        this.Parent = parent;
        this.Mode = mode;
        this.Value = value;
    }
}

// public enum MappingMergeActionTarget
// {
//     None = 0,
//     Input = 1,
//     InputSetting = 2,
//     Mapping = 3,
// }

public enum MappingMergeActionMode
{
    None = 0,
    Add = 1,
    Remove = 2,
    Replace = 3,
}