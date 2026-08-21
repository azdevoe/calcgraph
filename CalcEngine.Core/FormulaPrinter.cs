using System.Globalization;
using System.Linq;

namespace CalcEngine.Core;

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
    /// <summary>Prints expr as a complete formula, with the leading '='.</summary>
    public static string Print(IExpression expr) => "=" + expr.Accept(new FormulaPrinter());

    public string VisitNumber(NumberExpression expr) => expr.Value.ToString(CultureInfo.InvariantCulture);

    public string VisitText(TextExpression expr) => "\"" + expr.Value.Replace("\"", "\"\"") + "\"";

    public string VisitBoolean(BooleanExpression expr) => expr.Value ? "TRUE" : "FALSE";

    public string VisitCellRef(CellRefExpression expr) => expr.Ref.ToA1();

    public string VisitRange(RangeExpression expr) =>
        $"{expr.Range.TopLeft.ToA1()}:{expr.Range.BottomRight.ToA1()}";

    public string VisitUnary(UnaryExpression expr) => $"{expr.Op}({expr.Operand.Accept(this)})";

    public string VisitBinary(BinaryExpression expr) =>
        $"({expr.Left.Accept(this)}{OpText(expr.Op)}{expr.Right.Accept(this)})";

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
