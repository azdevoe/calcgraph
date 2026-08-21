using CalcEngine.Core.Model;

namespace CalcEngine.Core.Sorting;

/// <summary>
/// Sorts values high to low — the exact inverse of AscendingComparer,
/// not a separately defined order.
/// </summary>
public sealed class DescendingComparer : ISortComparer
{
    /// <summary>Decides which of two values comes first, highest first.</summary>
    /// <param name="a">The first value to compare.</param>
    /// <param name="b">The second value to compare.</param>
    /// <returns>
    /// A negative number if <paramref name="a"/> comes first, a positive
    /// number if <paramref name="b"/> does, and zero if neither comes before
    /// the other. The result is always the exact opposite of what
    /// AscendingComparer gives for the same two values.
    /// </returns>
    public int Compare(CellValue a, CellValue b) => -CellValueOrdering.CompareAscending(a, b);
}