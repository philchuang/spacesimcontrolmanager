namespace SSCM.Core;

public abstract class MappingImportMergerBase<TData> : IMappingImportMerger<TData>
{
    public event Action<string> StandardOutput = delegate {};
    public event Action<string> WarningOutput = delegate {};
    public event Action<string> DebugOutput = delegate {};

    private bool _suppressStandardOutput;
    protected void WriteLineStandard(string s) {
        if (!this._suppressStandardOutput) this.StandardOutput(s);
    }
    protected void WriteLineWarning(string s) => this.WarningOutput(s);
    protected void WriteLineDebug(string s) => this.DebugOutput(s);

    public abstract MappingMergeResultBase<TData> Result { get; set; }

    public bool Preview(TData current, TData updated)
    {
        this.CalculateDiffs(current, updated);

        return this.Result.HasDifferences && this.Result.CanAutoMerge;
    }

    public TData Merge(TData current, TData updated)
    {
        return this.Merge(current, updated, new DefaultUserInput());
    }

    public TData MergeInteractive(TData current, TData updated, IUserInput userInput)
    {
        return this.Merge(current, updated, userInput);
    }

    public InteractiveChangeSession CreateInteractiveSession(TData current, TData updated)
    {
        this._suppressStandardOutput = true;
        try
        {
            this.CalculateDiffs(current, updated);
        }
        finally
        {
            this._suppressStandardOutput = false;
        }

        var rows = this.Result.MergeActions.Select(action => this.CreateInteractiveRow(current, action)).ToList();
        return new InteractiveChangeSession(rows);
    }

    protected abstract void CalculateDiffs(TData current, TData updated);
    protected abstract TData Merge(TData current, TData updated, IUserInput userInput);
    protected abstract InteractiveChangeRow CreateInteractiveRow(TData current, MappingMergeAction action);

    protected InteractiveChangeRow CreateInteractiveRow(
        MappingMergeAction action,
        string itemId,
        string currentValue,
        string newValue,
        Func<bool> apply,
        string? rowId = null)
    {
        return new InteractiveChangeRow(
            rowId ?? itemId,
            action.Mode.ToString(),
            itemId,
            currentValue,
            action.Mode == MappingMergeActionMode.Remove ? string.Empty : newValue,
            !action.ExistingIsPreserved,
            apply);
    }

    protected static InvalidOperationException UnsupportedActionValue(MappingMergeAction action)
    {
        return new InvalidOperationException($"Unsupported merge action value [{action.Value.GetType().FullName}].");
    }

    protected static bool ApplyListAction<T>(
        IList<T> list,
        MappingMergeAction action,
        T value,
        Func<T, bool>? findExisting = null)
    {
        findExisting ??= item => EqualityComparer<T>.Default.Equals(item, value);

        if (action.Mode == MappingMergeActionMode.Add)
        {
            list.Add(value);
            return true;
        }

        if (action.Mode == MappingMergeActionMode.Remove)
        {
            var toRemove = list.Single(findExisting);
            list.Remove(toRemove);
            return true;
        }

        if (action.Mode == MappingMergeActionMode.Replace)
        {
            var toRemove = list.Single(findExisting);
            var idx = list.IndexOf(toRemove);
            list.Insert(idx, value);
            list.RemoveAt(idx + 1);
            return true;
        }

        return false;
    }
}
