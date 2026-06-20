---
title: TICKET-0026-doors
status: closed
ticket: c403e740-3568-46f5-836b-0580b8ba1b1a
ticket_number: 26
type: feature
created: 2026-06-20
intake:
pipeline_spec: docs/planning/pipeline/completed/WORK-doors.spec.md
---

# TICKET-0026-doors

## Summary

**Doors — switch-driven passability from `$data`** in the multi-map runtime + a placeable **`Lever`** built-in.
`WorldSim` already consumes `DoorRule`s + `SyncDoors()` (a door tile reflects its switch), but
`GameSession.LoadMap` passes `Array.Empty<DoorRule>()` and the map `$data` has no door fields. This slice adds a
`Doors` array to `TileMapData`/`MapSerializer` (cell + switch + closed/open tiles, total round-trip), threads the
loaded doors through `GameSession.LoadMap` + `StartMap.CreateWorld` to `WorldSim`, adds a placeable contract-first
`Lever` built-in (sets a switch + a message; D-0024), and bundles a lever→door demo in the start map. The library's
door (after Chest + GiveItem). Shop (a buy/sell UI) is the remaining built-in, a separate later feature.

## Why

A door is the most-requested no-code interaction after a chest, and the runtime already has the door MECHANISM
(`WorldSim.SyncDoors`) — it just never receives doors. Wiring doors-from-`$data` makes a placed `Lever` actually
open a door in the bundled (and any) multi-map game, completing the lever→door loop the #13 tracer only demoed.

## EARS Requirements

See the spec (`WORK-doors.spec.md`) — REQ-001 doors round-trip; REQ-002 `GameSession` passes doors (they sync);
REQ-003 the `Lever` built-in; REQ-004 totality (malformed door / bad params); REQ-005 the bundled lever→door demo;
REQ-006 FULL gate + the golden `start.json`.

## Constraints

D-0023 (doors are embedded `$data`), D-0026 (`GameSession` passes the doors to `WorldSim`), D-0024 (the `Lever`
built-in via the single descriptor + `.expect` + handler), D-0017 (compose `SetSwitch` + `ShowMessage` — no new
`Outcome`/applier/parser), totality, determinism, MRM-clean. Studio door-authoring + Shop deferred.
