---
pipeline_id: 6a36451b-b4b9-48c2-8f01-ed6f235c7251
title: WORK-p0-eventseam
ticket: 745c603e-fceb-44b3-b2c0-b002bb871c7e
type: work
intake:
notes: WORK-p0-eventseam.notes.md
status: Phase 5 — Complete PASS
---

# WORK-p0-eventseam

> Pipeline spec (always-loaded contract). Detailed per-phase work lives in the
> paired `.notes.md`. Phase advance = updating the `status:` frontmatter above.

## Work Spec
- **Title:** P0 slice 3b — the event-seam extraction: move `IMapEvent` / `EventTrigger`
  + a new `IEventContext` into `MonoRpgMaker.Abstractions` (the published seam authored
  events implement), consuming slice-3a's `GridPoint`.
- **Scope:** *In* — **Abstractions** gains `EventTrigger` (moved enum), `IMapEvent` (moved;
  `GridPoint Cell`, `EventTrigger Trigger`, `Run(IEventContext)`), and `IEventContext` (new
  verb surface). **Engine** keeps `GameState` (unchanged) + `EventContext` (now
  `: IEventContext`, verbs delegating to `GameState` + the message sink), gains a
  `Point`↔`GridPoint` adapter + the Engine→Abstractions `ProjectReference`. **Tracer**
  migrates: `LeverEvent`/`ChestEvent` (GridPoint ctor + verb calls), `WorldSim`
  (`Abstractions.IMapEvent` + adapter compare), `TracerRoom` (GridPoint), `TracerSliceTests`
  (updated). Delete the old Engine `Sim/EventTrigger.cs` + `Sim/IMapEvent.cs`. FULL gate green.
  *Out* — the float-ban analyzer; the generator/validator/scaffolding. `DoorRule` stays
  `Point`-based; the World/Entities/host stay on XNA `Point` (only the event seam migrates).
- **Systems:** **abstractions** (new contracts) · **events/sim** (the seam + tracer migration) · tests.

## Acceptance Criteria (EARS)
| ID | EARS Requirement | Verification |
|---|---|---|
| REQ-001 | `IMapEvent`, `EventTrigger`, `IEventContext` shall live in `MonoRpgMaker.Abstractions`. | build + NetArchTest ring |
| REQ-002 | `IMapEvent.Cell` shall be a `GridPoint` (not XNA `Point`). | build + review |
| REQ-003 | `IEventContext` shall expose `GetSwitch`/`SetSwitch`/`GetCounter`/`AddCounter`/`ShowMessage`; `EventContext` (Engine) shall implement it, delegating to `GameState` + the message sink. | unit test |
| REQ-004 | The tracer events shall implement `Abstractions.IMapEvent` with behaviour unchanged (lever opens the door once; chest gives one potion then reads empty). | TracerSliceTests (updated) |
| REQ-005 | A `Point`↔`GridPoint` adapter shall convert both ways; `WorldSim` shall fire step-on events by comparing the player cell to `IMapEvent.Cell` through it. | unit test + tracer tests |
| REQ-006 | `MonoRpgMaker.Abstractions` shall remain MonoGame-free and shall not depend on the Engine. | NetArchTest ring |
| REQ-007 | The slice shall pass the FULL `bin/gate.sh` (Engine + Abstractions MSI ≥ floor). | gate run (FULL) |

## Locked-In Decisions
- **Contracts move; runtime stays.** `IMapEvent` / `EventTrigger` / `IEventContext` are the
  published seam → `Abstractions` (D-0014). `GameState` + `EventContext` are runtime →
  stay in `Engine`; `EventContext` implements `IEventContext`. (The move-vs-abstract call:
  **abstract** the context behind an interface, don't drag `GameState` into Abstractions.)
- **`IEventContext` = the semantic-verb surface** (`GetSwitch`/`SetSwitch`/`GetCounter`/
  `AddCounter`/`ShowMessage`) — events call verbs, never poke a `GameState` object
  (agentic-substrate §4). This is the embryo of the P3 `EventContext` verb seam.
- **`IMapEvent.Cell : GridPoint`** (slice 3a) — a `Point`↔`GridPoint` adapter (Engine
  extension methods) bridges to the still-`Point`-based World/Entities. **No World-layer
  migration** — the ~13-file `Point` reach stays; only the event seam adopts `GridPoint`.
- Establishes the **Engine→Abstractions `ProjectReference`** (deferred since slice 1). The
  NetArchTest ring (Abstractions ⇏ MonoGame/Engine) still holds — the new contracts are pure.
- **`DoorRule` stays `Point`-based** (not an event-seam type; out of scope).
- **One slice** — the contracts are tiny (IMapEvent 16L / EventContext 29L / EventTrigger 8L /
  GameState 44L) and the edits mechanical. Behaviour is unchanged → existing `TracerSliceTests`
  (updated for `GridPoint`) is the regression guard.
- **OPEN — Phase 2:** whether `IEventContext` includes `GetCounter` (the tracer events use
  `Add` but not counter-read — include for a complete switch+counter read/write surface, with a
  test, vs. YAGNI-drop); whether `EventContext` keeps a public `State` property (WorldSim exposes
  `State` separately, so it can drop to the verb surface only); the adapter's home/name
  (`GridPointXnaExtensions`? static `ToGridPoint`/`ToXnaPoint`).

## Linked Artifacts
- Design docs: docs/decisions.md (D-0014, D-0016), docs/agentic-substrate.md (§4 seam
  taxonomy / EventContext semantic-verb surface), docs/agentic-overview.md (§3 don't-hack-core),
  docs/roadmap.md (P0; P3 event keystone)
- Intake doc: none
- Ticket doc: docs/planning/tickets/open/TICKET-0006-p0-eventseam.md
- Forge ticket: 745c603e-fceb-44b3-b2c0-b002bb871c7e (#6, project monorpgmaker
  `a9ee8162-3cfd-4c99-be5c-bbdb4be32b10`)
- Builds on: WORK-p0-gridpoint, WORK-p0-irandom-rng, WORK-p0-abstractions-fixedpoint (completed)
- AAR: b032ba16-e7a3-4e2f-adc4-60bce34ca148

## Phase Plan
| Phase | Status |
|---|---|
| 1 — Plan | PASS |
| 2 — Design | PASS |
| 3 — Implement | PASS |
| 3.5 — Inspect | PASS |
| 4 — Validate | PASS |
| 5 — Complete | PASS |
