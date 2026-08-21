using CalcEngine.Core.Model;

namespace CalcEngine.Core.ChangeTracking;

/// <summary>
/// Discriminates why a CellChangeSet failed, so a client can react
/// differently to a syntax error vs a rejected validation rule vs a
/// circular reference, without string-matching ErrorMessage.
/// </summary>
public enum ChangeFailureReason
{
    /// <summary>Success is true — no failure.</summary>
    None,
    /// <summary>The raw input could not be parsed.</summary>
    ParseError,
    /// <summary>The edit would create a circular reference.</summary>
    Circular,
    /// <summary>The value was rejected by a data validation rule.</summary>
    ValidationError
}

/// <summary>
/// The single return type for every client-facing operation on
/// CalculationEngine. A successful edit, a parse failure, a circular
/// reference, and a validation rejection all come back through this one
/// type, as data — the client is never required to catch anything.
///
/// Exactly one of these is true of any instance: Success is true and
/// ChangedCells describes what was recomputed, or Success is false and
/// FailureReason plus exactly one of ErrorMessage / CircularPath
/// explains why.
/// </summary>
public sealed class CellChangeSet
{
    /// <summary>The cell the client directly edited.</summary>
    public CellRef Edited { get; }

    /// <summary>True iff the edit succeeded and the workbook was updated.</summary>
    public bool Success { get; }

    /// <summary>
    /// Every cell recomputed as a result of this operation, including
    /// Edited itself, in the order they were recalculated. Empty when
    /// Success is false.
    /// </summary>
    public IReadOnlyList<CellRef> ChangedCells { get; }

    /// <summary>Set when Success is false due to a parse or validation failure; otherwise null.</summary>
    public string? ErrorMessage { get; }

    /// <summary>
    /// Set when Success is false because the edit would create a
    /// circular reference; otherwise null. The path reads
    /// A1, B3, C7, A1 — the entry cell repeated at the end.
    /// </summary>
    public IReadOnlyList<CellRef>? CircularPath { get; }

    /// <summary>Why this result failed. ChangeFailureReason.None when Success is true.</summary>
    public ChangeFailureReason FailureReason { get; }

    private CellChangeSet(
        CellRef edited,
        bool success,
        IReadOnlyList<CellRef> changedCells,
        string? errorMessage,
        IReadOnlyList<CellRef>? circularPath,
        ChangeFailureReason failureReason)
    {
        Edited = edited;
        Success = success;
        ChangedCells = changedCells;
        ErrorMessage = errorMessage;
        CircularPath = circularPath;
        FailureReason = failureReason;
    }

    /// <summary>Creates a result describing an edit that went through.</summary>
    /// <param name="edited">The cell the client edited.</param>
    /// <param name="changedCells">
    /// Every cell whose value changed, including the edited cell itself.
    /// </param>
    /// <returns>A result whose Success is true.</returns>
    public static CellChangeSet Ok(CellRef edited, IReadOnlyList<CellRef> changedCells) =>
        new(edited, success: true, changedCells, errorMessage: null, circularPath: null, ChangeFailureReason.None);

    /// <summary>
    /// Creates a result describing an edit refused because the text could not
    /// be read as a formula or a value.
    /// </summary>
    /// <param name="edited">The cell the client tried to edit.</param>
    /// <param name="message">What was wrong with the text, and where.</param>
    /// <returns>
    /// A result whose Success is false and whose FailureReason is ParseError.
    /// Nothing in the workbook has changed.
    /// </returns>
    public static CellChangeSet ParseFailure(CellRef edited, string message) =>
        new(edited, success: false, Array.Empty<CellRef>(), message, circularPath: null, ChangeFailureReason.ParseError);

    /// <summary>
    /// Creates a result describing an edit refused because it would have made
    /// a cell depend on itself.
    /// </summary>
    /// <param name="edited">The cell the client tried to edit.</param>
    /// <param name="cyclePath">
    /// The cells that lead round the loop, with the edited cell appearing
    /// again at the end.
    /// </param>
    /// <returns>
    /// A result whose Success is false and whose FailureReason is Circular.
    /// Nothing in the workbook has changed.
    /// </returns>
    public static CellChangeSet Circular(CellRef edited, IReadOnlyList<CellRef> cyclePath) =>
        new(edited, success: false, Array.Empty<CellRef>(), errorMessage: null, cyclePath, ChangeFailureReason.Circular);

    /// <summary>
    /// Creates a result describing an edit refused by a validation rule set on
    /// the cell.
    /// </summary>
    /// <param name="edited">The cell the client tried to edit.</param>
    /// <param name="message">Why the value was not allowed.</param>
    /// <returns>
    /// A result whose Success is false and whose FailureReason is
    /// ValidationError. Nothing in the workbook has changed.
    /// </returns>
    public static CellChangeSet ValidationFailed(CellRef edited, string message) =>
        new(edited, success: false, Array.Empty<CellRef>(), message, circularPath: null, ChangeFailureReason.ValidationError);
}