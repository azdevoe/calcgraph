using System.Globalization;
using System.Linq;

namespace CalcEngine.Core.Expressions;

/// <summary>
/// Serialises an expression tree back to formula text (Group C feature:
/// Sorting &amp; Filtering — RangeSorter needs this to turn a translated
/// tree back into raw input that SetCellCommand's normal edit path can
/// re-parse; the GUI formula bar needs the same round trip).
///
/// Every binary and unary operand is wrapped in parentheses, even when
/// not strictly required by precedence. The grammar accepts a
/// parenthesised expression anywhere an atom is legal, so this is
/// always valid input; it trades a prettier-but-precedence-dependent
/// printer for one that cannot mis-parenthesise and change meaning.
/// </summary>
public sealed class FormulaPrinter : IExpressionVisitor<string>
{
    /// <summary>Writes an expression out as a complete formula.</summary>
    /// <param name="expr">The expression to write out.</param>
    /// <returns>
    /// Formula text beginning with "=", which can be fed straight back in as
    /// a cell edit.
    /// </returns>
    public static string Print(IExpression expr) => "=" + expr.Accept(new FormulaPrinter());

    /// <summary>Writes out a numeric literal.</summary>
    /// <param name="expr">The node being visited.</param>
    /// <returns>The number, written the same way in every region.</returns>
    public string VisitNumber(NumberExpression expr) => expr.Value.ToString(CultureInfo.InvariantCulture);

    /// <summary>Writes out a text literal.</summary>
    /// <param name="expr">The node being visited.</param>
    /// <returns>The text in double quotes, with any quotes inside it doubled.</returns>
    public string VisitText(TextExpression expr) => "\"" + expr.Value.Replace("\"", "\"\"") + "\"";

    /// <summary>Writes out a TRUE or FALSE literal.</summary>
    /// <param name="expr">The node being visited.</param>
    /// <returns>"TRUE" or "FALSE".</returns>
    public string VisitBoolean(BooleanExpression expr) => expr.Value ? "TRUE" : "FALSE";

    /// <summary>Writes out a reference to a single cell.</summary>
    /// <param name="expr">The node being visited.</param>
    /// <returns>The address in A1 notation, such as "B2".</returns>
    public string VisitCellRef(CellRefExpression expr) => expr.Ref.ToA1();

    /// <summary>Writes out a reference to a range of cells.</summary>
    /// <param name="expr">The node being visited.</param>
    /// <returns>The two corner addresses separated by a colon, such as "B2:B45".</returns>
    public string VisitRange(RangeExpression expr) =>
        $"{expr.Range.TopLeft.ToA1()}:{expr.Range.BottomRight.ToA1()}";

    /// <summary>Writes out a signed expression.</summary>
    /// <param name="expr">The node being visited.</param>
    /// <returns>The sign followed by the operand in brackets.</returns>
    public string VisitUnary(UnaryExpression expr) => $"{expr.Op}({expr.Operand.Accept(this)})";

    /// <summary>Writes out an operator and its two operands.</summary>
    /// <param name="expr">The node being visited.</param>
    /// <returns>
    /// The operands and operator wrapped in brackets, so the formula keeps
    /// its meaning when it is read back.
    /// </returns>
    public string VisitBinary(BinaryExpression expr) =>
        $"({expr.Left.Accept(this)}{OpText(expr.Op)}{expr.Right.Accept(this)})";

    /// <summary>Writes out a function call.</summary>
    /// <param name="expr">The node being visited.</param>
    /// <returns>The name followed by the arguments in brackets, separated by commas.</returns>
    public string VisitFunction(FunctionExpression expr) =>
        $"{expr.Name}({string.Join(",", expr.Args.Select(a => a.Accept(this)))})";

    private static string OpText(BinaryOperator op) => op switch
    {
        BinaryOperator.Add => "+",
        BinaryOperator.Subtract => "-",
        BinaryOperator.Multiply => "*",
        BinaryOperator.Divide => "/",
        BinaryOperator.Equal => "=",
        BinaryOperator.NotEqual => "<>",
        BinaryOperator.LessThan => "<",
        BinaryOperator.LessOrEqual => "<=",
        BinaryOperator.GreaterThan => ">",
        BinaryOperator.GreaterOrEqual => ">=",
        _ => throw new ArgumentOutOfRangeException(nameof(op), op, "Unknown binary operator.")
    };
}
