using CalcEngine.Core;
using CalcEngine.Core.Parsing;
using CalcEngine.Core.Expressions;
using Xunit;

namespace CalcEngine.Tests.Parsing;

/// <summary>
/// Tests for FormulaParseResult — the return type of FormulaParser.Parse.
/// Same shape as CellChangeSet's success/failure split: a successful
/// parse and a syntax failure both come back as data.
/// </summary>
public class FormulaParseResultTests
{
    [Fact]
    public void Ok_SuccessIsTrue()
    {
        var tree = new NumberExpression(1);
        var result = FormulaParseResult.Ok(tree, isFormula: true);
        Assert.True(result.Success);
    }

    [Fact]
    public void Ok_SetsTree()
    {
        var tree = new NumberExpression(1);
        var result = FormulaParseResult.Ok(tree, isFormula: true);
        Assert.Same(tree, result.Tree);
    }

    [Fact]
    public void Ok_SetsIsFormula()
    {
        var tree = new NumberExpression(1);
        var result = FormulaParseResult.Ok(tree, isFormula: true);
        Assert.True(result.IsFormula);
    }

    [Fact]
    public void Ok_IsFormulaFalse_ForLiteral()
    {
        var tree = new NumberExpression(1);
        var result = FormulaParseResult.Ok(tree, isFormula: false);
        Assert.False(result.IsFormula);
    }

    [Fact]
    public void Ok_ErrorsIsEmpty()
    {
        var tree = new NumberExpression(1);
        var result = FormulaParseResult.Ok(tree, isFormula: true);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Failure_SuccessIsFalse()
    {
        var result = FormulaParseResult.Failure(new[] { "unexpected token" });
        Assert.False(result.Success);
    }

    [Fact]
    public void Failure_TreeIsNull()
    {
        var result = FormulaParseResult.Failure(new[] { "unexpected token" });
        Assert.Null(result.Tree);
    }

    [Fact]
    public void Failure_IsFormulaIsFalse()
    {
        var result = FormulaParseResult.Failure(new[] { "unexpected token" });
        Assert.False(result.IsFormula);
    }

    [Fact]
    public void Failure_SetsErrors()
    {
        var errors = new[] { "unexpected token", "missing ')'" };
        var result = FormulaParseResult.Failure(errors);
        Assert.Equal(errors, result.Errors);
    }
}