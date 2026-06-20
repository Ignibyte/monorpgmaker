---
title: TICKET-0025-builtin-library
status: closed
ticket: c33b2570-1ba3-452b-baac-dca68a1854b6
ticket_number: 25
type: feature
created: 2026-06-20
intake:
pipeline_spec: docs/planning/pipeline/completed/WORK-builtin-library.spec.md
---

# TICKET-0025-builtin-library

## Summary

**The built-in behaviour library — placeable `Chest` + `GiveItem`** via the contract-first recipe (D-0024): new
parameterized `IMapEvent` behaviours that compose the existing outcomes (`AddCounter` for items-as-counters,
`SetSwitch` for give-once, `ShowMessage`), each registered in `BehaviourRegistry` + an `.expect` (gate:13) + a
`HandlerRegistry` entry, placeable via the #22 kind-aware Studio inspector. Slice C of three follow-ups
(re-skin ✓ → save/load ✓ → the library). **Door** + **Shop** are deferred (see below).

## Why

Today only `ShowText` + `Warp` are placeable no-code behaviours. A trigger program needs a LIBRARY — the whole
point of the contract-first registry (D-0024) is that built-ins (and future agent-authored kinds) are
interchangeable. `Chest` + `GiveItem` are the cleanest next built-ins: they need no new engine machinery (items
are counters; they compose existing outcomes), so they prove the library grows by the recipe alone.

## Scope note (deferrals)

- **Door** — a placeable door that actually opens needs `DoorRule`-from-`$data` (the runtime currently passes no
  doors to the multi-map session) — an engine change; the immediate NEXT slice.
- **Shop** — a buy/sell screen + inventory/currency UI; a separate feature.

## EARS Requirements

See the spec (`WORK-builtin-library.spec.md`) — REQ-001 GiveItem grants + message; REQ-002 Chest give-once;
REQ-003 registered + `.expect` + handler (D-0024); REQ-004 bad params → typed `Failure`; REQ-005 the inspector
renders the new ParamKeys (single-source); REQ-006 FULL gate.

## Constraints

D-0024 (the single-descriptor recipe), D-0017 (return outcomes — compose the existing vocabulary; no new `Outcome`
case / applier / parser arm), items-as-`GameState`-counters (no new inventory), determinism, MRM-clean, gate:13.
