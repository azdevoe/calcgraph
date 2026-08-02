namespace CalcEngine.Core;

/// <summary>
/// A client that wants to be told about changes to the workbook —
/// typically the GUI grid. The engine informs an unknown number of
/// these without depending on any of them (Design_Portfolio 4.6, Fig 7).
/// </summary>
public interface ICellObserver
{
    /// <summary>
    /// Called once per client operation with the complete set of cells
    /// that changed — never once per cell. A 500-cell recalculation
    /// chain is one call carrying 500 cells, not 500 calls.
    /// </summary>
    void OnCellsChanged(CellChangeSet changeSet);

    /// <summary>
    /// Called instead of (not in addition to) OnCellsChanged when an
    /// edit is rejected for creating a circular reference. Carries the
    /// full cycle path so the grid can flag every cell involved, not
    /// just the one last edited.
    /// </summary>
    void OnCircularReference(IReadOnlyList<CellRef> cyclePath);
}