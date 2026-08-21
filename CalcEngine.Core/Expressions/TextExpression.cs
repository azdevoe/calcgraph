using CalcEngine.Core.Model;

namespace CalcEngine.Core.Expressions;

/// <summary>Leaf: a string literal, e.g. "hello".</summary>
public sealed class TextExpression : IExpression
{
    public string Value { get; }

    public TextExpression(string value)
    {
        Value = value ?? throw new ArgumentNullException(nameof(value));
    }

    public CellValue Evaluate(IEvalContext context) => CellValue.FromText(Value);

    public T Accept<T>(IExpressionVisitor<T> visitor) => visitor.VisitText(this);

    public override string ToString() => $"Text(\"{Value}\")";
}