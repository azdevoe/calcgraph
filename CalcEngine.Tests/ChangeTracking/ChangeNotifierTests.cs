using CalcEngine.Core.ChangeTracking;
using CalcEngine.Core.Model;
using Xunit;

namespace CalcEngine.Tests.ChangeTracking;

/// <summary>
/// Tests for ChangeNotifier — the Observer implementation that lets the
/// engine inform an unknown number of clients of changes without
/// depending on any of them (Design_Portfolio 4.6, Fig 7).
///
/// Two things are under test beyond basic subscribe/notify: batching
/// (one call per operation, not once per cell — 4.6, 9.1) and the
/// separate circular-reference channel (4.6) that carries the cycle
/// path so the grid can flag every cell involved.
/// </summary>
public class ChangeNotifierTests
{
    private readonly CellRef _a1 = CellRef.Parse("A1");
    private readonly CellRef _b2 = CellRef.Parse("B2");
    private readonly CellRef _c3 = CellRef.Parse("C3");

    /// <summary>Fake observer that records every call it receives.</summary>
    private sealed class RecordingObserver : ICellObserver
    {
        public List<CellChangeSet> ChangedCalls { get; } = new();
        public List<IReadOnlyList<CellRef>> CircularCalls { get; } = new();

        public void OnCellsChanged(CellChangeSet changeSet) => ChangedCalls.Add(changeSet);

        public void OnCircularReference(IReadOnlyList<CellRef> cyclePath) =>
            CircularCalls.Add(cyclePath);
    }

    // ── Subscribe / NotifyChanged ────────────────────────────────────

    [Fact]
    public void NotifyChanged_NoObservers_DoesNotThrow()
    {
        var notifier = new ChangeNotifier();
        var changeSet = CellChangeSet.Ok(_a1, new[] { _a1 });

        var ex = Record.Exception(() => notifier.NotifyChanged(changeSet));

        Assert.Null(ex);
    }

    [Fact]
    public void NotifyChanged_SingleObserver_ReceivesChangeSet()
    {
        var notifier = new ChangeNotifier();
        var observer = new RecordingObserver();
        notifier.Subscribe(observer);

        var changeSet = CellChangeSet.Ok(_a1, new[] { _a1 });
        notifier.NotifyChanged(changeSet);

        Assert.Single(observer.ChangedCalls);
        Assert.Same(changeSet, observer.ChangedCalls[0]);
    }

    [Fact]
    public void NotifyChanged_MultipleObservers_AllReceiveChangeSet()
    {
        var notifier = new ChangeNotifier();
        var observer1 = new RecordingObserver();
        var observer2 = new RecordingObserver();
        notifier.Subscribe(observer1);
        notifier.Subscribe(observer2);

        var changeSet = CellChangeSet.Ok(_a1, new[] { _a1 });
        notifier.NotifyChanged(changeSet);

        Assert.Single(observer1.ChangedCalls);
        Assert.Single(observer2.ChangedCalls);
    }

    [Fact]
    public void NotifyChanged_BatchesIntoOneCallRegardlessOfCellCount()
    {
        // A 500-cell chain must produce ONE notification carrying all
        // 500 cells, not 500 separate calls (4.6, 9.1).
        var notifier = new ChangeNotifier();
        var observer = new RecordingObserver();
        notifier.Subscribe(observer);

        var manyCells = Enumerable.Range(1, 500)
            .Select(i => new CellRef(i, 1))
            .ToArray();
        var changeSet = CellChangeSet.Ok(_a1, manyCells);

        notifier.NotifyChanged(changeSet);

        Assert.Single(observer.ChangedCalls);
        Assert.Equal(500, observer.ChangedCalls[0].ChangedCells.Count);
    }

    [Fact]
    public void NotifyChanged_TwoSeparateOperations_ProducesTwoCalls()
    {
        var notifier = new ChangeNotifier();
        var observer = new RecordingObserver();
        notifier.Subscribe(observer);

        notifier.NotifyChanged(CellChangeSet.Ok(_a1, new[] { _a1 }));
        notifier.NotifyChanged(CellChangeSet.Ok(_b2, new[] { _b2 }));

        Assert.Equal(2, observer.ChangedCalls.Count);
    }

    // ── Unsubscribe ────────────────────────────────────────────────

    [Fact]
    public void Unsubscribe_StopsFurtherNotifications()
    {
        var notifier = new ChangeNotifier();
        var observer = new RecordingObserver();
        notifier.Subscribe(observer);
        notifier.Unsubscribe(observer);

        notifier.NotifyChanged(CellChangeSet.Ok(_a1, new[] { _a1 }));

        Assert.Empty(observer.ChangedCalls);
    }

    [Fact]
    public void Unsubscribe_OneOfTwoObservers_OnlyOtherStillNotified()
    {
        var notifier = new ChangeNotifier();
        var observer1 = new RecordingObserver();
        var observer2 = new RecordingObserver();
        notifier.Subscribe(observer1);
        notifier.Subscribe(observer2);
        notifier.Unsubscribe(observer1);

        notifier.NotifyChanged(CellChangeSet.Ok(_a1, new[] { _a1 }));

        Assert.Empty(observer1.ChangedCalls);
        Assert.Single(observer2.ChangedCalls);
    }

    [Fact]
    public void Unsubscribe_NotPreviouslySubscribed_DoesNotThrow()
    {
        var notifier = new ChangeNotifier();
        var observer = new RecordingObserver();

        var ex = Record.Exception(() => notifier.Unsubscribe(observer));

        Assert.Null(ex);
    }

    [Fact]
    public void Subscribe_SameObserverTwice_NotifiedOnlyOnce()
    {
        // Subscribing twice must not double-fire notifications —
        // the observer set behaves like a set, not a list.
        var notifier = new ChangeNotifier();
        var observer = new RecordingObserver();
        notifier.Subscribe(observer);
        notifier.Subscribe(observer);

        notifier.NotifyChanged(CellChangeSet.Ok(_a1, new[] { _a1 }));

        Assert.Single(observer.ChangedCalls);
    }

    // ── NotifyCircularReference (separate channel) ───────────────────

    [Fact]
    public void NotifyCircularReference_ObserverReceivesCyclePath()
    {
        var notifier = new ChangeNotifier();
        var observer = new RecordingObserver();
        notifier.Subscribe(observer);

        var cyclePath = new[] { _a1, _b2, _c3, _a1 };
        notifier.NotifyCircularReference(cyclePath);

        Assert.Single(observer.CircularCalls);
        Assert.Equal(cyclePath, observer.CircularCalls[0]);
    }

    [Fact]
    public void NotifyCircularReference_DoesNotTriggerOnCellsChanged()
    {
        // The two channels are independent — a circular reference
        // report must not also fire OnCellsChanged.
        var notifier = new ChangeNotifier();
        var observer = new RecordingObserver();
        notifier.Subscribe(observer);

        notifier.NotifyCircularReference(new[] { _a1, _b2, _a1 });

        Assert.Empty(observer.ChangedCalls);
    }

    [Fact]
    public void NotifyChanged_DoesNotTriggerOnCircularReference()
    {
        var notifier = new ChangeNotifier();
        var observer = new RecordingObserver();
        notifier.Subscribe(observer);

        notifier.NotifyChanged(CellChangeSet.Ok(_a1, new[] { _a1 }));

        Assert.Empty(observer.CircularCalls);
    }

    [Fact]
    public void NotifyCircularReference_MultipleObservers_AllReceiveCyclePath()
    {
        var notifier = new ChangeNotifier();
        var observer1 = new RecordingObserver();
        var observer2 = new RecordingObserver();
        notifier.Subscribe(observer1);
        notifier.Subscribe(observer2);

        var cyclePath = new[] { _a1, _b2, _a1 };
        notifier.NotifyCircularReference(cyclePath);

        Assert.Single(observer1.CircularCalls);
        Assert.Single(observer2.CircularCalls);
    }

    [Fact]
    public void Unsubscribe_StopsCircularReferenceNotificationsToo()
    {
        var notifier = new ChangeNotifier();
        var observer = new RecordingObserver();
        notifier.Subscribe(observer);
        notifier.Unsubscribe(observer);

        notifier.NotifyCircularReference(new[] { _a1, _b2, _a1 });

        Assert.Empty(observer.CircularCalls);
    }
}