using CalcEngine.Core.Expressions;
using CalcEngine.Core.Model;

namespace CalcEngine.Core.Functions;

/// <summary>
/// IF(condition, trueBranch, falseBranch). Must NOT pre-evaluate both
/// branches — the condition is evaluated first, then exactly one branch,
/// so a division-by-zero in the untaken branch never surfaces.
/// </summary>
public sealed class IfStrategy : IFunctionStrategy
{
    /// <summary>Gets the name that calls this function, "IF".</summary>
    public string Name => "IF";

    /// <summary>Gets the fewest arguments IF accepts, which is 3.</summary>
    public int MinArgs => 3;

    /// <summary>Gets the most arguments IF accepts, which is 3.</summary>
    public int MaxArgs => 3;

    /// <summary>Chooses between two values depending on a condition.</summary>
    /// <param name="args">
    /// Three arguments: the condition, the value to use when it holds, and
    /// the value to use when it does not.
    /// </param>
    /// <param name="context">Supplies the cell values the arguments read.</param>
    /// <returns>
    /// The second argument when the condition holds, and the third when it
    /// does not. Any non-zero condition counts as true. The argument that is
    /// not chosen has no bearing on the result, so IF(A1=0, 0, 10/A1) gives 0
    /// rather than #DIV/0! when A1 is zero. A condition that is text gives
    /// #VALUE!, and a condition that is itself an error is the result.
    /// </returns>
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