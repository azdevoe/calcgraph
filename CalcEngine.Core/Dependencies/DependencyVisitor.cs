using CalcEngine.Core.Expressions;
using CalcEngine.Core.Model;

namespace CalcEngine.Core.Dependencies;

/// <summary>
/// Walks an expression tree and collects every cell it reads, so that
/// dependencies can be recorded and checked for loops before anything is
/// worked out.
///
/// A range counts as every cell it covers, so a LOOKUP over B2:B45 is
/// treated as reading forty-four separate cells rather than one thing.
/// </summary>
public sealed class DependencyVisitor : IExpressionVisitor<IReadOnlyList<CellRef>>
{
    /// <summary>Collects every cell an expression reads.</summary>
    /// <param name="expr">The expression to examine.</param>
    /// <returns>
    /// The cells it reads. A cell read more than once in the same formula
    /// appears more than once, and an expression that reads nothing gives an
    /// empty list.
    /// </returns>
    public static IReadOnlyList<CellRef> GetDependencies(IExpression expr)
        => expr.Accept(new DependencyVisitor());

    private static readonly IReadOnlyList<CellRef> None = Array.Empty<CellRef>();

    /// <summary>Collects the cells a numeric literal reads.</summary>
    /// <param name="expr">The node being visited.</param>
    /// <returns>An empty list, as a literal reads nothing.</returns>
    public IReadOnlyList<CellRef> VisitNumber(NumberExpression expr) => None;

    /// <summary>Collects the cells a text literal reads.</summary>
    /// <param name="expr">The node being visited.</param>
    /// <returns>An empty list, as a literal reads nothing.</returns>
    public IReadOnlyList<CellRef> VisitText(TextExpression expr) => None;

    /// <summary>Collects the cells a TRUE or FALSE literal reads.</summary>
    /// <param name="expr">The node being visited.</param>
    /// <returns>An empty list, as a literal reads nothing.</returns>
    public IReadOnlyList<CellRef> VisitBoolean(BooleanExpression expr) => None;

    /// <summary>Collects the cell a reference reads.</summary>
    /// <param name="expr">The node being visited.</param>
    /// <returns>A list holding the one address it points at.</returns>
    public IReadOnlyList<CellRef> VisitCellRef(CellRefExpression expr)
        => new[] { expr.Ref };

    /// <summary>Collects the cells a range reference reads.</summary>
    /// <param name="expr">The node being visited.</param>
    /// <returns>Every address the range covers, in row-major order.</returns>
    public IReadOnlyList<CellRef> VisitRange(RangeExpression expr)
        => expr.Range.GetCells().ToList();

    /// <summary>Collects the cells read inside a signed expression.</summary>
    /// <param name="expr">The node being visited.</param>
    /// <returns>Whatever its operand reads.</returns>
    public IReadOnlyList<CellRef> VisitUnary(UnaryExpression expr)
        => expr.Operand.Accept(this);

    /// <summary>Collects the cells read on both sides of an operator.</summary>
    /// <param name="expr">The node being visited.</param>
    /// <returns>What the left operand reads, followed by what the right one reads.</returns>
    public IReadOnlyList<CellRef> VisitBinary(BinaryExpression expr)
    {
        var result = new List<CellRef>();
        result.AddRange(expr.Left.Accept(this));
        result.AddRange(expr.Right.Accept(this));
        return result;
    }

    /// <summary>Collects the cells read by the arguments of a function call.</summary>
    /// <param name="expr">The node being visited.</param>
    /// <returns>
    /// What every argument reads, in the order the arguments were written.
    /// </returns>
    public IReadOnlyList<CellRef> VisitFunction(FunctionExpression expr)
    {
        var result = new List<CellRef>();
        foreach (var arg in expr.Args)
            result.AddRange(arg.Accept(this));
        return result;
    }
}