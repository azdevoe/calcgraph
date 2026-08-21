using System.Linq;
using CalcEngine.Core.Model;

namespace CalcEngine.Core.Filtering;

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

    /// <summary>Returns the filter currently set on a column of a range.</summary>
    /// <param name="range">The range to ask about.</param>
    /// <param name="column">The column within that range.</param>
    /// <returns>The filter on that column, or null if there is none.</returns>
    public IRowFilter? GetFilter(CellRange range, int column) =>
        _filters.TryGetValue((range, column), out var f) ? f : null;

    /// <summary>
    /// Sets the filter on a column of a range, replacing any filter already
    /// on that column.
    /// </summary>
    /// <param name="range">The range to filter.</param>
    /// <param name="column">The column within that range to filter on.</param>
    /// <param name="filter">The filter to apply.</param>
    public void SetFilter(CellRange range, int column, IRowFilter filter) =>
        _filters[(range, column)] = filter;

    /// <summary>
    /// Removes the filter from a column of a range. Removing a filter that
    /// was never set makes no difference.
    /// </summary>
    /// <param name="range">The range to change.</param>
    /// <param name="column">The column within that range.</param>
    public void ClearFilter(CellRange range, int column) =>
        _filters.Remove((range, column));

    /// <summary>Removes the filters from every column of a range.</summary>
    /// <param name="range">The range to clear. Filters on other ranges are left alone.</param>
    public void ClearAllFilters(CellRange range)
    {
        foreach (var key in _filters.Keys.Where(k => k.Range.Equals(range)).ToList())
            _filters.Remove(key);
    }

    /// <summary>Works out which rows of a range are still worth showing.</summary>
    /// <param name="range">The range to examine.</param>
    /// <param name="context">Supplies the cell values the filters read.</param>
    /// <returns>
    /// The row numbers that pass every filter set on the range, in ascending
    /// order. A row must pass all of them, so filters on different columns
    /// narrow the result together. A range with no filters gives all of its
    /// rows.
    /// </returns>
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
