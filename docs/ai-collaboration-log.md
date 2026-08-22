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
| 8 | 2026-07-17 | William | Claude | Clarified the project structure regarding whether the deliverable is an API vs. a GUI, and if they should live in separate repos. Claude explained the engine is a headless C# library consumed by a separate GUI project. The team decided to use a single solution containing both projects to avoid fragmenting the Git history required by the rubric. |
| 9 | 2026-07-17/18 | William | Claude | Requested a rough timeline and a strategy to split the 5 required modules across a 5-person team. Claude provided a week-by-week timeline and a primary-ownership-plus-rotation structure. The team used this output purely for reference and did not formally follow it. |
| 10 | 2026-07-18 | William | Claude | Requested a draft ANTLR grammar (`Formula.g4`) for the formula language covering numbers, text, references, operators, and the 8 required functions. Claude provided a complete left-recursive grammar. The team used it only for conceptual understanding and independently implemented a custom grammar with better operation precedence and unary/atom non-terminals. |
| 11 | 2026-07-18 | William | Claude | Asked how to wire the ANTLR grammar into a Visual Studio C# project. Claude provided step-by-step setup instructions using `Antlr4.Runtime.Standard` and `Antlr4BuildTasks`, alongside a draft xUnit test. The team adopted the build-time generation setup but decided to use MSTest instead of xUnit. |
| 12 | 2026-07-18/19 | William | Claude | Attempted to debug ANTLR token recognition errors where specific literals silently failed. Claude incorrectly diagnosed the issue as smart/curly quotes replacing ASCII quotes. The team independently discovered the true root cause was a syntax misunderstanding after the AI sent them in circles. |
| 13 | 2026-07-27 | William | Claude | Asked why the generated MSTest parser suite using `[DataTestMethod]` and `Assert.ThrowsException<T>` didn't compile. Claude explained that these methods were deprecated or removed entirely in MSTest v4. The team rejected the initial AI-generated test code and used the explanation to ensure all future tests targeted the updated v4 API surface. |
| 14 | 2026-07-27 | William | Claude | Requested guidance on structuring specification and test-writing across a 5-person team using MSTest. Claude proposed a shared-interfaces-first pass, a category breakdown per module, and team conventions. The team used this as a reference for their final formatting standard. |
| 15 | 2026-07-27 | William | Claude | Asked for group class diagramming tool recommendations. Claude suggested the text-based, git-trackable Mermaid tool and generated a first diagram for the Formula Representation module. The team chose not to use this output at all. |
| 16 | 2026-08-22 | William | Claude | Asked Claude to check the team's GitHub repository and explain its contents for orientation purposes. Claude provided a high-level summary identifying the separation between the C# class library (`CalcEngine.Core`), the WinForms client (`CalcEngine.Gui`), and the tests. |
| 17 | 2026-08-22 | William | Claude | Requested an explanation of `CalcEngine.Core` and a proposal to reorganize its ~55 flat files. Claude walked through the pipeline and proposed a modular folder layout (e.g., `Model/`, `Parsing/`, `Functions/`). The team essentially adopted the layout and used `git mv` to preserve commit history. |
| 18 | 2026-08-22 | William | Claude | Pasted `Form1.cs` and asked why the grid's row headers were blank while column headers showed letters. Claude correctly identified that the existing code setting the header values looked sound on paper and requested a screenshot to verify the actual symptom. |
| 19 | 2026-08-22 | William | Claude | Provided the requested screenshot showing the blank row headers. Claude incorrectly suggested the default rendering was unreliable and proposed a complex fix using the `RowPostPaint` event. The team rejected this and independently fixed the issue by simply widening the `RowHeadersWidth` property. |
| 20 | 2026-08-22 | William | Claude | Asked for a gap analysis between the GUI and the core engine. Claude methodically cross-checked the code and discovered that the `IRowFilter` and `ISortComparer` implementations for the Group C Sorting & Filtering feature were completely unwired in the GUI, which the team flagged as an open task. |
| 21 | 2026-08-22 | William | Claude | Asked how to turn the local reorganization and GUI fixes into a pull request using standard git workflows. The team followed the provided instructions but ran into a "permission denied" push access error on the repository. |
| 22 | 2026-08-22 | William | Claude | Requested a workaround to open a PR without direct push access. Claude provided a fork-and-PR workflow (adding the original repo as a second remote and targeting the base), which the team successfully used to open the PR. |
| 23 | 2026-08-22 | William | Claude | Asked for commit message suggestions for the local changes. Claude recommended splitting the unrelated file reorganization and GUI fixes into two separate commits. The team did this, but manually rewrote the GUI commit message to reflect the actual `RowHeadersWidth` fix rather than the AI's failed `RowPostPaint` idea. |
| 24 | 2026-08-22 | William | Claude | Asked how to add an exponentiation (`^`) operator to the formula language. Claude provided a four-file design involving a new `power` rule in `Formula.g4`, a new `BinaryOperator` enum, and evaluation overrides. The team implemented this initially, but it failed to work on the first try. |
| 25 | 2026-08-22 | William | Claude | Reported that the exponentiation implementation resulted in red error cells. Claude reviewed its previous answer and caught its own bug: it had failed to instruct the team to update `VisitUnary` to reference the newly renamed `context.power()` rule. Applying this correction resolved the issue. |
| 26 | 2026-08-22 | William | Claude | Asked Claude to generate an AI collaboration log encompassing all previous interactions. Claude generated a custom markdown log, which the team discarded in favor of appending the data directly into their existing table format for consistency. |
| 27 | 2026-08-21 | William | Gemini | Asked Gemini to critique the GitHub repository implementation. Gemini summarized the required core technical architecture (ANTLR, DAG, Strategy pattern), performance benchmarks (like sub-50ms propagation for 500 cells), and the required deliverables (including the Design Portfolio and demo video).|
| 28 | 2026-08-21 | William | Gemini | Requested help generating test values for GUI features. Gemini provided 8 comprehensive test scenarios validating algebraic precedence, built-in functions, logical short-circuits, LOOKUP vector matching, domain error fault tolerance, reactive recalculation, circular references, and the Command pattern's undo/redo stack.|
| 29 | 2026-08-21 | William | Gemini | Asked for guidance on recording the required 5-minute demo video. Gemini provided a structured minute-by-minute script covering architecture introductions, formula language, reactive recalculation benchmarks, fault tolerance, and custom features, along with technical recording tips.|
| 30 | 2026-08-21 | William | Gemini | Provided a list of 6 bugs discovered during testing and requested corrections. Gemini provided root-cause analysis and code fixes for input handling, error preservation via a tagged union `CellValue`, adding the `^` operator to ANTLR, and ADT specifications. The team adopted only the ANTLR grammar fix, resolving the remaining errors manually.|
| 31 | 2026-08-21 | William | Gemini | Pasted `Form1.cs` to request a plain-English explanation of the WinForms code and GUI fixes. Gemini explained the Observer pattern and provided updated `OnCellsChanged`, `OnCircularReference`, and `OnCellEndEdit` methods. The team used this breakdown to manually patch only the `OnCircularReference` method.|
| 32 | 2026-08-21 | William | Gemini | Asked for a complete LOOKUP function implementation. Gemini provided a full `LookupFunctionStrategy.cs` drop-in class implementing 1D vector extraction and approximate matching. The team rejected the code due to its structural assumptions about the codebase and requested architectural building blocks instead.|
| 33 | 2026-08-21 | William | Gemini | Requested the architectural building blocks for LOOKUP instead of full code. Gemini outlined 5 fundamental components: the Strategy Contract, Evaluation Context and Range Extractor, Vector Shape Inspector, Strongly Typed Value Comparator, and an Approximate Match Search Algorithm.|
| 34 | 2026-08-21 | William | Gemini | Pasted `Cell.cs` and asked to add missing XML comments. Gemini returned updated C# XML documentation, explicitly adding the required Abstraction Function (AF) and Representation Invariant (RI) clauses to the class remarks. The team fully adopted this specification.|

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
