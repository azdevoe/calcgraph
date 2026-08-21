using CalcEngine.Core.Model;

namespace CalcEngine.Core.Filtering;

/// <summary>
/// Matches a numeric value within [Min, Max], inclusive. Anything that
/// isn't ValueKind.Number does not match — same no-coercion philosophy
/// as RangeRule.
/// </summary>
public sealed class NumberRangeFilter : IRowFilter
{
    public double Min { get; }
    public double Max { get; }

    public NumberRangeFilter(double min, double max)
    {
        Min = min;
        Max = max;
    }

    public bool Matches(CellValue value) =>
        value.Kind == ValueKind.Number && value.Number >= Min && value.Number <= Max;
}