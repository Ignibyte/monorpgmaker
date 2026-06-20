---
pipeline_id: b3fbd14e-441d-4942-bf6f-07914a8ed46e
title: WORK-studio-tileset-picker
ticket: a65351b4-f606-460a-a07f-4e0392e701c6
type: work
intake: none (Studio-v2 polish arc; direct request via /work)
notes: WORK-studio-tileset-picker.notes.md
status: Phase 5 — Complete PASS
---

# WORK-studio-tileset-picker

> Pipeline spec (always-loaded contract). Detailed per-phase work lives in the
> paired `.notes.md`. Phase advance = updating the `status:` frontmatter above.

## Work Spec
- **Title:** Studio v2.2 — a per-map **tileset picker**: choose among the four committed LPC sheets, record the
  choice as a map-level **tileset name in `$data`**, and have **both** the Studio editor and the runtime Player
  render the chosen sheet.
- **Scope:**
  - *In* — a single-source tileset **catalog** (name → sheet geometry; shared by the editor picker and the
    runtime loader, so they cannot drift); a map-level `Tileset` name on `$data` (`TileMapData`), serialized by
    `MapSerializer`, **defaulted on absence** (`lpc-mountains`) and a **typed failure** on an unknown name;
    `MapPaintSession` gains `ActiveTileset` / `AvailableTilesets` / `SelectTileset(name)` and carries the name
    through `Save`/`Load`/`NewMap` (public surface stays framework-neutral — `string`/`Cell`/`int`); the Studio
    host gains a tileset **picker** (ComboBox → swap the session tileset + reload the canvas/palette bitmaps);
    the **runtime honors** the named sheet (the Player embeds all four sheets; `Game1` loads the map's named
    sheet; `RpgGame` already renders via `SetTileset`); gated tests.
  - *Out (deferred)* — multiple tilesets per map / per-layer; autotiles; tileset **import** (adding sheets at
    runtime); an embedded / `$data`-inlined tileset (reference by catalog name only); per-sheet tile-size
    variation (all LPC sheets are 32px); migrating the bundled start map's mountains indices to another sheet.
- **Systems:** map/tilemap · save/load (`$data`/`MapSerializer`) · editor (`MapPaintSession`, gated) · studio
  host (picker UI) · runtime/player (`Game1`/`RpgGame` sheet selection) · rendering.

## Acceptance Criteria (EARS)

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

## Locked-In Decisions
- **D-0022 holds (neutral thin-host boundary):** all new testable logic (the catalog, the session tileset
  state) lives in the gated `MonoRpgMaker.Editor` / `Engine.World`; the public surface is `string`/`Cell`/`int`
  — never an XNA `Point` or an Avalonia `Bitmap` (the CS0234 trap). Studio + Player stay thin
  `[ExcludeFromCodeCoverage]` hosts.
- **Reference tilesets by committed catalog NAME** (not an inlined sheet, not a roamed path) — D-0023, the repo
  is the game; the four sheets are committed assets keyed by stable name.
- **Single-source catalog** — the editor picker options and the runtime sheet loader derive from one descriptor
  (the `BehaviourRegistry.Kinds` pattern from #18/#19), so editor ⇄ runtime cannot drift.
- **Back-compat by default-on-absent** (`lpc-mountains`) — the existing `start.json` (no tileset field) and the
  #12 bundled map load unchanged; totality preserved.
- **One tileset per map (v1)** — multi-tileset / per-layer / autotiles / import are out.

### Open for design (Phase 2 decides)
- Where per-sheet **geometry** comes from: a gated catalog of dimensions vs host-derived from the loaded
  bitmap/texture. Constraint: the gated tests must not need image IO, so the catalog likely carries each sheet's
  pixel dimensions (or tile counts) — design picks the single source and how the hosts reconcile it with the
  real PNG.
- **Switch-on-non-empty-map** behavior: the recommended total rule is to re-interpret existing per-cell indices
  against the newly selected sheet (an index past the new sheet's `TileCount` renders empty via the existing
  `TryGetSourceRect` false path) — no data loss, no throw. Design confirms vs a clear/warn alternative.
- **Unknown-name policy** confirmation: REQ-003 specifies a typed failure (consistent with the existing
  trigger-validation in `MapSerializer.Deserialize`); design confirms this over a silent default.
- Whether the bundled `StartMap` / `start.json` stays mountains-pinned via the default or gains an explicit
  `tileset` field (a trivial regen either way).

## Linked Artifacts
- Design docs: `docs/decisions.md` (D-0022, D-0023), `docs/planning/pipeline/completed/WORK-studio-map-paint.spec.md`
  (the #16 deferral source), `docs/roadmap.md` (editor track).
- Intake doc: none (direct request; the Studio-v2 polish arc).
- Ticket doc: `docs/planning/tickets/open/TICKET-0020-studio-tileset-picker.md`
- Forge ticket: a65351b4-f606-460a-a07f-4e0392e701c6 (#20)

## Phase Plan
| Phase | Status |
|---|---|
| 1 — Plan | PASS |
| 2 — Design | PASS |
| 3 — Implement | PASS (build 0/0; 382 tests; GATE GREEN [fast]) |
| 3.5 — Inspect | PASS (3 critics; 1 MAJOR drift-seam fixed; gate green) |
| 4 — Validate | PASS (394 tests; GATE GREEN [full]: cov 93.3%, mut Eng 86.54/Edit 84.95) |
| 5 — Complete | PASS |
