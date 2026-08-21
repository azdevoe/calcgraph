using CalcEngine.Core.Model;

namespace CalcEngine.Core.Expressions;

/// <summary>
/// Branch: a function call, e.g. SUM(A1:B5) or IF(A1>5, TRUE, FALSE).
/// Args are stored unevaluated so IF can short-circuit.
/// </summary>
public sealed class FunctionExpression : IExpression
{
    /// <summary>Gets the function name, always in upper case.</summary>
    public string Name { get; }

    /// <summary>Gets the arguments to the call, in the order they were written.</summary>
    public IReadOnlyList<IExpression> Args { get; }

    /// <summary>Initializes a new function call.</summary>
    /// <param name="name">
    /// The function name. Case does not matter; it is upper-cased for you.
    /// </param>
    /// <param name="args">The arguments to the call. May be empty, but not null.</param>
    /// <exception cref="ArgumentException">
    /// <paramref name="name"/> is empty, or one of the arguments is null.
    /// </exception>
    /// <exception cref="ArgumentNullException"><paramref name="args"/> is null.</exception>
    public FunctionExpression(string name, IReadOnlyList<IExpression> args)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentException("Function name must not be empty.", nameof(name));
        Name = name.ToUpperInvariant();
        Args = args ?? throw new ArgumentNullException(nameof(args));

        for (int i = 0; i < Args.Count; i++)
            if (Args[i] is null)
                throw new ArgumentException($"Argument at index {i} is null.", nameof(args));
    }

    /// <summary>Calls the function and returns its result.</summary>
    /// <param name="context">Supplies the cell values and the function library.</param>
    /// <returns>
    /// Whatever the function returns. An unknown function name gives #NAME?,
    /// and the wrong number of arguments gives #VALUE!.
    /// </returns>
    public CellValue Evaluate(IEvalContext context)
        => context.CallFunction(Name, Args);

    /// <summary>Calls the visitor's VisitFunction method.</summary>
    /// <typeparam name="T">The type of result the visitor produces.</typeparam>
    /// <param name="visitor">The visitor to call back into.</param>
    /// <returns>Whatever the visitor returns for this node.</returns>
    public T Accept<T>(IExpressionVisitor<T> visitor) => visitor.VisitFunction(this);

    /// <summary>Returns a description of this node, for debugging.</summary>
    /// <returns>Text of the form Function(SUM, [arguments]).</returns>
    public override string ToString()
        => $"Function({Name}, [{string.Join(", ", Args)}])";
}