namespace CalcEngine.Core;

/// <summary>Which of the five states a CellValue is in. The "tag" of the tagged union.</summary>
public enum ValueKind
{
    Empty,      // nothing was ever typed here
    Number,
    Text,
    Boolean,
    Error
}

/// <summary>The six error values a user can see in the grid.</summary>
public enum ErrorKind
{
    None = 0,        // deliberately 0 so `default` means "no error"
    DivideByZero,    // #DIV/0!
    Value,           // #VALUE!  — wrong type
    Reference,       // #REF!    — cell removed or invalidated
    Name,            // #NAME?   — unknown function
    Circular,        // #CIRCULAR!
    NotAvailable     // #N/A     — LOOKUP found nothing
}