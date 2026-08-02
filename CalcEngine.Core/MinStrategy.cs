namespace CalcEngine.Core;

/// <summary>MIN(n1, n2, ...) — smallest of all arguments.</summary>
public sealed class MinStrategy : IFunctionStrategy
{
    public string Name => "MIN";
    public int MinArgs => 1;
    public int MaxArgs => int.MaxValue;

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