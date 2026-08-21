using CalcEngine.Core.Model;

namespace CalcEngine.Core.Sorting;

/// <summary>
/// Orders two cell values for SortRangeCommand (Group C feature:
/// Sorting & Filtering). Strategy interface — AscendingComparer and
/// DescendingComparer are the concrete strategies, the same shape as
/// IValidationRule's RangeRule/ListRule/TypeRule/CustomFormulaRule.
/// </summary>
public interface ISortComparer
{
    /// <summary>
    /// Negative if a sorts before b, positive if after, zero if equal
    /// for sorting purposes. Same contract as IComparer&lt;T&gt;.Compare.
    /// </summary>
    int Compare(CellValue a, CellValue b);
}