using CalcEngine.Core.ChangeTracking;
using CalcEngine.Core.Engine;
using CalcEngine.Core.Model;

namespace CalcEngine.Core.Commands;

/// <summary>
/// A reversible edit to one cell.
///
/// The command remembers the text the cell held before, not the value it
/// worked out to, so undoing an edit puts the cell back exactly as the
/// user left it and everything that reads that cell is brought up to date
/// along with it.
/// </summary>
public sealed class SetCellCommand : ICommand
{
    private readonly CalculationEngine _engine;
    private readonly CellRef _cellRef;
    private readonly string _newRawInput;
    private string _oldRawInput = string.Empty;

    /// <summary>
    /// Creates an edit that has not been carried out yet.
    /// </summary>
    /// <param name="engine">The engine holding the cell to edit.</param>
    /// <param name="cellRef">The cell to edit.</param>
    /// <param name="newRawInput">
    /// The text to put in the cell. An empty string empties the cell.
    /// </param>
    public SetCellCommand(CalculationEngine engine, CellRef cellRef, string newRawInput)
    {
        _engine = engine;
        _cellRef = cellRef;
        _newRawInput = newRawInput;
    }

    /// <summary>Puts the new text in the cell.</summary>
    /// <returns>
    /// The cells whose values changed as a result, or the reason the edit was
    /// refused. An edit is refused if the text cannot be read as a formula,
    /// if it would make the cell depend on itself, or if it breaks a
    /// validation rule set on the cell; in each case nothing changes.
    /// </returns>
    public CellChangeSet Execute()
    {
        // Capture what the cell holds right now so Undo can restore it.
        // After an Undo → re-Execute cycle the cell is back to
        // _oldRawInput, so re-capturing gives the same value.
        _oldRawInput = _engine.GetFormula(_cellRef);
        return _engine.ApplyEdit(_cellRef, _newRawInput);
    }

    /// <summary>Puts the cell back to the text it held before the edit.</summary>
    /// <returns>
    /// The cells whose values changed as a result. A cell that did not exist
    /// before the edit is emptied again.
    /// </returns>
    public CellChangeSet Undo()
    {
        // An empty old input means the cell didn't exist before this
        // command. Passing "" to ApplyEdit triggers the clear path.
        return _engine.ApplyEdit(_cellRef, _oldRawInput);
    }
}
