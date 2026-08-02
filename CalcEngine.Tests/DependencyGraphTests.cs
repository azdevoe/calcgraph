using CalcEngine.Core;
using Xunit;

namespace CalcEngine.Tests;

/// <summary>
/// Tests for DependencyGraph — which cells read which.
/// Edge direction: u -> v means "v depends on u", i.e. u must be
/// computed BEFORE v. Arrows point in the direction data flows.
/// </summary>
public class DependencyGraphTests
{
    // Helper so tests read like the spreadsheet they describe.
    private static CellRef R(string s) => CellRef.Parse(s);

    // ---------- edges ----------

    // The empty graph is a legal graph. HasCycle must answer sensibly
    // for it — false — rather than requiring SetDependencies first.
    [Fact]
    public void EmptyGraph_HasNoCycle()
        => Assert.False(new DependencyGraph().HasCycle());

    // B2 = A1 + 1  ->  edge A1 -> B2
    [Fact]
    public void SetDependencies_RegistersEdge_AndReportsNoCycle()
    {
        var g = new DependencyGraph();
        var cycle = g.SetDependencies(R("B2"), new[] { R("A1") });
        Assert.Null(cycle);              // null means "accepted, no cycle"
    }

    // Editing a formula must DROP its old references, not accumulate them.
    // B2 = A1, then B2 = C3. A1 must no longer be a precedent of B2.
    // This is the case the mirrored `precedents` map exists to make cheap.
    [Fact]
    public void SetDependencies_ReplacesOldEdges_RatherThanAdding()
    {
        var g = new DependencyGraph();
        g.SetDependencies(R("B2"), new[] { R("A1") });
        g.SetDependencies(R("B2"), new[] { R("C3") });

        Assert.Equal(new[] { R("C3") }, g.PrecedentsOf(R("B2")));
        Assert.Empty(g.DependentsOf(R("A1")));   // A1 was fully unlinked
    }

    // ---------- cycle detection ----------

    // A1 = A1. A cycle of length one, and the shortest thing that can
    // hang a naive evaluator forever.
    [Fact]
    public void SelfReference_IsRejectedAsACycle()
    {
        var g = new DependencyGraph();
        var cycle = g.SetDependencies(R("A1"), new[] { R("A1") });
        Assert.NotNull(cycle);
    }

    // The brief's own example: A1 -> B3 -> C7 -> back to A1.
    // Detection alone is not enough — the EXACT path must be reported.
    [Fact]
    public void Cycle_IsReportedWithTheExactPath()
    {
        var g = new DependencyGraph();
        g.SetDependencies(R("B3"), new[] { R("A1") });   // B3 reads A1
        g.SetDependencies(R("C7"), new[] { R("B3") });   // C7 reads B3

        // now close the loop: A1 reads C7
        var cycle = g.SetDependencies(R("A1"), new[] { R("C7") });

        Assert.NotNull(cycle);
        // path repeats the entry cell at the end so it reads as a loop
        Assert.Equal("A1 -> B3 -> C7 -> A1", string.Join(" -> ", cycle!));
    }

    // Detection happens at INSERTION time and the edge is rolled back,
    // so the graph is never left cyclic. Without rollback, every later
    // traversal would be walking a broken structure.
    [Fact]
    public void RejectedCycle_LeavesGraphUnchanged()
    {
        var g = new DependencyGraph();
        g.SetDependencies(R("B3"), new[] { R("A1") });
        g.SetDependencies(R("A1"), new[] { R("B3") });   // rejected

        Assert.False(g.HasCycle());                       // still acyclic
        Assert.Empty(g.PrecedentsOf(R("A1")));            // edge rolled back
    }

    // A diamond is NOT a cycle: D reads B and C, both read A.
    // A common bug is to flag any re-visited node as a cycle. It is only
    // a cycle if the node is on the CURRENT path, not merely seen before.
    [Fact]
    public void DiamondShape_IsNotACycle()
    {
        var g = new DependencyGraph();
        g.SetDependencies(R("B1"), new[] { R("A1") });
        g.SetDependencies(R("C1"), new[] { R("A1") });
        var cycle = g.SetDependencies(R("D1"), new[] { R("B1"), R("C1") });

        Assert.Null(cycle);
        Assert.False(g.HasCycle());
    }

    // ---------- affected cells ----------

    // A1 -> B1 -> C1. Editing A1 must recompute BOTH — the brief's
    // "directly or indirectly" requirement.
    [Fact]
    public void GetAffectedCells_FollowsChainsTransitively()
    {
        var g = new DependencyGraph();
        g.SetDependencies(R("B1"), new[] { R("A1") });
        g.SetDependencies(R("C1"), new[] { R("B1") });

        var affected = g.GetAffectedCells(R("A1"));

        Assert.Contains(R("B1"), affected);
        Assert.Contains(R("C1"), affected);          // indirect
        Assert.DoesNotContain(R("A1"), affected);    // never itself
    }

    // A cell nobody reads affects nothing. Empty, not a throw.
    [Fact]
    public void GetAffectedCells_UnknownCell_ReturnsEmpty()
        => Assert.Empty(new DependencyGraph().GetAffectedCells(R("Z99")));

    // Diamond: A1 feeds B1 and C1, both feed D1. D1 must appear ONCE —
    // without the seen-set it would be recomputed twice.
    [Fact]
    public void GetAffectedCells_DiamondVisitsEachCellOnce()
    {
        var g = new DependencyGraph();
        g.SetDependencies(R("B1"), new[] { R("A1") });
        g.SetDependencies(R("C1"), new[] { R("A1") });
        g.SetDependencies(R("D1"), new[] { R("B1"), R("C1") });

        Assert.Equal(3, g.GetAffectedCells(R("A1")).Count);
    }

    // ---------- topological sort ----------

    // The whole point: nothing computed before what it reads.
    [Fact]
    public void TopologicalSort_PutsPrecedentsFirst()
    {
        var g = new DependencyGraph();
        g.SetDependencies(R("B1"), new[] { R("A1") });
        g.SetDependencies(R("C1"), new[] { R("B1") });

        var order = g.TopologicalSort(new[] { R("C1"), R("A1"), R("B1") });

        Assert.True(order.IndexOf(R("A1")) < order.IndexOf(R("B1")));
        Assert.True(order.IndexOf(R("B1")) < order.IndexOf(R("C1")));
    }

    // B1 and C1 may come in either order — the test asserts the CONSTRAINT,
    // not one permutation, because both orderings are correct.
    [Fact]
    public void TopologicalSort_DiamondRespectsBothBranches()
    {
        var g = new DependencyGraph();
        g.SetDependencies(R("B1"), new[] { R("A1") });
        g.SetDependencies(R("C1"), new[] { R("A1") });
        g.SetDependencies(R("D1"), new[] { R("B1"), R("C1") });

        var order = g.TopologicalSort(new[] { R("D1"), R("C1"), R("B1"), R("A1") });

        Assert.True(order.IndexOf(R("A1")) < order.IndexOf(R("B1")));
        Assert.True(order.IndexOf(R("A1")) < order.IndexOf(R("C1")));
        Assert.True(order.IndexOf(R("B1")) < order.IndexOf(R("D1")));
        Assert.True(order.IndexOf(R("C1")) < order.IndexOf(R("D1")));
    }

    // Unrelated cells still all come out. Nothing is dropped.
    [Fact]
    public void TopologicalSort_UnrelatedCellsAllAppear()
    {
        var g = new DependencyGraph();
        Assert.Equal(3, g.TopologicalSort(new[] { R("A1"), R("B5"), R("Z9") }).Count);
    }
}