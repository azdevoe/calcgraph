using CalcEngine.Core.Model;

namespace CalcEngine.Core.Validation;

/// <summary>
/// Requires a numeric value within [Min, Max], inclusive. Anything that
/// isn't ValueKind.Number fails — no coercion, same philosophy as
/// CellValue.AsNumber(): the caller checks Kind before treating a value
/// as a number.
/// </summary>
public sealed class RangeRule : IValidationRule
{
    public double Min { get; }
    public double Max { get; }

    public RangeRule(double min, double max)
    {
        Min = min;
        Max = max;
    }

    public ValidationResult Validate(CellValue value, IEvalContext context)
    {
        if (value.Kind != ValueKind.Number)
            return ValidationResult.Fail("value must be a number");

        if (value.Number < Min || value.Number > Max)
            return ValidationResult.Fail($"value must be between {Min} and {Max}");

        return ValidationResult.Ok();
    }
}