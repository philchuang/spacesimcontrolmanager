namespace SCCM.Core;

public class MappingMergeAction
{
    public object Target { get; private set; } 

    public MappingMergeActionMode Mode { get; private set; }

    public object NewValue { get; private set; }
    
    public MappingMergeAction(object target, MappingMergeActionMode mode, object newvalue)
    {
        if (mode == MappingMergeActionMode.None) throw new ArgumentOutOfRangeException(mode.ToString());
        this.Target = target;
        this.Mode = mode;
        this.NewValue = newvalue;
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