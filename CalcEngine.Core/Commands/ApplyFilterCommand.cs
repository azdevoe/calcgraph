using CalcEngine.Core.ChangeTracking;
using CalcEngine.Core.Filtering;
using CalcEngine.Core.Model;
namespace CalcEngine.Core.Commands;

/// <summary>
/// Reversible attach/detach/replace of the row filter on one
/// (range, column) key (Group C feature: Sorting &amp; Filtering).
/// Filtering never touches the workbook, a cell's value, or the
/// dependency graph, so Execute/Undo only swap FilterManager state —
/// the returned CellChangeSet always reports zero changed cells, the
/// same "view state only" guarantee FilterManager itself documents.
///
/// newFilter of null means "clear the filter at this key" — the same
/// command class serves SetFilter and ClearFilter, the same shape as
/// SetCellCommand using an empty raw input to mean "clear the cell".
/// </summary>
public sealed class ApplyFilterCommand : ICommand
{
    private readonly FilterManager _filters;
    private readonly CellRange _range;
    private readonly int _column;
    private readonly IRowFilter? _newFilter;
    private IRowFilter? _oldFilter;

    /// <summary>Creates a filter change that has not been applied yet.</summary>
    /// <param name="filters">The set of filters to change.</param>
    /// <param name="range">The range the filter applies to.</param>
    /// <param name="column">The column within that range to filter on.</param>
    /// <param name="newFilter">
    /// The filter to apply, or null to remove whatever filter is on that
    /// column.
    /// </param>
    public ApplyFilterCommand(FilterManager filters, CellRange range, int column, IRowFilter? newFilter)
    {
        _filters = filters;
        _range = range;
        _column = column;
        _newFilter = newFilter;
    }

    /// <summary>Applies the filter, replacing any filter already on that column.</summary>
    /// <returns>
    /// A successful result reporting no changed cells. Filtering changes only
    /// which rows are worth showing, never any cell's value or formula.
    /// </returns>
    public CellChangeSet Execute()
    {
        _oldFilter = _filters.GetFilter(_range, _column);
        Apply(_newFilter);
        return CellChangeSet.Ok(_range.TopLeft, Array.Empty<CellRef>());
    }

    /// <summary>Puts back whatever filter was on that column before.</summary>
    /// <returns>
    /// A successful result reporting no changed cells.
    /// </returns>
    public CellChangeSet Undo()
    {
        Apply(_oldFilter);
        return CellChangeSet.Ok(_range.TopLeft, Array.Empty<CellRef>());
    }

    private void Apply(IRowFilter? filter)
    {
        if (filter is null) _filters.ClearFilter(_range, _column);
        else _filters.SetFilter(_range, _column, filter);
    }
}
