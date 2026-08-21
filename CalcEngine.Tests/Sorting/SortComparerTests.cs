using CalcEngine.Core.Model;
using CalcEngine.Core.Sorting;
using Xunit;

namespace CalcEngine.Tests.Sorting;

/// <summary>
/// Tests for the sort-comparer family — ISortComparer and its two
/// implementations (Group C feature: Sorting & Filtering). Both
/// strategies share one ordering rule: Number &lt; Text &lt; Boolean &lt;
/// Empty &lt; Error, with a natural comparison within each kind
/// (numeric, ordinal case-insensitive text, false-before-true).
/// DescendingComparer is exactly the inverse of AscendingComparer,
/// not an independently defined order.
/// </summary>
public class SortComparerTests
{
    // ── AscendingComparer: within a kind ────────────────────────

    [Fact]
    public void Ascending_NumbersInNaturalOrder()
    {
        var cmp = new AscendingComparer();
        Assert.True(cmp.Compare(CellValue.FromNumber(1), CellValue.FromNumber(2)) < 0);
        Assert.True(cmp.Compare(CellValue.FromNumber(2), CellValue.FromNumber(1)) > 0);
        Assert.Equal(0, cmp.Compare(CellValue.FromNumber(5), CellValue.FromNumber(5)));
    }

    [Fact]
    public void Ascending_TextOrdinalCaseInsensitive()
    {
        var cmp = new AscendingComparer();
        Assert.True(cmp.Compare(CellValue.FromText("apple"), CellValue.FromText("banana")) < 0);
        Assert.Equal(0, cmp.Compare(CellValue.FromText("Apple"), CellValue.FromText("apple")));
    }

    [Fact]
    public void Ascending_BooleanFalseBeforeTrue()
    {
        var cmp = new AscendingComparer();
        Assert.True(cmp.Compare(CellValue.FromBoolean(false), CellValue.FromBoolean(true)) < 0);
    }

    // ── AscendingComparer: across kinds ─────────────────────────

    [Fact]
    public void Ascending_NumberBeforeText()
    {
        var cmp = new AscendingComparer();
        Assert.True(cmp.Compare(CellValue.FromNumber(1), CellValue.FromText("a")) < 0);
    }

    [Fact]
    public void Ascending_TextBeforeBoolean()
    {
        var cmp = new AscendingComparer();
        Assert.True(cmp.Compare(CellValue.FromText("a"), CellValue.FromBoolean(true)) < 0);
    }

    [Fact]
    public void Ascending_BooleanBeforeEmpty()
    {
        var cmp = new AscendingComparer();
        Assert.True(cmp.Compare(CellValue.FromBoolean(false), CellValue.Empty) < 0);
    }

    [Fact]
    public void Ascending_EmptyBeforeError()
    {
        var cmp = new AscendingComparer();
        Assert.True(cmp.Compare(CellValue.Empty, CellValue.FromError(ErrorKind.Value)) < 0);
    }

    // ── DescendingComparer: exact inverse ───────────────────────

    [Fact]
    public void Descending_IsInverseOfAscending_SameKind()
    {
        var asc = new AscendingComparer();
        var desc = new DescendingComparer();
        var a = CellValue.FromNumber(1);
        var b = CellValue.FromNumber(2);

        Assert.Equal(-Math.Sign(asc.Compare(a, b)), Math.Sign(desc.Compare(a, b)));
    }

    [Fact]
    public void Descending_NumbersHighestFirst()
    {
        var cmp = new DescendingComparer();
        Assert.True(cmp.Compare(CellValue.FromNumber(2), CellValue.FromNumber(1)) < 0);
    }

    [Fact]
    public void Descending_AcrossKinds_IsInverted()
    {
        var cmp = new DescendingComparer();
        // Ascending puts Number before Text; Descending must reverse that.
        Assert.True(cmp.Compare(CellValue.FromText("a"), CellValue.FromNumber(1)) < 0);
    }
}