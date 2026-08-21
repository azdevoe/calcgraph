using CalcEngine.Core.Expressions;
using CalcEngine.Core.Model;

namespace CalcEngine.Core.Functions;

/// <summary>SUM(n1, n2, ...) — sums all arguments. Ranges are expanded.</summary>
public sealed class SumStrategy : IFunctionStrategy
{
    public string Name => "SUM";
    public int MinArgs => 1;
    public int MaxArgs => int.MaxValue;

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