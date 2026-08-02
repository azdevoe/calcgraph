using CalcEngine.Core;
using Xunit;

namespace CalcEngine.Tests;

/// <summary>
/// Tests for SetCellCommand — the concrete Command that wraps a single
/// cell edit (Design_Portfolio 4.9, Fig 9). Also exercises the ICommand
/// interface implicitly since SetCellCommand implements it.
///
/// Design rule under test: commands store raw input text, not computed
/// values. Execute captures the old raw input from the engine; Undo
/// replays it through the normal edit path so the dependency graph
/// never drifts out of sync with the workbook.
/// </summary>
public class SetCellCommandTests
{
    private readonly CellRef _a1 = CellRef.Parse("A1");
    private readonly CellRef _a2 = CellRef.Parse("A2");
    private readonly CellRef _b1 = CellRef.Parse("B1");

    // ── Execute: basics ────────────────────────────────────────────

    [Fact]
    public void Execute_SetsNewValue()
    {
        var engine = new CalculationEngine();
        var cmd = new SetCellCommand(engine, _a1, "42");

        cmd.Execute();

        Assert.Equal(CellValue.FromNumber(42), engine.GetValue(_a1));
    }

    [Fact]
    public void Execute_ReturnsSuccessfulChangeSet()
    {
        var engine = new CalculationEngine();
        var cmd = new SetCellCommand(engine, _a1, "42");

        var result = cmd.Execute();

        Assert.True(result.Success);
        Assert.Equal(_a1, result.Edited);
        Assert.Contains(_a1, result.ChangedCells);
    }

    [Fact]
    public void Execute_FormulaInput_SetsComputedValue()
    {
        var engine = new CalculationEngine();
        engine.SetCellContent(_a1, "10");

        var cmd = new SetCellCommand(engine, _b1, "=A1+5");
        cmd.Execute();

        Assert.Equal(CellValue.FromNumber(15), engine.GetValue(_b1));
    }

    [Fact]
    public void Execute_OverwritesExistingValue()
    {
        var engine = new CalculationEngine();
        engine.SetCellContent(_a1, "1");

        var cmd = new SetCellCommand(engine, _a1, "99");
        cmd.Execute();

        Assert.Equal(CellValue.FromNumber(99), engine.GetValue(_a1));
    }

    // ── Undo: basics ──────────────────────────────────────────────

    [Fact]
    public void Undo_RestoresPreviousValue()
    {
        var engine = new CalculationEngine();
        engine.SetCellContent(_a1, "1");

        var cmd = new SetCellCommand(engine, _a1, "99");
        cmd.Execute();
        cmd.Undo();

        Assert.Equal(CellValue.FromNumber(1), engine.GetValue(_a1));
    }

    [Fact]
    public void Undo_PreviouslyEmptyCell_ClearsIt()
    {
        var engine = new CalculationEngine();
        var cmd = new SetCellCommand(engine, _a1, "42");
        cmd.Execute();

        cmd.Undo();

        Assert.Equal(CellValue.Empty, engine.GetValue(_a1));
    }

    [Fact]
    public void Undo_ReturnsSuccessfulChangeSet()
    {
        var engine = new CalculationEngine();
        var cmd = new SetCellCommand(engine, _a1, "42");
        cmd.Execute();

        var result = cmd.Undo();

        Assert.True(result.Success);
        Assert.Contains(_a1, result.ChangedCells);
    }

    [Fact]
    public void Undo_RestoresFormula()
    {
        var engine = new CalculationEngine();
        engine.SetCellContent(_a1, "=B1+1");

        var cmd = new SetCellCommand(engine, _a1, "99");
        cmd.Execute();
        Assert.Equal(CellValue.FromNumber(99), engine.GetValue(_a1));

        cmd.Undo();

        Assert.Equal("=B1+1", engine.GetFormula(_a1));
    }

    // ── Undo: dependency propagation ──────────────────────────────

    [Fact]
    public void Undo_RecomputesDependents()
    {
        var engine = new CalculationEngine();
        engine.SetCellContent(_a1, "10");
        engine.SetCellContent(_b1, "=A1+1");
        Assert.Equal(CellValue.FromNumber(11), engine.GetValue(_b1));

        var cmd = new SetCellCommand(engine, _a1, "50");
        cmd.Execute();
        Assert.Equal(CellValue.FromNumber(51), engine.GetValue(_b1));

        cmd.Undo();

        Assert.Equal(CellValue.FromNumber(10), engine.GetValue(_a1));
        Assert.Equal(CellValue.FromNumber(11), engine.GetValue(_b1));
    }

    // ── Round-trip: Execute → Undo → re-Execute ───────────────────

    [Fact]
    public void Execute_AfterUndo_RestoresNewValueAgain()
    {
        var engine = new CalculationEngine();
        engine.SetCellContent(_a1, "1");

        var cmd = new SetCellCommand(engine, _a1, "99");
        cmd.Execute();
        cmd.Undo();
        cmd.Execute();

        Assert.Equal(CellValue.FromNumber(99), engine.GetValue(_a1));
    }

    [Fact]
    public void RoundTrip_WithDependents_AllValuesCorrectAtEachStep()
    {
        var engine = new CalculationEngine();
        engine.SetCellContent(_a1, "1");
        engine.SetCellContent(_a2, "=A1*10");

        var cmd = new SetCellCommand(engine, _a1, "5");

        cmd.Execute();
        Assert.Equal(CellValue.FromNumber(5), engine.GetValue(_a1));
        Assert.Equal(CellValue.FromNumber(50), engine.GetValue(_a2));

        cmd.Undo();
        Assert.Equal(CellValue.FromNumber(1), engine.GetValue(_a1));
        Assert.Equal(CellValue.FromNumber(10), engine.GetValue(_a2));

        cmd.Execute();
        Assert.Equal(CellValue.FromNumber(5), engine.GetValue(_a1));
        Assert.Equal(CellValue.FromNumber(50), engine.GetValue(_a2));
    }

    // ── GetFormula preserved through undo ──────────────────────────

    [Fact]
    public void Undo_PreviouslyEmptyCell_GetFormulaReturnsEmpty()
    {
        var engine = new CalculationEngine();
        var cmd = new SetCellCommand(engine, _a1, "42");
        cmd.Execute();
        cmd.Undo();

        Assert.Equal(string.Empty, engine.GetFormula(_a1));
    }

    [Fact]
    public void Undo_OverwrittenLiteral_GetFormulaReturnsOldInput()
    {
        var engine = new CalculationEngine();
        engine.SetCellContent(_a1, "\"hello\"");

        var cmd = new SetCellCommand(engine, _a1, "99");
        cmd.Execute();
        cmd.Undo();

        Assert.Equal("\"hello\"", engine.GetFormula(_a1));
    }
}