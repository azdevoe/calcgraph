using CalcEngine.Core.Model;

namespace CalcEngine.Core.Sorting;

/// <summary>
/// The one ordering rule both sort-comparer strategies are built from.
/// AscendingComparer returns this directly; DescendingComparer negates
/// it — Descending is not an independently defined order, it is
/// exactly the inverse of Ascending, which is what lets a test assert
/// that relationship instead of re-deriving the rule twice.
///
/// Kind rank, lowest first: Number, Text, Boolean, Empty, Error.
/// Within a kind: numeric comparison for Number; ordinal
/// case-insensitive comparison for Text; false before true for
/// Boolean; Empty and Error each compare equal to themselves (Error
/// further orders by ErrorKind, purely so the sort is stable/total —
/// which #DIV/0! sorts before which #VALUE! has no meaning to a user
/// and isn't asserted on).
/// </summary>
internal static class CellValueOrdering
{
    /// <summary>Decides which of two values comes first, lowest first.</summary>
    /// <param name="a">The first value to compare.</param>
    /// <param name="b">The second value to compare.</param>
    /// <returns>
    /// A negative number if <paramref name="a"/> comes first, a positive
    /// number if <paramref name="b"/> does, and zero if neither comes before
    /// the other. Every pair of values can be ordered, so a column holding a
    /// mixture of kinds still sorts.
    /// </returns>
    public static int CompareAscending(CellValue a, CellValue b)
    {
        int rankA = KindRank(a.Kind);
        int rankB = KindRank(b.Kind);
        if (rankA != rankB) return rankA.CompareTo(rankB);

        return a.Kind switch
        {
            ValueKind.Number => a.Number.CompareTo(b.Number),
            ValueKind.Text => string.Compare(a.Text, b.Text, StringComparison.OrdinalIgnoreCase),
            ValueKind.Boolean => a.Boolean.CompareTo(b.Boolean),
            ValueKind.Error => a.Error.CompareTo(b.Error),
            _ => 0 // Empty vs Empty
        };
    }

    private static int KindRank(ValueKind kind) => kind switch
    {
        ValueKind.Number => 0,
        ValueKind.Text => 1,
        ValueKind.Boolean => 2,
        ValueKind.Empty => 3,
        ValueKind.Error => 4,
        _ => 5
    };
}