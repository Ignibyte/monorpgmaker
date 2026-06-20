---
pipeline_id: 714d59a1-ddf0-4dce-a8ec-78db74007d00
title: WORK-doors
ticket: c403e740-3568-46f5-836b-0580b8ba1b1a
type: work
intake: none (direct /work request under a standing /goal — the library, door)
notes: WORK-doors.notes.md
status: Phase 5 — Complete PASS
---

# WORK-doors

> Pipeline spec (always-loaded contract). Detailed per-phase work lives in the
> paired `.notes.md`. Phase advance = updating the `status:` frontmatter above.

## Work Spec
- **Title:** Doors — switch-driven passability from `$data` in the multi-map runtime + a placeable **`Lever`** built-in.
- **Scope:**
  - *In (engine — doors-from-`$data`)* — `TileMapData` gains a `Doors` array (`DoorData = { X, Y, Switch,
    ClosedTile, OpenTile }`, each tile a `TileData{TilesetId,Blocking}` — mirrors the existing tile/event DTOs);
    `MapSerializer` (de)serialises it **totally** (a malformed door — off-map cell / empty switch — → the existing
    typed `MapLoadResult.Failure`, never a throw); `MapLoadResult` exposes `Doors`. `MapSerializer.Serialize`
    gains a **defaulted** doors param (keeps the existing call-sites green). `GameSession.LoadMap` **and** the
    single-map `StartMap.CreateWorld` convert the loaded `DoorData` → `DoorRule[]` and pass them to
    `WorldSim.TryCreate` (replacing `Array.Empty<DoorRule>()`). `WorldSim.SyncDoors` (switch-driven, re-synced each
    step) is unchanged.
  - *In (a placeable `Lever` built-in — contract-first D-0024)* — a parameterized switch-setter (kind `Lever`,
    params `[switch, message]`): `Run => [SetSwitch(switch, true), ShowMessage(message)]` (give-once — nothing once
    the switch is set), registered in `BehaviourRegistry` + an `.expect` (gate:13) + a `HandlerRegistry` entry.
    Composes EXISTING outcomes — **no new `Outcome` case** (like Chest/GiveItem). Named to avoid clashing with the
    #13 tracer `Sim/Tracer/LeverEvent`.
  - *In (a bundled demo)* — the start map's `$data` gets a door + a `Lever` placement (regen `content/maps/
    start.json`), so the lever→door works end-to-end through `GameSession`.
  - *Out / deferred* — **Studio door-tile authoring** (defining a `DoorRule` visually — doors are authored in
    `$data` for now; the `Lever` EVENT is placeable via the #22 inspector). Auto-close / animated doors. **Shop**
    (the remaining built-in — a buy/sell UI; a separate later feature).
- **Systems:** map/tilemap (`$data` doors) · save/load (`MapSerializer`/`MapLoadResult`) · sim
  (`GameSession`/`WorldSim` doors) · events (the `Lever` built-in + registry + `.expect`) · runtime (the bundled door demo).

## Acceptance Criteria (EARS)

| ID | EARS Requirement | Verification |
|---|---|---|
| REQ-001 | A map's `$data` doors (cell + switch + closed/open tiles) shall round-trip through `MapSerializer` (serialize→deserialize), exposed on `MapLoadResult.Doors`. | unit |
| REQ-002 | `GameSession.LoadMap` shall pass a loaded map's doors to the `WorldSim`, so a door tile reflects its switch (synced each step — switch unset → the closed tile; set → the open tile). | unit |
| REQ-003 | A `Lever` built-in (params `switch` + `message`) shall be registered + materialise, and on its trigger return `[SetSwitch(switch, true), ShowMessage(message)]` (nothing once the switch is set) — placeable, `.expect`-validated. | unit + gate:13 |
| REQ-004 | A malformed door in `$data` (off-map cell or empty switch) shall be a typed `MapLoadResult.Failure` (never a throw); bad `Lever` params → `BehaviourResult.Failure`. | unit |
| REQ-005 | The bundled start map shall carry a door + a `Lever` placement, so setting the lever's switch opens the door tile end-to-end through `GameSession`. | unit + manual smoke |
| REQ-006 | The FULL `bin/gate.sh` shall be green (coverage + mutation + the `.expect` oracle), and the golden `start.json` shall match `MapSerializer.Serialize(StartMap.Build(), StartMap.Events(), StartMap.Doors())`. | gate |

## Locked-In Decisions
- **Doors are embedded `$data` (D-0023):** `TileMapData.Doors` (a `DoorData` DTO mirroring `TileData`/`EventData`);
  `MapSerializer` round-trips them totally (malformed → `MapLoadResult.Failure`). `Serialize` gains a **defaulted**
  doors param so the existing call-sites + the editor (which doesn't author doors yet) stay green.
- **The sim-host passes the doors (D-0026):** `GameSession.LoadMap` + `StartMap.CreateWorld` thread the loaded
  `DoorData → DoorRule[]` into `WorldSim.TryCreate`; `WorldSim.SyncDoors` (existing, switch-driven) flips the tile.
  A door opens by a behaviour `SetSwitch`-ing the door's switch → SyncDoors — **no `SetTile` outcome** needed.
- **The `Lever` built-in is contract-first (D-0024) + declarative (D-0017):** one `Registration` + `.expect` +
  handler; composes `SetSwitch` + `ShowMessage` (no new `Outcome`/applier/parser). The new class is named to avoid
  the tracer `LeverEvent` clash.
- **Deferred:** Studio door-tile authoring; auto-close doors; **Shop** (a buy/sell UI). Determinism; MRM-clean.

## Linked Artifacts
- Design docs: `docs/decisions.md` (D-0017, D-0023, D-0024, D-0026), `docs/roadmap.md`.
- Intake doc: none (direct `/work` under a standing `/goal`).
- Ticket doc: `docs/planning/tickets/open/TICKET-0026-doors.md`
- Forge ticket: c403e740-3568-46f5-836b-0580b8ba1b1a (#26)

## Phase Plan
| Phase | Status |
|---|---|
| 1 — Plan | PASS (autonomous — standing /goal; doors-from-$data + a placeable Lever; Studio authoring + Shop deferred) |
| 2 — Design | PASS (doors-from-$data + DoorRule.FromData + SwitchEvent[kind Lever]; defaulted Serialize doors; golden regen) |
| 3 — Implement | PASS (doors-from-$data + Lever; build 0/0; oracle 11/7; golden regen; GATE GREEN [fast]) |
| 3.5 — Inspect | PASS (1 critic, all lenses cleared w/ repros; +4 mutation-coverage tests [bounds Theory + null-tile + no-doors-key + Lever-msg]; no code defect) |
| 4 — Validate | PASS (518 tests; cov 92.9%; MSI Engine 83.92/Abs 82.22/Analyzers 91.18/Editor 84.03; oracle 11/7; GATE GREEN [full]) |
| 5 — Complete | PASS (docs touched; AAR closed; ticket #26 done; archived) |
