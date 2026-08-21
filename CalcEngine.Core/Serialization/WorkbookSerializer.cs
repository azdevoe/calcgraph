using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.IO;
using CalcEngine.Core.Model;
using CalcEngine.Core.Engine;

namespace CalcEngine.Core.Serialization
{
    /// <summary>
    /// Data Transfer Object representing a cell's reference and raw input string for serialization.
    /// </summary>
    /// <param name="Reference">The A1-style reference string (e.g., "A1", "B5").</param>
    /// <param name="Input">The raw cell input text or formula (e.g., "100", "=SUM(A1:A4)").</param>
    public record CellDTO(string Reference, string Input);

    /// <summary>
    /// Provides JSON serialization and deserialization functionality for persisting workbook state.
    /// </summary>
    public static class WorkbookSerializer
    {
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

        /// <summary>
        /// Serializes every non-empty cell in the specified workbook to a JSON file.
        /// </summary>
        /// <param name="workbook">The <see cref="Workbook"/> instance whose cell contents will be saved.</param>
        /// <param name="filePath">The file path where the JSON data will be written.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="workbook"/> or <paramref name="filePath"/> is null.</exception>
        /// <exception cref="IOException">Thrown if an error occurs while writing to the file.</exception>
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
        /// <param name="engine">The target <see cref="CalculationEngine"/> instance to populate.</param>
        /// <param name="filePath">The path of the JSON file to read.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="engine"/> or <paramref name="filePath"/> is null.</exception>
        /// <exception cref="FileNotFoundException">Thrown when the specified file at <paramref name="filePath"/> does not exist.</exception>
        /// <exception cref="JsonException">Thrown if the file content is not valid JSON.</exception>
        public static void LoadFromJSON(CalculationEngine engine, string filePath)
        {
            ArgumentNullException.ThrowIfNull(engine);
            ArgumentNullException.ThrowIfNull(filePath);

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("Workbook file not found.", filePath);
            }

            string jsonString = File.ReadAllText(filePath);

            // Fix: Specify List<CellDTO> as the generic target type
            var cellDataList = JsonSerializer.Deserialize<List<CellDTO>>(jsonString);

            engine.Clear();

            if (cellDataList is null) return;

            engine.BeginBatch();
            try
            {
                foreach (var item in cellDataList)
                {
                    if (!string.IsNullOrWhiteSpace(item.Reference) && !string.IsNullOrWhiteSpace(item.Input))
                    {
                        CellRef cellRef = CellRef.Parse(item.Reference);
                        engine.SetCellContent(cellRef, item.Input);
                    }
                }
            }
            finally
            {
                engine.EndBatch();
            }
        }
    }
}