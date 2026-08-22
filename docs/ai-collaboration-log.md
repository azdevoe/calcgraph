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
| 35 | 2026-07-31 | Abdulazeez | claude | Asked for ANTLR setup guidance and a project roadmap; caught that the roadmap omitted git repo initialization needed for timestamped history |
| 36 | 2026-08-02 | Abdulazeez | claude | Design Portfolio — first pass, then an audit caught a broken grammar, five missing ADT specs, a missing Observer pattern, and a Cell.Value : double that couldn't hold errors or text |
| 37 | 2026-08-31 | Abdulazeez | claude | Foundation ADTs (CellRef, CellValue, DependencyGraph) built test-first, 44 tests green |
| 38 | 2026-08-31 | Abdulazeez | claude | Grammar, expression tree, and function strategies; caught two test-first order violations and logged them |
| 39 | 2026-08-31 | Abdulazeez | claude | Cell/Workbook/Observer/CalculationEngine facade + Command pattern; caught a namespace collision, an infinite-recursion risk, and an empty-undo edge case |
| 40 | 2026-08-31 | Abdulazeez | claude | Data Validation feature; surfaced and fixed a latent CommandManager stack-desync bug it would have exposed |
| 41 | 2026-08-31 | Abdulazeez | claude | WinForms GUI scaffolded; three real bugs found in testing, one of them a misdiagnosis worth logging on its own |
| 42 | 2026=08-22 | Peter | Gemini | Evaluated project codebase to identify missing JSON persistence module; implemented WorkbookSerializer (SaveToJSON/LoadFromJSON) and CellDTO positional record; removed BeginBatch() wrapping from LoadFromJSON to ensure immediate synchronous formula evaluation upon loading.

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
