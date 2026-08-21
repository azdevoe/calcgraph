using CalcEngine.Core.ChangeTracking;
using CalcEngine.Core.Commands;
using CalcEngine.Core.Dependencies;
using CalcEngine.Core.Model;
using CalcEngine.Core.Parsing;
using CalcEngine.Core.Validation;
using CalcEngine.Core.Filtering;
using CalcEngine.Core.Sorting;
using CalcEngine.Core.Expressions;

namespace CalcEngine.Core.Engine;

/// <summary>
/// The public API surface of the engine (Design_Portfolio 4.1, Fig 2).
/// Wires Workbook, DependencyGraph, FormulaInputParser, ChangeNotifier,
/// CommandManager and ValidationRegistry together behind one entry
/// point — the GUI client calls CalculationEngine and nothing else.
///
/// Every mutating operation returns a CellChangeSet: a successful
/// edit, a parse failure, a circular reference, and a validation
/// rejection all come back as data, never as a thrown exception.
///
/// Undo/Redo are backed by CommandManager (Design_Portfolio 6.5).
/// SetCellContent and ClearCell create SetCellCommand objects and route
/// them through ExecuteCommand so every edit is automatically recorded.
///
/// Data validation (Group C feature) is backed by ValidationRegistry:
/// a cell may have at most one IValidationRule attached. ApplyEdit
/// checks the EVALUATED VALUE against that rule — after parsing and
/// the cycle check, before anything is written to the workbook — so a
/// rejected edit leaves the workbook, the dependency graph, and
/// observers exactly as they were, same as a parse failure or a
/// circular reference.
/// </summary>
public sealed class CalculationEngine
{
    private readonly Workbook _workbook = new();
    private readonly DependencyGraph _graph = new();
    private readonly FormulaInputParser _parser = new();
    private readonly ChangeNotifier _notifier = new();
    private readonly CommandManager _commandManager = new();
    private readonly ValidationRegistry _validationRegistry = new();
    private readonly FilterManager _filterManager = new();

    // Batch state (Group C feature: Sorting & Filtering — a sort must
    // cost one topological sort, not one per moved cell). See
    // BeginBatch/EndBatch.
    private int _batchDepth;
    private readonly HashSet<CellRef> _batchRoots = new();

    // ── Public API: editing ────────────────────────────────────────

    /// <summary>
    /// Parses and applies rawInput to the cell at ref. A formula
    /// (leading '=') has its dependencies extracted and checked for
    /// cycles before anything is written; a literal simply replaces
    /// the cell's content. If a validation rule is attached to the
    /// cell, the evaluated value must satisfy it or the edit is
    /// rejected. Either way, every cell that must be recomputed as a
    /// result is recalculated in one topological pass and observers
    /// are notified once with the complete set.
    ///
    /// The edit is recorded in the undo stack so it can be reversed —
    /// but only if it succeeds; a rejected edit never touches undo/redo.
    /// </summary>
    public CellChangeSet SetCellContent(CellRef cellRef, string rawInput)
    {
        var cmd = new SetCellCommand(this, cellRef, rawInput);
        return _commandManager.ExecuteCommand(cmd);
    }

    /// <summary>
    /// Empties a cell and recomputes everything that depended on it.
    /// Cells that read the cleared cell now see CellValue.Empty, the
    /// same as any other never-touched cell.
    ///
    /// Implemented as a SetCellCommand that sets the content to "" so
    /// it participates in undo/redo. The internal ApplyEdit path
    /// detects the empty string and clears the cell.
    /// </summary>
    public CellChangeSet ClearCell(CellRef cellRef)
    {
        // Capture the current raw input so undo can restore it.
        // Then issue a command that sets the cell to "" (empty),
        // which ApplyEdit handles by clearing.
        var cmd = new SetCellCommand(this, cellRef, string.Empty);
        return _commandManager.ExecuteCommand(cmd);
    }

    // ── Public API: querying ──────────────────────────────────────

    /// <summary>Current value of a cell. Empty for a cell that has never been set.</summary>
    public CellValue GetValue(CellRef cellRef) => _workbook.GetCellValue(cellRef);

    /// <summary>The exact raw text last given to SetCellContent, or "" if never set.</summary>
    public string GetFormula(CellRef cellRef) => _workbook.TryGet(cellRef)?.RawInput ?? string.Empty;

    // ── Public API: undo / redo ────────────────────────────────────

    /// <summary>True when at least one edit can be undone.</summary>
    public bool CanUndo => _commandManager.CanUndo;

    /// <summary>True when at least one undone edit can be reapplied.</summary>
    public bool CanRedo => _commandManager.CanRedo;

    /// <summary>
    /// Reverses the most recent edit. The dependency graph is rebuilt
    /// through the normal edit path, not from a cache.
    /// </summary>
    public CellChangeSet Undo() => _commandManager.Undo();

    /// <summary>
    /// Reapplies the most recently undone edit.
    /// </summary>
    public CellChangeSet Redo() => _commandManager.Redo();

    // ── Public API: data validation ────────────────────────────────

    /// <summary>
    /// Attaches rule to cellRef. Every future edit to that cell must
    /// produce a value satisfying rule, or the edit is rejected with
    /// CellChangeSet.ValidationFailed. Replaces any rule already there.
    /// Does not retroactively check the cell's current value.
    /// </summary>
    public void SetValidationRule(CellRef cellRef, IValidationRule rule) =>
        _validationRegistry.SetRule(cellRef, rule);

    /// <summary>Removes any validation rule attached to cellRef. A no-op if none was set.</summary>
    public void ClearValidationRule(CellRef cellRef) =>
        _validationRegistry.ClearRule(cellRef);

    // ── Public API: filtering (Group C feature) ─────────────────────

    /// <summary>
    /// Attaches filter to (range, column), replacing any filter already
    /// there. Filtering is a view operation: it never changes a cell's
    /// value, formula, or the dependency graph — only which rows
    /// GetVisibleRows reports. Recorded on the same undo stack as cell
    /// edits.
    /// </summary>
    public CellChangeSet SetFilter(CellRange range, int column, IRowFilter filter) =>
        _commandManager.ExecuteCommand(new ApplyFilterCommand(_filterManager, range, column, filter));

    /// <summary>Removes the filter at (range, column). A no-op if none was set.</summary>
    public CellChangeSet ClearFilter(CellRange range, int column) =>
        _commandManager.ExecuteCommand(new ApplyFilterCommand(_filterManager, range, column, null));

    /// <summary>
    /// Row numbers within range that satisfy every filter attached to
    /// range (AND across columns), in ascending order.
    /// </summary>
    public IReadOnlyList<int> GetVisibleRows(CellRange range) =>
        _filterManager.GetVisibleRows(range, _workbook);

    // ── Public API: sorting (Group C feature) ───────────────────────

    /// <summary>
    /// Sorts range's rows by keys (first key primary, later keys break
    /// ties), moving whole rows — every column of range travels with
    /// its row, not just the sort-key columns. hasHeader excludes
    /// range's first row from reordering.
    ///
    /// A moved formula has every cell reference inside it translated by
    /// that row's own move delta (RangeSorter Option B: Excel's actual
    /// move semantics, not copy semantics — see SortRangeCommand). A
    /// rejected write anywhere in the range (a validation rule, a
    /// cycle introduced by the move) rolls back the entire sort, the
    /// same all-or-nothing guarantee a single rejected edit gives.
    /// Applied as one undoable operation and one batched recalculation.
    /// </summary>
    public CellChangeSet SortRange(CellRange range, IReadOnlyList<SortKey> keys, bool hasHeader = false)
    {
        ArgumentNullException.ThrowIfNull(keys);
        if (keys.Count == 0)
            throw new ArgumentException("At least one sort key is required.", nameof(keys));
        foreach (var key in keys)
            if (key.Column < range.TopLeft.Column || key.Column > range.BottomRight.Column)
                throw new ArgumentException(
                    $"Sort key column {key.Column} is outside range {range}.", nameof(keys));

        return _commandManager.ExecuteCommand(new SortRangeCommand(this, range, keys, hasHeader));
    }

    // ── Public API: batching (Group C feature support) ──────────────

    /// <summary>
    /// Starts deferring recalculation: edits made through ApplyEdit
    /// while a batch is open update the workbook and dependency graph
    /// immediately (so cycle detection and validation still run per
    /// edit) but do not recompute dependents or notify observers until
    /// the matching EndBatch. Nestable — only the outermost EndBatch
    /// triggers the recompute pass. Bulk operations like SortRange use
    /// this so moving N cells costs one topological sort, not N.
    /// </summary>
    public void BeginBatch() => _batchDepth++;

    /// <summary>
    /// Ends the innermost open batch. If this was the outermost
    /// BeginBatch, recomputes every cell affected by any edit made
    /// during the batch — in one topological pass — and raises one
    /// change notification covering all of them.
    /// </summary>
    /// <exception cref="InvalidOperationException">No batch is open.</exception>
    public CellChangeSet EndBatch()
    {
        if (_batchDepth == 0)
            throw new InvalidOperationException("EndBatch called without a matching BeginBatch.");

        _batchDepth--;
        if (_batchDepth > 0)
            return CellChangeSet.Ok(default, Array.Empty<CellRef>());

        var roots = _batchRoots.ToList();
        _batchRoots.Clear();

        var affected = new HashSet<CellRef>(roots);
        foreach (var root in roots)
            foreach (var d in _graph.GetAffectedCells(root))
                affected.Add(d);

        var order = _graph.TopologicalSort(affected);
        foreach (var r in order)
        {
            var cell = _workbook.TryGet(r);
            if (cell is { IsFormula: true })
                cell.SetValue(cell.Tree!.Evaluate(_workbook));
        }

        var edited = roots.Count > 0 ? roots[0] : default;
        var changeSet = CellChangeSet.Ok(edited, order);
        _notifier.NotifyChanged(changeSet);
        return changeSet;
    }

    // ── Public API: bulk recalculation ─────────────────────────────

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

    // ── Public API: observer registration ──────────────────────────

    /// <summary>Registers an observer for future change notifications.</summary>
    public void Subscribe(ICellObserver observer) => _notifier.Subscribe(observer);

    /// <summary>Removes a previously registered observer.</summary>
    public void Unsubscribe(ICellObserver observer) => _notifier.Unsubscribe(observer);

    // ── Internal: the actual edit path ─────────────────────────────

    /// <summary>
    /// Applies a raw-input edit to a cell. This is the internal method
    /// that SetCellCommand calls — it does the same work that the old
    /// public SetCellContent did, but without creating a command (to
    /// avoid infinite recursion).
    ///
    /// An empty rawInput clears the cell (same behaviour as the old
    /// public ClearCell). A clear is never checked against a
    /// validation rule — CellValue.Empty is always an acceptable way
    /// to leave a cell, even one with a rule attached.
    /// </summary>
    internal CellChangeSet ApplyEdit(CellRef cellRef, string rawInput)
    {
        // Empty string → clear the cell.
        if (rawInput.Length == 0)
            return ApplyClear(cellRef);

        var parseResult = _parser.Parse(rawInput);
        if (!parseResult.Success)
            return CellChangeSet.ParseFailure(cellRef, string.Join("; ", parseResult.Errors));

        var deps = parseResult.IsFormula
            ? DependencyVisitor.GetDependencies(parseResult.Tree!)
            : Array.Empty<CellRef>();

        // Captured BEFORE SetDependencies below overwrites them — this
        // is the graph's actual prior state, the only thing a rollback
        // can correctly restore to. (Reading PrecedentsOf after the
        // accept-then-maybe-reject sequence below would return the new,
        // not-yet-committed deps instead, since SetDependencies has
        // already applied them by then.)
        var previousDeps = _graph.PrecedentsOf(cellRef).ToList();

        // Checked (and, if necessary, rolled back) before the workbook
        // is touched — a rejected edit must leave the cell exactly as
        // it was, per Design_Portfolio 4.5/6.3.
        var cycle = _graph.SetDependencies(cellRef, deps);
        if (cycle is not null)
        {
            _notifier.NotifyCircularReference(cycle);
            return CellChangeSet.Circular(cellRef, cycle);
        }

        // Evaluate the candidate value against the CURRENT workbook
        // state (the cell itself hasn't been written yet) so a
        // validation rule can check it before anything is committed.
        // If it fails, the dependency edges we just accepted above
        // must be rolled back too — SetDependencies has no memory of
        // "provisional", so we explicitly restore the old edges by
        // re-running it with the previous dependency set.
        var candidateValue = parseResult.Tree!.Evaluate(_workbook);
        var rule = _validationRegistry.GetRule(cellRef);
        if (rule is not null)
        {
            var validation = rule.Validate(candidateValue, _workbook);
            if (!validation.Success)
            {
                _graph.SetDependencies(cellRef, previousDeps);
                return CellChangeSet.ValidationFailed(cellRef, validation.ErrorMessage!);
            }
        }

        var cell = _workbook.GetOrCreate(cellRef);
        if (parseResult.IsFormula)
        {
            cell.SetFormula(rawInput, parseResult.Tree!);
            cell.SetValue(candidateValue);
        }
        else
        {
            cell.SetLiteral(rawInput, candidateValue);
        }

        if (_batchDepth > 0)
        {
            _batchRoots.Add(cellRef);
            return CellChangeSet.Ok(cellRef, new List<CellRef> { cellRef });
        }

        var changedCells = RecomputeAffected(cellRef);

        var changeSet = CellChangeSet.Ok(cellRef, changedCells);
        _notifier.NotifyChanged(changeSet);
        return changeSet;
    }

    /// <summary>
    /// Internal clear path — same as the old public ClearCell body.
    /// </summary>
    private CellChangeSet ApplyClear(CellRef cellRef)
    {
        _graph.SetDependencies(cellRef, Array.Empty<CellRef>());
        _workbook.Remove(cellRef);

        if (_batchDepth > 0)
        {
            _batchRoots.Add(cellRef);
            return CellChangeSet.Ok(cellRef, new List<CellRef> { cellRef });
        }

        var changedCells = RecomputeAffected(cellRef);

        var changeSet = CellChangeSet.Ok(cellRef, changedCells);
        _notifier.NotifyChanged(changeSet);
        return changeSet;
    }

    /// <summary>
    /// The cached parsed tree for cellRef, or null if it holds no
    /// formula (literal, empty, or absent). Used by SortRangeCommand to
    /// translate a moved formula's references without re-parsing it.
    /// </summary>
    internal IExpression? GetTree(CellRef cellRef) => _workbook.TryGet(cellRef)?.Tree;

    /// <summary>
    /// Recomputes every cell downstream of cellRef, in dependency
    /// order, and returns the full list of changed cells (cellRef
    /// first, then its dependents in the order they were recomputed).
    /// Shared by ApplyEdit and ApplyClear — both change a cell's
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