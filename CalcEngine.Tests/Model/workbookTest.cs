using CalcEngine.Core;
using CalcEngine.Core.Expressions;
using CalcEngine.Core.Functions;
using CalcEngine.Core.Model;
using Xunit;

namespace CalcEngine.Tests.Model;

/// <summary>
/// Tests for Workbook — sparse Dictionary&lt;CellRef, Cell&gt; storage
/// (Design_Portfolio 4.2, 6.4) and its role as the IEvalContext used
/// during formula evaluation (4.4, IEvalContext.cs).
/// </summary>
public class WorkbookTests
{
    private readonly CellRef _a1 = CellRef.Parse("A1");
    private readonly CellRef _a2 = CellRef.Parse("A2");
    private readonly CellRef _a3 = CellRef.Parse("A3");
    private readonly CellRef _b1 = CellRef.Parse("B1");
    private readonly CellRef _b2 = CellRef.Parse("B2");
    private readonly CellRef _c5 = CellRef.Parse("C5");

    // ── Empty workbook ─────────────────────────────────────────────

    [Fact]
    public void NewWorkbook_CountIsZero()
    {
        var wb = new Workbook();
        Assert.Equal(0, wb.Count);
    }

    // ── GetOrCreate ────────────────────────────────────────────────

    [Fact]
    public void GetOrCreate_ReturnsCell()
    {
        var wb = new Workbook();
        var cell = wb.GetOrCreate(_a1);
        Assert.NotNull(cell);
    }

    [Fact]
    public void GetOrCreate_CellHasCorrectRef()
    {
        var wb = new Workbook();
        var cell = wb.GetOrCreate(_b2);
        Assert.Equal(_b2, cell.Ref);
    }

    [Fact]
    public void GetOrCreate_NewCellIsEmpty()
    {
        var wb = new Workbook();
        var cell = wb.GetOrCreate(_a1);
        Assert.Equal(string.Empty, cell.RawInput);
        Assert.Equal(CellValue.Empty, cell.Value);
    }

    [Fact]
    public void GetOrCreate_ReturnsSameInstanceOnSecondCall()
    {
        var wb = new Workbook();
        var first = wb.GetOrCreate(_a1);
        var second = wb.GetOrCreate(_a1);
        Assert.Same(first, second);
    }

    [Fact]
    public void GetOrCreate_DifferentRefsReturnDifferentCells()
    {
        var wb = new Workbook();
        var cellA1 = wb.GetOrCreate(_a1);
        var cellB2 = wb.GetOrCreate(_b2);
        Assert.NotSame(cellA1, cellB2);
    }

    [Fact]
    public void GetOrCreate_IncrementsCount()
    {
        var wb = new Workbook();
        wb.GetOrCreate(_a1);
        Assert.Equal(1, wb.Count);
        wb.GetOrCreate(_b2);
        Assert.Equal(2, wb.Count);
    }

    [Fact]
    public void GetOrCreate_SameRefDoesNotIncrementCount()
    {
        var wb = new Workbook();
        wb.GetOrCreate(_a1);
        wb.GetOrCreate(_a1);
        Assert.Equal(1, wb.Count);
    }

    // ── TryGet ─────────────────────────────────────────────────────

    [Fact]
    public void TryGet_ReturnsNullForAbsentRef()
    {
        var wb = new Workbook();
        Assert.Null(wb.TryGet(_a1));
    }

    [Fact]
    public void TryGet_ReturnsCellAfterGetOrCreate()
    {
        var wb = new Workbook();
        var created = wb.GetOrCreate(_a1);
        var found = wb.TryGet(_a1);
        Assert.Same(created, found);
    }

    [Fact]
    public void TryGet_DoesNotCreateCell()
    {
        var wb = new Workbook();
        wb.TryGet(_a1);
        Assert.Equal(0, wb.Count);
    }

    // ── Remove ─────────────────────────────────────────────────────

    [Fact]
    public void Remove_ReturnsTrueForExistingCell()
    {
        var wb = new Workbook();
        wb.GetOrCreate(_a1);
        Assert.True(wb.Remove(_a1));
    }

    [Fact]
    public void Remove_ReturnsFalseForAbsentRef()
    {
        var wb = new Workbook();
        Assert.False(wb.Remove(_a1));
    }

    [Fact]
    public void Remove_CellNoLongerFound()
    {
        var wb = new Workbook();
        wb.GetOrCreate(_a1);
        wb.Remove(_a1);
        Assert.Null(wb.TryGet(_a1));
    }

    [Fact]
    public void Remove_DecrementsCount()
    {
        var wb = new Workbook();
        wb.GetOrCreate(_a1);
        wb.GetOrCreate(_b2);
        wb.Remove(_a1);
        Assert.Equal(1, wb.Count);
    }

    [Fact]
    public void Remove_ThenGetOrCreate_CreatesNewInstance()
    {
        var wb = new Workbook();
        var original = wb.GetOrCreate(_a1);
        original.SetLiteral("42", CellValue.FromNumber(42));

        wb.Remove(_a1);
        var recreated = wb.GetOrCreate(_a1);

        Assert.NotSame(original, recreated);
        Assert.Equal(CellValue.Empty, recreated.Value);
    }

    // ── IEvalContext: GetCellValue ─────────────────────────────────

    [Fact]
    public void EvalContext_GetCellValue_ReturnsEmptyForAbsentCell()
    {
        var wb = new Workbook();
        IEvalContext ctx = wb;
        Assert.Equal(CellValue.Empty, ctx.GetCellValue(_c5));
    }

    [Fact]
    public void EvalContext_GetCellValue_ReturnsCellValue()
    {
        var wb = new Workbook();
        var cell = wb.GetOrCreate(_a1);
        cell.SetLiteral("42", CellValue.FromNumber(42));

        IEvalContext ctx = wb;
        Assert.Equal(CellValue.FromNumber(42), ctx.GetCellValue(_a1));
    }

    [Fact]
    public void EvalContext_GetCellValue_DoesNotCreateCell()
    {
        var wb = new Workbook();
        IEvalContext ctx = wb;
        ctx.GetCellValue(_a1);
        Assert.Equal(0, wb.Count);
    }

    [Fact]
    public void EvalContext_GetCellValue_ReflectsUpdatedValue()
    {
        var wb = new Workbook();
        var cell = wb.GetOrCreate(_a1);
        cell.SetLiteral("1", CellValue.FromNumber(1));

        IEvalContext ctx = wb;
        Assert.Equal(CellValue.FromNumber(1), ctx.GetCellValue(_a1));

        cell.SetValue(CellValue.FromNumber(999));
        Assert.Equal(CellValue.FromNumber(999), ctx.GetCellValue(_a1));
    }

    // ── IEvalContext: GetRangeValues ────────────────────────────────

    [Fact]
    public void EvalContext_GetRangeValues_ReturnsEmptyForEmptyRange()
    {
        var wb = new Workbook();
        var range = new CellRange(CellRef.Parse("D1"), CellRef.Parse("D3"));

        IEvalContext ctx = wb;
        var values = ctx.GetRangeValues(range);

        Assert.Equal(3, values.Count);
        Assert.All(values, v => Assert.Equal(CellValue.Empty, v));
    }

    [Fact]
    public void EvalContext_GetRangeValues_ReturnsCellValuesInRowMajorOrder()
    {
        var wb = new Workbook();
        wb.GetOrCreate(_a1).SetLiteral("10", CellValue.FromNumber(10));
        wb.GetOrCreate(_a2).SetLiteral("20", CellValue.FromNumber(20));
        wb.GetOrCreate(_a3).SetLiteral("30", CellValue.FromNumber(30));

        var range = new CellRange(_a1, _a3);

        IEvalContext ctx = wb;
        var values = ctx.GetRangeValues(range);

        Assert.Equal(3, values.Count);
        Assert.Equal(CellValue.FromNumber(10), values[0]);
        Assert.Equal(CellValue.FromNumber(20), values[1]);
        Assert.Equal(CellValue.FromNumber(30), values[2]);
    }

    [Fact]
    public void EvalContext_GetRangeValues_MixedOccupiedAndEmpty()
    {
        // A1=10, A2=empty, A3=30
        var wb = new Workbook();
        wb.GetOrCreate(_a1).SetLiteral("10", CellValue.FromNumber(10));
        wb.GetOrCreate(_a3).SetLiteral("30", CellValue.FromNumber(30));

        var range = new CellRange(_a1, _a3);

        IEvalContext ctx = wb;
        var values = ctx.GetRangeValues(range);

        Assert.Equal(3, values.Count);
        Assert.Equal(CellValue.FromNumber(10), values[0]);
        Assert.Equal(CellValue.Empty, values[1]);
        Assert.Equal(CellValue.FromNumber(30), values[2]);
    }

    [Fact]
    public void EvalContext_GetRangeValues_MultiColumnRange()
    {
        // A1=1, B1=2 — range A1:B1 is 1 row x 2 columns, row-major:
        // A1 then B1 (matches CellRange.GetCells()).
        var wb = new Workbook();
        wb.GetOrCreate(_a1).SetLiteral("1", CellValue.FromNumber(1));
        wb.GetOrCreate(_b1).SetLiteral("2", CellValue.FromNumber(2));

        var range = new CellRange(_a1, _b1);

        IEvalContext ctx = wb;
        var values = ctx.GetRangeValues(range);

        Assert.Equal(2, values.Count);
        Assert.Equal(CellValue.FromNumber(1), values[0]);
        Assert.Equal(CellValue.FromNumber(2), values[1]);
    }

    [Fact]
    public void EvalContext_GetRangeValues_DoesNotCreateCells()
    {
        var wb = new Workbook();
        var range = new CellRange(_a1, _a3);

        IEvalContext ctx = wb;
        ctx.GetRangeValues(range);

        Assert.Equal(0, wb.Count);
    }

    // ── IEvalContext: CallFunction ──────────────────────────────────
    // Workbook must delegate to a FunctionFactory so expression trees
    // can call SUM/IF/etc. through the same context that resolves
    // cell and range references.

    [Fact]
    public void EvalContext_CallFunction_UnknownNameReturnsNameError()
    {
        var wb = new Workbook();
        IEvalContext ctx = wb;

        var result = ctx.CallFunction("NOTAREALFUNCTION", Array.Empty<IExpression>());

        Assert.Equal(CellValue.FromError(ErrorKind.Name), result);
    }

    [Fact]
    public void EvalContext_CallFunction_SumDelegatesToDefaultFactory()
    {
        var wb = new Workbook();
        IEvalContext ctx = wb;

        IReadOnlyList<IExpression> args = new IExpression[]
        {
            new NumberExpression(2),
            new NumberExpression(3),
        };

        var result = ctx.CallFunction("SUM", args);

        Assert.Equal(CellValue.FromNumber(5), result);
    }

    [Fact]
    public void EvalContext_CallFunction_CanReadLiveCellValuesThroughSameContext()
    {
        // SUM(A1, A2) exercises CallFunction -> FunctionFactory ->
        // SumStrategy -> args evaluated against the SAME Workbook
        // instance acting as IEvalContext (Workbook passes itself).
        var wb = new Workbook();
        wb.GetOrCreate(_a1).SetLiteral("4", CellValue.FromNumber(4));
        wb.GetOrCreate(_a2).SetLiteral("6", CellValue.FromNumber(6));

        IEvalContext ctx = wb;
        IReadOnlyList<IExpression> args = new IExpression[]
        {
            new CellRefExpression(_a1),
            new CellRefExpression(_a2),
        };

        var result = ctx.CallFunction("SUM", args);

        Assert.Equal(CellValue.FromNumber(10), result);
    }

    [Fact]
    public void Constructor_AcceptsCustomFunctionFactory()
    {
        // A caller (e.g. tests, or a future extension registering a
        // custom function) can supply its own factory instead of the
        // default eight-function set.
        var customFactory = new FunctionFactory();
        customFactory.Register(new SumStrategy());

        var wb = new Workbook(customFactory);
        IEvalContext ctx = wb;

        var result = ctx.CallFunction("ROUND", new IExpression[] { new NumberExpression(1.2) });

        // ROUND was never registered on the custom factory, so this
        // must still be #NAME? even though the default factory knows it.
        Assert.Equal(CellValue.FromError(ErrorKind.Name), result);
    }
}