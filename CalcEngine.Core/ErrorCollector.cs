using System.IO;
using Antlr4.Runtime;

namespace CalcEngine.Core;

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

    /// <summary>Lexer-level errors: unrecognized characters, e.g. lowercase letters.</summary>
    public void SyntaxError(
        TextWriter output, IRecognizer recognizer, int offendingSymbol,
        int line, int charPositionInLine, string msg, RecognitionException e)
        => _errors.Add(Format(line, charPositionInLine, msg));

    /// <summary>Parser-level errors: unexpected tokens, missing closing brackets, etc.</summary>
    public void SyntaxError(
        TextWriter output, IRecognizer recognizer, IToken offendingSymbol,
        int line, int charPositionInLine, string msg, RecognitionException e)
        => _errors.Add(Format(line, charPositionInLine, msg));

    private static string Format(int line, int charPositionInLine, string msg) =>
        $"Line {line}:{charPositionInLine} {msg}";
}