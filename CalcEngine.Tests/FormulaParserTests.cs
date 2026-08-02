using CalcEngine.Core;
using Xunit;

namespace CalcEngine.Tests;

/// <summary>
/// Tests for FormulaParser — wraps the ANTLR lexer/parser pipeline with
/// ErrorCollector wired in, so malformed input ("normal input" per
/// Design_Portfolio 1) returns a FormulaParseResult instead of throwing
/// or printing to the console.
///
/// These run the real generated lexer/parser (same pipeline as
/// ExpressionTreeBuilderTests' Build() helper), not mocks — a parser
/// wrapper is exactly the kind of thing that's cheap to get subtly
/// wrong by mocking and only catches real bugs when it touches the
/// real grammar.
/// </summary>
public class FormulaParserTests
{
    private readonly FormulaParser _parser = new();
    private readonly CellRef _a1 = CellRef.Parse("A1");
    private readonly CellRef _a2 = CellRef.Parse("A2");
    private readonly CellRef _a3 = CellRef.Parse("A3");

    // ── Formulas (leading '=') ────────────────────────────────────

    [Fact]
    public void Formula_Number_Succeeds()
    {
        var result = _parser.Parse("=42");
        Assert.True(result.Success);
        Assert.True(result.IsFormula);
        Assert.Equal(42.0, result.Tree!.Evaluate(new Workbook()).AsNumber());
    }

    [Fact]
    public void Formula_Addition_EvaluatesCorrectly()
    {
        var result = _parser.Parse("=2+3");
        Assert.True(result.Success);
        Assert.Equal(5.0, result.Tree!.Evaluate(new Workbook()).AsNumber());
    }

    [Fact]
    public void Formula_CellReference_EvaluatesAgainstContext()
    {
        var wb = new Workbook();
        wb.GetOrCreate(_a1).SetLiteral("10", CellValue.FromNumber(10));

        var result = _parser.Parse("=A1+5");

        Assert.True(result.Success);
        Assert.Equal(15.0, result.Tree!.Evaluate(wb).AsNumber());
    }

    [Fact]
    public void Formula_SumOverRange_EvaluatesCorrectly()
    {
        var wb = new Workbook();
        wb.GetOrCreate(_a1).SetLiteral("1", CellValue.FromNumber(1));
        wb.GetOrCreate(_a2).SetLiteral("2", CellValue.FromNumber(2));
        wb.GetOrCreate(_a3).SetLiteral("3", CellValue.FromNumber(3));

        var result = _parser.Parse("=SUM(A1:A3)");

        Assert.True(result.Success);
        Assert.Equal(6.0, result.Tree!.Evaluate(wb).AsNumber());
    }

    [Fact]
    public void Formula_UnknownFunctionName_ParsesSuccessfully()
    {
        // The grammar accepts any FUNCNAME — resolution failure is an
        // evaluation-time #NAME?, not a parse error (Design_Portfolio 4.8).
        var result = _parser.Parse("=NOTAREALFUNC(1)");

        Assert.True(result.Success);
        var value = result.Tree!.Evaluate(new Workbook());
        Assert.Equal(ErrorKind.Name, value.Error);
    }

    // ── Literals (no leading '=') ─────────────────────────────────

    [Fact]
    public void Literal_Number_Succeeds()
    {
        var result = _parser.Parse("42");
        Assert.True(result.Success);
        Assert.False(result.IsFormula);
        Assert.Equal(42.0, result.Tree!.Evaluate(new Workbook()).AsNumber());
    }

    [Fact]
    public void Literal_Boolean_Succeeds()
    {
        var result = _parser.Parse("TRUE");
        Assert.True(result.Success);
        Assert.False(result.IsFormula);
        Assert.True(result.Tree!.Evaluate(new Workbook()).Boolean);
    }

    [Fact]
    public void Literal_QuotedString_Succeeds()
    {
        var result = _parser.Parse("\"hello\"");
        Assert.True(result.Success);
        Assert.False(result.IsFormula);
        Assert.Equal("hello", result.Tree!.Evaluate(new Workbook()).AsText());
    }

    // ── Malformed input ────────────────────────────────────────────

    [Fact]
    public void MissingCloseParen_Fails()
    {
        var result = _parser.Parse("=SUM(A1:A5");
        Assert.False(result.Success);
        Assert.NotEmpty(result.Errors);
        Assert.Null(result.Tree);
    }

    [Fact]
    public void TrailingOperator_Fails()
    {
        var result = _parser.Parse("=1+");
        Assert.False(result.Success);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public void UnrecognizedCharacters_LexerErrorFails()
    {
        // Lowercase letters match no lexer rule at all — CELLREF and
        // FUNCNAME are both uppercase-only — so this exercises the
        // lexer's error channel specifically, not just the parser's.
        var result = _parser.Parse("hello");
        Assert.False(result.Success);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public void EmptyInput_Fails()
    {
        var result = _parser.Parse("");
        Assert.False(result.Success);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public void MalformedInput_NeverThrows()
    {
        var ex = Record.Exception(() => _parser.Parse("=SUM(((("));
        Assert.Null(ex);
    }

    [Fact]
    public void EmptyInput_NeverThrows()
    {
        var ex = Record.Exception(() => _parser.Parse(""));
        Assert.Null(ex);
    }

    // ── IsFormula flag ──────────────────────────────────────────────

    [Fact]
    public void LeadingEquals_IsFormulaTrue()
    {
        Assert.True(_parser.Parse("=1").IsFormula);
    }

    [Fact]
    public void NoLeadingEquals_IsFormulaFalse()
    {
        Assert.False(_parser.Parse("1").IsFormula);
    }
}