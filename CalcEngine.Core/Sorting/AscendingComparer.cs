using CalcEngine.Core.Model;

namespace CalcEngine.Core.Sorting;

/// <summary>
/// Sorts values low to high using CellValueOrdering's rule directly.
/// </summary>
public sealed class AscendingComparer : ISortComparer
{
    /// <summary>Decides which of two values comes first, lowest first.</summary>
    /// <param name="a">The first value to compare.</param>
    /// <param name="b">The second value to compare.</param>
    /// <returns>
    /// A negative number if <paramref name="a"/> comes first, a positive
    /// number if <paramref name="b"/> does, and zero if neither comes before
    /// the other. Values of different kinds are grouped in the order numbers,
    /// text, TRUE and FALSE, empty cells, then errors. Numbers compare by
    /// size, text ignoring case, and FALSE before TRUE.
    /// </returns>
    public int Compare(CellValue a, CellValue b) => CellValueOrdering.CompareAscending(a, b);
}