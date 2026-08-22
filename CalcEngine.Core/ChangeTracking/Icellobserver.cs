using CalcEngine.Core.Model;

namespace CalcEngine.Core.ChangeTracking;

/// <summary>
/// A client that wants to be told about changes to the workbook —
/// typically the GUI grid. The engine informs an unknown number of
/// these without depending on any of them.
/// </summary>
public interface ICellObserver
{
    /// <summary>
    /// Called once for each operation the client carries out, with every cell
    /// that changed. A recalculation running through 500 cells is one call
    /// carrying 500 cells, not 500 separate calls.
    /// </summary>
    /// <param name="changeSet">The complete record of what changed.</param>
    void OnCellsChanged(CellChangeSet changeSet);

    /// <summary>
    /// Called when an edit is refused for making a cell depend on itself.
    /// This happens instead of OnCellsChanged, not as well as it.
    /// </summary>
    /// <param name="cyclePath">
    /// The cells that lead round the loop, with the edited cell appearing
    /// again at the end, so that every cell caught up in it can be flagged.
    /// </param>
    void OnCircularReference(IReadOnlyList<CellRef> cyclePath);
}