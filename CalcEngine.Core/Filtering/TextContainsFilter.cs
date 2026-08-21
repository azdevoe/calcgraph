using CalcEngine.Core.Model;

namespace CalcEngine.Core.Filtering;

/// <summary>
/// Matches a text value containing Substring. Case-insensitive by
/// default, since that is the more useful default for a spreadsheet
/// filter (matching ListRule's case-insensitive comparison elsewhere
/// in this codebase); pass ignoreCase: false for an exact-case match.
/// Anything that isn't ValueKind.Text does not match.
/// </summary>
public sealed class TextContainsFilter : IRowFilter
{
    /// <summary>Gets the text being searched for.</summary>
    public string Substring { get; }

    /// <summary>Gets a value indicating whether the search ignores case.</summary>
    public bool IgnoreCase { get; }

    /// <summary>Initializes a filter that keeps text containing a given fragment.</summary>
    /// <param name="substring">
    /// The fragment to look for. An empty string matches any text.
    /// </param>
    /// <param name="ignoreCase">
    /// true to ignore case, which is the usual choice for a spreadsheet
    /// filter; false to match case exactly.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="substring"/> is null.</exception>
    public TextContainsFilter(string substring, bool ignoreCase = true)
    {
        ArgumentNullException.ThrowIfNull(substring);
        Substring = substring;
        IgnoreCase = ignoreCase;
    }

    /// <summary>Decides whether a value contains the text being searched for.</summary>
    /// <param name="value">The value to test.</param>
    /// <returns>
    /// true if the value is text containing the fragment; otherwise, false.
    /// Numbers, TRUE and FALSE, empty cells and errors never pass, even when
    /// the text they are shown as would contain it.
    /// </returns>
    public bool Matches(CellValue value)
    {
        if (value.Kind != ValueKind.Text || value.Text is null) return false;

        var comparison = IgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        return value.Text.Contains(Substring, comparison);
    }
}