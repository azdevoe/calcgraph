using CalcEngine.Core;
using Xunit;

namespace CalcEngine.Tests;

/// <summary>
/// Tests for CommandManager — the bounded undo/redo controller
/// (Design_Portfolio 6.5). Exercises every operation and every clause
/// of the representation invariant:
///
///   RI-1: neither stack is null or contains null
///   RI-2: undoStack.Count &lt;= 100 (bounded deque)
///   RI-3: every command on undoStack has been executed exactly once
///   RI-4: every command on redoStack has been undone after executing
///   RI-5: ExecuteCommand clears redoStack
/// </summary>
public class CommandManagerTests
{
    private readonly CellRef _a1 = CellRef.Parse("A1");
    private readonly CellRef _a2 = CellRef.Parse("A2");
    private readonly CellRef _b1 = CellRef.Parse("B1");

    // ── CanUndo / CanRedo initial state ────────────────────────────

    [Fact]
    public void Fresh_CanUndoIsFalse()
    {
        var mgr = new CommandManager();
        Assert.False(mgr.CanUndo);
    }

    [Fact]
    public void Fresh_CanRedoIsFalse()
    {
        var mgr = new CommandManager();
        Assert.False(mgr.CanRedo);
    }

    // ── ExecuteCommand ─────────────────────────────────────────────

    [Fact]
    public void ExecuteCommand_RunsTheCommand()
    {
        var engine = new CalculationEngine();
        var mgr = new CommandManager();
        var cmd = new SetCellCommand(engine, _a1, "42");

        mgr.ExecuteCommand(cmd);

        Assert.Equal(CellValue.FromNumber(42), engine.GetValue(_a1));
    }

    [Fact]
    public void ExecuteCommand_ReturnsChangeSet()
    {
        var engine = new CalculationEngine();
        var mgr = new CommandManager();
        var cmd = new SetCellCommand(engine, _a1, "42");

        var result = mgr.ExecuteCommand(cmd);

        Assert.True(result.Success);
        Assert.Contains(_a1, result.ChangedCells);
    }

    [Fact]
    public void ExecuteCommand_SetsCanUndoTrue()
    {
        var engine = new CalculationEngine();
        var mgr = new CommandManager();

        mgr.ExecuteCommand(new SetCellCommand(engine, _a1, "1"));

        Assert.True(mgr.CanUndo);
    }

    // ── RI-5: ExecuteCommand clears redo ───────────────────────────

    [Fact]
    public void ExecuteCommand_ClearsRedoStack()
    {
        var engine = new CalculationEngine();
        var mgr = new CommandManager();

        mgr.ExecuteCommand(new SetCellCommand(engine, _a1, "1"));
        mgr.Undo();
        Assert.True(mgr.CanRedo);

        // A new edit must invalidate the redo future.
        mgr.ExecuteCommand(new SetCellCommand(engine, _a1, "2"));

        Assert.False(mgr.CanRedo);
    }

    // ── Undo ───────────────────────────────────────────────────────

    [Fact]
    public void Undo_ReversesLastCommand()
    {
        var engine = new CalculationEngine();
        var mgr = new CommandManager();
        engine.SetCellContent(_a1, "1");

        mgr.ExecuteCommand(new SetCellCommand(engine, _a1, "99"));
        Assert.Equal(CellValue.FromNumber(99), engine.GetValue(_a1));

        mgr.Undo();

        Assert.Equal(CellValue.FromNumber(1), engine.GetValue(_a1));
    }

    [Fact]
    public void Undo_ReturnsChangeSet()
    {
        var engine = new CalculationEngine();
        var mgr = new CommandManager();
        mgr.ExecuteCommand(new SetCellCommand(engine, _a1, "42"));

        var result = mgr.Undo();

        Assert.True(result.Success);
        Assert.Contains(_a1, result.ChangedCells);
    }

    [Fact]
    public void Undo_SetsCanRedoTrue()
    {
        var engine = new CalculationEngine();
        var mgr = new CommandManager();
        mgr.ExecuteCommand(new SetCellCommand(engine, _a1, "1"));

        mgr.Undo();

        Assert.True(mgr.CanRedo);
        Assert.False(mgr.CanUndo);
    }

    [Fact]
    public void Undo_MultipleCommands_UndoesInReverseOrder()
    {
        var engine = new CalculationEngine();
        var mgr = new CommandManager();

        mgr.ExecuteCommand(new SetCellCommand(engine, _a1, "1"));
        mgr.ExecuteCommand(new SetCellCommand(engine, _a1, "2"));
        mgr.ExecuteCommand(new SetCellCommand(engine, _a1, "3"));

        mgr.Undo(); // undo "3" → back to "2"
        Assert.Equal(CellValue.FromNumber(2), engine.GetValue(_a1));

        mgr.Undo(); // undo "2" → back to "1"
        Assert.Equal(CellValue.FromNumber(1), engine.GetValue(_a1));

        mgr.Undo(); // undo "1" → back to empty
        Assert.Equal(CellValue.Empty, engine.GetValue(_a1));
    }

    // ── Redo ───────────────────────────────────────────────────────

    [Fact]
    public void Redo_ReappliesUndoneCommand()
    {
        var engine = new CalculationEngine();
        var mgr = new CommandManager();

        mgr.ExecuteCommand(new SetCellCommand(engine, _a1, "42"));
        mgr.Undo();
        Assert.Equal(CellValue.Empty, engine.GetValue(_a1));

        mgr.Redo();

        Assert.Equal(CellValue.FromNumber(42), engine.GetValue(_a1));
    }

    [Fact]
    public void Redo_ReturnsChangeSet()
    {
        var engine = new CalculationEngine();
        var mgr = new CommandManager();
        mgr.ExecuteCommand(new SetCellCommand(engine, _a1, "42"));
        mgr.Undo();

        var result = mgr.Redo();

        Assert.True(result.Success);
        Assert.Contains(_a1, result.ChangedCells);
    }

    [Fact]
    public void Redo_MovesCommandBackToUndoStack()
    {
        var engine = new CalculationEngine();
        var mgr = new CommandManager();
        mgr.ExecuteCommand(new SetCellCommand(engine, _a1, "42"));
        mgr.Undo();

        mgr.Redo();

        Assert.True(mgr.CanUndo);
        Assert.False(mgr.CanRedo);
    }

    [Fact]
    public void Redo_MultipleUndos_RedoesInForwardOrder()
    {
        var engine = new CalculationEngine();
        var mgr = new CommandManager();

        mgr.ExecuteCommand(new SetCellCommand(engine, _a1, "1"));
        mgr.ExecuteCommand(new SetCellCommand(engine, _a1, "2"));
        mgr.ExecuteCommand(new SetCellCommand(engine, _a1, "3"));

        mgr.Undo(); // 3 → 2
        mgr.Undo(); // 2 → 1
        mgr.Undo(); // 1 → empty

        mgr.Redo(); // empty → 1
        Assert.Equal(CellValue.FromNumber(1), engine.GetValue(_a1));

        mgr.Redo(); // 1 → 2
        Assert.Equal(CellValue.FromNumber(2), engine.GetValue(_a1));

        mgr.Redo(); // 2 → 3
        Assert.Equal(CellValue.FromNumber(3), engine.GetValue(_a1));
    }

    // ── Undo/Redo with dependencies ────────────────────────────────

    [Fact]
    public void UndoRedo_PropagatesThroughDependencyChain()
    {
        var engine = new CalculationEngine();
        var mgr = new CommandManager();

        engine.SetCellContent(_a1, "10");
        engine.SetCellContent(_b1, "=A1+1");

        mgr.ExecuteCommand(new SetCellCommand(engine, _a1, "50"));
        Assert.Equal(CellValue.FromNumber(51), engine.GetValue(_b1));

        mgr.Undo();
        Assert.Equal(CellValue.FromNumber(11), engine.GetValue(_b1));

        mgr.Redo();
        Assert.Equal(CellValue.FromNumber(51), engine.GetValue(_b1));
    }

    // ── RI-2: bounded deque — capacity 100 ─────────────────────────

    [Fact]
    public void BoundedDeque_101stCommand_DiscardsOldest()
    {
        var engine = new CalculationEngine();
        var mgr = new CommandManager();

        // Execute 101 commands, each setting A1 to a different value.
        // The zeroth command (value "0") should be discarded when the
        // 101st is pushed.
        for (int i = 0; i <= 100; i++)
            mgr.ExecuteCommand(new SetCellCommand(engine, _a1, i.ToString()));

        // Value should be 100 right now.
        Assert.Equal(CellValue.FromNumber(100), engine.GetValue(_a1));

        // Undo all 100 that fit in the deque (commands 1 through 100).
        for (int i = 0; i < 100; i++)
            mgr.Undo();

        // After undoing all 100, we should be back to the state after
        // command 0 (value "0") — but command 0 itself was discarded,
        // so we can't undo any further.
        Assert.Equal(CellValue.FromNumber(0), engine.GetValue(_a1));
        Assert.False(mgr.CanUndo);
    }

    [Fact]
    public void BoundedDeque_ExactlyAtCapacity_NothingDiscarded()
    {
        var engine = new CalculationEngine();
        var mgr = new CommandManager();

        for (int i = 0; i < 100; i++)
            mgr.ExecuteCommand(new SetCellCommand(engine, _a1, i.ToString()));

        // All 100 should be undoable.
        for (int i = 0; i < 100; i++)
            mgr.Undo();

        Assert.False(mgr.CanUndo);
        Assert.Equal(CellValue.Empty, engine.GetValue(_a1));
    }

    // ── Edge cases ────────────────────────────────────────────────

    [Fact]
    public void CanUndo_AfterUndoingAll_IsFalse()
    {
        var engine = new CalculationEngine();
        var mgr = new CommandManager();
        mgr.ExecuteCommand(new SetCellCommand(engine, _a1, "1"));
        mgr.Undo();

        Assert.False(mgr.CanUndo);
    }

    [Fact]
    public void CanRedo_AfterRedoingAll_IsFalse()
    {
        var engine = new CalculationEngine();
        var mgr = new CommandManager();
        mgr.ExecuteCommand(new SetCellCommand(engine, _a1, "1"));
        mgr.Undo();
        mgr.Redo();

        Assert.False(mgr.CanRedo);
    }

    [Fact]
    public void ExecuteCommand_AfterUndos_ClearsEntireRedoStack()
    {
        var engine = new CalculationEngine();
        var mgr = new CommandManager();

        mgr.ExecuteCommand(new SetCellCommand(engine, _a1, "1"));
        mgr.ExecuteCommand(new SetCellCommand(engine, _a1, "2"));
        mgr.Undo();
        mgr.Undo();

        // Both commands are now on the redo stack.
        // A new command must clear ALL of them.
        mgr.ExecuteCommand(new SetCellCommand(engine, _a1, "999"));

        Assert.False(mgr.CanRedo);
        Assert.Equal(CellValue.FromNumber(999), engine.GetValue(_a1));
    }

    [Fact]
    public void MultipleUndoRedo_AcrossDifferentCells()
    {
        var engine = new CalculationEngine();
        var mgr = new CommandManager();

        mgr.ExecuteCommand(new SetCellCommand(engine, _a1, "10"));
        mgr.ExecuteCommand(new SetCellCommand(engine, _a2, "20"));

        mgr.Undo(); // undo A2
        Assert.Equal(CellValue.FromNumber(10), engine.GetValue(_a1));
        Assert.Equal(CellValue.Empty, engine.GetValue(_a2));

        mgr.Undo(); // undo A1
        Assert.Equal(CellValue.Empty, engine.GetValue(_a1));
        Assert.Equal(CellValue.Empty, engine.GetValue(_a2));

        mgr.Redo(); // redo A1
        Assert.Equal(CellValue.FromNumber(10), engine.GetValue(_a1));
        Assert.Equal(CellValue.Empty, engine.GetValue(_a2));

        mgr.Redo(); // redo A2
        Assert.Equal(CellValue.FromNumber(10), engine.GetValue(_a1));
        Assert.Equal(CellValue.FromNumber(20), engine.GetValue(_a2));
    }
}