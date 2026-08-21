using CalcEngine.Core.Model;

namespace CalcEngine.Core.Expressions;

/// <summary>
/// Leaf: a rectangular cell range, e.g. B2:B45.
/// RI clause 4: appears only as a direct function argument,
/// never as an operand of a BinaryExpression or UnaryExpression.
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