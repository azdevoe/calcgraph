using CalcEngine.Core.ChangeTracking;

namespace CalcEngine.Core.Commands;

/// <summary>
/// A reversible operation on the spreadsheet engine. Every command can
/// execute itself and undo itself, returning the set of cells affected
/// each time.
///
/// Commands store the raw input text before and after, not computed
/// values. Undoing replays the old text through the normal edit path
/// so that the dependency graph, the cached expression tree and every
/// dependent cell are restored by the same code that maintains them
/// during ordinary editing.
/// </summary>
public interface ICommand
{
    /// <summary>Carries out this operation.</summary>
    /// <returns>
    /// A record of what changed, or of why the operation was refused. When it
    /// is refused, nothing has changed and the operation may be discarded.
    /// </returns>
    CellChangeSet Execute();

    /// <summary>
    /// Reverses this operation, putting the workbook back as it was before
    /// Execute was last called.
    /// </summary>
    /// <returns>A record of what changed in the course of reversing it.</returns>
    CellChangeSet Undo();
}