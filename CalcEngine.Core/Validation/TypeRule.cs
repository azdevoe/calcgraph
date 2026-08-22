using CalcEngine.Core.Model;

namespace CalcEngine.Core.Validation;

/// <summary>
/// Requires the value's Kind to match exactly one expected ValueKind
/// (e.g. reject text in a column that must hold numbers).
/// </summary>
public sealed class TypeRule : IValidationRule
{
    /// <summary>Gets the kind of value the cell must hold.</summary>
    public ValueKind ExpectedKind { get; }

    /// <summary>Initializes a rule that limits a cell to one kind of value.</summary>
    /// <param name="expectedKind">The only kind of value to allow.</param>
    public TypeRule(ValueKind expectedKind)
    {
        ExpectedKind = expectedKind;
    }

    /// <summary>Checks that a value is of the expected kind.</summary>
    /// <param name="value">The value a cell is about to be given.</param>
    /// <param name="context">Not used. The value is judged on its own.</param>
    /// <returns>
    /// A passing result if the value is of the expected kind, and a failing
    /// one otherwise. Nothing is converted, so a number is not accepted in
    /// place of text.
    /// </returns>
    public ValidationResult Validate(CellValue value, IEvalContext context)
    {
        if (value.Kind != ExpectedKind)
            return ValidationResult.Fail($"value must be a {ExpectedKind}");

        return ValidationResult.Ok();
    }
}