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
    /// <summary>Works out what order a set of rows should be put into.</summary>
    /// <param name="dataRows">
    /// The row numbers to arrange. Any header row must already have been left
    /// out.
    /// </param>
    /// <param name="keys">
    /// The columns to sort on, most important first. Rows that tie on the
    /// first key are settled by the second, and so on.
    /// </param>
    /// <param name="valueAt">
    /// Supplies the value at a given row and column, so that the ordering can
    /// be worked out without reaching into a workbook.
    /// </param>
    /// <returns>
    /// The same row numbers rearranged: the row named at each position is the
    /// one whose contents belong there. Rows that tie on every key keep the
    /// order they were given in.
    /// </returns>
    /// <exception cref="ArgumentNullException">Any argument is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="keys"/> is empty.</exception>
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
