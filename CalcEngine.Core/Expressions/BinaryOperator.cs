namespace CalcEngine.Core.Expressions;

/// <summary>
/// Operators for BinaryExpression. Maps to tokens in the grammar's
/// comparison, addition, and multiply rules.
/// </summary>
public enum BinaryOperator
{
    /// <summary>Addition, written +.</summary>
    Add,

    /// <summary>Subtraction, written -.</summary>
    Subtract,

    /// <summary>Multiplication, written *.</summary>
    Multiply,

    /// <summary>Division, written /. Dividing by zero gives #DIV/0!.</summary>
    Divide,
    Power,

    /// <summary>Equality, written =.</summary>
    Equal,

    /// <summary>Inequality, written as a less-than sign followed by a greater-than sign.</summary>
    NotEqual,

    /// <summary>Less than.</summary>
    LessThan,

    /// <summary>Less than or equal to.</summary>
    LessOrEqual,

    /// <summary>Greater than.</summary>
    GreaterThan,

    /// <summary>Greater than or equal to.</summary>
    GreaterOrEqual
}