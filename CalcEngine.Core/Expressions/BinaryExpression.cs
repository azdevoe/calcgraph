using CalcEngine.Core.Model;

namespace CalcEngine.Core.Expressions;

/// <summary>
/// Branch: a binary operator applied to two operands.
/// Error model: first error wins, div-by-zero yields #DIV/0!,
/// text in arithmetic yields #VALUE!.
/// </summary>
public sealed class BinaryExpression : IExpression
{
    public IExpression Left { get; }
    public BinaryOperator Op { get; }
    public IExpression Right { get; }

    public BinaryExpression(IExpression left, BinaryOperator op, IExpression right)
    {
        Left = left ?? throw new ArgumentNullException(nameof(left));
        Right = right ?? throw new ArgumentNullException(nameof(right));
        Op = op;
    }

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

    public T Accept<T>(IExpressionVisitor<T> visitor) => visitor.VisitBinary(this);

    public override string ToString() => $"Binary({Left}, {Op}, {Right})";
}