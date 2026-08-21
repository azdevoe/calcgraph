using CalcEngine.Core.Validation;
using Xunit;

namespace CalcEngine.Tests.Validation;

/// <summary>
/// Tests for ValidationResult — the return type of IValidationRule.Validate.
/// Same shape as CellChangeSet / FormulaParseResult: a passed check and a
/// failed check both come back as data, never an exception.
/// </summary>
public class ValidationResultTests
{
    [Fact]
    public void Ok_SuccessIsTrue()
    {
        var result = ValidationResult.Ok();
        Assert.True(result.Success);
    }

    [Fact]
    public void Ok_ErrorMessageIsNull()
    {
        var result = ValidationResult.Ok();
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void Fail_SuccessIsFalse()
    {
        var result = ValidationResult.Fail("value must be between 0 and 100");
        Assert.False(result.Success);
    }

    [Fact]
    public void Fail_SetsErrorMessage()
    {
        var result = ValidationResult.Fail("value must be between 0 and 100");
        Assert.Equal("value must be between 0 and 100", result.ErrorMessage);
    }
}