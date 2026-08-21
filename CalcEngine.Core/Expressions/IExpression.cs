using CalcEngine.Core.Model;

namespace CalcEngine.Core.Expressions;

/// <summary>
/// A node in the expression tree — Composite structure with an
/// Interpreter evaluation method.
///
/// Seven concrete types implement this interface:
///   Leaves:   NumberExpression, TextExpression, BooleanExpression,
///             CellRefExpression, RangeExpression
///   Branches: UnaryExpression, BinaryExpression, FunctionExpression
/// </summary>
public interface IExpression
{
    /// <summary>Works out what this expression is worth.</summary>
    /// <param name="context">
    /// Supplies the cell values the expression reads. Pass the workbook the
    /// formula belongs to.
    /// </param>
    /// <returns>
    /// The value of the expression. A problem such as a type mismatch or a
    /// division by zero comes back as an error value; this method does not
    /// throw for bad data.
    /// </returns>
    CellValue Evaluate(IEvalContext context);

    /// <summary>
    /// Calls the visitor method that matches this node's type.
    /// </summary>
    /// <typeparam name="T">The type of result the visitor produces.</typeparam>
    /// <param name="visitor">The visitor to call back into.</param>
    /// <returns>Whatever the visitor returns for this node.</returns>
    T Accept<T>(IExpressionVisitor<T> visitor);
}