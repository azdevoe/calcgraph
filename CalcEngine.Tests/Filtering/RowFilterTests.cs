using CalcEngine.Core;
using CalcEngine.Core.Model;
using CalcEngine.Core.Filtering;
using Xunit;

namespace CalcEngine.Tests.Filtering;

/// <summary>
/// Tests for the row-filter family — IRowFilter and its three
/// implementations (Group C feature: Sorting & Filtering). Filtering
/// is a read-only query (CalculationEngine.FilterRange): it never
/// mutates the workbook, the same way Excel's filter hides rows
/// rather than deleting data, so these strategies only need to answer
/// Matches(value) — no IEvalContext, no undo/redo involved.
/// </summary>
public class RowFilterTests
{
    // ── NumberRangeFilter ────────────────────────────────────────

    [Fact]
    public void NumberRangeFilter_WithinRange_Matches()
    {
        var filter = new NumberRangeFilter(min: 0, max: 100);
        Assert.True(filter.Matches(CellValue.FromNumber(50)));
    }

    [Fact]
    public void NumberRangeFilter_AtBoundaries_Matches()
    {
        var filter = new NumberRangeFilter(min: 0, max: 100);
        Assert.True(filter.Matches(CellValue.FromNumber(0)));
        Assert.True(filter.Matches(CellValue.FromNumber(100)));
    }

    [Fact]
    public void NumberRangeFilter_OutsideRange_DoesNotMatch()
    {
        var filter = new NumberRangeFilter(min: 0, max: 100);
        Assert.False(filter.Matches(CellValue.FromNumber(-1)));
        Assert.False(filter.Matches(CellValue.FromNumber(101)));
    }

    [Fact]
    public void NumberRangeFilter_NonNumber_DoesNotMatch()
    {
        var filter = new NumberRangeFilter(min: 0, max: 100);
        Assert.False(filter.Matches(CellValue.FromText("50")));
        Assert.False(filter.Matches(CellValue.Empty));
    }

    // ── TextContainsFilter ───────────────────────────────────────

    [Fact]
    public void TextContainsFilter_SubstringPresent_Matches()
    {
        var filter = new TextContainsFilter("pass");
        Assert.True(filter.Matches(CellValue.FromText("Passed")));
    }

    [Fact]
    public void TextContainsFilter_CaseInsensitiveByDefault_Matches()
    {
        var filter = new TextContainsFilter("PASS");
        Assert.True(filter.Matches(CellValue.FromText("passed")));
    }

    [Fact]
    public void TextContainsFilter_SubstringAbsent_DoesNotMatch()
    {
        var filter = new TextContainsFilter("fail");
        Assert.False(filter.Matches(CellValue.FromText("Passed")));
    }

    [Fact]
    public void TextContainsFilter_NonText_DoesNotMatch()
    {
        var filter = new TextContainsFilter("50");
        Assert.False(filter.Matches(CellValue.FromNumber(50)));
    }

    // ── NonEmptyFilter ───────────────────────────────────────────

    [Fact]
    public void NonEmptyFilter_NumberValue_Matches()
    {
        var filter = new NonEmptyFilter();
        Assert.True(filter.Matches(CellValue.FromNumber(0)));
    }

    [Fact]
    public void NonEmptyFilter_TextValue_Matches()
    {
        var filter = new NonEmptyFilter();
        Assert.True(filter.Matches(CellValue.FromText("")));
    }

    [Fact]
    public void NonEmptyFilter_EmptyValue_DoesNotMatch()
    {
        var filter = new NonEmptyFilter();
        Assert.False(filter.Matches(CellValue.Empty));
    }
}