using CalcEngine.Core.Model;

namespace CalcEngine.Core.Expressions;

/// <summary>Leaf: a numeric literal, e.g. 3.14 or 1.5e10.</summary>
public sealed class NumberExpression : IExpression
{
    /// <summary>Gets the number this literal stands for.</summary>
    public double Value { get; }

    /// <summary>Initializes a new numeric literal.</summary>
    /// <param name="value">The number the literal stands for.</param>
    public NumberExpression(double value) => Value = value;

    /// <summary>Returns the number this literal stands for.</summary>
    /// <param name="context">Not used. A literal does not read any cells.</param>
    /// <returns>The literal's value.</returns>
    public CellValue Evaluate(IEvalContext context) => CellValue.FromNumber(Value);

    /// <summary>Calls the visitor's VisitNumber method.</summary>
    /// <typeparam name="T">The type of result the visitor produces.</typeparam>
    /// <param name="visitor">The visitor to call back into.</param>
    /// <returns>Whatever the visitor returns for this node.</returns>
    public T Accept<T>(IExpressionVisitor<T> visitor) => visitor.VisitNumber(this);

    /// <summary>Returns a description of this node, for debugging.</summary>
    /// <returns>Text of the form Number(3.14).</returns>
    public override string ToString() => $"Number({Value})";
}