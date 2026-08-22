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
    /// <summary>
    /// Gets the name that calls this function in a formula, such as "SUM".
    /// Always upper case.
    /// </summary>
    string Name { get; }

    /// <summary>Gets the fewest arguments this function accepts.</summary>
    int MinArgs { get; }

    /// <summary>
    /// Gets the most arguments this function accepts. Use int.MaxValue for a
    /// function such as SUM that takes as many as you give it.
    /// </summary>
    int MaxArgs { get; }

    /// <summary>Works out the function's result.</summary>
    /// <param name="args">
    /// The arguments to the call, given as expressions rather than values so
    /// that a function such as IF can leave the branch it does not take
    /// unevaluated. The count is already known to be within MinArgs and
    /// MaxArgs.
    /// </param>
    /// <param name="context">Supplies the cell values the arguments read.</param>
    /// <returns>
    /// The function's result. A problem with the arguments comes back as an
    /// error value rather than an exception.
    /// </returns>
    CellValue Evaluate(IReadOnlyList<IExpression> args, IEvalContext context);
}