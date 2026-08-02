namespace CalcEngine.Core;

/// <summary>
/// The public API surface of the engine (Design_Portfolio 4.1, Fig 2).
/// Wires Workbook, DependencyGraph, FormulaInputParser and
/// ChangeNotifier together behind one entry point — the GUI client
/// calls CalculationEngine and nothing else.
///
/// Every mutating operation returns a CellChangeSet: a successful
/// edit, a parse failure, and a circular reference all come back as
/// data, never as a thrown exception.
///
/// Undo/Redo are intentionally not present yet. Fig 2 lists them on
/// this class, but they depend on CommandManager, which doesn't exist
/// in the repo yet — they'll be added when the Command unit is built,
/// per the project's own next-steps ordering.
/// </summary>
public sealed class CalculationEngine
{
    private readonly Workbook _workbook = new();
    private readonly DependencyGraph _graph = new();
    private readonly FormulaInputParser _parser = new();
    private readonly ChangeNotifier _notifier = new();

    /// <summary>
    /// Parses and applies rawInput to the cell at ref. A formula
    /// (leading '=') has its dependencies extracted and checked for
    /// cycles before anything is written; a literal simply replaces
    /// the cell's content. Either way, every cell that must be
    /// recomputed as a result is recalculated in one topological pass
    /// and observers are notified once with the complete set.
    /// </summary>
    public CellChangeSet SetCellContent(CellRef cellRef, string rawInput)
    {
        var parseResult = _parser.Parse(rawInput);
        if (!parseResult.Success)
            return CellChangeSet.ParseFailure(cellRef, string.Join("; ", parseResult.Errors));

        var deps = parseResult.IsFormula
            ? DependencyVisitor.GetDependencies(parseResult.Tree!)
            : Array.Empty<CellRef>();

        // Checked (and, if necessary, rolled back) before the workbook
        // is touched — a rejected edit must leave the cell exactly as
        // it was, per Design_Portfolio 4.5/6.3.
        var cycle = _graph.SetDependencies(cellRef, deps);
        if (cycle is not null)
        {
            _notifier.NotifyCircularReference(cycle);
            return CellChangeSet.Circular(cellRef, cycle);
        }

        var cell = _workbook.GetOrCreate(cellRef);
        if (parseResult.IsFormula)
        {
            cell.SetFormula(rawInput, parseResult.Tree!);
            cell.SetValue(cell.Tree!.Evaluate(_workbook));
        }
        else
        {
            cell.SetLiteral(rawInput, parseResult.Tree!.Evaluate(_workbook));
        }

        var changedCells = RecomputeAffected(cellRef);

        var changeSet = CellChangeSet.Ok(cellRef, changedCells);
        _notifier.NotifyChanged(changeSet);
        return changeSet;
    }

    /// <summary>Current value of a cell. Empty for a cell that has never been set.</summary>
    public CellValue GetValue(CellRef cellRef) => _workbook.GetCellValue(cellRef);

    /// <summary>The exact raw text last given to SetCellContent, or "" if never set.</summary>
    public string GetFormula(CellRef cellRef) => _workbook.TryGet(cellRef)?.RawInput ?? string.Empty;

    /// <summary>
    /// Empties a cell and recomputes everything that depended on it.
    /// Cells that read the cleared cell now see CellValue.Empty, the
    /// same as any other never-touched cell.
    /// </summary>
    public CellChangeSet ClearCell(CellRef cellRef)
    {
        _graph.SetDependencies(cellRef, Array.Empty<CellRef>()); // drop this cell's own precedents
        _workbook.Remove(cellRef);

        var changedCells = RecomputeAffected(cellRef);

        var changeSet = CellChangeSet.Ok(cellRef, changedCells);
        _notifier.NotifyChanged(changeSet);
        return changeSet;
    }

    /// <summary>
    /// Recomputes every formula cell in the workbook from scratch, in
    /// dependency order. Does not notify observers — this is a bulk
    /// utility operation (e.g. after loading a workbook), not a single
    /// client edit with one describable change set.
    /// </summary>
    public void RecalculateAll()
    {
        var formulaCells = _workbook.AllCells()
            .Where(c => c.IsFormula)
            .Select(c => c.Ref)
            .ToList();

        var order = _graph.TopologicalSort(formulaCells);
        foreach (var r in order)
        {
            var cell = _workbook.TryGet(r);
            if (cell is { IsFormula: true })
                cell.SetValue(cell.Tree!.Evaluate(_workbook));
        }
    }

    /// <summary>Registers an observer for future change notifications.</summary>
    public void Subscribe(ICellObserver observer) => _notifier.Subscribe(observer);

    /// <summary>Removes a previously registered observer.</summary>
    public void Unsubscribe(ICellObserver observer) => _notifier.Unsubscribe(observer);

    /// <summary>
    /// Recomputes every cell downstream of cellRef, in dependency
    /// order, and returns the full list of changed cells (cellRef
    /// first, then its dependents in the order they were recomputed).
    /// Shared by SetCellContent and ClearCell — both change a cell's
    /// content and then must propagate that change outward.
    /// </summary>
    private List<CellRef> RecomputeAffected(CellRef cellRef)
    {
        var affected = _graph.GetAffectedCells(cellRef);
        var order = _graph.TopologicalSort(affected);

        foreach (var r in order)
        {
            var dependentCell = _workbook.TryGet(r);
            if (dependentCell is { IsFormula: true })
                dependentCell.SetValue(dependentCell.Tree!.Evaluate(_workbook));
        }

        var changedCells = new List<CellRef> { cellRef };
        changedCells.AddRange(order);
        return changedCells;
    }
}