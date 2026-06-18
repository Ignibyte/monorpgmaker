---
title: TICKET-0008-p0-outcome-vocabulary
status: done
ticket: c744fb96-12f1-4152-9238-272e52c35dee
ticket_number: 8
type: feature
created: 2026-06-18
intake:
pipeline_spec: docs/planning/pipeline/completed/WORK-p0-outcome-vocabulary.spec.md
---

# TICKET-0008-p0-outcome-vocabulary

## Summary

Establish the **D-0017** "handlers **RETURN** the shared outcome vocabulary so raw
state mutation won't type-check" model on the existing event seam: define the
**minimal outcome vocabulary** (the event-effect subset — set-switch, add-counter,
show-message) in `MonoRpgMaker.Abstractions` and migrate the seam from
*mutate-via-verbs* to *return-then-apply*. This is the linchpin that unblocks the
**outcome-return purity analyzer** (the next slice) + `monorpg scaffold`.

## Why

Today `IMapEvent.Run(IEventContext)` returns `void` and **mutates** via the context's
write-verbs (`SetSwitch`/`AddCounter`/`ShowMessage`); the tracer `LeverEvent`/
`ChestEvent` call them directly. D-0017's model is the inverse — handlers are **pure
over a read-context and RETURN declarative outcomes**, so a raw mutation won't
compile and intent becomes an executable `.expect` oracle. Establishing the model on
the *real* seam now de-risks the analyzer (which enforces it) and the scaffolder
(which emits signatures that return it).

**Design tension (carried into review):** the design docs say to *lock the full
shared outcome vocabulary in P2, co-designed with battle (one registry, not two)*.
Mitigation: scope to the **event-effect subset** (orthogonal to battle's HpDelta/
state effects) with an **additive, forward-compatible** shape (D-0019) so P2
*extends* the same vocabulary rather than forking a second one.

## EARS Requirements

| ID | EARS Requirement | Verification |
|---|---|---|
| REQ-001 | When a map event runs, `IMapEvent.Run` shall return its declarative outcomes as a read-only sequence (not `void`). | build + review |
| REQ-002 | The outcome vocabulary shall express set-switch, add-counter, and show-message as distinct, value-equatable declarative types in `MonoRpgMaker.Abstractions`. | unit test |
| REQ-003 | When the Engine applies an event's returned outcomes, the resulting `GameState` + message effects shall match the prior mutate-via-verbs behavior (lever opens the door once; chest gives one potion then reads empty). | tracer tests (updated) + seeded replay |
| REQ-004 | `IEventContext` shall expose only the read verbs (`GetSwitch`/`GetCounter`) the handler branches on; set-switch/add-counter/show-message shall no longer be context methods (they are returned outcomes). | build + review |
| REQ-005 | A new outcome kind shall be addable without changing existing outcome types or the `IMapEvent` signature (additive extensibility, so P2's effect spine extends this vocabulary). | review + design demonstration |
| REQ-006 | `MonoRpgMaker.Abstractions` shall remain MonoGame-free and float-free (outcome payloads are `int`/`bool`/`string` only). | NetArchTest ring + MRM determinism analyzer (green) + review |
| REQ-007 | The slice shall pass the FULL `bin/gate.sh` (mutation MSI ≥ floor on Engine + Abstractions + Analyzers). | gate run (FULL) |

## Scope

- **In:** the minimal outcome vocabulary type(s) in Abstractions; `IMapEvent.Run`
  returns outcomes; `IEventContext` slimmed to read verbs; an Engine-side applier;
  tracer `LeverEvent`/`ChestEvent` + `WorldSim` migration; updated tests. Abstractions
  stays pure + deterministic. FULL gate green.
- **Out:** the **outcome-return purity analyzer** (the next slice — enforces this at
  compile time); the full **P2 effect spine** (`IEffect`/`ITrait`/`IDamageFormula`/
  `IStateBehavior` + `TraitDomain` fold rules + combat effects); the **`.expect`
  oracle** (gate #13 / `MRM0006`); `monorpg scaffold`; outcome save/serialization format.

## Notes

- Forge ticket: c744fb96-12f1-4152-9238-272e52c35dee (#8, project monorpgmaker `a9ee8162-3cfd-4c99-be5c-bbdb4be32b10`)
- Related docs: `docs/decisions.md` (D-0017 outcome-return + scaffolding, D-0019 additive seam kinds, D-0011/D-0012 agentic authoring), `docs/agentic-substrate.md` (declarative outcome vocabulary / `.expect`), `docs/agentic-features.md` (effects co-designed with battle), `docs/roadmap.md` (P0 remaining; P2 effect spine)
- Promoted from intake: none (chosen from the LOCAL roadmap; direction confirmed by the user)
- Active pipeline: docs/planning/pipeline/active/WORK-p0-outcome-vocabulary.spec.md
