using CalcEngine.Core.ChangeTracking;
using CalcEngine.Core.Engine;
using CalcEngine.Core.Filtering;
using CalcEngine.Core.Model;
using CalcEngine.Core.Validation;
using CalcEngine.Core.Sorting;
using Xunit;

namespace CalcEngine.Tests.Sorting;

/// <summary>
/// Integration tests for CalculationEngine.SortRange, SetFilter and
/// GetVisibleRows (Group C feature: Sorting &amp; Filtering) — the whole
/// pipeline through the public API: undo/redo, dependency-graph
/// consistency, and the "filtering never mutates anything" guarantee
/// the project plan calls out explicitly.
/// </summary>
public class CalculationEngineSortAndFilterTests
{
    private static CellRef Ref(string a1) => CellRef.Parse(a1);

    // ── Sorting ──────────────────────────────────────────────────────

    [Fact]
    public void SortRange_Numbers_OrdersAscending()
    {
        var engine = new CalculationEngine();
        engine.SetCellContent(Ref("A1"), "30");
        engine.SetCellContent(Ref("A2"), "10");
        engine.SetCellContent(Ref("A3"), "20");

        var result = engine.SortRange(
            CellRange.Parse("A1:A3"),
            new[] { new SortKey(1, new AscendingComparer()) });

        Assert.True(result.Success);
        Assert.Equal(10, engine.GetValue(Ref("A1")).Number);
        Assert.Equal(20, engine.GetValue(Ref("A2")).Number);
        Assert.Equal(30, engine.GetValue(Ref("A3")).Number);
    }

    [Fact]
    public void SortRange_MovesWholeRow_NotJustTheKeyColumn()
    {
        var engine = new CalculationEngine();
        engine.SetCellContent(Ref("A1"), "2");
        engine.SetCellContent(Ref("B1"), "\"second\"");
        engine.SetCellContent(Ref("A2"), "1");
        engine.SetCellContent(Ref("B2"), "\"first\"");

        engine.SortRange(CellRange.Parse("A1:B2"), new[] { new SortKey(1, new AscendingComparer()) });

        Assert.Equal(1, engine.GetValue(Ref("A1")).Number);
        Assert.Equal("first", engine.GetValue(Ref("B1")).Text);
        Assert.Equal(2, engine.GetValue(Ref("A2")).Number);
        Assert.Equal("second", engine.GetValue(Ref("B2")).Text);
    }

    [Fact]
    public void SortRange_HeaderRow_IsExcludedFromReordering()
    {
        var engine = new CalculationEngine();
        engine.SetCellContent(Ref("A1"), "\"Header\"");
        engine.SetCellContent(Ref("A2"), "30");
        engine.SetCellContent(Ref("A3"), "10");

        engine.SortRange(
            CellRange.Parse("A1:A3"),
            new[] { new SortKey(1, new AscendingComparer()) },
            hasHeader: true);

        Assert.Equal("Header", engine.GetValue(Ref("A1")).Text);
        Assert.Equal(10, engine.GetValue(Ref("A2")).Number);
        Assert.Equal(30, engine.GetValue(Ref("A3")).Number);
    }

    [Fact]
    public void SortRange_FormulaReferencingInsideRange_IsTranslated()
    {
        var engine = new CalculationEngine();
        // A1=2, A2=1, B2 = "=A2+1" references its own row, inside the range.
        engine.SetCellContent(Ref("A1"), "2");
        engine.SetCellContent(Ref("A2"), "1");
        engine.SetCellContent(Ref("B2"), "=A2+1");

        engine.SortRange(CellRange.Parse("A1:B2"), new[] { new SortKey(1, new AscendingComparer()) });

        // Row with A=1 (was row 2) moves to row 1; its formula must now
        // read A1 (its own new row), not the pre-move A2.
        Assert.Equal("=(A1+1)", engine.GetFormula(Ref("B1")));
        Assert.Equal(2, engine.GetValue(Ref("B1")).Number);
    }

    [Fact]
    public void SortRange_FormulaReferencingOutsideRange_IsTranslatedByTheSameDelta()
    {
        var engine = new CalculationEngine();
        engine.SetCellContent(Ref("Z1"), "100");
        engine.SetCellContent(Ref("Z2"), "200");
        engine.SetCellContent(Ref("A1"), "2");
        engine.SetCellContent(Ref("B1"), "=Z1");
        engine.SetCellContent(Ref("A2"), "1");
        engine.SetCellContent(Ref("B2"), "=Z2");

        engine.SortRange(CellRange.Parse("A1:B2"), new[] { new SortKey(1, new AscendingComparer()) });

        // Row that held A=1 (row 2, referencing Z2) moves up to row 1;
        // its move delta (-1) applies to the Z reference too, giving Z1.
        Assert.Equal("=Z1", engine.GetFormula(Ref("B1")));
        Assert.Equal(100, engine.GetValue(Ref("B1")).Number);
    }

    [Fact]
    public void SortRange_MultiKey_SecondColumnBreaksTies()
    {
        var engine = new CalculationEngine();
        engine.SetCellContent(Ref("A1"), "\"A\"");
        engine.SetCellContent(Ref("B1"), "2");
        engine.SetCellContent(Ref("A2"), "\"A\"");
        engine.SetCellContent(Ref("B2"), "1");

        engine.SortRange(
            CellRange.Parse("A1:B2"),
            new[]
            {
                new SortKey(1, new AscendingComparer()),
                new SortKey(2, new AscendingComparer())
            });

        Assert.Equal(1, engine.GetValue(Ref("B1")).Number);
        Assert.Equal(2, engine.GetValue(Ref("B2")).Number);
    }

    [Fact]
    public void SortRange_Undo_RestoresExactPriorContentIncludingFormulaText()
    {
        var engine = new CalculationEngine();
        engine.SetCellContent(Ref("A1"), "2");
        engine.SetCellContent(Ref("B1"), "=A1*10");
        engine.SetCellContent(Ref("A2"), "1");
        engine.SetCellContent(Ref("B2"), "=A2*10");

        engine.SortRange(CellRange.Parse("A1:B2"), new[] { new SortKey(1, new AscendingComparer()) });
        Assert.Equal(1, engine.GetValue(Ref("A1")).Number); // sort took effect

        var undoResult = engine.Undo();

        Assert.True(undoResult.Success);
        Assert.Equal(2, engine.GetValue(Ref("A1")).Number);
        Assert.Equal("=A1*10", engine.GetFormula(Ref("B1")));
        Assert.Equal(1, engine.GetValue(Ref("A2")).Number);
        Assert.Equal("=A2*10", engine.GetFormula(Ref("B2")));
    }

    [Fact]
    public void SortRange_RejectedByValidation_LeavesWorkbookUnchanged()
    {
        var engine = new CalculationEngine();
        engine.SetCellContent(Ref("A1"), "30");
        engine.SetCellContent(Ref("A2"), "10");

        // A1 may only ever hold a value >= 20; after the sort A1 would
        // receive 10, which the rule must reject.
        engine.SetValidationRule(Ref("A1"), new RangeRule(20, double.MaxValue));

        var result = engine.SortRange(CellRange.Parse("A1:A2"), new[] { new SortKey(1, new AscendingComparer()) });

        Assert.False(result.Success);
        Assert.Equal(30, engine.GetValue(Ref("A1")).Number);
        Assert.Equal(10, engine.GetValue(Ref("A2")).Number);
    }

    [Fact]
    public void SortRange_KeyColumnOutsideRange_Throws()
    {
        var engine = new CalculationEngine();
        Assert.Throws<ArgumentException>(() =>
            engine.SortRange(CellRange.Parse("A1:A3"), new[] { new SortKey(2, new AscendingComparer()) }));
    }

    [Fact]
    public void SortRange_BlankCellWithinRange_MovesAsBlank()
    {
        var engine = new CalculationEngine();
        engine.SetCellContent(Ref("A1"), "2");
        // B1 left blank on purpose.
        engine.SetCellContent(Ref("A2"), "1");
        engine.SetCellContent(Ref("B2"), "\"has data\"");

        engine.SortRange(CellRange.Parse("A1:B2"), new[] { new SortKey(1, new AscendingComparer()) });

        Assert.Equal(1, engine.GetValue(Ref("A1")).Number);
        Assert.Equal("has data", engine.GetValue(Ref("B1")).Text);
        Assert.Equal(2, engine.GetValue(Ref("A2")).Number);
        Assert.Equal(ValueKind.Empty, engine.GetValue(Ref("B2")).Kind);
        Assert.Equal(string.Empty, engine.GetFormula(Ref("B2")));
    }

    [Fact]
    public void SortRange_TranslationWouldGoOutsideSheet_IsRejectedNotThrown_AndLeavesWorkbookUnchanged()
    {
        var engine = new CalculationEngine();
        // Row 3 (A=1, the smaller value) will sort above row 2 (A=5),
        // moving up by one row. Its formula references row 1 — after a
        // one-row-up move that reference would need to become row 0,
        // which is outside the sheet.
        engine.SetCellContent(Ref("A1"), "999");
        engine.SetCellContent(Ref("A2"), "5");
        engine.SetCellContent(Ref("A3"), "1");
        engine.SetCellContent(Ref("B3"), "=A1");

        var result = engine.SortRange(CellRange.Parse("A2:B3"), new[] { new SortKey(1, new AscendingComparer()) });

        Assert.False(result.Success);
        Assert.Equal(ChangeFailureReason.ParseError, result.FailureReason);
        // Nothing was written — Plan() runs entirely before any ApplyEdit.
        Assert.Equal(5, engine.GetValue(Ref("A2")).Number);
        Assert.Equal(1, engine.GetValue(Ref("A3")).Number);
        Assert.Equal("=A1", engine.GetFormula(Ref("B3")));

        // The rejected sort never reached the undo stack — the next
        // undo reverts the last real edit (setting B3), not the sort.
        engine.Undo();
        Assert.Equal(string.Empty, engine.GetFormula(Ref("B3")));
    }

    // ── Filtering ────────────────────────────────────────────────────

    [Fact]
    public void GetVisibleRows_NoFilter_ReturnsEveryRow()
    {
        var engine = new CalculationEngine();
        var range = CellRange.Parse("A1:A3");

        var visible = engine.GetVisibleRows(range);

        Assert.Equal(new[] { 1, 2, 3 }, visible);
    }

    [Fact]
    public void SetFilter_HidesNonMatchingRows()
    {
        var engine = new CalculationEngine();
        engine.SetCellContent(Ref("A1"), "50");
        engine.SetCellContent(Ref("A2"), "150");
        engine.SetCellContent(Ref("A3"), "75");
        var range = CellRange.Parse("A1:A3");

        engine.SetFilter(range, column: 1, new NumberRangeFilter(0, 100));

        Assert.Equal(new[] { 1, 3 }, engine.GetVisibleRows(range));
    }

    [Fact]
    public void SetFilter_MultipleColumns_CombineWithAnd()
    {
        var engine = new CalculationEngine();
        engine.SetCellContent(Ref("A1"), "50");
        engine.SetCellContent(Ref("B1"), "\"pass\"");
        engine.SetCellContent(Ref("A2"), "50");
        engine.SetCellContent(Ref("B2"), "\"fail\"");
        var range = CellRange.Parse("A1:B2");

        engine.SetFilter(range, column: 1, new NumberRangeFilter(0, 100));
        engine.SetFilter(range, column: 2, new TextContainsFilter("pass"));

        Assert.Equal(new[] { 1 }, engine.GetVisibleRows(range));
    }

    [Fact]
    public void SetFilter_OverRangeContainingErrors_ErrorRowsSimplyDoNotMatch()
    {
        var engine = new CalculationEngine();
        engine.SetCellContent(Ref("A1"), "=1/0");
        engine.SetCellContent(Ref("A2"), "50");
        var range = CellRange.Parse("A1:A2");

        engine.SetFilter(range, column: 1, new NumberRangeFilter(0, 100));

        Assert.Equal(new[] { 2 }, engine.GetVisibleRows(range));
    }

    [Fact]
    public void ClearFilter_RestoresAllRowsVisible()
    {
        var engine = new CalculationEngine();
        engine.SetCellContent(Ref("A1"), "150");
        var range = CellRange.Parse("A1:A1");
        engine.SetFilter(range, column: 1, new NumberRangeFilter(0, 100));
        Assert.Empty(engine.GetVisibleRows(range));

        engine.ClearFilter(range, column: 1);

        Assert.Equal(new[] { 1 }, engine.GetVisibleRows(range));
    }

    [Fact]
    public void SetFilter_Undo_RemovesTheFilter()
    {
        var engine = new CalculationEngine();
        engine.SetCellContent(Ref("A1"), "150");
        var range = CellRange.Parse("A1:A1");

        engine.SetFilter(range, column: 1, new NumberRangeFilter(0, 100));
        Assert.Empty(engine.GetVisibleRows(range));

        engine.Undo();

        Assert.Equal(new[] { 1 }, engine.GetVisibleRows(range));
    }

    [Fact]
    public void Filtering_NeverChangesCellValuesOrTheDependencyGraph()
    {
        var engine = new CalculationEngine();
        engine.SetCellContent(Ref("A1"), "50");
        engine.SetCellContent(Ref("A2"), "150");
        engine.SetCellContent(Ref("B1"), "=A1+1");
        var range = CellRange.Parse("A1:A2");

        var beforeA1 = engine.GetValue(Ref("A1"));
        var beforeA2 = engine.GetValue(Ref("A2"));
        var beforeB1 = engine.GetValue(Ref("B1"));
        var beforeB1Formula = engine.GetFormula(Ref("B1"));

        engine.SetFilter(range, column: 1, new NumberRangeFilter(0, 100));

        Assert.Equal(beforeA1, engine.GetValue(Ref("A1")));
        Assert.Equal(beforeA2, engine.GetValue(Ref("A2")));
        Assert.Equal(beforeB1, engine.GetValue(Ref("B1")));
        Assert.Equal(beforeB1Formula, engine.GetFormula(Ref("B1")));
    }
}
