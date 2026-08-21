using CalcEngine.Core.Model;

namespace CalcEngine.Core.ChangeTracking;

/// <summary>
/// Observer subject: lets the engine inform an unknown number of
/// ICellObserver clients of changes without depending on any of them
/// (Design_Portfolio 4.6, Fig 7).
///
/// A HashSet, not a List, backs the subscriber collection — subscribing
/// the same observer twice must not cause it to be notified twice.
/// </summary>
public sealed class ChangeNotifier
{
    private readonly HashSet<ICellObserver> _observers = new();

    /// <summary>Registers an observer. Subscribing an already-subscribed observer is a no-op.</summary>
    public void Subscribe(ICellObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        _observers.Add(observer);
    }

    /// <summary>Removes an observer. A no-op if it was never subscribed.</summary>
    public void Unsubscribe(ICellObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        _observers.Remove(observer);
    }

    /// <summary>
    /// Notifies every subscribed observer of a completed operation, in
    /// one call each — batching is the caller's responsibility (the
    /// caller must have already assembled the full CellChangeSet).
    /// </summary>
    public void NotifyChanged(CellChangeSet changeSet)
    {
        ArgumentNullException.ThrowIfNull(changeSet);
        foreach (var observer in _observers)
            observer.OnCellsChanged(changeSet);
    }

    /// <summary>
    /// Notifies every subscribed observer that an edit was rejected for
    /// creating a circular reference. Independent of NotifyChanged —
    /// an operation triggers exactly one of the two, never both.
    /// </summary>
    public void NotifyCircularReference(IReadOnlyList<CellRef> cyclePath)
    {
        ArgumentNullException.ThrowIfNull(cyclePath);
        foreach (var observer in _observers)
            observer.OnCircularReference(cyclePath);
    }
}