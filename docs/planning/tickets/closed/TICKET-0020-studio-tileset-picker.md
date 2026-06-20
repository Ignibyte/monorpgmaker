---
title: TICKET-0020-studio-tileset-picker
status: done
ticket: a65351b4-f606-460a-a07f-4e0392e701c6
ticket_number: 20
type: feature
created: 2026-06-20
closed: 2026-06-20
intake:
pipeline_spec: docs/planning/pipeline/completed/WORK-studio-tileset-picker.spec.md
---

# TICKET-0020-studio-tileset-picker

## Summary

A **per-map tileset picker** for the Studio map editor: the author chooses among the four committed LPC
sheets (grass / dirt / water / mountains), the choice is recorded as a **map-level tileset name in `$data`**,
and **both** the Studio canvas/palette and the **runtime Player render that chosen sheet**. The first slice of
the Studio-v2 polish arc (the slice after #19 event placement; undo/redo is the planned next slice).

## Why

#16 fixed the editor + runtime to one hardcoded sheet (`lpc-mountains.png`) and explicitly deferred
"multi-tileset / a `$data` tileset reference". Today `$data` carries no sheet identity — `TileData.TilesetId`
is a bare per-cell index, so the four committed sheets cannot be chosen per map. Recording the sheet name in
`$data` is the smallest **coherent** way to let authors pick art: a pure editor-side preview (no persistence)
would record nothing and the Player would still render mountains.

## EARS Requirements

| ID | EARS Requirement | Verification |
|---|---|---|
| REQ-001 | The map `$data` shall carry a map-level tileset name, and `MapSerializer` shall round-trip it (serialize → deserialize preserves the name). | unit |
| REQ-002 | When a map `$data` omits the tileset name, the loader shall default it to the canonical sheet (`lpc-mountains`) so pre-existing maps load unchanged. | unit |
| REQ-003 | When a map `$data` names a tileset absent from the catalog, the loader shall return a typed failure (never throw, never render blank). | unit |
| REQ-004 | While a paint session is active, `SelectTileset(name)` to a catalog member shall switch the active tileset and a subsequent `Save()` shall persist that name; an unknown name shall be rejected without mutating state. | unit |
| REQ-005 | The catalog of available tilesets shall be a single shared descriptor from which both the editor picker options and the runtime sheet loader derive (they cannot drift). | unit |
| REQ-006 | When the Player loads a map whose `$data` names tileset T, the runtime shall select and render sheet T rather than a hardcoded sheet. | unit (selection) + manual smoke (render) |
| REQ-007 | The Studio shall present a tileset picker; selecting a sheet shall re-render the palette and canvas with that sheet, and Save shall write its name. | manual smoke |
| REQ-008 | The FULL `bin/gate.sh` shall be green, with the new gated logic (catalog, serializer field, session tileset state) meeting the coverage and Editor/Engine mutation floors. | gate |

## Scope

- **In:** a single-source tileset **catalog** (name → sheet geometry, shared editor⇄runtime); a map-level
  `Tileset` name in `$data` (serialized, default-on-absent, typed failure on unknown); `MapPaintSession`
  `ActiveTileset` / `AvailableTilesets` / `SelectTileset` + Save/Load + NewMap default (neutral public surface);
  the Studio picker (ComboBox → swap tileset + reload canvas/palette bitmaps); the runtime honoring the named
  sheet (Player embeds all four; `Game1` loads the map's named sheet); gated tests.
- **Out (deferred):** multiple tilesets per map / per-layer; autotiles; tileset **import** (adding sheets at
  runtime); an embedded/`$data`-inlined tileset (we reference by catalog name only); per-sheet tile-size
  variation (all LPC sheets are 32px); migrating the bundled start map's mountains indices to another sheet.

## Notes

- Forge ticket: a65351b4-f606-460a-a07f-4e0392e701c6 (#20, project monorpgmaker `a9ee8162-3cfd-4c99-be5c-bbdb4be32b10`)
- Related docs: `docs/decisions.md` (D-0022 Avalonia thin host + neutral boundary; D-0023 repo-is-the-game);
  `docs/planning/pipeline/completed/WORK-studio-map-paint.spec.md` (the #16 deferral source); `docs/roadmap.md`
  (the editor track).
- Builds on: `Tileset` (Engine.World geometry), `TileMapData`/`MapSerializer` (#12 `$data`), `MapPaintSession`
  (#16/#19 editor session), `RpgGame.SetTileset` (#12 runtime), the `BehaviourRegistry.Kinds` single-source
  descriptor pattern (#18/#19).
- Promoted from intake: none (direct request via `/work`; the Studio-v2 polish arc).
- Active pipeline: docs/planning/pipeline/active/WORK-studio-tileset-picker.spec.md
