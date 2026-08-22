using CalcEngine.Core.ChangeTracking;

namespace CalcEngine.Core.Commands;

/// <summary>
/// Bounded undo/redo controller.
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

    /// <summary>Gets a value indicating whether there is anything to undo.</summary>
    /// <value>true if at least one command can be undone; otherwise, false.</value>
    public bool CanUndo => _undoStack.Count > 0;

    /// <summary>Gets a value indicating whether there is anything to redo.</summary>
    /// <value>true if at least one undone command can be reapplied; otherwise, false.</value>
    public bool CanRedo => _redoStack.Count > 0;

    /// <summary>
    /// Carries out a command and remembers it so that it can be undone
    /// later.
    /// </summary>
    /// <param name="command">The command to carry out.</param>
    /// <returns>
    /// Whatever the command reports. A command that is refused is not
    /// remembered, so it can be neither undone nor redone, and anything that
    /// was waiting to be redone stays available.
    /// </returns>
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
    /// Reverses the most recent command, and makes it available to redo.
    /// </summary>
    /// <returns>A record of what changed in the course of reversing it.</returns>
    /// <exception cref="InvalidOperationException">
    /// There is nothing to undo. Check CanUndo first.
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
    /// Carries out the most recently undone command again, and makes it
    /// available to undo.
    /// </summary>
    /// <returns>A record of what changed.</returns>
    /// <exception cref="InvalidOperationException">
    /// There is nothing to redo. Check CanRedo first.
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

    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
    }
}