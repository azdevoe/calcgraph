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
/// The public face of the engine, and the only type a client needs to
/// touch. Everything a spreadsheet has to do is reached from here:
/// entering values and formulas, reading results back, undo and redo,
/// validation, sorting and filtering.
///
/// Every operation that changes something returns a CellChangeSet. A
/// successful edit, a formula that could not be read, a circular
/// reference, and a value turned away by a validation rule all come back
/// the same way, as data. A client never has to catch anything.
///
/// An operation that is refused changes nothing at all: the values, the
/// recorded dependencies, and the undo history are left exactly as they
/// were, and nothing is reported to observers.
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

    /// <summary>Puts a value or a formula into a cell.</summary>
    /// <param name="cellRef">The cell to fill in.</param>
    /// <param name="rawInput">
    /// What the user typed. Text beginning with "=" is treated as a formula;
    /// anything else is a plain value. An empty string empties the cell.
    /// </param>
    /// <returns>
    /// On success, every cell whose value changed, the edited cell included,
    /// with cells that read others already brought up to date. The edit can
    /// then be undone.
    ///
    /// The edit is refused, and nothing changes, if the text cannot be read,
    /// if it would make the cell depend on itself, or if the resulting value
    /// breaks a validation rule set on the cell. The result says which of
    /// those it was, and a refused edit cannot be undone, since it never
    /// happened.
    /// </returns>
    public CellChangeSet SetCellContent(CellRef cellRef, string rawInput)
    {
        var cmd = new SetCellCommand(this, cellRef, rawInput);
        return _commandManager.ExecuteCommand(cmd);
    }

    /// <summary>Empties a cell.</summary>
    /// <param name="cellRef">The cell to empty. Emptying an empty cell is harmless.</param>
    /// <returns>
    /// Every cell whose value changed. Formulas that read the emptied cell
    /// now see it as blank, exactly as they would a cell nobody ever filled
    /// in. The clear can be undone.
    /// </returns>
    public CellChangeSet ClearCell(CellRef cellRef)
    {
        // Capture the current raw input so undo can restore it.
        // Then issue a command that sets the cell to "" (empty),
        // which ApplyEdit handles by clearing.
        var cmd = new SetCellCommand(this, cellRef, string.Empty);
        return _commandManager.ExecuteCommand(cmd);
    }

    // ── Public API: querying ──────────────────────────────────────

    /// <summary>Returns what a cell currently works out to.</summary>
    /// <param name="cellRef">The cell to read.</param>
    /// <returns>
    /// The cell's value, which for a formula is its latest result. A cell
    /// that was never filled in reads as empty rather than failing.
    /// </returns>
    public CellValue GetValue(CellRef cellRef) => _workbook.GetCellValue(cellRef);

    /// <summary>Returns what was typed into a cell, rather than what it works out to.</summary>
    /// <param name="cellRef">The cell to read.</param>
    /// <returns>
    /// The text last put into the cell, still in its original form, so a
    /// formula comes back as the formula and not as its result. An empty
    /// string if the cell was never filled in. This is what a formula bar
    /// should show.
    /// </returns>
    public string GetFormula(CellRef cellRef) => _workbook.TryGet(cellRef)?.RawInput ?? string.Empty;

    // ── Public API: undo / redo ────────────────────────────────────

    /// <summary>Gets a value indicating whether there is anything to undo.</summary>
    /// <value>true if at least one operation can be undone; otherwise, false.</value>
    public bool CanUndo => _commandManager.CanUndo;

    /// <summary>Gets a value indicating whether there is anything to redo.</summary>
    /// <value>true if at least one undone operation can be reapplied; otherwise, false.</value>
    public bool CanRedo => _commandManager.CanRedo;

    /// <summary>Reverses the most recent operation.</summary>
    /// <returns>
    /// Every cell whose value changed. Cells that read the ones put back are
    /// brought up to date along with them. At least the last 100 operations
    /// can be undone this way.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// There is nothing to undo. Check CanUndo first.
    /// </exception>
    public CellChangeSet Undo() => _commandManager.Undo();

    /// <summary>Carries out the most recently undone operation again.</summary>
    /// <returns>
    /// Every cell whose value changed. Making a fresh edit discards anything
    /// that was waiting to be redone.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// There is nothing to redo. Check CanRedo first.
    /// </exception>
    public CellChangeSet Redo() => _commandManager.Redo();

    // ── Public API: data validation ────────────────────────────────

    /// <summary>
    /// Sets a rule that every future value of a cell must satisfy, replacing
    /// any rule already on it.
    /// </summary>
    /// <param name="cellRef">The cell to guard.</param>
    /// <param name="rule">The rule its values must satisfy.</param>
    /// <remarks>
    /// The value already in the cell is not checked, so setting a rule never
    /// turns an existing entry into an invalid one. Only edits made from now
    /// on are judged against it.
    /// </remarks>
    public void SetValidationRule(CellRef cellRef, IValidationRule rule) =>
        _validationRegistry.SetRule(cellRef, rule);

    /// <summary>
    /// Takes the validation rule off a cell. Clearing a cell that has no rule
    /// is harmless.
    /// </summary>
    /// <param name="cellRef">The cell to stop guarding.</param>
    public void ClearValidationRule(CellRef cellRef) =>
        _validationRegistry.ClearRule(cellRef);

    // ── Public API: filtering (Group C feature) ─────────────────────

    /// <summary>
    /// Filters a range on one of its columns, replacing any filter already on
    /// that column.
    /// </summary>
    /// <param name="range">The range to filter.</param>
    /// <param name="column">The column within that range to filter on.</param>
    /// <param name="filter">The test a row's value must pass.</param>
    /// <returns>
    /// A successful result reporting no changed cells. Filtering decides only
    /// which rows are worth showing; no value, formula or total is affected,
    /// so a SUM over a filtered range still counts the hidden rows. The
    /// filter can be undone like any other operation.
    /// </returns>
    public CellChangeSet SetFilter(CellRange range, int column, IRowFilter filter) =>
        _commandManager.ExecuteCommand(new ApplyFilterCommand(_filterManager, range, column, filter));

    /// <summary>Removes the filter from a column of a range.</summary>
    /// <param name="range">The range to change.</param>
    /// <param name="column">The column to stop filtering on.</param>
    /// <returns>
    /// A successful result reporting no changed cells. Clearing a filter that
    /// was never set is harmless.
    /// </returns>
    public CellChangeSet ClearFilter(CellRange range, int column) =>
        _commandManager.ExecuteCommand(new ApplyFilterCommand(_filterManager, range, column, null));

    /// <summary>Returns which rows of a range are still worth showing.</summary>
    /// <param name="range">The range to examine.</param>
    /// <returns>
    /// The row numbers that pass every filter set on the range, in ascending
    /// order. A row must pass all of them, so filters on different columns
    /// narrow the result together. A range with no filters gives all of its
    /// rows.
    /// </returns>
    public IReadOnlyList<int> GetVisibleRows(CellRange range) =>
        _filterManager.GetVisibleRows(range, _workbook);

    // ── Public API: sorting (Group C feature) ───────────────────────

    /// <summary>Reorders the rows of a range.</summary>
    /// <param name="range">The range whose rows are to be reordered.</param>
    /// <param name="keys">
    /// The columns to sort on, most important first. Rows that tie on the
    /// first are settled by the second, and so on. Every column must lie
    /// within the range.
    /// </param>
    /// <param name="hasHeader">
    /// true to leave the range's first row where it is; false to sort every
    /// row.
    /// </param>
    /// <returns>
    /// Every cell whose value changed. Whole rows travel together, so a row's
    /// other columns stay alongside the value that was sorted on, and a
    /// formula that moves has the references inside it shifted by the same
    /// distance the row moved: a row carrying =A1+1 that moves from row 5 to
    /// row 10 ends up holding =A6+1. Rows that tie on every key keep the
    /// order they were already in.
    ///
    /// The sort is refused as a whole, leaving the range exactly as it was,
    /// if any shifted reference would fall off the sheet, or if any of the
    /// resulting values would break a validation rule or make a cell depend
    /// on itself. The whole sort undoes as one operation.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="keys"/> is null.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="keys"/> is empty, or names a column outside the range.
    /// </exception>
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
    /// Begins a run of edits that should settle together rather than one at a
    /// time.
    /// </summary>
    /// <remarks>
    /// Edits made while a batch is open still take effect, and are still
    /// checked for circular references and against validation rules, one by
    /// one. What waits until the batch closes is bringing the rest of the
    /// workbook up to date and telling observers about it, which then happens
    /// once for the whole run instead of once per edit. Batches may be nested,
    /// and each call must be matched by a call to EndBatch.
    /// </remarks>
    public void BeginBatch() => _batchDepth++;

    /// <summary>Closes the innermost open batch.</summary>
    /// <returns>
    /// When this closes the last open batch, every cell whose value changed
    /// over the whole run, reported to observers as a single change. When
    /// batches are still open inside it, an empty result, since nothing has
    /// settled yet.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// No batch is open, meaning this call has no matching BeginBatch.
    /// </exception>
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

    /// <summary>Works out every formula in the workbook again from scratch.</summary>
    /// <remarks>
    /// Each formula is given values that are already up to date, so the
    /// results are the same as if the whole workbook had been entered afresh.
    /// Observers are not told about any of it: this is meant for moments such
    /// as just after loading a file, where there is no single edit to report
    /// and the client already knows to redraw everything.
    /// </remarks>
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

    /// <summary>
    /// Signs an observer up to be told whenever cells change, so that a grid
    /// can keep itself up to date without asking after every edit.
    /// </summary>
    /// <param name="observer">The observer to sign up.</param>
    /// <remarks>
    /// Signing the same observer up twice makes no difference; it is still
    /// told once per change. Only changes made from now on are reported.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="observer"/> is null.</exception>
    public void Subscribe(ICellObserver observer) => _notifier.Subscribe(observer);

    /// <summary>
    /// Stops telling an observer about changes. Removing one that was never
    /// signed up is harmless.
    /// </summary>
    /// <param name="observer">The observer to remove.</param>
    /// <exception cref="ArgumentNullException"><paramref name="observer"/> is null.</exception>
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