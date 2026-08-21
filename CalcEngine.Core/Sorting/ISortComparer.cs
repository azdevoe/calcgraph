using CalcEngine.Core.Model;

namespace CalcEngine.Core.Sorting;

/// <summary>
/// Orders two cell values for SortRangeCommand (Group C feature:
/// Sorting and Filtering). Strategy interface — AscendingComparer and
/// DescendingComparer are the concrete strategies, the same shape as
/// IValidationRule's RangeRule/ListRule/TypeRule/CustomFormulaRule.
/// </summary>
public interface ISortComparer
{
    /// <summary>Decides which of two values comes first.</summary>
    /// <param name="a">The first value to compare.</param>
    /// <param name="b">The second value to compare.</param>
    /// <returns>
    /// A negative number if <paramref name="a"/> comes first, a positive
    /// number if <paramref name="b"/> does, and zero if neither comes before
    /// the other. This is the same arrangement the standard comparer
    /// interfaces use.
    /// </returns>
    int Compare(CellValue a, CellValue b);
}