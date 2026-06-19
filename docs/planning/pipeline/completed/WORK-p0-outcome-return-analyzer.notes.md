# WORK-p0-outcome-return-analyzer — Notes

Per-phase working notes for the paired `.spec.md`. The spec is the contract;
this is the record of what happened.

## Phase 1 — Plan
- **Request:** P0 next slice — the **outcome-return purity analyzer** (D-0017): a new Roslyn rule
  **MRM1006** in the existing `MonoRpgMaker.Analyzers` that makes an outcome-returning sim handler
  mutating game state un-compilable. Now unblocked by the outcome vocabulary (#8). Auto-approved
  through `/commit`. Direction confirmed by the user.
- **Intake source:** none. Slice from the LOCAL roadmap (P0 "Remaining P0" — the outcome-return analyzer).
- **Classification / tier:** work pipeline, **type = feature**. **Smaller than #7** — it REUSES the
  analyzer project + infra (no new project, no packaging), adding a rule + a marker attribute + verifier
  tests. One slice.
- **Forge recall (lessons/design surfaced):**
  - **D-0017** is governing: "add **outcome-return** + float-ban analyzers." The float/determinism ban
    landed (#7); the `Outcome` DU + return-then-apply seam landed (#8); this enforces "raw state mutation
    won't type-check" — making the model real by construction for current + future/scaffolded handlers,
    and a precondition for `monorpg scaffold` + the `.expect` oracle.
  - **High reuse** (`AD-claude-sim-determinism-analyzer-001` + `AD-claude-outcome-vocabulary-shape-001`):
    plug into `MonoRpgMaker.Analyzers` (netstandard2.0) + `SimScope` + the `MRM1xxx` band + `AnalyzerReleases`
    + the verifier harness from #7. The `[StateMutator]` marker mirrors `[DeterminismExempt]`.
  - **Lands GREEN on current code** (the key dynamic, like #7): today's handlers get a read-only
    `IEventContext` + return outcomes, so they already can't reach `GameState`'s mutators — the analyzer
    flags nothing on existing handlers and is proven via **verifier fixtures** (handler-mutates → MRM1006;
    OutcomeApplier → clean).
  - **The crux (carried to design):** how to identify "handler" scope (return type `IReadOnlyList<Outcome>`
    vs `IMapEvent.Run` vs a marker) and the mutator-detection mechanism (`[StateMutator]` attribute vs
    hardcoded `GameState` symbols vs broad purity) WITHOUT flagging the `OutcomeApplier` (returns void).
  - **Mutation lesson** (`PR-claude-analyzer-mutation-isolated-asserts-001`): isolated exact-count verifier
    tests got #7 to 92.35% MSI; keep the decision logic in pure helpers.
  - **Forge hygiene:** no oathstar bleed used; grounded in the local docs + the #7/#8 ADs.
- **Ticket:** **aa332074-7c27-4bdc-b69f-95e63e945b5b (#9)** — created with explicit `project_id a9ee8162`;
  returned row confirms **monorpgmaker** (#1–#8 done, #9 open). Local doc:
  `docs/planning/tickets/open/TICKET-0009-p0-outcome-return-analyzer.md`.
- **AAR id (from `aar-open`):** 0b1168ea-ee36-4b1f-927c-66de904dc545
- **EARS requirements reviewed:** REQ-001..008 (handler-mutates→MRM1006; applier-not-flagged; the
  `[StateMutator]` marker on GameState.Set/Add; lands green on current handlers; `-warnaserror`; MRM1006 band
  + AnalyzerReleases; SimScope-scoped + Abstractions purity; FULL gate).

## Phase 2 — Design

### Resolved OPEN-Phase-2 decisions
- **(a) Handler-scope** = a method whose **`ReturnType` is `IReadOnlyList<Outcome>`** (general — covers
  `IMapEvent.Run` + future/scaffolded handler kinds). Checked by `OriginalDefinition == IReadOnlyList\`1`
  + `TypeArguments[0] == Outcome`.
- **(b) Mutator** = a method bearing **`[StateMutator]`** (a new pure marker in Abstractions, mirroring
  `[DeterminismExempt]`), applied to `GameState.Set`/`Add`. The analyzer flags an **invocation** of a
  `[StateMutator]` method when it occurs in handler scope. `GameState` (Engine) referencing an Abstractions
  attribute is clean (Engine→Abstractions).
- **(c) `MRM1006`** — Category `Determinism`, Warning (→ error via `-warnaserror`), message has `{0}` + `D-0017`;
  added to `AnalyzerReleases.Unshipped.md`.
- **(d) A SECOND analyzer class** `OutcomeReturnPurityAnalyzer` (not a rule inside `SimDeterminismAnalyzer`) —
  single-responsibility; reuses `SimScope` (the sim pre-gate). No csproj change: every `[DiagnosticAnalyzer]`
  in `MonoRpgMaker.Analyzers` already applies to Engine + Abstractions via the existing analyzer ProjectReference.
- **(e) Scope walk** = from the `[StateMutator]` invocation, walk the `ContainingSymbol` chain; if **any**
  enclosing `IMethodSymbol` returns `IReadOnlyList<Outcome>`, it's in handler scope — so a mutator call inside a
  nested **local function / lambda** of a handler is still caught (the local-fn/lambda's `ContainingSymbol`
  reaches the handler method).

### Approach / architecture
- **`OutcomeReturnPurityAnalyzer`** (`MonoRpgMaker.Analyzers`): `Initialize` → `ConfigureGeneratedCodeAnalysis(None)`
  + `EnableConcurrentExecution()` + `RegisterCompilationStartAction`. On start, resolve
  `MonoRpgMaker.Abstractions.StateMutatorAttribute`, `…Abstractions.Outcome`, and
  `System.Collections.Generic.IReadOnlyList\`1`; if any is absent → no rule (no crash). Register a single
  `OperationKind.Invocation` action.
- **`AnalyzeInvocation`:** gate `SimScope.IsInSimNamespace(context.ContainingSymbol)` (sim pre-filter — host/
  renderer + tests excluded); if `SimScope.HasAttribute(invocation.TargetMethod, stateMutator)` **and**
  `SimScope.IsInsideOutcomeHandler(context.ContainingSymbol, readOnlyList, outcome)` → report **MRM1006** at the
  invocation, with the mutator name as `{0}`.
- **Pure helpers** (added to `SimScope`, for mutation-testability): `HasAttribute(ISymbol, attr)` and
  `IsInsideOutcomeHandler(ISymbol?, IReadOnlyList\`1 def, Outcome)` (the `ContainingSymbol` walk + the
  return-type match).
- **Lands GREEN on current code:** the only `[StateMutator]` calls in Engine are in `OutcomeApplier.Apply`
  (returns `void` → not a handler → not flagged); the handlers (`LeverEvent`/`ChestEvent.Run`) call only
  `IEventContext.GetSwitch` (a read, not `[StateMutator]`) and return outcomes → no MRM1006. `EventContext`/
  `WorldSim` aren't outcome-returning handlers. The build confirms.

### File manifest
| # | File | Change |
|---|---|---|
| 1 | `src/MonoRpgMaker.Abstractions/StateMutatorAttribute.cs` | **NEW.** `[AttributeUsage(AttributeTargets.Method)]` public sealed marker (mirrors `DeterminismExemptAttribute`); XML-doc'd. |
| 2 | `src/MonoRpgMaker.Analyzers/OutcomeReturnPurityAnalyzer.cs` | **NEW.** The `DiagnosticAnalyzer`: CompilationStart resolves the 3 symbols; Invocation action; sim-gated; flags `[StateMutator]` calls in outcome-returning handler scope → MRM1006. |
| 3 | `src/MonoRpgMaker.Analyzers/DeterminismDiagnostics.cs` | **MODIFY.** Add the `OutcomeReturnPurity` descriptor (MRM1006, Category `Determinism`, Warning, message `{0}`+`D-0017`). |
| 4 | `src/MonoRpgMaker.Analyzers/SimScope.cs` | **MODIFY.** Add pure helpers `HasAttribute` + `IsInsideOutcomeHandler` (+ the private `ReturnsOutcomeList`). |
| 5 | `src/MonoRpgMaker.Analyzers/AnalyzerReleases.Unshipped.md` | **MODIFY.** Add the `MRM1006 | Determinism | Warning | …` row. |
| 6 | `src/MonoRpgMaker.Engine/Sim/GameState.cs` | **MODIFY.** Mark `Set` + `Add` with `[StateMutator]` (+ `using MonoRpgMaker.Abstractions;`). |
| 7 | `tests/MonoRpgMaker.Engine.Tests/OutcomeReturnPurityAnalyzerTests.cs` | **NEW.** Verifier fixtures (the harness pattern from `SimDeterminismAnalyzerTests`, instantiating `new OutcomeReturnPurityAnalyzer()`) + the `GameState`-marked reflection test + descriptor-shape test. |

### Regression Test Plan
| # | Test | Proves Requirement |
|---|---|---|
| T1 | A sim-namespace handler returning `IReadOnlyList<Outcome>` that calls a `[StateMutator]` (stand-in) method → exactly **MRM1006** at the call. | REQ-001 |
| T2 | A `void` method (the applier shape) calling the same `[StateMutator]` method → **no** MRM1006 (carve-out). | REQ-002 |
| T3 | A handler returning `IReadOnlyList<Outcome>` that only constructs + returns outcomes (no `[StateMutator]` call) → no MRM1006. | REQ-004 |
| T4 | The same `[StateMutator]` call in a handler in a **host** namespace (`MonoRpgMaker.Engine.Core`) → no MRM1006 (SimScope gate). | REQ-007 |
| T5 | A handler whose nested **local function** calls a `[StateMutator]` method → MRM1006 (the `ContainingSymbol` walk). | REQ-001 (escape) |
| T6 | A handler whose **lambda** calls a `[StateMutator]` method → MRM1006. | REQ-001 (escape) |
| T7 | `GameState.Set` and `GameState.Add` bear `[StateMutator]` (reflection); + an integration fixture: a handler calling the real `GameState.Set` → MRM1006. | REQ-003 |
| T8 | The current `LeverEvent`/`ChestEvent.Run` shape (reads + returns outcomes) → clean; the FULL build/suite is green (no MRM1006 on the real codebase). | REQ-004 |
| T9 | The MRM1006 descriptor is Warning + enabled + Category `Determinism` + `MRM1`-band + message contains `{0}` and `D-0017`. | REQ-005/REQ-006 |
| T10 | FULL `bin/gate.sh` green — build `-warnaserror` (a violating fixture is an error), coverage ≥ 80, mutation MSI ≥ 80 (Analyzers incl. the new rule). | REQ-005/REQ-008 |

All positive tests use **isolated exact-count** assertions (`PR-claude-analyzer-mutation-isolated-asserts-001`).
**Uncoverable:** none new — the analyzer runs in-process under the verifier; the host/test exclusion is covered by T4.

### Risks / decisions
- **R1 — `[StateMutator]` scope (v1 = `GameState.Set`/`Add`):** a handler could still mutate via an *unmarked*
  path (`Map.SetTile`, a static, a captured field). v1 covers the game-state-mutator path (what the event seam
  applies); the `[StateMutator]` mechanism extends to more mutators as P2 handler kinds land. **Documented
  limitation** (also in the spec OUT — no broad purity analysis beyond the mutator-call ban).
- **R2 — the `ContainingSymbol` walk** for local functions/lambdas must chain to the handler; pinned by T5/T6.
- **R3 — analyzer mutation MSI:** the walk + attribute check + return-type match + the descriptor strings must
  be killed by isolated fixtures (T1-T9); keep the logic in pure `SimScope` helpers.
- **R4 — lands green:** verified by code census (only `OutcomeApplier.Apply`, void, calls `Set`/`Add`); the FULL
  build is the gate. If any current handler mutated, gate:2 would go red (correctly).
- **R5 — RS2008:** MRM1006 must be in `AnalyzerReleases.Unshipped.md` with a Category/Severity matching the descriptor.
- **Reversible-but-load-bearing:** second analyzer class (vs a rule in `SimDeterminismAnalyzer`); `[StateMutator]`
  scope; the test-harness duplication (a shared `AnalyzerVerifier` helper is a nice-to-have refactor, deferred).

## Phase 3 — Implement
- **Built (6 files, exactly to the manifest):**
  1. `src/MonoRpgMaker.Abstractions/StateMutatorAttribute.cs` (NEW) — `[AttributeUsage(Method)]` pure sealed
     marker, XML-doc'd, mirroring `DeterminismExemptAttribute`.
  2. `src/MonoRpgMaker.Engine/Sim/GameState.cs` — `using MonoRpgMaker.Abstractions;` + `[StateMutator]` on
     `Set` and `Add`.
  3. `src/MonoRpgMaker.Analyzers/DeterminismDiagnostics.cs` — added the `OutcomeReturnPurity` descriptor
     (MRM1006, Category `Determinism`, Warning, message `{0}`+`D-0017`); class doc now covers both analyzers.
  4. `src/MonoRpgMaker.Analyzers/SimScope.cs` — added `HasAttribute(ISymbol, attr)`,
     `IsInsideOutcomeHandler(ISymbol?, readOnlyListDef, outcome)` (the `ContainingSymbol` walk), and the
     private `ReturnsOutcomeList` (constructed-`IReadOnlyList<Outcome>` match).
  5. `src/MonoRpgMaker.Analyzers/OutcomeReturnPurityAnalyzer.cs` (NEW) — the 2nd `[DiagnosticAnalyzer]`:
     CompilationStart resolves `StateMutatorAttribute`/`Outcome`/`IReadOnlyList\`1` (no-op if any absent);
     Invocation action gated by `SimScope.IsInSimNamespace`; flags a `[StateMutator]` call inside an
     outcome-returning handler → MRM1006 at the call, with the mutator name as `{0}`.
  6. `src/MonoRpgMaker.Analyzers/AnalyzerReleases.Unshipped.md` — added the `MRM1006` row (RS2008).
- **Builds (as-I-go):** analyzers `-warnaserror` → **0/0** (RS analyzer rules + RS2008 release-tracking green);
  full solution `-warnaserror` → **0/0** ⇒ **MRM1006 lands GREEN on current code** (the only `[StateMutator]`
  caller in Engine is `OutcomeApplier.Apply`, which returns `void` → not handler-scoped → not flagged; the
  handlers `LeverEvent`/`ChestEvent.Run` call only the read verb `GetSwitch` → not flagged).
- **No test authoring** here (Phase 4) — no existing tests referenced the new symbols, so no compile-fix edits
  were needed.
- **Deviations from design:** none. (The "analyzer actually FIRES on a violating handler" proof is Phase-4 T1;
  implement requires only a clean compile + green-on-current-code, both confirmed.)

## Inspect (Phase 3.5)
- **Lenses run:** 2 critics — (1) **analyzer correctness** (general-purpose; **executed** the real analyzer
  against 16 in-memory snippets), (2) design / purity / reuse (read-only).
- **No logic findings** — Critic 1 **proved MRM1006 fires correctly**; the only fixes were doc-accuracy. Findings:

  | # | Sev | Finding | Verdict | Resolution |
  |---|---|---|---|---|
  | F1 | — | **The analyzer FIRES (not a silent no-op).** Critic 1 ran 16 snippets: P1 direct mutator-in-handler → MRM1006; P2 local-function escape → MRM1006; P3 lambda escape → MRM1006; P6 real `IMapEvent.Run` signature → MRM1006; N1 void `Apply()` → none; N2 pure handler → none; N3 host namespace → none. Green-on-current-code is **real**. | **VERIFIED CLEAN** (by execution) | none |
  | F2 | LOW | **`ContainingSymbol` semantics doc was imprecise** — for an invocation `ContainingSymbol` is *already* the enclosing member (the handler), so the chain walk matches at iteration 1 for in-handler local-fns/lambdas; the walk's real job is *rejecting* non-handler containers (e.g. a field-initializer lambda → its field, proven by N5 → no flag). | **REAL (doc only)** | **FIXED** — reworded the doc in `SimScope.IsInsideOutcomeHandler` + `OutcomeReturnPurityAnalyzer`. |
  | F3 | LOW→HIGH(critic) | **`[StateMutator]` v1-scope** (only `GameState.Set`/`Add`; `Map.SetTile`/static/captured-field escapes) was in spec/notes but not the attribute's own doc. | **REAL (honesty)** | **FIXED** — added a forward caveat to `StateMutatorAttribute` XML-doc. (Limitation also in spec OUT + R1.) |
  | F4 | CRIT(critic) | "Throwaway test file `ZzzThrowawayOutcomePurityProbe.cs` still present, blocks the build." | **REJECTED — race artifact** | Critic 2 (read-only) observed Critic 1's temp file **mid-flight**; Critic 1 deleted it. **Verified clean**: `git status` = 6-file diff + docs only; `dotnet build -warnaserror` 0/0. |
  | F5 | CRIT(critic) | "Test harness incomplete for the mutation gate." | **NOT A FINDING** — Phase 4 | The T1-T10 verifier tests are validate's job (planned); implement correctly authored no tests. |
  | F6 | LOW | **Scope boundary:** a handler declaring `List<Outcome>`/`IList<Outcome>` (not the `IReadOnlyList<Outcome>` interface) escapes MRM1006 (N8). | **ACCEPTABLE** — matches `IMapEvent.Run`'s exact return type | Phase 4 pins it with a negative test (N8). |
  | F7 | MED(critic) | 2nd-analyzer-class duplicates the CompilationStart/Initialize boilerplate. | **DEFER (P2)** | A shared `CompilationStart` factory is a P2 refactor; single-responsibility is right for P0. |
  | — | — | Abstractions purity (pure marker; Engine→Abstractions legal; no MRM/ring trip), RS1032/RS2008 descriptor shape, `SimScope` placement (no dup with `IsExempt` — one walks, one doesn't), §14 conventions | **VERIFIED CLEAN** (both critics) | none |

- **Phase-4 carry-forward (the proven fixture set):** author `OutcomeReturnPurityAnalyzerTests` with P1 (direct→MRM1006), P2 (local-fn), P3 (lambda), N1 (void applier→none), N2 (pure handler→none), N3 (host ns→none), **N6** (`IReadOnlyList<string>`→none), **N7** (`IReadOnlyList<SetSwitch>`→none — exact, not assignable), **N8** (`List<Outcome>`→none — pins the boundary), the `GameState.Set`/`Add` `[StateMutator]` reflection test, and the MRM1006 descriptor-shape test. Isolated exact-count (`PR-claude-analyzer-mutation-isolated-asserts-001`).
- **CRITICAL test-authoring trap → `PR-claude-analyzer-verifier-resolve-types-001`:** an unresolved type in a verifier snippet (CS0246) yields no bound invocation → the analyzer fires nothing → a **false-negative green** (Critic 1 hit this: a host-ns snippet with bare `GameState` 'passed' via CS0246 until fully-qualified + a sim-ns control added). Phase 4 must fully-qualify cross-namespace types (or use snippet-local stand-ins) **and assert zero CS-error compile diagnostics** before asserting analyzer diagnostics.
- **Forge capture:** `prevention-rule-record PR-claude-analyzer-verifier-resolve-types-001`. No `failure-record` (no bug — the slice is correct; F4 was a parallel-critic race, F2/F3 were doc-only).

## Phase 4 — Validate
- **Tests added — `OutcomeReturnPurityAnalyzerTests` (11), the inspect-proven fixture set:**
  - **Positives → MRM1006** (REQ-001): P1 direct mutator call in a handler; P2 the call hidden in a nested
    **local function**; P3 the call hidden in a **lambda**. Each exact-count `"MRM1006"`.
  - **Negatives → none:** N1 a `void` method (the applier shape, REQ-002); N2 a pure handler that returns
    outcomes without mutating (REQ-004); N3 the same handler-mutates body in a **host** namespace
    (REQ-007); N6 `IReadOnlyList<string>`, N7 `IReadOnlyList<SetSwitch>` (exact-match miss), N8 concrete
    `List<Outcome>` (`OriginalDefinition` miss) — the return-type boundary. **Each negative shares P1's
    body**, varying one condition, with P1 as the positive control.
  - **REQ-003:** reflection — `GameState.Set`/`Add` bear `[StateMutator]`, `Get`/`GetCount` do not.
  - **REQ-005/006:** the MRM1006 descriptor shape (via the analyzer's public `SupportedDiagnostics`).
  - **Harness:** mirrors `SimDeterminismAnalyzerTests` (TPA references → snippets name the real
    `GameState`, fully-qualified) and **asserts each snippet compiles (zero CS errors) before** running the
    analyzer — bakes in `PR-claude-analyzer-verifier-resolve-types-001` (no false-negative greens).
- **`dotnet test MonoRpgMaker.slnx`: Passed — 172 passed, 0 failed** (+11; was 161).
- **`bin/gate.sh`: GREEN [full]** — all 12 gates. Coverage **97.4%**; mutation MSI **Analyzers 91.18% /
  Engine 90.67% / Abstractions 82.22%** (all ≥ 80). Receipt written.
- **Analyzers MSI 92.35% → 91.18%:** the new `IsInsideOutcomeHandler` walk + `HasAttribute` +
  `ReturnsOutcomeList` (the `OriginalDefinition` / `TypeArguments[0]==Outcome` / length checks) are killed by
  the isolated fixtures (N1 kills the void carve-out; N6/N7/N8 kill the type checks; P1-P3 kill the
  positive path). The slight dip is the rule's added surface; well above the 80 floor — no floor lowered.
  (Any `IsInsideOutcomeHandler` loop-*continuation* mutant is **equivalent**: for an invocation
  `ContainingSymbol` is always the enclosing member, so iterating past it never changes the result — proven
  by Critic 1's symbol dumps in inspect F2.)
- **Pre-existing exclusions:** none. No new packages → lock files unchanged. `StrykerOutput/` gitignored.

## Phase 5 — Complete
- Docs updated:
- Forge capture (aar/failures/rules/decisions):
- Ticket closed:
- Archived:
