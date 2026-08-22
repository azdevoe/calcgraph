# AI Collaboration Log

Every significant use of an AI assistant on this project is recorded
here: the date, who was driving, the tool, what was asked for, what
came back, what was actually kept or changed, and why. Per the brief:
an entry that explains a rejection is worth more than an entry that
says "it worked so we pasted it." This file should grow week by week
as the project continues — add your own entries in the same format
below the existing ones, not reconstructed from memory at the end.

**Format:** one row in the summary table per entry, plus a detailed
subsection below with the full "what changed and why" — the table
alone is not enough to earn the marks this log is graded on.

## Summary

| # | Date | Member | Tool | Summary |
|---|---|---|---|---|
| 1 | 2026-08-21 | Sherif | Claude (Sonnet 5, Claude Code) | Implemented Sorting & Filtering (Group C feature) end to end |
| 2 | 2026-08-21 | Sherif | Claude (Sonnet 5, Claude Code) | Self-review of the sort/filter implementation found and fixed two real bugs |
| 3 | 2026-08-22 | Sherif | Claude (Sonnet 5, Claude Code) | Wired Sort/Filter into the GUI; flagged an undo/redo desync it introduced |
| 4 | 2026-08-22 | Sherif | Claude (Sonnet 5, Claude Code) | Built the benchmark harness; first version's numbers were caught as meaningless |
| 5 | 2026-08-22 | Sherif | Claude (Sonnet 5, Claude Code) | Design portfolio ADT specs — a copied invariant claim was checked and found false |
| 6 | 2026-08-22 | Sherif | Claude (Sonnet 5, Claude Code) | Design portfolio semantics table — surfaced an unhandled-exception bug (`=A0+1`) |
| 7 | 2026-08-22 | Sherif | Claude (Sonnet 5, Claude Code) | ADT specs for `DependencyGraph`/`CommandManager` accepted as generated |

---

## 1 — Sorting & Filtering implementation

**Asked for:** an implementation of the assigned Group C feature
(Sorting & Filtering), following the project's own plan: `FilterManager`
as pure view state, `SortRangeCommand` using "Option B" (move raw
content, translate cell references by the row's own move delta), and a
`BeginBatch`/`EndBatch` primitive so a sort costs one recalculation, not
one per moved cell.

**Got:** a full implementation — `FilterManager`, `ApplyFilterCommand`,
`SortKey`, `RangeSorter`, `ReferenceTranslationVisitor`,
`FormulaPrinter`, `SortRangeCommand`, and `BeginBatch`/`EndBatch` on
`CalculationEngine` — plus a first draft of `FormulaPrinter` that
tracked operator precedence to decide when parentheses were needed.

**Changed:** rewrote `FormulaPrinter` to *always* wrap every binary and
unary operand in parentheses, rather than reasoning about precedence to
decide when they're needed.

**Why:** the grammar accepts a parenthesized expression anywhere any
atom is legal, so always-parenthesizing is provably safe — it cannot
change what a reprinted formula means. A precedence-aware printer has
to get every operator's precedence and associativity exactly right or
it silently reprints a formula with a different meaning than the one
that was translated, and that's the kind of bug that would only show up
much later as a wrong cell value after a sort, not as a crash. Simpler
and unconditionally correct beat cleverer and one-bug-away-from-wrong.

---

## 2 — Self-review of the sort/filter implementation

**Asked for:** an explicit correctness pass over the sort/filter code
just written — "correctly implement sorting and filtering and ensure
no errors introduced" — rather than accepting the first passing test
run as proof of correctness.

**Got:** two findings from re-reading the code against edge cases the
first implementation pass hadn't exercised:

- `SortRange` could throw an unhandled `ArgumentException`. A row
  moving upward whose formula references a row near the top of the
  sheet can require translating a reference to row `< 1`;
  `CellRefExpression`'s constructor throws for that, and nothing was
  catching it — the exception would have propagated straight out of
  `SortRange` to the client.
- `ApplyEdit`'s validation-rejection rollback read
  `_graph.PrecedentsOf(cellRef)` **after** `SetDependencies` had
  already installed the new (about-to-be-rejected) edges, so the
  "rollback" was actually a no-op — the graph ended up believing the
  cell depended on whatever the rejected formula referenced, while the
  cell's actual stored content never changed.

**Changed:** wrapped the sort's planning phase in a `try`/`catch` that
converts the out-of-bounds case into a clean `CellChangeSet.ParseFailure`
(nothing is written before planning completes, so there's nothing to
roll back); moved the `previousDeps` capture in `ApplyEdit` to *before*
the tentative `SetDependencies` call, so it captures the graph's real
prior state.

**Why:** the first bug would have violated the engine's own core
promise — "malformed input is normal input, never an exception that
escapes to the client" — the exact promise the brief states by name.
The second bug is subtler and more dangerous: it doesn't crash
anything, it just leaves the dependency graph quietly wrong after a
rejected edit, which `SortRangeCommand`'s own rollback logic depends on
being correct. Both were confirmed with a reproduction before fixing,
and both got a regression test that fails without the fix and passes
with it, so "it's fixed" wasn't taken on faith either.

---

## 3 — Wiring Sort/Filter into the GUI

**Asked for:** `Sort...`/`Filter...` dialogs and toolbar buttons in
`Form1`, operating on whatever range the grid has selected.

**Got:** `SortRangeDialog`, `FilterRangeDialog`, and the wiring code
(selection → `CellRange`, dialog → engine call, row-visibility
refresh on every `OnCellsChanged`).

**Changed:** nothing about the generated design, but added an explicit
comment documenting a limitation it introduced without flagging: only
one filtered range is tracked client-side (`_activeFilterRange`/
`_activeFilterColumns`), and undoing a filter command through Ctrl+Z
does not resync that bookkeeping, because `FilterManager` has no
"list active filters" query the GUI could rebuild its state from.

**Why:** building a fully filter-history-aware GUI would mean adding a
query method to `FilterManager` purely to serve the demo client, which
is more surface area on the engine for a corner the brief doesn't
actually require to be perfect. Documenting the limitation explicitly,
in the code, matches the project's own stated philosophy — a documented
trade-off earns marks, a silent one loses them — so the choice was to
cut the corner visibly rather than either hide it or over-build for it.

---

## 4 — Benchmark harness

**Asked for:** `CalcEngine.Benchmarks`, measuring the brief's two hard
targets (propagation < 50ms, full recalculation < 2000ms) against a
generated 100,000-cell workbook with a 500-cell dependency chain.

**Got:** a first version where the 99,500 "filler" cells (everything
outside the 500-cell chain) were plain literals. It ran and reported
full recalculation at **0.8ms** — comfortably passing.

**Changed:** rejected that result and changed the filler cells from
literals (`"0"`) to trivial independent formulas (`"=0"`).

**Why:** `CalculationEngine.RecalculateAll` only recomputes cells where
`Cell.IsFormula` is true. With literal filler, "full recalculation of
the 100,000-cell workbook" was secretly only recomputing the 500-cell
chain — the other 99,500 cells were never touched at all, so the
benchmark would have passed without proving anything about scale. The
0.8ms number was suspicious precisely *because* it was so fast for
"100,000 cells," which is what prompted checking `RecalculateAll`'s
actual filter condition instead of accepting the passing number.
After the fix, the honest number is **~18–19ms** — still a wide margin
under the 2000ms target, but now an actual measurement of evaluating
~100,000 expression trees, not ~500.

---

## 5 — Design portfolio: ADT specification for `RangeExpression`

**Asked for:** the representation invariant for `RangeExpression`, as
part of writing the design portfolio's ADT specifications section.

**Got:** a first draft that reused the pre-existing source code
comment on `RangeExpression`, which claimed: "appears only as a direct
function argument, never as an operand of a `BinaryExpression` or
`UnaryExpression`."

**Changed:** before writing that claim into a graded document, wrote a
throwaway test parsing `=B2:B3+1`. It parsed successfully and
evaluated to `#VALUE!` — directly contradicting the comment, since the
grammar's `atom` rule admits a bare `RANGE` anywhere any other atom is
legal. Corrected both the stale comment in `RangeExpression.cs` and
the portfolio's ADT section to state the actual (weaker, but true)
invariant, and to explain that `#VALUE!` — not a parse-time rejection
— is what makes that case well-defined.

**Why:** the brief specifically grades ADT specifications, and an
invariant that reads well but isn't true is worse than no invariant at
all — it's exactly the kind of thing an oral defence question would
catch. Verifying against the real grammar before writing, rather than
trusting an existing comment (AI-written or not) at face value, is the
whole point of this exercise.

---

## 6 — Design portfolio: semantics table surfaced a crash

**Asked for:** the semantics reference table (§5 of `docs/portfolio.md`)
— documenting, for every operator/function/error path, what the code
actually does, cross-checked against the implementation rather than
against what it was supposed to do.

**Got:** while tracing the "reference to a cell outside the sheet"
row of that table, found that the `CELLREF` grammar token
(`LETTERS DIGITS`) accepts any digit string as a row, including `0`.
`=A0+1` therefore parses successfully at the ANTLR level;
`ExpressionTreeBuilder` then calls `CellRef.Parse("A0")`, which throws
`FormatException` — uncaught, propagating all the way out of
`CalculationEngine.SetCellContent` to the client. Confirmed with a
reproduction test before fixing.

**Changed:** wrapped `ExpressionTreeBuilder().Visit(tree)` inside
`FormulaInputParser.Parse` in a `try`/`catch` for
`FormatException`/`ArgumentException`, converting either into a
`FormulaParseResult.Failure` with a client-facing message.

**Why:** same rule as finding 2 — "malformed input is normal input,
never an exception that escapes to the client" — except this time the
gap was in the boundary between syntactic validity (the grammar accepts
`A0`) and semantic validity (row 0 is outside the sheet), which the
existing error-collector machinery didn't cover because it only catches
*syntax* errors, not exceptions thrown *after* a successful parse.
Two regression tests added (`CELLREF` and `RANGE` cases) before
considering this done.

---

## 7 — Design portfolio: `DependencyGraph`/`CommandManager` ADT specs

**Asked for:** abstraction functions and representation invariants for
`DependencyGraph` and `CommandManager`, following the same
"verify against the code, don't just describe intent" approach used in
entry 5.

**Got:** AF/RI text for both, including the "two mirrored maps must
always agree" invariant for `DependencyGraph` and the "`undoStack.Count
<= 100`, oldest evicted on overflow" invariant for `CommandManager`.

**Changed:** kept as generated — cross-checked each invariant clause
against `AddEdge`/`RemoveIncomingEdges` and `ExecuteCommand`/`Undo`/`Redo`
directly and found no discrepancy, unlike entries 5 and 6.

**Why:** logged deliberately as a "no correction needed" entry, for
balance — this log should show what was checked and held up, not only
what was checked and broke. A log containing only rejections would be
just as unrepresentative as one containing none.
