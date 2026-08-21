using CalcEngine.Core.Model;
using CalcEngine.Core.Validation;
using Xunit;

namespace CalcEngine.Tests.Validation;

/// <summary>
/// Tests for ValidationRegistry — holds the (at most one) IValidationRule
/// attached to each cell. CalculationEngine consults this before
/// committing an edit; it does not evaluate anything itself.
/// </summary>
public class ValidationRegistryTests
{
    private readonly CellRef _a1 = CellRef.Parse("A1");
    private readonly CellRef _b2 = CellRef.Parse("B2");

    [Fact]
    public void GetRule_NoRuleSet_ReturnsNull()
    {
        var registry = new ValidationRegistry();
        Assert.Null(registry.GetRule(_a1));
    }

    [Fact]
    public void SetRule_GetRuleReturnsIt()
    {
        var registry = new ValidationRegistry();
        var rule = new RangeRule(0, 100);
        registry.SetRule(_a1, rule);
        Assert.Same(rule, registry.GetRule(_a1));
    }

    [Fact]
    public void SetRule_OverwritesPreviousRule()
    {
        var registry = new ValidationRegistry();
        var first = new RangeRule(0, 100);
        var second = new TypeRule(ValueKind.Number);
        registry.SetRule(_a1, first);
        registry.SetRule(_a1, second);
        Assert.Same(second, registry.GetRule(_a1));
    }

    [Fact]
    public void SetRule_DoesNotAffectOtherCells()
    {
        var registry = new ValidationRegistry();
        registry.SetRule(_a1, new RangeRule(0, 100));
        Assert.Null(registry.GetRule(_b2));
    }

    [Fact]
    public void ClearRule_GetRuleReturnsNullAfterwards()
    {
        var registry = new ValidationRegistry();
        registry.SetRule(_a1, new RangeRule(0, 100));
        registry.ClearRule(_a1);
        Assert.Null(registry.GetRule(_a1));
    }

    [Fact]
    public void ClearRule_NoRuleSet_DoesNotThrow()
    {
        var registry = new ValidationRegistry();
        var exception = Record.Exception(() => registry.ClearRule(_a1));
        Assert.Null(exception);
    }
}