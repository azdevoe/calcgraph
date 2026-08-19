namespace CalcEngine.Core;

/// <summary>
/// Matches a text value containing Substring. Case-insensitive by
/// default, since that is the more useful default for a spreadsheet
/// filter (matching ListRule's case-insensitive comparison elsewhere
/// in this codebase); pass ignoreCase: false for an exact-case match.
/// Anything that isn't ValueKind.Text does not match.
/// </summary>
public sealed class TextContainsFilter : IRowFilter
{
    public string Substring { get; }
    public bool IgnoreCase { get; }

    public TextContainsFilter(string substring, bool ignoreCase = true)
    {
        ArgumentNullException.ThrowIfNull(substring);
        Substring = substring;
        IgnoreCase = ignoreCase;
    }

    public bool Matches(CellValue value)
    {
        if (value.Kind != ValueKind.Text || value.Text is null) return false;

        var comparison = IgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        return value.Text.Contains(Substring, comparison);
    }
}