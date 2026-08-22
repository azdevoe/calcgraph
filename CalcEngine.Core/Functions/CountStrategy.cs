using CalcEngine.Core.Expressions;
using CalcEngine.Core.Model;

namespace CalcEngine.Core.Functions;

/// <summary>COUNT(n1, n2, ...) — number of numeric values among the arguments. Text and empty cells are not counted; errors still propagate.</summary>
public sealed class CountStrategy : IFunctionStrategy
{
    /// <summary>Gets the name that calls this function, "COUNT".</summary>
    public string Name => "COUNT";

    /// <summary>Gets the fewest arguments COUNT accepts, which is 1.</summary>
    public int MinArgs => 1;

    /// <summary>Gets the most arguments COUNT accepts, which is as many as you give it.</summary>
    public int MaxArgs => int.MaxValue;

    /// <summary>Counts how many of the values given to it are numbers.</summary>
    /// <param name="args">
    /// The values to count. A range counts as all the cells inside it.
    /// </param>
    /// <param name="context">Supplies the cell values the arguments read.</param>
    /// <returns>
    /// How many values were numbers, counting TRUE and FALSE as numbers too.
    /// Text and empty cells are not counted, so counting an empty range gives
    /// 0 rather than an error. A value that is already an error is passed
    /// straight through.
    /// </returns>
    public CellValue Evaluate(IReadOnlyList<IExpression> args, IEvalContext context)
    {
        int count = 0;
        foreach (var value in FunctionHelpers.FlattenArgs(args, context))
        {
            if (value.Kind == ValueKind.Error) return value;
            if (value.Kind == ValueKind.Number || value.Kind == ValueKind.Boolean)
                count++;
        }
        return CellValue.FromNumber(count);
    }
}