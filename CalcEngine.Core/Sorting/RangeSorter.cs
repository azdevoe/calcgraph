using CalcEngine.Core.Model;
namespace CalcEngine.Core.Sorting;

/// <summary>
/// Computes the new row order for a range sort (Group C feature:
/// Sorting &amp; Filtering). Pure function of the current sort-key values
/// — it knows nothing about raw formula text, the workbook, or the
/// dependency graph; SortRangeCommand is what turns an order into
/// writes. Kept separate so the ordering rule (multi-key, stable,
/// cross-type via ISortComparer) is unit-testable without a workbook.
///
/// Stable: built on LINQ OrderBy/ThenBy, which are documented as
/// stable sorts, so rows with equal keys keep their original relative
/// order — the only sane default when ties are common (e.g. sorting a
/// column of booleans or a mostly-empty column).
/// </summary>
public static class RangeSorter
{
    /// <summary>
    /// Reorders dataRows (row numbers, header already excluded by the
    /// caller) by keys, evaluated via valueAt(row, column). Returns the
    /// row numbers in their new order — dataRows[i] should end up
    /// holding whatever is currently at ComputeOrder(...)[i].
    /// </summary>
    public static IReadOnlyList<int> ComputeOrder(
        IReadOnlyList<int> dataRows,
        IReadOnlyList<SortKey> keys,
        Func<int, int, CellValue> valueAt)
    {
        ArgumentNullException.ThrowIfNull(dataRows);
        ArgumentNullException.ThrowIfNull(keys);
        ArgumentNullException.ThrowIfNull(valueAt);
        if (keys.Count == 0)
            throw new ArgumentException("At least one sort key is required.", nameof(keys));

        if (dataRows.Count <= 1)
            return dataRows.ToList();

        IOrderedEnumerable<int>? ordered = null;
        foreach (var key in keys)
        {
            var comparer = Comparer<int>.Create(
                (rowA, rowB) => key.Comparer.Compare(valueAt(rowA, key.Column), valueAt(rowB, key.Column)));

            ordered = ordered is null
                ? dataRows.OrderBy(row => row, comparer)
                : ordered.ThenBy(row => row, comparer);
        }

        return ordered!.ToList();
    }
}
