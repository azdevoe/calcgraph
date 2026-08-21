using CalcEngine.Core;
using CalcEngine.Core.Expressions;
using CalcEngine.Core.Model;
using Xunit;

namespace CalcEngine.Tests.Model;

/// <summary>
/// Tests for Cell — one occupied slot in the Workbook. Covers the three
/// states a cell can be in (empty, literal, formula) and the transitions
/// between them, per Design_Portfolio 4.3 and the Workbook RI in 6.4.
/// </summary>
public class CellTests
{
    private readonly CellRef _a1 = CellRef.Parse("A1");
    private readonly CellRef _b2 = CellRef.Parse("B2");

    // ── Construction defaults ──────────────────────────────────────

    [Fact]
    public void NewCell_HasCorrectRef()
    {
        var cell = new Cell(_a1);
        Assert.Equal(_a1, cell.Ref);
    }

    [Fact]
    public void NewCell_RawInputIsEmpty()
    {
        var cell = new Cell(_a1);
        Assert.Equal(string.Empty, cell.RawInput);
    }

    [Fact]
    public void NewCell_ValueIsEmpty()
    {
        var cell = new Cell(_a1);
        Assert.Equal(CellValue.Empty, cell.Value);
    }

    [Fact]
    public void NewCell_TreeIsNull()
    {
        var cell = new Cell(_a1);
        Assert.Null(cell.Tree);
    }

    [Fact]
    public void NewCell_IsFormulaIsFalse()
    {
        var cell = new Cell(_a1);
        Assert.False(cell.IsFormula);
    }

    // ── SetLiteral ─────────────────────────────────────────────────

    [Fact]
    public void SetLiteral_UpdatesRawInput()
    {
        var cell = new Cell(_a1);
        cell.SetLiteral("42", CellValue.FromNumber(42));
        Assert.Equal("42", cell.RawInput);
    }

    [Fact]
    public void SetLiteral_UpdatesValue()
    {
        var cell = new Cell(_a1);
        cell.SetLiteral("42", CellValue.FromNumber(42));
        Assert.Equal(CellValue.FromNumber(42), cell.Value);
    }

    [Fact]
    public void SetLiteral_TreeIsNull()
    {
        var cell = new Cell(_a1);
        cell.SetLiteral("hello", CellValue.FromText("hello"));
        Assert.Null(cell.Tree);
    }

    [Fact]
    public void SetLiteral_IsFormulaIsFalse()
    {
        var cell = new Cell(_a1);
        cell.SetLiteral("42", CellValue.FromNumber(42));
        Assert.False(cell.IsFormula);
    }

    [Fact]
    public void SetLiteral_TextValue()
    {
        var cell = new Cell(_a1);
        cell.SetLiteral("hello", CellValue.FromText("hello"));
        Assert.Equal(CellValue.FromText("hello"), cell.Value);
    }

    [Fact]
    public void SetLiteral_BooleanValue()
    {
        var cell = new Cell(_a1);
        cell.SetLiteral("TRUE", CellValue.FromBoolean(true));
        Assert.Equal(CellValue.FromBoolean(true), cell.Value);
    }

    // ── SetFormula ─────────────────────────────────────────────────
    // NumberExpression is used here as a stand-in tree; Cell must not
    // care about the concrete IExpression type.

    [Fact]
    public void SetFormula_UpdatesRawInput()
    {
        var cell = new Cell(_a1);
        var tree = new NumberExpression(10);
        cell.SetFormula("=10", tree);
        Assert.Equal("=10", cell.RawInput);
    }

    [Fact]
    public void SetFormula_StoresTree()
    {
        var cell = new Cell(_a1);
        var tree = new NumberExpression(10);
        cell.SetFormula("=10", tree);
        Assert.Same(tree, cell.Tree);
    }

    [Fact]
    public void SetFormula_IsFormulaIsTrue()
    {
        var cell = new Cell(_a1);
        var tree = new NumberExpression(10);
        cell.SetFormula("=10", tree);
        Assert.True(cell.IsFormula);
    }

    [Fact]
    public void SetFormula_ValueUnchangedUntilEngineEvaluates()
    {
        // SetFormula only stores the tree. The engine calls SetValue
        // separately once Pass 2 has actually computed a result —
        // Cell itself never evaluates its own tree.
        var cell = new Cell(_a1);
        var tree = new NumberExpression(10);
        cell.SetFormula("=10", tree);
        Assert.Equal(CellValue.Empty, cell.Value);
    }

    // ── SetValue (recalculation path) ──────────────────────────────

    [Fact]
    public void SetValue_UpdatesValue()
    {
        var cell = new Cell(_a1);
        cell.SetValue(CellValue.FromNumber(99));
        Assert.Equal(CellValue.FromNumber(99), cell.Value);
    }

    [Fact]
    public void SetValue_DoesNotTouchRawInput()
    {
        var cell = new Cell(_a1);
        var tree = new NumberExpression(5);
        cell.SetFormula("=5", tree);
        cell.SetValue(CellValue.FromNumber(5));
        Assert.Equal("=5", cell.RawInput);
    }

    [Fact]
    public void SetValue_DoesNotTouchTree()
    {
        var cell = new Cell(_a1);
        var tree = new NumberExpression(5);
        cell.SetFormula("=5", tree);
        cell.SetValue(CellValue.FromNumber(5));
        Assert.Same(tree, cell.Tree);
    }

    // ── Clear ──────────────────────────────────────────────────────

    [Fact]
    public void Clear_ResetsRawInput()
    {
        var cell = new Cell(_a1);
        cell.SetLiteral("42", CellValue.FromNumber(42));
        cell.Clear();
        Assert.Equal(string.Empty, cell.RawInput);
    }

    [Fact]
    public void Clear_ResetsValue()
    {
        var cell = new Cell(_a1);
        cell.SetLiteral("42", CellValue.FromNumber(42));
        cell.Clear();
        Assert.Equal(CellValue.Empty, cell.Value);
    }

    [Fact]
    public void Clear_ResetsTree()
    {
        var cell = new Cell(_a1);
        var tree = new NumberExpression(10);
        cell.SetFormula("=10", tree);
        cell.Clear();
        Assert.Null(cell.Tree);
    }

    [Fact]
    public void Clear_ResetsIsFormula()
    {
        var cell = new Cell(_a1);
        var tree = new NumberExpression(10);
        cell.SetFormula("=10", tree);
        cell.Clear();
        Assert.False(cell.IsFormula);
    }

    // ── Transitions between states ─────────────────────────────────

    [Fact]
    public void FormulaToLiteral_ClearsTree()
    {
        var cell = new Cell(_a1);
        var tree = new NumberExpression(10);
        cell.SetFormula("=10", tree);
        cell.SetLiteral("hello", CellValue.FromText("hello"));
        Assert.Null(cell.Tree);
        Assert.False(cell.IsFormula);
    }

    [Fact]
    public void LiteralToFormula_SetsTree()
    {
        var cell = new Cell(_a1);
        cell.SetLiteral("42", CellValue.FromNumber(42));
        var tree = new NumberExpression(99);
        cell.SetFormula("=99", tree);
        Assert.Same(tree, cell.Tree);
        Assert.True(cell.IsFormula);
    }

    [Fact]
    public void SetFormula_ThenSetValue_ThenClear_FullCycle()
    {
        var cell = new Cell(_a1);

        // 1. Formula entered — tree cached, not yet evaluated
        var tree = new NumberExpression(7);
        cell.SetFormula("=7", tree);
        Assert.True(cell.IsFormula);

        // 2. Engine runs Pass 2, pushes the result in
        cell.SetValue(CellValue.FromNumber(7));
        Assert.Equal(CellValue.FromNumber(7), cell.Value);

        // 3. User clears the cell
        cell.Clear();
        Assert.Equal(string.Empty, cell.RawInput);
        Assert.Equal(CellValue.Empty, cell.Value);
        Assert.Null(cell.Tree);
        Assert.False(cell.IsFormula);
    }
}