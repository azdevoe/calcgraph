using CalcEngine.Core.Expressions;

namespace CalcEngine.Core.Parsing;

/// <summary>
/// The return type of FormulaParser.Parse. A successful parse and a
/// syntax failure both come back as data — same shape as CellChangeSet
/// for the same reason: the client is never required to catch anything.
/// </summary>
public sealed class FormulaParseResult
{
    /// <summary>True iff the input parsed without error.</summary>
    public bool Success { get; }

    /// <summary>The built expression tree. Null when Success is false.</summary>
    public IExpression? Tree { get; }

    /// <summary>
    /// True if the input began with '=' (a formula), false if it was a
    /// bare literal (number, boolean, or quoted string). Meaningless
    /// (and false) when Success is false.
    /// </summary>
    public bool IsFormula { get; }

    /// <summary>Syntax errors encountered, in order. Empty when Success is true.</summary>
    public IReadOnlyList<string> Errors { get; }

    private FormulaParseResult(bool success, IExpression? tree, bool isFormula, IReadOnlyList<string> errors)
    {
        Success = success;
        Tree = tree;
        IsFormula = isFormula;
        Errors = errors;
    }

    /// <summary>Creates a result describing a successful parse.</summary>
    /// <param name="tree">The expression tree that was built.</param>
    /// <param name="isFormula">
    /// true if the input began with "="; false if it was a plain literal.
    /// </param>
    /// <returns>A result whose Success is true and whose Errors is empty.</returns>
    public static FormulaParseResult Ok(IExpression tree, bool isFormula) =>
        new(success: true, tree, isFormula, Array.Empty<string>());

    /// <summary>Creates a result describing a parse that failed.</summary>
    /// <param name="errors">
    /// The problems found, in the order they were met, each naming the
    /// position it occurred at.
    /// </param>
    /// <returns>A result whose Success is false and whose Tree is null.</returns>
    public static FormulaParseResult Failure(IReadOnlyList<string> errors) =>
        new(success: false, tree: null, isFormula: false, errors);
}