using CalcEngine.Core.Expressions;
using CalcEngine.Core.Model;

namespace CalcEngine.Core.Functions;

/// <summary>
/// Resolves a function name to a strategy at evaluation time.
/// Registering a function is the only step needed to add one.
/// An unknown name is not a parse error — the grammar accepts any
/// FUNCNAME — so resolution failure here is what produces #NAME?.
/// </summary>
public sealed class FunctionFactory
{
    private readonly Dictionary<string, IFunctionStrategy> _strategies = new();

    /// <summary>
    /// Adds a function to the library, so formulas can call it by name.
    /// Registering a function whose name is already taken replaces it.
    /// </summary>
    /// <param name="strategy">The function to add.</param>
    /// <exception cref="ArgumentNullException"><paramref name="strategy"/> is null.</exception>
    public void Register(IFunctionStrategy strategy)
    {
        ArgumentNullException.ThrowIfNull(strategy);
        _strategies[strategy.Name] = strategy;
    }

    /// <summary>Calls a function by name and returns its result.</summary>
    /// <param name="name">The function name, such as "SUM".</param>
    /// <param name="args">The arguments to the call.</param>
    /// <param name="context">Supplies the cell values the arguments read.</param>
    /// <returns>
    /// The function's result. A name that has not been registered gives
    /// #NAME?, and the wrong number of arguments gives #VALUE!.
    /// </returns>
    public CellValue Evaluate(string name, IReadOnlyList<IExpression> args, IEvalContext context)
    {
        if (!_strategies.TryGetValue(name, out var strategy))
            return CellValue.FromError(ErrorKind.Name);

        if (args.Count < strategy.MinArgs || args.Count > strategy.MaxArgs)
            return CellValue.FromError(ErrorKind.Value);

        return strategy.Evaluate(args, context);
    }

    /// <summary>
    /// Creates a library holding the eight built-in functions: SUM, AVERAGE,
    /// MIN, MAX, COUNT, IF, ROUND and LOOKUP.
    /// </summary>
    /// <returns>A library ready to use, which you can add further functions to.</returns>
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