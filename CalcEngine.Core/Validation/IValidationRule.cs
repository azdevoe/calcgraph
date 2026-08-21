using CalcEngine.Core.Model;

namespace CalcEngine.Core.Validation;

/// <summary>
/// A single data-validation check for a cell's value (Group C feature).
/// Concrete rules — RangeRule, ListRule, TypeRule, CustomFormulaRule —
/// each encode one kind of check. context is available for rules that
/// need to look elsewhere in the workbook (CustomFormulaRule); the
/// others ignore it.
/// </summary>
public interface IValidationRule
{
    /// <summary>Checks a value against this rule.</summary>
    /// <param name="value">The value a cell is about to be given.</param>
    /// <param name="context">
    /// Supplies the rest of the workbook, for rules that need to look at other
    /// cells. Rules that judge the value on its own ignore it.
    /// </param>
    /// <returns>
    /// A result saying whether the value is allowed, and if not, a message
    /// explaining why that is fit to show a user. A value that breaks the rule
    /// is reported this way rather than by throwing.
    /// </returns>
    ValidationResult Validate(CellValue value, IEvalContext context);
}