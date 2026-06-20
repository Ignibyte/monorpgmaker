---
title: TICKET-0022-multimap-warp
status: closed
ticket: 1ba1efe9-d4d7-418e-95e1-520805d30a56
ticket_number: 22
type: feature
created: 2026-06-20
intake:
pipeline_spec: docs/planning/pipeline/completed/WORK-multimap-warp.spec.md
---

# TICKET-0022-multimap-warp

## Summary

**Multi-map + the built-in `Warp` + Studio multi-map authoring** (all-in-one — the split was declined at the
plan gate): the runtime supports more than one map and **map-to-map transitions** (step on / action a `Warp`
tile → load the target map at the target cell), **and** the Studio authors the map set + the Warp placements.
The last Studio-v2 slice *and* the start of the trigger program's Slice 2 (the built-in behaviour library). See
the spec for the full EARS (REQ-001..010); the runtime half is REQ-001..006/010, the Studio half is REQ-007..009.

## Why

The runtime today loads exactly one bundled map — a game can't have a second room, let alone a world. Multi-map
+ `Warp` is the keystone: it makes the runtime a *world* runner and lands the first behaviour that needs
cross-map state, on the same contract-first recipe (D-0024) as `ShowText`.

## EARS Requirements

| ID | EARS Requirement | Verification |
|---|---|---|
| REQ-001 | The runtime shall hold a registry of embedded maps addressable by a stable id with a start-map pointer; loading a map by id shall be total (an unknown id → a typed failure, never a throw). | unit |
| REQ-002 | A built-in `WarpEvent` (`IMapEvent`) shall, on its trigger, return a declarative `Warp` outcome carrying the target map id + cell — no direct state mutation (D-0017). | unit |
| REQ-003 | When a `Warp` outcome fires, the runtime shall switch the active map to the target and place the player at the target cell, carrying the game state (switches/counters persist across the warp). | unit + smoke |
| REQ-004 | The `Warp` behaviour shall be materialised by the `BehaviourRegistry` from a `Warp` placement (kind + params) and carry an `.expect` contract validated by gate:13. | unit + gate:13 |
| REQ-005 | A `Warp` to an unknown map id or an out-of-bounds target cell shall be handled totally (a typed failure / safe no-op), never a throw. | unit |
| REQ-006 | A save taken after a warp shall record the active map id so a load resumes the player on that map (multi-map save round-trip). | unit *(design confirms Slice-A scope vs defer)* |
| REQ-007 | The FULL `bin/gate.sh` shall be green, including the `.expect` oracle (gate:13) and the Engine coverage + mutation floors. | gate |

## Scope

- **In (Slice A):** the embedded multi-map registry + start-map pointer; a new additive `Warp` outcome; the
  `WarpEvent` built-in + its `BehaviourRegistry` registration + `.expect`; the map-switch seam (the applier
  signals a pending warp; a new gated orchestrator above `WorldSim` performs the switch, carrying `GameState`);
  totality on bad warp targets; an end-to-end runtime smoke (two maps + a Warp tile).
- **Editor (the all-in-one half):** a gated multi-map project/map-set layer (`MonoRpgMaker.Editor`) + the Studio
  multi-map UI (map list / new / switch / save the set) + the Warp event inspector (target-map dropdown + cell).
- **Out / deferred:** autorun / parallel triggers; a transition animation; the rest of the built-in library
  (Shop / GiveItem / Door — trigger Slice 2 continues separately). (Per-map tileset already exists from #20.)

## Notes

- Forge ticket: 1ba1efe9-d4d7-418e-95e1-520805d30a56 (#22, project monorpgmaker `a9ee8162-3cfd-4c99-be5c-bbdb4be32b10`)
- Related docs: `docs/decisions.md` (D-0023 repo-is-the-game; D-0024 contract-first triggers; D-0017 declarative
  outcomes; D-0022 thin host); `docs/roadmap.md` (the runtime + editor tracks).
- Builds on: `WorldSim`/`OutcomeApplier`/`Outcome` (the return-then-apply model), `BehaviourRegistry` +
  `ShowTextEvent` (the recipe, #18), `MapSerializer`/`$data` (#12), the `$game` save spine (#15), the per-map
  tileset (#20). Branch off `main` (`2cb743d`).
- Promoted from intake: none (direct request via `/work`).
- Active pipeline: docs/planning/pipeline/active/WORK-multimap-warp.spec.md
