using CalcEngine.Core;
using Xunit;
using System.Linq;

namespace CalcEngine.Tests;

public class DependencyGraphTests
{
    private static CellRef R(string s) => CellRef.Parse(s);

    [Fact]
    public void EmptyGraph_HasNoCycle()
        => Assert.False(new DependencyGraph().HasCycle());

    [Fact]
    public void SetDependencies_RegistersEdge_AndReportsNoCycle()
    {
        var g = new DependencyGraph();
        var cycle = g.SetDependencies(R("B2"), new[] { R("A1") });
        Assert.Null(cycle);
    }

    [Fact]
    public void SetDependencies_ReplacesOldEdges_RatherThanAdding()
    {
        var g = new DependencyGraph();
        g.SetDependencies(R("B2"), new[] { R("A1") });
        g.SetDependencies(R("B2"), new[] { R("C3") });

        Assert.Equal(new[] { R("C3") }, g.PrecedentsOf(R("B2")));
        Assert.Empty(g.DependentsOf(R("A1")));
    }

    [Fact]
    public void SelfReference_IsRejectedAsACycle()
    {
        var g = new DependencyGraph();
        var cycle = g.SetDependencies(R("A1"), new[] { R("A1") });
        Assert.NotNull(cycle);
    }

    [Fact]
    public void Cycle_IsReportedWithTheExactPath()
    {
        var g = new DependencyGraph();
        g.SetDependencies(R("B3"), new[] { R("A1") });
        g.SetDependencies(R("C7"), new[] { R("B3") });

        var cycle = g.SetDependencies(R("A1"), new[] { R("C7") });

        Assert.NotNull(cycle);
        Assert.Equal("A1 -> B3 -> C7 -> A1", string.Join(" -> ", cycle!));
    }

    [Fact]
    public void RejectedCycle_LeavesGraphUnchanged()
    {
        var g = new DependencyGraph();
        g.SetDependencies(R("B3"), new[] { R("A1") });
        g.SetDependencies(R("A1"), new[] { R("B3") });

        Assert.False(g.HasCycle());
        Assert.Empty(g.PrecedentsOf(R("A1")));
    }

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

    [Fact]
    public void GetAffectedCells_FollowsChainsTransitively()
    {
        var g = new DependencyGraph();
        g.SetDependencies(R("B1"), new[] { R("A1") });
        g.SetDependencies(R("C1"), new[] { R("B1") });

        var affected = g.GetAffectedCells(R("A1"));

        Assert.Contains(R("B1"), affected);
        Assert.Contains(R("C1"), affected);
        Assert.DoesNotContain(R("A1"), affected);
    }

    [Fact]
    public void GetAffectedCells_UnknownCell_ReturnsEmpty()
        => Assert.Empty(new DependencyGraph().GetAffectedCells(R("Z99")));

    [Fact]
    public void GetAffectedCells_DiamondVisitsEachCellOnce()
    {
        var g = new DependencyGraph();
        g.SetDependencies(R("B1"), new[] { R("A1") });
        g.SetDependencies(R("C1"), new[] { R("A1") });
        g.SetDependencies(R("D1"), new[] { R("B1"), R("C1") });

        Assert.Equal(3, g.GetAffectedCells(R("A1")).Count);
    }

    [Fact]
    public void TopologicalSort_PutsPrecedentsFirst()
    {
        var g = new DependencyGraph();
        g.SetDependencies(R("B1"), new[] { R("A1") });
        g.SetDependencies(R("C1"), new[] { R("B1") });

        var order = g.TopologicalSort(new[] { R("C1"), R("A1"), R("B1") }).ToList();

        Assert.True(order.IndexOf(R("A1")) < order.IndexOf(R("B1")));
        Assert.True(order.IndexOf(R("B1")) < order.IndexOf(R("C1")));
    }

    [Fact]
    public void TopologicalSort_DiamondRespectsBothBranches()
    {
        var g = new DependencyGraph();
        g.SetDependencies(R("B1"), new[] { R("A1") });
        g.SetDependencies(R("C1"), new[] { R("A1") });
        g.SetDependencies(R("D1"), new[] { R("B1"), R("C1") });

        var order = g.TopologicalSort(new[] { R("D1"), R("C1"), R("B1"), R("A1") }).ToList();

        Assert.True(order.IndexOf(R("A1")) < order.IndexOf(R("B1")));
        Assert.True(order.IndexOf(R("A1")) < order.IndexOf(R("C1")));
        Assert.True(order.IndexOf(R("B1")) < order.IndexOf(R("D1")));
        Assert.True(order.IndexOf(R("C1")) < order.IndexOf(R("D1")));
    }

    [Fact]
    public void TopologicalSort_UnrelatedCellsAllAppear()
    {
        var g = new DependencyGraph();
        Assert.Equal(3, g.TopologicalSort(new[] { R("A1"), R("B5"), R("Z9") }).Count);
    }
}