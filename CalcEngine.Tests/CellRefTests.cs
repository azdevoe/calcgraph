using CalcEngine.Core;
using Xunit;

namespace CalcEngine.Tests;

public class CellRefTests
{
    [Theory]
    [InlineData("A1", 1, 1)]
    [InlineData("B2", 2, 2)]
    [InlineData("Z1", 1, 26)]
    [InlineData("AA1", 1, 27)]
    [InlineData("AB10", 10, 28)]
    [InlineData("B45", 45, 2)]
    public void Parse_ValidReference_ReturnsRowAndColumn(string input, int row, int col)
    {
        var r = CellRef.Parse(input);
        Assert.Equal(row, r.Row);
        Assert.Equal(col, r.Column);
    }

    [Theory]
    [InlineData("A1")]
    [InlineData("Z1")]
    [InlineData("AA1")]
    [InlineData("AB10")]
    public void ToA1_RoundTripsWithParse(string input)
        => Assert.Equal(input, CellRef.Parse(input).ToA1());

    [Theory]
    [InlineData("")]
    [InlineData("A")]
    [InlineData("1")]
    [InlineData("A0")]
    [InlineData("1A")]
    [InlineData("A1B")]
    public void Parse_Invalid_Throws(string input)
        => Assert.Throws<FormatException>(() => CellRef.Parse(input));

    [Fact]
    public void EqualStructurally_SameRowAndColumn()
    {
        Assert.Equal(CellRef.Parse("C7"), new CellRef(7, 3));
        Assert.Equal(CellRef.Parse("C7").GetHashCode(), new CellRef(7, 3).GetHashCode());
    }
}