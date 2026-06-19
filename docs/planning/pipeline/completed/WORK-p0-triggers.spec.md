---
pipeline_id: 365a3a62-c0bc-4ee4-b8ad-b434beb7bac7
title: WORK-p0-triggers
ticket: 0e3ab789-cc7f-48f5-ae0e-fc0854e8ee85
type: work
intake:
notes: WORK-p0-triggers.notes.md
status: Phase 5 — Complete PASS
---

# WORK-p0-triggers

> Pipeline spec (always-loaded contract). Detail per phase lives in the paired `.notes.md`.

## Work Spec
- **Title:** P0 #13 — trigger kinds + the ordered hook lifecycle: an `ActionButton` interact verb + player
  `Facing`, and an explicit **ordered, ambiguity-checked** event dispatch (replacing implicit insertion order),
  keeping replay bit-identical. MVP — the extensibility spine.
- **Scope:**
  *In* — `ActionButton` trigger; player `Facing` (reuse `Direction`) + `WorldSim.PressAction()` (faced cell);
  `IMapEvent.Order` (non-breaking default-interface member); an ordered dispatch with a TYPED ambiguity error
  (equal Order at same cell+trigger) at sim build/load; tests. FULL gate green.
  *Out (deferred)* — Autorun/Parallel triggers; the module-registration/subscription system; the visual UI;
  save/load (#15); unified-entity (#14).
- **Systems:** **abstractions** (`EventTrigger` += ActionButton; `IMapEvent.Order`) · **engine/sim** (`WorldSim`
  dispatch + `PressAction`; the ordered/ambiguity dispatcher) · **engine/entities** (player `Facing`) · tests.

## Acceptance Criteria (EARS)
| ID | EARS Requirement | Verification |
|---|---|---|
| REQ-001 | `EventTrigger` shall include an `ActionButton` kind in addition to `StepOn`. | unit test |
| REQ-002 | The player shall track a `Facing`, set to the attempted direction on every `MovePlayer` (incl. a blocked move). | unit test |
| REQ-003 | `WorldSim.PressAction()` shall dispatch `ActionButton` events at the faced cell + apply outcomes; a step shall NOT fire `ActionButton`, `PressAction` shall NOT fire `StepOn`. | unit test |
| REQ-004 | `IMapEvent` shall expose `Order` (default `0`); a (cell, trigger) group dispatches in ascending `Order`. | unit test |
| REQ-005 | Equal `Order` for 2+ events at the same cell + trigger → a typed ambiguity error at build/load — no silent order, no throw on the validation path. | unit test |
| REQ-006 | The dispatch order shall be deterministic + total (MRM-clean; no float/clock/RNG/nondeterministic enumeration). | build `-warnaserror` + review |
| REQ-007 | Existing `StepOn` behaviour shall be preserved. | `TracerSliceTests` green |
| REQ-008 | The slice shall pass the FULL `bin/gate.sh` (coverage + mutation; Engine MSI ≥ 80). | gate run (FULL) |

## Locked-In Decisions
- **`ActionButton` added to `EventTrigger`**; Autorun/Parallel **deferred**.
- **`IMapEvent.Order` as a non-breaking `int Order => 0;`** default-interface member (Abstractions is net10.0 ⇒
  supported; LeverEvent/ChestEvent/the scaffolded skeleton keep compiling at Order 0).
- **Player `Facing`** reuses `Engine.World.Direction`; the faced cell = `Player.Cell + Facing.ToStep()`; updated
  on **every** `MovePlayer` (incl. a blocked move — turn in place).
- **An ordered, ambiguity-checked dispatch** — within a (cell, trigger) group, ascending `Order`; equal Order =
  a **typed error** at sim build/load (mirror the `.expect`/map-loader totality — never a throw on validation).
- **Determinism:** per-group order total (ties disallowed); MRM-clean (sort a list, do NOT enumerate a
  `Dictionary` for ordered output).
- **Out:** Autorun/Parallel + module-registration (follow-up); visual UI; save/load (#15); unified-entity (#14).

### OPEN — Phase 2 (design decides)
- **(a)** Where the ambiguity check lives — a `HookSchedule`/`EventDispatcher` built from the events returning
  `Ok | Error`, consumed by `WorldSim`, vs a `WorldSim.TryCreate(...)` factory. Reconcile with the current
  null-guard-throwing ctor (the ambiguity is a typed result, distinct from null guards).
- **(b)** Where `Facing` lives — `Entity`/`Actor.Facing` (general actor state) vs WorldSim-tracked. Lean Entity.
- **(c)** Grouping + iteration kept MRM-clean + deterministic (sort, no dict-foreach).
- **(d)** Confirm the seam change stays additive (scaffolder skeleton + #10 oracle compile + stay green).

## Linked Artifacts
- Design docs: `docs/agentic-substrate.md` (§2 hook lifecycle, §1 seam rings), `docs/product-phasing.md` (#13)
- Ticket doc: docs/planning/tickets/open/TICKET-0013-p0-triggers.md
- Forge ticket: 0e3ab789-cc7f-48f5-ae0e-fc0854e8ee85 (#13, project monorpgmaker `a9ee8162-3cfd-4c99-be5c-bbdb4be32b10`)
- Builds on: `IMapEvent`/`EventTrigger` (Abstractions), `WorldSim` (Engine.Sim), `Direction`/`DirectionExtensions` (Engine.World), `Actor`/`Entity` (Engine.Entities)
- Reuse PRs: PR-claude-total-parser-substring-overlap-001, PR-claude-long-product-dimension-guard-001, PR-claude-analyzer-mutation-isolated-asserts-001
- Ground-truth anchors: `src/MonoRpgMaker.Abstractions/{EventTrigger,IMapEvent}.cs`, `src/MonoRpgMaker.Engine/Sim/WorldSim.cs` (FireStepOn), `src/MonoRpgMaker.Engine/Entities/Actor.cs`, `src/MonoRpgMaker.Engine/World/Direction.cs`, `src/MonoRpgMaker.Engine/Sim/Tracer/TracerRoom.cs`
- AAR: b1e4e334-b1bb-4247-8ab3-2e48533bdc89

## Phase Plan
| Phase | Status |
|---|---|
| 1 — Plan | PASS |
| 2 — Design | PASS |
| 3 — Implement | PASS |
| 3.5 — Inspect | PASS |
| 4 — Validate | PASS |
| 5 — Complete | PASS |
