namespace CalcEngine.Core.Model;

/// <summary>
/// An immutable rectangular range of cells, e.g. B2:B45.
/// Value type for zero-allocation dictionary keys and struct fields
/// inside expression trees. Record for value equality.
///
/// RI: TopLeft.Row is less than or equal to BottomRight.Row AND TopLeft.Column is less than or equal to BottomRight.Column
/// </summary>
public readonly record struct CellRange
{
    /// <summary>Gets the address of the range's top-left corner.</summary>
    public CellRef TopLeft { get; }

    /// <summary>Gets the address of the range's bottom-right corner.</summary>
    public CellRef BottomRight { get; }

    /// <summary>Initializes a new range spanning two opposite corners.</summary>
    /// <param name="topLeft">The top-left corner of the range.</param>
    /// <param name="bottomRight">
    /// The bottom-right corner. Its row and column must both be greater than
    /// or equal to those of <paramref name="topLeft"/>.
    /// </param>
    /// <exception cref="ArgumentException">
    /// <paramref name="topLeft"/> lies below or to the right of
    /// <paramref name="bottomRight"/>.
    /// </exception>
    public CellRange(CellRef topLeft, CellRef bottomRight)
    {
        if (topLeft.Row > bottomRight.Row || topLeft.Column > bottomRight.Column)
            throw new ArgumentException(
                $"TopLeft ({topLeft}) must not exceed BottomRight ({bottomRight}).");
        TopLeft = topLeft;
        BottomRight = bottomRight;
    }

    /// <summary>Converts range text such as "B2:B45" into a CellRange.</summary>
    /// <param name="text">
    /// Two A1-style addresses separated by a colon.
    /// </param>
    /// <returns>The range that <paramref name="text"/> represents.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> is null.</exception>
    /// <exception cref="FormatException">
    /// <paramref name="text"/> contains no colon, or either side of the colon
    /// is not a valid cell address.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// The two addresses are the wrong way round.
    /// </exception>
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

    /// <summary>Returns every cell address in the range, row by row.</summary>
    /// <returns>
    /// The addresses in row-major order. For B2:C3 that is B2, C2, B3, C3.
    /// </returns>
    public IEnumerable<CellRef> GetCells()
    {
        for (int r = TopLeft.Row; r <= BottomRight.Row; r++)
            for (int c = TopLeft.Column; c <= BottomRight.Column; c++)
                yield return new CellRef(r, c);
    }

    /// <summary>Gets the number of cells the range covers.</summary>
    /// <value>Width multiplied by height, so B2:C3 gives 4.</value>
    public int CellCount =>
        (BottomRight.Row - TopLeft.Row + 1) * (BottomRight.Column - TopLeft.Column + 1);

    /// <summary>Returns the range written in A1 notation.</summary>
    /// <returns>The two corner addresses separated by a colon, such as "B2:B45".</returns>
    public override string ToString() => $"{TopLeft}:{BottomRight}";
}