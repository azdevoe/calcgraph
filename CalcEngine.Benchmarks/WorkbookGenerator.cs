using CalcEngine.Core.Engine;
using CalcEngine.Core.Model;

namespace CalcEngine.Benchmarks;

/// <summary>
/// Builds the workbook shape the brief's performance targets are
/// stated against: "a workbook of 100,000 cells with a chain of 500
/// dependent cells." Column A, rows 1..ChainLength is the chain
/// (A2 = A1+1, A3 = A2+1, ...); everything else is filler spread
/// across the rest of the sheet.
///
/// Filler cells are formulas ("=0"), not bare literals — deliberately.
/// CalculationEngine.RecalculateAll only recomputes formula cells
/// (Cell.IsFormula), so literal filler would make "full recalculation
/// of 100,000 cells" secretly only recompute the 500-cell chain and
/// the benchmark would pass without proving anything about scale. A
/// filler formula still has zero dependencies (its own cell ref
/// expression tree, no CellRef children), so it does not add graph
/// edges — the chain is still the only place edges exist, so the
/// propagation benchmark's affected-cells walk still touches only the
/// 500-cell chain, exactly as reactive recalculation should — but it
/// does force RecalculateAll's bulk pass to actually evaluate all
/// ~100,000 expression trees instead of silently skipping the filler.
/// </summary>
public static class WorkbookGenerator
{
    public const int TotalCells = 100_000;
    public const int ChainLength = 500;
    public const int ChainColumn = 1; // column A

    /// <summary>The first cell of the chain — editing this is what the propagation benchmark times.</summary>
    public static readonly CellRef ChainHead = new(1, ChainColumn);

    /// <summary>The last cell of the chain — everything the head's edit must reach.</summary>
    public static readonly CellRef ChainTail = new(ChainLength, ChainColumn);

    /// <summary>Dependency edges in this shape: one per link in the chain. Known analytically — no need to inspect the graph.</summary>
    public const int ExpectedDependencyEdges = ChainLength - 1;

    public static CalculationEngine Build()
    {
        var engine = new CalculationEngine();

        engine.SetCellContent(ChainHead, "1");
        for (int row = 2; row <= ChainLength; row++)
        {
            var prev = new CellRef(row - 1, ChainColumn);
            var here = new CellRef(row, ChainColumn);
            engine.SetCellContent(here, $"={prev.ToA1()}+1");
        }

        int filler = TotalCells - ChainLength;
        const int fillerRowsPerColumn = 10_000;
        int col = ChainColumn + 1;
        int row2 = 1;
        for (int i = 0; i < filler; i++)
        {
            engine.SetCellContent(new CellRef(row2, col), "=0");
            row2++;
            if (row2 > fillerRowsPerColumn)
            {
                row2 = 1;
                col++;
            }
        }

        return engine;
    }
}
