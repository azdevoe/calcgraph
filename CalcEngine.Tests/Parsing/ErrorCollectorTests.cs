using Antlr4.Runtime;
using CalcEngine.Core.Parsing;
using Xunit;

namespace CalcEngine.Tests.Parsing;

/// <summary>
/// Tests for ErrorCollector — an IAntlrErrorListener that records syntax
/// errors instead of printing them to the console (ANTLR's default) or
/// letting them propagate. This is what lets FormulaParser return a
/// described, located, recoverable result instead of throwing
/// (Design_Portfolio 1: "malformed input is normal input").
///
/// ErrorCollector implements two listener interfaces because the lexer
/// and parser report errors through different overloads: the lexer's
/// offending symbol is an int (a character code, before tokens exist),
/// the parser's is an IToken.
/// </summary>
public class ErrorCollectorTests
{
    [Fact]
    public void NewCollector_HasErrorsIsFalse()
    {
        var collector = new ErrorCollector();
        Assert.False(collector.HasErrors);
    }

    [Fact]
    public void NewCollector_ErrorsIsEmpty()
    {
        var collector = new ErrorCollector();
        Assert.Empty(collector.Errors);
    }

    [Fact]
    public void SyntaxError_LexerOverload_RecordsError()
    {
        var collector = new ErrorCollector();
        IAntlrErrorListener<int> listener = collector;

        listener.SyntaxError(null!, null!, 0, 3, 5, "token recognition error", null!);

        Assert.True(collector.HasErrors);
        Assert.Single(collector.Errors);
    }

    [Fact]
    public void SyntaxError_ParserOverload_RecordsError()
    {
        var collector = new ErrorCollector();
        IAntlrErrorListener<IToken> listener = collector;

        listener.SyntaxError(null!, null!, null!, 1, 2, "missing ')'", null!);

        Assert.True(collector.HasErrors);
        Assert.Single(collector.Errors);
    }

    [Fact]
    public void SyntaxError_MessageContainsLineColumnAndText()
    {
        var collector = new ErrorCollector();
        IAntlrErrorListener<int> listener = collector;

        listener.SyntaxError(null!, null!, 0, 7, 4, "unexpected token", null!);

        var message = collector.Errors[0];
        Assert.Contains("7", message);
        Assert.Contains("4", message);
        Assert.Contains("unexpected token", message);
    }

    [Fact]
    public void SyntaxError_MultipleCalls_AccumulateInOrder()
    {
        var collector = new ErrorCollector();
        IAntlrErrorListener<int> listener = collector;

        listener.SyntaxError(null!, null!, 0, 1, 0, "first error", null!);
        listener.SyntaxError(null!, null!, 0, 2, 0, "second error", null!);

        Assert.Equal(2, collector.Errors.Count);
        Assert.Contains("first error", collector.Errors[0]);
        Assert.Contains("second error", collector.Errors[1]);
    }

    [Fact]
    public void SyntaxError_BothOverloads_AccumulateIntoSameList()
    {
        var collector = new ErrorCollector();
        ((IAntlrErrorListener<int>)collector).SyntaxError(null!, null!, 0, 1, 0, "lexer error", null!);
        ((IAntlrErrorListener<IToken>)collector).SyntaxError(null!, null!, null!, 2, 0, "parser error", null!);

        Assert.Equal(2, collector.Errors.Count);
    }
}