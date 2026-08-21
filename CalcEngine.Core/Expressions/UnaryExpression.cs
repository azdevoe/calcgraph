using CalcEngine.Core.Model;

namespace CalcEngine.Core.Expressions;

/// <summary>
/// Branch: a prefix unary operator applied to one operand.
/// Op is "+" or "-". Recursive: --A1 is legal.
/// </summary>
public sealed class UnaryExpression : IExpression
{
    /// <summary>Gets the sign being applied, either "+" or "-".</summary>
    public string Op { get; }

    /// <summary>Gets the expression the sign applies to.</summary>
    public IExpression Operand { get; }

    /// <summary>Initializes a new signed expression.</summary>
    /// <param name="op">The sign to apply. Must be "+" or "-".</param>
    /// <param name="operand">The expression the sign applies to.</param>
    /// <exception cref="ArgumentException">
    /// <paramref name="op"/> is not "+" or "-".
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="operand"/> is null.
    /// </exception>
    public UnaryExpression(string op, IExpression operand)
    {
        if (op is not ("+" or "-"))
            throw new ArgumentException($"Unknown unary operator: '{op}'", nameof(op));
        Op = op;
        Operand = operand ?? throw new ArgumentNullException(nameof(operand));
    }

    /// <summary>Applies the sign to its operand.</summary>
    /// <param name="context">Supplies the cell values the operand reads.</param>
    /// <returns>
    /// The operand's value, negated if the sign is "-". An operand that is
    /// already an error is passed straight through, and text gives #VALUE!.
    /// </returns>
    public CellValue Evaluate(IEvalContext context)
    {
        var val = Operand.Evaluate(context);

        if (val.Kind == ValueKind.Error) return val;
        if (val.Kind == ValueKind.Text) return CellValue.FromError(ErrorKind.Value);

        double num = val.AsNumber();
        return CellValue.FromNumber(Op == "-" ? -num : num);
    }

    /// <summary>Calls the visitor's VisitUnary method.</summary>
    /// <typeparam name="T">The type of result the visitor produces.</typeparam>
    /// <param name="visitor">The visitor to call back into.</param>
    /// <returns>Whatever the visitor returns for this node.</returns>
    public T Accept<T>(IExpressionVisitor<T> visitor) => visitor.VisitUnary(this);

    /// <summary>Returns a description of this node, for debugging.</summary>
    /// <returns>Text of the form Unary(-, operand).</returns>
    public override string ToString() => $"Unary({Op}, {Operand})";
}