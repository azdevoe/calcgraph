using CalcEngine.Core.Expressions;
using CalcEngine.Core.Model;

namespace CalcEngine.Core.Dependencies;

/// <summary>
/// Pass 1 of the two-pass evaluation model (Design Portfolio Section 4.4).
/// Walks the expression tree and collects every cell it reads, so the
/// dependency graph can be updated and checked for cycles before
/// anything is evaluated.
///
/// A range expands into every cell it covers — a LOOKUP over B2:B45
/// registers forty-four edges, not one (Section 4.8).
/// </summary>
public sealed class DependencyVisitor : IExpressionVisitor<IReadOnlyList<CellRef>>
{
    /// <summary>Convenience entry point: collects all dependencies of a tree.</summary>
    public static IReadOnlyList<CellRef> GetDependencies(IExpression expr)
        => expr.Accept(new DependencyVisitor());

    private static readonly IReadOnlyList<CellRef> None = Array.Empty<CellRef>();

    public IReadOnlyList<CellRef> VisitNumber(NumberExpression expr) => None;
    public IReadOnlyList<CellRef> VisitText(TextExpression expr) => None;
    public IReadOnlyList<CellRef> VisitBoolean(BooleanExpression expr) => None;

    public IReadOnlyList<CellRef> VisitCellRef(CellRefExpression expr)
        => new[] { expr.Ref };

    public IReadOnlyList<CellRef> VisitRange(RangeExpression expr)
        => expr.Range.GetCells().ToList();

    public IReadOnlyList<CellRef> VisitUnary(UnaryExpression expr)
        => expr.Operand.Accept(this);

    public IReadOnlyList<CellRef> VisitBinary(BinaryExpression expr)
    {
        var result = new List<CellRef>();
        result.AddRange(expr.Left.Accept(this));
        result.AddRange(expr.Right.Accept(this));
        return result;
    }

    public IReadOnlyList<CellRef> VisitFunction(FunctionExpression expr)
    {
        var result = new List<CellRef>();
        foreach (var arg in expr.Args)
            result.AddRange(arg.Accept(this));
        return result;
    }
}