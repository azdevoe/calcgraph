namespace CalcEngine.Core.Model;

/// <summary>Which of the five states a CellValue is in. The "tag" of the tagged union.</summary>
public enum ValueKind
{
    /// <summary>Nothing was ever typed here. Arithmetic treats it as 0.</summary>
    Empty,

    /// <summary>A number. The payload is in CellValue.Number.</summary>
    Number,

    /// <summary>Text. The payload is in CellValue.Text.</summary>
    Text,

    /// <summary>TRUE or FALSE, stored as 1 or 0 in CellValue.Number.</summary>
    Boolean,

    /// <summary>An error value. The type of error is in CellValue.Error.</summary>
    Error
}

/// <summary>The six error values a user can see in the grid.</summary>
public enum ErrorKind
{
    /// <summary>
    /// No error. Deliberately 0 so that a default ErrorKind means "nothing wrong".
    /// </summary>
    None = 0,

    /// <summary>Division by zero. Shown as #DIV/0!.</summary>
    DivideByZero,

    /// <summary>Wrong type, such as text where a number was needed. Shown as #VALUE!.</summary>
    Value,

    /// <summary>A reference that no longer points anywhere usable. Shown as #REF!.</summary>
    Reference,

    /// <summary>An unknown function name. Shown as #NAME?.</summary>
    Name,

    /// <summary>The cell takes part in a circular reference. Shown as #CIRCULAR!.</summary>
    Circular,

    /// <summary>LOOKUP found no match. Shown as #N/A.</summary>
    NotAvailable
}