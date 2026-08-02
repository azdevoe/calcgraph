using CalcEngine.Core;
using Xunit;

namespace CalcEngine.Tests;

/// <summary>
/// Tests for CalculationEngine — the public API surface (Design_Portfolio
/// 4.1, Fig 2). Wires Workbook, DependencyGraph, FormulaInputParser and
/// ChangeNotifier together. Every operation returns a CellChangeSet so
/// a successful edit, a parse failure, and a circular reference all
/// come back as data — the client never needs to catch anything.
///
/// Undo/Redo are deliberately NOT tested or implemented here: they
/// depend on CommandManager, which doesn't exist yet. Fig 2 lists them
/// on CalculationEngine, but wiring them in is scoped to the Command
/// unit that comes next, per the project's own build order.
/// </summary>
public class CalculationEngineTests
{
    private readonly CellRef _a1 = CellRef.Parse("A1");
    private readonly CellRef _a2 = CellRef.Parse("A2");
    private readonly CellRef _a3 = CellRef.Parse("A3");
    private readonly CellRef _b1 = CellRef.Parse("B1");
    private readonly CellRef _c1 = CellRef.Parse("C1");

    private sealed class RecordingObserver : ICellObserver
    {
        public List<CellChangeSet> ChangedCalls { get; } = new();
        public List<IReadOnlyList<CellRef>> CircularCalls { get; } = new();

        public void OnCellsChanged(CellChangeSet changeSet) => ChangedCalls.Add(changeSet);

        public void OnCircularReference(IReadOnlyList<CellRef> cyclePath) =>
            CircularCalls.Add(cyclePath);
    }

    // ── SetCellContent: literals ─────────────────────────────────────

    [Fact]
    public void SetCellContent_Number_UpdatesValue()
    {
        var engine = new CalculationEngine();
        engine.SetCellContent(_a1, "42");
        Assert.Equal(CellValue.FromNumber(42), engine.GetValue(_a1));
    }

    [Fact]
    public void SetCellContent_Number_ReturnsSuccessfulChangeSet()
    {
        var engine = new CalculationEngine();
        var result = engine.SetCellContent(_a1, "42");

        Assert.True(result.Success);
        Assert.Equal(_a1, result.Edited);
        Assert.Contains(_a1, result.ChangedCells);
    }

    [Fact]
    public void SetCellContent_Boolean_UpdatesValue()
    {
        var engine = new CalculationEngine();
        engine.SetCellContent(_a1, "TRUE");
        Assert.True(engine.GetValue(_a1).Boolean);
    }

    [Fact]
    public void SetCellContent_QuotedString_UpdatesValue()
    {
        var engine = new CalculationEngine();
        engine.SetCellContent(_a1, "\"hello\"");
        Assert.Equal("hello", engine.GetValue(_a1).AsText());
    }

    // ── SetCellContent: formulas ──────────────────────────────────────

    [Fact]
    public void SetCellContent_SimpleFormula_UpdatesValue()
    {
        var engine = new CalculationEngine();
        engine.SetCellContent(_a1, "=2+3");
        Assert.Equal(CellValue.FromNumber(5), engine.GetValue(_a1));
    }

    [Fact]
    public void SetCellContent_FormulaReferencingEmptyCell_TreatsItAsZero()
    {
        var engine = new CalculationEngine();
        engine.SetCellContent(_b1, "=A1+1");
        Assert.Equal(CellValue.FromNumber(1), engine.GetValue(_b1));
    }

    [Fact]
    public void SetCellContent_FormulaReferencingSetCell_EvaluatesCorrectly()
    {
        var engine = new CalculationEngine();
        engine.SetCellContent(_a1, "10");
        engine.SetCellContent(_b1, "=A1+5");
        Assert.Equal(CellValue.FromNumber(15), engine.GetValue(_b1));
    }

    // ── Reactive recalculation ────────────────────────────────────────

    [Fact]
    public void SetCellContent_UpdatingPrecedent_RecomputesDependent()
    {
        var engine = new CalculationEngine();
        engine.SetCellContent(_b1, "=A1+1");
        Assert.Equal(CellValue.FromNumber(1), engine.GetValue(_b1));

        engine.SetCellContent(_a1, "10");

        Assert.Equal(CellValue.FromNumber(11), engine.GetValue(_b1));
    }

    [Fact]
    public void SetCellContent_UpdatingPrecedent_ChangeSetIncludesDependent()
    {
        var engine = new CalculationEngine();
        engine.SetCellContent(_b1, "=A1+1");

        var result = engine.SetCellContent(_a1, "10");

        Assert.Contains(_a1, result.ChangedCells);
        Assert.Contains(_b1, result.ChangedCells);
    }

    [Fact]
    public void SetCellContent_ChainOfThree_PropagatesToEnd()
    {
        var engine = new CalculationEngine();
        engine.SetCellContent(_a2, "=A1+1");
        engine.SetCellContent(_a3, "=A2+1");

        engine.SetCellContent(_a1, "1");

        Assert.Equal(CellValue.FromNumber(1), engine.GetValue(_a1));
        Assert.Equal(CellValue.FromNumber(2), engine.GetValue(_a2));
        Assert.Equal(CellValue.FromNumber(3), engine.GetValue(_a3));
    }

    [Fact]
    public void SetCellContent_SumOverRange_RecomputesWhenMemberChanges()
    {
        var engine = new CalculationEngine();
        engine.SetCellContent(_a1, "1");
        engine.SetCellContent(_a2, "2");
        engine.SetCellContent(_a3, "3");
        engine.SetCellContent(_b1, "=SUM(A1:A3)");
        Assert.Equal(CellValue.FromNumber(6), engine.GetValue(_b1));

        engine.SetCellContent(_a2, "20");

        Assert.Equal(CellValue.FromNumber(24), engine.GetValue(_b1));
    }

    [Fact]
    public void SetCellContent_ChangingFormula_DropsOldDependency()
    {
        var engine = new CalculationEngine();
        engine.SetCellContent(_a1, "=B1");
        engine.SetCellContent(_b1, "5");
        Assert.Equal(CellValue.FromNumber(5), engine.GetValue(_a1));

        // A1 no longer reads B1 — changing B1 must not touch A1 anymore.
        engine.SetCellContent(_a1, "=C1");
        engine.SetCellContent(_c1, "99");
        engine.SetCellContent(_b1, "1000");

        Assert.Equal(CellValue.FromNumber(99), engine.GetValue(_a1));
    }

    // ── Parse failures ─────────────────────────────────────────────

    [Fact]
    public void SetCellContent_MalformedFormula_ReturnsFailure()
    {
        var engine = new CalculationEngine();
        var result = engine.SetCellContent(_a1, "=1+");

        Assert.False(result.Success);
        Assert.NotEmpty(result.ErrorMessage!);
    }

    [Fact]
    public void SetCellContent_MalformedFormula_DoesNotTouchWorkbook()
    {
        var engine = new CalculationEngine();
        var result = engine.SetCellContent(_a1, "=1+");

        Assert.Equal(CellValue.Empty, engine.GetValue(_a1));
    }

    [Fact]
    public void SetCellContent_MalformedFormula_DoesNotNotifyObservers()
    {
        var engine = new CalculationEngine();
        var observer = new RecordingObserver();
        engine.Subscribe(observer);

        engine.SetCellContent(_a1, "=1+");

        Assert.Empty(observer.ChangedCalls);
    }

    // ── Circular references ────────────────────────────────────────

    [Fact]
    public void SetCellContent_SelfReference_ReturnsCircular()
    {
        var engine = new CalculationEngine();
        var result = engine.SetCellContent(_a1, "=A1");

        Assert.False(result.Success);
        Assert.NotNull(result.CircularPath);
        Assert.Equal(_a1, result.CircularPath![0]);
        Assert.Equal(_a1, result.CircularPath![^1]);
    }

    [Fact]
    public void SetCellContent_SelfReference_DoesNotSetValue()
    {
        var engine = new CalculationEngine();
        engine.SetCellContent(_a1, "=A1");
        Assert.Equal(CellValue.Empty, engine.GetValue(_a1));
    }

    [Fact]
    public void SetCellContent_TwoCellCycle_RejectedWithoutDisturbingFirstEdit()
    {
        var engine = new CalculationEngine();
        engine.SetCellContent(_a1, "=B1");           // fine: B1 empty, A1 = Empty (bare ref, no coercion)
        var result = engine.SetCellContent(_b1, "=A1"); // would close the loop

        Assert.False(result.Success);
        Assert.NotNull(result.CircularPath);
        Assert.Equal(CellValue.Empty, engine.GetValue(_b1)); // B1's edit was rejected
        Assert.Equal(CellValue.Empty, engine.GetValue(_a1)); // A1 unaffected: =B1, B1 still empty
    }

    [Fact]
    public void SetCellContent_CircularReference_NotifiesViaCircularChannel()
    {
        var engine = new CalculationEngine();
        var observer = new RecordingObserver();
        engine.Subscribe(observer);

        engine.SetCellContent(_a1, "=A1");

        Assert.Single(observer.CircularCalls);
        Assert.Empty(observer.ChangedCalls);
    }

    // ── GetValue / GetFormula ──────────────────────────────────────

    [Fact]
    public void GetValue_UntouchedCell_ReturnsEmpty()
    {
        var engine = new CalculationEngine();
        Assert.Equal(CellValue.Empty, engine.GetValue(_a1));
    }

    [Fact]
    public void GetFormula_UntouchedCell_ReturnsEmptyString()
    {
        var engine = new CalculationEngine();
        Assert.Equal(string.Empty, engine.GetFormula(_a1));
    }

    [Fact]
    public void GetFormula_AfterLiteral_ReturnsExactRawInput()
    {
        var engine = new CalculationEngine();
        engine.SetCellContent(_a1, "42");
        Assert.Equal("42", engine.GetFormula(_a1));
    }

    [Fact]
    public void GetFormula_AfterFormula_ReturnsExactRawInput()
    {
        var engine = new CalculationEngine();
        engine.SetCellContent(_a1, "=A2+1");
        Assert.Equal("=A2+1", engine.GetFormula(_a1));
    }

    // ── ClearCell ──────────────────────────────────────────────────

    [Fact]
    public void ClearCell_ResetsValueToEmpty()
    {
        var engine = new CalculationEngine();
        engine.SetCellContent(_a1, "42");
        engine.ClearCell(_a1);
        Assert.Equal(CellValue.Empty, engine.GetValue(_a1));
    }

    [Fact]
    public void ClearCell_ResetsFormulaToEmptyString()
    {
        var engine = new CalculationEngine();
        engine.SetCellContent(_a1, "=1+1");
        engine.ClearCell(_a1);
        Assert.Equal(string.Empty, engine.GetFormula(_a1));
    }

    [Fact]
    public void ClearCell_RecomputesDependents()
    {
        var engine = new CalculationEngine();
        engine.SetCellContent(_a1, "10");
        engine.SetCellContent(_b1, "=A1+1");
        Assert.Equal(CellValue.FromNumber(11), engine.GetValue(_b1));

        engine.ClearCell(_a1);

        Assert.Equal(CellValue.FromNumber(1), engine.GetValue(_b1)); // A1 now empty -> 0
    }

    [Fact]
    public void ClearCell_AlreadyEmptyCell_StillReturnsSuccess()
    {
        var engine = new CalculationEngine();
        var result = engine.ClearCell(_a1);
        Assert.True(result.Success);
    }

    [Fact]
    public void ClearCell_NotifiesObservers()
    {
        var engine = new CalculationEngine();
        engine.SetCellContent(_a1, "42");
        var observer = new RecordingObserver();
        engine.Subscribe(observer);

        engine.ClearCell(_a1);

        Assert.Single(observer.ChangedCalls);
    }

    // ── RecalculateAll ─────────────────────────────────────────────

    [Fact]
    public void RecalculateAll_EmptyWorkbook_DoesNotThrow()
    {
        var engine = new CalculationEngine();
        var ex = Record.Exception(() => engine.RecalculateAll());
        Assert.Null(ex);
    }

    [Fact]
    public void RecalculateAll_RecomputesChainCorrectly()
    {
        var engine = new CalculationEngine();
        engine.SetCellContent(_a1, "5");
        engine.SetCellContent(_a2, "=A1+1");
        engine.SetCellContent(_a3, "=A2+1");

        engine.RecalculateAll();

        Assert.Equal(CellValue.FromNumber(5), engine.GetValue(_a1));
        Assert.Equal(CellValue.FromNumber(6), engine.GetValue(_a2));
        Assert.Equal(CellValue.FromNumber(7), engine.GetValue(_a3));
    }

    // ── Subscribe / Unsubscribe ─────────────────────────────────────

    [Fact]
    public void Subscribe_ObserverReceivesChangeNotification()
    {
        var engine = new CalculationEngine();
        var observer = new RecordingObserver();
        engine.Subscribe(observer);

        engine.SetCellContent(_a1, "1");

        Assert.Single(observer.ChangedCalls);
    }

    [Fact]
    public void Unsubscribe_StopsFurtherNotifications()
    {
        var engine = new CalculationEngine();
        var observer = new RecordingObserver();
        engine.Subscribe(observer);
        engine.Unsubscribe(observer);

        engine.SetCellContent(_a1, "1");

        Assert.Empty(observer.ChangedCalls);
    }

    [Fact]
    public void SetCellContent_ChainReaction_NotifiesExactlyOnce()
    {
        // A single edit that ripples through a chain must still be ONE
        // notification carrying every changed cell — not one per cell
        // (Design_Portfolio 4.6, 9.1).
        var engine = new CalculationEngine();
        engine.SetCellContent(_a2, "=A1+1");
        engine.SetCellContent(_a3, "=A2+1");

        var observer = new RecordingObserver();
        engine.Subscribe(observer);

        engine.SetCellContent(_a1, "1");

        Assert.Single(observer.ChangedCalls);
        Assert.Equal(3, observer.ChangedCalls[0].ChangedCells.Count); // A1, A2, A3
    }
}