namespace CalcEngine.Core;

/// <summary>
/// The single return type for every client-facing operation on
/// CalculationEngine (Design_Portfolio 4.1). A successful edit, a
/// parse failure, and a circular reference all come back through this
/// one type, as data — the client is never required to catch anything.
///
/// Exactly one of two things is true of any instance: Success is true
/// and ChangedCells describes what was recomputed, or Success is false
/// and exactly one of ErrorMessage / CircularPath explains why.
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

    /// <summary>Set when Success is false due to a parse failure; otherwise null.</summary>
    public string? ErrorMessage { get; }

    /// <summary>
    /// Set when Success is false because the edit would create a
    /// circular reference; otherwise null. The path reads
    /// A1, B3, C7, A1 — the entry cell repeated at the end.
    /// </summary>
    public IReadOnlyList<CellRef>? CircularPath { get; }

    private CellChangeSet(
        CellRef edited,
        bool success,
        IReadOnlyList<CellRef> changedCells,
        string? errorMessage,
        IReadOnlyList<CellRef>? circularPath)
    {
        Edited = edited;
        Success = success;
        ChangedCells = changedCells;
        ErrorMessage = errorMessage;
        CircularPath = circularPath;
    }

    /// <summary>A successful edit. changedCells includes edited itself.</summary>
    public static CellChangeSet Ok(CellRef edited, IReadOnlyList<CellRef> changedCells) =>
        new(edited, success: true, changedCells, errorMessage: null, circularPath: null);

    /// <summary>The raw input could not be parsed as a valid formula or literal.</summary>
    public static CellChangeSet ParseFailure(CellRef edited, string message) =>
        new(edited, success: false, Array.Empty<CellRef>(), message, circularPath: null);

    /// <summary>The edit was rejected because it would create a circular reference.</summary>
    public static CellChangeSet Circular(CellRef edited, IReadOnlyList<CellRef> cyclePath) =>
        new(edited, success: false, Array.Empty<CellRef>(), errorMessage: null, cyclePath);
}