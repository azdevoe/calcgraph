# CSC322 Calculation Engine — Group Project Plan

**Deliverable:** A C# class library (the calculation engine API) + a thin GUI client that demonstrates it, plus a design portfolio, test suite, benchmarks, AI collaboration log, critique, reflection and demo video.

**Governing principle:** The API is the product. The GUI is evidence. Every design decision must be defensible out loud, by every member of the group.

---

## 0. Read this before planning your week

The marking scheme is **45% non-code**:

| Component | Marks | Where it is won |
|---|---|---|
| Design portfolio | 20 | Class diagrams, ADT specs (abstraction function + rep invariant), formal grammar — **produced before coding** |
| Working system & benchmarks | 25 | Hidden test suite, perf targets demonstrated, assigned features |
| Tests & process evidence | 15 | Coverage **and** a git history that shows test-first work |
| AI log & critique | 10 | Honesty, depth, documented *rejections* of AI output |
| Oral defence & live modification | 20 | Every member explains any line; implements an unseen change live |
| Reflection | 10 | Two pages, specific, self-critical |

Two consequences that should shape everything below:

1. **Git history is read, not just the final tree.** A commit that adds tests followed by a commit that makes them pass is worth more than the same code arriving in one dump. This cannot be faked retroactively in any convincing way.
2. **The oral defence is individual and includes a live modification.** Any module that only one person understands is a 20-mark liability for everyone else. Cross-teaching is scheduled work, not a nicety.

**Group C assigned features: Data Validation, and Sorting & Filtering.** See Phase 6 — and note the three hooks they require in the Phase 1 interface contract.

**Two unknowns still to pin down:**
- The real submission deadline (the brief says 7 August 2026; that date has passed, so confirm the revised one and back-fill the calendar below).
- Team size, so you can map the four work tracks in §2 onto people.

---

## 1. Architecture — decide this once, in writing, before any code

```
Client (GUI / any consumer)
        │
        ▼
┌─────────────────────────────────────────────┐
│                 Workbook                    │  ← public API surface, Observer source
│  SetCell / GetValue / Undo / Redo / Events  │
└───────┬──────────────┬───────────────┬──────┘
        │              │               │
        ▼              ▼               ▼
   FormulaParser  DependencyGraph   CommandHistory
   (ANTLR → AST)  (edges, topo,     (undo/redo,
        │          cycle detect)     Command pattern)
        ▼              │
   Expression tree ────┘
   (Composite)
        │
        ▼
    Evaluator  ──► FunctionRegistry (Strategy/Factory)
   (Interpreter)
```

### 1.1 Project layout

**Full annotated tree, with track ownership per folder: `CSC322-Repository-Structure.md`.** That document is the single source of truth for where files live; this section gives only the shape, and each phase below lists the specific files it produces.

```
calc-engine/
├── docs/                     the 45% that isn't code
├── src/
│   ├── CalcEngine.Core/      the deliverable — NO UI references, ever
│   └── CalcEngine.Gui/       the demonstration
├── tests/
│   ├── CalcEngine.Tests/     mirrors Core folder-for-folder
│   └── CalcEngine.Benchmarks/
└── samples/
    └── CalcEngine.ConsoleDemo/   proves the API stands alone
```

Inside `src/CalcEngine.Core/`: `Abstractions/` (the frozen Phase 1 contract), then `Values/`, `Model/`, `Parsing/`, `Expressions/`, `Evaluation/`, `Functions/`, `Graph/`, `Commands/`, and — for your assigned features — `Validation/`, `Sorting/`, `Filtering/`.

Two properties of this layout do real work:

**`CalcEngine.Core` has zero UI dependencies.** This is the proof that you built an API and not an app, and it is visible from the directory listing before anyone opens a file. CI enforces it; a PR that breaks it fails the build.

**Tests mirror Core folder-for-folder.** A missing test folder is then a visible coverage gap rather than something you discover the week before submission.

### 1.2 The five decisions that determine your grade

**(a) Cell addressing — use a value type, not strings.**
`readonly record struct CellRef(int Col, int Row)`. A1-notation parsing and formatting happen only at the boundary (parser in, error messages out). Strings as dictionary keys in the recalculation hot path will cost you the 50 ms target. Same for `CellRange(CellRef TopLeft, CellRef BottomRight)`.

**(b) Errors are values, not exceptions.** This is the single most common way engines like this fail a hidden test suite. Model:

```
CellValue = Number(double) | Text(string) | Boolean(bool) | Error(ErrorKind) | Empty
ErrorKind = DivZero | Value | Ref | Name | Circular | NotAvailable
```

`1/0` yields `#DIV/0!`, and `=A1+1` where A1 is `#DIV/0!` yields `#DIV/0!` — the error *propagates through arithmetic as data*. Nothing escapes to the client as a .NET exception. Write the propagation rule down once and test it for every operator and every function.

**(c) The dependency graph — store both directions.**

```
dependencies : CellRef → HashSet<CellRef>   (cells this formula reads)
dependents   : CellRef → HashSet<CellRef>   (cells that read this cell)
```

You need `dependencies` to remove stale edges cheaply when a formula is replaced, and `dependents` to walk forwards on recalculation. Keeping only one direction makes formula replacement O(V+E).

Recalculation is: walk `dependents` from the changed cell to collect the **affected set**, then topologically sort *only that subgraph* (Kahn), then evaluate in order. A global topological sort on every edit will miss the 50 ms budget on a 100,000-cell workbook.

**The range problem — decide and document it.** `=SUM(B2:B45)` expands to 44 edges. In a large workbook with many range formulas the edge count explodes. Two options:
- *Materialise every cell edge.* Simple, obviously correct, memory-hungry.
- *Block/region indexing.* Bucket cells into fixed blocks; the formula depends on blocks, not cells. Far more complex.

**Recommendation:** materialise, measure it in your benchmarks, and write up the trade-off explicitly in the design portfolio and reflection. A documented, measured, deliberately-chosen limitation earns marks. An undocumented one loses them.

**(d) Cycle detection must be iterative and must report the path.**
Use an explicit stack DFS with three-colour marking (white / grey / black). Hitting a grey node means a cycle; reconstruct the exact path from the stack so you can report `A1 → B3 → C7 → A1` as the brief demands. Recursive DFS will stack-overflow on a hostile deep chain — that is a crash, which the brief forbids by name.

Detect at **set time** (walk from the new formula's dependencies looking for the target cell) so the client gets immediate feedback, and keep Kahn's leftover-nodes check as a safety net during full recalculation. Do not forget the self-reference case `A1 = A1 + 1`.

**(e) `IF` must be lazily evaluated.** If you evaluate both branches eagerly, `=IF(A1=0, 0, 10/A1)` returns `#DIV/0!` when A1 is 0. That is wrong, and it is a classic. Therefore function invocation receives **unevaluated argument expressions** plus the evaluation context, and each function decides what to evaluate:

```csharp
interface IFunction {
    string Name { get; }
    int MinArgs { get; }
    int MaxArgs { get; }   // int.MaxValue for variadic
    CellValue Invoke(IReadOnlyList<IExpression> args, IEvaluationContext ctx);
}
```

Registered in a `FunctionRegistry` dictionary (case-insensitive). Adding a function must mean adding one class and one registration line — no `switch` statement anywhere.

### 1.3 Semantics you must define now and test against

Write these into the portfolio as a specification table. The hidden test suite will probe them, and you can only defend what you documented:

- `COUNT` counts numeric values only; empty cells and text are not counted.
- `AVERAGE` of an empty range → `#DIV/0!`.
- `MIN`/`MAX` of an empty range → `0` (Excel) — or `#VALUE!` if you prefer; pick one, state it.
- `ROUND(number, digits)` — half away from zero, not banker's rounding (`Math.Round(x, d, MidpointRounding.AwayFromZero)`).
- `LOOKUP(value, lookupVector, resultVector)` — vector form, lookup vector assumed ascending, returns the result matching the largest lookup value ≤ the search value; `#N/A` if the search value is smaller than every entry. State the assumption that the vector is sorted.
- Text in arithmetic → `#VALUE!`. Empty cell in arithmetic → treated as `0`.
- Comparison operators return `Boolean`; `Boolean` in arithmetic → `TRUE`=1, `FALSE`=0.
- Reference to a cell outside the sheet bounds → `#REF!`.

---

## 2. Work tracks

Four tracks. Map them onto people by team size:

| Track | Owns | 3 people | 4 people | 5–6 people |
|---|---|---|---|---|
| **A — Grammar & Parsing** | `.g4` grammar, ANTLR pipeline, AST node types, AST builder visitor, error listener & messages | Person 1 | Person 1 | Person 1 |
| **B — Values & Functions** | `CellValue`, error model & propagation, `Evaluator`, `IFunction`, all eight functions | Person 2 | Person 2 | Persons 2 + 3 (split value model / function library) |
| **C — Graph & History** | `DependencyGraph`, topological sort, cycle detection, `Workbook` API, Command pattern, undo/redo, Observer events | Person 3 | Person 3 | Persons 3 + 4 |
| **D — Client & Infrastructure** | GUI, benchmark harness, CI, repo hygiene, docs assembly | Split A+C | Person 4 | Persons 5 + 6 |

**Everyone, regardless of track:**
- Writes the tests for their own module, committed before or with the implementation.
- Writes the class diagram and ADT specification for their own module.
- Keeps their own AI collaboration log entries as they go — not reconstructed at the end.
- Reviews at least one other track's pull requests each week.

**Interfaces are frozen in Phase 1.** Tracks work in parallel only because the interfaces between them were agreed in writing first. If an interface must change mid-flight, it changes by group agreement in a single PR, not unilaterally.

---

## 3. Phases

Timings below are relative. Back-fill real dates once you confirm the deadline. The whole plan compresses to roughly six weeks; if you have less, cut Phase 6 scope (the assigned features) last and Phase 7 (polish) first — never Phase 1.

---

### Phase 0 — Foundation (2–3 days)

**Goal:** nobody is blocked by tooling, and the rules of engagement are agreed.

1. **Run `scaffold.sh`.** One member runs it, pushes, everyone else clones. It creates the solution, all five projects, every folder, the NuGet references, `.gitignore`, `.gitattributes`, `.editorconfig`, `Directory.Build.props`, the CI workflow, `CONTRIBUTING.md` and the `docs/` stubs, then makes the first commit. On Windows it needs Git Bash, not PowerShell.

   It deliberately leaves `src/CalcEngine.Core/Abstractions/` empty — those interfaces are Phase 1's deliverable and your strongest defence preparation.

2. Read `CSC322-Repository-Structure.md` together as a group, so everyone knows which folders their track owns before anyone writes code. Ten minutes here prevents most merge conflicts later.

3. Confirm the rules in the generated `CONTRIBUTING.md` and amend if you disagree:
   - Branch naming: `track-a/grammar-numeric-literals`
   - Every change goes through a PR with one reviewer from another track.
   - Commit convention: `test: ...` commits precede or accompany `feat: ...` commits.
   - No direct commits to `main`.
4. Verify the ANTLR toolchain end-to-end on a throwaway grammar that parses `1+2`. Do this **now** — ANTLR C# setup (the `Antlr4.Runtime.Standard` package plus the `Antlr4BuildTasks` MSBuild integration) is the single most common early time sink. Everyone runs it on their own machine.

   **Every machine needs a JDK 11 or newer.** ANTLR's tool is a Java program; `Antlr4BuildTasks` downloads the correct JAR for you but needs a JRE to run it. Only the build needs Java — the compiled engine does not. Discovering this in Phase 2, when Track A is blocked, costs a day.
5. Set up CI (GitHub Actions): restore, build, test on every push. A red build must be visible.
6. Create `docs/ai-collaboration-log.md` with the agreed table format: date, member, tool, what was asked, what came back, what was changed, why.

**Exit criteria:** every member can clone, build, run an empty test, and regenerate the throwaway ANTLR parser. CI is green.

---

### Phase 1 — Design portfolio (5–7 days) — **no implementation code**

This phase is 20 marks on its own and it de-risks everything after it. Resist the pull to start coding.

1. **Formal grammar** — write the EBNF for the formula language on paper first, with the precedence ladder explicit:

   ```
   formula      → '=' expression
   expression   → comparison
   comparison   → additive ( ('=' | '<>' | '<' | '<=' | '>' | '>=') additive )*
   additive     → multiplicative ( ('+' | '-') multiplicative )*
   multiplicative → power ( ('*' | '/') power )*
   power        → unary ( '^' unary )*
   unary        → ('-' | '+')? primary
   primary      → NUMBER | STRING | BOOLEAN
                | cellRef | range
                | functionCall
                | '(' expression ')'
   range        → cellRef ':' cellRef
   functionCall → NAME '(' ( expression (',' expression)* )? ')'
   ```

   Then write the sample formulas you expect to parse *and* the malformed ones you expect to reject, with the exact error message each should produce. These become tests in Phase 2.

2. **ADT specifications.** For the expression tree, the dependency graph, and the workbook, write:
   - the **abstraction function** — what does the concrete representation *mean*? e.g. *AF(node) = the mathematical value obtained by applying the node's operator to the values of its children*
   - the **representation invariant** — what must always be true? e.g. *no node is its own descendant; a BinaryOp has exactly two non-null children; every CellRef in the tree is within sheet bounds*
   - preconditions/postconditions for each public method.

   This is explicitly named in the marking scheme. Write it in prose in `docs/portfolio.md`, and implement `CheckRep()` methods in Phase 2 that assert the invariants in debug builds — that turns a document into working evidence.

3. **Class diagrams**, one per module (Parsing, Expressions, Evaluation, Graph, Commands, Workbook) plus one overall. Use draw.io, PlantUML or Mermaid — PlantUML/Mermaid text lives in the repo and diffs, which is a small extra credit for process evidence.

4. **The interface contract document.** The exact C# interface signatures every track will code against: `IExpression`, `IEvaluationContext`, `IFunction`, `IDependencyGraph`, `IEditCommand`, `IWorkbook`. Commit these as compiling C# interface files with XML doc comments and no implementations. This is what unblocks parallel work.

   **Your assigned features (Data Validation, Sorting & Filtering) impose three requirements on this contract — design them in now, not in Phase 6:**
   - `IWorkbook.BeginBatch()` / `EndBatch()`, deferring recalculation until a bulk edit completes. Sorting a 10,000-cell range must produce **one** affected-set computation and **one** topological sort, not ten thousand. Retrofitting this later means reworking `SetCell`, the Command layer and the Observer event together.
   - Validation rules and filter state stored on the **workbook**, keyed by range — never inside the expression tree or the cell value.
   - An `IExpressionVisitor` abstraction over the tree. You need it twice in Phase 6 (reference translation for sorting, formula serialisation for the GUI formula bar), and adding it up front costs nothing.

5. **Book a slot with the lecturer** to walk through the design before implementation. The brief invites this; taking it up is free signal that you designed first.

**Files this phase produces:**
```
docs/grammar.md                          docs/adt-specifications.md
docs/interface-contract.md               docs/semantics.md
docs/diagrams/*.puml
src/CalcEngine.Core/Abstractions/        ← IWorkbook, IExpression, IExpressionVisitor,
                                           IEvaluationContext, IFunction,
                                           IDependencyGraph, IEditCommand,
                                           IValidationRule, IFilterPredicate
```
`Abstractions/` is the only Core folder touched in Phase 1, and after this phase it is **frozen** — changing an interface is a separate PR agreed by the whole group.

**Exit criteria:** `docs/portfolio.md` complete; interface files compile; lecturer conversation done; assigned features confirmed.

---

### Phase 2 — Parser and expression tree (Track A lead, ~1 week)

Test-first throughout: for each grammar rule, commit the failing parse test, then the rule.

1. Write `Formula.g4` — lexer rules (NUMBER, STRING, BOOLEAN, CELLREF, NAME, operators) then parser rules per the ladder above. Watch the lexer ambiguity between a cell reference `A1` and a function name `SUM` — order your lexer rules so `CELLREF` matches `[A-Z]+[0-9]+` and `NAME` matches identifiers not followed by digits, or disambiguate in the parser.
2. Verify the parse tree shape with `grun`/the ANTLR test rig before writing any C#.
3. Build the **AST separately from the ANTLR parse tree.** Write an `AstBuilder : FormulaBaseVisitor<IExpression>` that converts ANTLR contexts into your own node types. Do not use ANTLR's generated context classes as your ADT — they are not yours, you cannot state a meaningful representation invariant for them, and it will show at the defence.
4. Implement node types: `NumberLiteral`, `TextLiteral`, `BooleanLiteral`, `CellReference`, `RangeReference`, `UnaryOp`, `BinaryOp`, `FunctionCall`. Each implements `IExpression`.
5. Implement dependency extraction: `IEnumerable<CellRef> GetDependencies()` walking the tree, expanding ranges.
6. **Error messages.** Remove `ConsoleErrorListener` and install your own `IAntlrErrorListener` that collects `(line, column, message)`. Produce messages that say what and where: `Unexpected ')' at column 12 — expected a number, cell reference or function name.` The brief calls this out specifically: *malformed input is normal input*.
7. Implement `CheckRep()` on the tree asserting the invariants from Phase 1.

**Files this phase produces:**
```
src/CalcEngine.Core/Parsing/       Formula.g4, FormulaParser, AstBuilder,
                                   CollectingErrorListener, ParseError, ParseResult
src/CalcEngine.Core/Expressions/   the eight node types
src/CalcEngine.Core/Expressions/Visitors/   DependencyCollector, FormulaPrinter
tests/CalcEngine.Tests/Parsing/    GrammarTests, ErrorMessageTests, AstBuilderTests
```
Track A owns all of these; no other track should have files open here.

**Exit criteria:** every formula in the Phase 1 sample list parses to the expected tree; every malformed sample produces the expected message with correct position; ≥90% line coverage on the Parsing folder.

---

### Phase 3 — Evaluation and function library (Track B lead, ~1 week, parallel with Phase 4)

1. `CellValue` and `ErrorKind`. Implement coercion rules and the propagation rule from §1.3 in one place, and test every operator × every value-type combination. A table-driven `[Theory]` test is the right shape here.
2. `IEvaluationContext` — gives an expression access to `GetValue(CellRef)` and range materialisation. This is the seam between the evaluator and the workbook; it lets you unit-test evaluation with a fake context, no workbook required.
3. `Evaluator` implementing Interpreter over the Composite tree.
4. `FunctionRegistry` + the eight required functions. Order: SUM, AVERAGE, MIN, MAX, COUNT (aggregates, all similar), then ROUND, then IF (lazy — write that test first), then LOOKUP (most fiddly).
5. Test each function against: normal input, empty range, wrong arity, wrong argument type, error value in input, range vs scalar arguments.

**Files this phase produces:**
```
src/CalcEngine.Core/Values/        CellValue, ValueKind, ErrorKind, ValueCoercion
src/CalcEngine.Core/Evaluation/    Evaluator, WorkbookEvaluationContext, RangeMaterializer
src/CalcEngine.Core/Functions/     FunctionRegistry, FunctionBase,
                                   Aggregates/, Logical/, Math/, Lookup/
tests/CalcEngine.Tests/Values/     CoercionTests, ErrorPropagationTests
tests/CalcEngine.Tests/Functions/  one file per function + LazyEvaluationTests, ArityTests
tests/CalcEngine.Tests/TestUtilities/   FakeEvaluationContext
```
Track B works entirely inside these folders. `FakeEvaluationContext` is what lets this phase run in parallel with Phase 4 — Track B tests evaluation without needing Track C's workbook to exist yet.

**Exit criteria:** all eight functions pass their specification tests; the error-propagation matrix is fully green; no code path throws an exception to the caller.

---

### Phase 4 — Dependency graph and reactive recalculation (Track C lead, ~1 week, parallel with Phase 3)

1. `DependencyGraph` with both edge directions. Methods: `SetDependencies(cell, deps)` (removes stale edges and adds new atomically), `GetDependents(cell)`, `Clear(cell)`.
2. `GetAffectedCells(changed)` — iterative BFS/DFS over `dependents`, returning the affected set.
3. `TopologicalSort(subset)` — Kahn's algorithm over the induced subgraph only.
4. `CycleDetector` — iterative three-colour DFS returning the exact cycle path as `IReadOnlyList<CellRef>`. Test: two-cell cycle, three-cell cycle, self-reference, cycle reached indirectly, cycle introduced by *editing* an existing formula, and a 500-deep non-cyclic chain that must **not** be flagged.
5. `Workbook.SetCell(ref, input)` wiring it together: parse → extract dependencies → cycle check → update graph → collect affected → topologically sort → evaluate in order → raise a **single batched** `CellsChanged` event (Observer). Batching matters: per-cell events will make the GUI unusable at 100k cells.
6. Cache the parsed expression tree per cell. Never re-parse during recalculation — this alone is the difference between hitting and missing the 2-second target.

**Files this phase produces:**
```
src/CalcEngine.Core/Graph/    DependencyGraph, TopologicalSorter, CycleDetector,
                              CycleReport, RecalculationPlanner
src/CalcEngine.Core/Model/    CellRef, CellRange, A1Notation, Cell, Workbook,
                              BatchScope, CellsChangedEventArgs
tests/CalcEngine.Tests/Graph/        DependencyGraphTests, TopologicalSortTests,
                                     CycleDetectionTests
tests/CalcEngine.Tests/Integration/  EndToEndTests  ← first cross-track file
tests/CalcEngine.Tests/TestUtilities/   WorkbookBuilder
```
`Integration/` is the first genuinely shared folder. It is also the first point where Tracks A, B and C discover whether the Phase 1 interfaces were right — which is why the twice-weekly integration checkpoint matters most during this phase.

**Exit criteria:** editing a cell recomputes exactly the cells that depend on it, in a correct order, and nothing else. Every cycle test reports the exact path. Integration test: Phase 2 + 3 + 4 together on a small worked example.

---

### Phase 5 — Undo/redo, and the public API surface (~4 days)

1. `IEditCommand { void Execute(); void Undo(); }`; `SetCellCommand` capturing the previous raw input and the new one.
2. `CommandHistory` with two bounded stacks, capacity 100. New edit clears the redo stack. Test the boundary: 100 undos work, the 101st does not, and pushing a 101st command evicts the oldest.
3. **Undo must route through the same `SetCell` path** so the dependency graph and recalculation happen identically. Do not write a separate restore path — that is where divergence bugs live.
4. Finalise the public API: `IWorkbook` with `SetCell`, `GetValue`, `GetFormula`, `Undo`, `Redo`, `CanUndo`, `CanRedo`, and the `CellsChanged` event. XML doc comments on every public member.
5. Write a small console program that drives the API with no GUI. This proves the library stands alone and doubles as a defence artefact.

**Files this phase produces:**
```
src/CalcEngine.Core/Commands/    CommandHistory, SetCellCommand, CompositeCommand
tests/CalcEngine.Tests/Commands/ UndoRedoTests, HistoryCapacityTests
samples/CalcEngine.ConsoleDemo/  Program.cs — the API driven with no GUI
```
The remaining command types (`SetValidationCommand`, `SortRangeCommand`, `ApplyFilterCommand`) arrive in Phase 6, but `CompositeCommand` must exist now — it is what makes a batched sort undo as one operation rather than ten thousand.

**Exit criteria:** undo/redo correct across formula edits, value edits, cycle-creating edits and deletions. Public API documented.

---

### Phase 6 — Assigned features: Data Validation, Sorting & Filtering (~5–6 days)

Both assigned features are **range operations**. Neither should require a single change to the evaluator or the dependency graph. If you find yourself modifying either, stop — the feature is being built in the wrong layer. That both features slot in cleanly is the strongest sentence you will write in your reflection, so build for it deliberately.

Two things they need from earlier phases, which is why they are flagged in Phase 1 and Phase 5:

- a **bulk-edit API** on the workbook (`BeginBatch()` / `EndBatch()`), because sorting a 10,000-cell range must not trigger 10,000 recalculations;
- **validation rules and filter state stored on the workbook**, not on the expression tree.

---

#### 6A. Data Validation

**Model.** A `ValidationRule` is attached to a `CellRange` and stored in the workbook, alongside cells rather than inside them. Rule types:

| Type | Parameters | Notes |
|---|---|---|
| `WholeNumber` | min, max, operator | between / not between / greater than / equal to … |
| `Decimal` | min, max, operator | same operator set |
| `TextLength` | min, max, operator | operates on string length |
| `List` | explicit values **or** a source range | the range form reads cells — see below |
| `Custom` | a formula | reuses your parser and evaluator wholesale |

```csharp
interface IValidationRule {
    ValidationResult Validate(CellValue candidate, CellRef target, IEvaluationContext ctx);
}
record ValidationResult(bool IsValid, string? Message);
```

**The `Custom` rule is where this feature earns its marks.** A rule like `=A1>B1` is parsed by the ANTLR parser you already have and evaluated by the evaluator you already have, against a context anchored at the target cell. No new machinery. Say exactly this at the defence.

**Three decisions to make explicitly and write down:**

1. **Reject or flag?** Recommended: `SetCell` validates the *raw input* before committing, and returns a result indicating rejection without changing state. Invalid input never enters the workbook by the front door.

2. **Do you re-validate computed results?** Recommended: **no.** A cell whose formula result drifts out of range because a dependency changed is not a rejected edit. Instead expose `IEnumerable<CellRef> FindInvalidCells()` — an on-demand sweep the client can call, which the GUI surfaces as circled cells. This mirrors Excel's *Circle Invalid Data* and it keeps validation out of the recalculation hot path.

3. **Do validation formulas create dependency edges?** Recommended: **no.** Adding them to the main graph would pollute the topological ordering and could manufacture false circular-reference reports. Evaluate them on demand instead. This is a real trade-off with a real cost (a `List` rule sourced from `D1:D10` will not auto-refresh) — document the cost, do not hide it. A documented trade-off is worth more than a silent one.

**Undo/redo:** `SetValidationCommand : IEditCommand`, capturing the previous rule for the range. Editing a rule goes on the same history stack as editing a cell.

**Tests:** each rule type against valid input, invalid input, boundary values (min and max exactly), empty input, wrong type, and a custom formula that itself evaluates to an error value. Plus: a rejected edit leaves the workbook, the dependency graph and the undo stack completely unchanged.

---

#### 6B. Sorting

**This is the semantically hardest thing in the whole project, and the lecturer will ask about it.** The question you must be able to answer instantly is: *"What happens when I sort a range that contains `=A1+1`?"*

Sorting moves cell contents. Formulas contain cell references. So you must decide what happens to those references — and the wrong move is to discover this during implementation.

**Three options, in increasing order of difficulty:**

| | Behaviour | Cost | Verdict |
|---|---|---|---|
| **A** | Sort computed **values only**; formulas in the range are replaced by their values (or the sort is refused if the range contains formulas) | Trivial | Acceptable floor. Document the limitation loudly. |
| **B** | Sort raw cell **content**, translating every cell reference in a moved formula by the row/column delta of its move | ~Half a day extra, given you already have an AST | **Recommended.** This is Excel's actual behaviour and it reuses the tree you already built. |
| **C** | Full Excel semantics including absolute references (`$A$1` pinned, `A1` translated) | Requires extending the grammar | Only if everything else is finished. |

**Implementing option B.** You already have the parse tree, so translation is a visitor:

1. `ReferenceTranslationVisitor : IExpressionVisitor` — clones the tree, offsetting every `CellReference` and `RangeReference` by `(Δcol, Δrow)`.
2. `FormulaPrinter : IExpressionVisitor` — serialises an AST back to formula text. You need this anyway for the GUI formula bar, so it is not wasted work.
3. Sort pipeline: read the range → build sort keys from **computed values** → reorder rows (or columns) → for each moved cell, translate its formula by its own delta → write everything back inside a single batch → one recalculation at the end.

Because the base grammar has no absolute references, every reference is relative and translation is uniform. That simplification is worth stating in the portfolio.

**Specification details to pin down:**
- Sort keys: multi-column (sort by column B, ties broken by column D), ascending/descending per key.
- Cross-type ordering. Excel's order is: numbers < text < booleans < errors < empty. Pick an order, write it in the spec table, test it. This *will* be probed.
- Header row: does the sort range include one? Make it an explicit parameter, not a guess.
- Case sensitivity in text comparison — declare it.
- Stability: use a **stable** sort so equal keys preserve original order. `OrderBy` in LINQ is stable; `List.Sort` is not.

**Undo:** `SortRangeCommand` must capture the entire prior raw content of the range. Note the memory cost — a 10,000-cell sort held across 100 undo slots is not free. Either cap the undoable sort size or document the cost. Measuring it in your benchmarks is a good look.

**Tests:** already-sorted range, reverse-sorted, all-equal keys (stability), mixed types, empty cells within the range, a range containing formulas that reference inside the range, a range containing formulas that reference outside it, multi-key sort, undo restoring exact prior state including formula text.

---

#### 6C. Filtering

Much simpler than sorting, provided you hold one line: **a filter is a view, not an edit.**

- Filtering **never** changes a cell value, a formula, or the dependency graph. It marks rows hidden. The GUI renders only visible rows.
- Expose it as `SetFilter(range, columnIndex, IFilterPredicate)` plus `IEnumerable<int> GetVisibleRows(range)`.
- Predicates reuse the evaluator: comparison (`> 50`), text (`contains`, `begins with`), value lists (`is one of {…}`), and a custom formula predicate — the same `IExpression` path as custom validation.
- Multiple active filters on different columns combine with AND.

**The one temptation to refuse:** making `SUM` skip hidden rows. That would make the engine's results depend on view state, which breaks the API/client separation that the entire project is built on. If you want the behaviour, add a *separate* `SUBTOTAL`-style function that takes visibility as an explicit argument — never make the existing aggregates visibility-aware. Being able to explain why you refused this is a defence answer worth having.

**Undo:** include `ApplyFilterCommand` on the history stack; it is cheap (predicate plus prior visibility set).

**Tests:** predicate correctness per type, filters combining across columns, filter over a range containing errors, clearing a filter, and — the important one — an assertion that cell values and the dependency graph are byte-identical before and after filtering.

---

**Files this phase produces:**
```
src/CalcEngine.Core/Validation/   ValidationRegistry, ValidationResult,
                                  InvalidCellScanner, Rules/
src/CalcEngine.Core/Sorting/      RangeSorter, SortSpecification, CellValueComparer
src/CalcEngine.Core/Filtering/    FilterManager, VisibilityState, Predicates/
src/CalcEngine.Core/Expressions/Visitors/ReferenceTranslator.cs   ← for sorting
src/CalcEngine.Core/Commands/     SetValidationCommand, SortRangeCommand,
                                  ApplyFilterCommand
tests/CalcEngine.Tests/Validation/ tests/CalcEngine.Tests/Sorting/
tests/CalcEngine.Tests/Filtering/
```
**Notice what is absent from that list:** `Evaluation/`, `Graph/`, `Functions/` and `Parsing/` are untouched. `ReferenceTranslator` is a new file in an existing folder, not a change to an existing one. If a diff in this phase modifies the evaluator or the dependency graph, the feature is being built in the wrong layer — stop and reconsider before continuing.

That property is also your reflection's strongest paragraph, and it is checkable: `git diff --stat` at the end of Phase 6 is the evidence.

**Phase 6 exit criteria:** all three sub-features specified in the portfolio with their semantics tables (cross-type sort order, validation rule types, filter predicates); tested; implemented; and a written note in the reflection on which parts of the core design absorbed them unchanged and which required extension.

---

### Phase 7 — GUI, benchmarks, and performance (~1 week)

**GUI (Track D).** Avalonia if anyone is off Windows (and it works well in Rider); WPF otherwise.
- A **virtualised** scrollable grid. Non-virtualised will die well before 100k cells.
- Select a cell → formula bar shows the raw formula; the grid shows the computed value.
- Dependent cells visibly update on edit.
- Error values render distinctly (red text, `#DIV/0!` etc.).
- Circular references flagged with the cycle path shown to the user.
- Undo/redo buttons and keyboard shortcuts.
- The GUI holds **no calculation logic whatsoever.** It subscribes to `CellsChanged` and re-renders. If anyone finds themselves writing arithmetic in a view-model, the design has leaked.

**Benchmarks.** Build `CalcEngine.Benchmarks` as a runnable harness with documented instructions:
1. *Propagation:* 100,000-cell workbook, a chain of 500 dependent cells, edit the head, measure to last update. Target **< 50 ms**.
2. *Full recalculation:* same workbook, recompute everything. Target **< 2 s**.
3. Report memory and edge count too — it shows you understood the range trade-off.

Run in **Release** with warm-up iterations; BenchmarkDotNet handles this properly and its output is credible in a report. If you miss a target, profile before optimising: the usual culprits are re-parsing during recalculation, string-keyed dictionaries, per-cell event raising, and LINQ allocations in the inner evaluation loop.

**Files this phase produces:**
```
src/CalcEngine.Gui/Views/          MainWindow, SpreadsheetView, FormulaBar,
                                   ValidationDialog, SortFilterDialog
src/CalcEngine.Gui/ViewModels/     MainWindowViewModel, SpreadsheetViewModel,
                                   CellViewModel
src/CalcEngine.Gui/Controls/       VirtualizingGrid
src/CalcEngine.Gui/Services/       WorkbookService  ← the ONLY class touching Core
tests/CalcEngine.Benchmarks/       WorkbookGenerator, PropagationBenchmark,
                                   FullRecalculationBenchmark, SortBenchmark,
                                   MemoryProfile
docs/benchmarks.md
```
Nothing under `src/CalcEngine.Core/` changes in this phase. If the GUI forces an engine change, that is a design flaw surfacing late — note it in the reflection rather than hiding it, since an honestly reported late discovery reads better than a silent one.

**Exit criteria:** both targets met and reproducible from documented instructions; a screenshot or recording of the benchmark output in the portfolio.

---

### Phase 8 — Documents, defence preparation, submission (~1 week)

1. **AI critique exercise.** Pick one module — the dependency graph is the richest choice. Ask an assistant for a complete solution, save the full transcript, then write the two-page senior-engineer review: where it is correct, where it is *subtly* wrong, where it falls short of the brief's specifications, and what you did differently. Strong things to look for: does it re-sort the whole graph on every edit? does it detect cycles recursively? does it treat errors as exceptions? does it handle range dependencies at all? does it re-parse on recalculation? Each of those is a concrete, defensible criticism.
2. **AI collaboration log** — final pass. It should already be populated week by week. Entries recording something you *rejected and why* are explicitly worth more than entries recording something that worked.
3. **Reflection (2 pages):** what you designed, what you would do differently, what the AI tools got wrong. Be specific and name real incidents — the range-edge trade-off, the lazy-`IF` bug, a rejected AI design. Generic reflection reads as generic.
4. **Demo video (5 minutes):** GUI driving the API. Script it: type a formula → dependents update → introduce an error → introduce a cycle and show the reported path → undo/redo → show the benchmark run. Do not improvise; rehearse once.
5. **Defence preparation — treat this as real work, not revision.**
   - Each member gives a 10-minute walkthrough of a module they *did not write*, to the group. Gaps surface immediately.
   - Practise live modifications on each other. Realistic prompts: *add a `MEDIAN` function*; *make `COUNT` also count booleans*; *report the cycle path in reverse order*; *add a `PRODUCT` function*; *change `ROUND` to banker's rounding*; *make division by an empty cell return `#DIV/0!`*. Time them at 10 minutes each.
   - Every member should be able to draw the architecture diagram from memory and state the abstraction function of the expression tree.

**Files this phase produces:**
```
docs/critique.md            docs/critique-transcript.md
docs/reflection.md          docs/portfolio.md            ← final assembly
docs/ai-collaboration-log.md   ← final pass, not first draft
```
Every one of these was created as a stub by the scaffold in Phase 0 and should have been filling up week by week. If any is still empty at the start of Phase 8, that is the real warning sign — a log written in one sitting reads exactly like a log written in one sitting.

**Exit criteria:** all six deliverables present; every member has successfully done a timed live modification on a module they did not write.

---

## 4. Cadence

- **Daily:** 10-minute stand-up — done / doing / blocked.
- **Twice weekly:** integration checkpoint. Merge everything to `main`, run the full test suite. Integrating twice a week means integration bugs are small; integrating once at the end means integration bugs are the project.
- **Weekly:** cross-teaching session. One member presents a module they don't own, with the owner correcting them. Also the weekly AI-log review.
- **End of each phase:** exit criteria checked off explicitly before the next phase starts. Write the check-off in the PR description.

---

## 5. Risk register

| Risk | Likelihood | Mitigation |
|---|---|---|
| ANTLR C# toolchain setup burns days | High | Phase 0, item 4. Everyone verifies before Phase 1. |
| Range dependencies blow up memory/time | Medium | Materialise, measure, document the trade-off. Fall back to block indexing only if benchmarks fail. |
| `IF` evaluated eagerly → wrong errors | High if unaware | Design the `IFunction` interface to take unevaluated expressions. Write the test in Phase 3 first. |
| Recursive cycle detection stack-overflows | Medium | Iterative DFS with explicit stack, from the start. |
| 50 ms target missed | Medium | Cache parsed trees; sort only the affected subgraph; profile before optimising. |
| Interfaces drift; integration fails late | High without discipline | Freeze interfaces in Phase 1; integrate twice weekly. |
| One member cannot defend a module | High without effort | Weekly cross-teaching; timed live-modification practice in Phase 8. |
| Git history looks like a code dump | Medium | Test commits before implementation commits, enforced at review. This is unfixable retroactively. |
| Non-virtualised GUI dies at 100k cells | Medium | Choose a virtualising grid control in Phase 7 day one. |
| AI log reconstructed at the end | Medium | Log entries are part of the PR checklist, every week. |
| Sort semantics undecided until implementation | High | Choose option A/B/C in Phase 1 and write it in the portfolio. Have the `=A1+1` answer ready. |
| Bulk edits (sort) recalculate per cell and blow the time budget | High | `BeginBatch`/`EndBatch` in the Phase 1 interface contract, not bolted on in Phase 6. |
| Filtering leaks into engine semantics | Medium | Filter is view state only. Refuse visibility-aware aggregates. |
| `SortRangeCommand` undo snapshots exhaust memory | Low–Medium | Cap undoable sort size or document the cost; measure it. |

---

## 6. Definition of done — the master checklist

**Engine**
- [ ] Grammar formally specified and implemented in ANTLR
- [ ] All eight functions: SUM, AVERAGE, MIN, MAX, COUNT, IF, ROUND, LOOKUP
- [ ] Numbers, text, cell refs, ranges, arithmetic, comparison, parentheses
- [ ] Errors as values; no exception ever escapes to the client
- [ ] Reactive recalculation: only affected cells, correct order
- [ ] Circular references detected, exact cycle reported, no crash or hang
- [ ] Undo/redo, at least 100 operations
- [ ] Data Validation: all rule types, custom-formula rules, rejected edits leave no trace, `FindInvalidCells()` sweep
- [ ] Sorting: multi-key, stable, documented cross-type ordering, reference translation, undo restores exact prior content
- [ ] Filtering: predicates, AND-combination across columns, provably zero effect on values and graph
- [ ] Bulk edits batched — one recalculation per sort, not one per cell

**Performance**
- [ ] 500-cell chain in a 100k workbook propagates in < 50 ms, demonstrated
- [ ] Full recalculation of 100k cells in < 2 s, demonstrated
- [ ] Benchmark instructions written and reproducible

**Process**
- [ ] Git history shows tests before or with implementation
- [ ] Test suite covers grammar, evaluation, graph, cycles, undo/redo, errors
- [ ] CI green on `main`

**Documents**
- [ ] Class diagrams (per module and overall)
- [ ] ADT specifications: abstraction functions and representation invariants
- [ ] Formal grammar document
- [ ] AI collaboration log
- [ ] Critique exercise (2 pages) + transcript attached
- [ ] Reflection (2 pages)
- [ ] 5-minute demonstration video

**Defence**
- [ ] Every member can explain every module
- [ ] Every member has completed a timed live modification
