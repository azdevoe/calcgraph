using System.Diagnostics;
using CalcEngine.Core.Engine;

namespace CalcEngine.Benchmarks;

/// <summary>
/// Times CalculationEngine.RecalculateAll over the same 100,000-cell
/// workbook — the brief's second hard target: under 2 seconds. Fewer
/// trials than the propagation benchmark since each one recomputes
/// every formula cell in the workbook, not just a 500-cell chain.
/// </summary>
public static class FullRecalculationBenchmark
{
    public static BenchmarkResult Run(CalculationEngine engine, int warmupTrials = 1, int measuredTrials = 5)
    {
        var timings = new List<double>();

        for (int trial = 0; trial < warmupTrials + measuredTrials; trial++)
        {
            var sw = Stopwatch.StartNew();
            engine.RecalculateAll();
            sw.Stop();

            if (trial >= warmupTrials)
                timings.Add(sw.Elapsed.TotalMilliseconds);
        }

        return BenchmarkResult.From("Full recalculation (every formula cell in 100k-cell workbook)", timings);
    }
}
