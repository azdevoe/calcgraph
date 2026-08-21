namespace CalcEngine.Core.Model;

/// <summary>
/// An immutable rectangular range of cells, e.g. B2:B45.
/// Value type for zero-allocation dictionary keys and struct fields
/// inside expression trees. Record for value equality.
///
/// RI: TopLeft.Row &lt;= BottomRight.Row AND TopLeft.Column &lt;= BottomRight.Column
/// </summary>
public readonly record struct CellRange
{
    public CellRef TopLeft { get; }
    public CellRef BottomRight { get; }

    public CellRange(CellRef topLeft, CellRef bottomRight)
    {
        if (topLeft.Row > bottomRight.Row || topLeft.Column > bottomRight.Column)
            throw new ArgumentException(
                $"TopLeft ({topLeft}) must not exceed BottomRight ({bottomRight}).");
        TopLeft = topLeft;
        BottomRight = bottomRight;
    }

    /// <summary>Parses "B2:B45" into a CellRange.</summary>
    public static CellRange Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        int colon = text.IndexOf(':');
        if (colon < 0)
            throw new FormatException($"Invalid range format (no ':'): '{text}'");
        return new CellRange(
            CellRef.Parse(text[..colon]),
            CellRef.Parse(text[(colon + 1)..]));
    }

    /// <summary>
    /// Every CellRef in the range, row-major order.
    /// For B2:C3 this yields B2, C2, B3, C3.
    /// </summary>
    public IEnumerable<CellRef> GetCells()
    {
        for (int r = TopLeft.Row; r <= BottomRight.Row; r++)
            for (int c = TopLeft.Column; c <= BottomRight.Column; c++)
                yield return new CellRef(r, c);
    }

    /// <summary>Total number of cells in the range.</summary>
    public int CellCount =>
        (BottomRight.Row - TopLeft.Row + 1) * (BottomRight.Column - TopLeft.Column + 1);

    public override string ToString() => $"{TopLeft}:{BottomRight}";
}