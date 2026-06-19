---
pipeline_id: f344468a-3678-4455-952e-1d6db5425aa5
title: WORK-p0-outcome-return-analyzer
ticket: aa332074-7c27-4bdc-b69f-95e63e945b5b
type: work
intake:
notes: WORK-p0-outcome-return-analyzer.notes.md
status: Phase 5 — Complete PASS
---

# WORK-p0-outcome-return-analyzer

> Pipeline spec (always-loaded contract). Detailed per-phase work lives in the
> paired `.notes.md`. Phase advance = updating the `status:` frontmatter above.

## Work Spec
- **Title:** P0 — the **outcome-return purity analyzer** (D-0017): a new Roslyn rule
  **MRM1006** in the existing `MonoRpgMaker.Analyzers` that makes an *outcome-returning sim
  handler* mutating game state **un-compilable** — completing the D-0017 analyzer pair (the
  float/determinism ban #7 + outcome-return now), enforcing the return-then-apply model (#8).
- **Scope:**
  *In* — `MRM1006` in the **existing** `MonoRpgMaker.Analyzers` (no new project): inside an
  outcome-returning handler's scope, flag a call to a state-mutating method. A **`[StateMutator]`**
  marker in `MonoRpgMaker.Abstractions` (mirroring `[DeterminismExempt]`); mark `GameState.Set`/`Add`
  with it (declarative + extensible to future mutators). `AnalyzerReleases` entry for MRM1006.
  Verifier-test fixtures: a handler that calls a mutator → MRM1006; the `OutcomeApplier`
  (returns `void`) → clean; the current `LeverEvent`/`ChestEvent.Run` → clean (lands green).
  FULL gate green.
  *Out* — the generator/validator/scaffolding; the `.expect` oracle (gate #13 / `MRM0006`); the
  full P2 effect spine; broad general-purpose purity analysis beyond the state-mutator-**call** ban
  (arbitrary field-assignment / static-mutator escapes — a documented follow-up).
- **Systems:** **analyzers** (the MRM1006 rule + its pure decision logic) · **abstractions**
  (the `[StateMutator]` marker) · **engine** (`GameState.Set`/`Add` marked; the handler scope analyzed)
  · **tests** (verifier fixtures).

## Acceptance Criteria (EARS)
| ID | EARS Requirement | Verification |
|---|---|---|
| REQ-001 | When an outcome-returning sim handler calls a state-mutating method, the analyzer shall emit an `MRM1006` diagnostic. | analyzer verifier test (positive) |
| REQ-002 | When non-handler code (e.g. `OutcomeApplier`, which returns `void`) calls a state-mutating method, the analyzer shall not emit `MRM1006`. | analyzer verifier test (carve-out negative) |
| REQ-003 | A state-mutator marker shall exist in `MonoRpgMaker.Abstractions` and the current sim mutators (`GameState.Set`/`Add`) shall carry it. | build + review |
| REQ-004 | The analyzer shall emit no `MRM1006` on the existing handlers (`LeverEvent`/`ChestEvent.Run`) — the slice lands green on current code. | build + full suite green |
| REQ-005 | The `MRM1006` diagnostic shall build as an error under `-warnaserror` (gate:2). | gate:2 + a deliberately-violating fixture |
| REQ-006 | `MRM1006` shall live in the analyzers' `MRM1xxx` band (distinct from the validator's `MRM0xxx`) and be declared in `AnalyzerReleases`. | build (RS2008) + review |
| REQ-007 | Only sim handler code shall be analyzed (`SimScope`-scoped); `MonoRpgMaker.Abstractions` stays pure (the Project→Abstractions ring + the MRM determinism analyzer stay green). | review + NetArchTest |
| REQ-008 | The slice shall pass the FULL `bin/gate.sh` (mutation MSI ≥ floor on Analyzers + Engine + Abstractions). | gate run (FULL) |

## Locked-In Decisions
- **D-0017 governs.** Outcome-returning handlers must be **pure** (return outcomes, don't mutate
  state); this analyzer is the compile-time enforcement, completing the D-0017 analyzer pair.
- **Reuse the existing analyzer infra (no new project).** Plug into `MonoRpgMaker.Analyzers`
  (`netstandard2.0`), `SimScope` (sim-namespace scope), the `MRM1xxx` band (new id **MRM1006**),
  `AnalyzerReleases` tracking, and the verifier-test harness from #7 (`AD-claude-sim-determinism-analyzer-001`).
- **The `OutcomeApplier` + non-handler code are not flagged** — only outcome-returning handler scope is.
- **Lands green on current code** (today's handlers are already pure via the read-only `IEventContext`);
  the teeth are proven via **verifier fixtures** (a handler that tries to mutate → MRM1006).
- **Abstractions purity holds** — `[StateMutator]` is a pure marker attribute; the analyzer scopes to sim
  handler code only; payloads/markers carry no float/MonoGame.
- **Mutation:** isolated **exact-count verifier tests** per `PR-claude-analyzer-mutation-isolated-asserts-001`
  (this took #7 to 92.35% MSI); keep the decision logic in pure helpers for mutation-kill.

### Resolved in Phase 2 (full design in the notes)
- Handler-scope = a method whose `ReturnType` is `IReadOnlyList<Outcome>`; mutator = a `[StateMutator]`-marked
  method (on `GameState.Set`/`Add`), flagged when invoked in handler scope. **MRM1006** (Category `Determinism`).
  A **second analyzer class** `OutcomeReturnPurityAnalyzer` reusing `SimScope` (no csproj change). The handler
  check **walks the `ContainingSymbol` chain** so a mutator call inside a nested local-function/lambda is caught.
  v1 teeth = the `[StateMutator]`-call ban (broader purity / unmarked-path escapes are a documented follow-up);
  no exemption needed (the applier carve-out is by scope). File manifest + T1-T10 test plan in the notes.

## Linked Artifacts
- Design docs: `docs/decisions.md` (D-0017 outcome-return analyzer — the governing decision; D-0016/D-0018
  MRM bands), `docs/agentic-substrate.md` (pure-over-context / return-outcomes / the outcome-return analyzer),
  `docs/roadmap.md` (P0 remaining)
- Intake doc: none (LOCAL roadmap; user-confirmed; auto-approved through commit)
- Ticket doc: docs/planning/tickets/open/TICKET-0009-p0-outcome-return-analyzer.md
- Forge ticket: aa332074-7c27-4bdc-b69f-95e63e945b5b (#9, project monorpgmaker `a9ee8162-3cfd-4c99-be5c-bbdb4be32b10`)
- Builds on: WORK-p0-sim-determinism-analyzer (#7 — the analyzer infra/SimScope/MRM band/verifier harness),
  WORK-p0-outcome-vocabulary (#8 — the `Outcome` DU + `IMapEvent.Run` handler shape)
- Reuse ADs: AD-claude-sim-determinism-analyzer-001, AD-claude-outcome-vocabulary-shape-001
- Ground-truth anchors: `src/MonoRpgMaker.Analyzers/SimDeterminismAnalyzer.cs` + `SimScope.cs` (the infra to
  extend), `src/MonoRpgMaker.Abstractions/DeterminismExemptAttribute.cs` (the marker pattern),
  `src/MonoRpgMaker.Engine/Sim/GameState.cs` (Set/Add — the mutators), `Outcome.cs` + `IMapEvent.cs` (handler shape),
  `src/MonoRpgMaker.Engine/Sim/OutcomeApplier.cs` (the must-not-flag carve-out)
- AAR: 0b1168ea-ee36-4b1f-927c-66de904dc545

## Phase Plan
| Phase | Status |
|---|---|
| 1 — Plan | PASS |
| 2 — Design | PASS |
| 3 — Implement | PASS |
| 3.5 — Inspect | PASS |
| 4 — Validate | PASS |
| 5 — Complete | PASS |
