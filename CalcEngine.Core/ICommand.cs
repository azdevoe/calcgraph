namespace CalcEngine.Core;

/// <summary>
/// A reversible operation on the spreadsheet engine (Design_Portfolio
/// 4.9, Fig 9). Every command can execute itself and undo itself,
/// returning the set of cells affected each time.
///
/// Commands store the raw input text before and after, not computed
/// values. Undoing replays the old text through the normal edit path
/// so that the dependency graph, the cached expression tree and every
/// dependent cell are restored by the same code that maintains them
/// during ordinary editing.
/// </summary>
public interface ICommand
{
    /// <summary>
    /// Applies this operation to the workbook. Must be called exactly
    /// once before Undo, and may be called again after an Undo to
    /// support redo.
    /// </summary>
    CellChangeSet Execute();

    /// <summary>
    /// Inverts this operation, restoring the workbook to the state
    /// before Execute was last called.
    /// </summary>
    CellChangeSet Undo();
}