using CalcEngine.Core.Expressions;
using CalcEngine.Core.Model;

namespace CalcEngine.Core.Functions;

/// <summary>SUM(n1, n2, ...) — sums all arguments. Ranges are expanded.</summary>
public sealed class SumStrategy : IFunctionStrategy
{
    /// <summary>Gets the name that calls this function, "SUM".</summary>
    public string Name => "SUM";

    /// <summary>Gets the fewest arguments SUM accepts, which is 1.</summary>
    public int MinArgs => 1;

    /// <summary>Gets the most arguments SUM accepts, which is as many as you give it.</summary>
    public int MaxArgs => int.MaxValue;

    /// <summary>Adds up every value given to it.</summary>
    /// <param name="args">
    /// The values to add. A range counts as all the cells inside it.
    /// </param>
    /// <param name="context">Supplies the cell values the arguments read.</param>
    /// <returns>
    /// The total. Empty cells count as nothing, TRUE and FALSE count as 1 and
    /// 0, text gives #VALUE!, and a value that is already an error is passed
    /// straight through.
    /// </returns>
    public CellValue Evaluate(IReadOnlyList<IExpression> args, IEvalContext context)
    {
        double sum = 0;
        foreach (var value in FunctionHelpers.FlattenArgs(args, context))
        {
            if (value.Kind == ValueKind.Error) return value;
            if (value.Kind == ValueKind.Text) return CellValue.FromError(ErrorKind.Value);
            sum += value.AsNumber();
        }
        return CellValue.FromNumber(sum);
    }
}