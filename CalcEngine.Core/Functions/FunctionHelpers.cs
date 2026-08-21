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