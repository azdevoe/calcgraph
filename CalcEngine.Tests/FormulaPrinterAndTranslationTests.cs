using CalcEngine.Core;
using Xunit;

namespace CalcEngine.Tests;

/// <summary>
/// Tests for FormulaPrinter and ReferenceTranslationVisitor — the two
/// visitors RangeSorter uses to move a formula's raw text (Group C
/// feature: Sorting &amp; Filtering, RangeSorter Option B). Round-trips
/// through FormulaInputParser so each test reads as ordinary formula
/// text in and formula text out, not hand-built trees.
/// </summary>
public class FormulaPrinterAndTranslationTests
{
    private static readonly FormulaInputParser Parser = new();

    private static IExpression ParseTree(string rawInput)
    {
        var result = Parser.Parse(rawInput);
        Assert.True(result.Success, string.Join("; ", result.Errors));
        return result.Tree!;
    }

    // ── FormulaPrinter: round trip ──────────────────────────────────

    [Theory]
    [InlineData("=A1+B2")]
    [InlineData("=SUM(A1:A5)")]
    [InlineData("=IF(A1>5,\"big\",\"small\")")]
    [InlineData("=-A1")]
    [InlineData("=TRUE")]
    public void Print_ThenReparse_ProducesEquivalentTree(string original)
    {
        var tree = ParseTree(original);
        var printed = FormulaPrinter.Print(tree);
        var reparsed = ParseTree(printed);

        // Same shape when printed again — the definition of "equivalent
        // tree" used here, since IExpression has no structural equality.
        Assert.Equal(printed, FormulaPrinter.Print(reparsed));
    }

    [Fact]
    public void Print_CellRef_UsesA1Notation()
    {
        var tree = ParseTree("=B2");
        Assert.Equal("=B2", FormulaPrinter.Print(tree));
    }

    [Fact]
    public void Print_Range_UsesColonNotation()
    {
        var tree = ParseTree("=SUM(B2:B45)");
        Assert.Equal("=SUM(B2:B45)", FormulaPrinter.Print(tree));
    }

    // ── ReferenceTranslationVisitor ──────────────────────────────────

    [Fact]
    public void Translate_CellRef_ShiftsRowByDelta()
    {
        var tree = ParseTree("=A1");
        var translated = ReferenceTranslationVisitor.Translate(tree, deltaRow: 4, deltaColumn: 0);
        Assert.Equal("=A5", FormulaPrinter.Print(translated));
    }

    [Fact]
    public void Translate_ShiftsEveryReferenceInFormula_InsideAndOutsideOriginalRow()
    {
        var tree = ParseTree("=A1+C10");
        var translated = ReferenceTranslationVisitor.Translate(tree, deltaRow: 5, deltaColumn: 0);
        Assert.Equal("=(A6+C15)", FormulaPrinter.Print(translated));
    }

    [Fact]
    public void Translate_Range_ShiftsBothCorners()
    {
        var tree = ParseTree("=SUM(A1:A5)");
        var translated = ReferenceTranslationVisitor.Translate(tree, deltaRow: 2, deltaColumn: 0);
        Assert.Equal("=SUM(A3:A7)", FormulaPrinter.Print(translated));
    }

    [Fact]
    public void Translate_ZeroDelta_LeavesReferencesUnchanged()
    {
        var tree = ParseTree("=A1+B2");
        var translated = ReferenceTranslationVisitor.Translate(tree, deltaRow: 0, deltaColumn: 0);
        Assert.Equal(FormulaPrinter.Print(tree), FormulaPrinter.Print(translated));
    }

    [Fact]
    public void Translate_DoesNotTouchLiteralsInsideTheFormula()
    {
        var tree = ParseTree("=A1+\"literal text unaffected\"");
        var translated = ReferenceTranslationVisitor.Translate(tree, deltaRow: 3, deltaColumn: 0);
        Assert.Contains("literal text unaffected", FormulaPrinter.Print(translated));
    }
}
