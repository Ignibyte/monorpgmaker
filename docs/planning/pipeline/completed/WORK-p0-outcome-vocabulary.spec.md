---
pipeline_id: 7168d4b3-ab65-427b-a7a8-051755b3b9de
title: WORK-p0-outcome-vocabulary
ticket: c744fb96-12f1-4152-9238-272e52c35dee
type: work
intake:
notes: WORK-p0-outcome-vocabulary.notes.md
status: Phase 5 — Complete PASS
---

# WORK-p0-outcome-vocabulary

> Pipeline spec (always-loaded contract). Detailed per-phase work lives in the
> paired `.notes.md`. Phase advance = updating the `status:` frontmatter above.

## Work Spec
- **Title:** P0 — the **minimal shared outcome vocabulary**: map events **RETURN**
  declarative outcomes (set-switch / add-counter / show-message) instead of mutating
  via `IEventContext` write-verbs (D-0017's "raw state mutation won't type-check"),
  proven by migrating the event seam to *return-then-apply*.
- **Scope:**
  *In* — define the **minimal outcome vocabulary** in `MonoRpgMaker.Abstractions` (the
  event-effect subset: set-switch, add-counter, show-message — as RETURNED, value-equatable
  declarative types, **additively extensible** per D-0019); `IMapEvent.Run` returns the
  outcome(s) (a read-only sequence — `ChestEvent` emits 3) instead of `void`; `IEventContext`
  slims to the **read verbs** the handler branches on (`GetSwitch`/`GetCounter`); an
  **Engine-side applier** interprets the returned outcomes against `GameState` + the message
  sink (replacing `EventContext`'s write methods); migrate the tracer `LeverEvent`/`ChestEvent`
  + `WorldSim` (the caller) + the tracer replay + tests. Abstractions stays pure (Project→
  Abstractions ring) + deterministic (no float). FULL gate green.
  *Out* — the **outcome-return purity analyzer** (the NEXT slice — enforces this at compile
  time; reuses `MonoRpgMaker.Analyzers`/`SimScope`/MRM band); the full **P2 effect spine**
  (`IEffect`/`ITrait`/`IDamageFormula`/`IStateBehavior` + `TraitDomain` fold rules + combat
  effects like `HpDelta`); the **`.expect` oracle** (gate #13 / `MRM0006`); `monorpg scaffold`;
  outcome save/serialization format.
- **Systems:** **abstractions** (new outcome types + slimmed `IEventContext` + `IMapEvent`
  signature) · **events/sim** (the applier + tracer migration + `WorldSim`) · tests.

## Acceptance Criteria (EARS)
| ID | EARS Requirement | Verification |
|---|---|---|
| REQ-001 | When a map event runs, `IMapEvent.Run` shall return its declarative outcomes as a read-only sequence (not `void`). | build + review |
| REQ-002 | The outcome vocabulary shall express set-switch, add-counter, and show-message as distinct, value-equatable declarative types in `MonoRpgMaker.Abstractions`. | unit test |
| REQ-003 | When the Engine applies an event's returned outcomes, the resulting `GameState` + message effects shall match the prior mutate-via-verbs behavior (lever opens the door once; chest gives one potion then reads empty). | tracer tests (updated) + seeded replay |
| REQ-004 | `IEventContext` shall expose only the read verbs (`GetSwitch`/`GetCounter`); set-switch/add-counter/show-message shall no longer be context methods (they are returned outcomes). | build + review |
| REQ-005 | A new outcome kind shall be addable without changing existing outcome types or the `IMapEvent` signature (additive extensibility, so P2's effect spine extends this vocabulary). | review + design demonstration |
| REQ-006 | `MonoRpgMaker.Abstractions` shall remain MonoGame-free and float-free (outcome payloads are `int`/`bool`/`string` only). | NetArchTest ring + the MRM determinism analyzer (green) + review |
| REQ-007 | The slice shall pass the FULL `bin/gate.sh` (mutation MSI ≥ floor on Engine + Abstractions + Analyzers). | gate run (FULL) |

## Locked-In Decisions
- **D-0017 model.** Handlers are **pure over a read-context and RETURN the shared outcome
  vocabulary**; raw state mutation is expressed as a returned outcome, not a verb call. This
  slice establishes the model on the real seam; the **analyzer that enforces it is the next slice**.
- **One shared vocabulary, started additively (D-0019).** Define the **event-effect subset**
  now; P2's battle/effect spine **extends** the same vocabulary — not a second registry. The
  Phase-2 shape must be **forward-compatible** with P2's `IEffect`/registry direction.
- **Reads stay on the context; writes become returned outcomes.** `GetSwitch`/`GetCounter`
  remain on `IEventContext`; `SetSwitch`/`AddCounter`/`ShowMessage` become outcome types.
- **Lives in Abstractions; pure + deterministic.** No MonoGame, no float (the event payloads
  are `int`/`bool`/`string`); the MRM determinism analyzer + the Project→Abstractions ring stay green.
- **Behavior parity is the regression guard.** The seeded tracer replay + `TracerSliceTests`
  must show identical observable behavior after the migration.

### Phase-2 design decisions — RESOLVED (rationale + manifest + test plan in `.notes.md` Phase 2)
- **Shape** → sealed-record DU: `abstract record Outcome` (non-public base ctor, closed to Projects) +
  `SetSwitch(string Key, bool Value)` / `AddCounter(string Key, int Amount)` / `ShowMessage(string Text)`.
- **Forward-compat** → VERIFIED: P2's `IEffect`/`ITrait`/`IStateBehavior`/`IDamageFormula` "return declarative
  outcomes" (agentic-features) → the SAME `Outcome` DU, extended additively. One registry, not two.
- **Naming** → `Outcome` + case records 1:1 with the old verbs. **Cardinality** → `IReadOnlyList<Outcome>`.
- **`IMapEvent.Run`** → returns `IReadOnlyList<Outcome>`, keeps the read-context param. **`IEventContext`** →
  reads only (`GetSwitch`/`GetCounter`).
- **Applier** → new `OutcomeApplier` (Engine.Sim) applies outcomes to `GameState` + the message sink;
  **`EventContext`** → read-only adapter (sink moves to the applier).

## Linked Artifacts
- Design docs: `docs/decisions.md` (D-0017 outcome-return + scaffolding; D-0019 additive seam kinds;
  D-0011/D-0012 agentic authoring; D-0016 determinism), `docs/agentic-substrate.md` (declarative
  outcome vocabulary / pure-over-context / `.expect`), `docs/agentic-features.md` ("the shared outcome
  vocabulary must be co-designed with battle — one registry, not two"), `docs/roadmap.md` (P0 remaining; P2 effect spine)
- Intake doc: none (direction confirmed by the user at `/work`)
- Ticket doc: docs/planning/tickets/open/TICKET-0008-p0-outcome-vocabulary.md
- Forge ticket: c744fb96-12f1-4152-9238-272e52c35dee (#8, project monorpgmaker `a9ee8162-3cfd-4c99-be5c-bbdb4be32b10`)
- Builds on: WORK-p0-eventseam (the seam this migrates), WORK-p0-sim-determinism-analyzer (the analyzer this sets up)
- Current seam (ground truth): `src/MonoRpgMaker.Abstractions/IMapEvent.cs` (Run→void), `IEventContext.cs`
  (read+write verbs), `src/MonoRpgMaker.Engine/Sim/EventContext.cs` (the verb impl over `GameState`),
  `Sim/Tracer/LeverEvent.cs` + `ChestEvent.cs` (call write verbs; chest emits 3), `Sim/GameState.cs`, `Sim/WorldSim.cs` (calls `Run`)
- AAR: 7da6a87b-ee57-42a3-bb00-87b629322bee

## Phase Plan
| Phase | Status |
|---|---|
| 1 — Plan | PASS |
| 2 — Design | PASS |
| 3 — Implement | PASS |
| 3.5 — Inspect | PASS |
| 4 — Validate | PASS |
| 5 — Complete | PASS |
