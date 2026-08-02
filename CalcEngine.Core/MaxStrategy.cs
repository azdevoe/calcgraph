namespace CalcEngine.Core;

/// <summary>MAX(n1, n2, ...) — largest of all arguments.</summary>
public sealed class MaxStrategy : IFunctionStrategy
{
    public string Name => "MAX";
    public int MinArgs => 1;
    public int MaxArgs => int.MaxValue;

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