using System.Diagnostics;
using CalcEngine.Core.Engine;

namespace CalcEngine.Benchmarks;

/// <summary>
/// Times a single-cell edit that must propagate through a 500-cell
/// dependency chain inside a 100,000-cell workbook — the brief's first
/// hard target: under 50ms. Runs a few discarded warm-up trials (JIT
/// tiering, first-call ANTLR/regex init) before the trials that are
/// actually reported, per the project plan's "warm-up iterations"
/// instruction.
/// </summary>
public static class PropagationBenchmark
{
    public static BenchmarkResult Run(CalculationEngine engine, int warmupTrials = 5, int measuredTrials = 30)
    {
        var timings = new List<double>();
        int nextValue = 2; // ChainHead already holds "1" from WorkbookGenerator

        for (int trial = 0; trial < warmupTrials + measuredTrials; trial++)
        {
            var sw = Stopwatch.StartNew();
            var result = engine.SetCellContent(WorkbookGenerator.ChainHead, nextValue.ToString());
            sw.Stop();

            if (!result.Success)
                throw new InvalidOperationException($"Propagation edit was rejected: {result.ErrorMessage}");

            // The chain must actually have recomputed, not been skipped —
            // a benchmark that doesn't check this could "pass" a
            // regression that stopped propagating.
            double expectedTail = nextValue + (WorkbookGenerator.ChainLength - 1);
            double actualTail = engine.GetValue(WorkbookGenerator.ChainTail).Number;
            if (actualTail != expectedTail)
                throw new InvalidOperationException(
                    $"Chain tail is {actualTail}, expected {expectedTail} — propagation did not reach the end of the chain.");

            if (trial >= warmupTrials)
                timings.Add(sw.Elapsed.TotalMilliseconds);

            nextValue++;
        }

        return BenchmarkResult.From("Propagation (edit head of 500-cell chain in 100k-cell workbook)", timings);
    }
}
