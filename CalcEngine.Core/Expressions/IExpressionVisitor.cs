namespace CalcEngine.Core.Expressions;

/// <summary>
/// Visitor over the IExpression hierarchy.
/// </summary>
public interface IExpressionVisitor<T>
{
    T VisitNumber(NumberExpression expr);
    T VisitText(TextExpression expr);
    T VisitBoolean(BooleanExpression expr);
    T VisitCellRef(CellRefExpression expr);
    T VisitRange(RangeExpression expr);
    T VisitUnary(UnaryExpression expr);
    T VisitBinary(BinaryExpression expr);
    T VisitFunction(FunctionExpression expr);
}