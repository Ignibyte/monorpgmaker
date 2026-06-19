---
pipeline_id: bc5d9639-8a7a-4950-bf47-6c6bd17a1529
title: WORK-p0-map-data
ticket: a9c6b9f7-fc66-4f7a-b7d8-5cc99e427e16
type: work
intake:
notes: WORK-p0-map-data.notes.md
status: Phase 5 — Complete PASS
---

# WORK-p0-map-data

> Pipeline spec (always-loaded contract). Detail per phase lives in the paired `.notes.md`.

## Work Spec
- **Title:** P0 #12 — content / `$data`: a serializable JSON `TileMap` format + a pure deterministic
  serializer/loader, making a map **authorable as data** (the precondition for multiple maps, the editor,
  and save/load). MVP = the **tile map only**.
- **Scope:**
  *In* — a System.Text.Json `$data` schema for a `TileMap` (informed parity, our schema; int/bool only); a
  pure `MapSerializer` + `TileMapData` DTO in Engine (`Serialize(TileMap)→string`,
  `Deserialize(string)→LoadResult<TileMap>` with typed errors, never a throw on parse); a committed sample
  `$data` file; round-trip + sample-load + malformed tests. FULL gate green.
  *Out (deferred)* — placed-events/database from `$data` (follow-up); the editor GUI; runtime save/load
  (#15); tileset IMAGES.
- **Systems:** **engine/world+data** (the serializer + DTO + the sample) · consumes `TileMap`/`Tile` ·
  the host reads the file (not the Engine) · tests.

## Acceptance Criteria (EARS)
| ID | EARS Requirement | Verification |
|---|---|---|
| REQ-001 | `MapSerializer.Serialize(TileMap)` shall produce a JSON `$data` string capturing width, height, and each cell's `tilesetId` + `blocking` (row-major). | unit test |
| REQ-002 | A `Serialize → Deserialize` round-trip shall yield a `TileMap` structurally equal to the original. | round-trip test |
| REQ-003 | When the input is malformed (invalid JSON, non-positive dims, `width*height != tiles.Length`, missing fields), `Deserialize` shall return a typed error (reason), never throw (§14). | unit test per case |
| REQ-004 | A committed sample `$data` map file shall `Deserialize` into a `TileMap` with the expected dims + tiles. | sample-load test |
| REQ-005 | The serializer + DTO shall be integer/bool-only + deterministic (no float/clock/RNG); the MRM analyzer stays green over it. | build `-warnaserror` + review |
| REQ-006 | The Engine shall stay framework-thin — the serializer operates on a string (no file IO); file reading is the host's job. | review |
| REQ-007 | The slice shall pass the FULL `bin/gate.sh` (coverage + mutation). | gate run (FULL) |

## Locked-In Decisions
- **JSON `$data` via System.Text.Json** (informed parity — our own schema, not byte-compat); int/bool only.
- **A pure `MapSerializer` + `TileMapData` DTO in Engine**; `Deserialize → LoadResult<TileMap>` (typed error,
  **no throw** on the parse path — mirror the `.expect` parser's totality); reject non-positive dims +
  `width*height != tiles.Length`. The **file read stays in the host** (Engine framework-thin).
- **Determinism:** int/bool only ⇒ replay stays bit-identical; MRM-clean; prefer the **STJ source-generator
  context** (the sanctioned no-reflection carve-out, AOT-safe) over reflection-based serialization.
- **MVP = the tile map only.** Placed-events + the database from `$data` are **deferred** follow-up tickets.
- **Out:** editor GUI; events/database from data; runtime save/load (#15); tileset images.

### OPEN — Phase 2 (design decides)
- **(a) Schema shape** — per-cell objects `[{tilesetId,blocking}]` vs two parallel arrays (`int[] tilesetIds`
  + a `bool[]`/bitmask `blocking`). (Lean: per-cell objects for clarity, or parallel arrays for compactness.)
- **(b) Loader home + names** — `Engine.World` (next to `TileMap`) vs a new `Engine.Data` serialization area;
  `MapSerializer` / `TileMapData` / `LoadResult` naming.
- **(c) STJ source-gen vs reflection** — a `[JsonSerializable]` `JsonSerializerContext` (preferred) vs
  reflection-based.
- **(d) TracerRoom integration** — rewire `TracerRoom` to load its map from a sample `$data` (behaviour-
  preserving, guarded by `TracerSliceTests`) **now**, vs ship the serializer + sample + tests and defer the
  rewire. (Lean: include the integration if clean — strongest proof — else defer.)
- **(e) The `TileMap` structural-equality** helper for the round-trip test.

## Linked Artifacts
- Design docs: `docs/decisions.md` (informed parity), `docs/feature-research.md` (content layout),
  `docs/agentic-substrate.md` (§1 "Project = DATA + MODULES"; STJ source-gen carve-out), `docs/product-phasing.md` (#12)
- Ticket doc: docs/planning/tickets/open/TICKET-0012-p0-map-data.md
- Forge ticket: a9c6b9f7-fc66-4f7a-b7d8-5cc99e427e16 (#12, project monorpgmaker `a9ee8162-3cfd-4c99-be5c-bbdb4be32b10`)
- Builds on: `Engine.World` `TileMap`/`Tile`; mirrors the `.expect` parser totality (#10)
- Reuse PRs: PR-claude-total-parser-substring-overlap-001, PR-claude-analyzer-mutation-isolated-asserts-001
- Ground-truth anchors: `src/MonoRpgMaker.Engine/World/TileMap.cs` + `Tile.cs` (what's serialized),
  `src/MonoRpgMaker.Engine/Sim/Tracer/TracerRoom.cs` (the integration target), `src/MonoRpgMaker.Editor/Expectations/ExpectationParser.cs` (the typed-result/total-parser pattern)
- AAR: 57a27055-9109-4cf5-968c-f9a9b8450423

## Phase Plan
| Phase | Status |
|---|---|
| 1 — Plan | PASS |
| 2 — Design | PASS |
| 3 — Implement | PASS |
| 3.5 — Inspect | PASS |
| 4 — Validate | PASS |
| 5 — Complete | PASS |
