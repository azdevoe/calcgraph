using CalcEngine.Core.Expressions;
using CalcEngine.Core.Functions;
using CalcEngine.Core.Model;

namespace CalcEngine.Tests.Functions;

public class FunctionStrategyTests
{
    // ── Shared test context ──

    private class TestContext : IEvalContext
    {
        private readonly Dictionary<CellRef, CellValue> _cells = new();

        public TestContext Set(CellRef r, CellValue v) { _cells[r] = v; return this; }

        public CellValue GetCellValue(CellRef cellRef)
            => _cells.TryGetValue(cellRef, out var v) ? v : CellValue.Empty;

        public IReadOnlyList<CellValue> GetRangeValues(CellRange range)
            => range.GetCells().Select(GetCellValue).ToList();

        public CellValue CallFunction(string name, IReadOnlyList<IExpression> args)
            => throw new NotSupportedException("Not used in these tests.");
    }

    private static readonly TestContext EmptyCtx = new();

    private static IExpression Num(double d) => new NumberExpression(d);
    private static IExpression Rng(CellRange r) => new RangeExpression(r);

    // ════════════════════════════════════════════════════════════
    //  SUM
    // ════════════════════════════════════════════════════════════

    [Fact]
    public void Sum_ScalarArgs_AddsThem()
    {
        var result = new SumStrategy().Evaluate(new[] { Num(1), Num(2), Num(3) }, EmptyCtx);
        Assert.Equal(6.0, result.AsNumber());
    }

    [Fact]
    public void Sum_WithRange_SumsAllCells()
    {
        var ctx = new TestContext()
            .Set(new CellRef(1, 1), CellValue.FromNumber(10))
            .Set(new CellRef(2, 1), CellValue.FromNumber(20));
        var range = new CellRange(new CellRef(1, 1), new CellRef(2, 1));

        var result = new SumStrategy().Evaluate(new[] { Rng(range) }, ctx);
        Assert.Equal(30.0, result.AsNumber());
    }

    [Fact]
    public void Sum_TextArgument_ReturnsValueError()
    {
        var result = new SumStrategy().Evaluate(new IExpression[] { new TextExpression("x") }, EmptyCtx);
        Assert.Equal(ValueKind.Error, result.Kind);
    }

    [Fact]
    public void Sum_ErrorArgument_Propagates()
    {
        var ctx = new TestContext().Set(new CellRef(1, 1), CellValue.FromError(ErrorKind.DivideByZero));
        var result = new SumStrategy().Evaluate(
            new IExpression[] { new CellRefExpression(new CellRef(1, 1)) }, ctx);
        Assert.Equal(ValueKind.Error, result.Kind);
    }

    // ════════════════════════════════════════════════════════════
    //  AVERAGE
    // ════════════════════════════════════════════════════════════

    [Fact]
    public void Average_ThreeNumbers_ReturnsMean()
    {
        var result = new AverageStrategy().Evaluate(new[] { Num(2), Num(4), Num(6) }, EmptyCtx);
        Assert.Equal(4.0, result.AsNumber());
    }

    // ════════════════════════════════════════════════════════════
    //  MIN / MAX
    // ════════════════════════════════════════════════════════════

    [Fact]
    public void Min_ReturnsSmallest()
    {
        var result = new MinStrategy().Evaluate(new[] { Num(5), Num(1), Num(3) }, EmptyCtx);
        Assert.Equal(1.0, result.AsNumber());
    }

    [Fact]
    public void Max_ReturnsLargest()
    {
        var result = new MaxStrategy().Evaluate(new[] { Num(5), Num(1), Num(3) }, EmptyCtx);
        Assert.Equal(5.0, result.AsNumber());
    }

    // ════════════════════════════════════════════════════════════
    //  COUNT
    // ════════════════════════════════════════════════════════════

    [Fact]
    public void Count_MixedNumbersAndText_CountsOnlyNumbers()
    {
        var args = new IExpression[] { Num(1), new TextExpression("x"), Num(2) };
        var result = new CountStrategy().Evaluate(args, EmptyCtx);
        Assert.Equal(2.0, result.AsNumber());
    }

    [Fact]
    public void Count_EmptyCell_NotCounted()
    {
        // B99 is absent → Empty → not counted
        var args = new IExpression[] { Num(1), new CellRefExpression(new CellRef(99, 99)) };
        var result = new CountStrategy().Evaluate(args, EmptyCtx);
        Assert.Equal(1.0, result.AsNumber());
    }

    // ════════════════════════════════════════════════════════════
    //  IF
    // ════════════════════════════════════════════════════════════

    [Fact]
    public void If_TrueCondition_ReturnsTrueBranch()
    {
        var args = new IExpression[] { new BooleanExpression(true), Num(1), Num(2) };
        var result = new IfStrategy().Evaluate(args, EmptyCtx);
        Assert.Equal(1.0, result.AsNumber());
    }

    [Fact]
    public void If_FalseCondition_ReturnsFalseBranch()
    {
        var args = new IExpression[] { new BooleanExpression(false), Num(1), Num(2) };
        var result = new IfStrategy().Evaluate(args, EmptyCtx);
        Assert.Equal(2.0, result.AsNumber());
    }

    [Fact]
    public void If_DoesNotEvaluateUntakenBranch()
    {
        // False branch divides by zero — must NOT surface since condition is true.
        var untakenDivByZero = new BinaryExpression(Num(1), BinaryOperator.Divide, Num(0));
        var args = new IExpression[] { new BooleanExpression(true), Num(99), untakenDivByZero };

        var result = new IfStrategy().Evaluate(args, EmptyCtx);
        Assert.Equal(99.0, result.AsNumber()); // no error, untaken branch never ran
    }

    [Fact]
    public void If_NumericConditionNonZero_IsTruthy()
    {
        var args = new IExpression[] { Num(5), Num(1), Num(2) };
        var result = new IfStrategy().Evaluate(args, EmptyCtx);
        Assert.Equal(1.0, result.AsNumber());
    }

    // ════════════════════════════════════════════════════════════
    //  ROUND
    // ════════════════════════════════════════════════════════════

    [Fact]
    public void Round_TwoDecimalPlaces()
    {
        var args = new IExpression[] { Num(3.14159), Num(2) };
        var result = new RoundStrategy().Evaluate(args, EmptyCtx);
        Assert.Equal(3.14, result.AsNumber());
    }

    [Fact]
    public void Round_ZeroDigits_RoundsToInteger()
    {
        var args = new IExpression[] { Num(3.7), Num(0) };
        var result = new RoundStrategy().Evaluate(args, EmptyCtx);
        Assert.Equal(4.0, result.AsNumber());
    }

    // ════════════════════════════════════════════════════════════
    //  LOOKUP
    // ════════════════════════════════════════════════════════════

    [Fact]
    public void Lookup_FindsMatchInRange()
    {
        var ctx = new TestContext()
            .Set(new CellRef(1, 1), CellValue.FromNumber(10))
            .Set(new CellRef(2, 1), CellValue.FromNumber(20))
            .Set(new CellRef(3, 1), CellValue.FromNumber(30));
        var range = new CellRange(new CellRef(1, 1), new CellRef(3, 1));

        var args = new IExpression[] { Num(20), Rng(range) };
        var result = new LookupStrategy().Evaluate(args, ctx);
        Assert.Equal(20.0, result.AsNumber());
    }

    [Fact]
    public void Lookup_NoMatch_ReturnsNotAvailableError()
    {
        var ctx = new TestContext()
            .Set(new CellRef(1, 1), CellValue.FromNumber(10));
        var range = new CellRange(new CellRef(1, 1), new CellRef(1, 1));

        var args = new IExpression[] { Num(999), Rng(range) };
        var result = new LookupStrategy().Evaluate(args, ctx);
        Assert.Equal(ValueKind.Error, result.Kind);
    }

    [Fact]
    public void Lookup_SecondArgNotRange_ReturnsValueError()
    {
        var args = new IExpression[] { Num(1), Num(2) }; // not a range
        var result = new LookupStrategy().Evaluate(args, EmptyCtx);
        Assert.Equal(ValueKind.Error, result.Kind);
    }

    // ════════════════════════════════════════════════════════════
    //  FunctionFactory
    // ════════════════════════════════════════════════════════════

    [Fact]
    public void Factory_UnknownFunction_ReturnsNameError()
    {
        var factory = FunctionFactory.CreateDefault();
        var result = factory.Evaluate("TOTAL", new[] { Num(1) }, EmptyCtx);
        Assert.Equal(ValueKind.Error, result.Kind);
    }

    [Fact]
    public void Factory_TooFewArgs_ReturnsValueError()
    {
        // IF requires exactly 3 args.
        var factory = FunctionFactory.CreateDefault();
        var result = factory.Evaluate("IF", new[] { Num(1) }, EmptyCtx);
        Assert.Equal(ValueKind.Error, result.Kind);
    }

    [Fact]
    public void Factory_TooManyArgs_ReturnsValueError()
    {
        var factory = FunctionFactory.CreateDefault();
        var result = factory.Evaluate("IF", new[] { Num(1), Num(2), Num(3), Num(4) }, EmptyCtx);
        Assert.Equal(ValueKind.Error, result.Kind);
    }

    [Fact]
    public void Factory_AllEightFunctions_AreRegistered()
    {
        var factory = FunctionFactory.CreateDefault();
        var names = new[] { "SUM", "AVERAGE", "MIN", "MAX", "COUNT", "IF", "ROUND", "LOOKUP" };

        foreach (var name in names)
        {
            // SUM/AVERAGE/MIN/MAX/COUNT need 1 arg; IF needs 3; ROUND/LOOKUP need 2.
            // Just confirm it's NOT #NAME? (i.e. it's registered).
            var args = name switch
            {
                "IF" => new[] { Num(1), Num(1), Num(1) },
                "ROUND" or "LOOKUP" => new[] { Num(1), Num(1) },
                _ => new[] { Num(1) }
            };
            var result = factory.Evaluate(name, args, EmptyCtx);
            Assert.NotEqual(ErrorKind.Name, result.Error);
        }
    }

    [Fact]
    public void Factory_SumViaFactory_MatchesDirectCall()
    {
        var factory = FunctionFactory.CreateDefault();
        var result = factory.Evaluate("SUM", new[] { Num(1), Num(2), Num(3) }, EmptyCtx);
        Assert.Equal(6.0, result.AsNumber());
    }
}