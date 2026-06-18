namespace SSCM.Core;

public class InteractiveChangeSession
{
    private readonly IList<InteractiveChangeRow> _rows;

    public IReadOnlyList<InteractiveChangeRow> Rows => this._rows.ToList();
    public bool HasRows => this._rows.Any();

    public InteractiveChangeSession(IEnumerable<InteractiveChangeRow> rows)
    {
        this._rows = rows.ToList();
        var duplicate = this._rows
            .GroupBy(r => r.RowId, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(g => g.Count() > 1);
        if (duplicate != null)
        {
            throw new ArgumentException($"Interactive change row IDs must be unique. Duplicate row ID [{duplicate.Key}].", nameof(rows));
        }
    }

    public void Toggle(string rowId)
    {
        var row = this.GetRow(rowId);
        row.IsSelected = !row.IsSelected;
    }

    public void SelectAll()
    {
        foreach (var row in this._rows)
        {
            row.IsSelected = true;
        }
    }

    public void ClearSelection()
    {
        foreach (var row in this._rows)
        {
            row.IsSelected = false;
        }
    }

    public int ApplySelected()
    {
        var rowsToApply = this._rows.Where(r => r.IsSelected).ToList();
        var applied = 0;
        foreach (var row in rowsToApply)
        {
            if (row.Apply())
            {
                applied++;
            }
            this._rows.Remove(row);
        }
        return applied;
    }

    private InteractiveChangeRow GetRow(string rowId)
    {
        return this._rows.Single(r => string.Equals(r.RowId, rowId, StringComparison.OrdinalIgnoreCase));
    }
}
