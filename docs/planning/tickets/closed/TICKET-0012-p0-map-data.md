---
title: TICKET-0012-p0-map-data
status: done
ticket: a9c6b9f7-fc66-4f7a-b7d8-5cc99e427e16
ticket_number: 12
type: feature
created: 2026-06-19
intake:
pipeline_spec: docs/planning/pipeline/completed/WORK-p0-map-data.spec.md
---

# TICKET-0012-p0-map-data

## Summary

Make a map **authorable as DATA** instead of hand-wired code: a JSON `$data` format for a
`TileMap` + a pure, deterministic serializer/loader. The precondition for multiple maps, the
visual editor, and save/load. MVP = the **tile map only** (placed-events + the database from
data are deferred follow-ups).

## EARS Requirements

| ID | EARS Requirement | Verification |
|---|---|---|
| REQ-001 | `MapSerializer.Serialize(TileMap)` shall produce a JSON `$data` string capturing width, height, and each cell's `tilesetId` + `blocking` (row-major). | unit test |
| REQ-002 | A `Serialize → Deserialize` round-trip shall yield a `TileMap` structurally equal to the original (same dims + every cell). | round-trip test |
| REQ-003 | When the input is malformed (invalid JSON, non-positive dims, `width*height != tiles.Length`, missing fields), `Deserialize` shall return a **typed error** (reason), never throw (§14). | unit test per case |
| REQ-004 | A committed sample `$data` map file shall `Deserialize` into a `TileMap` with the expected dimensions + tiles. | sample-load test |
| REQ-005 | The serializer + DTO shall be integer/bool-only + deterministic (no float/clock/RNG); the MRM determinism analyzer stays green over it (it lives in a sim namespace). | build `-warnaserror` + review |
| REQ-006 | The Engine shall stay framework-thin — the serializer operates on a **string** (no file IO); file reading is the host's responsibility. | review |
| REQ-007 | The slice shall pass the FULL `bin/gate.sh` (coverage + mutation; the parse/build logic mutation-tested). | gate run (FULL) |

## Scope

- **In:** a System.Text.Json `$data` schema for a `TileMap`; a pure `MapSerializer` + `TileMapData` DTO in
  Engine (`Serialize`/`Deserialize`→typed `LoadResult`); a committed sample `$data` file; round-trip +
  sample-load + malformed-input tests. FULL gate green.
- **Out (deferred follow-ups):** placed-events-from-data + the database (items/actors) from `$data`; the
  visual editor GUI; runtime save/load of `$game` state (#15); tileset IMAGES (rendering stays flat-color).

## Notes

- Forge ticket: a9c6b9f7-fc66-4f7a-b7d8-5cc99e427e16 (#12, project monorpgmaker `a9ee8162-3cfd-4c99-be5c-bbdb4be32b10`)
- Related: `docs/decisions.md` (informed-parity / MZ-era), `docs/feature-research.md` (content layout),
  `docs/agentic-substrate.md` (§1 "Project = DATA + MODULES"; STJ source-gen no-reflection carve-out),
  `docs/product-phasing.md` (#12)
- Builds on: the `TileMap`/`Tile` types (`Engine.World`); mirrors the `.expect` parser's totality
- Reuse PRs: PR-claude-total-parser-substring-overlap-001, PR-claude-analyzer-mutation-isolated-asserts-001
- Promoted from intake: none (autonomous goal run #11–#15)
- Active pipeline: docs/planning/pipeline/completed/WORK-p0-map-data.spec.md
