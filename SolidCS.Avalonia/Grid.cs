// SPDX-License-Identifier: MIT

using System.Collections;

namespace SolidCS.Avalonia;

/// <summary>
/// Grid container whose rows and columns are defined with a string mini-language
/// (XAML's GridLength notation: "Auto" / "*" / "2*" / "100"). It has two modes:
/// <list type="bullet">
/// <item>rows omitted (auto-flow): plain Add(child) flows children left to right,
/// auto-generating as many Auto rows as needed.</item>
/// <item>rows specified (explicit): place children explicitly with
/// { child, row, col } / { child, row, col, rowSpan, colSpan }.
/// Out-of-range placement fails fast at Add time.</item>
/// </list>
/// Mixing the modes throws InvalidOperationException.
/// </summary>
public sealed class Grid : Component, IEnumerable<Component>
{
    private readonly global::Avalonia.Controls.Grid _grid;
    private readonly List<Component> _children = new();
    private readonly bool _autoFlow;
    private readonly int _rowCount;
    private readonly int _columnCount;
    private int _autoIndex;

    public Grid(string? rows = null, string? columns = null)
    {
        _grid = new global::Avalonia.Controls.Grid
        {
            ColumnDefinitions = new global::Avalonia.Controls.ColumnDefinitions(columns ?? "*"),
        };
        _columnCount = _grid.ColumnDefinitions.Count;

        _autoFlow = rows is null;
        if (rows is not null)
        {
            _grid.RowDefinitions = new global::Avalonia.Controls.RowDefinitions(rows);
            _rowCount = _grid.RowDefinitions.Count;
        }
    }

    public void Add(Component child)
    {
        if (!_autoFlow)
        {
            throw new InvalidOperationException(
                "A Grid with rows specified requires explicit placement via { child, row, col }.");
        }

        var row = _autoIndex / _columnCount;
        var col = _autoIndex % _columnCount;
        _autoIndex++;

        while (_grid.RowDefinitions.Count <= row)
        {
            _grid.RowDefinitions.Add(new global::Avalonia.Controls.RowDefinition
            {
                Height = global::Avalonia.Controls.GridLength.Auto,
            });
        }

        Place(child, row, col, 1, 1);
    }

    public void Add(Component child, int row, int col) => AddExplicit(child, row, col, 1, 1);

    public void Add(Component child, int row, int col, int rowSpan, int colSpan)
        => AddExplicit(child, row, col, rowSpan, colSpan);

    private void AddExplicit(Component child, int row, int col, int rowSpan, int colSpan)
    {
        if (_autoFlow)
        {
            throw new InvalidOperationException(
                "A Grid without rows (auto-flow) requires adding children via Add(child).");
        }
        if (row < 0 || rowSpan < 1 || row + rowSpan > _rowCount)
        {
            throw new ArgumentOutOfRangeException(nameof(row),
                $"row {row} + rowSpan {rowSpan} is out of range for {_rowCount} rows.");
        }
        if (col < 0 || colSpan < 1 || col + colSpan > _columnCount)
        {
            throw new ArgumentOutOfRangeException(nameof(col),
                $"col {col} + colSpan {colSpan} is out of range for {_columnCount} columns.");
        }

        Place(child, row, col, rowSpan, colSpan);
    }

    private void Place(Component child, int row, int col, int rowSpan, int colSpan)
    {
        var control = child.GetControl();
        global::Avalonia.Controls.Grid.SetRow(control, row);
        global::Avalonia.Controls.Grid.SetColumn(control, col);
        if (rowSpan > 1) global::Avalonia.Controls.Grid.SetRowSpan(control, rowSpan);
        if (colSpan > 1) global::Avalonia.Controls.Grid.SetColumnSpan(control, colSpan);

        _children.Add(child);
        _grid.Children.Add(control);
    }

    public override global::Avalonia.Controls.Control GetControl() => _grid;

    protected override void OnDispose()
    {
        foreach (var c in _children) c.Dispose();
    }

    public IEnumerator<Component> GetEnumerator() => _children.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
