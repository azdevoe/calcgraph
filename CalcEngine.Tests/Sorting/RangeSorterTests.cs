using CalcEngine.Core.Model;
using CalcEngine.Core.Sorting;
using Xunit;

namespace CalcEngine.Tests.Sorting;

/// <summary>
/// Tests for RangeSorter.ComputeOrder — the pure row-ordering rule
/// behind SortRange (Group C feature: Sorting &amp; Filtering). No
/// workbook involved: valueAt is a fake so these tests pin down the
/// ordering rule (multi-key, stability, cross-type) independently of
/// how the rows are actually moved.
/// </summary>
public class RangeSorterTests
{
    private static Func<int, int, CellValue> Column(Dictionary<int, CellValue> values, int column) =>
        (row, col) => col == column ? values[row] : CellValue.Empty;

    [Fact]
    public void AlreadySorted_StaysInPlace()
    {
        var values = new Dictionary<int, CellValue>
        {
            [1] = CellValue.FromNumber(1),
            [2] = CellValue.FromNumber(2),
            [3] = CellValue.FromNumber(3),
        };

        var order = RangeSorter.ComputeOrder(
            new[] { 1, 2, 3 },
            new[] { new SortKey(1, new AscendingComparer()) },
            Column(values, 1));

        Assert.Equal(new[] { 1, 2, 3 }, order);
    }

    [Fact]
    public void ReverseSorted_IsFlipped()
    {
        var values = new Dictionary<int, CellValue>
        {
            [1] = CellValue.FromNumber(3),
            [2] = CellValue.FromNumber(2),
            [3] = CellValue.FromNumber(1),
        };

        var order = RangeSorter.ComputeOrder(
            new[] { 1, 2, 3 },
            new[] { new SortKey(1, new AscendingComparer()) },
            Column(values, 1));

        Assert.Equal(new[] { 3, 2, 1 }, order);
    }

    [Fact]
    public void EqualKeys_AreStable_OriginalOrderPreserved()
    {
        // All three rows tie on the sort key; row 1, 2, 3 must come
        // back in that exact order, not shuffled.
        var values = new Dictionary<int, CellValue>
        {
            [1] = CellValue.FromNumber(5),
            [2] = CellValue.FromNumber(5),
            [3] = CellValue.FromNumber(5),
        };

        var order = RangeSorter.ComputeOrder(
            new[] { 1, 2, 3 },
            new[] { new SortKey(1, new AscendingComparer()) },
            Column(values, 1));

        Assert.Equal(new[] { 1, 2, 3 }, order);
    }

    [Fact]
    public void MixedTypes_FollowCrossTypeOrder_NumberBeforeTextBeforeBoolean()
    {
        var values = new Dictionary<int, CellValue>
        {
            [1] = CellValue.FromBoolean(true),
            [2] = CellValue.FromText("a"),
            [3] = CellValue.FromNumber(1),
        };

        var order = RangeSorter.ComputeOrder(
            new[] { 1, 2, 3 },
            new[] { new SortKey(1, new AscendingComparer()) },
            Column(values, 1));

        Assert.Equal(new[] { 3, 2, 1 }, order);
    }

    [Fact]
    public void MultiKey_SecondKeyBreaksTiesOnFirst()
    {
        // Column 1 (primary): A, A, B — column 2 (tiebreak): 2, 1, 0.
        var col1 = new Dictionary<int, CellValue>
        {
            [1] = CellValue.FromText("A"),
            [2] = CellValue.FromText("A"),
            [3] = CellValue.FromText("B"),
        };
        var col2 = new Dictionary<int, CellValue>
        {
            [1] = CellValue.FromNumber(2),
            [2] = CellValue.FromNumber(1),
            [3] = CellValue.FromNumber(0),
        };

        Func<int, int, CellValue> valueAt = (row, col) => col == 1 ? col1[row] : col2[row];

        var order = RangeSorter.ComputeOrder(
            new[] { 1, 2, 3 },
            new[] { new SortKey(1, new AscendingComparer()), new SortKey(2, new AscendingComparer()) },
            valueAt);

        // Rows 1 and 2 tie on column 1 ("A"); column 2 ascending puts
        // row 2 (1) before row 1 (2). Row 3 ("B") sorts last.
        Assert.Equal(new[] { 2, 1, 3 }, order);
    }

    [Fact]
    public void SingleRow_ReturnsUnchanged()
    {
        var order = RangeSorter.ComputeOrder(
            new[] { 1 },
            new[] { new SortKey(1, new AscendingComparer()) },
            (_, _) => CellValue.FromNumber(0));

        Assert.Equal(new[] { 1 }, order);
    }

    [Fact]
    public void NoKeys_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            RangeSorter.ComputeOrder(new[] { 1, 2 }, Array.Empty<SortKey>(), (_, _) => CellValue.Empty));
    }
}
