using CalcEngine.Core.Model;

namespace CalcEngine.Core.Expressions;

/// <summary>Leaf: a string literal, e.g. "hello".</summary>
public sealed class TextExpression : IExpression
{
    /// <summary>Gets the text this literal stands for.</summary>
    public string Value { get; }

    /// <summary>Initializes a new text literal.</summary>
    /// <param name="value">The text the literal stands for.</param>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is null.</exception>
    public TextExpression(string value)
    {
        Value = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>Returns the text this literal stands for.</summary>
    /// <param name="context">Not used. A literal does not read any cells.</param>
    /// <returns>The literal's value.</returns>
    public CellValue Evaluate(IEvalContext context) => CellValue.FromText(Value);

    /// <summary>Calls the visitor's VisitText method.</summary>
    /// <typeparam name="T">The type of result the visitor produces.</typeparam>
    /// <param name="visitor">The visitor to call back into.</param>
    /// <returns>Whatever the visitor returns for this node.</returns>
    public T Accept<T>(IExpressionVisitor<T> visitor) => visitor.VisitText(this);

    /// <summary>Returns a description of this node, for debugging.</summary>
    /// <returns>Text of the form Text("hello").</returns>
    public override string ToString() => $"Text(\"{Value}\")";
}