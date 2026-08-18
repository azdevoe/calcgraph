namespace CalcEngine.Core;

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