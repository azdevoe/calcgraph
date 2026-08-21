using CalcEngine.Core.Expressions;
using CalcEngine.Core.Generated;
using CalcEngine.Core.Model;

namespace CalcEngine.Core.Parsing;

/// <summary>
/// Walks the ANTLR parse tree and builds the corresponding IExpression
/// tree. Runs once per edit; the result is cached on the cell.
/// This is the "EvalVisitor" pass named in the Design Portfolio —
/// it does not compute values, only builds the tree that Evaluate
/// later interprets.
/// </summary>
public sealed class ExpressionTreeBuilder : FormulaBaseVisitor<IExpression>
{
    public override IExpression VisitFormulaEntry(FormulaParser.FormulaEntryContext context)
        => Visit(context.expr());

    public override IExpression VisitLiteralEntry(FormulaParser.LiteralEntryContext context)
        => Visit(context.literal());

    public override IExpression VisitExpr(FormulaParser.ExprContext context)
        => Visit(context.comparison());

    // ── Precedence chain: each rule is either the recursive binary
    // form or falls through to the next tighter level. ──

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

    public override IExpression VisitAddition(FormulaParser.AdditionContext context)
    {
        if (context.addition() == null)
            return Visit(context.multiply());

        var left = Visit(context.addition());
        var right = Visit(context.multiply());
        var op = context.op.Text == "+" ? BinaryOperator.Add : BinaryOperator.Subtract;
        return new BinaryExpression(left, op, right);
    }

    public override IExpression VisitMultiply(FormulaParser.MultiplyContext context)
    {
        if (context.multiply() == null)
            return Visit(context.unary());

        var left = Visit(context.multiply());
        var right = Visit(context.unary());
        var op = context.op.Text == "*" ? BinaryOperator.Multiply : BinaryOperator.Divide;
        return new BinaryExpression(left, op, right);
    }

    public override IExpression VisitPower(FormulaParser.PowerContext context)
    {
        var baseExpr = Visit(context.atom());
        if (context.unary() == null)
            return baseExpr;

        var exponent = Visit(context.unary());
        return new BinaryExpression(baseExpr, BinaryOperator.Power, exponent);
    }

    public override IExpression VisitUnary(FormulaParser.UnaryContext context)
    {
        if (context.power() != null)
            return Visit(context.power());

        // Recursive form: op unary (allows --A1)
        var operand = Visit(context.unary());
        return new UnaryExpression(context.op.Text, operand);
    }

    // ── Atoms (leaves and function calls) ──

    public override IExpression VisitNumberAtom(FormulaParser.NumberAtomContext context)
        => new NumberExpression(ParseNumber(context.NUMBER().GetText()));

    public override IExpression VisitStringAtom(FormulaParser.StringAtomContext context)
        => new TextExpression(Unquote(context.STRING().GetText()));

    public override IExpression VisitBooleanAtom(FormulaParser.BooleanAtomContext context)
        => new BooleanExpression(context.BOOLEAN().GetText() == "TRUE");

    public override IExpression VisitCellRefAtom(FormulaParser.CellRefAtomContext context)
        => new CellRefExpression(CellRef.Parse(context.CELLREF().GetText()));

    public override IExpression VisitRangeAtom(FormulaParser.RangeAtomContext context)
        => new RangeExpression(CellRange.Parse(context.RANGE().GetText()));

    public override IExpression VisitCallAtom(FormulaParser.CallAtomContext context)
        => Visit(context.functionCall());

    public override IExpression VisitParenAtom(FormulaParser.ParenAtomContext context)
        => Visit(context.expr());

    // ── Function calls ──

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

    public override IExpression VisitArg(FormulaParser.ArgContext context)
    {
        // arg : RANGE | expr — a range that isn't wrapped in a
        // rangeAtom, since it appears directly as a function argument.
        if (context.RANGE() != null)
            return new RangeExpression(CellRange.Parse(context.RANGE().GetText()));

        return Visit(context.expr());
    }

    // ── Plain literals (a cell with no leading '=') ──

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

    private static double ParseNumber(string text)
        => double.Parse(text, System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>Strips the surrounding quotes and un-escapes "" to ".</summary>
    private static string Unquote(string raw)
    {
        var inner = raw.Substring(1, raw.Length - 2);
        return inner.Replace("\"\"", "\"");
    }
}