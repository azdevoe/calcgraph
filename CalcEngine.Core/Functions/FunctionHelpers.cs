using CalcEngine.Core.Expressions;
using CalcEngine.Core.Model;

namespace CalcEngine.Core.Functions;

/// <summary>
/// Shared helpers for aggregate function strategies (SUM, AVERAGE, MIN,
/// MAX, COUNT). Each argument is either a scalar expression or a
/// RangeExpression; this flattens both into one sequence of values.
/// </summary>
internal static class FunctionHelpers
{
    /// <summary>
    /// Reads the arguments of a call as one flat run of values, opening any
    /// range out into the cells it covers.
    /// </summary>
    /// <param name="args">The arguments to the call.</param>
    /// <param name="context">Supplies the cell values the arguments read.</param>
    /// <returns>
    /// The values in the order they were written, with each range's cells in
    /// row-major order. Empty cells appear rather than being skipped, so a
    /// function can decide for itself what to do with them.
    /// </returns>
    public static IEnumerable<CellValue> FlattenArgs(
        IReadOnlyList<IExpression> args, IEvalContext context)
    {
        foreach (var arg in args)
        {
            if (arg is RangeExpression rangeExpr)
            {
                foreach (var v in context.GetRangeValues(rangeExpr.Range))
                    yield return v;
            }
            else
            {
                yield return arg.Evaluate(context);
            }
        }
    }
}