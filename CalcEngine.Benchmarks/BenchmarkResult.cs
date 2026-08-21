namespace CalcEngine.Benchmarks;

/// <summary>Summary statistics (in milliseconds) over a benchmark's measured trials.</summary>
public sealed class BenchmarkResult
{
    public string Name { get; }
    public double MinMs { get; }
    public double MedianMs { get; }
    public double MeanMs { get; }
    public double MaxMs { get; }
    public int TrialCount { get; }

    private BenchmarkResult(string name, double minMs, double medianMs, double meanMs, double maxMs, int trialCount)
    {
        Name = name;
        MinMs = minMs;
        MedianMs = medianMs;
        MeanMs = meanMs;
        MaxMs = maxMs;
        TrialCount = trialCount;
    }

    public static BenchmarkResult From(string name, IReadOnlyList<double> timingsMs)
    {
        if (timingsMs.Count == 0)
            throw new ArgumentException("At least one measured trial is required.", nameof(timingsMs));

        var sorted = timingsMs.OrderBy(t => t).ToList();
        double median = sorted.Count % 2 == 1
            ? sorted[sorted.Count / 2]
            : (sorted[sorted.Count / 2 - 1] + sorted[sorted.Count / 2]) / 2.0;

        return new BenchmarkResult(name, sorted[0], median, sorted.Average(), sorted[^1], sorted.Count);
    }

    /// <summary>
    /// Judges against targetMs using the median — a single slow trial
    /// (GC pause, OS scheduling hiccup) shouldn't flip a pass/fail
    /// verdict on its own; the max is still reported alongside it so a
    /// genuinely bad tail isn't hidden.
    /// </summary>
    public bool MeetsTarget(double targetMs) => MedianMs < targetMs;

    public override string ToString() =>
        $"{Name}\n" +
        $"  trials: {TrialCount}\n" +
        $"  min:    {MinMs:F3} ms\n" +
        $"  median: {MedianMs:F3} ms\n" +
        $"  mean:   {MeanMs:F3} ms\n" +
        $"  max:    {MaxMs:F3} ms";
}
