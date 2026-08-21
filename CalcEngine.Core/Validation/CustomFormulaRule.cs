using CalcEngine.Core.Expressions;
using CalcEngine.Core.Model;

namespace CalcEngine.Core.Validation;

/// <summary>
/// Requires a pre-parsed formula (an IExpression, already built by
/// FormulaInputParser from something like "=A1>0") to evaluate to the
/// boolean TRUE under the given context. Anything else — FALSE, a
/// non-boolean result, or an error value — fails.
/// </summary>
public sealed class CustomFormulaRule : IValidationRule
{
    /// <summary>Gets the condition the cell must satisfy.</summary>
    public IExpression Formula { get; }

    /// <summary>Initializes a rule that tests a cell against a condition.</summary>
    /// <param name="formula">
    /// The condition, already read in from text such as "=A1&gt;0".
    /// </param>
    public CustomFormulaRule(IExpression formula)
    {
        Formula = formula;
    }

    /// <summary>Checks that the condition holds.</summary>
    /// <param name="value">
    /// The value a cell is about to be given. The condition is free to ignore
    /// it and look at other cells instead.
    /// </param>
    /// <param name="context">Supplies the cell values the condition reads.</param>
    /// <returns>
    /// A passing result only if the condition works out to TRUE. FALSE, a
    /// result that is not TRUE or FALSE at all, and an error all fail.
    /// </returns>
    public ValidationResult Validate(CellValue value, IEvalContext context)
    {
        var result = Formula.Evaluate(context);

        if (result.Kind == ValueKind.Boolean && result.Boolean)
            return ValidationResult.Ok();

        return ValidationResult.Fail("value does not satisfy custom formula");
    }
}