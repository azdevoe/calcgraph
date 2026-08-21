namespace CalcEngine.Core.Model;

/// <summary>
/// What a cell holds after evaluation. A tagged union over five states.
/// readonly struct for the same reasons as CellRef: no heap allocation in the
/// recalculation loop, and a snapshot handed to the GUI cannot mutate underneath it.
/// </summary>
public readonly record struct CellValue
{
    public ValueKind Kind  { get; }
    public double    Number { get; }
    public string?   Text  { get; }
    public ErrorKind Error { get; }

    // Private — the only way in is through the factory methods below.
    // That guarantees the representation invariant: you cannot build a
    // CellValue whose Kind disagrees with its payload.
    private CellValue(ValueKind kind, double number, string? text, ErrorKind error)
    {
        Kind = kind; Number = number; Text = text; Error = error;
    }

    public static readonly CellValue Empty =
        new(ValueKind.Empty, 0, null, ErrorKind.None);

    public static CellValue FromNumber(double d)  => new(ValueKind.Number,  d, null, ErrorKind.None);
    public static CellValue FromText(string s)    => new(ValueKind.Text,    0, s,    ErrorKind.None);
    public static CellValue FromBoolean(bool b)   => new(ValueKind.Boolean, b ? 1 : 0, null, ErrorKind.None);
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
    public double AsNumber() => Kind switch
    {
        ValueKind.Number  => Number,
        ValueKind.Boolean => Number,   // TRUE is 1, FALSE is 0
        ValueKind.Empty   => 0,
        _                 => 0
    };

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

    public string AsText() => Text ?? string.Empty;
}