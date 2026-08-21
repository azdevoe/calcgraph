using CalcEngine.Core.Expressions;

namespace CalcEngine.Core.Model;

/// <summary>
/// One occupied slot in a Workbook. A cell is in exactly one of two
/// states, literal or formula, plus the special case of being empty,
/// which the workbook treats as not really being there at all.
///
/// Cell caches its parsed Tree so a formula is parsed once, at edit
/// time, and re-evaluated as many times as its precedents change
/// without ever being re-parsed. Cell itself never evaluates Tree;
/// that is the engine's job. Cell only stores what SetValue is told
/// to store.
/// </summary>
public sealed class Cell
{
    /// <summary>Gets the address this cell sits at.</summary>
    public CellRef Ref { get; }

    /// <summary>The exact text the user typed, e.g. "42" or "=SUM(A1:A5)".</summary>
    public string RawInput { get; private set; } = string.Empty;

    /// <summary>Current value. Empty until a literal is set or a formula is evaluated.</summary>
    public CellValue Value { get; private set; } = CellValue.Empty;

    /// <summary>The parsed formula tree, or null if this cell is not a formula.</summary>
    public IExpression? Tree { get; private set; }

    /// <summary>True iff this cell currently holds a formula (Tree is non-null).</summary>
    public bool IsFormula => Tree is not null;

    /// <summary>Initializes a new, empty cell at the given address.</summary>
    /// <param name="cellRef">The address the cell will occupy.</param>
    public Cell(CellRef cellRef)
    {
        Ref = cellRef;
    }

    /// <summary>
    /// Sets this cell to a literal value: text, number, or boolean typed
    /// directly (no leading '='). Drops any previously cached formula
    /// tree — a cell cannot be a literal and a formula at once.
    /// </summary>
    /// <param name="rawInput">The text the user typed, such as "42".</param>
    /// <param name="value">The value that text represents.</param>
    public void SetLiteral(string rawInput, CellValue value)
    {
        RawInput = rawInput;
        Value = value;
        Tree = null;
    }

    /// <summary>
    /// Sets this cell to a formula: caches the raw text and the already-
    /// parsed tree. Does NOT evaluate — Value is left as-is until the
    /// engine calls SetValue with the result of Pass 2.
    /// </summary>
    /// <param name="rawInput">The formula text the user typed, such as "=SUM(A1:A5)".</param>
    /// <param name="tree">The already-parsed tree for that text.</param>
    /// <exception cref="ArgumentNullException"><paramref name="tree"/> is null.</exception>
    public void SetFormula(string rawInput, IExpression tree)
    {
        ArgumentNullException.ThrowIfNull(tree);
        RawInput = rawInput;
        Tree = tree;
    }

    /// <summary>
    /// Updates Value only. Used by the engine after (re)evaluating this
    /// cell's Tree, or after a literal's value needs refreshing without
    /// touching RawInput/Tree. Never called by Cell itself.
    /// </summary>
    /// <param name="value">The value to store.</param>
    public void SetValue(CellValue value)
    {
        Value = value;
    }

    /// <summary>
    /// Resets this cell to its empty state. The caller (Workbook) is
    /// responsible for then removing the entry entirely so the sparse
    /// invariant holds — Clear only resets the Cell's own fields.
    /// </summary>
    public void Clear()
    {
        RawInput = string.Empty;
        Value = CellValue.Empty;
        Tree = null;
    }
}