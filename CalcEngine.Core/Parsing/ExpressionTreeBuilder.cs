using CalcEngine.Core.Expressions;
using CalcEngine.Core.Generated;
using CalcEngine.Core.Model;

namespace CalcEngine.Core.Parsing;

/// <summary>
/// Turns the raw parse tree into the expression tree the engine works
/// with. It does not compute any values, only builds the shape that
/// Evaluate later interprets.
/// </summary>
public sealed class ExpressionTreeBuilder : FormulaBaseVisitor<IExpression>
{
    /// <summary>Builds the expression for a cell entry that began with "=".</summary>
    /// <param name="context">The parse node for the whole entry.</param>
    /// <returns>The expression the formula describes.</returns>
    public override IExpression VisitFormulaEntry(FormulaParser.FormulaEntryContext context)
        => Visit(context.expr());

    /// <summary>Builds the expression for a cell entry that is a plain value.</summary>
    /// <param name="context">The parse node for the whole entry.</param>
    /// <returns>A literal expression holding that value.</returns>
    public override IExpression VisitLiteralEntry(FormulaParser.LiteralEntryContext context)
        => Visit(context.literal());

    /// <summary>Builds the expression for a complete formula body.</summary>
    /// <param name="context">The parse node for the expression.</param>
    /// <returns>The expression it describes.</returns>
    public override IExpression VisitExpr(FormulaParser.ExprContext context)
        => Visit(context.comparison());

    // ── Precedence chain: each rule is either the recursive binary
    // form or falls through to the next tighter level. ──

    /// <summary>
    /// Builds a comparison such as A1 &gt; 5, or passes through to the
    /// arithmetic below it when there is no comparison to make.
    /// </summary>
    /// <param name="context">The parse node for the comparison.</param>
    /// <returns>
    /// An operator expression for the comparison, or the expression for
    /// whatever was written in its place.
    /// </returns>
    public override IExpression VisitComparison(FormulaParser.ComparisonContext context)
    {
        if (context.comparison() == null)
            return Visit(context.addition());

        var left = Visit(context.comparison());
        var right = Visit(context.addition());
        var op = context.op.Text switch
        {
            "=" => BinaryOperator.Equal,
            "<>" => BinaryOperator.NotEqual,
            "<" => BinaryOperator.LessThan,
            "<=" => BinaryOperator.LessOrEqual,
            ">" => BinaryOperator.GreaterThan,
            ">=" => BinaryOperator.GreaterOrEqual,
            _ => throw new InvalidOperationException($"Unknown comparison operator '{context.op.Text}'")
        };
        return new BinaryExpression(left, op, right);
    }

    /// <summary>
    /// Builds an addition or subtraction, or passes through to the
    /// multiplication below it when there is neither.
    /// </summary>
    /// <param name="context">The parse node for the addition.</param>
    /// <returns>
    /// An operator expression for the addition or subtraction, or the
    /// expression for whatever was written in its place.
    /// </returns>
    public override IExpression VisitAddition(FormulaParser.AdditionContext context)
    {
        if (context.addition() == null)
            return Visit(context.multiply());

        var left = Visit(context.addition());
        var right = Visit(context.multiply());
        var op = context.op.Text == "+" ? BinaryOperator.Add : BinaryOperator.Subtract;
        return new BinaryExpression(left, op, right);
    }

    /// <summary>
    /// Builds a multiplication or division, or passes through to the sign
    /// below it when there is neither.
    /// </summary>
    /// <param name="context">The parse node for the multiplication.</param>
    /// <returns>
    /// An operator expression for the multiplication or division, or the
    /// expression for whatever was written in its place.
    /// </returns>
    public override IExpression VisitMultiply(FormulaParser.MultiplyContext context)
    {
        if (context.multiply() == null)
            return Visit(context.unary());

        var left = Visit(context.multiply());
        var right = Visit(context.unary());
        var op = context.op.Text == "*" ? BinaryOperator.Multiply : BinaryOperator.Divide;
        return new BinaryExpression(left, op, right);
    }
    
    /// <summary>
    /// Builds a power expression such as 2^3, or passes through to the base
    /// expression when there is no exponent. Powers may be chained, so 2^3^2
    /// is allowed.
    /// </summary>
    /// <param name="context">The parse node for the power expression.</param>
    /// <returns>
    /// A power expression, or the expression for the base value when no exponent
    /// was written.
    /// </returns>

    public override IExpression VisitPower(FormulaParser.PowerContext context)
    {
        var baseExpr = Visit(context.atom());
        if (context.unary() == null)
            return baseExpr;

        var exponent = Visit(context.unary());
        return new BinaryExpression(baseExpr, BinaryOperator.Power, exponent);
    }
    
    /// <summary>
    /// Builds a signed expression such as -A1, or passes through to the value
    /// below it when there is no sign. Signs may be stacked, so --A1 is
    /// allowed.
    /// </summary>
    /// <param name="context">The parse node for the signed expression.</param>
    /// <returns>
    /// A signed expression, or the expression for whatever was written in its
    /// place.
    /// </returns>
    public override IExpression VisitUnary(FormulaParser.UnaryContext context)
    {
        if (context.power() != null)
            return Visit(context.power());

        // Recursive form: op unary (allows --A1)
        var operand = Visit(context.unary());
        return new UnaryExpression(context.op.Text, operand);
    }

    // ── Atoms (leaves and function calls) ──

    /// <summary>Builds a numeric literal.</summary>
    /// <param name="context">The parse node holding the number.</param>
    /// <returns>A literal expression for that number.</returns>
    public override IExpression VisitNumberAtom(FormulaParser.NumberAtomContext context)
        => new NumberExpression(ParseNumber(context.NUMBER().GetText()));

    /// <summary>Builds a text literal.</summary>
    /// <param name="context">The parse node holding the quoted text.</param>
    /// <returns>
    /// A literal expression for that text, without its surrounding quotes and
    /// with any doubled quotes inside it reduced to single ones.
    /// </returns>
    public override IExpression VisitStringAtom(FormulaParser.StringAtomContext context)
        => new TextExpression(Unquote(context.STRING().GetText()));

    /// <summary>Builds a TRUE or FALSE literal.</summary>
    /// <param name="context">The parse node holding the word.</param>
    /// <returns>A literal expression for that value.</returns>
    public override IExpression VisitBooleanAtom(FormulaParser.BooleanAtomContext context)
        => new BooleanExpression(context.BOOLEAN().GetText() == "TRUE");

    /// <summary>Builds a reference to a single cell.</summary>
    /// <param name="context">The parse node holding the address.</param>
    /// <returns>A reference expression for that address.</returns>
    /// <exception cref="FormatException">The address is not a usable one.</exception>
    /// <exception cref="ArgumentException">The address falls outside the sheet.</exception>
    public override IExpression VisitCellRefAtom(FormulaParser.CellRefAtomContext context)
        => new CellRefExpression(CellRef.Parse(context.CELLREF().GetText()));

    /// <summary>Builds a reference to a range of cells.</summary>
    /// <param name="context">The parse node holding the two addresses.</param>
    /// <returns>A range expression covering those cells.</returns>
    /// <exception cref="FormatException">Either address is not a usable one.</exception>
    /// <exception cref="ArgumentException">
    /// The two addresses are the wrong way round, or fall outside the sheet.
    /// </exception>
    public override IExpression VisitRangeAtom(FormulaParser.RangeAtomContext context)
        => new RangeExpression(CellRange.Parse(context.RANGE().GetText()));

    /// <summary>Builds a function call written in place of a value.</summary>
    /// <param name="context">The parse node holding the call.</param>
    /// <returns>The expression for that call.</returns>
    public override IExpression VisitCallAtom(FormulaParser.CallAtomContext context)
        => Visit(context.functionCall());

    /// <summary>Builds an expression written inside brackets.</summary>
    /// <param name="context">The parse node holding the bracketed expression.</param>
    /// <returns>
    /// The expression from inside the brackets. The brackets themselves leave
    /// no trace, having already done their work of grouping.
    /// </returns>
    public override IExpression VisitParenAtom(FormulaParser.ParenAtomContext context)
        => Visit(context.expr());

    // ── Function calls ──

    /// <summary>Builds a function call from its name and arguments.</summary>
    /// <param name="context">The parse node holding the call.</param>
    /// <returns>
    /// A call expression carrying the arguments in the order they were
    /// written. A call written with no arguments at all gives an empty list;
    /// whether that suits the function is settled when it is worked out, not
    /// here.
    /// </returns>
    public override IExpression VisitFunctionCall(FormulaParser.FunctionCallContext context)
    {
        string name = context.FUNCNAME().GetText();
        var args = new List<IExpression>();

        var argListCtx = context.argList();
        if (argListCtx != null)
        {
            foreach (var argCtx in argListCtx.arg())
                args.Add(VisitArg(argCtx));
        }

        return new FunctionExpression(name, args);
    }

    /// <summary>Builds one argument of a function call.</summary>
    /// <param name="context">The parse node holding the argument.</param>
    /// <returns>
    /// A range expression when the argument is a range such as B2:B45, and
    /// otherwise the expression the argument was written as.
    /// </returns>
    public override IExpression VisitArg(FormulaParser.ArgContext context)
    {
        // arg : RANGE | expr — a range that isn't wrapped in a
        // rangeAtom, since it appears directly as a function argument.
        if (context.RANGE() != null)
            return new RangeExpression(CellRange.Parse(context.RANGE().GetText()));

        return Visit(context.expr());
    }

    // ── Plain literals (a cell with no leading '=') ──

    /// <summary>
    /// Builds the value in a cell that was filled in without a leading "=".
    /// </summary>
    /// <param name="context">The parse node holding the value.</param>
    /// <returns>
    /// A literal expression for the number, text, or TRUE or FALSE that was
    /// entered.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// The entry is none of those three. This means the grammar and this
    /// method have fallen out of step with each other, not that the user
    /// typed something odd.
    /// </exception>
    public override IExpression VisitLiteral(FormulaParser.LiteralContext context)
    {
        if (context.NUMBER() != null)
            return new NumberExpression(ParseNumber(context.NUMBER().GetText()));
        if (context.STRING() != null)
            return new TextExpression(Unquote(context.STRING().GetText()));
        if (context.BOOLEAN() != null)
            return new BooleanExpression(context.BOOLEAN().GetText() == "TRUE");

        throw new InvalidOperationException("Literal has no recognized token.");
    }

    // ── Helpers ──
   /// <summary>
   /// Parses a numeric literal using invariant culture.
   /// </summary>
   /// <param name="text">The numeric literal to parse.</param>
   /// <returns>The parsed value as a double.</returns>
    private static double ParseNumber(string text)
        => double.Parse(text, System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>Strips the surrounding quotes and un-escapes "" to ".</summary>
    private static string Unquote(string raw)
    {
        var inner = raw.Substring(1, raw.Length - 2);
        return inner.Replace("\"\"", "\"");
    }
}