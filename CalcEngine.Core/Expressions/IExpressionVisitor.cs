namespace CalcEngine.Core.Expressions;

/// <summary>
/// Visitor over the IExpression hierarchy. Implement this to walk a formula
/// tree and produce something from it, such as the list of cells it reads or
/// the text it was written as, without adding a method to every node type.
/// </summary>
/// <typeparam name="T">The type of result the visitor produces.</typeparam>
public interface IExpressionVisitor<T>
{
    /// <summary>Handles a numeric literal.</summary>
    /// <param name="expr">The node being visited.</param>
    /// <returns>The result for this node.</returns>
    T VisitNumber(NumberExpression expr);

    /// <summary>Handles a text literal.</summary>
    /// <param name="expr">The node being visited.</param>
    /// <returns>The result for this node.</returns>
    T VisitText(TextExpression expr);

    /// <summary>Handles a TRUE or FALSE literal.</summary>
    /// <param name="expr">The node being visited.</param>
    /// <returns>The result for this node.</returns>
    T VisitBoolean(BooleanExpression expr);

    /// <summary>Handles a reference to a single cell.</summary>
    /// <param name="expr">The node being visited.</param>
    /// <returns>The result for this node.</returns>
    T VisitCellRef(CellRefExpression expr);

    /// <summary>Handles a reference to a range of cells.</summary>
    /// <param name="expr">The node being visited.</param>
    /// <returns>The result for this node.</returns>
    T VisitRange(RangeExpression expr);

    /// <summary>Handles a sign applied to a single operand.</summary>
    /// <param name="expr">The node being visited.</param>
    /// <returns>The result for this node.</returns>
    T VisitUnary(UnaryExpression expr);

    /// <summary>Handles an operator applied to two operands.</summary>
    /// <param name="expr">The node being visited.</param>
    /// <returns>The result for this node.</returns>
    T VisitBinary(BinaryExpression expr);

    /// <summary>Handles a function call.</summary>
    /// <param name="expr">The node being visited.</param>
    /// <returns>The result for this node.</returns>
    T VisitFunction(FunctionExpression expr);
}