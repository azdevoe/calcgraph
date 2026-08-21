using CalcEngine.Core.Expressions;
using CalcEngine.Core.Model;

namespace CalcEngine.Tests.Expressions;

public class ExpressionTests
{
    // ── Test helpers ─────────────────────────────────────────────

    private class TestContext : IEvalContext
    {
        private readonly Dictionary<CellRef, CellValue> _cells = new();

        public TestContext Set(CellRef r, CellValue v)
        {
            _cells[r] = v;
            return this;
        }

        public CellValue GetCellValue(CellRef cellRef)
            => _cells.TryGetValue(cellRef, out var v) ? v : CellValue.Empty;

        public IReadOnlyList<CellValue> GetRangeValues(CellRange range)
            => range.GetCells().Select(c => GetCellValue(c)).ToList();

        public CellValue CallFunction(string name, IReadOnlyList<IExpression> args)
        {
            if (name == "SUM")
            {
                double sum = 0;
                foreach (var arg in args)
                {
                    if (arg is RangeExpression rangeExpr)
                        foreach (var v in GetRangeValues(rangeExpr.Range))
                            sum += v.AsNumber();
                    else
                        sum += arg.Evaluate(this).AsNumber();
                }
                return CellValue.FromNumber(sum);
            }
            return CellValue.FromError(ErrorKind.Name);
        }
    }

    private class TypeNameVisitor : IExpressionVisitor<string>
    {
        public string VisitNumber(NumberExpression e) => "Number";
        public string VisitText(TextExpression e) => "Text";
        public string VisitBoolean(BooleanExpression e) => "Boolean";
        public string VisitCellRef(CellRefExpression e) => "CellRef";
        public string VisitRange(RangeExpression e) => "Range";
        public string VisitUnary(UnaryExpression e) => "Unary";
        public string VisitBinary(BinaryExpression e) => "Binary";
        public string VisitFunction(FunctionExpression e) => "Function";
    }

    private static readonly TestContext EmptyCtx = new();
    private static readonly TypeNameVisitor TypeVisitor = new();

    // ── NumberExpression ─────────────────────────────────────────

    [Fact]
    public void Number_Evaluate_ReturnsNumberValue()
    {
        var result = new NumberExpression(42.0).Evaluate(EmptyCtx);
        Assert.Equal(ValueKind.Number, result.Kind);
        Assert.Equal(42.0, result.AsNumber());
    }

    [Fact]
    public void Number_Accept_DispatchesToVisitNumber()
    {
        Assert.Equal("Number", new NumberExpression(1).Accept(TypeVisitor));
    }

    // ── TextExpression ───────────────────────────────────────────

    [Fact]
    public void Text_Evaluate_ReturnsTextValue()
    {
        var result = new TextExpression("hello").Evaluate(EmptyCtx);
        Assert.Equal(ValueKind.Text, result.Kind);
    }

    [Fact]
    public void Text_NullValue_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new TextExpression(null!));
    }

    // ── BooleanExpression ────────────────────────────────────────

    [Fact]
    public void Boolean_True_AsNumberIsOne()
    {
        Assert.Equal(1.0, new BooleanExpression(true).Evaluate(EmptyCtx).AsNumber());
    }

    [Fact]
    public void Boolean_False_AsNumberIsZero()
    {
        Assert.Equal(0.0, new BooleanExpression(false).Evaluate(EmptyCtx).AsNumber());
    }

    // ── CellRefExpression ────────────────────────────────────────

    [Fact]
    public void CellRef_Evaluate_ReturnsContextValue()
    {
        var cellRef = new CellRef(1, 1);
        var ctx = new TestContext().Set(cellRef, CellValue.FromNumber(99));
        Assert.Equal(99.0, new CellRefExpression(cellRef).Evaluate(ctx).AsNumber());
    }

    [Fact]
    public void CellRef_AbsentCell_ReturnsEmpty()
    {
        var result = new CellRefExpression(new CellRef(1, 1)).Evaluate(EmptyCtx);
        Assert.Equal(ValueKind.Empty, result.Kind);
    }

    [Fact]
    public void CellRef_InvalidRow_Throws()
    {
        Assert.Throws<ArgumentException>(() => new CellRefExpression(new CellRef(0, 1)));
    }

    // ── RangeExpression ──────────────────────────────────────────

    [Fact]
    public void Range_Evaluate_ReturnsValueError()
    {
        var range = new CellRange(new CellRef(1, 1), new CellRef(5, 1));
        var result = new RangeExpression(range).Evaluate(EmptyCtx);
        Assert.Equal(ValueKind.Error, result.Kind);
    }

    // ── UnaryExpression ──────────────────────────────────────────

    [Fact]
    public void Unary_Minus_NegatesNumber()
    {
        var expr = new UnaryExpression("-", new NumberExpression(5));
        Assert.Equal(-5.0, expr.Evaluate(EmptyCtx).AsNumber());
    }

    [Fact]
    public void Unary_DoubleMinus_IsPositive()
    {
        var expr = new UnaryExpression("-", new UnaryExpression("-", new NumberExpression(5)));
        Assert.Equal(5.0, expr.Evaluate(EmptyCtx).AsNumber());
    }

    [Fact]
    public void Unary_Minus_Text_ReturnsValueError()
    {
        var expr = new UnaryExpression("-", new TextExpression("abc"));
        Assert.Equal(ValueKind.Error, expr.Evaluate(EmptyCtx).Kind);
    }

    [Fact]
    public void Unary_InvalidOp_Throws()
    {
        Assert.Throws<ArgumentException>(() => new UnaryExpression("*", new NumberExpression(1)));
    }

    // ── BinaryExpression — arithmetic ────────────────────────────

    [Fact]
    public void Binary_Add_NumberPlusNumber()
    {
        var expr = new BinaryExpression(new NumberExpression(1), BinaryOperator.Add, new NumberExpression(2));
        Assert.Equal(3.0, expr.Evaluate(EmptyCtx).AsNumber());
    }

    [Fact]
    public void Binary_DivideByZero_ReturnsDivError()
    {
        var expr = new BinaryExpression(new NumberExpression(1), BinaryOperator.Divide, new NumberExpression(0));
        Assert.Equal(ValueKind.Error, expr.Evaluate(EmptyCtx).Kind);
    }

    [Fact]
    public void Binary_TextInArithmetic_ReturnsValueError()
    {
        var expr = new BinaryExpression(new TextExpression("abc"), BinaryOperator.Add, new NumberExpression(1));
        Assert.Equal(ValueKind.Error, expr.Evaluate(EmptyCtx).Kind);
    }

    [Fact]
    public void Binary_ErrorPropagation_LeftWins()
    {
        var ctx = new TestContext()
            .Set(new CellRef(1, 1), CellValue.FromError(ErrorKind.DivideByZero))
            .Set(new CellRef(1, 2), CellValue.FromError(ErrorKind.Name));

        var expr = new BinaryExpression(
            new CellRefExpression(new CellRef(1, 1)),
            BinaryOperator.Add,
            new CellRefExpression(new CellRef(1, 2)));

        Assert.Equal(ValueKind.Error, expr.Evaluate(ctx).Kind);
    }

    // ── BinaryExpression — comparison ────────────────────────────

    [Fact]
    public void Binary_Equal_TrueCase()
    {
        var expr = new BinaryExpression(new NumberExpression(5), BinaryOperator.Equal, new NumberExpression(5));
        Assert.Equal(1.0, expr.Evaluate(EmptyCtx).AsNumber());
    }

    [Fact]
    public void Binary_LessThan()
    {
        var expr = new BinaryExpression(new NumberExpression(3), BinaryOperator.LessThan, new NumberExpression(5));
        Assert.Equal(1.0, expr.Evaluate(EmptyCtx).AsNumber());
    }

    // ── FunctionExpression ───────────────────────────────────────

    [Fact]
    public void Function_KnownFunction_DelegatesToContext()
    {
        var expr = new FunctionExpression("SUM", new IExpression[] { new NumberExpression(1), new NumberExpression(2) });
        Assert.Equal(3.0, expr.Evaluate(EmptyCtx).AsNumber());
    }

    [Fact]
    public void Function_WithRange_DelegatesToContext()
    {
        var ctx = new TestContext()
            .Set(new CellRef(1, 1), CellValue.FromNumber(10))
            .Set(new CellRef(2, 1), CellValue.FromNumber(20))
            .Set(new CellRef(3, 1), CellValue.FromNumber(30));

        var range = new CellRange(new CellRef(1, 1), new CellRef(3, 1));
        var expr = new FunctionExpression("SUM", new IExpression[] { new RangeExpression(range) });

        Assert.Equal(60.0, expr.Evaluate(ctx).AsNumber());
    }

    [Fact]
    public void Function_UnknownName_ReturnsNameError()
    {
        var expr = new FunctionExpression("TOTAL", new IExpression[] { new NumberExpression(1) });
        Assert.Equal(ValueKind.Error, expr.Evaluate(EmptyCtx).Kind);
    }

    [Fact]
    public void Function_NameIsUppercased()
    {
        var expr = new FunctionExpression("sum", Array.Empty<IExpression>());
        Assert.Equal("SUM", expr.Name);
    }

    // ── Composite tree ───────────────────────────────────────────

    [Fact]
    public void Tree_SumTimesScalar_MatchesPortfolioExample()
    {
        // Design Portfolio 3.6: =SUM(B2:B45)*0.3
        var ctx = new TestContext()
            .Set(new CellRef(2, 2), CellValue.FromNumber(10))
            .Set(new CellRef(3, 2), CellValue.FromNumber(20))
            .Set(new CellRef(4, 2), CellValue.FromNumber(30));

        var range = new CellRange(new CellRef(2, 2), new CellRef(4, 2));
        var sumExpr = new FunctionExpression("SUM", new IExpression[] { new RangeExpression(range) });
        var expr = new BinaryExpression(sumExpr, BinaryOperator.Multiply, new NumberExpression(0.3));

        Assert.Equal(18.0, expr.Evaluate(ctx).AsNumber(), 5);
    }
}