namespace CalcEngine.Core;

/// <summary>
/// One column to sort by, and the direction to sort it in (Group C
/// feature: Sorting &amp; Filtering). SortRange takes an ordered list of
/// these for multi-key sorting: rows are ordered by the first key,
/// ties broken by the second, and so on.
/// </summary>
public sealed class SortKey
{
    /// <summary>Absolute column number (CellRef.Column) to read the sort value from.</summary>
    public int Column { get; }

    /// <summary>Ascending or descending, or any other ordering strategy.</summary>
    public ISortComparer Comparer { get; }

    public SortKey(int column, ISortComparer comparer)
    {
        if (column < 1)
            throw new ArgumentException($"Column must be >= 1, got {column}.", nameof(column));
        Column = column;
        Comparer = comparer ?? throw new ArgumentNullException(nameof(comparer));
    }
}
