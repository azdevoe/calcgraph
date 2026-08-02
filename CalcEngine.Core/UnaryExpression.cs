namespace CalcEngine.Core;

/// <summary>
/// Branch: a prefix unary operator applied to one operand.
/// Op is "+" or "-". Recursive: --A1 is legal.
/// </summary>
public sealed class UnaryExpression : IExpression
{
    public string Op { get; }
    public IExpression Operand { get; }

    public UnaryExpression(string op, IExpression operand)
    {
        if (op is not ("+" or "-"))
            throw new ArgumentException($"Unknown unary operator: '{op}'", nameof(op));
        Op = op;
        Operand = operand ?? throw new ArgumentNullException(nameof(operand));
    }

    public CellValue Evaluate(IEvalContext context)
    {
        var val = Operand.Evaluate(context);

        if (val.Kind == ValueKind.Error) return val;
        if (val.Kind == ValueKind.Text) return CellValue.FromError(ErrorKind.Value);

        double num = val.AsNumber();
        return CellValue.FromNumber(Op == "-" ? -num : num);
    }

    public T Accept<T>(IExpressionVisitor<T> visitor) => visitor.VisitUnary(this);

    public override string ToString() => $"Unary({Op}, {Operand})";
}