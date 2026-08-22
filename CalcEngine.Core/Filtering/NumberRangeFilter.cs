using CalcEngine.Core.Model;

namespace CalcEngine.Core.Filtering;

/// <summary>
/// Matches a numeric value within [Min, Max], inclusive. Anything that
/// isn't ValueKind.Number does not match — same no-coercion philosophy
/// as RangeRule.
/// </summary>
public sealed class NumberRangeFilter : IRowFilter
{
    /// <summary>Gets the lowest value that passes the filter.</summary>
    public double Min { get; }

    /// <summary>Gets the highest value that passes the filter.</summary>
    public double Max { get; }

    /// <summary>Initializes a filter that keeps numbers between two bounds.</summary>
    /// <param name="min">The lowest value to keep.</param>
    /// <param name="max">
    /// The highest value to keep. A maximum below the minimum keeps nothing.
    /// </param>
    public NumberRangeFilter(double min, double max)
    {
        Min = min;
        Max = max;
    }

    /// <summary>Decides whether a value falls between the two bounds.</summary>
    /// <param name="value">The value to test.</param>
    /// <returns>
    /// true if the value is a number that is at least Min and at most Max;
    /// otherwise, false. Text, TRUE and FALSE, empty cells and errors never
    /// pass, even when they could be read as a number.
    /// </returns>
    public bool Matches(CellValue value) =>
        value.Kind == ValueKind.Number && value.Number >= Min && value.Number <= Max;
}