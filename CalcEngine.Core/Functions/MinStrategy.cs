using CalcEngine.Core.Expressions;
using CalcEngine.Core.Model;

namespace CalcEngine.Core.Functions;

/// <summary>MIN(n1, n2, ...) — smallest of all arguments.</summary>
public sealed class MinStrategy : IFunctionStrategy
{
    /// <summary>Gets the name that calls this function, "MIN".</summary>
    public string Name => "MIN";

    /// <summary>Gets the fewest arguments MIN accepts, which is 1.</summary>
    public int MinArgs => 1;

    /// <summary>Gets the most arguments MIN accepts, which is as many as you give it.</summary>
    public int MaxArgs => int.MaxValue;

    /// <summary>Finds the smallest of the values given to it.</summary>
    /// <param name="args">
    /// The values to compare. A range counts as all the cells inside it.
    /// </param>
    /// <param name="context">Supplies the cell values the arguments read.</param>
    /// <returns>
    /// The smallest value. Empty cells count as 0, TRUE and FALSE count as 1
    /// and 0, text gives #VALUE!, and a value that is already an error is
    /// passed straight through. #VALUE! if there was nothing to compare.
    /// </returns>
    public CellValue Evaluate(IReadOnlyList<IExpression> args, IEvalContext context)
    {
        double? min = null;
        foreach (var value in FunctionHelpers.FlattenArgs(args, context))
        {
            if (value.Kind == ValueKind.Error) return value;
            if (value.Kind == ValueKind.Text) return CellValue.FromError(ErrorKind.Value);
            double n = value.AsNumber();
            if (min is null || n < min) min = n;
        }
        return min is null
            ? CellValue.FromError(ErrorKind.Value)
            : CellValue.FromNumber(min.Value);
    }
}