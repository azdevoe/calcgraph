using CalcEngine.Core;
using Xunit;

namespace CalcEngine.Tests;

/// <summary>
/// Tests for Workbook.AllCells() — a small addition to the Workbook
/// built earlier. CalculationEngine.RecalculateAll needs to walk every
/// occupied formula cell, and Workbook had no way to enumerate its
/// contents (only GetOrCreate/TryGet/Remove, which all address a single
/// cell). Kept in its own file rather than merged into the existing
/// WorkbookTests.cs, so nothing you already have needs editing.
/// </summary>
public class WorkbookAllCellsTests
{
    private readonly CellRef _a1 = CellRef.Parse("A1");
    private readonly CellRef _b2 = CellRef.Parse("B2");

    [Fact]
    public void NewWorkbook_AllCellsIsEmpty()
    {
        var wb = new Workbook();
        Assert.Empty(wb.AllCells());
    }

    [Fact]
    public void AllCells_ReturnsCreatedCells()
    {
        var wb = new Workbook();
        wb.GetOrCreate(_a1);
        wb.GetOrCreate(_b2);

        var all = wb.AllCells();

        Assert.Equal(2, all.Count);
    }

    [Fact]
    public void AllCells_ContainsTheActualCellInstances()
    {
        var wb = new Workbook();
        var cell = wb.GetOrCreate(_a1);

        Assert.Contains(cell, wb.AllCells());
    }

    [Fact]
    public void AllCells_ReflectsRemoval()
    {
        var wb = new Workbook();
        wb.GetOrCreate(_a1);
        wb.GetOrCreate(_b2);
        wb.Remove(_a1);

        var all = wb.AllCells();

        Assert.Single(all);
        Assert.DoesNotContain(all, c => c.Ref == _a1);
    }
}