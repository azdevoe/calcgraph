using System.Linq;

namespace CalcEngine.Core;

/// <summary>
/// Stores active row filters, keyed by (range, column), and answers
/// which rows of a range remain visible (Group C feature: Sorting &amp;
/// Filtering). Pure view state: SetFilter/ClearFilter/GetVisibleRows
/// never touch a cell's raw input, its value, or the dependency graph
/// — filtering hides rows, it does not edit them. Filters on different
/// columns of the same range combine with AND; only one filter per
/// (range, column) pair is kept, a second SetFilter on the same key
/// replaces the first.
/// </summary>
public sealed class FilterManager
{
    private readonly Dictionary<(CellRange Range, int Column), IRowFilter> _filters = new();

    /// <summary>The filter active on (range, column), or null if none.</summary>
    public IRowFilter? GetFilter(CellRange range, int column) =>
        _filters.TryGetValue((range, column), out var f) ? f : null;

    /// <summary>Attaches filter to (range, column), replacing any filter already there.</summary>
    public void SetFilter(CellRange range, int column, IRowFilter filter) =>
        _filters[(range, column)] = filter;

    /// <summary>Removes the filter at (range, column). A no-op if none was set.</summary>
    public void ClearFilter(CellRange range, int column) =>
        _filters.Remove((range, column));

    /// <summary>Removes every filter attached to range, across all columns.</summary>
    public void ClearAllFilters(CellRange range)
    {
        foreach (var key in _filters.Keys.Where(k => k.Range.Equals(range)).ToList())
            _filters.Remove(key);
    }

    /// <summary>
    /// Row numbers within range that satisfy every filter attached to
    /// range, in ascending order. A range with no active filters
    /// returns every row.
    /// </summary>
    public IReadOnlyList<int> GetVisibleRows(CellRange range, IEvalContext context)
    {
        var columnFilters = _filters.Where(kv => kv.Key.Range.Equals(range)).ToList();
        var visible = new List<int>();

        for (int row = range.TopLeft.Row; row <= range.BottomRight.Row; row++)
        {
            bool matchesAll = true;
            foreach (var (key, filter) in columnFilters)
            {
                var value = context.GetCellValue(new CellRef(row, key.Column));
                if (!filter.Matches(value))
                {
                    matchesAll = false;
                    break;
                }
            }
            if (matchesAll) visible.Add(row);
        }

        return visible;
    }
}
