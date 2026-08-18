using CalcEngine.Core;
using Xunit;

namespace CalcEngine.Tests;

/// <summary>
/// Tests for the Data Validation rule family — IValidationRule and its
/// four implementations. Each rule takes a CellValue (and an IEvalContext,
/// for rules that need to look elsewhere in the workbook) and returns a
/// ValidationResult, the same result-as-data shape used everywhere else
/// in this engine.
/// </summary>
public class ValidationRulesTests
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
            => range.GetCells().Select(GetCellValue).ToList();

        public CellValue CallFunction(string name, IReadOnlyList<IExpression> args)
            => CellValue.FromError(ErrorKind.Name);
    }

    private static readonly TestContext Ctx = new();

    // ── RangeRule ────────────────────────────────────────────────

    [Fact]
    public void RangeRule_NumberWithinRange_Ok()
    {
        var rule = new RangeRule(min: 0, max: 100);
        var result = rule.Validate(CellValue.FromNumber(50), Ctx);
        Assert.True(result.Success);
    }

    [Fact]
    public void RangeRule_NumberAtBoundaries_Ok()
    {
        var rule = new RangeRule(min: 0, max: 100);
        Assert.True(rule.Validate(CellValue.FromNumber(0), Ctx).Success);
        Assert.True(rule.Validate(CellValue.FromNumber(100), Ctx).Success);
    }

    [Fact]
    public void RangeRule_NumberBelowMin_Fails()
    {
        var rule = new RangeRule(min: 0, max: 100);
        var result = rule.Validate(CellValue.FromNumber(-1), Ctx);
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public void RangeRule_NumberAboveMax_Fails()
    {
        var rule = new RangeRule(min: 0, max: 100);
        var result = rule.Validate(CellValue.FromNumber(101), Ctx);
        Assert.False(result.Success);
    }

    [Fact]
    public void RangeRule_NonNumber_Fails()
    {
        var rule = new RangeRule(min: 0, max: 100);
        var result = rule.Validate(CellValue.FromText("fifty"), Ctx);
        Assert.False(result.Success);
    }

    // ── ListRule ─────────────────────────────────────────────────

    [Fact]
    public void ListRule_ValueInList_Ok()
    {
        var rule = new ListRule(new[] { "Pass", "Fail", "Pending" });
        var result = rule.Validate(CellValue.FromText("Pass"), Ctx);
        Assert.True(result.Success);
    }

    [Fact]
    public void ListRule_ValueNotInList_Fails()
    {
        var rule = new ListRule(new[] { "Pass", "Fail", "Pending" });
        var result = rule.Validate(CellValue.FromText("Maybe"), Ctx);
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public void ListRule_CaseInsensitiveMatch_Ok()
    {
        var rule = new ListRule(new[] { "Pass", "Fail", "Pending" });
        var result = rule.Validate(CellValue.FromText("pass"), Ctx);
        Assert.True(result.Success);
    }

    // ── TypeRule ─────────────────────────────────────────────────

    [Fact]
    public void TypeRule_MatchingKind_Ok()
    {
        var rule = new TypeRule(ValueKind.Number);
        var result = rule.Validate(CellValue.FromNumber(42), Ctx);
        Assert.True(result.Success);
    }

    [Fact]
    public void TypeRule_MismatchedKind_Fails()
    {
        var rule = new TypeRule(ValueKind.Number);
        var result = rule.Validate(CellValue.FromText("42"), Ctx);
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
    }

    // ── CustomFormulaRule ────────────────────────────────────────

    [Fact]
    public void CustomFormulaRule_FormulaEvaluatesTrue_Ok()
    {
        var rule = new CustomFormulaRule(new BooleanExpression(true));
        var result = rule.Validate(CellValue.Empty, Ctx);
        Assert.True(result.Success);
    }

    [Fact]
    public void CustomFormulaRule_FormulaEvaluatesFalse_Fails()
    {
        var rule = new CustomFormulaRule(new BooleanExpression(false));
        var result = rule.Validate(CellValue.Empty, Ctx);
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public void CustomFormulaRule_FormulaEvaluatesNonBoolean_Fails()
    {
        var rule = new CustomFormulaRule(new NumberExpression(1));
        var result = rule.Validate(CellValue.Empty, Ctx);
        Assert.False(result.Success);
    }
}