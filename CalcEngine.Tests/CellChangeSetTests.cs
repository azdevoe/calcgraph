using CalcEngine.Core;
using Xunit;

namespace CalcEngine.Tests;

/// <summary>
/// Tests for CellChangeSet — the single return type for every client-
/// facing operation (Design_Portfolio 4.1). A successful edit, a parse
/// failure, a circular reference, and a validation rejection all come
/// back through this one type, as data, so the client is never
/// required to catch anything.
/// </summary>
public class CellChangeSetTests
{
    private readonly CellRef _a1 = CellRef.Parse("A1");
    private readonly CellRef _b2 = CellRef.Parse("B2");
    private readonly CellRef _c3 = CellRef.Parse("C3");

    // ── Ok ─────────────────────────────────────────────────────────

    [Fact]
    public void Ok_SuccessIsTrue()
    {
        var result = CellChangeSet.Ok(_a1, new[] { _a1 });
        Assert.True(result.Success);
    }

    [Fact]
    public void Ok_SetsEditedCell()
    {
        var result = CellChangeSet.Ok(_a1, new[] { _a1 });
        Assert.Equal(_a1, result.Edited);
    }

    [Fact]
    public void Ok_SetsChangedCells()
    {
        var changed = new[] { _a1, _b2, _c3 };
        var result = CellChangeSet.Ok(_a1, changed);
        Assert.Equal(changed, result.ChangedCells);
    }

    [Fact]
    public void Ok_ErrorMessageIsNull()
    {
        var result = CellChangeSet.Ok(_a1, new[] { _a1 });
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void Ok_CircularPathIsNull()
    {
        var result = CellChangeSet.Ok(_a1, new[] { _a1 });
        Assert.Null(result.CircularPath);
    }

    [Fact]
    public void Ok_FailureReasonIsNone()
    {
        var result = CellChangeSet.Ok(_a1, new[] { _a1 });
        Assert.Equal(ChangeFailureReason.None, result.FailureReason);
    }

    // ── ParseFailure ───────────────────────────────────────────────

    [Fact]
    public void ParseFailure_SuccessIsFalse()
    {
        var result = CellChangeSet.ParseFailure(_a1, "unexpected token");
        Assert.False(result.Success);
    }

    [Fact]
    public void ParseFailure_SetsEditedCell()
    {
        var result = CellChangeSet.ParseFailure(_a1, "unexpected token");
        Assert.Equal(_a1, result.Edited);
    }

    [Fact]
    public void ParseFailure_SetsErrorMessage()
    {
        var result = CellChangeSet.ParseFailure(_a1, "unexpected token");
        Assert.Equal("unexpected token", result.ErrorMessage);
    }

    [Fact]
    public void ParseFailure_ChangedCellsIsEmpty()
    {
        var result = CellChangeSet.ParseFailure(_a1, "unexpected token");
        Assert.Empty(result.ChangedCells);
    }

    [Fact]
    public void ParseFailure_CircularPathIsNull()
    {
        var result = CellChangeSet.ParseFailure(_a1, "unexpected token");
        Assert.Null(result.CircularPath);
    }

    [Fact]
    public void ParseFailure_FailureReasonIsParseError()
    {
        var result = CellChangeSet.ParseFailure(_a1, "unexpected token");
        Assert.Equal(ChangeFailureReason.ParseError, result.FailureReason);
    }

    // ── Circular ───────────────────────────────────────────────────

    [Fact]
    public void Circular_SuccessIsFalse()
    {
        var path = new[] { _a1, _b2, _a1 };
        var result = CellChangeSet.Circular(_a1, path);
        Assert.False(result.Success);
    }

    [Fact]
    public void Circular_SetsEditedCell()
    {
        var path = new[] { _a1, _b2, _a1 };
        var result = CellChangeSet.Circular(_a1, path);
        Assert.Equal(_a1, result.Edited);
    }

    [Fact]
    public void Circular_SetsCircularPath()
    {
        var path = new[] { _a1, _b2, _a1 };
        var result = CellChangeSet.Circular(_a1, path);
        Assert.Equal(path, result.CircularPath);
    }

    [Fact]
    public void Circular_ChangedCellsIsEmpty()
    {
        var path = new[] { _a1, _b2, _a1 };
        var result = CellChangeSet.Circular(_a1, path);
        Assert.Empty(result.ChangedCells);
    }

    [Fact]
    public void Circular_ErrorMessageIsNull()
    {
        // The circular path IS the error information; ErrorMessage
        // stays null so a client checks CircularPath, not both fields.
        var path = new[] { _a1, _b2, _a1 };
        var result = CellChangeSet.Circular(_a1, path);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void Circular_FailureReasonIsCircular()
    {
        var path = new[] { _a1, _b2, _a1 };
        var result = CellChangeSet.Circular(_a1, path);
        Assert.Equal(ChangeFailureReason.Circular, result.FailureReason);
    }

    // ── ValidationFailed ───────────────────────────────────────────

    [Fact]
    public void ValidationFailed_SuccessIsFalse()
    {
        var result = CellChangeSet.ValidationFailed(_a1, "value must be between 0 and 100");
        Assert.False(result.Success);
    }

    [Fact]
    public void ValidationFailed_SetsEditedCell()
    {
        var result = CellChangeSet.ValidationFailed(_a1, "value must be between 0 and 100");
        Assert.Equal(_a1, result.Edited);
    }

    [Fact]
    public void ValidationFailed_SetsErrorMessage()
    {
        var result = CellChangeSet.ValidationFailed(_a1, "value must be between 0 and 100");
        Assert.Equal("value must be between 0 and 100", result.ErrorMessage);
    }

    [Fact]
    public void ValidationFailed_ChangedCellsIsEmpty()
    {
        var result = CellChangeSet.ValidationFailed(_a1, "value must be between 0 and 100");
        Assert.Empty(result.ChangedCells);
    }

    [Fact]
    public void ValidationFailed_CircularPathIsNull()
    {
        var result = CellChangeSet.ValidationFailed(_a1, "value must be between 0 and 100");
        Assert.Null(result.CircularPath);
    }

    [Fact]
    public void ValidationFailed_FailureReasonIsValidationError()
    {
        var result = CellChangeSet.ValidationFailed(_a1, "value must be between 0 and 100");
        Assert.Equal(ChangeFailureReason.ValidationError, result.FailureReason);
    }
}