namespace CalcEngine.Core;

/// <summary>
/// IF(condition, trueBranch, falseBranch). Must NOT pre-evaluate both
/// branches — the condition is evaluated first, then exactly one branch,
/// so a division-by-zero in the untaken branch never surfaces.
/// </summary>
public sealed class IfStrategy : IFunctionStrategy
{
    public string Name => "IF";
    public int MinArgs => 3;
    public int MaxArgs => 3;

    public CellValue Evaluate(IReadOnlyList<IExpression> args, IEvalContext context)
    {
        var condition = args[0].Evaluate(context);
        if (condition.Kind == ValueKind.Error) return condition;
        if (condition.Kind == ValueKind.Text) return CellValue.FromError(ErrorKind.Value);

        // Truthiness matches AsNumber's coercion: nonzero is true.
        bool isTrue = condition.AsNumber() != 0;
        return isTrue ? args[1].Evaluate(context) : args[2].Evaluate(context);
    }
}