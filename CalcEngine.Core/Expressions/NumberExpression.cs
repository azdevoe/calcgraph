using CalcEngine.Core.Model;

namespace CalcEngine.Core.Expressions;

/// <summary>Leaf: a numeric literal, e.g. 3.14 or 1.5e10.</summary>
public sealed class NumberExpression : IExpression
{
    public double Value { get; }

    public NumberExpression(double value) => Value = value;

    public CellValue Evaluate(IEvalContext context) => CellValue.FromNumber(Value);

    public T Accept<T>(IExpressionVisitor<T> visitor) => visitor.VisitNumber(this);

    public override string ToString() => $"Number({Value})";
}