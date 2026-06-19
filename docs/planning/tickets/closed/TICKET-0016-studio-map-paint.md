---
title: TICKET-0016-studio-map-paint
status: closed
ticket: 979ed134-7fce-4757-9533-d0fb4e2e71e3
ticket_number: 16
type: feature
created: 2026-06-19
intake:
pipeline_spec: docs/planning/pipeline/active/WORK-studio-map-paint.spec.md
---

# TICKET-0016-studio-map-paint

## Summary

The maker tool's **first GUI**: a visual map-paint editor in **Avalonia** (locked as the editor GUI
framework). A window with a map canvas, a tile palette, click/drag painting, and New/Save/Load `$data`.
The testable logic lives in `MonoRpgMaker.Editor` (gated); the Avalonia `Studio` exe is a thin host (the
Player pattern). Flat-color tiles for v1 (no tileset images yet).

## EARS Requirements

| ID | EARS Requirement | Verification |
|---|---|---|
| REQ-001 | The editor view-model shall map a pointer pixel (given a cell size) to the correct grid cell, and report out-of-bounds for a point outside the map. | unit test |
| REQ-002 | Selecting a palette entry shall set the active `Tile` used for painting. | unit test |
| REQ-003 | Painting at an in-bounds cell shall set that cell to the active `Tile` (via `MapEditor`); painting out-of-bounds shall be a no-op. | unit test |
| REQ-004 | "New" shall produce a fresh, all-empty map of the requested size. | unit test |
| REQ-005 | A Save → Load round-trip (through the `MapSerializer` string) shall reproduce a structurally-equal `TileMap`. | unit test |
| REQ-006 | A malformed load string shall surface the typed error to the view-model without throwing/crashing. | unit test |
| REQ-007 | The Avalonia `Studio` app shall launch and support paint / save / load (the thin view is `[ExcludeFromCodeCoverage]`). | manual smoke |
| REQ-008 | The slice shall pass the FULL `bin/gate.sh` — `Editor` coverage + MSI ≥ 80 incl. the new view-model; Avalonia licensed (SPDX) + pinned (lock); `Studio` exempt as a host. | gate run (FULL) |

## Scope

- **In:** a pure, tested editor view-model in `MonoRpgMaker.Editor` (point→cell, palette, paint, new, save/load
  orchestration over `MapSerializer` strings); a new `MonoRpgMaker.Studio` Avalonia exe (window + map canvas +
  palette + paint + file dialogs) as a thin host; the gate plumbing (Avalonia license + lock).
- **Out (deferred follow-ups):** layers / autotiles / regions / per-tile metadata; tileset **images**; the
  database editor + the Modules panel + the (agentic-replaced) event editor; undo/redo; multi-map / map list;
  placing events/entities on the map; the fantasy UI skin.

## Notes

- Forge ticket: 979ed134-7fce-4757-9533-d0fb4e2e71e3 (#16, project monorpgmaker `a9ee8162-3cfd-4c99-be5c-bbdb4be32b10`)
- **Locks Avalonia** as the editor GUI framework (was unpinned) — an `architecture-decision-record` at complete.
- Builds on: `MapEditor` (paint/clear), `MapSerializer`/`MapLoadResult` (#12), `TileMap`/`Tile`; the Player as the GUI-host precedent.
- Reuse PRs: PR-claude-analyzer-mutation-isolated-asserts-001, PR-claude-readonly-collection-for-true-immutability-001, MapSerializer totality.
- Related docs: `docs/roadmap.md` (the visual editor track), `docs/technical-architecture.md` (Editor "GUI later"), `docs/feature-research.md` (the three editor pillars).
- Active pipeline: docs/planning/pipeline/active/WORK-studio-map-paint.spec.md
