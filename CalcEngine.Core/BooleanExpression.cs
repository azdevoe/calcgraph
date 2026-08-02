namespace CalcEngine.Core;

/// <summary>Leaf: a boolean literal — TRUE or FALSE.</summary>
public sealed class BooleanExpression : IExpression
{
    public bool Value { get; }

    public BooleanExpression(bool value) => Value = value;

    public CellValue Evaluate(IEvalContext context) => CellValue.FromBoolean(Value);

    public T Accept<T>(IExpressionVisitor<T> visitor) => visitor.VisitBoolean(this);

    public override string ToString() => $"Boolean({Value})";
}