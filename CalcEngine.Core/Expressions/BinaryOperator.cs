namespace CalcEngine.Core.Expressions;

/// <summary>
/// Operators for BinaryExpression. Maps to tokens in the grammar's
/// comparison, addition, and multiply rules.
/// </summary>
public enum BinaryOperator
{
    // Arithmetic
    Add,
    Subtract,
    Multiply,
    Divide,

    // Comparison
    Equal,
    NotEqual,
    LessThan,
    LessOrEqual,
    GreaterThan,
    GreaterOrEqual
}