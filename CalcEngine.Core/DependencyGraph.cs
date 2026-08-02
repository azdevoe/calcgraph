namespace CalcEngine.Core;

/// <summary>
/// Which cells read which. Edge u -> v means "v depends on u",
/// so u must be computed before v.
/// Two mirrored maps: dependents answers "who must I recompute?",
/// precedents makes dropping a formula's old edges O(d) instead of O(E).
/// </summary>
public sealed class DependencyGraph
{
    private readonly Dictionary<CellRef, HashSet<CellRef>> precedents = new();
    private readonly Dictionary<CellRef, HashSet<CellRef>> dependents = new();

    private static readonly CellRef[] None = Array.Empty<CellRef>();

    public IReadOnlyCollection<CellRef> PrecedentsOf(CellRef c)
        => precedents.TryGetValue(c, out var s) ? s : None;

    public IReadOnlyCollection<CellRef> DependentsOf(CellRef c)
        => dependents.TryGetValue(c, out var s) ? s : None;

    /// <summary>
    /// Point `cell` at a new set of references. Returns null if accepted,
    /// or the cycle path if the edit would close a loop — in which case the
    /// previous edges are restored, so the graph is never left cyclic.
    /// </summary>
    public IReadOnlyList<CellRef>? SetDependencies(CellRef cell, IEnumerable<CellRef> dependsOn)
    {
        var old = precedents.TryGetValue(cell, out var p)
            ? new HashSet<CellRef>(p)
            : new HashSet<CellRef>();

        RemoveIncomingEdges(cell);
        foreach (var u in dependsOn) AddEdge(u, cell);

        var cycle = FindCycle(cell);
        if (cycle is not null)
        {
            RemoveIncomingEdges(cell);
            foreach (var u in old) AddEdge(u, cell);
            return cycle;
        }
        return null;
    }

    public bool HasCycle()
    {
        foreach (var node in dependents.Keys)
            if (FindCycle(node) is not null) return true;
        return false;
    }

    /// <summary>
    /// One cycle reachable from `start`, in order, entry cell repeated at
    /// the end: A1 -> B3 -> C7 -> A1. Null if there is none.
    /// </summary>
    public IReadOnlyList<CellRef>? FindCycle(CellRef start)
        => Walk(start, new HashSet<CellRef>(), new List<CellRef>());

    /// <summary>
    /// Everything that must be recomputed when `cell` changes — direct and
    /// indirect. Reverse BFS over dependents. Excludes `cell` itself.
    /// </summary>
    public IReadOnlyList<CellRef> GetAffectedCells(CellRef cell)
    {
        var seen = new HashSet<CellRef>();
        var result = new List<CellRef>();
        var queue = new Queue<CellRef>();
        queue.Enqueue(cell);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!dependents.TryGetValue(current, out var next)) continue;

            foreach (var d in next)
                if (seen.Add(d))          // false if already seen — stops the
                {                          // diamond producing D1 twice
                    result.Add(d);
                    queue.Enqueue(d);
                }
        }
        return result;
    }

    /// <summary>
    /// Orders `cells` so nothing is computed before what it reads.
    /// Kahn's algorithm, restricted to the given set.
    /// </summary>
    public IReadOnlyList<CellRef> TopologicalSort(IEnumerable<CellRef> cells)
    {
        var set = new HashSet<CellRef>(cells);

        var indegree = new Dictionary<CellRef, int>();
        foreach (var c in set)
        {
            int n = 0;
            if (precedents.TryGetValue(c, out var ps))
                foreach (var q in ps)
                    if (set.Contains(q)) n++;
            indegree[c] = n;
        }

        var ready = new Queue<CellRef>();
        foreach (var c in set)
            if (indegree[c] == 0) ready.Enqueue(c);

        var order = new List<CellRef>(set.Count);
        while (ready.Count > 0)
        {
            var c = ready.Dequeue();
            order.Add(c);

            if (!dependents.TryGetValue(c, out var ds)) continue;
            foreach (var d in ds)
                if (set.Contains(d) && --indegree[d] == 0)
                    ready.Enqueue(d);
        }

        if (order.Count < set.Count)
            foreach (var c in set)
                if (!order.Contains(c)) order.Add(c);

        return order;
    }

    // ---------- private ----------

    private IReadOnlyList<CellRef>? Walk(CellRef src, HashSet<CellRef> visited, List<CellRef> path)
    {
        int at = path.IndexOf(src);
        if (at >= 0)
        {
            var cycle = path.GetRange(at, path.Count - at);
            cycle.Add(src);
            return cycle;
        }

        if (!visited.Add(src)) return null;

        path.Add(src);

        if (dependents.TryGetValue(src, out var next))
            foreach (var n in next)
            {
                var found = Walk(n, visited, path);
                if (found is not null) return found;
            }

        path.RemoveAt(path.Count - 1);
        return null;
    }

    private void AddEdge(CellRef from, CellRef to)
    {
        if (!dependents.TryGetValue(from, out var d)) dependents[from] = d = new();
        d.Add(to);
        if (!precedents.TryGetValue(to, out var p)) precedents[to] = p = new();
        p.Add(from);
    }

    /// <summary>Drops every edge INTO cell, keeping both maps consistent.</summary>
    private void RemoveIncomingEdges(CellRef cell)
    {
        if (!precedents.TryGetValue(cell, out var old)) return;
        foreach (var u in old)
            if (dependents.TryGetValue(u, out var d)) d.Remove(cell);
        old.Clear();
    }
}