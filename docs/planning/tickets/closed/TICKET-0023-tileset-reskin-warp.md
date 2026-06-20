---
title: TICKET-0023-tileset-reskin-warp
status: closed
ticket: 765e62a0-a48d-4f6a-b9b5-a50d840381bf
ticket_number: 23
type: feature
created: 2026-06-20
intake:
pipeline_spec: docs/planning/pipeline/completed/WORK-tileset-reskin-warp.spec.md
---

# TICKET-0023-tileset-reskin-warp

## Summary

**Cross-map tileset re-skin on warp** — when a `GameSession` warp (#22, D-0026) switches the active map to one
with a different tileset, the runtime host reloads + applies that map's sheet (through the single-source
`TilesetCatalog`, D-0025) so it renders with its own tileset, and the bundled town re-skins to `lpc-grass` to
prove + smoke it. The #22 multi-map deferred item (notes Phase-3 item 4 + the inspect NIT). Slice A of three
follow-ups (re-skin → save/load host → built-in library).

## Why

`RpgGame` loads the tileset sheet ONCE at `Game1.LoadContent` and caches it; the camera + geometry already
recompute per-frame from `Sim.Map`, but the *texture* is stale after a warp. Today the bundled start+town share
`lpc-mountains`, so a warp never looks wrong — but the moment a game has two maps with different sheets, the
second map renders with the first's art. This closes that gap so multi-map worlds can mix tilesets.

## EARS Requirements

See the spec (`WORK-tileset-reskin-warp.spec.md`) for the authoritative table — REQ-001 re-skin on a tileset-name
change; REQ-002 resolve through the catalog; REQ-003 no churn when unchanged; REQ-004 the town uses a different
sheet (visible re-skin); REQ-005 FULL gate + the golden `town.json`.

## Constraints

D-0022 (neutral thin host — `RpgGame` signals a tileset change, the host `Game1` reloads; `RpgGame` never loads
embedded resources), D-0025 (single-source `TilesetCatalog`), D-0026 (the GameSession switch is unchanged),
determinism. The change-detect decision is a gated/pure seam (unit-coverable); the texture reload stays host.
