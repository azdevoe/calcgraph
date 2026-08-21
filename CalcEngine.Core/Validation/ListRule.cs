using CalcEngine.Core.Model;

namespace CalcEngine.Core.Validation;

/// <summary>
/// Requires the value's text representation to match one of a fixed set
/// of allowed strings, case-insensitively (the same forgiving comparison
/// a spreadsheet's dropdown-list validation uses).
/// </summary>
public sealed class ListRule : IValidationRule
{
    /// <summary>Gets the values the cell is allowed to hold.</summary>
    public IReadOnlyList<string> AllowedValues { get; }

    /// <summary>Initializes a rule that limits a cell to a fixed set of entries.</summary>
    /// <param name="allowedValues">
    /// The entries to allow. An empty set allows nothing.
    /// </param>
    public ListRule(IReadOnlyList<string> allowedValues)
    {
        AllowedValues = allowedValues;
    }

    /// <summary>Checks that a value is one of the allowed entries.</summary>
    /// <param name="value">The value a cell is about to be given.</param>
    /// <param name="context">Not used. The value is judged on its own.</param>
    /// <returns>
    /// A passing result if the value, written out as it would appear in the
    /// grid, matches one of the allowed entries ignoring case. Otherwise a
    /// failing result listing what is allowed.
    /// </returns>
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