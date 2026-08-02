namespace CalcEngine.Core;

/// <summary>COUNT(n1, n2, ...) — number of numeric values among the arguments. Text and empty cells are not counted; errors still propagate.</summary>
public sealed class CountStrategy : IFunctionStrategy
{
    public string Name => "COUNT";
    public int MinArgs => 1;
    public int MaxArgs => int.MaxValue;

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