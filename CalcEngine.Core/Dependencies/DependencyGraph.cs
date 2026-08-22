using CalcEngine.Core.Model;

namespace CalcEngine.Core.Dependencies;

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

    /// <summary>Returns the cells a given cell reads.</summary>
    /// <param name="c">The cell to ask about.</param>
    /// <returns>
    /// The cells its formula reads, in no particular order. Empty if it reads
    /// nothing, or holds no formula at all.
    /// </returns>
    public IReadOnlyCollection<CellRef> PrecedentsOf(CellRef c)
        => precedents.TryGetValue(c, out var s) ? s : None;

    /// <summary>Returns the cells that read a given cell.</summary>
    /// <param name="c">The cell to ask about.</param>
    /// <returns>
    /// The cells whose formulas read it directly, in no particular order.
    /// Cells that read it only through another cell are not included.
    /// </returns>
    public IReadOnlyCollection<CellRef> DependentsOf(CellRef c)
        => dependents.TryGetValue(c, out var s) ? s : None;

    /// <summary>
    /// Records which cells a cell now reads, replacing whatever it read
    /// before.
    /// </summary>
    /// <param name="cell">The cell whose formula has changed.</param>
    /// <param name="dependsOn">
    /// The cells the new formula reads. Pass an empty sequence for a cell
    /// that reads nothing.
    /// </param>
    /// <returns>
    /// null if the change was accepted. If it would have made a cell depend
    /// on itself, directly or through others, the change is refused and the
    /// loop is returned instead, naming the cells in the order they lead
    /// round with the starting cell appearing again at the end. The recorded
    /// dependencies are left exactly as they were.
    /// </returns>
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

    /// <summary>Reports whether any cell currently depends on itself.</summary>
    /// <returns>
    /// true if a loop exists anywhere; otherwise, false. A graph built only
    /// through SetDependencies never has one.
    /// </returns>
    public bool HasCycle()
    {
        foreach (var node in dependents.Keys)
            if (FindCycle(node) is not null) return true;
        return false;
    }

    /// <summary>Looks for a loop of dependencies leading out of a cell.</summary>
    /// <param name="start">The cell to start from.</param>
    /// <returns>
    /// The cells that lead round the loop, in order, with the starting cell
    /// appearing again at the end, as in A1, B3, C7, A1. null if there is no
    /// loop.
    /// </returns>
    public IReadOnlyList<CellRef>? FindCycle(CellRef start)
        => Walk(start, new HashSet<CellRef>(), new List<CellRef>());

    /// <summary>
    /// Returns everything that has to be worked out again when a cell
    /// changes.
    /// </summary>
    /// <param name="cell">The cell that changed.</param>
    /// <returns>
    /// Every cell that reads it, whether directly or through other cells.
    /// The cell itself is not included, and no cell appears twice even if
    /// there is more than one route to it. 
    /// </returns>
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
    /// Puts a set of cells into an order where no cell comes before one it
    /// reads.
    /// </summary>
    /// <param name="cells">The cells to order. Duplicates are ignored.</param>
    /// <returns>
    /// The same cells, arranged so that no cell appears before another cell
    /// in the set that it reads. Dependencies on cells outside the set place
    /// no constraint on the order.
    /// </returns>
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