using CalcEngine.Core.Model;

namespace CalcEngine.Core.Filtering;

/// <summary>
/// A single filter predicate for CalculationEngine.FilterRange (Group C
/// feature: Sorting & Filtering). Strategy interface — NumberRangeFilter,
/// TextContainsFilter and NonEmptyFilter are the concrete strategies,
/// the same shape as IValidationRule's rule family. Unlike
/// IValidationRule, filtering is read-only: FilterRange only asks
/// "does this row's value match?" and never writes to the workbook,
/// so no IEvalContext is needed here.
/// </summary>
public interface IRowFilter
{
    /// <summary>True if value should be included in the filtered result.</summary>
    bool Matches(CellValue value);
}