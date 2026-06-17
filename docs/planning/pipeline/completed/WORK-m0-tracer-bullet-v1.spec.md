---
pipeline_id: 08f60125-b502-4e21-a0c9-bb891292da63
title: WORK-m0-tracer-bullet-v1
ticket: b1454ae5-81fa-4933-813e-95eaf873a8f9
type: work
intake:
notes: WORK-m0-tracer-bullet-v1.notes.md
status: Phase 5 — Complete PASS
---

# WORK-m0-tracer-bullet-v1

> Pipeline spec (always-loaded contract). Detailed per-phase work lives in the
> paired `.notes.md`. Phase advance = updating the `status:` frontmatter above.

## Work Spec
- **Title:** M0 tracer bullet — a hand-wired authored-event vertical slice that proves the
  AI-directed authoring loop before the chassis (D-0015 / roadmap P-(-1)).
- **Scope:** *In* — a small hand-painted map (int grid + minimal placeholder tileset:
  floor/wall/door/lever); 4-dir grid player movement + tile collision (reuse/extend `Entity.TryStep`
  + `TileMap.IsBlocked`); a minimal in-memory `GameState` switch store; ONE authored event (lever:
  walk-on → message-once → set `door_open`); switch-driven door passability; rendering of map +
  player + a minimal on-screen message; keyboard input; `MonoRpgMaker.Player` boots the slice; unit
  tests for the deterministic sim; the prototype-gate invocation. *Out* — the source generator,
  `Abstractions` assembly, contract-manifest, replay-hash, fixed-point, the scaffolder/`{{var}}`
  templates, `EventContext`/seam taxonomy, battle, menus, dialogue text-codes, save/load persistence,
  editor GUI, real art assets, AI-authoring tooling, coverage/mutation gates.
- **Systems:** map/tilemap · entities/actors · events · runtime/player · rendering · input · engine.

## Acceptance Criteria (EARS)
Each acceptance criterion uses EARS syntax, one observable behavior, with a verification method.

| ID | EARS Requirement | Verification |
|---|---|---|
| REQ-001 | If the player attempts to move into a tile marked impassable, then the system shall reject the move and leave the player's cell unchanged. | unit test (Entity/TileMap collision) |
| REQ-002 | When the player steps onto the lever tile, the system shall display the lever message exactly once across repeated entries and set the `door_open` switch to true. | unit test (event + game state) |
| REQ-003 | While the `door_open` switch is false the door tile shall be impassable; when it becomes true the door tile shall be passable. | unit test (passability resolution) |
| REQ-004 | When the Player application is launched, the system shall render the painted map and a player sprite and move the player one tile per arrow-key press within map bounds. | smoke check (manual run of MonoRpgMaker.Player) |
| REQ-005 | The slice's simulation code shall use no `System.Random` or `DateTime`; any randomness shall be obtained from an injected `IRandom`. | source-bans gate + review |
| REQ-006 | The prototype shall build clean under `dotnet build -warnaserror` and `dotnet format --verify-no-changes` (the relaxed prototype gate). | prototype gate run |

## Locked-In Decisions
- **Pre-chassis (D-0015):** NO source generator, NO `Abstractions` assembly, NO contract-manifest, NO
  replay-hash gate, NO fixed-point math. Those arrive in M1 (the chassis), built as an *extraction*
  from this working slice — so keep the sim logic cleanly separable from rendering/input.
- **Relaxed prototype gate (D-0015):** `dotnet format` + `dotnet build -warnaserror` + source-bans.
  Coverage and mutation floors are NOT required for this spike.
- **The authored event is hand-written C#** = the target shape the scaffolder will later emit. Do NOT
  build the generator/scaffolder here; just write the exemplar by hand.
- **Determinism discipline even pre-chassis:** inject `IRandom`; no `System.Random`/`DateTime` in sim
  code (the habit M1 formalizes with fixed-point + the analyzer — D-0016).
- **Placeholder tile art now**; real assets swapped in later (location/format TBD).
- **The timing/feel spike is the M0 GO/NO-GO:** author a "chest gives one potion, then empty" variant
  and time the AI-directed loop vs RPG Maker's click-path (target within ~3× and feels good). The
  measurement is recorded at `/pipeline:complete`.

## Linked Artifacts
- Design docs: docs/plan.md (M0), docs/roadmap.md (P-(-1)), docs/decisions.md (D-0015, D-0016),
  docs/agentic-overview.md, docs/agentic-features.md (events)
- Intake doc: none
- Ticket doc: docs/planning/tickets/open/TICKET-0001-m0-tracer-bullet.md
- Forge ticket: b1454ae5-81fa-4933-813e-95eaf873a8f9 (#1)
- AAR: 51172405-c52c-4894-b39b-fac1b02815fc

## Phase Plan
| Phase | Status |
|---|---|
| 1 — Plan | PASS |
| 2 — Design | PASS |
| 3 — Implement | PASS |
| 3.5 — Inspect | PASS |
| 4 — Validate | PASS |
| 5 — Complete | PASS |
