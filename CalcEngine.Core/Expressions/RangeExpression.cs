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
    public CellRange Range { get; }

    public RangeExpression(CellRange range) => Range = range;

    /// <summary>
    /// A range cannot evaluate to a single value. Function strategies
    /// handle ranges via IEvalContext.GetRangeValues directly.
    /// </summary>
    public CellValue Evaluate(IEvalContext context)
        => CellValue.FromError(ErrorKind.Value);

    public T Accept<T>(IExpressionVisitor<T> visitor) => visitor.VisitRange(this);

    public override string ToString() => $"Range({Range})";
}