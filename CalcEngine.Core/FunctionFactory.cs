namespace CalcEngine.Core;

/// <summary>
/// Resolves a function name to a strategy at evaluation time.
/// Registering a function is the only step needed to add one.
/// An unknown name is not a parse error — the grammar accepts any
/// FUNCNAME — so resolution failure here is what produces #NAME?.
/// </summary>
public sealed class FunctionFactory
{
    private readonly Dictionary<string, IFunctionStrategy> _strategies = new();

    public void Register(IFunctionStrategy strategy)
    {
        ArgumentNullException.ThrowIfNull(strategy);
        _strategies[strategy.Name] = strategy;
    }

    /// <summary>
    /// Evaluates a function call. Returns #NAME? if unregistered,
    /// #VALUE! if the argument count is outside [MinArgs, MaxArgs].
    /// </summary>
    public CellValue Evaluate(string name, IReadOnlyList<IExpression> args, IEvalContext context)
    {
        if (!_strategies.TryGetValue(name, out var strategy))
            return CellValue.FromError(ErrorKind.Name);

        if (args.Count < strategy.MinArgs || args.Count > strategy.MaxArgs)
            return CellValue.FromError(ErrorKind.Value);

        return strategy.Evaluate(args, context);
    }

    /// <summary>
    /// Builds a factory with all eight required functions registered.
    /// SUM, AVERAGE, MIN, MAX, COUNT, IF, ROUND, LOOKUP.
    /// </summary>
    public static FunctionFactory CreateDefault()
    {
        var factory = new FunctionFactory();
        factory.Register(new SumStrategy());
        factory.Register(new AverageStrategy());
        factory.Register(new MinStrategy());
        factory.Register(new MaxStrategy());
        factory.Register(new CountStrategy());
        factory.Register(new IfStrategy());
        factory.Register(new RoundStrategy());
        factory.Register(new LookupStrategy());
        return factory;
    }
}