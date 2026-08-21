using Antlr4.Runtime;
using CalcEngine.Core.Dependencies;
using CalcEngine.Core.Parsing;
using CalcEngine.Core.Model;
using CalcEngine.Core.Expressions;
using CalcEngine.Core.Generated;

namespace CalcEngine.Tests.Dependencies;

public class DependencyVisitorTests
{
    // Reuses the same parse-to-tree helper as ExpressionTreeBuilderTests.
    private static IExpression Build(string formulaText)
    {
        var lexer = new FormulaLexer(new AntlrInputStream(formulaText));
        var tokens = new CommonTokenStream(lexer);
        var parser = new FormulaParser(tokens);
        var tree = parser.formula();
        return new ExpressionTreeBuilder().Visit(tree);
    }

    // ── No dependencies ──

    [Fact]
    public void NumberLiteral_HasNoDependencies()
    {
        var deps = DependencyVisitor.GetDependencies(Build("=42"));
        Assert.Empty(deps);
    }

    [Fact]
    public void TextLiteral_HasNoDependencies()
    {
        var deps = DependencyVisitor.GetDependencies(Build("=\"hello\""));
        Assert.Empty(deps);
    }

    // ── Single cell ──

    [Fact]
    public void SingleCellRef_ReturnsOneDependency()
    {
        var deps = DependencyVisitor.GetDependencies(Build("=B2"));
        Assert.Single(deps);
        Assert.Equal(new CellRef(2, 2), deps[0]); // B2
    }

    // ── Binary and unary composition ──

    [Fact]
    public void BinaryExpression_CollectsBothOperands()
    {
        var deps = DependencyVisitor.GetDependencies(Build("=A1+B2"));
        Assert.Equal(2, deps.Count);
        Assert.Contains(new CellRef(1, 1), deps); // A1
        Assert.Contains(new CellRef(2, 2), deps); // B2
    }

    [Fact]
    public void UnaryExpression_CollectsOperand()
    {
        var deps = DependencyVisitor.GetDependencies(Build("=-A1"));
        Assert.Single(deps);
        Assert.Equal(new CellRef(1, 1), deps[0]);
    }

    [Fact]
    public void NestedExpression_CollectsAllLeafRefs()
    {
        // (A1+B1)*C1 → three distinct dependencies
        var deps = DependencyVisitor.GetDependencies(Build("=(A1+B1)*C1"));
        Assert.Equal(3, deps.Count);
        Assert.Contains(new CellRef(1, 1), deps); // A1
        Assert.Contains(new CellRef(1, 2), deps); // B1
        Assert.Contains(new CellRef(1, 3), deps); // C1
    }

    [Fact]
    public void SameCellReferencedTwice_ReturnsTwoEntries()
    {
        // Not deduplicated — DependencyGraph.SetDependencies handles that
        // via its HashSet edge storage. This visitor just collects reads.
        var deps = DependencyVisitor.GetDependencies(Build("=A1+A1"));
        Assert.Equal(2, deps.Count);
    }

    // ── Ranges expand to every cell ──

    [Fact]
    public void Range_ExpandsToEveryCell()
    {
        // B2:B4 → B2, B3, B4 (3 cells)
        var deps = DependencyVisitor.GetDependencies(Build("=SUM(B2:B4)"));
        Assert.Equal(3, deps.Count);
        Assert.Contains(new CellRef(2, 2), deps);
        Assert.Contains(new CellRef(3, 2), deps);
        Assert.Contains(new CellRef(4, 2), deps);
    }

    [Fact]
    public void LookupOverFortyFourCells_RegistersFortyFourEdges()
    {
        // Design Portfolio Section 4.8: "A LOOKUP over B2:B45
        // registers forty-four edges, not one."
        var deps = DependencyVisitor.GetDependencies(Build("=LOOKUP(B2:B45)"));
        Assert.Equal(44, deps.Count);
    }

    // ── Function arguments ──

    [Fact]
    public void FunctionWithMixedArgs_CollectsFromAllArgs()
    {
        // SUM(A1, B1:B2, C1) → A1, B1, B2, C1 = 4 total
        var deps = DependencyVisitor.GetDependencies(Build("=SUM(A1,B1:B2,C1)"));
        Assert.Equal(4, deps.Count);
        Assert.Contains(new CellRef(1, 1), deps); // A1
        Assert.Contains(new CellRef(1, 2), deps); // B1
        Assert.Contains(new CellRef(2, 2), deps); // B2
        Assert.Contains(new CellRef(1, 3), deps); // C1
    }

    [Fact]
    public void FunctionWithNoArgs_ReturnsEmpty()
    {
        var deps = DependencyVisitor.GetDependencies(Build("=TOTAL()"));
        Assert.Empty(deps);
    }

    [Fact]
    public void IfWithConditionAndBranches_CollectsFromAllThree()
    {
        // IF(A1>5, B1, C1) — dependency extraction sees ALL branches,
        // unlike evaluation which only touches the taken branch.
        var deps = DependencyVisitor.GetDependencies(Build("=IF(A1>5,B1,C1)"));
        Assert.Equal(3, deps.Count);
        Assert.Contains(new CellRef(1, 1), deps); // A1
        Assert.Contains(new CellRef(1, 2), deps); // B1
        Assert.Contains(new CellRef(1, 3), deps); // C1
    }

    // ── Worked example from the portfolio ──

    [Fact]
    public void SumTimesScalar_MatchesPortfolioWorkedExample()
    {
        // Section 3.6: =SUM(B2:B45)*0.3
        // Pass 1 (DependencyVisitor) returns: [B2, B3, B4, ..., B45]
        var deps = DependencyVisitor.GetDependencies(Build("=SUM(B2:B45)*0.3"));
        Assert.Equal(44, deps.Count); // B2 through B45 inclusive
    }
}