using CalcEngine.Core.Model;

namespace CalcEngine.Core.Sorting;

/// <summary>
/// Sorts values low to high using CellValueOrdering's rule directly.
/// </summary>
public sealed class AscendingComparer : ISortComparer
{
    public int Compare(CellValue a, CellValue b) => CellValueOrdering.CompareAscending(a, b);
}