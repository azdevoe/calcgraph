using CalcEngine.Core.Expressions;

namespace CalcEngine.Core.Model;

/// <summary>
/// One occupied slot in a Workbook. A cell is in exactly one of two
/// states — literal or formula — plus the special case of being empty
/// (RawInput == "", which Workbook treats as "not really here"; see
/// Workbook RI clause 3, Design_Portfolio 6.4).
///
/// Cell caches its parsed Tree so a formula is parsed once, at edit
/// time, and re-evaluated as many times as its precedents change
/// without ever being re-parsed (4.3). Cell itself never evaluates
/// Tree — that is the engine's job (Pass 2 / EvalVisitor); Cell only
/// stores what SetValue is told to store.
/// </summary>
public sealed class Cell
{
    public CellRef Ref { get; }

    /// <summary>The exact text the user typed, e.g. "42" or "=SUM(A1:A5)".</summary>
    public string RawInput { get; private set; } = string.Empty;

    /// <summary>Current value. Empty until a literal is set or a formula is evaluated.</summary>
    public CellValue Value { get; private set; } = CellValue.Empty;

    /// <summary>The parsed formula tree, or null if this cell is not a formula.</summary>
    public IExpression? Tree { get; private set; }

    /// <summary>True iff this cell currently holds a formula (Tree is non-null).</summary>
    public bool IsFormula => Tree is not null;

    public Cell(CellRef cellRef)
    {
        Ref = cellRef;
    }

    /// <summary>
    /// Sets this cell to a literal value: text, number, or boolean typed
    /// directly (no leading '='). Drops any previously cached formula
    /// tree — a cell cannot be a literal and a formula at once.
    /// </summary>
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