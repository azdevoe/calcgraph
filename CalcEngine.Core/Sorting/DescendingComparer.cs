using CalcEngine.Core.Model;

namespace CalcEngine.Core.Sorting;

/// <summary>
/// Sorts values high to low — the exact inverse of AscendingComparer,
/// not a separately defined order.
/// </summary>
public sealed class DescendingComparer : ISortComparer
{
    public int Compare(CellValue a, CellValue b) => -CellValueOrdering.CompareAscending(a, b);
}