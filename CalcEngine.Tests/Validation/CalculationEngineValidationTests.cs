using CalcEngine.Core;
using CalcEngine.Core.ChangeTracking;
using CalcEngine.Core.Model;
using CalcEngine.Core.Validation;
using CalcEngine.Core.Engine;
using Xunit;

namespace CalcEngine.Tests.Validation;

/// <summary>
/// Tests for CalculationEngine's data validation integration (Group C
/// feature). SetValidationRule/ClearValidationRule attach an
/// IValidationRule to a cell via ValidationRegistry; ApplyEdit checks
/// the CANDIDATE VALUE — the evaluated result, not the raw formula
/// text — against that rule before anything is written. A rejected
/// edit behaves like a parse failure or a circular reference: the
/// workbook, the dependency graph, and observers are all left exactly
/// as they were.
/// </summary>
public class CalculationEngineValidationTests
{
    private readonly CellRef _a1 = CellRef.Parse("A1");
    private readonly CellRef _b1 = CellRef.Parse("B1");
    private readonly CellRef _c1 = CellRef.Parse("C1");

    private sealed class RecordingObserver : ICellObserver
    {
        public List<CellChangeSet> ChangedCalls { get; } = new();
        public void OnCellsChanged(CellChangeSet changeSet) => ChangedCalls.Add(changeSet);
        public void OnCircularReference(IReadOnlyList<CellRef> cyclePath) { }
    }

    [Fact]
    public void SetCellContent_ValueSatisfiesRule_Succeeds()
    {
        var engine = new CalculationEngine();
        engine.SetValidationRule(_a1, new RangeRule(0, 100));

        var result = engine.SetCellContent(_a1, "50");

        Assert.True(result.Success);
        Assert.Equal(CellValue.FromNumber(50), engine.GetValue(_a1));
    }

    [Fact]
    public void SetCellContent_ValueViolatesRule_ReturnsValidationFailed()
    {
        var engine = new CalculationEngine();
        engine.SetValidationRule(_a1, new RangeRule(0, 100));

        var result = engine.SetCellContent(_a1, "500");

        Assert.False(result.Success);
        Assert.Equal(ChangeFailureReason.ValidationError, result.FailureReason);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public void SetCellContent_ValueViolatesRule_DoesNotTouchWorkbook()
    {
        var engine = new CalculationEngine();
        engine.SetValidationRule(_a1, new RangeRule(0, 100));

        engine.SetCellContent(_a1, "500");

        Assert.Equal(CellValue.Empty, engine.GetValue(_a1));
    }

    [Fact]
    public void SetCellContent_ValueViolatesRule_DoesNotNotifyObservers()
    {
        var engine = new CalculationEngine();
        var observer = new RecordingObserver();
        engine.Subscribe(observer);
        engine.SetValidationRule(_a1, new RangeRule(0, 100));

        engine.SetCellContent(_a1, "500");

        Assert.Empty(observer.ChangedCalls);
    }

    [Fact]
    public void SetCellContent_ValueViolatesRule_PreservesPreviousValue()
    {
        var engine = new CalculationEngine();
        engine.SetValidationRule(_a1, new RangeRule(0, 100));
        engine.SetCellContent(_a1, "50");

        var result = engine.SetCellContent(_a1, "500");

        Assert.False(result.Success);
        Assert.Equal(CellValue.FromNumber(50), engine.GetValue(_a1));
    }

    [Fact]
    public void SetCellContent_RejectedFormulaEdit_PreservesPreviousFormula()
    {
        var engine = new CalculationEngine();
        engine.SetCellContent(_a1, "10");
        engine.SetCellContent(_c1, "20");
        engine.SetCellContent(_b1, "=A1");           // B1 = 10, no rule yet
        engine.SetValidationRule(_b1, new RangeRule(0, 15));

        var result = engine.SetCellContent(_b1, "=C1"); // would make B1 = 20, over the limit

        Assert.False(result.Success);
        Assert.Equal("=A1", engine.GetFormula(_b1));    // dependency graph + formula unchanged
        Assert.Equal(CellValue.FromNumber(10), engine.GetValue(_b1));
    }

    [Fact]
    public void SetCellContent_RuleAppliesToEvaluatedResult_NotRawFormulaText()
    {
        var engine = new CalculationEngine();
        engine.SetCellContent(_a1, "60");
        engine.SetValidationRule(_b1, new RangeRule(0, 100));

        var result = engine.SetCellContent(_b1, "=A1*2"); // evaluates to 120

        Assert.False(result.Success);
        Assert.Equal(CellValue.Empty, engine.GetValue(_b1));
    }

    [Fact]
    public void ClearValidationRule_SubsequentEditNoLongerChecked()
    {
        var engine = new CalculationEngine();
        engine.SetValidationRule(_a1, new RangeRule(0, 100));
        engine.ClearValidationRule(_a1);

        var result = engine.SetCellContent(_a1, "500");

        Assert.True(result.Success);
        Assert.Equal(CellValue.FromNumber(500), engine.GetValue(_a1));
    }

    [Fact]
    public void SetCellContent_NoRuleRegistered_AnyValueSucceeds()
    {
        var engine = new CalculationEngine();

        var result = engine.SetCellContent(_a1, "999999");

        Assert.True(result.Success);
        Assert.Equal(CellValue.FromNumber(999999), engine.GetValue(_a1));
    }

    [Fact]
    public void SetCellContent_ValueViolatesRule_DoesNotAddUndoableCommand()
    {
        // Exercises the CommandManager RI-6 fix end to end: a rejected
        // validation edit must not leave a phantom entry to undo.
        var engine = new CalculationEngine();
        engine.SetValidationRule(_a1, new RangeRule(0, 100));

        engine.SetCellContent(_a1, "500");

        Assert.False(engine.CanUndo);
    }
}