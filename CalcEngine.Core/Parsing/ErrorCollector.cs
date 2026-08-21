using System.IO;
using Antlr4.Runtime;

namespace CalcEngine.Core.Parsing;

/// <summary>
/// An ANTLR error listener that records syntax errors instead of
/// ANTLR's default behaviour of printing them to the console. Attach
/// one instance to both the lexer and the parser (via
/// RemoveErrorListeners + AddErrorListener) so malformed input is
/// captured as data rather than escaping as an exception or a stderr
/// message the client never sees.
///
/// Implements two listener interfaces because the lexer and parser
/// report through different overloads: the lexer's offending symbol is
/// an int (a raw character code — no tokens exist yet), the parser's is
/// an IToken.
/// </summary>
public sealed class ErrorCollector : IAntlrErrorListener<int>, IAntlrErrorListener<IToken>
{
    private readonly List<string> _errors = new();

    /// <summary>Every syntax error recorded so far, in the order they occurred.</summary>
    public IReadOnlyList<string> Errors => _errors;

    /// <summary>True iff at least one syntax error has been recorded.</summary>
    public bool HasErrors => _errors.Count > 0;

    /// <summary>
    /// Records a character in the input that the formula language does not
    /// recognise at all, such as a stray symbol.
    /// </summary>
    /// <param name="output">Where errors would have been printed. Not used.</param>
    /// <param name="recognizer">The reader that met the problem.</param>
    /// <param name="offendingSymbol">The character that could not be read.</param>
    /// <param name="line">The line the problem occurred on, counting from 1.</param>
    /// <param name="charPositionInLine">The position within that line, counting from 0.</param>
    /// <param name="msg">A description of the problem.</param>
    /// <param name="e">More detail about the problem, if any is available.</param>
    public void SyntaxError(
        TextWriter output, IRecognizer recognizer, int offendingSymbol,
        int line, int charPositionInLine, string msg, RecognitionException e)
        => _errors.Add(Format(line, charPositionInLine, msg));

    /// <summary>
    /// Records input that is made of things the formula language knows, but
    /// arranged in a way it does not allow, such as a missing closing bracket.
    /// </summary>
    /// <param name="output">Where errors would have been printed. Not used.</param>
    /// <param name="recognizer">The reader that met the problem.</param>
    /// <param name="offendingSymbol">The piece of input that was not expected here.</param>
    /// <param name="line">The line the problem occurred on, counting from 1.</param>
    /// <param name="charPositionInLine">The position within that line, counting from 0.</param>
    /// <param name="msg">A description of the problem.</param>
    /// <param name="e">More detail about the problem, if any is available.</param>
    public void SyntaxError(
        TextWriter output, IRecognizer recognizer, IToken offendingSymbol,
        int line, int charPositionInLine, string msg, RecognitionException e)
        => _errors.Add(Format(line, charPositionInLine, msg));

    private static string Format(int line, int charPositionInLine, string msg) =>
        $"Line {line}:{charPositionInLine} {msg}";
}