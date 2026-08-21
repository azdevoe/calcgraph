using CalcEngine.Core.ChangeTracking;
using CalcEngine.Core.Engine;
using CalcEngine.Core.Model;

namespace CalcEngine.Core.Commands;

/// <summary>
/// A reversible single-cell edit (Design_Portfolio 4.9, Fig 9).
///
/// On Execute the command captures the cell's current raw input from
/// the engine, then writes the new raw input through the internal
/// ApplyEdit path. On Undo it replays the captured old input through
/// the same path — or clears the cell if the old input was empty
/// (i.e. the cell didn't exist before).
///
/// This design ensures the dependency graph is always rebuilt by the
/// same code that maintains it during ordinary editing; cached values
/// are never restored directly, so the graph and the workbook cannot
/// drift apart (Design_Portfolio 4.9, Section 6.5 RI).
///
/// Note: calls CalculationEngine.ApplyEdit (internal), NOT the public
/// SetCellContent — the public method routes through CommandManager,
/// which would cause infinite recursion.
/// </summary>
public sealed class SetCellCommand : ICommand
{
    private readonly CalculationEngine _engine;
    private readonly CellRef _cellRef;
    private readonly string _newRawInput;
    private string _oldRawInput = string.Empty;

    /// <summary>
    /// Creates a command that will set <paramref name="cellRef"/> to
    /// <paramref name="newRawInput"/> when executed. The previous
    /// content is captured automatically at Execute time.
    /// </summary>
    public SetCellCommand(CalculationEngine engine, CellRef cellRef, string newRawInput)
    {
        _engine = engine;
        _cellRef = cellRef;
        _newRawInput = newRawInput;
    }

    /// <inheritdoc />
    public CellChangeSet Execute()
    {
        // Capture what the cell holds right now so Undo can restore it.
        // After an Undo → re-Execute cycle the cell is back to
        // _oldRawInput, so re-capturing gives the same value.
        _oldRawInput = _engine.GetFormula(_cellRef);
        return _engine.ApplyEdit(_cellRef, _newRawInput);
    }

    /// <inheritdoc />
    public CellChangeSet Undo()
    {
        // An empty old input means the cell didn't exist before this
        // command. Passing "" to ApplyEdit triggers the clear path.
        return _engine.ApplyEdit(_cellRef, _oldRawInput);
    }
}
