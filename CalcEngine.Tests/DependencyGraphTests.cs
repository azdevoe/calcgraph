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
        Assert.Equal("A1 -> C7 -> A1", string.Join(" -> ", cycle!));
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
}