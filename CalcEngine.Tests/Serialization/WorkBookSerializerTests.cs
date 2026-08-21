using System;
using System.IO;
using Xunit;
using CalcEngine.Core.Engine;
using CalcEngine.Core.Model;
using CalcEngine.Core.Serialization;

namespace CalcEngine.Tests.Serialization;

/// <summary>
/// Unit tests for verifying the JSON serialization and deserialization functionality of <see cref="WorkbookSerializer"/>.
/// </summary>
public class WorkbookSerializerTests : IDisposable
{
    private readonly string _tempFilePath;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkbookSerializerTests"/> class and creates a temporary file path for test outputs.
    /// </summary>
    public WorkbookSerializerTests()
    {
        _tempFilePath = Path.GetTempFileName();
    }

    /// <summary>
    /// Cleans up temporary test files created during test executions.
    /// </summary>
    public void Dispose()
    {
        if (File.Exists(_tempFilePath))
        {
            File.Delete(_tempFilePath);
        }
    }

    /// <summary>
    /// Verifies that <see cref="WorkbookSerializer.SaveToJson"/> successfully writes populated cells and formulas to a JSON file.
    /// </summary>
    [Fact]
    public void SaveToJSON_WritesPopulatedCellsToFile()
    {
        // Arrange
        var engine = new CalculationEngine();
        engine.SetCellContent(CellRef.Parse("A1"), "10");
        engine.SetCellContent(CellRef.Parse("A2"), "20");
        engine.SetCellContent(CellRef.Parse("A3"), "=A1+A2");

        // Act
        WorkbookSerializer.SaveToJSON(engine.Workbook, _tempFilePath);

        // Assert
        Assert.True(File.Exists(_tempFilePath));
        string jsonContent = File.ReadAllText(_tempFilePath);

        Assert.Contains("\"Reference\": \"A1\"", jsonContent);
        Assert.Contains("\"Input\": \"10\"", jsonContent);
        Assert.Contains("\"Reference\": \"A3\"", jsonContent);
        Assert.Contains("\"Input\": \"=A1+A2\"", jsonContent);
    }

    /// <summary>
    /// Verifies that <see cref="WorkbookSerializer.LoadFromJson"/> restores cell inputs and re-evaluates dependency graphs correctly.
    /// </summary>
    [Fact]
    public void LoadFromJSON_PopulatesEngineAndRecalculatesFormulas()
    {
        // Arrange
        var sourceEngine = new CalculationEngine();
        sourceEngine.SetCellContent(CellRef.Parse("A1"), "15");
        sourceEngine.SetCellContent(CellRef.Parse("A2"), "25");
        sourceEngine.SetCellContent(CellRef.Parse("B1"), "=A1*A2");

        WorkbookSerializer.SaveToJSON(sourceEngine.Workbook, _tempFilePath);

        var targetEngine = new CalculationEngine();

        // Act
        WorkbookSerializer.LoadFromJSON(targetEngine, _tempFilePath);

        // Assert
        Assert.Equal("15", targetEngine.GetFormula(CellRef.Parse("A1")));
        Assert.Equal("25", targetEngine.GetFormula(CellRef.Parse("A2")));
        Assert.Equal("=A1*A2", targetEngine.GetFormula(CellRef.Parse("B1")));

        // Ensure evaluated formula result is accurate (15 * 25 = 375)
        Assert.Equal(375, targetEngine.GetValue(CellRef.Parse("B1")).AsNumber());
    }

    /// <summary>
    /// Verifies that <see cref="WorkbookSerializer.LoadFromJson"/> resets and clears any preexisting workbook state prior to loading.
    /// </summary>
    [Fact]
    public void LoadFromJSON_ClearsExistingEngineStateBeforeLoading()
    {
        // Arrange
        var initialEngine = new CalculationEngine();
        initialEngine.SetCellContent(CellRef.Parse("Z100"), "Old Data");

        // Save a clean workbook with only A1
        var sourceEngine = new CalculationEngine();
        sourceEngine.SetCellContent(CellRef.Parse("A1"), "New Data");
        WorkbookSerializer.SaveToJSON(sourceEngine.Workbook, _tempFilePath);

        // Act
        WorkbookSerializer.LoadFromJSON(initialEngine, _tempFilePath);

        // Assert
        Assert.Equal("New Data", initialEngine.GetFormula(CellRef.Parse("A1")));
        Assert.Equal(string.Empty, initialEngine.GetFormula(CellRef.Parse("Z100")));
    }

    /// <summary>
    /// Verifies that <see cref="WorkbookSerializer.LoadFromJson"/> throws a <see cref="FileNotFoundException"/> when given an invalid file path.
    /// </summary>
    [Fact]
    public void LoadFromJSON_ThrowsFileNotFoundException_WhenFileDoesNotExist()
    {
        // Arrange
        var engine = new CalculationEngine();
        string nonExistentPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.json");

        // Act & Assert
        Assert.Throws<FileNotFoundException>(() =>
            WorkbookSerializer.LoadFromJSON(engine, nonExistentPath)
        );
    }
}