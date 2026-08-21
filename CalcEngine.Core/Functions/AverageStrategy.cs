using CalcEngine.Core.Expressions;
using CalcEngine.Core.Model;

namespace CalcEngine.Core.Functions;

/// <summary>AVERAGE(n1, n2, ...) — mean of all arguments. Empty cells count as 0 (Excel: they're excluded, but this matches AsNumber's Empty→0 coercion used everywhere else in this engine).</summary>
public sealed class AverageStrategy : IFunctionStrategy
{
    /// <summary>Gets the name that calls this function, "AVERAGE".</summary>
    public string Name => "AVERAGE";

    /// <summary>Gets the fewest arguments AVERAGE accepts, which is 1.</summary>
    public int MinArgs => 1;

    /// <summary>Gets the most arguments AVERAGE accepts, which is as many as you give it.</summary>
    public int MaxArgs => int.MaxValue;

    /// <summary>Works out the mean of every value given to it.</summary>
    /// <param name="args">
    /// The values to average. A range counts as all the cells inside it.
    /// </param>
    /// <param name="context">Supplies the cell values the arguments read.</param>
    /// <returns>
    /// The mean. Empty cells count as 0 and are included in the division,
    /// TRUE and FALSE count as 1 and 0, text gives #VALUE!, and a value that
    /// is already an error is passed straight through. Averaging nothing at
    /// all gives #DIV/0!.
    /// </returns>
    public CellValue Evaluate(IReadOnlyList<IExpression> args, IEvalContext context)
    {
        double sum = 0;
        int count = 0;
        foreach (var value in FunctionHelpers.FlattenArgs(args, context))
        {
            if (value.Kind == ValueKind.Error) return value;
            if (value.Kind == ValueKind.Text) return CellValue.FromError(ErrorKind.Value);
            sum += value.AsNumber();
            count++;
        }
        if (count == 0) return CellValue.FromError(ErrorKind.DivideByZero);
        return CellValue.FromNumber(sum / count);
    }
}