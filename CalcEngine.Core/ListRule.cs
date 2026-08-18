namespace CalcEngine.Core;

/// <summary>
/// Requires the value's text representation to match one of a fixed set
/// of allowed strings, case-insensitively (the same forgiving comparison
/// a spreadsheet's dropdown-list validation uses).
/// </summary>
public sealed class ListRule : IValidationRule
{
    public IReadOnlyList<string> AllowedValues { get; }

    public ListRule(IReadOnlyList<string> allowedValues)
    {
        AllowedValues = allowedValues;
    }

    public ValidationResult Validate(CellValue value, IEvalContext context)
    {
        var text = value.ToString();

        foreach (var allowed in AllowedValues)
        {
            if (string.Equals(allowed, text, StringComparison.OrdinalIgnoreCase))
                return ValidationResult.Ok();
        }

        return ValidationResult.Fail($"value must be one of: {string.Join(", ", AllowedValues)}");
    }
}