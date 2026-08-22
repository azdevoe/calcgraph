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
    /// <summary>Gets the lowest value the cell may hold.</summary>
    public double Min { get; }

    /// <summary>Gets the highest value the cell may hold.</summary>
    public double Max { get; }

    /// <summary>Initializes a rule that keeps a cell's value between two bounds.</summary>
    /// <param name="min">The lowest value to allow.</param>
    /// <param name="max">
    /// The highest value to allow. A maximum below the minimum allows nothing.
    /// </param>
    public RangeRule(double min, double max)
    {
        Min = min;
        Max = max;
    }

    /// <summary>Checks that a value is a number between the two bounds.</summary>
    /// <param name="value">The value a cell is about to be given.</param>
    /// <param name="context">Not used. The value is judged on its own.</param>
    /// <returns>
    /// A passing result if the value is a number that is at least Min and at
    /// most Max. Anything that is not a number is refused, TRUE and FALSE
    /// included, rather than being read as one.
    /// </returns>
    public ValidationResult Validate(CellValue value, IEvalContext context)
    {
        if (value.Kind != ValueKind.Number)
            return ValidationResult.Fail("value must be a number");

        if (value.Number < Min || value.Number > Max)
            return ValidationResult.Fail($"value must be between {Min} and {Max}");

        return ValidationResult.Ok();
    }
}