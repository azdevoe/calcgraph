namespace CalcEngine.Core;

/// <summary>
/// Branch: a function call, e.g. SUM(A1:B5) or IF(A1>5, TRUE, FALSE).
/// Args are stored unevaluated so IF can short-circuit.
/// </summary>
public sealed class FunctionExpression : IExpression
{
    public string Name { get; }
    public IReadOnlyList<IExpression> Args { get; }

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

    public CellValue Evaluate(IEvalContext context)
        => context.CallFunction(Name, Args);

    public T Accept<T>(IExpressionVisitor<T> visitor) => visitor.VisitFunction(this);

    public override string ToString()
        => $"Function({Name}, [{string.Join(", ", Args)}])";
}