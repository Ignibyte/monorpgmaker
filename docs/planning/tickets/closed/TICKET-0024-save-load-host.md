---
title: TICKET-0024-save-load-host
status: closed
ticket: 6ea08b5c-3302-4762-91c1-1c7be5b4f4a8
ticket_number: 24
type: feature
created: 2026-06-20
intake:
pipeline_spec: docs/planning/pipeline/completed/WORK-save-load-host.spec.md
---

# TICKET-0024-save-load-host

## Summary

**In-game save/load host** — bring the #22 REQ-006 save end-to-end. #15 built the deterministic `$game` save
serializer and #22 added `SaveState.MapId`, but nothing in-game saves or reconstructs the session. This slice
adds a gated `GameSession.TryRestore(SaveState)` (rebuild the session in place at the saved map with the restored
`GameState` + player position) and the Player host wiring (a SAVE key + a LOAD key → `Game1` file IO +
`SaveSerializer` + `TryRestore`). Slice B of three follow-ups (re-skin ✓ → save/load → built-in library).

## Why

A save you can't load isn't a save. The serializer round-trips, but a player can't press a key to persist their
progress and resume it. This closes the loop: the runtime becomes replayable across sessions (one slot, one key).

## EARS Requirements

See the spec (`WORK-save-load-host.spec.md`) for the authoritative table — REQ-001 `TryRestore` rebuilds at the
saved map + position; REQ-002 unknown saved map → `false`/no-op; REQ-003 a save→load round-trip restores
state+map+position; REQ-004 the Player saves/loads on keys (smoke); REQ-005 FULL gate.

## Constraints

D-0026 (the `GameSession` switch + injectable `GameState` — `TryRestore` reuses it), D-0022 (the host owns the
file IO; the engine never touches the filesystem), D-0023 (governs embedded CONTENT, not user SAVES — saves go to
a user-writable path), totality (an unknown saved map id → a safe no-op). `rngState` saved as `0` (no `IRandom`
yet); facing not restored (the `Actor` ctor takes no facing).
