using CalcEngine.Core.Model;

namespace CalcEngine.Core.Filtering;

/// <summary>
/// A single filter predicate for CalculationEngine.FilterRange (Group C
/// feature: Sorting and Filtering). Strategy interface — NumberRangeFilter,
/// TextContainsFilter, and NonEmptyFilter are the concrete strategies,
/// the same shape as IValidationRule's rule family. Unlike
/// IValidationRule, filtering is read-only: FilterRange only asks
/// "does this row's value match?" and never writes to the workbook,
/// so no IEvalContext is needed here.
/// </summary>
public interface IRowFilter
{
    /// <summary>Decides whether a value passes this filter.</summary>
    /// <param name="value">The value from the column being filtered on.</param>
    /// <returns>
    /// true if the row holding this value should stay visible; otherwise,
    /// false.
    /// </returns>
    bool Matches(CellValue value);
}