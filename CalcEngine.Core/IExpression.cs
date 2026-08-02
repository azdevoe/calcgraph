namespace CalcEngine.Core;

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
    /// <summary>
    /// Computes the value of this expression under context.
    /// Never throws; type errors and missing references produce error values.
    /// </summary>
    CellValue Evaluate(IEvalContext context);

    /// <summary>
    /// Dispatches to the visitor method for this node's concrete type.
    /// </summary>
    T Accept<T>(IExpressionVisitor<T> visitor);
}