namespace SSCM.Core;

public class InteractiveChangeRow
{
    private readonly Func<bool> _apply;

    public string RowId { get; }
    public string ChangeKind { get; }
    public string ItemId { get; }
    public string CurrentValue { get; }
    public string NewValue { get; }
    public bool IsSelected { get; set; }

    public InteractiveChangeRow(
        string rowId,
        string changeKind,
        string itemId,
        string currentValue,
        string newValue,
        bool isSelected,
        Func<bool> apply)
    {
        this.RowId = rowId;
        this.ChangeKind = changeKind;
        this.ItemId = itemId;
        this.CurrentValue = currentValue;
        this.NewValue = newValue;
        this.IsSelected = isSelected;
        this._apply = apply;
    }

    public bool Apply() => this._apply();
}
