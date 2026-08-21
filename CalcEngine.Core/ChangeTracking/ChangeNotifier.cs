using CalcEngine.Core.Model;

namespace CalcEngine.Core.ChangeTracking;

/// <summary>
/// Observer subject: lets the engine inform an unknown number of
/// ICellObserver clients of changes without depending on any of them.
///
/// A HashSet, not a List, backs the subscriber collection — subscribing
/// the same observer twice must not cause it to be notified twice.
/// </summary>
public sealed class ChangeNotifier
{
    private readonly HashSet<ICellObserver> _observers = new();

    /// <summary>
    /// Signs an observer up to be told about future changes. Signing the same
    /// observer up twice makes no difference; it is still told once.
    /// </summary>
    /// <param name="observer">The observer to sign up.</param>
    /// <exception cref="ArgumentNullException"><paramref name="observer"/> is null.</exception>
    public void Subscribe(ICellObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        _observers.Add(observer);
    }

    /// <summary>
    /// Stops telling an observer about changes. Removing one that was never
    /// signed up makes no difference.
    /// </summary>
    /// <param name="observer">The observer to remove.</param>
    /// <exception cref="ArgumentNullException"><paramref name="observer"/> is null.</exception>
    public void Unsubscribe(ICellObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        _observers.Remove(observer);
    }

    /// <summary>
    /// Tells every signed-up observer about a completed operation.
    /// </summary>
    /// <param name="changeSet">
    /// The complete record of what changed. Each observer is told once,
    /// however many cells it covers.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="changeSet"/> is null.</exception>
    public void NotifyChanged(CellChangeSet changeSet)
    {
        ArgumentNullException.ThrowIfNull(changeSet);
        foreach (var observer in _observers)
            observer.OnCellsChanged(changeSet);
    }

    /// <summary>
    /// Tells every signed-up observer that an edit was refused for making a
    /// cell depend on itself. An operation causes either this or
    /// NotifyChanged, never both.
    /// </summary>
    /// <param name="cyclePath">
    /// The cells that lead round the loop, so that every cell caught up in it
    /// can be flagged and not just the one last edited.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="cyclePath"/> is null.</exception>
    public void NotifyCircularReference(IReadOnlyList<CellRef> cyclePath)
    {
        ArgumentNullException.ThrowIfNull(cyclePath);
        foreach (var observer in _observers)
            observer.OnCircularReference(cyclePath);
    }
}