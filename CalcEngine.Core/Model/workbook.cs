using CalcEngine.Core.Expressions;
using CalcEngine.Core.Functions;

namespace CalcEngine.Core.Model;

/// <summary>
/// Sparse cell storage: Dictionary&lt;CellRef, Cell&gt;, not a 2D array
/// (Design_Portfolio 4.2). AF(w) = the partial function CellRef -&gt;
/// cell contents, where an absent key denotes an empty cell whose
/// value is CellValue.Empty (6.4).
///
/// Workbook also implements IEvalContext: it is the environment an
/// expression tree evaluates against, resolving cell references,
/// ranges, and function calls all through the same object so a
/// function like SUM(A1:A5) can call back into live cell values.
/// </summary>
public sealed class Workbook : IEvalContext
{
    private readonly Dictionary<CellRef, Cell> _cells = new();
    private readonly FunctionFactory _functions;

    public Workbook() : this(FunctionFactory.CreateDefault())
    {
    }

    public Workbook(FunctionFactory functions)
    {
        ArgumentNullException.ThrowIfNull(functions);
        _functions = functions;
    }

    /// <summary>Number of occupied cells.</summary>
    public int Count => _cells.Count;

    /// <summary>
    /// Returns the cell at ref, creating and storing an empty one first
    /// if none exists. Used on the write path, where the caller is
    /// about to populate the cell (SetLiteral/SetFormula).
    /// </summary>
    public Cell GetOrCreate(CellRef cellRef)
    {
        if (!_cells.TryGetValue(cellRef, out var cell))
        {
            cell = new Cell(cellRef);
            _cells[cellRef] = cell;
        }
        return cell;
    }

    /// <summary>
    /// Returns the stored cell at ref, or null if none exists. Does not
    /// create — used on the read path so evaluating a formula can never
    /// grow the workbook (Design_Portfolio 6.4).
    /// </summary>
    public Cell? TryGet(CellRef cellRef) =>
        _cells.TryGetValue(cellRef, out var cell) ? cell : null;

    /// <summary>Removes the entry at ref. Returns true if a cell was removed,
    /// false if none existed. Does not touch the dependency graph —
    /// the caller (CalculationEngine) sequences both.
    /// </summary>
    public bool Remove(CellRef cellRef) => _cells.Remove(cellRef);

    /// <summary>
    /// Every occupied cell, in no particular order. Used by
    /// CalculationEngine.RecalculateAll to find every formula cell
    /// without needing its own separate bookkeeping.
    /// </summary>
    public IReadOnlyCollection<Cell> AllCells() => _cells.Values;

    // ── IEvalContext ─────────────────────────────────────────────────

    /// <summary>Current value of the cell. An absent cell is CellValue.Empty.</summary>
    public CellValue GetCellValue(CellRef cellRef) =>
        TryGet(cellRef)?.Value ?? CellValue.Empty;

    /// <summary>Every value in the range, row-major order (matches CellRange.GetCells()).</summary>
    public IReadOnlyList<CellValue> GetRangeValues(CellRange range)
    {
        var values = new List<CellValue>(range.CellCount);
        foreach (var cellRef in range.GetCells())
            values.Add(GetCellValue(cellRef));
        return values;
    }

    /// <summary>
    /// Evaluates a function call by delegating to this Workbook's
    /// FunctionFactory, passing itself as the context so a function's
    /// arguments (cell refs, ranges, nested calls) resolve against the
    /// same live data.
    /// </summary>
    public CellValue CallFunction(string name, IReadOnlyList<IExpression> args) =>
        _functions.Evaluate(name, args, this);
}