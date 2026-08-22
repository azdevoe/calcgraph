namespace CalcEngine.Core.Model;

/// <summary>
/// What a cell holds after evaluation. A tagged union over five states.
/// readonly struct for the same reasons as CellRef: no heap allocation in the
/// recalculation loop, and a snapshot handed to the GUI cannot mutate underneath it.
/// </summary>
public readonly record struct CellValue
{
    /// <summary>Gets which of the five states this value is in.</summary>
    public ValueKind Kind  { get; }

    /// <summary>
    /// Gets the numeric payload. Meaningful when Kind is Number, or when Kind
    /// is Boolean, where TRUE is stored as 1 and FALSE as 0. Otherwise 0.
    /// </summary>
    public double    Number { get; }

    /// <summary>
    /// Gets the text payload. Meaningful when Kind is Text, and null otherwise.
    /// </summary>
    public string?   Text  { get; }

    /// <summary>
    /// Gets which error this value carries. Meaningful when Kind is Error,
    /// and ErrorKind.None otherwise.
    /// </summary>
    public ErrorKind Error { get; }

    // Private — the only way in is through the factory methods below.
    // That guarantees the representation invariant: you cannot build a
    // CellValue whose Kind disagrees with its payload.
    private CellValue(ValueKind kind, double number, string? text, ErrorKind error)
    {
        Kind = kind; Number = number; Text = text; Error = error;
    }

    /// <summary>
    /// The value of a cell that has never been written to. Reading any cell
    /// the workbook does not hold returns this.
    /// </summary>
    public static readonly CellValue Empty =
        new(ValueKind.Empty, 0, null, ErrorKind.None);

    /// <summary>Creates a numeric value.</summary>
    /// <param name="d">The number to store.</param>
    /// <returns>A CellValue whose Kind is Number.</returns>
    public static CellValue FromNumber(double d)  => new(ValueKind.Number,  d, null, ErrorKind.None);

    /// <summary>Creates a text value.</summary>
    /// <param name="s">The text to store.</param>
    /// <returns>A CellValue whose Kind is Text.</returns>
    public static CellValue FromText(string s)    => new(ValueKind.Text,    0, s,    ErrorKind.None);

    /// <summary>Creates a TRUE or FALSE value.</summary>
    /// <param name="b">The boolean to store.</param>
    /// <returns>A CellValue whose Kind is Boolean.</returns>
    public static CellValue FromBoolean(bool b)   => new(ValueKind.Boolean, b ? 1 : 0, null, ErrorKind.None);

    /// <summary>
    /// Creates an error value. This is how the engine reports a problem such
    /// as division by zero, rather than by throwing.
    /// </summary>
    /// <param name="e">The error to report.</param>
    /// <returns>A CellValue whose Kind is Error.</returns>
    public static CellValue FromError(ErrorKind e)=> new(ValueKind.Error,   0, null, e);

    /// <summary>True for booleans. Stored as 1/0 in Number so no extra field is needed.</summary>
    public bool Boolean => Number != 0;

    /// <summary>The flag the evaluator branches on to propagate errors.</summary>
    public bool IsError => Kind == ValueKind.Error;

    /// <summary>
    /// Coercion point: what number arithmetic should use.
    /// Empty gives 0 — that is what makes =B45+1 return 1 for an untouched B45.
    /// Text gives 0 here; the CALLER checks Kind first and raises #VALUE! when
    /// text appears where a number was required.
    /// </summary>
    /// <returns>
    /// The stored number for Number and Boolean, and 0 for Empty, Text and Error.
    /// </returns>
    public double AsNumber() => Kind switch
    {
        ValueKind.Number  => Number,
        ValueKind.Boolean => Number,   // TRUE is 1, FALSE is 0
        ValueKind.Empty   => 0,
        _                 => 0
    };

    /// <summary>
    /// Returns the text a spreadsheet would show for this value.
    /// </summary>
    /// <returns>
    /// An empty string for Empty, the number or text for Number and Text,
    /// "TRUE" or "FALSE" for Boolean, and the usual spreadsheet error text
    /// such as "#DIV/0!" for Error.
    /// </returns>
    public override string ToString() => Kind switch
    {
        ValueKind.Empty   => "",
        ValueKind.Number  => Number.ToString(),
        ValueKind.Text    => Text ?? "",
        ValueKind.Boolean => Boolean ? "TRUE" : "FALSE",
        ValueKind.Error   => Error switch
        {
            ErrorKind.DivideByZero => "#DIV/0!",
            ErrorKind.Value        => "#VALUE!",
            ErrorKind.Reference    => "#REF!",
            ErrorKind.Name         => "#NAME?",
            ErrorKind.Circular     => "#CIRCULAR!",
            ErrorKind.NotAvailable => "#N/A",
            _ => "#ERROR!"
        },
        _ => ""
    };

    /// <summary>Returns the text payload, or an empty string if there is none.</summary>
    /// <returns>
    /// The stored text when Kind is Text, and an empty string for every other kind.
    /// </returns>
    public string AsText() => Text ?? string.Empty;
}