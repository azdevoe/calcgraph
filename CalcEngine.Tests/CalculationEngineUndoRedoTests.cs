using CalcEngine.Core;
using Xunit;

namespace CalcEngine.Tests;

/// <summary>
/// Tests for Undo/Redo wired into CalculationEngine — the final
/// public API surface additions per Design_Portfolio Fig 2.
///
/// These tests exercise the high-level user workflow: edit via
/// SetCellContent, undo via Undo(), redo via Redo(). The underlying
/// CommandManager and SetCellCommand are tested individually in their
/// own test classes; these tests verify the wiring.
/// </summary>
public class CalculationEngineUndoRedoTests
{
    private readonly CellRef _a1 = CellRef.Parse("A1");
    private readonly CellRef _a2 = CellRef.Parse("A2");
    private readonly CellRef _b1 = CellRef.Parse("B1");

    private sealed class RecordingObserver : ICellObserver
    {
        public List<CellChangeSet> ChangedCalls { get; } = new();
        public List<IReadOnlyList<CellRef>> CircularCalls { get; } = new();

        public void OnCellsChanged(CellChangeSet changeSet) => ChangedCalls.Add(changeSet);

        public void OnCircularReference(IReadOnlyList<CellRef> cyclePath) =>
            CircularCalls.Add(cyclePath);
    }

    // ── CanUndo / CanRedo ──────────────────────────────────────────

    [Fact]
    public void CanUndo_InitiallyFalse()
    {
        var engine = new CalculationEngine();
        Assert.False(engine.CanUndo);
    }

    [Fact]
    public void CanRedo_InitiallyFalse()
    {
        var engine = new CalculationEngine();
        Assert.False(engine.CanRedo);
    }

    [Fact]
    public void CanUndo_TrueAfterEdit()
    {
        var engine = new CalculationEngine();
        engine.SetCellContent(_a1, "1");
        Assert.True(engine.CanUndo);
    }

    // ── Basic undo ────────────────────────────────────────────────

    [Fact]
    public void Undo_RevertsEdit()
    {
        var engine = new CalculationEngine();
        engine.SetCellContent(_a1, "42");

        engine.Undo();

        Assert.Equal(CellValue.Empty, engine.GetValue(_a1));
    }

    [Fact]
    public void Undo_ReturnsChangeSet()
    {
        var engine = new CalculationEngine();
        engine.SetCellContent(_a1, "42");

        var result = engine.Undo();

        Assert.True(result.Success);
        Assert.Contains(_a1, result.ChangedCells);
    }

    [Fact]
    public void Undo_RestoresPreviousLiteral()
    {
        var engine = new CalculationEngine();
        engine.SetCellContent(_a1, "1");
        engine.SetCellContent(_a1, "99");

        engine.Undo();

        Assert.Equal(CellValue.FromNumber(1), engine.GetValue(_a1));
    }

    [Fact]
    public void Undo_RestoresFormula()
    {
        var engine = new CalculationEngine();
        engine.SetCellContent(_a1, "=B1+1");
        engine.SetCellContent(_a1, "99");

        engine.Undo();

        Assert.Equal("=B1+1", engine.GetFormula(_a1));
    }

    // ── Basic redo ────────────────────────────────────────────────

    [Fact]
    public void Redo_ReappliesUndonEdit()
    {
        var engine = new CalculationEngine();
        engine.SetCellContent(_a1, "42");
        engine.Undo();

        engine.Redo();

        Assert.Equal(CellValue.FromNumber(42), engine.GetValue(_a1));
    }

    [Fact]
    public void Redo_ReturnsChangeSet()
    {
        var engine = new CalculationEngine();
        engine.SetCellContent(_a1, "42");
        engine.Undo();

        var result = engine.Redo();

        Assert.True(result.Success);
        Assert.Contains(_a1, result.ChangedCells);
    }

    // ── New edit clears redo ──────────────────────────────────────

    [Fact]
    public void NewEdit_ClearsRedo()
    {
        var engine = new CalculationEngine();
        engine.SetCellContent(_a1, "1");
        engine.Undo();
        Assert.True(engine.CanRedo);

        engine.SetCellContent(_a1, "2");

        Assert.False(engine.CanRedo);
    }

    // ── Dependency propagation through undo/redo ──────────────────

    [Fact]
    public void Undo_RecomputesDependentCells()
    {
        var engine = new CalculationEngine();
        engine.SetCellContent(_a1, "10");
        engine.SetCellContent(_b1, "=A1+1");
        engine.SetCellContent(_a1, "50");
        Assert.Equal(CellValue.FromNumber(51), engine.GetValue(_b1));

        engine.Undo();

        Assert.Equal(CellValue.FromNumber(10), engine.GetValue(_a1));
        Assert.Equal(CellValue.FromNumber(11), engine.GetValue(_b1));
    }

    [Fact]
    public void Redo_RecomputesDependentCells()
    {
        var engine = new CalculationEngine();
        engine.SetCellContent(_a1, "10");
        engine.SetCellContent(_b1, "=A1+1");
        engine.SetCellContent(_a1, "50");
        engine.Undo();

        engine.Redo();

        Assert.Equal(CellValue.FromNumber(50), engine.GetValue(_a1));
        Assert.Equal(CellValue.FromNumber(51), engine.GetValue(_b1));
    }

    // ── Observer notifications from undo/redo ─────────────────────

    [Fact]
    public void Undo_NotifiesObservers()
    {
        var engine = new CalculationEngine();
        engine.SetCellContent(_a1, "42");

        var observer = new RecordingObserver();
        engine.Subscribe(observer);

        engine.Undo();

        Assert.Single(observer.ChangedCalls);
    }

    [Fact]
    public void Redo_NotifiesObservers()
    {
        var engine = new CalculationEngine();
        engine.SetCellContent(_a1, "42");
        engine.Undo();

        var observer = new RecordingObserver();
        engine.Subscribe(observer);

        engine.Redo();

        Assert.Single(observer.ChangedCalls);
    }

    // ── ClearCell participates in undo ─────────────────────────────

    [Fact]
    public void ClearCell_CanBeUndone()
    {
        var engine = new CalculationEngine();
        engine.SetCellContent(_a1, "42");
        engine.ClearCell(_a1);
        Assert.Equal(CellValue.Empty, engine.GetValue(_a1));

        engine.Undo();

        Assert.Equal(CellValue.FromNumber(42), engine.GetValue(_a1));
    }

    [Fact]
    public void ClearCell_UndoRedo_RoundTrips()
    {
        var engine = new CalculationEngine();
        engine.SetCellContent(_a1, "42");
        engine.ClearCell(_a1);

        engine.Undo();
        Assert.Equal(CellValue.FromNumber(42), engine.GetValue(_a1));

        engine.Redo();
        Assert.Equal(CellValue.Empty, engine.GetValue(_a1));
    }

    // ── Multi-step workflow ────────────────────────────────────────

    [Fact]
    public void FullWorkflow_EditUndoRedoEdit()
    {
        var engine = new CalculationEngine();

        engine.SetCellContent(_a1, "1");
        engine.SetCellContent(_a1, "2");
        engine.SetCellContent(_a1, "3");

        engine.Undo(); // 3 → 2
        Assert.Equal(CellValue.FromNumber(2), engine.GetValue(_a1));

        engine.Undo(); // 2 → 1
        Assert.Equal(CellValue.FromNumber(1), engine.GetValue(_a1));

        engine.Redo(); // 1 → 2
        Assert.Equal(CellValue.FromNumber(2), engine.GetValue(_a1));

        // New edit: invalidates redo of "3"
        engine.SetCellContent(_a1, "999");
        Assert.False(engine.CanRedo);
        Assert.Equal(CellValue.FromNumber(999), engine.GetValue(_a1));
    }
}