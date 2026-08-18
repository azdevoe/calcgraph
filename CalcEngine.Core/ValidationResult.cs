namespace CalcEngine.Core;

/// <summary>
/// The return type of IValidationRule.Validate. Same shape as
/// CellChangeSet / FormulaParseResult: a passed check and a failed
/// check both come back as data, never an exception.
/// </summary>
public sealed class ValidationResult
{
    /// <summary>True iff the value satisfied the rule.</summary>
    public bool Success { get; }

    /// <summary>Set when Success is false; otherwise null.</summary>
    public string? ErrorMessage { get; }

    private ValidationResult(bool success, string? errorMessage)
    {
        Success = success;
        ErrorMessage = errorMessage;
    }

    /// <summary>The value satisfied the rule.</summary>
    public static ValidationResult Ok() => new(success: true, errorMessage: null);

    /// <summary>The value violated the rule, with a client-facing explanation.</summary>
    public static ValidationResult Fail(string message) => new(success: false, message);
}