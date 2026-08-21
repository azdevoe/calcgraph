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

    public static FormulaParseResult Ok(IExpression tree, bool isFormula) =>
        new(success: true, tree, isFormula, Array.Empty<string>());

    public static FormulaParseResult Failure(IReadOnlyList<string> errors) =>
        new(success: false, tree: null, isFormula: false, errors);
}