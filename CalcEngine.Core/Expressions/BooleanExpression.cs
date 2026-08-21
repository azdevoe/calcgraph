using CalcEngine.Core.Model;

namespace CalcEngine.Core.Expressions;

/// <summary>Leaf: a boolean literal — TRUE or FALSE.</summary>
public sealed class BooleanExpression : IExpression
{
    /// <summary>Gets the value this literal stands for.</summary>
    public bool Value { get; }

    /// <summary>Initializes a new TRUE or FALSE literal.</summary>
    /// <param name="value">The value the literal stands for.</param>
    public BooleanExpression(bool value) => Value = value;

    /// <summary>Returns the value this literal stands for.</summary>
    /// <param name="context">Not used. A literal does not read any cells.</param>
    /// <returns>The literal's value.</returns>
    public CellValue Evaluate(IEvalContext context) => CellValue.FromBoolean(Value);

    /// <summary>Calls the visitor's VisitBoolean method.</summary>
    /// <typeparam name="T">The type of result the visitor produces.</typeparam>
    /// <param name="visitor">The visitor to call back into.</param>
    /// <returns>Whatever the visitor returns for this node.</returns>
    public T Accept<T>(IExpressionVisitor<T> visitor) => visitor.VisitBoolean(this);

    /// <summary>Returns a description of this node, for debugging.</summary>
    /// <returns>Text of the form Boolean(True).</returns>
    public override string ToString() => $"Boolean({Value})";
}