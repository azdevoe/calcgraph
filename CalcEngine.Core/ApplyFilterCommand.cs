namespace CalcEngine.Core;

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

    public ApplyFilterCommand(FilterManager filters, CellRange range, int column, IRowFilter? newFilter)
    {
        _filters = filters;
        _range = range;
        _column = column;
        _newFilter = newFilter;
    }

    public CellChangeSet Execute()
    {
        _oldFilter = _filters.GetFilter(_range, _column);
        Apply(_newFilter);
        return CellChangeSet.Ok(_range.TopLeft, Array.Empty<CellRef>());
    }

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
