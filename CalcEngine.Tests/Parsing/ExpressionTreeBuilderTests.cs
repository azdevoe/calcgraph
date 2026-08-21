using Antlr4.Runtime;
using CalcEngine.Core.Expressions;
using CalcEngine.Core.Generated;
using CalcEngine.Core.Parsing;
using CalcEngine.Core.Model;

namespace CalcEngine.Tests.Parsing;

public class ExpressionTreeBuilderTests
{
    // ── Test helper: parses formula text all the way to an IExpression ──

    private static IExpression Build(string formulaText)
    {
        var lexer = new FormulaLexer(new AntlrInputStream(formulaText));
        var tokens = new CommonTokenStream(lexer);
        var parser = new FormulaParser(tokens);
        var tree = parser.formula();
        return new ExpressionTreeBuilder().Visit(tree);
    }

    private static readonly TestContext EmptyCtx = new();

    private class TestContext : IEvalContext
    {
        private readonly Dictionary<CellRef, CellValue> _cells = new();

        public TestContext Set(CellRef r, CellValue v) { _cells[r] = v; return this; }

        public CellValue GetCellValue(CellRef cellRef)
            => _cells.TryGetValue(cellRef, out var v) ? v : CellValue.Empty;

        public IReadOnlyList<CellValue> GetRangeValues(CellRange range)
            => range.GetCells().Select(GetCellValue).ToList();

        public CellValue CallFunction(string name, IReadOnlyList<IExpression> args)
        {
            if (name == "SUM")
            {
                double sum = 0;
                foreach (var arg in args)
                {
                    if (arg is RangeExpression r)
                        foreach (var v in GetRangeValues(r.Range)) sum += v.AsNumber();
                    else
                        sum += arg.Evaluate(this).AsNumber();
                }
                return CellValue.FromNumber(sum);
            }
            return CellValue.FromError(ErrorKind.Name);
        }
    }

    // ── Leaves ──

    [Fact]
    public void Number_ParsesToNumberExpression()
    {
        var expr = Build("=42");
        Assert.IsType<NumberExpression>(expr);
        Assert.Equal(42.0, expr.Evaluate(EmptyCtx).AsNumber());
    }

    [Fact]
    public void DecimalNumber_ParsesCorrectly()
    {
        var expr = Build("=3.14");
        Assert.Equal(3.14, expr.Evaluate(EmptyCtx).AsNumber());
    }

    [Fact]
    public void String_ParsesAndStripsQuotes()
    {
        var expr = Build("=\"hello\"");
        Assert.IsType<TextExpression>(expr);
        Assert.Equal("hello", ((TextExpression)expr).Value);
    }

    [Fact]
    public void String_EscapedQuotes_Unescaped()
    {
        // ="she said ""hi""" → she said "hi"
        var expr = Build("=\"she said \"\"hi\"\"\"");
        Assert.Equal("she said \"hi\"", ((TextExpression)expr).Value);
    }

    [Fact]
    public void Boolean_True_ParsesCorrectly()
    {
        var expr = Build("=TRUE");
        Assert.IsType<BooleanExpression>(expr);
        Assert.True(((BooleanExpression)expr).Value);
    }

    [Fact]
    public void CellRef_ParsesToCorrectAddress()
    {
        var expr = Build("=B2");
        var cellRefExpr = Assert.IsType<CellRefExpression>(expr);
        Assert.Equal(2, cellRefExpr.Ref.Row);
        Assert.Equal(2, cellRefExpr.Ref.Column); // B
    }

    // ── Precedence ──

    [Fact]
    public void Precedence_MultiplyBeforeAdd()
    {
        // A1+B2*3 must parse as A1+(B2*3)
        var ctx = new TestContext()
            .Set(new CellRef(1, 1), CellValue.FromNumber(1))  // A1
            .Set(new CellRef(2, 2), CellValue.FromNumber(2)); // B2

        var expr = Build("=A1+B2*3");
        // 1 + (2*3) = 7, NOT (1+2)*3 = 9
        Assert.Equal(7.0, expr.Evaluate(ctx).AsNumber());
    }

    [Fact]
    public void Precedence_ParenthesesOverridePrecedence()
    {
        var expr = Build("=(1+2)*3");
        Assert.Equal(9.0, expr.Evaluate(EmptyCtx).AsNumber());
    }

    [Fact]
    public void Precedence_UnaryBeforeMultiply()
    {
        // -A1*B1 parses as (-A1)*B1
        var ctx = new TestContext()
            .Set(new CellRef(1, 1), CellValue.FromNumber(5))  // A1
            .Set(new CellRef(1, 2), CellValue.FromNumber(3)); // B1

        var expr = Build("=-A1*B1");
        Assert.Equal(-15.0, expr.Evaluate(ctx).AsNumber());
    }

    [Fact]
    public void DoubleUnaryMinus_ParsesAndEvaluates()
    {
        var expr = Build("=--5");
        Assert.Equal(5.0, expr.Evaluate(EmptyCtx).AsNumber());
    }

    // ── Comparison ──

    [Fact]
    public void Comparison_Equal()
    {
        var expr = Build("=5=5");
        Assert.Equal(1.0, expr.Evaluate(EmptyCtx).AsNumber());
    }

    [Fact]
    public void Comparison_NotEqual()
    {
        var expr = Build("=5<>3");
        Assert.Equal(1.0, expr.Evaluate(EmptyCtx).AsNumber());
    }

    [Fact]
    public void Comparison_GreaterOrEqual()
    {
        var expr = Build("=5>=5");
        Assert.Equal(1.0, expr.Evaluate(EmptyCtx).AsNumber());
    }

    // ── Functions and ranges ──

    [Fact]
    public void Function_Sum_WithRange()
    {
        var ctx = new TestContext()
            .Set(new CellRef(2, 2), CellValue.FromNumber(10))  // B2
            .Set(new CellRef(3, 2), CellValue.FromNumber(20))  // B3
            .Set(new CellRef(4, 2), CellValue.FromNumber(30)); // B4

        var expr = Build("=SUM(B2:B4)");
        Assert.Equal(60.0, expr.Evaluate(ctx).AsNumber());
    }

    [Fact]
    public void Function_SumTimesScalar_MatchesPortfolioWorkedExample()
    {
        // Design Portfolio Section 3.6: =SUM(B2:B45)*0.3
        var ctx = new TestContext()
            .Set(new CellRef(2, 2), CellValue.FromNumber(10))
            .Set(new CellRef(3, 2), CellValue.FromNumber(20))
            .Set(new CellRef(4, 2), CellValue.FromNumber(30));

        var expr = Build("=SUM(B2:B4)*0.3");
        Assert.Equal(18.0, expr.Evaluate(ctx).AsNumber(), 5);
    }

    [Fact]
    public void Function_WithTwoRanges()
    {
        var ctx = new TestContext()
            .Set(new CellRef(1, 1), CellValue.FromNumber(1))  // A1
            .Set(new CellRef(1, 2), CellValue.FromNumber(2)); // B1

        var expr = Build("=SUM(A1:A1,B1:B1)");
        Assert.Equal(3.0, expr.Evaluate(ctx).AsNumber());
    }

    [Fact]
    public void Function_UnknownName_EvaluatesToNameError()
    {
        var expr = Build("=TOTAL(A1)");
        Assert.Equal(ValueKind.Error, expr.Evaluate(EmptyCtx).Kind);
    }

    [Fact]
    public void Function_NoArgs_BuildsEmptyArgsList()
    {
        // Grammar allows argList? — zero-arg calls must not throw during build.
        var expr = Build("=TOTAL()");
        var fn = Assert.IsType<FunctionExpression>(expr);
        Assert.Empty(fn.Args);
    }

    // ── Plain literals (no leading '=') ──

    [Fact]
    public void PlainNumber_NoEqualsSign_ParsesAsLiteral()
    {
        var expr = Build("42");
        Assert.IsType<NumberExpression>(expr);
        Assert.Equal(42.0, expr.Evaluate(EmptyCtx).AsNumber());
    }

    [Fact]
    public void PlainString_NoEqualsSign_ParsesAsLiteral()
    {
        var expr = Build("\"hello\"");
        Assert.IsType<TextExpression>(expr);
    }

    [Fact]
    public void PlainBoolean_NoEqualsSign_ParsesAsLiteral()
    {
        var expr = Build("TRUE");
        Assert.IsType<BooleanExpression>(expr);
    }
}