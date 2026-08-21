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

    /// <summary>Initializes a visitor that shifts references by a fixed amount.</summary>
    /// <param name="deltaRow">Rows to shift by. Negative moves up.</param>
    /// <param name="deltaColumn">Columns to shift by. Negative moves left.</param>
    public ReferenceTranslationVisitor(int deltaRow, int deltaColumn)
    {
        _deltaRow = deltaRow;
        _deltaColumn = deltaColumn;
    }

    /// <summary>
    /// Returns a copy of an expression with every reference in it shifted by
    /// the given amount.
    /// </summary>
    /// <param name="expr">The expression to shift.</param>
    /// <param name="deltaRow">Rows to shift by. Negative moves up.</param>
    /// <param name="deltaColumn">Columns to shift by. Negative moves left.</param>
    /// <returns>
    /// A new expression. The original is left untouched, and a shift of zero
    /// still gives back a fresh copy.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// The shift would take a reference off the sheet, above row 1 or left of
    /// column A.
    /// </exception>
    public static IExpression Translate(IExpression expr, int deltaRow, int deltaColumn) =>
        expr.Accept(new ReferenceTranslationVisitor(deltaRow, deltaColumn));

    /// <summary>Returns a numeric literal unchanged, as it holds no reference.</summary>
    /// <param name="expr">The node being visited.</param>
    /// <returns>The same node.</returns>
    public IExpression VisitNumber(NumberExpression expr) => expr;

    /// <summary>Returns a text literal unchanged, as it holds no reference.</summary>
    /// <param name="expr">The node being visited.</param>
    /// <returns>The same node.</returns>
    public IExpression VisitText(TextExpression expr) => expr;

    /// <summary>Returns a TRUE or FALSE literal unchanged, as it holds no reference.</summary>
    /// <param name="expr">The node being visited.</param>
    /// <returns>The same node.</returns>
    public IExpression VisitBoolean(BooleanExpression expr) => expr;

    /// <summary>Shifts a reference to a single cell.</summary>
    /// <param name="expr">The node being visited.</param>
    /// <returns>A reference to the shifted address.</returns>
    /// <exception cref="ArgumentException">The shift would take the reference off the sheet.</exception>
    public IExpression VisitCellRef(CellRefExpression expr) => new CellRefExpression(Shift(expr.Ref));

    /// <summary>Shifts both corners of a range reference.</summary>
    /// <param name="expr">The node being visited.</param>
    /// <returns>A reference to the shifted range.</returns>
    /// <exception cref="ArgumentException">The shift would take a corner off the sheet.</exception>
    public IExpression VisitRange(RangeExpression expr) =>
        new RangeExpression(new CellRange(Shift(expr.Range.TopLeft), Shift(expr.Range.BottomRight)));

    /// <summary>Shifts the references inside a signed expression.</summary>
    /// <param name="expr">The node being visited.</param>
    /// <returns>A copy of the node with its operand shifted.</returns>
    public IExpression VisitUnary(UnaryExpression expr) =>
        new UnaryExpression(expr.Op, expr.Operand.Accept(this));

    /// <summary>Shifts the references on both sides of an operator.</summary>
    /// <param name="expr">The node being visited.</param>
    /// <returns>A copy of the node with both operands shifted.</returns>
    public IExpression VisitBinary(BinaryExpression expr) =>
        new BinaryExpression(expr.Left.Accept(this), expr.Op, expr.Right.Accept(this));

    /// <summary>Shifts the references inside each argument of a function call.</summary>
    /// <param name="expr">The node being visited.</param>
    /// <returns>A copy of the call with every argument shifted.</returns>
    public IExpression VisitFunction(FunctionExpression expr) =>
        new FunctionExpression(expr.Name, expr.Args.Select(a => a.Accept(this)).ToList());

    private CellRef Shift(CellRef r) => new(r.Row + _deltaRow, r.Column + _deltaColumn);
}
