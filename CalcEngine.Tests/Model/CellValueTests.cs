using CalcEngine.Core.Model;
using Xunit;

namespace CalcEngine.Tests.Model;

/// <summary>
/// Tests for CellValue — what a cell HOLDS after evaluation.
/// A spreadsheet cell is not a number. It is one of five things:
/// empty, a number, text, a boolean, or an error. This type is a
/// tagged union over those five, and the tag is ValueKind.
/// </summary>
public class CellValueTests
{
    // Each factory must set the Kind tag AND carry the payload.
    // If Kind were wrong, the evaluator would read the wrong field
    // and silently return 0 for a piece of text.

    [Fact]
    public void FromNumber_HasNumberKindAndValue()
    {
        var v = CellValue.FromNumber(42.5);
        Assert.Equal(ValueKind.Number, v.Kind);
        Assert.Equal(42.5, v.Number);
        Assert.False(v.IsError);
    }

    [Fact]
    public void FromText_HasTextKindAndValue()
    {
        var v = CellValue.FromText("Vengance");
        Assert.Equal(ValueKind.Text, v.Kind);
        Assert.Equal("Vengance", v.Text);
        Assert.False(v.IsError);
    }

    [Fact]
    public void FromBoolean_HasBooleanKind()
    {
        var v = CellValue.FromBoolean(true);
        Assert.Equal(ValueKind.Boolean, v.Kind);
        Assert.True(v.Boolean);
    }

    [Fact]
    public void FromError_HasErrorKindAndIsError()
    {
        var v = CellValue.FromError(ErrorKind.DivideByZero);
        Assert.Equal(ValueKind.Error, v.Kind);
        Assert.Equal(ErrorKind.DivideByZero, v.Error);
        Assert.True(v.IsError);   // the flag the evaluator branches on
    }

    // Empty is a real, addressable state — not null. A formula may
    // reference a cell nobody ever typed into, and that must evaluate
    // to something rather than crash.
    [Fact]
    public void Empty_IsItsOwnKindAndNotAnError()
    {
        var v = CellValue.Empty;
        Assert.Equal(ValueKind.Empty, v.Kind);
        Assert.False(v.IsError);
    }

    // In a spreadsheet, an empty cell behaves as 0 in arithmetic:
    // =B45+1 gives 1 when B45 was never touched.
    [Fact]
    public void Empty_CoercesToZeroForArithmetic()
        => Assert.Equal(0, CellValue.Empty.AsNumber());

    // Text where a number is required is #VALUE!, NOT an exception.
    // This is the rule the brief states: errors reach the client as
    // values, never as exceptions escaping mid-recalculation.
    [Fact]
    public void AsNumber_OnText_ThrowsNothing_ButKindStaysText()
    {
        var v = CellValue.FromText("hello");
        Assert.Equal(ValueKind.Text, v.Kind);
        Assert.False(v.IsError);   // text is not itself an error...
    }

    // Error display strings are what the GUI grid shows. They are
    // the standard spreadsheet spellings, so the fixture doubles as
    // documentation of the whole error model.
    [InlineData(ErrorKind.DivideByZero, "#DIV/0!")]
    [InlineData(ErrorKind.Value,        "#VALUE!")]
    [InlineData(ErrorKind.Reference,    "#REF!")]
    [InlineData(ErrorKind.Name,         "#NAME?")]
    [InlineData(ErrorKind.Circular,     "#CIRCULAR!")]
    [InlineData(ErrorKind.NotAvailable, "#N/A")]
    [Theory]
    public void FromError_DisplaysStandardSpreadsheetText(ErrorKind kind, string expected)
        => Assert.Equal(expected, CellValue.FromError(kind).ToString());

    // Value equality again, same reason as CellRef: change sets are
    // compared, and a test asserting "this cell now holds 5" needs
    // two separately built CellValues to compare equal.
    [Fact]
    public void EqualStructurally_SameKindAndPayload()
    {
        Assert.Equal(CellValue.FromNumber(5), CellValue.FromNumber(5));
        Assert.NotEqual(CellValue.FromNumber(5), CellValue.FromText("5"));
    }
}