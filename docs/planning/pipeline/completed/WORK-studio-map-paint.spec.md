---
pipeline_id: e7d20190-09f4-481c-a38b-2c8ff74805d1
title: WORK-studio-map-paint
ticket: 979ed134-7fce-4757-9533-d0fb4e2e71e3
type: work
intake:
notes: WORK-studio-map-paint.notes.md
status: Phase 5 — Complete PASS
---

# WORK-studio-map-paint

> Pipeline spec (always-loaded contract). Detail per phase lives in the paired `.notes.md`.

## Work Spec
- **Title:** Studio v1 — a **Tiled-like map-paint editor** in **Avalonia** (the maker tool's first GUI). A window
  with a **sprite-tile map canvas + a sheet-sliced palette** painting from a committed **LPC tileset**, plus
  click/drag painting and New/Save/Load `$data`. The testable logic lives in `MonoRpgMaker.Editor` (gated); the
  Avalonia `Studio` exe is a thin host.
- **Scope:**
  *In* — a small **tileset model** (sheet + tile size + columns; `TilesetId` → a source rect — pure int math,
  tested); a pure, tested editor view-model in `MonoRpgMaker.Editor` (point→cell, palette/active-tile selection,
  paint over `MapEditor`, new/save/load orchestration over `MapSerializer` **strings**); a new
  `MonoRpgMaker.Studio` Avalonia exe (window + a custom `MapCanvas` that **blits sprite tiles** from the LPC
  sheet + a **sheet palette** + paint + file dialogs) as a thin `[ExcludeFromCodeCoverage]` host; the committed
  **LPC tileset** (CC-BY-SA, `assets/tilesets/lpc/`) + credits; the gate plumbing (Avalonia license + lock).
  *Out (deferred)* — **autotiles** (the LPC terrain sheets are autotile-oriented; v1 is plain index paint),
  layers/regions/per-tile metadata; multi-tileset / a `$data` tileset reference (v1 = one fixed tileset);
  the database/Modules/event editors; undo/redo; multi-map; placing events/entities; the UI skin.
- **Systems:** **editor** (the tileset model + view-model — gated logic) · NEW **studio** (the Avalonia host) ·
  consumes `MapEditor`/`MapSerializer`/`TileMap` · the committed LPC art · build/license/lock plumbing · tests.

## Acceptance Criteria (EARS)
| ID | EARS Requirement | Verification |
|---|---|---|
| REQ-001 | The editor view-model shall map a pointer pixel (given a cell size) to the correct grid cell, and report out-of-bounds for a point outside the map. | unit test |
| REQ-002 | The tileset model shall map a `TilesetId` to the correct source rect in the sheet (`col = id % Columns`, `row = id / Columns`; pixel rect at `TileSize`). | unit test |
| REQ-003 | Selecting a palette entry (a sheet tile index) shall set the active `Tile` used for painting. | unit test |
| REQ-004 | Painting at an in-bounds cell shall set that cell to the active `Tile` (via `MapEditor`); out-of-bounds shall be a no-op. | unit test |
| REQ-005 | "New" shall produce a fresh, all-empty map of the requested size. | unit test |
| REQ-006 | A Save → Load round-trip (through the `MapSerializer` string) shall reproduce a structurally-equal `TileMap`. | unit test |
| REQ-007 | A malformed load string shall surface the typed error to the view-model without throwing/crashing. | unit test |
| REQ-008 | The Avalonia `Studio` app shall launch and **render the painted map as LPC sprite tiles**, support paint / save / load (the thin view is `[ExcludeFromCodeCoverage]`). | manual smoke |
| REQ-009 | The slice shall pass the FULL `bin/gate.sh` (Editor coverage + MSI ≥ 80 incl. the model + view-model; Avalonia licensed + pinned; the LPC art credited; Studio exempt as a host). | gate run (FULL) |

## Locked-In Decisions
- **Avalonia is the editor GUI framework** (was unpinned) — for the full maker GUI. → `architecture-decision-record` at complete.
- **Tiled-like with real LPC tileset sprites** (the user's call) — v1 paints **sprite tiles** from a committed
  **LPC Base Assets** sheet (Sharm; CC-BY-SA 3.0 / GPL 3.0 / OGA-BY 3.0), not flat colors. The art lives in
  `assets/tilesets/lpc/` (`lpc-grass/dirt/water/mountains.png`, 32×32) with `CREDITS.md` + the upstream
  `LPC-CREDITS.TXT`; art is licensed separately from the MIT code.
- **A small tileset model** (`TileSize`, `Columns`, `TilesetId` → source rect — pure int math) + **the pure
  editor view-model** live in `MonoRpgMaker.Editor` (already coverage+mutation-gated; NOT MRM-gated, so float
  pixel-math is legal); tested to Editor MSI ≥ 80. They orchestrate `MapEditor` + `MapSerializer` over **strings**.
- **A new `MonoRpgMaker.Studio` Avalonia exe is a thin HOST** (the Player pattern): App + MainWindow + a custom
  `MapCanvas` (loads the LPC sheet as a Bitmap, blits each cell's source rect; routes pointer events to the
  view-model) + a sheet palette + Save/Load file dialogs. `[ExcludeFromCodeCoverage]`, **not** in gate:12, **not**
  test-referenced (so it does not drag coverage). Owns all file IO + the Bitmap.
- **v1 = one fixed tileset** (no `$data` tileset reference, no multi-tileset, no autotiles); single map.
- **Gate plumbing:** Avalonia (MIT) → `.config/nuget-license-allowed.json` (+ overrides if needed); regen
  `packages.lock.json`; gate:4 vuln clean; solution builds `-warnaserror`. The committed PNGs are art (not NuGet)
  so gate:5 is unaffected; they carry their own license file.
- **Out:** autotiles; layers/regions/metadata; multi-tileset / `$data` tileset refs; database/Modules/event
  editors; undo/redo; multi-map; placing events/entities; the UI skin; tileset-image *import* UI.

### OPEN — Phase 2 (design decides)
- The default v1 sheet (e.g. `lpc-mountains.png` — the richest, 12×9=108 tiles) and whether v1 offers a simple
  switch among the committed sheets or fixes one.
- The model + view-model types/API (`Tileset`, `MapPaintSession`/view-model) + whether the view-model is
  `INotifyPropertyChanged` (binding) or a plain model the code-behind drives.
- The `MapCanvas` render (Avalonia `DrawingContext`/`RenderTargetBitmap`; source-rect blit; a blocking
  indicator) + pointer routing; the file-dialog wiring (`StorageProvider`); how the sheet PNG is delivered to
  Studio (content-copy vs embedded resource).
- The `Studio` project/axaml layout + the exact Avalonia packages (Avalonia, Avalonia.Desktop, Themes.Fluent…).

## Linked Artifacts
- Design docs: `docs/roadmap.md` (the visual editor track), `docs/technical-architecture.md` (Editor "GUI later"), `docs/feature-research.md` (the three editor pillars), `docs/product-phasing.md` (Phase E)
- Ticket doc: docs/planning/tickets/open/TICKET-0016-studio-map-paint.md
- Forge ticket: 979ed134-7fce-4757-9533-d0fb4e2e71e3 (#16, project monorpgmaker `a9ee8162-3cfd-4c99-be5c-bbdb4be32b10`)
- Builds on: `MapEditor`, `MapSerializer`/`MapLoadResult` (#12), `TileMap`/`Tile`; the Player (GUI-host precedent)
- Committed art: `assets/tilesets/lpc/` (LPC Base Assets by Sharm; CC-BY-SA 3.0 / GPL 3.0 / OGA-BY 3.0)
- Reuse PRs: PR-claude-analyzer-mutation-isolated-asserts-001, PR-claude-readonly-collection-for-true-immutability-001
- Ground-truth anchors: `src/MonoRpgMaker.Editor/MapEditor.cs`, `src/MonoRpgMaker.Engine/World/MapSerializer.cs`, `src/MonoRpgMaker.Player/MonoRpgMaker.Player.csproj`, `bin/gate.sh` (gate:12 list), `.config/nuget-license-allowed.json`
- AAR: ec48739d-bd84-4852-aff4-93d975b54162

## Phase Plan
| Phase | Status |
|---|---|
| 1 — Plan | PASS (reviewed) |
| 2 — Design | PASS (reviewed) |
| 3 — Implement | PASS |
| 3.5 — Inspect | PASS |
| 4 — Validate | PASS (gate 13/13 green) |
| 5 — Complete | PASS |
