using CalcEngine.Core.Model;

namespace CalcEngine.Core.Expressions;

/// <summary>
/// Branch: a binary operator applied to two operands.
/// Error model: first error wins, div-by-zero yields #DIV/0!,
/// text in arithmetic yields #VALUE!.
/// </summary>
public sealed class BinaryExpression : IExpression
{
    /// <summary>Gets the operand on the left of the operator.</summary>
    public IExpression Left { get; }

    /// <summary>Gets the operator being applied.</summary>
    public BinaryOperator Op { get; }

    /// <summary>Gets the operand on the right of the operator.</summary>
    public IExpression Right { get; }

    /// <summary>Initializes a new operator node.</summary>
    /// <param name="left">The operand on the left.</param>
    /// <param name="op">The operator to apply.</param>
    /// <param name="right">The operand on the right.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="left"/> or <paramref name="right"/> is null.
    /// </exception>
    public BinaryExpression(IExpression left, BinaryOperator op, IExpression right)
    {
        Left = left ?? throw new ArgumentNullException(nameof(left));
        Right = right ?? throw new ArgumentNullException(nameof(right));
        Op = op;
    }

    /// <summary>Applies the operator to its two operands.</summary>
    /// <param name="context">Supplies the cell values the operands read.</param>
    /// <returns>
    /// The result of the operation. If either operand is already an error,
    /// that error is passed straight through, and the left one wins if both
    /// are errors. Arithmetic on text gives #VALUE!, and dividing by zero
    /// gives #DIV/0!. A comparison always gives TRUE or FALSE: text is
    /// compared with text ignoring case, and text counts as greater than
    /// anything that is not text. Empty cells count as 0, and TRUE and FALSE
    /// count as 1 and 0.
    /// </returns>
    public CellValue Evaluate(IEvalContext context)
    {
        var left = Left.Evaluate(context);
        var right = Right.Evaluate(context);

        if (left.Kind == ValueKind.Error) return left;
        if (right.Kind == ValueKind.Error) return right;

        return Op switch
        {
            BinaryOperator.Add => ArithOp(left, right, (a, b) => a + b),
            BinaryOperator.Subtract => ArithOp(left, right, (a, b) => a - b),
            BinaryOperator.Multiply => ArithOp(left, right, (a, b) => a * b),
            BinaryOperator.Divide => Divide(left, right),

            BinaryOperator.Equal => Compare(left, right, c => c == 0),
            BinaryOperator.NotEqual => Compare(left, right, c => c != 0),
            BinaryOperator.LessThan => Compare(left, right, c => c < 0),
            BinaryOperator.LessOrEqual => Compare(left, right, c => c <= 0),
            BinaryOperator.GreaterThan => Compare(left, right, c => c > 0),
            BinaryOperator.GreaterOrEqual => Compare(left, right, c => c >= 0),

            _ => CellValue.FromError(ErrorKind.Value)
        };
    }

    private static CellValue ArithOp(
        CellValue left, CellValue right, Func<double, double, double> op)
    {
        if (left.Kind == ValueKind.Text || right.Kind == ValueKind.Text)
            return CellValue.FromError(ErrorKind.Value);
        return CellValue.FromNumber(op(left.AsNumber(), right.AsNumber()));
    }

    private static CellValue Divide(CellValue left, CellValue right)
    {
        if (left.Kind == ValueKind.Text || right.Kind == ValueKind.Text)
            return CellValue.FromError(ErrorKind.Value);
        double divisor = right.AsNumber();
        return divisor == 0.0
            ? CellValue.FromError(ErrorKind.DivideByZero)
            : CellValue.FromNumber(left.AsNumber() / divisor);
    }

    private static CellValue Compare(
        CellValue left, CellValue right, Func<int, bool> predicate)
    {
        int cmp;
        if (left.Kind == ValueKind.Text && right.Kind == ValueKind.Text)
        {
            cmp = string.Compare(left.AsText(), right.AsText(),
                StringComparison.OrdinalIgnoreCase);
        }
        else if (left.Kind == ValueKind.Text || right.Kind == ValueKind.Text)
        {
            cmp = left.Kind == ValueKind.Text ? 1 : -1;
        }
        else
        {
            cmp = left.AsNumber().CompareTo(right.AsNumber());
        }
        return CellValue.FromBoolean(predicate(cmp));
    }

    /// <summary>Calls the visitor's VisitBinary method.</summary>
    /// <typeparam name="T">The type of result the visitor produces.</typeparam>
    /// <param name="visitor">The visitor to call back into.</param>
    /// <returns>Whatever the visitor returns for this node.</returns>
    public T Accept<T>(IExpressionVisitor<T> visitor) => visitor.VisitBinary(this);

    /// <summary>Returns a description of this node, for debugging.</summary>
    /// <returns>Text of the form Binary(left, operator, right).</returns>
    public override string ToString() => $"Binary({Left}, {Op}, {Right})";
}