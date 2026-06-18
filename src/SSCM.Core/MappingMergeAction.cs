namespace SSCM.Core;

public class MappingMergeAction
{
    public MappingMergeActionMode Mode { get; private set; }

    public object Value { get; private set; }

    public bool ExistingIsPreserved { get; private set; }
    
    public MappingMergeAction(MappingMergeActionMode mode, object value, bool existingIsPreserved = false)
    {
        if (mode == MappingMergeActionMode.None) throw new ArgumentOutOfRangeException(mode.ToString());
        this.Mode = mode;
        this.Value = value;
        this.ExistingIsPreserved = existingIsPreserved;
    }
}

public enum MappingMergeActionMode
{
    None = 0,
    Add = 1,
    Remove = 2,
    Replace = 3,
}