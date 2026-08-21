using System.Linq;
using CalcEngine.Core.Model;

namespace CalcEngine.Core.Expressions;

/// <summary>
/// Clones an expression tree, offsetting every cell reference and
/// range reference by (deltaRow, deltaColumn) — Excel's behaviour when
/// a formula is *moved* (as opposed to copied) to a new location
/// (Group C feature: Sorting &amp; Filtering, RangeSorter Option B from
/// the project plan). Every reference in the formula shifts by the
/// same delta the cell itself moved by, whether that reference points
/// inside or outside the sorted range — that is what "move" means, and
/// it is what makes =A1+C10 on a row that moves from row 5 to row 10
/// become =A6+C15, not something that depends on whether A1 or C10
/// happens to be in the range being sorted.
///
/// The base grammar has no absolute ($A$1-style) references, so every
/// reference is relative and translation is uniform: there is nothing
/// to pin in place.
/// </summary>
public sealed class ReferenceTranslationVisitor : IExpressionVisitor<IExpression>
{
    private readonly int _deltaRow;
    private readonly int _deltaColumn;

    public ReferenceTranslationVisitor(int deltaRow, int deltaColumn)
    {
        _deltaRow = deltaRow;
        _deltaColumn = deltaColumn;
    }

    /// <summary>Translates expr by (deltaRow, deltaColumn). A zero delta still clones the tree.</summary>
    public static IExpression Translate(IExpression expr, int deltaRow, int deltaColumn) =>
        expr.Accept(new ReferenceTranslationVisitor(deltaRow, deltaColumn));

    public IExpression VisitNumber(NumberExpression expr) => expr;
    public IExpression VisitText(TextExpression expr) => expr;
    public IExpression VisitBoolean(BooleanExpression expr) => expr;

    public IExpression VisitCellRef(CellRefExpression expr) => new CellRefExpression(Shift(expr.Ref));

    public IExpression VisitRange(RangeExpression expr) =>
        new RangeExpression(new CellRange(Shift(expr.Range.TopLeft), Shift(expr.Range.BottomRight)));

    public IExpression VisitUnary(UnaryExpression expr) =>
        new UnaryExpression(expr.Op, expr.Operand.Accept(this));

    public IExpression VisitBinary(BinaryExpression expr) =>
        new BinaryExpression(expr.Left.Accept(this), expr.Op, expr.Right.Accept(this));

    public IExpression VisitFunction(FunctionExpression expr) =>
        new FunctionExpression(expr.Name, expr.Args.Select(a => a.Accept(this)).ToList());

    private CellRef Shift(CellRef r) => new(r.Row + _deltaRow, r.Column + _deltaColumn);
}
