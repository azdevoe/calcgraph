using CalcEngine.Core.ChangeTracking;
using CalcEngine.Core.Commands;
using CalcEngine.Core.Engine;
using CalcEngine.Core.Expressions;
using CalcEngine.Core.Model;
namespace CalcEngine.Core.Sorting;

/// <summary>
/// Reversible sort of range's rows by one or more column keys (Group C
/// feature: Sorting &amp; Filtering — RangeSorter Option B from the
/// project plan). Whole rows move together: every column of range is
/// carried along with the row, not just the sort-key columns.
///
/// A formula that moves has every cell reference inside it translated
/// by that row's own move delta (ReferenceTranslationVisitor); a
/// literal moves unchanged. The header row, if any, never moves.
///
/// All of it is applied through CalculationEngine.ApplyEdit — the same
/// path a normal edit takes — inside one BeginBatch/EndBatch pair, so
/// the dependency graph, cycle detection and validation all run
/// exactly as they do for a single-cell edit, and the whole range costs
/// one topological sort and one notification instead of one per cell.
///
/// The first Execute plans the sort (captures the prior raw content of
/// every cell in range, then computes the destination content); later
/// Execute/Undo calls just replay the two captured snapshots, so redo
/// after undo cannot drift from what was first computed.
/// </summary>
public sealed class SortRangeCommand : ICommand
{
    private readonly CalculationEngine _engine;
    private readonly CellRange _range;
    private readonly IReadOnlyList<SortKey> _keys;
    private readonly bool _hasHeader;

    private Dictionary<CellRef, string>? _before;
    private Dictionary<CellRef, string>? _after;

    public SortRangeCommand(CalculationEngine engine, CellRange range, IReadOnlyList<SortKey> keys, bool hasHeader)
    {
        _engine = engine;
        _range = range;
        _keys = keys;
        _hasHeader = hasHeader;
    }

    public CellChangeSet Execute()
    {
        if (_after is null)
        {
            try
            {
                (_before, _after) = Plan();
            }
            catch (ArgumentException ex)
            {
                // A moved formula's reference would fall outside the
                // sheet (row or column < 1) — e.g. a row referencing
                // the row above it moves to row 1. Excel would show
                // #REF! in that one cell; our raw-input round trip has
                // no way to write a bare error value into a cell, so
                // the whole sort is refused instead — malformed input
                // is normal input, and this must come back as a
                // rejected CellChangeSet, not an exception (nothing
                // was written yet: Plan() runs entirely before the
                // first ApplyEdit, so there is nothing to roll back).
                return CellChangeSet.ParseFailure(_range.TopLeft,
                    $"Sort refused: a moved formula would reference a cell outside the sheet ({ex.Message})");
            }
        }

        return Apply(_after!, fallback: _before!);
    }

    public CellChangeSet Undo() => Apply(_before!, fallback: _after!);

    // ── Planning ─────────────────────────────────────────────────

    private (Dictionary<CellRef, string> before, Dictionary<CellRef, string> after) Plan()
    {
        var dataRows = new List<int>();
        int firstDataRow = _hasHeader ? _range.TopLeft.Row + 1 : _range.TopLeft.Row;
        for (int r = firstDataRow; r <= _range.BottomRight.Row; r++)
            dataRows.Add(r);

        var before = new Dictionary<CellRef, string>();
        for (int r = firstDataRow; r <= _range.BottomRight.Row; r++)
            for (int c = _range.TopLeft.Column; c <= _range.BottomRight.Column; c++)
            {
                var cellRef = new CellRef(r, c);
                before[cellRef] = _engine.GetFormula(cellRef);
            }

        if (dataRows.Count <= 1)
            return (before, new Dictionary<CellRef, string>(before));

        var newOrder = RangeSorter.ComputeOrder(
            dataRows,
            _keys,
            (row, col) => _engine.GetValue(new CellRef(row, col)));

        var after = new Dictionary<CellRef, string>();
        for (int i = 0; i < dataRows.Count; i++)
        {
            int destRow = dataRows[i];
            int sourceRow = newOrder[i];
            int deltaRow = destRow - sourceRow;

            for (int c = _range.TopLeft.Column; c <= _range.BottomRight.Column; c++)
            {
                var sourceRef = new CellRef(sourceRow, c);
                var destRef = new CellRef(destRow, c);
                var rawInput = before[sourceRef];

                after[destRef] = deltaRow == 0 || !IsFormula(rawInput)
                    ? rawInput
                    : Translate(sourceRef, deltaRow);
            }
        }

        return (before, after);
    }

    private static bool IsFormula(string rawInput) => rawInput.Length > 0 && rawInput[0] == '=';

    private string Translate(CellRef sourceRef, int deltaRow)
    {
        var tree = _engine.GetTree(sourceRef);
        if (tree is null) return _engine.GetFormula(sourceRef); // defensive; should not happen for a formula cell
        var translated = ReferenceTranslationVisitor.Translate(tree, deltaRow, 0);
        return FormulaPrinter.Print(translated);
    }

    // ── Applying ─────────────────────────────────────────────────

    /// <summary>
    /// Writes every cell in content through ApplyEdit inside one batch.
    /// If any individual write is rejected (validation, cycle), every
    /// write already made in this call is reverted using fallback so
    /// the workbook is left exactly as it was before this Apply call —
    /// a rejected sort leaves no trace, the same guarantee a rejected
    /// single-cell edit gives.
    /// </summary>
    private CellChangeSet Apply(Dictionary<CellRef, string> content, Dictionary<CellRef, string> fallback)
    {
        _engine.BeginBatch();

        var applied = new List<CellRef>();
        CellChangeSet? failure = null;

        foreach (var (cellRef, rawInput) in content)
        {
            var result = _engine.ApplyEdit(cellRef, rawInput);
            if (!result.Success)
            {
                failure = result;
                break;
            }
            applied.Add(cellRef);
        }

        if (failure is not null)
        {
            foreach (var cellRef in applied)
                _engine.ApplyEdit(cellRef, fallback[cellRef]);

            _engine.EndBatch();
            return failure;
        }

        return _engine.EndBatch();
    }
}
