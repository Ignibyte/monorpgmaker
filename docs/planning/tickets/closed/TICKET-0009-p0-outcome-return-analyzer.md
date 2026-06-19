---
title: TICKET-0009-p0-outcome-return-analyzer
status: done
ticket: aa332074-7c27-4bdc-b69f-95e63e945b5b
ticket_number: 9
type: feature
created: 2026-06-18
intake:
pipeline_spec: docs/planning/pipeline/completed/WORK-p0-outcome-return-analyzer.spec.md
---

# TICKET-0009-p0-outcome-return-analyzer

## Summary

The **D-0017 outcome-return purity analyzer** — the compile-time enforcement of the
return-then-apply model the outcome vocabulary (#8) established. A new Roslyn rule
**MRM1006** in the **existing** `MonoRpgMaker.Analyzers` project: an *outcome-returning
sim handler* must be **pure** — it must not mutate game state. A handler that calls a
state-mutating method is a build error under `-warnaserror`; the `OutcomeApplier` (which
returns `void` and legitimately applies outcomes) is **not** flagged. Completes the
D-0017 analyzer pair (float/determinism ban #7 + outcome-return now).

## Why

D-0017: "add **outcome-return** + float-ban analyzers." The float/determinism ban landed
(#7); the `Outcome` DU + return-then-apply seam landed (#8). This analyzer **enforces**
"raw state mutation won't type-check" — making the model real *by construction* for
current + future/scaffolded handlers, and a precondition for `monorpg scaffold` + the
`.expect` oracle. It **lands green on the current code** (today's handlers get a read-only
`IEventContext` + return outcomes, so they already can't reach `GameState`'s mutators —
like #7, it flags nothing on existing code and is proven via verifier fixtures).

## EARS Requirements

| ID | EARS Requirement | Verification |
|---|---|---|
| REQ-001 | When an outcome-returning sim handler calls a state-mutating method, the analyzer shall emit an `MRM1006` diagnostic. | analyzer verifier test (positive) |
| REQ-002 | When non-handler code (e.g. `OutcomeApplier`, which returns `void`) calls a state-mutating method, the analyzer shall not emit `MRM1006`. | analyzer verifier test (carve-out negative) |
| REQ-003 | A state-mutator marker shall exist in `MonoRpgMaker.Abstractions` and the current sim mutators (`GameState.Set`/`Add`) shall carry it, so the ban is declarative + extensible. | build + review |
| REQ-004 | The analyzer shall emit no `MRM1006` on the existing handlers (`LeverEvent`/`ChestEvent.Run`) — the slice lands green on current code. | build + full suite green |
| REQ-005 | The `MRM1006` diagnostic shall build as an error under `dotnet build -warnaserror` (gate:2). | gate:2 + a deliberately-violating fixture |
| REQ-006 | `MRM1006` shall live in the analyzers' `MRM1xxx` band (distinct from the validator's `MRM0xxx`) and be declared in `AnalyzerReleases`. | build (RS2008) + review |
| REQ-007 | Only sim handler code shall be analyzed (`SimScope`-scoped); `MonoRpgMaker.Abstractions` stays pure (the Project→Abstractions ring + the MRM determinism analyzer stay green). | review + NetArchTest |
| REQ-008 | The slice shall pass the FULL `bin/gate.sh` (mutation MSI ≥ floor on Analyzers + Engine + Abstractions). | gate run (FULL) |

## Scope

- **In:** the `MRM1006` rule in the existing `MonoRpgMaker.Analyzers` (a `[StateMutator]`-call
  ban inside outcome-returning handler scope); a `[StateMutator]`-style marker in Abstractions
  (mirroring `[DeterminismExempt]`); marking `GameState.Set`/`Add`; `AnalyzerReleases` entry;
  verifier-test fixtures (handler-mutates → flagged; applier → clean; current handlers → clean).
  FULL gate green.
- **Out:** the generator/validator/scaffolding; the `.expect` oracle (gate #13 / `MRM0006`); the
  full P2 effect spine; broad general-purpose purity analysis beyond the state-mutator-call ban
  (e.g. arbitrary field-assignment / static-mutator escapes — noted as a follow-up).

## Notes

- Forge ticket: aa332074-7c27-4bdc-b69f-95e63e945b5b (#9, project monorpgmaker `a9ee8162-3cfd-4c99-be5c-bbdb4be32b10`)
- Related docs: `docs/decisions.md` (D-0017 outcome-return analyzer; D-0016/D-0018 MRM bands), `docs/agentic-substrate.md` (pure-over-context, return outcomes), `docs/roadmap.md` (P0 remaining)
- Builds on: WORK-p0-sim-determinism-analyzer (#7 — the analyzer infra/SimScope/MRM band/verifier harness it reuses), WORK-p0-outcome-vocabulary (#8 — the Outcome DU + handler shape it enforces)
- Reuse ADs: AD-claude-sim-determinism-analyzer-001, AD-claude-outcome-vocabulary-shape-001
- Promoted from intake: none (LOCAL roadmap; user-confirmed next slice, auto-approved through commit)
- Active pipeline: docs/planning/pipeline/active/WORK-p0-outcome-return-analyzer.spec.md
