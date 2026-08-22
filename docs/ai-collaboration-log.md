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

| #  | Date | Member | Tool | Summary |
|----|---|---|---|---|
| 1  | 2026-08-21 | Sherif | Claude (Sonnet 5, Claude Code) | Implemented Sorting & Filtering (Group C feature) end to end |
| 2  | 2026-08-21 | Sherif | Claude (Sonnet 5, Claude Code) | Self-review of the sort/filter implementation found and fixed two real bugs |
| 3  | 2026-08-22 | Sherif | Claude (Sonnet 5, Claude Code) | Wired Sort/Filter into the GUI; flagged an undo/redo desync it introduced |
| 4  | 2026-08-22 | Sherif | Claude (Sonnet 5, Claude Code) | Built the benchmark harness; first version's numbers were caught as meaningless |
| 5  | 2026-08-22 | Sherif | Claude (Sonnet 5, Claude Code) | Design portfolio ADT specs — a copied invariant claim was checked and found false |
| 6  | 2026-08-22 | Sherif | Claude (Sonnet 5, Claude Code) | Design portfolio semantics table — surfaced an unhandled-exception bug (`=A0+1`) |
| 7  | 2026-08-22 | Sherif | Claude (Sonnet 5, Claude Code) | ADT specs for `DependencyGraph`/`CommandManager` accepted as generated |
| 8  | 2026-07-17 | William | Claude | Clarified the project structure regarding whether the deliverable is an API vs. a GUI, and if they should live in separate repos. Claude explained the engine is a headless C# library consumed by a separate GUI project. The team decided to use a single solution containing both projects to avoid fragmenting the Git history required by the rubric. |
| 9  | 2026-07-17/18 | William | Claude | Requested a rough timeline and a strategy to split the 5 required modules across a 5-person team. Claude provided a week-by-week timeline and a primary-ownership-plus-rotation structure. The team used this output purely for reference and did not formally follow it. |
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
| 35 | 2026-07-31 | Abdulazeez | claude | Asked for ANTLR setup guidance and a project roadmap; caught that the roadmap omitted git repo initialization needed for timestamped history |
| 36 | 2026-08-02 | Abdulazeez | claude | Design Portfolio — first pass, then an audit caught a broken grammar, five missing ADT specs, a missing Observer pattern, and a Cell.Value : double that couldn't hold errors or text |
| 37 | 2026-08-31 | Abdulazeez | claude | Foundation ADTs (CellRef, CellValue, DependencyGraph) built test-first, 44 tests green |
| 38 | 2026-08-31 | Abdulazeez | claude | Grammar, expression tree, and function strategies; caught two test-first order violations and logged them |
| 39 | 2026-08-31 | Abdulazeez | claude | Cell/Workbook/Observer/CalculationEngine facade + Command pattern; caught a namespace collision, an infinite-recursion risk, and an empty-undo edge case |
| 40 | 2026-08-31 | Abdulazeez | claude | Data Validation feature; surfaced and fixed a latent CommandManager stack-desync bug it would have exposed |
| 41 | 2026-08-31 | Abdulazeez | claude | WinForms GUI scaffolded; three real bugs found in testing, one of them a misdiagnosis worth logging on its own |
| 42 | 2026-08-22 | Peter | Gemini | Evaluated project codebase to identify missing JSON persistence module; implemented WorkbookSerializer (SaveToJSON/LoadFromJSON) and CellDTO positional record.
| 43 | 2026-08-22 | Peter | Gemini | Resolved deferred formula evaluation during JSON loading by removing BeginBatch() and EndBatch() wrappers from LoadFromJSON to guarantee immediate synchronous cell state restoration.
| 44 | 2026-08-22 | Peter | Gemini | Created WorkbookSerializerTests in xUnit and configured UnsafeRelaxedJsonEscaping in JsonSerializerOptions to prevent arithmetic operators (+) in formulas from HTML-escaping during serialization.
| 45 | 2026-08-22 | Peter | Gemini | Fixed null deserialization bindings in CellDTO record by adding [property: JsonPropertyName] attributes and enabling PropertyNameCaseInsensitive for System.Text.Json primary constructor reflection.
| 46 | 2026-08-22 | Peter | Gemini | Diagnosed background testhost.exe assembly locks holding stale DLLs in memory, terminating test runner processes and forcing a non-incremental rebuild to reach 440/440 passing tests.
 47 | 2026-08-22 | Kamal | Zed Agent (GPT-5.6 Sol) | Wrote a specification for `VisitPower`; the user rejected the Requires/Postcondition style and the direct file edit |
| 48 | 2026-08-22 | Kamal | Zed Agent (GPT-5.6 Sol) | Reviewed a hand-written `ParseNumber` specification; the user rejected a revision for leaking implementation detail |
| 49 | 2026-08-22 | Kamal | Zed Agent (GPT-5.6 Sol) | Regenerated all CalcEngine.UML class diagrams after the prior `.puml` files were gone; the render pass surfaced unresolved syntax errors |
| 50 | 2026-08-22 | Kamal | Gemini | Diagnosed and attempted a fix for the `expressions.puml` syntax error via Gemini; one of two explanations flagged as unverified |
| 51 | 2026-08-22 | Kamal | Gemini | Converted the portfolio's eight Mermaid class diagrams to PlantUML via Gemini; rejected as stale against the current source |
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


35 — ANTLR setup and project roadmap

Asked for: whether ANTLR needed to be installed on the system or added as a project dependency, and a rough roadmap for the CalcEngine project split into stages.

Got: a two-part answer (NuGet runtime package Antlr4.Runtime.Standard for the code, plus Antlr4.CodeGenerator for build-time .g4 compilation, avoiding a separate Java install) and a full staged roadmap covering grammar, class diagrams, ADT specs, and implementation order.

Changed: the roadmap as given had no step for initializing the git repository. This was flagged back to Claude directly ("you forgot to add creating a repo so it will have time stamps"), and the missing step was added before any other work started — repo init, first commit, and a plan to commit each design artifact separately so the git history would show design-first-then-code, not everything landing in one commit at the end.

Why: the brief requires git history to demonstrate the project was built progressively rather than assembled and backdated. A roadmap that skips the one step that produces that evidence would have meant losing marks for a reason that had nothing to do with the actual engineering. Catching it before writing any code was cheap; catching it after would not have been.

36 — Design Portfolio: first pass and audit

Asked for: the 20-mark Design Portfolio deliverable — formal ANTLR grammar, UML class diagrams, and ADT specifications (abstraction functions and representation invariants).

Got: an initial draft, followed later by a full audit pass that surfaced it was significantly incomplete: abstraction functions and representation invariants were missing for five ADTs (Expression Tree, DependencyGraph, Workbook, CommandManager, CellValue); the grammar was missing the =, <>, <=, >= operators and had no entry rule or whitespace rule; only 4 of the 8 required functions were specified; the Observer pattern and its sequence diagrams were absent entirely; and Cell.Value was typed as a bare double, which cannot represent an error state or a text value.

Changed: all of the above were fixed in a rebuilt portfolio. Separately, the HasCycle ADT spec initially carried a precondition that SetDependencies must have been called at least once first. This was checked against the actual behaviour rather than accepted — HasCycle on an empty graph just returns false, no cycle, trivially — so the precondition was dropped and the spec corrected to Requires: True.

Also logged as friction rather than worked around silently: default phrasing repeatedly needed redirecting toward shorter, plainer sentences, and the model repeatedly stopped at optional-sounding checkpoints ("want to stop here?") instead of producing the finished document, requiring several explicit follow-ups before the actual .docx — not a description of one — was delivered.

Why: a portfolio missing half its required ADT specs and a quarter of its grammar would have been graded as incomplete regardless of how good the parts that existed were, so the audit pass mattered as much as the original draft. The HasCycle precondition is the same category of error as later entries in this log — an invariant that reads plausibly but doesn't match the code — caught by checking, not trusting the draft.

37 — Foundation ADTs: CellRef, CellValue, DependencyGraph

Asked for: the first implementation units, test-first: CellRef (bijective base-26 A1-notation parsing), CellValue (an immutable 5-state tagged union with a private constructor and factory methods), and DependencyGraph (mirrored precedents/dependents adjacency maps, cycle detection returning the exact cycle path, affected-cells lookup via reverse BFS, topological sort via Kahn's algorithm).

Got: all three, reaching 44 passing tests, built one unit at a time with a test: commit before each feat: commit.

Changed: nothing rejected in this session specifically — the notable output was process discipline imposed on how work got delivered: complete files to copy rather than diffs, one unit at a time rather than several at once, no rebasing of pushed commits, and dotnet test run and confirmed green before every commit.

Why: logged deliberately as a clean entry, for the same reason entry 7 in this log exists — a log of only corrections would misrepresent how much of the collaboration just worked. The process rules set here (test-first commit ordering, no history rewrites) became the standard the rest of the project was held to, including the violations caught in entry 38.

38 — Grammar, expression tree, and function strategies

Asked for: the ANTLR grammar matching the Design Portfolio's grammar sections exactly, the full IExpression hierarchy (8 node types implementing Composite + Interpreter), ExpressionTreeBuilder, DependencyVisitor for dependency extraction, and the 8 required functions (SUM, AVERAGE, MIN, MAX, COUNT, IF, ROUND, LOOKUP) as Strategy + Factory.

Got: all of it — grammar verified against 23/23 parse tests, the full expression hierarchy, and all 8 function strategies. Test suite grew from 44 to 126.

Changed: caught Claude delivering a working implementation file before its matching test file, twice, breaking the test-first ordering that entry 37 had established. Both were flagged in the moment rather than let slide, written up as entries in a separate AI Critique Log, and the correct order (test commit, confirm red, then implementation commit) was re-enforced for the rest of the session.

Why: the whole point of the test-first commit ordering is that the git history itself is graded evidence of test-first development — an implementation committed ahead of its test doesn't just violate a preference, it quietly falsifies the evidence the rubric is checking for. Catching both instances and logging them (rather than fixing the commit order silently) keeps that evidence honest.

39 — Cell/Workbook/Observer/CalculationEngine facade + Command pattern

Asked for: the remaining core units built one at a time, test-first: Cell, Workbook (sparse dictionary storage + IEvalContext), the Observer pipeline (CellChangeSet, ICellObserver, ChangeNotifier), the ANTLR error-handling wrapper (ErrorCollector, FormulaParseResult, a formula-input parser), the CalculationEngine facade, and finally ICommand/SetCellCommand/ CommandManager for undo-redo.

Got: all six units, tests climbing 126 → 180 → 209 → 241 → 245 → 278 → 317 (329 after later fixes).

Changed: four separate issues caught during this session, not after:

a hand-written class named FormulaParser collided with the ANTLR-generated CalcEngine.Core.Generated.FormulaParser. A using alias inside the new file protected only that file — it didn't protect ExpressionTreeBuilder.cs, which referenced the generated type by bare name in the same namespace and broke with 17 CS0426 errors, because C# resolves a bare type name against the current namespace before it even looks at using imports. Fixed by renaming to FormulaInputParser, with a rule written down afterward: grep the whole repo for a new class name before using it, not just check the new file.
SetCellCommand.Execute() couldn't call the public SetCellContent(), because that method now itself routes through CommandManager — calling it from inside a command would recurse indefinitely. Built to call the internal ApplyEdit() directly instead.
Undo() on an edit that started from an empty cell can't route through SetCellContent(""), because the grammar rejects an empty string as valid input. Caught during design and routed through ClearCell() instead.
a first integration attempt failed to compile because only some of the four new Command files had been copied into CalcEngine.Core/; fixed by making sure all four landed together before rebuilding.

Why: the first three are bugs that wouldn't have shown up in a first happy-path test — they'd have surfaced as a build break in an unrelated file, a stack overflow the first time a command actually executed, or a crash the first time anyone undid an edit on a previously-empty cell. Catching them at design time, before the recursive call or the bad namespace collision ever ran, meant none of them had to be debugged blind later.

40 — Data Validation and a latent CommandManager bug

Asked for: the Data Validation feature (Group C) — ValidationResult, IValidationRule, RangeRule, ListRule, TypeRule, CustomFormulaRule, ValidationRegistry, wired into CalculationEngine.ApplyEdit — test-first, same discipline as before.

Got: the full feature, and mid-session, a pre-existing bug in CommandManager was surfaced: ExecuteCommand, Undo, and Redo were pushing and popping the undo/redo stacks unconditionally, regardless of whether the underlying edit had actually succeeded.

Changed: added a result.Success guard across all three CommandManager methods, verified with four new tests. Also added a distinct CellChangeSet.ValidationFailed case (a new ChangeFailureReason enum) instead of reusing the existing ParseFailure case, so the GUI can tell a validation rejection apart from a syntax error. 375 tests green after both changes.

Why: the stack bug had been harmless up to this point because every prior edit either parsed successfully or failed to parse — there was no "parsed fine, then got rejected anyway" case yet. Data Validation makes that case the normal one, so the bug would have started desyncing CanUndo/CanRedo from the real workbook state on essentially every declined edit if it had shipped un-fixed. It was existing latent risk that this feature was specifically going to trigger, not a new bug this feature introduced.

41 — GUI scaffolding (CalcEngine.Gui)

Asked for: a WinForms GUI — a grid, formula bar, undo/redo buttons, a status bar for error messages, a right-click menu to attach a RangeRule to a cell, wired to the engine through the existing Observer pattern rather than polling for changes.

Got: a CalcEngine.Gui project with a 100-row × 26-column DataGridView grid and all of the above.

Changed: three real bugs found during manual testing, not in the first-draft code:

right-click didn't move CurrentCell before the context-menu handler ran, so a validation rule got attached to whatever cell was previously selected, not the one actually right-clicked.
Controls.Add ordering hid the status bar behind the grid.
header text appeared blank in a way that looked like a DataGridView rendering bug; the actual cause was a stale build, not incorrect code. This misdiagnosis was written up separately in the AI Critique Log rather than just quietly fixed and forgotten.

Why: the first two are the ordinary cost of wiring a UI to underlying state — worth logging as concrete, reproducible bugs with a named cause, not vague "polish." The third is worth logging for a different reason: the first diagnosis (a framework rendering quirk) sounded technically plausible and was wrong, and the actual fix — a rebuild — was far simpler than the theory. It's a reminder to rule out "did the code even rebuild" before reasoning about deeper causes.

**42 — Project gap analysis & WorkbookSerializer design**

**Asked for:** A gap analysis of missing features between the project specification and codebase, followed by the initial architecture and implementation of `WorkbookSerializer` (`SaveToJSON`/`LoadFromJSON`) and its Data Transfer Object (`CellDTO`).

**Got:** Gap analysis identifying the missing JSON persistence module required for saving and restoring workbook state; complete implementation of `WorkbookSerializer` using `System.Text.Json` to extract non-empty cells (`cell.RawInput`) mapped to A1 references (`cell.Ref.ToA1()`), clear state via `engine.Clear()`, and restore contents via `engine.SetCellContent`.

**Changed:** Retained `CellDTO` as an immutable C# positional `record` (`record CellDTO(...)`) rather than mutating it into a traditional class with parameterless constructors and mutable properties.

**Why:** Positional records provide concise, thread-safe, and immutable data transfer without boilerplate setter methods, preserving modern C# design principles.

---

**43 — Synchronous state restoration in LoadFromJSON**

**Asked for:** Debugging engine state restoration during JSON loading where restored formula cells failed to evaluate immediately post-load.

**Got:** Diagnosis showing that `LoadFromJSON` originally wrapped `SetCellContent` calls inside `engine.BeginBatch()` and `engine.EndBatch()`, deferring recalculation and observer updates until batch completion.

**Changed:** Removed `BeginBatch()` and `EndBatch()` wrappers from `LoadFromJSON`, allowing `engine.SetCellContent` to evaluate each cell synchronously as it is restored from JSON.

**Why:** Batching deferred cell state commits, causing post-load formula assertions (`Assert.Equal(...)`) to evaluate against uncommitted state. Synchronous restoration guarantees that every cell formula and dependency edge is immediately computed upon deserialization.

---

**44 — xUnit test creation & JSON formula encoding fix**

**Asked for:** Creation of an xUnit test suite (`WorkbookSerializerTests`) to verify save/load persistence, handling of empty cells, missing file handling, and preservation of raw formula text.

**Got:** Initial test suite, but `SaveToJSON_WritesPopulatedCellsToFile` failed because `System.Text.Json`'s default security encoder escaped mathematical operators (converting `=A1+A2` into `=A1\u002BA2`).

**Changed:** Configured `JsonSerializerOptions` in `WorkbookSerializer` with `JavaScriptEncoder.UnsafeRelaxedJsonEscaping`.

**Why:** Default JSON security rules convert arithmetic characters (`+`, `<`, `>`) into HTML unicode escape sequences, corrupting formula strings when written to disk and causing parser errors when loaded back into the engine.

---

**45 — System.Text.Json record binding fix**

**Asked for:** Fixing `LoadFromJSON` deserialization where `CellDTO` instances were instantiated with `null` values for `Reference` and `Input`, skipping cell restoration entirely.

**Got:** Diagnosis revealing a `System.Text.Json` constructor binding mismatch when reflection attempts to map JSON keys to positional record parameters during deserialization.

**Changed:** Applied explicit positional attributes (`[property: JsonPropertyName("...")]`) to `CellDTO` record parameters and set `PropertyNameCaseInsensitive = true` on `JsonSerializerOptions`.

**Why:** System.Text.Json constructor reflection fails to map JSON property keys to primary constructor parameters on records without explicit property targeting attributes, resulting in silent `null` deserialization.

---

**46 — Process assembly lock & MSBuild test runner cleansing**

**Asked for:** Debugging persistent test failures in `WorkbookSerializerTests` where source code fixes appeared to have no effect when running `dotnet test`.

**Got:** Root-cause diagnosis showing background `testhost.exe` test runner processes held `CalcEngine.Core.dll` locked in memory, preventing `dotnet build` from overwriting binaries in `bin/` and `obj/`.

**Changed:** Terminated background test host processes via PowerShell (`Stop-Process`), purged `bin/` and `obj/` build directories, and executed `dotnet build --no-incremental` to force a clean re-compilation, reaching 440/440 passing tests.

**Why:** Diagnosing background assembly locking prevented chasing phantom code bugs in valid C# logic when the underlying issue was a stale DLL locked on disk by the xUnit runner.

---

## 47 — `VisitPower` specification style corrected after rejection

**Asked for:** a specification comment for the newly added
`ExpressionTreeBuilder.VisitPower` method (built from the grammar's new
`power` production, `power : atom ('^' unary)?`), "using the format used
in the project."

**Got:** the assistant read `ExpressionTreeBuilder.cs` and the design
portfolio's ADT-specification style (§4, written in explicit
**pre**/**post**condition prose), and applied that formal style directly
to the method as an XML `<remarks>` block with `<b>Precondition:</b>`/
`<b>Postcondition:</b>` paragraphs, then used a direct file edit to write
it into `ExpressionTreeBuilder.cs`.

**User rejection:** the user rejected both the style and the action:
"you should use xml comments as a way to write it, not requires,
effects and modifies as the specification pattern is not coherent with
the other modules," and, separately, after a follow-up edit failed to
match the file's current text, said "don't write to the buffer, just
give me a specification in that format" rejecting the direct-edit
workflow itself, not only the wording.

**Changed:** discarded the Requires/Postcondition remarks block.
Rewrote the documentation using the same plain `<summary>`/`<param>`/
`<returns>` shape already used by the neighbouring `VisitUnary` method
(which the user supplied as the reference example), and returned it as
text in the conversation rather than applying it to the file.

**Why:** the portfolio's formal pre/postcondition language is reserved
for the ADT specification document, not scattered through method-level
XML doc comments — every other visitor method in the file documents
itself with plain prose, so a differently structured comment on one
method would read as inconsistent rather than more rigorous. The second
rejection was procedural: this was a specification-writing exercise, not
an authorized code change, so nothing should be written to the file
until the user has reviewed and accepted the wording.
 
---

## 48 — `ParseNumber` specification refined after rejection

**Asked for:** a review of a specification the user had written
themselves for the private helper `ParseNumber(string): double` — "How's
this specification for this method."

**Got:** feedback that the draft was serviceable but circular ("Parses a
number that is written as text into a number") and missing a `<param
name="text">` entry, plus a suggested rewrite that additionally
explained *why* the parse is culture-consistent, naming
`CultureInfo.InvariantCulture` directly in the summary text.

**Changed:** removed the culture reference from the summary and rewrote
it as "Converts a numeric literal from a formula into its numeric
value," keeping only the added `<param>` tag as the genuine improvement
over the user's original draft.

**Why:** a specification should describe observable behaviour only, so
it stays valid if the implementation changes — if invariant-culture
parsing were later replaced by a hand-rolled parser, this wording would
not need to change, whereas the earlier draft would have gone stale.
This matches the project's own ADT-specification convention (§4 of the
portfolio), which is written in terms of what an operation guarantees,
never how it is coded.
 
---

## 49 — UML class diagrams regenerated; two diagrams left unresolved

**Asked for:** a fresh set of `.puml` class diagrams for
`CalcEngine.UML`, with visible, non-crossing, well-spaced orthogonal
connectors, after confirming that the diagrams built earlier in this
project's history no longer existed on disk.

**Got:** since no diagram source could be reused, the assistant
re-inventoried the current codebase directly — Model, Expressions,
Parsing, Dependencies, Engine, Commands/ChangeTracking, Serialization,
Functions, Validation, Sorting, Filtering, and the GUI — rather than
reconstructing the diagrams from memory of the earlier versions. That
inventory surfaced API surface added since the diagrams were last
written: `BinaryOperator.Power` and the grammar's `power` production,
the new `Serialization` folder (`WorkbookSerializer`, `CellDTO`),
`CalculationEngine.Workbook` and `CalculationEngine.Clear()`, and
`CommandManager.Clear()`. Twelve `.puml` files were then written — the
previous eleven plus a new `serialization.puml` — using orthogonal
routing, wide `nodesep`/`ranksep`, and hidden layout edges to keep
connectors in separate lanes, following the approach already validated
for `expressions.puml` and `parsing.puml` earlier in the project.

**Changed:** created `engine.puml`, `model.puml`, `dependencies.puml`,
`serialization.puml`, `expressions.puml`, `parsing.puml`,
`functions.puml`, `commands_changetracking.puml`, `sorting.puml`,
`filtering.puml`, `validation.puml`, and `gui.puml`. None have been
confirmed to render cleanly yet; `functions.puml` and `expressions.puml`
need a follow-up fix before they can be treated as finished.

**Why:** verifying the diagrams against the live source rather than
trusting the earlier diagram set — which predates `Power`,
`Serialization`, and both `Clear()` methods — avoids shipping UML that
misrepresents the current design, the same principle the ADT
specification work in earlier entries was built on. Logging the render
failures instead of silently retrying keeps the log honest about the
diagrams' actual state, consistent with the file's stated purpose of
recording rejections and unresolved issues, not only successes.

## 50 — `expressions.puml` syntax error diagnosed via Gemini; one of two explanations unverified

**Asked for:** a diagnosis of the `Error line 30` PlantUML syntax
failure that entry 9 left unresolved, by pasting the full
`expressions.puml` source into Gemini and asking what was wrong, then
narrowing the question to "The error is on line 30,"
**Got:** two candidate causes. The first: the
`<<IExpressionVisitor<string>>>` and `<<IExpressionVisitor<IExpression>>>`
stereotypes on `FormulaPrinter` and `ReferenceTranslationVisitor` end in
three consecutive `>` characters, and Gemini says PlantUML's parser
closes the stereotype on the first `>>` it finds, leaving a stray `>`
that breaks the rest of the line. The second: a claim that the file's
indentation contains invisible non-breaking-space (U+00A0) characters,
which Gemini says a strict PlantUML parser cannot read as ordinary
whitespace. Gemini's final "fully corrected" version added a space
inside every generic stereotype (`<< IExpressionVisitor<string> >>`)
and expanded every single-line leaf-node class body —
`class NumberExpression { + Value : double }` and its four siblings —
onto multiple lines.

**Changed:** nothing has been applied back to
`CalcEngine.UML/expressions.puml` yet, and the corrected file has not
been re-rendered to confirm the error is actually gone. The
stereotype-spacing fix is worth keeping the greedy `>>` match Gemini
describes is a real, previously-documented PlantUML parsing gotcha, and
both `FormulaPrinter` and `ReferenceTranslationVisitor` do use exactly
that pattern. The non-breaking-space explanation is not credible as-is:
the file was produced by a coding tool writing plain ASCII text
directly, not pasted in from a word processor or web page, which is the
usual source of stray NBSP characters, and Gemini had no way to inspect
the file's actual bytes — it inferred this purely from the symptom. It
is also not a complete explanation on its own, since `functions.puml`
failed at the same line number with none of the nested-generic
stereotypes this diagnosis targets, which the "do it for me" fix does
not address at all.

**Why:** logging the NBSP claim as unverified rather than folding it
into the fix silently matches the same standard entries 5 and 6 already
set for this project ,a plausible-sounding explanation from an AI
assistant is not evidence on its own, and this project's practice has
been to re-derive or test a claim before writing it down as fact. Since
`functions.puml`'s identical line-30 failure is still unexplained by
either theory, this fix should be treated as a partial, unconfirmed
lead rather than a resolved bug; the next step is to apply only the
stereotype-spacing change and re-run the PlantUML render pass entry 9
stopped at, rather than accepting the whole diagnosis at face value.
 
---

## 51 — Portfolio Mermaid diagrams converted to PlantUML via Gemini; flagged as stale against the current source

**Asked for:** PlantUML equivalents of the eight Mermaid class diagrams
already committed in `docs/portfolio.md` §3 (Parsing; Expression tree;
Evaluation and functions; Dependency graph; Commands; Data validation;
Sorting and filtering; Engine facade), by pasting the Mermaid source
directly and asking Gemini to "Generate the .puml code equivalent of
all this."

**Got:** eight separate `@startuml` blocks, one per portfolio
subsection, produced by mechanically translating Mermaid syntax to
PlantUML: `~T~` generics became `<T>`, the trailing `$` Mermaid uses for
static members became `{static}`, and the stereotype-spacing fix from
entry 10 was applied throughout to avoid the same nested-generic parsing
issue.

**Changed:** none of the eight blocks were written into `CalcEngine.UML`
or rendered. They were not adopted as replacements for the twelve
`.puml` files already authored directly against the source code in
entry 9, because they are a direct transliteration of the Mermaid
diagrams' text and those Mermaid diagrams predate the same code
changes entry 9's inventory found missing from the *old* `.puml` set:
`BinaryOperator.Power` and the grammar's `power` production, the
`Serialization` folder (`WorkbookSerializer`, `CellDTO`), and
`CalculationEngine.Workbook`/`CalculationEngine.Clear()`/
`CommandManager.Clear()`. None of that appears in these eight diagrams
either, since Gemini only had the Mermaid text to work from, not the
codebase.

**Why:** converting a stale diagram into a different notation does not
make it current the entire point of entry 9 was to stop trusting the
previous diagrams and re-derive the class model from the live source,
and adopting these eight blocks in place of that work would silently
reintroduce the exact staleness problem entry 9 was written to fix.
They're logged here as a rejected alternative, not a pending file
change, so a future session isn't tempted to treat "already converted
to PlantUML" as equivalent to "already verified against the code."
 





