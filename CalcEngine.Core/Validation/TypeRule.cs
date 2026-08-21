using CalcEngine.Core.Model;

namespace CalcEngine.Core.Validation;

/// <summary>
/// Requires the value's Kind to match exactly one expected ValueKind
/// (e.g. reject text in a column that must hold numbers).
/// </summary>
public sealed class TypeRule : IValidationRule
{
    public ValueKind ExpectedKind { get; }

    public TypeRule(ValueKind expectedKind)
    {
        ExpectedKind = expectedKind;
    }

    public ValidationResult Validate(CellValue value, IEvalContext context)
    {
        if (value.Kind != ExpectedKind)
            return ValidationResult.Fail($"value must be a {ExpectedKind}");

        return ValidationResult.Ok();
    }
}