using CalcEngine.Core.Expressions;
using CalcEngine.Core.Model;

namespace CalcEngine.Core.Functions;

/// <summary>
/// One spreadsheet function (SUM, IF, ROUND, ...) behind a common interface.
/// MinArgs/MaxArgs let arity be validated uniformly rather than inside
/// each implementation. Args are unevaluated IExpression — most strategies
/// evaluate all of them, but IF must not, so the interface has to allow it.
/// </summary>
public interface IFunctionStrategy
{
    /// <summary>The function name, e.g. "SUM". Always uppercase.</summary>
    string Name { get; }

    /// <summary>Fewest arguments this function accepts.</summary>
    int MinArgs { get; }

    /// <summary>Most arguments this function accepts. int.MaxValue for unbounded.</summary>
    int MaxArgs { get; }

    /// <summary>
    /// Computes the function's result. Arity has already been validated
    /// by FunctionFactory before this is called.
    /// </summary>
    CellValue Evaluate(IReadOnlyList<IExpression> args, IEvalContext context);
}