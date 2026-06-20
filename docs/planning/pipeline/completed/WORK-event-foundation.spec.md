---
pipeline_id: d99ac071-f98a-4482-9337-6e1eaf38e258
title: WORK-event-foundation
ticket: 4afa20c7-7974-45e1-afca-87a0c2579bb5
type: work
intake: docs/planning/intake/INTAKE-event-trigger-system.md
notes: WORK-event-foundation.notes.md
status: Phase 5 — Complete PASS
---

# WORK-event-foundation

> Pipeline spec (always-loaded contract). Detail per phase lives in the paired `.notes.md`.

## Work Spec
- **Title:** The **event-trigger foundation** (Phase A, Slice 1 of D-0024) — placed events in `$data`, a behaviour
  **registry**, and a built-in **`ShowText`** dialogue you walk up to and press Space to read. **Contract-first,
  zero agent, zero one-off code.**
- **Scope:**
  *In* — extend `$data` (`TileMapData`, Engine.World) with an optional **`Events[]`** (`{id, cell, trigger,
  behaviour{kind, params}}`), total deserialize; a **`BehaviourRegistry`** (Engine.Sim, `kind`→`IMapEvent`
  factory, unknown-kind → typed error); the built-in **`ShowTextEvent : IMapEvent`** (`Run` → `[ShowMessage(text)]`)
  + its `.expect` sample + `HandlerRegistry` entry; `StartMap.LoadWorld` materialises events via the registry →
  `WorldSim.TryCreate`; `RpgGame` (host) wires **Space→`PressAction`** + a marker at event cells; one `ShowText`
  event in `content/maps/start.json`.
  *Out (Phase B/C + later)* — the Studio UI to place events; the agent custom-module flow; Autorun/Parallel;
  multi-map/Warp; the GiveItem/Shop/Door built-ins (Slice 2); richer dialogue (portraits/choices).
- **Systems:** **world/data** (`$data` Events schema) · **sim** (registry, `ShowTextEvent`, materialisation,
  `WorldSim` dispatch) · **runtime/host** (`RpgGame` action key + marker) · the `.expect` oracle (gate:13) · tests.

## Acceptance Criteria (EARS)
| ID | EARS Requirement | Verification |
|---|---|---|
| REQ-001 | A map's `$data` shall carry optional placed events `{id, cell{x,y}, trigger, behaviour{kind, params}}`; the loader shall round-trip them and be **total** (malformed → typed `MapLoadResult` error, never a throw). | unit |
| REQ-002 | A behaviour **registry** shall materialise each placement into an `IMapEvent` by `kind`; an unknown kind / bad params → a typed failure, not a crash. | unit |
| REQ-003 | The built-in `ShowTextEvent` shall implement `IMapEvent` and `Run` → `[ShowMessage(text)]` (ActionButton); its contract shall be validated by gate:13 (a `ShowText` `.expect` sample) + unit tests for the parameterisation. | unit + gate:13 |
| REQ-004 | The runtime shall materialise the start map's `$data` events via the registry and dispatch them: pressing Space facing the event cell shall surface its message. | unit + manual |
| REQ-005 | The start map shall carry one `ShowText` event (a sign/NPC cell, ActionButton) rendered with a visible marker; pressing Space facing it shows the line. | manual smoke |
| REQ-006 | The slice shall pass the FULL `bin/gate.sh` (Engine coverage + MSI ≥ 80 incl. the schema/registry/ShowText; gate:13 incl. ShowText; Player host exempt). | gate (FULL) |

## Locked-In Decisions
- **Contract-first (D-0024) — non-negotiable.** Every behaviour implements the EXISTING **`IMapEvent`** seam (#13)
  and is bound by ONE **registry**; the recipe is always *implement `IMapEvent` → register the `kind` → declare its
  `.expect`.* No one-off triggers. Built-in + future agent kinds are interchangeable through the same interface.
- **`ShowText` reuses `ShowMessage`** — the outcome already exists in the closed `Outcome` set; `Run` →
  `[ShowMessage(text)]`. Do NOT invent a new outcome. Declarative (no direct mutation — D-0017).
- **`$data` `Events[]`** lives in `TileMapData` (Engine.World); **total** deserialize (malformed placement → typed
  failure; reuse PR-claude-validate-deserialized-elements-not-just-container).
- **`BehaviourRegistry`** (Engine.Sim): `kind`→an `IMapEvent` factory; an unknown kind / bad params surfaces a
  typed error through `LoadWorld` (never a crash). The single binding point for built-in + agent kinds.
- **Gated logic** (schema/registry/ShowText/materialisation) is MRM-clean (int/string/`GridPoint`, declarative
  outcomes). **Host** (`RpgGame`/`Game1`, `[ExcludeFromCodeCoverage]`): the Space action key + the event marker.
- The start-map event lives in **`content/maps/start.json`** (the data-driven path), regenerated.

### OPEN — Phase 2 (design decides)
- The exact `Events[]` schema shape — `params` as a typed per-kind shape vs a generic string map; favour
  STJ-source-gen-friendly + total. How the new shape composes with the existing `TileMapData` (extend it vs a
  wrapping document) without breaking #17's `start.json` round-trip / freshness test.
- Where the registry + `ShowTextEvent` live (Engine.Sim) + how the unknown-kind error threads through
  `StartMap.LoadWorld` → the runtime (and the fallback when an event fails to materialise).
- How `ShowText`'s parameterised contract is expressed to the `.expect` oracle (a sample handler in `HandlerRegistry`
  + the `.expect` file location for a non-tracer built-in) **and** unit tests for the parameterisation.
- The event marker visual (a distinct quad/tile at event cells) in `RpgGame`.

## Linked Artifacts
- Source intake: docs/planning/intake/INTAKE-event-trigger-system.md (Slice 1) — D-0024
- Ticket doc: docs/planning/tickets/open/TICKET-0018-event-foundation.md
- Forge ticket: 4afa20c7-7974-45e1-afca-87a0c2579bb5 (#18, project monorpgmaker `a9ee8162-3cfd-4c99-be5c-bbdb4be32b10`)
- Ground-truth anchors: `src/MonoRpgMaker.Abstractions/{IMapEvent,EventTrigger,Outcome}.cs`, `src/MonoRpgMaker.Engine/Sim/WorldSim.cs` (`TryCreate`/`PressAction`), `.../Sim/Tracer/LeverEvent.cs` (exemplar) + `Expectations/*.expect`, `.../World/{TileMapData,MapSerializer}.cs`, `.../Sim/StartMap.cs`, `.../Core/RpgGame.cs`, `src/MonoRpgMaker.Editor/Expectations/HandlerRegistry.cs`
- Reuse PRs: PR-claude-validate-deserialized-elements-not-just-container, PR-claude-analyzer-mutation-isolated-asserts-001
- AAR: fa3b7ace-3aa9-4cd2-a2d7-5d17799f3668

## Phase Plan
| Phase | Status |
|---|---|
| 1 — Plan | PASS |
| 2 — Design | PASS |
| 3 — Implement | PASS |
| 3.5 — Inspect | PASS |
| 4 — Validate | PASS (gate 13/13 green) |
| 5 — Complete | PASS |
