using System.Diagnostics;
using CalcEngine.Benchmarks;

const double propagationTargetMs = 50.0;
const double fullRecalcTargetMs = 2000.0;

#if DEBUG
Console.WriteLine("WARNING: running a Debug build. The project plan requires Release for");
Console.WriteLine("credible numbers (dotnet run -c Release --project CalcEngine.Benchmarks).");
Console.WriteLine();
#endif

Console.WriteLine("Building workbook: " +
    $"{WorkbookGenerator.TotalCells:N0} cells, {WorkbookGenerator.ChainLength}-cell dependency chain...");

long memoryBefore = GC.GetTotalMemory(forceFullCollection: true);
var buildSw = Stopwatch.StartNew();
var engine = WorkbookGenerator.Build();
buildSw.Stop();
long memoryAfter = GC.GetTotalMemory(forceFullCollection: true);

Console.WriteLine($"  build time:       {buildSw.Elapsed.TotalSeconds:F2} s (one-off setup, not a graded target)");
Console.WriteLine($"  dependency edges: {WorkbookGenerator.ExpectedDependencyEdges:N0} " +
    "(only the chain has edges — filler cells are formulas with no dependencies of their own)");
Console.WriteLine($"  managed memory:   {(memoryAfter - memoryBefore) / (1024.0 * 1024.0):F1} MB " +
    "(GC.GetTotalMemory delta; approximate)");
Console.WriteLine($"  process working set: {Process.GetCurrentProcess().WorkingSet64 / (1024.0 * 1024.0):F1} MB");
Console.WriteLine();

Console.WriteLine("Running propagation benchmark (target: < 50 ms, median of measured trials)...");
var propagation = PropagationBenchmark.Run(engine);
Console.WriteLine(propagation);
Console.WriteLine($"  target: < {propagationTargetMs} ms -> " +
    (propagation.MeetsTarget(propagationTargetMs) ? "PASS" : "FAIL"));
Console.WriteLine();

Console.WriteLine("Running full recalculation benchmark (target: < 2000 ms, median of measured trials)...");
var fullRecalc = FullRecalculationBenchmark.Run(engine);
Console.WriteLine(fullRecalc);
Console.WriteLine($"  target: < {fullRecalcTargetMs} ms -> " +
    (fullRecalc.MeetsTarget(fullRecalcTargetMs) ? "PASS" : "FAIL"));
Console.WriteLine();

bool allPassed = propagation.MeetsTarget(propagationTargetMs) && fullRecalc.MeetsTarget(fullRecalcTargetMs);
Console.WriteLine(allPassed ? "All targets met." : "One or more targets missed.");

return allPassed ? 0 : 1;
