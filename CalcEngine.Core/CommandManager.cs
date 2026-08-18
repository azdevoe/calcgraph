namespace CalcEngine.Core;

/// <summary>
/// Bounded undo/redo controller (Design_Portfolio 6.5, Fig 9).
///
/// <para>
/// <b>Representation:</b> a bounded <see cref="LinkedList{T}"/> used as
/// a deque for the undo stack (capacity 100) and a plain
/// <see cref="Stack{T}"/> for the redo stack.
/// </para>
///
/// <para>
/// <b>AF(m)</b> = the pair (history, future) where<br/>
///   history = undoStack read first to last — operations applied to
///             the workbook, oldest first, that can still be undone<br/>
///   future  = redoStack read top to bottom — operations undone and
///             now available to reapply, next-to-redo first
/// </para>
///
/// <para><b>Representation invariant:</b></para>
/// <list type="number">
///   <item>Neither stack is null and neither contains a null command.</item>
///   <item>undoStack.Count &lt;= 100. On pushing a 101st command the
///         oldest is discarded from the tail.</item>
///   <item>Every command in undoStack has been executed exactly once
///         and has captured the state needed to invert itself.</item>
///   <item>Every command in redoStack has been executed and then undone,
///         in that order.</item>
///   <item>After any call to ExecuteCommand, redoStack is empty.</item>
/// </list>
/// </summary>
public sealed class CommandManager
{
    /// <summary>Maximum number of undoable commands retained.</summary>
    private const int Capacity = 100;

    // LinkedList gives O(1) push/pop at both ends — used as a bounded
    // deque. First = oldest (tail to discard), Last = most recent (top).
    private readonly LinkedList<ICommand> _undoStack = new();

    private readonly Stack<ICommand> _redoStack = new();

    /// <summary>True when at least one command can be undone.</summary>
    public bool CanUndo => _undoStack.Count > 0;

    /// <summary>True when at least one undone command can be reapplied.</summary>
    public bool CanRedo => _redoStack.Count > 0;

    /// <summary>
    /// Executes <paramref name="command"/> and records it so it can be
    /// undone. If the undo stack already holds 100 commands, the oldest
    /// one is silently discarded. Clears the redo stack (RI-5).
    /// </summary>
    public CellChangeSet ExecuteCommand(ICommand command)
{
    var result = command.Execute();

    if (!result.Success)
        return result;

    _redoStack.Clear();
    _undoStack.AddLast(command);

    if (_undoStack.Count > Capacity)
        _undoStack.RemoveFirst();

    return result;
}

    /// <summary>
    /// Undoes the most recent command and moves it to the redo stack.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="CanUndo"/> is false.
    /// </exception>
    public CellChangeSet Undo()
    {
        if (!CanUndo)
            throw new InvalidOperationException("Nothing to undo.");

        var command = _undoStack.Last!.Value;
        _undoStack.RemoveLast();

        var result = command.Undo();
        _redoStack.Push(command);

        return result;
    }

    /// <summary>
    /// Re-executes the most recently undone command and moves it back
    /// to the undo stack.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="CanRedo"/> is false.
    /// </exception>
    public CellChangeSet Redo()
    {
        if (!CanRedo)
            throw new InvalidOperationException("Nothing to redo.");

        var command = _redoStack.Pop();

        var result = command.Execute();
        _undoStack.AddLast(command);

        // Capacity can't be exceeded here: a redo puts back a command
        // that was already counted, and no new command was added.

        return result;
    }
}