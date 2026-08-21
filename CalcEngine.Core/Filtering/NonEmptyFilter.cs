using CalcEngine.Core.Model;

namespace CalcEngine.Core.Filtering;

/// <summary>
/// Matches any value that isn't ValueKind.Empty — a quick "show me
/// rows that have anything in this column" filter. Note that
/// CellValue.FromText("") still matches: it is a Text cell whose
/// content happens to be an empty string, which is a different thing
/// from a cell that was never set (ValueKind.Empty).
/// </summary>
public sealed class NonEmptyFilter : IRowFilter
{
    /// <summary>Decides whether a cell has anything in it.</summary>
    /// <param name="value">The value to test.</param>
    /// <returns>
    /// true for anything other than a cell that was never filled in, errors
    /// included. A cell holding empty text passes, since something was
    /// entered there.
    /// </returns>
    public bool Matches(CellValue value) => value.Kind != ValueKind.Empty;
}