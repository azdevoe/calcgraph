# Benchmarks

`CalcEngine.Benchmarks` is a console harness (not BenchmarkDotNet — see
"Why a custom harness" below) that measures the two hard performance
targets from the brief against a generated 100,000-cell workbook
containing a 500-cell dependency chain (`WorkbookGenerator`):

1. **Propagation**: edit the head of the 500-cell chain, time until the
   whole chain has recomputed. Target: **< 50 ms**.
2. **Full recalculation**: recompute every formula cell in the
   100,000-cell workbook from scratch. Target: **< 2000 ms**.

Both also report memory (managed heap delta and process working set)
and the dependency-edge count, per the project plan's instruction to
report memory and edge count alongside timing.

## Running it

```
dotnet run -c Release --project CalcEngine.Benchmarks
```

Must be run in **Release** — a Debug build prints a warning and the
numbers are not representative (no tiered-JIT optimisation, debug
checks left in). Exit code is 0 if both targets were met, 1 otherwise.

## Workbook shape

- Column A, rows 1–500: the dependency chain. `A1 = 1`, `A2 = A1+1`,
  ..., `A500 = A499+1`. 499 dependency edges — the only edges in the
  workbook.
- The remaining 99,500 cells: independent formulas with no cell
  references (`=0`), spread across the rest of the sheet.

Filler cells are formulas, not bare literals, and that's deliberate.
`CalculationEngine.RecalculateAll` only recomputes cells where
`Cell.IsFormula` is true — literal filler would make "full
recalculation of 100,000 cells" silently only recompute the 500-cell
chain, and the benchmark would pass without exercising the other
99,500 cells at all. As formulas (with no dependencies of their own),
filler cells still don't add graph edges, so the propagation
benchmark's affected-cells walk still touches only the 500-cell chain
— exactly what "only cells that depend on it are recomputed" should
look like — while the full-recalculation pass now genuinely evaluates
all ~100,000 expression trees.

## Representative results

Measured on the development machine (Apple Silicon, Release build);
your numbers will differ but should be well inside both targets by a
wide margin — see "Why the margin is so wide" below.

```
Building workbook: 100,000 cells, 500-cell dependency chain...
  build time:       0.6 s (one-off setup, not a graded target)
  dependency edges: 499
  managed memory:   ~20 MB
  process working set: ~72 MB

Propagation (edit head of 500-cell chain in 100k-cell workbook)
  trials: 30, median: ~1.1 ms   -> PASS (target < 50 ms)

Full recalculation (every formula cell in 100k-cell workbook)
  trials: 5, median: ~19 ms     -> PASS (target < 2000 ms)
```

## Design decisions, stated explicitly

**Why a custom harness instead of BenchmarkDotNet.** The project plan
recommends BenchmarkDotNet, and for the full-recalculation benchmark
alone that would be the better choice. But the propagation benchmark
needs a *fresh, unedited* 100,000-cell workbook for every single timed
trial — the whole point is timing one edit's propagation, and a second
edit against an already-propagated workbook doesn't measure the same
thing. BenchmarkDotNet's `[IterationSetup]` would have to rebuild the
100k-cell workbook (0.6s) before every one of its default ~15 timed
iterations, most of which is workbook construction, not the
propagation being measured, and BenchmarkDotNet's statistical engine
is built around many cheap, independent iterations, not a handful of
expensive stateful ones. A Stopwatch-based harness with explicit
warm-up trials (5 discarded before 30 measured, for propagation; 1
discarded before 5 measured, for full recalculation, since each trial
there is itself a 100k-cell pass) gets the metric the brief actually
asks for with a fraction of the machinery. The trade-off: no
confidence intervals, no outlier classification, less credible for
sub-millisecond deltas — acceptable here because both measured medians
sit roughly 40× and 100× under their targets, so precision to the
microsecond doesn't change the verdict.

**Why the margin is so wide.** `DependencyGraph.GetAffectedCells` and
`TopologicalSort` are both restricted to the affected subgraph
(§ dependency tracking in the design portfolio), and `Cell.Tree` is
parsed once and cached, never re-parsed during recalculation — so a
500-cell propagation is 500 dictionary lookups and tree evaluations,
not a scan of the other 99,500 cells. That is the reactive-recalculation
property the brief asks for, and the benchmark margin is the visible
evidence of it, not evidence the target was set too low.
