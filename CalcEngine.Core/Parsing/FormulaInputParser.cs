using Antlr4.Runtime;
using AntlrLexer = CalcEngine.Core.Generated.FormulaLexer;
using AntlrParser = CalcEngine.Core.Generated.FormulaParser;
using CalcEngine.Core.Expressions;

namespace CalcEngine.Core.Parsing;

/// <summary>
/// Entry point for turning raw cell input into an expression tree.
/// Wraps the ANTLR-generated lexer and parser (Grammar/Formula.g4),
/// wiring an ErrorCollector into both so a malformed formula returns a
/// FormulaParseResult.Failure instead of throwing or printing to the
/// console. Malformed input is normal input: users mistype formulas.
///
/// Named FormulaInputParser, not FormulaParser: the ANTLR-generated
/// parser is already CalcEngine.Core.Generated.FormulaParser, and C#
/// resolves a bare type name to one declared in the CURRENT namespace
/// before it looks at any `using` import. A second FormulaParser here
/// in CalcEngine.Core would silently shadow the generated one for
/// every other file in this namespace — including ExpressionTreeBuilder.cs,
/// which references FormulaParser.FormulaEntryContext and friends
/// unqualified. Aliasing inside this file alone does not protect that
/// file, so the two types need genuinely different names.
/// </summary>
public sealed class FormulaInputParser
{
    /// <summary>Reads what a user typed into a cell.</summary>
    /// <param name="rawInput">
    /// The text to read. Text beginning with "=" is treated as a formula;
    /// anything else is treated as a plain number, TRUE or FALSE, or quoted
    /// text.
    /// </param>
    /// <returns>
    /// A result holding the expression that was built, or, if the text could
    /// not be read, the problems found with it. Mistyped input is reported
    /// this way rather than by throwing, including a reference that falls
    /// outside the sheet such as A0.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="rawInput"/> is null.</exception>
    public FormulaParseResult Parse(string rawInput)
    {
        ArgumentNullException.ThrowIfNull(rawInput);

        var errorCollector = new ErrorCollector();

        var lexer = new AntlrLexer(new AntlrInputStream(rawInput));
        lexer.RemoveErrorListeners();
        lexer.AddErrorListener(errorCollector);

        var tokens = new CommonTokenStream(lexer);
        var parser = new AntlrParser(tokens);
        parser.RemoveErrorListeners();
        parser.AddErrorListener(errorCollector);

        var tree = parser.formula();

        if (errorCollector.HasErrors)
            return FormulaParseResult.Failure(errorCollector.Errors);

        // The grammar's CELLREF/RANGE tokens accept any digit string as
        // a row (e.g. "A0"), so a syntactically valid parse tree can
        // still describe a cell outside the sheet (row/column < 1).
        // CellRef.Parse and CellRefExpression's constructor both guard
        // that invariant and throw — appropriate for misuse of the
        // internal API, but this is user-typed formula text, and
        // "malformed input is normal input" means it must come back as
        // a FormulaParseResult.Failure, not an exception that escapes
        // ApplyEdit to the client.
        IExpression expression;
        try
        {
            expression = new ExpressionTreeBuilder().Visit(tree);
        }
        catch (Exception ex) when (ex is FormatException or ArgumentException)
        {
            return FormulaParseResult.Failure(new[] { $"Invalid cell reference: {ex.Message}" });
        }

        bool isFormula = tree is AntlrParser.FormulaEntryContext;

        return FormulaParseResult.Ok(expression, isFormula);
    }
}