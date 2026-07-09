using System.Collections;

namespace SolidCS.Avalonia;

/// <summary>
/// 行・列を文字列ミニ言語 (XAML の GridLength 記法: "Auto" / "*" / "2*" / "100") で
/// 定義するグリッドコンテナ。2 つのモードがある:
/// <list type="bullet">
/// <item>rows 省略 (auto-flow): 素の Add(child) で子を左→右に流し込み、
/// 必要なだけ Auto 行を自動生成する。</item>
/// <item>rows 指定 (explicit): { child, row, col } / { child, row, col, rowSpan, colSpan }
/// で明示配置する。範囲外は Add 時点で fail-fast。</item>
/// </list>
/// モードの混在は InvalidOperationException。
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
                "rows を指定した Grid では { child, row, col } 形式で明示配置すること。");
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
                "rows を省略した Grid (auto-flow) では Add(child) で追加すること。");
        }
        if (row < 0 || rowSpan < 1 || row + rowSpan > _rowCount)
        {
            throw new ArgumentOutOfRangeException(nameof(row),
                $"row {row} + rowSpan {rowSpan} は行数 {_rowCount} の範囲外。");
        }
        if (col < 0 || colSpan < 1 || col + colSpan > _columnCount)
        {
            throw new ArgumentOutOfRangeException(nameof(col),
                $"col {col} + colSpan {colSpan} は列数 {_columnCount} の範囲外。");
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

    public override void Dispose()
    {
        foreach (var c in _children) c.Dispose();
    }

    public IEnumerator<Component> GetEnumerator() => _children.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
