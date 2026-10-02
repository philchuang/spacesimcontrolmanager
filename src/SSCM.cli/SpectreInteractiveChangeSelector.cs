using SSCM.Core;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace SSCM.cli;

public class SpectreInteractiveChangeSelector : IInteractiveChangeSelector
{
    private const int TableAndFooterRowCount = 6;

    private readonly Func<string> _gameTypeFunc;
    private readonly int? _rowCount;
    private int _cursor;
    private int _windowStart;

    public SpectreInteractiveChangeSelector(Func<string> gameTypeFunc, int? rowCount = null)
    {
        if (rowCount <= 0) throw new ArgumentOutOfRangeException(nameof(rowCount), "Row count must be greater than zero.");

        this._gameTypeFunc = gameTypeFunc ?? throw new ArgumentException("Game type is required.", nameof(gameTypeFunc));;
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

                    var control = key.Modifiers.HasFlag(ConsoleModifiers.Control);
                    if (key.Key == ConsoleKey.UpArrow && control) this.ScrollWindow(session, -1);
                    else if (key.Key == ConsoleKey.DownArrow && control) this.ScrollWindow(session, 1);
                    else if (key.Key == ConsoleKey.UpArrow) this.MoveCursor(session, -1);
                    else if (key.Key == ConsoleKey.DownArrow) this.MoveCursor(session, 1);
                    else if (key.Key == ConsoleKey.PageUp) this.MovePage(session, -1);
                    else if (key.Key == ConsoleKey.PageDown) this.MovePage(session, 1);
                    else if (key.Key == ConsoleKey.Spacebar) session.Toggle(session.Rows[this._cursor].RowId);
                    else if (key.Key == ConsoleKey.A) session.SelectAll();
                    else if (key.Key == ConsoleKey.N) session.ClearSelection();
                    else if (key.Key == ConsoleKey.Enter)
                    {
                        if (!this.ConfirmApply(context, session)) continue;
                        result = session.ApplySelected() > 0;
                        return;
                    }
                }
            });

        return result;
    }

    private void MoveCursor(InteractiveChangeSession session, int delta)
    {
        var total = session.Rows.Count;
        if (total == 0 || delta == 0) return;

        this.EnsureVisible(total);
        var windowSize = Math.Min(this.GetRowCount(), total);
        var windowEnd = this._windowStart + windowSize - 1;

        if (delta > 0)
        {
            if (this._cursor < windowEnd)
            {
                this._cursor++;
                return;
            }

            if (this._cursor < total - 1)
            {
                this._cursor++;
                this._windowStart++;
                return;
            }

            this._cursor = 0;
            this._windowStart = 0;
            return;
        }

        if (this._cursor > this._windowStart)
        {
            this._cursor--;
            return;
        }

        if (this._cursor > 0)
        {
            this._cursor--;
            this._windowStart--;
            return;
        }

        this._cursor = total - 1;
        this._windowStart = total - windowSize;
    }

    private void MovePage(InteractiveChangeSession session, int direction)
    {
        var total = session.Rows.Count;
        if (total == 0 || direction == 0) return;

        this.EnsureVisible(total);
        var windowSize = Math.Min(this.GetRowCount(), total);
        var windowEnd = this._windowStart + windowSize - 1;
        var maxStart = total - windowSize;
        var pageStep = Math.Max(1, windowSize - 1);

        if (direction > 0)
        {
            if (this._cursor < windowEnd)
            {
                this._cursor = windowEnd;
                return;
            }

            if (this._windowStart >= maxStart) return;

            this._windowStart = Math.Min(this._windowStart + pageStep, maxStart);
            this._cursor = Math.Min(this._windowStart + windowSize - 1, total - 1);
            return;
        }

        if (this._cursor > this._windowStart)
        {
            this._cursor = this._windowStart;
            return;
        }

        if (this._windowStart == 0) return;

        this._windowStart = Math.Max(0, this._windowStart - pageStep);
        this._cursor = this._windowStart;
    }

    private void ScrollWindow(InteractiveChangeSession session, int direction)
    {
        var total = session.Rows.Count;
        if (total == 0 || direction == 0) return;

        this.EnsureVisible(total);
        var windowSize = Math.Min(this.GetRowCount(), total);
        var maxStart = total - windowSize;
        var nextStart = direction > 0
            ? Math.Min(this._windowStart + 1, maxStart)
            : Math.Max(this._windowStart - 1, 0);
        if (nextStart == this._windowStart) return;

        this._windowStart = nextStart;
        var windowEnd = this._windowStart + windowSize - 1;
        if (this._cursor < this._windowStart) this._cursor = this._windowStart;
        else if (this._cursor > windowEnd) this._cursor = windowEnd;
    }

    private IRenderable CreateDisplay(InteractiveChangeSession session)
    {
        var rows = session.Rows;
        this.EnsureVisible(rows.Count);
        var rowCount = this.GetRowCount();
        var startIndex = this._windowStart;
        var visibleRows = rows
            .Skip(startIndex)
            .Take(rowCount)
            .Select((row, index) => (Row: row, Index: startIndex + index));

        var table = new Table()
            .Expand()
            .Border(TableBorder.Rounded)
            .AddColumn(new TableColumn("Sel").NoWrap())
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

        var legend = "[UP/DN] move  [CTRL+UP/DN] scroll  [PGUP/PGDN] page  [SPACE] select  [A]ll  [N]one  [ENTER] review  [ESC/Q]uit";
        var status = $"Rows {visibleStart}-{visibleEnd} of {rows.Count}  Selected {selectedCount}";
        var spacing = Math.Max(1, Console.WindowWidth - legend.Length - status.Length);
        var footer = $"{legend}{new string(' ', spacing)}{status}";
        var titleText = $"SpaceSim Control Manager - {this._gameTypeFunc()}";
        var title = new Panel(new Markup($"[bold white on blue] {Markup.Escape(titleText)} [/]"))
            .Expand()
            .Border(BoxBorder.None)
            .Padding(0, 0);

        return new Rows(
            title,
            table,
            new Markup($"[grey]{Markup.Escape(footer)}[/]"));
    }

    private bool ConfirmApply(LiveDisplayContext context, InteractiveChangeSession session)
    {
        var selected = session.Rows.Where(r => r.IsSelected).ToList();
        if (selected.Count == 0) return false;

        var windowStart = 0;
        while (true)
        {
            var rowCount = Math.Min(this.GetRowCount(), selected.Count);
            var maxStart = Math.Max(0, selected.Count - rowCount);
            if (windowStart > maxStart) windowStart = maxStart;

            context.UpdateTarget(this.CreateSummaryDisplay(selected, windowStart));
            context.Refresh();

            var key = Console.ReadKey(true);
            if (key.Key == ConsoleKey.Y || key.Key == ConsoleKey.Enter) return true;
            if (key.Key == ConsoleKey.N || key.Key == ConsoleKey.Escape || key.Key == ConsoleKey.Q) return false;

            var pageStep = Math.Max(1, rowCount - 1);
            if (key.Key == ConsoleKey.UpArrow) windowStart = Math.Max(0, windowStart - 1);
            else if (key.Key == ConsoleKey.DownArrow) windowStart = Math.Min(maxStart, windowStart + 1);
            else if (key.Key == ConsoleKey.PageUp) windowStart = Math.Max(0, windowStart - pageStep);
            else if (key.Key == ConsoleKey.PageDown) windowStart = Math.Min(maxStart, windowStart + pageStep);
        }
    }

    private IRenderable CreateSummaryDisplay(IReadOnlyList<InteractiveChangeRow> selected, int windowStart)
    {
        var rowCount = this.GetRowCount();
        var visibleRows = selected.Skip(windowStart).Take(rowCount);
        var table = new Table()
            .Expand()
            .Border(TableBorder.Rounded)
            .AddColumn(new TableColumn("Action").NoWrap())
            .AddColumn(new TableColumn("Current").NoWrap())
            .AddColumn(new TableColumn("New").NoWrap());

        var width = Math.Max(80, Console.WindowWidth);
        var valueWidth = Math.Max(20, (width - 24) / 2);
        foreach (var row in visibleRows)
        {
            table.AddRow(
                new Markup(Markup.Escape($"{row.ChangeKind} {row.ItemId}")),
                new Markup(Markup.Escape(Trim(row.CurrentValue, valueWidth))),
                new Markup(Markup.Escape(Trim(row.NewValue, valueWidth))));
        }

        var visibleStart = selected.Count == 0 ? 0 : windowStart + 1;
        var visibleEnd = Math.Min(windowStart + rowCount, selected.Count);
        var changeLabel = selected.Count == 1 ? "change" : "changes";
        var legend = "[UP/DN] scroll  [PGUP/PGDN] page  [Y/ENTER] confirm  [N/ESC] back";
        var status = $"Apply {selected.Count} {changeLabel}  {visibleStart}-{visibleEnd} of {selected.Count}";
        var spacing = Math.Max(1, Console.WindowWidth - legend.Length - status.Length);
        var footer = $"{legend}{new string(' ', spacing)}{status}";
        var titleText = $"SpaceSim Control Manager - {this._gameTypeFunc()} - Confirm";
        var title = new Panel(new Markup($"[bold white on blue] {Markup.Escape(titleText)} [/]"))
            .Expand()
            .Border(BoxBorder.None)
            .Padding(0, 0);

        return new Rows(
            title,
            table,
            new Markup($"[grey]{Markup.Escape(footer)}[/]"));
    }

    private int GetRowCount()
    {
        return this._rowCount ?? Math.Max(1, Console.WindowHeight - TableAndFooterRowCount);
    }

    private void EnsureVisible(int total)
    {
        if (total <= 0)
        {
            this._cursor = 0;
            this._windowStart = 0;
            return;
        }

        if (this._cursor < 0) this._cursor = 0;
        if (this._cursor >= total) this._cursor = total - 1;

        var windowSize = Math.Min(this.GetRowCount(), total);
        var maxStart = total - windowSize;
        if (this._cursor < this._windowStart)
            this._windowStart = this._cursor;
        else if (this._cursor >= this._windowStart + windowSize)
            this._windowStart = this._cursor - windowSize + 1;

        if (this._windowStart < 0) this._windowStart = 0;
        if (this._windowStart > maxStart) this._windowStart = maxStart;
    }

    private static string Trim(string value, int width)
    {
        value = value ?? string.Empty;
        if (value.Length <= width) return value;
        if (width <= 3) return value.Substring(0, width);
        return value.Substring(0, width - 3) + "...";
    }
}
