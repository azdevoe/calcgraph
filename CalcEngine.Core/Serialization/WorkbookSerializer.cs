using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using CalcEngine.Core.Engine;
using CalcEngine.Core.Model;

namespace CalcEngine.Core.Serialization
{
    /// <summary>
    /// Data Transfer Object representing a cell's reference and raw input string for serialization.
    /// </summary>
    public class CellDTO
    {
        [JsonPropertyName("Reference")]
        public string Reference { get; set; } = string.Empty;

        [JsonPropertyName("Input")]
        public string Input { get; set; } = string.Empty;

        public CellDTO() { }

        public CellDTO(string reference, string input)
        {
            Reference = reference;
            Input = input;
        }
    }

    /// <summary>
    /// Provides JSON serialization and deserialization functionality for persisting workbook state.
    /// </summary>
    public static class WorkbookSerializer
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            PropertyNameCaseInsensitive = true
        };

        /// <summary>
        /// Serializes every non-empty cell in the specified workbook to a JSON file.
        /// </summary>
        public static void SaveToJSON(Workbook workbook, string filePath)
        {
            ArgumentNullException.ThrowIfNull(workbook);
            ArgumentNullException.ThrowIfNull(filePath);

            var cellDataList = new List<CellDTO>();

            foreach (var cell in workbook.AllCells())
            {
                if (!string.IsNullOrWhiteSpace(cell.RawInput))
                {
                    cellDataList.Add(new CellDTO(cell.Ref.ToA1(), cell.RawInput));
                }
            }

            string jsonString = JsonSerializer.Serialize(cellDataList, JsonOptions);
            File.WriteAllText(filePath, jsonString);
        }

        /// <summary>
        /// Clears the engine's current state and populates it with cell contents loaded from a JSON file.
        /// </summary>
        public static void LoadFromJSON(CalculationEngine engine, string filePath)
        {
            ArgumentNullException.ThrowIfNull(engine);
            ArgumentNullException.ThrowIfNull(filePath);

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("Workbook file not found.", filePath);
            }

            string jsonString = File.ReadAllText(filePath);
            var cellDataList = JsonSerializer.Deserialize<List<CellDTO>>(jsonString, JsonOptions);

            engine.Clear();

            if (cellDataList is null) return;

            foreach (var item in cellDataList)
            {
                if (!string.IsNullOrWhiteSpace(item.Reference) && item.Input is not null)
                {
                    CellRef cellRef = CellRef.Parse(item.Reference);
                    engine.SetCellContent(cellRef, item.Input);
                }
            }
        }
    }
}