using SSCM.Core;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace SSCM.cli;

public class SpectreInteractiveChangeSelector : IInteractiveChangeSelector
{
    private const int TableAndFooterRowCount = 5;

    private readonly int? _rowCount;
    private int _cursor;

    public SpectreInteractiveChangeSelector(int? rowCount = null)
    {
        if (rowCount <= 0) throw new ArgumentOutOfRangeException(nameof(rowCount), "Row count must be greater than zero.");

        this._rowCount = rowCount;
    }

    public bool SelectAndApply(InteractiveChangeSession session)
    {
        if (!session.HasRows) return false;

        var result = false;
        Console.Clear();
        AnsiConsole.Live(this.CreateDisplay(session))
            .AutoClear(true)
            .Start(context =>
            {
                while (session.HasRows)
                {
                    context.UpdateTarget(this.CreateDisplay(session));
                    context.Refresh();

                    var key = Console.ReadKey(true);
                    if (key.Key == ConsoleKey.Escape || key.Key == ConsoleKey.Q) return;
                    if (key.Key == ConsoleKey.UpArrow) this.MoveCursor(session, -1);
                    if (key.Key == ConsoleKey.DownArrow) this.MoveCursor(session, 1);
                    if (key.Key == ConsoleKey.Spacebar) session.Toggle(session.Rows[this._cursor].RowId);
                    if (key.Key == ConsoleKey.A) session.SelectAll();
                    if (key.Key == ConsoleKey.N) session.ClearSelection();
                    if (key.Key == ConsoleKey.Enter)
                    {
                        result = session.ApplySelected() > 0;
                        return;
                    }
                }
            });

        return result;
    }

    private void MoveCursor(InteractiveChangeSession session, int delta)
    {
        this._cursor += delta;
        if (this._cursor < 0) this._cursor = session.Rows.Count - 1;
        if (this._cursor >= session.Rows.Count) this._cursor = 0;
    }

    private IRenderable CreateDisplay(InteractiveChangeSession session)
    {
        var rows = session.Rows;
        if (this._cursor >= rows.Count) this._cursor = Math.Max(0, rows.Count - 1);
        var rowCount = this.GetRowCount();
        var startIndex = GetStartIndex(this._cursor, rows.Count, rowCount);
        var visibleRows = rows
            .Skip(startIndex)
            .Take(rowCount)
            .Select((row, index) => (Row: row, Index: startIndex + index));

        var table = new Table()
            .Expand()
            .Border(TableBorder.Rounded)
            .AddColumn(new TableColumn("").NoWrap())
            .AddColumn(new TableColumn("Action").NoWrap())
            .AddColumn(new TableColumn("Current").NoWrap())
            .AddColumn(new TableColumn("New").NoWrap());

        var width = Math.Max(80, Console.WindowWidth);
        var valueWidth = Math.Max(20, (width - 32) / 2);
        foreach (var visibleRow in visibleRows)
        {
            var row = visibleRow.Row;
            var style = visibleRow.Index == this._cursor ? new Style(Color.Black, Color.Grey) : Style.Plain;
            table.AddRow(
                new Markup(row.IsSelected ? "[green][[x]][/]" : "[grey][[ ]][/]"),
                new Markup(Markup.Escape($"{row.ChangeKind} {row.ItemId}"), style),
                new Markup(Markup.Escape(Trim(row.CurrentValue, valueWidth)), style),
                new Markup(Markup.Escape(Trim(row.NewValue, valueWidth)), style));
        }

        var visibleStart = rows.Count == 0 ? 0 : startIndex + 1;
        var visibleEnd = Math.Min(startIndex + rowCount, rows.Count);
        var selectedCount = rows.Count(r => r.IsSelected);

        var legend = "Up/Down move  Space select  A all  N none  Enter apply  Esc/Q cancel";
        var status = $"Rows {visibleStart}-{visibleEnd} of {rows.Count}  Selected {selectedCount}";
        var spacing = Math.Max(1, Console.WindowWidth - legend.Length - status.Length);
        var footer = $"{legend}{new string(' ', spacing)}{status}";

        return new Rows(
            table,
            new Markup($"[grey]{Markup.Escape(footer)}[/]"));
    }

    private int GetRowCount()
    {
        return this._rowCount ?? Math.Max(1, Console.WindowHeight - TableAndFooterRowCount);
    }

    private static int GetStartIndex(int cursor, int rowTotal, int rowCount)
    {
        if (rowTotal <= rowCount) return 0;

        var startIndex = cursor - rowCount + 1;
        if (startIndex < 0) return 0;

        return Math.Min(startIndex, rowTotal - rowCount);
    }

    private static string Trim(string value, int width)
    {
        value = value ?? string.Empty;
        if (value.Length <= width) return value;
        if (width <= 3) return value.Substring(0, width);
        return value.Substring(0, width - 3) + "...";
    }
}
