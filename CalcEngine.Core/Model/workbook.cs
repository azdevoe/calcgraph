using CalcEngine.Core.Expressions;
using CalcEngine.Core.Functions;

namespace CalcEngine.Core.Model;

/// <summary>
/// The cells of a spreadsheet, addressed in A1 notation. Only cells that
/// have been written to take up any room; every other address simply reads
/// as empty.
///
/// Workbook is also the environment a formula is evaluated against. Cell
/// references, ranges, and function calls are all resolved through it, so
/// a formula such as SUM(A1:A5) reads the same live values a user sees in
/// the grid.
/// </summary>
public sealed class Workbook : IEvalContext
{
    private readonly Dictionary<CellRef, Cell> _cells = new();
    private readonly FunctionFactory _functions;

    /// <summary>
    /// Initializes an empty workbook with the eight built-in functions
    /// available to formulas.
    /// </summary>
    public Workbook() : this(FunctionFactory.CreateDefault())
    {
    }

    /// <summary>
    /// Initializes an empty workbook that resolves function calls through
    /// the factory you supply. Use this to add functions of your own.
    /// </summary>
    /// <param name="functions">The function library formulas will call into.</param>
    /// <exception cref="ArgumentNullException"><paramref name="functions"/> is null.</exception>
    public Workbook(FunctionFactory functions)
    {
        ArgumentNullException.ThrowIfNull(functions);
        _functions = functions;
    }

    /// <summary>Gets the number of occupied cells.</summary>
    public int Count => _cells.Count;

    /// <summary>
    /// Returns the cell at the given address, adding an empty one to the
    /// workbook first if that address does not have one yet.
    /// </summary>
    /// <param name="cellRef">The address to look up.</param>
    /// <returns>The cell now occupying that address.</returns>
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
    /// Returns the cell at the given address without adding anything to the
    /// workbook.
    /// </summary>
    /// <param name="cellRef">The address to look up.</param>
    /// <returns>The cell at that address, or null if the address is empty.</returns>
    public Cell? TryGet(CellRef cellRef) =>
        _cells.TryGetValue(cellRef, out var cell) ? cell : null;

    /// <summary>Empties the cell at the given address.</summary>
    /// <param name="cellRef">The address to empty.</param>
    /// <returns>
    /// true if a cell was removed; false if that address was already empty.
    /// </returns>
    public bool Remove(CellRef cellRef) => _cells.Remove(cellRef);

    /// <summary>Returns every occupied cell, in no particular order.</summary>
    /// <returns>The cells currently stored in the workbook.</returns>
    public IReadOnlyCollection<Cell> AllCells() => _cells.Values;

    // ── IEvalContext ─────────────────────────────────────────────────

    /// <summary>Returns the current value of a cell.</summary>
    /// <param name="cellRef">The address to read.</param>
    /// <returns>The cell's value, or CellValue.Empty if nothing is stored there.</returns>
    public CellValue GetCellValue(CellRef cellRef) =>
        TryGet(cellRef)?.Value ?? CellValue.Empty;

    /// <summary>Returns the current value of every cell in a range.</summary>
    /// <param name="range">The range to read.</param>
    /// <returns>
    /// The values in row-major order, matching the order of CellRange.GetCells.
    /// Empty cells appear as CellValue.Empty rather than being skipped.
    /// </returns>
    public IReadOnlyList<CellValue> GetRangeValues(CellRange range)
    {
        var values = new List<CellValue>(range.CellCount);
        foreach (var cellRef in range.GetCells())
            values.Add(GetCellValue(cellRef));
        return values;
    }

    /// <summary>
    /// Evaluates a function call against the values in this workbook.
    /// </summary>
    /// <param name="name">The function name, such as "SUM".</param>
    /// <param name="args">
    /// The arguments to the call, given as expressions rather than values so
    /// that a function such as IF can leave the branch it does not take
    /// unevaluated.
    /// </param>
    /// <returns>
    /// The result of the call, or an error value if the name is unknown or
    /// the arguments do not suit it.
    /// </returns>
    public CellValue CallFunction(string name, IReadOnlyList<IExpression> args) =>
        _functions.Evaluate(name, args, this);
}