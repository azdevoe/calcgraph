using CalcEngine.Core;
using Xunit;

namespace CalcEngine.Tests;

/// <summary>
/// Tests for CellRef — the cell ADDRESS type (not the cell's contents).
/// Two directions are under test:  text -> numbers (Parse),  numbers -> text (ToA1).
/// </summary>
public class CellRefTests
{
    // [Theory] = one test method run many times with different data.
    // [InlineData] supplies one set of arguments per run. Six runs here.
    // (A [Fact] by contrast takes no arguments and runs exactly once.)

    [InlineData("A1",   1,  1)]   // simplest case: first row, first column
    [InlineData("B2",   2,  2)]   // both numbers differ from 1, catches a swapped Row/Column
    [InlineData("Z1",   1, 26)]   // last single letter — the boundary before letters double up
    [InlineData("AA1",  1, 27)]   // first DOUBLE letter. 26+1, not 26*2. Catches base-26 errors.
    [InlineData("AB10", 10, 28)]  // two letters AND two digits at once
    [InlineData("B45",  45,  2)]  // the reference used in the project brief: =SUM(B2:B45)
    [Theory]
    public void Parse_ValidReference_ReturnsRowAndColumn(string input, int row, int col)
    {
        var r = CellRef.Parse(input);

        // Assert.Equal(expected, actual) — expected always goes FIRST.
        // Swapping them still passes, but the failure message reads backwards.
        Assert.Equal(row, r.Row);
        Assert.Equal(col, r.Column);
    }

    // A "round trip" = convert one way, then back, and check you got the original.
    // This catches Parse and ToA1 disagreeing with EACH OTHER, which the test above
    // cannot see. If both had the same base-26 bug they would still round-trip fine
    // for A1..Z1 — which is exactly why AA1 is in this list.
    [InlineData("A1")]
    [InlineData("Z1")]
    [InlineData("AA1")]   // fails if the n-- in ToA1 is missing: produces "BB1"
    [InlineData("AB10")]
    [Theory]
    public void ToA1_RoundTripsWithParse(string input)
        => Assert.Equal(input, CellRef.Parse(input).ToA1());

    // Malformed input must be REJECTED, not silently misread.
    // A wrong answer is worse than an error — the brief opens with a corrupted
    // CGPA caused by one bad cell reference nobody noticed.
    [InlineData("")]      // empty string
    [InlineData("A")]     // letters but no row number
    [InlineData("1")]     // digits but no column letters
    [InlineData("A0")]    // row 0 — rows are 1-based, this breaks the invariant
    [InlineData("1A")]    // right characters, wrong order
    [InlineData("A1B")]   // trailing junk. Without the digit check this parses as "A1".
    [Theory]
    public void Parse_Invalid_Throws(string input)
        // Assert.Throws needs a lambda, not a value. Passing CellRef.Parse(input)
        // directly would run it HERE and the exception would escape uncaught.
        // The () => defers it so xUnit can run it and catch what comes out.
        => Assert.Throws<FormatException>(() => CellRef.Parse(input));

    // The most important test in the file, despite looking trivial.
    // CellRef is a dictionary KEY everywhere in the engine. A dictionary finds a key
    // in two steps: GetHashCode picks the bucket, then Equals confirms the match.
    // Both must be value-based or lookups fail.
    [Fact]
    public void EqualStructurally_SameRowAndColumn()
    {
        // Same cell, built two different ways: parsed from text vs constructed directly.
        // Without the `record` keyword these compare as unequal (identity, not value),
        // cells[CellRef.Parse("C7")] and cells[new CellRef(7,3)] hit different buckets,
        // and the dependency graph never links a formula to the cells it reads.
        Assert.Equal(CellRef.Parse("C7"), new CellRef(7, 3));

        // Equals alone is not enough. A type can have correct Equals and a broken
        // GetHashCode — the dictionary checks the bucket BEFORE it checks equality,
        // so it would never reach the comparison. Both must be asserted.
        Assert.Equal(CellRef.Parse("C7").GetHashCode(), new CellRef(7, 3).GetHashCode());
    }
}