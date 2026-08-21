using CalcEngine.Core.Model;

namespace CalcEngine.Core.Expressions;

/// <summary>
/// Leaf: a rectangular cell range, e.g. B2:B45.
///
/// The grammar's `atom` rule admits a bare RANGE anywhere any other
/// atom is legal (`atom : ... | RANGE # RangeAtom | ...`), so a range
/// can in fact appear as an operand of BinaryExpression or
/// UnaryExpression — `=B2:B3+1` parses successfully. There is no
/// parse-time restriction to a function-argument position; Evaluate
/// below is what turns that case into #VALUE!, the same way a real
/// spreadsheet reports "a range where a scalar was expected" as a
/// value, not a syntax error.
/// </summary>
public sealed class RangeExpression : IExpression
{
    /// <summary>Gets the range of cells this node covers.</summary>
    public CellRange Range { get; }

    /// <summary>Initializes a new reference to a range of cells.</summary>
    /// <param name="range">The range to cover.</param>
    public RangeExpression(CellRange range) => Range = range;

    /// <summary>
    /// Always reports an error, because a range of cells is not a single
    /// value. Functions such as SUM read the cells of a range themselves,
    /// so they never call this.
    /// </summary>
    /// <param name="context">Not used.</param>
    /// <returns>#VALUE!, the error for a value of the wrong shape.</returns>
    public CellValue Evaluate(IEvalContext context)
        => CellValue.FromError(ErrorKind.Value);

    /// <summary>Calls the visitor's VisitRange method.</summary>
    /// <typeparam name="T">The type of result the visitor produces.</typeparam>
    /// <param name="visitor">The visitor to call back into.</param>
    /// <returns>Whatever the visitor returns for this node.</returns>
    public T Accept<T>(IExpressionVisitor<T> visitor) => visitor.VisitRange(this);

    /// <summary>Returns a description of this node, for debugging.</summary>
    /// <returns>Text of the form Range(B2:B45).</returns>
    public override string ToString() => $"Range({Range})";
}