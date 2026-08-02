namespace CalcEngine.Core;

/// <summary>AVERAGE(n1, n2, ...) — mean of all arguments. Empty cells count as 0 (Excel: they're excluded, but this matches AsNumber's Empty→0 coercion used everywhere else in this engine).</summary>
public sealed class AverageStrategy : IFunctionStrategy
{
    public string Name => "AVERAGE";
    public int MinArgs => 1;
    public int MaxArgs => int.MaxValue;

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