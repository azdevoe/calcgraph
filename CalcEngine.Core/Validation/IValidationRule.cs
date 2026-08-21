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
    /// <summary>
    /// Checks value against this rule. Never throws — a violation comes
    /// back as ValidationResult.Fail with a client-facing message, the
    /// same result-as-data shape as CellChangeSet and FormulaParseResult.
    /// </summary>
    ValidationResult Validate(CellValue value, IEvalContext context);
}