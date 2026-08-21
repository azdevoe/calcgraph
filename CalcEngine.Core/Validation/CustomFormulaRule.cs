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
    public IExpression Formula { get; }

    public CustomFormulaRule(IExpression formula)
    {
        Formula = formula;
    }

    public ValidationResult Validate(CellValue value, IEvalContext context)
    {
        var result = Formula.Evaluate(context);

        if (result.Kind == ValueKind.Boolean && result.Boolean)
            return ValidationResult.Ok();

        return ValidationResult.Fail("value does not satisfy custom formula");
    }
}