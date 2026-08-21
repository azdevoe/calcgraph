namespace CalcEngine.Core.Validation;

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

    /// <summary>Creates a result saying the value is allowed.</summary>
    /// <returns>A result whose Success is true and whose ErrorMessage is null.</returns>
    public static ValidationResult Ok() => new(success: true, errorMessage: null);

    /// <summary>Creates a result saying the value is not allowed.</summary>
    /// <param name="message">
    /// Why the value was refused, worded so it can be shown to a user.
    /// </param>
    /// <returns>A result whose Success is false and which carries the message.</returns>
    public static ValidationResult Fail(string message) => new(success: false, message);
}