using CalcEngine.Core.Expressions;
using CalcEngine.Core.Model;

namespace CalcEngine.Core.Functions;

/// <summary>MAX(n1, n2, ...) — largest of all arguments.</summary>
public sealed class MaxStrategy : IFunctionStrategy
{
    /// <summary>Gets the name that calls this function, "MAX".</summary>
    public string Name => "MAX";

    /// <summary>Gets the fewest arguments MAX accepts, which is 1.</summary>
    public int MinArgs => 1;

    /// <summary>Gets the most arguments MAX accepts, which is as many as you give it.</summary>
    public int MaxArgs => int.MaxValue;

    /// <summary>Finds the largest of the values given to it.</summary>
    /// <param name="args">
    /// The values to compare. A range counts as all the cells inside it.
    /// </param>
    /// <param name="context">Supplies the cell values the arguments read.</param>
    /// <returns>
    /// The largest value. Empty cells count as 0, TRUE and FALSE count as 1
    /// and 0, text gives #VALUE!, and a value that is already an error is
    /// passed straight through. #VALUE! if there was nothing to compare.
    /// </returns>
    public CellValue Evaluate(IReadOnlyList<IExpression> args, IEvalContext context)
    {
        double? max = null;
        foreach (var value in FunctionHelpers.FlattenArgs(args, context))
        {
            if (value.Kind == ValueKind.Error) return value;
            if (value.Kind == ValueKind.Text) return CellValue.FromError(ErrorKind.Value);
            double n = value.AsNumber();
            if (max is null || n > max) max = n;
        }
        return max is null
            ? CellValue.FromError(ErrorKind.Value)
            : CellValue.FromNumber(max.Value);
    }
}