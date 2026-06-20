---
pipeline_id: 2b4e4990-d529-49af-9901-fa32aba53d01
title: WORK-runtime-playable
ticket: 403f50c5-aa6c-408c-b6fa-e540d17f425e
type: work
intake:
notes: WORK-runtime-playable.notes.md
status: Phase 5 — Complete PASS
---

# WORK-runtime-playable

> Pipeline spec (always-loaded contract). Detail per phase lives in the paired `.notes.md`.

## Work Spec
- **Title:** Make a painted map **playable** — the Player loads a bundled `$data` map, renders it with **LPC
  tileset sprites**, and you walk a character with **collision + a following camera**. Closes the
  author→save→play loop (the runtime payoff of #12 `$data` + #16 Studio).
- **Scope:**
  *In* — move `Tileset`/`SourceRect` `Editor`→`Engine.World` (one source of truth, int-only/MRM-clean; fix the
  `MapPaintSession` ref); a bundled **start map** `$data` (LPC indices, embedded resource); a pure **`Camera`**
  (centre on player + clamp to map pixel bounds) in the gated Engine; a `WorldSim` built over a **loaded** map
  (no events); the Player **host** — load the LPC sheet as a `Texture2D`, blit sprite tiles via the `Tileset`
  source-rects (replace `ColorFor`), apply the camera, draw a player sprite.
  *Out (deferred)* — multi-map/transitions; NPCs/events on the map + in-runtime triggering; Studio→Player
  live-reload; animated walk-cycles; database-driven actor; engine-as-package split; audio; the rest of the GUI.
- **Systems:** **runtime/player** (`RpgGame`/`Game1` host — rendering/input/textures) · **world** (the moved
  `Tileset`, the `Camera`, world-from-map) · consumes `MapSerializer`/`TileMap`/`Entity.TryStep`/`WorldSim` ·
  committed LPC art + the bundled start map · tests (Engine scope).

## Acceptance Criteria (EARS)
| ID | EARS Requirement | Verification |
|---|---|---|
| REQ-001 | The runtime shall load its bundled `$data` start map and surface a typed host message (never crash) on a malformed/missing resource. | unit + manual |
| REQ-002 | The moved `Engine.World.Tileset` shall map a tile index to the correct sheet source-rect (the #16 suite passes in the new namespace). | unit test |
| REQ-003 | The player shall step on the loaded map and be **blocked** by `Blocking` tiles (`TryStep`/`IsBlocked`). | unit test |
| REQ-004 | The `Camera` shall centre on the player and clamp to the map's pixel bounds (no over-scroll at any edge; no scroll when the map ≤ the viewport). | unit test |
| REQ-005 | The start map shall be a valid `$data` map of the expected dimensions using LPC tile indices (load round-trips). | unit test |
| REQ-006 | The Player shall render the map as LPC **sprite** tiles + a player sprite + a following camera, and be walkable. | manual smoke |
| REQ-007 | The slice shall pass the FULL `bin/gate.sh` (Engine coverage + MSI ≥ 80 incl. `Camera` + the moved `Tileset`; Player exempt; build `-warnaserror`; art credited). | gate (FULL) |

## Locked-In Decisions
- **D-0023 — the repo IS the game** (lock now; → `docs/decisions.md` + `AD-claude-repo-is-the-game-001` at complete):
  one repo = one game = one binary. **No runtime project loader / "open project" / project-manager.** The engine
  loads ONLY its own **bundled** content (embedded resource — console-safe, not a roamed filesystem path); the
  Studio does file I/O as a dev convenience but the runtime never opens arbitrary projects. New game = clone the
  template repo. (Flag, don't build: engine-as-package vs fork-the-template.)
- **Move `Tileset` + `SourceRect` to `Engine.World`** — one source of truth for the editor (Studio) AND the
  runtime renderer; int-only so MRM-clean; update `MapPaintSession`'s namespace ref; the #16 `Tileset` tests move
  with it.
- **Bundled start map** = `$data` (a real `MapSerializer` map authored with LPC `lpc-mountains` indices), embedded
  as a resource the Player loads via a manifest-resource stream. The LPC sheet is likewise embedded in the Player.
- **A pure `Camera` in the gated Engine** (centre + clamp; int-pixel or `FixedPoint` math to stay MRM-clean) —
  tested. **`WorldSim` over a loaded map** (a player start, no events).
- **The Player host** (`RpgGame`/`Game1`, `[ExcludeFromCodeCoverage]`, not gated): `Texture2D.FromStream` the LPC
  sheet; `SpriteBatch.Draw` each cell's source-rect (replace `ColorFor`); apply the camera offset; a player
  sprite (an LPC character under OGA-BY/D-0021, or a simple placeholder for v1). Empty tiles draw nothing.
- **Out:** see Scope.

### OPEN — Phase 2 (design decides)
- `WorldSim` construction over a loaded map — its exact ctor/`TryCreate` (no-events path); read `WorldSim`.
- Camera representation (int `Point` offset vs `FixedPoint`/`Vector2`) + where it lives (a non-sim `World` type vs
  a sim namespace) to stay MRM-clean; how the host applies it (`SpriteBatch.Begin(transformMatrix)` vs offset rects).
- Start map: a hand-authored JSON vs a tested `StartMap.Build()` that serializes (the runtime loads the DATA either way) + its exact tiles/dimensions/player-start.
- Embedding mechanics (the `$data` JSON + the PNG as `EmbeddedResource`; the resource names; `Texture2D.FromStream`).
- The player sprite source (LPC character re-fetch vs placeholder) + how the host draws it.
- Whether the window stays fixed-size with a scrolling camera, or resizes.

## Linked Artifacts
- Design docs: `docs/technical-architecture.md`, `docs/game-overview.md` (the "players run the packaged project" model), `docs/roadmap.md`, `docs/product-phasing.md`, `docs/decisions.md` (D-0023)
- Ticket doc: docs/planning/tickets/open/TICKET-0017-runtime-playable.md
- Forge ticket: 403f50c5-aa6c-408c-b6fa-e540d17f425e (#17, project monorpgmaker `a9ee8162-3cfd-4c99-be5c-bbdb4be32b10`)
- Ground-truth anchors: `src/MonoRpgMaker.Engine/Core/RpgGame.cs`, `.../Sim/Tracer/TracerRoom.cs` (WorldSim), `.../Entities/Entity.cs` (TryStep), `.../World/{MapSerializer,TileMap,Tile}.cs`, `src/MonoRpgMaker.Editor/Tileset.cs` (to move), `src/MonoRpgMaker.Player/{Game1,MonoRpgMaker.Player.csproj}`, `assets/tilesets/lpc/lpc-mountains.png`
- Reuse PRs: PR-claude-analyzer-mutation-isolated-asserts-001, PR-claude-editor-view-math-into-tested-layer-001
- AAR: 254e90c0-ccd9-49c5-99c1-66eb08d3b604

## Phase Plan
| Phase | Status |
|---|---|
| 1 — Plan | PASS |
| 2 — Design | PASS |
| 3 — Implement | PASS |
| 3.5 — Inspect | PASS |
| 4 — Validate | PASS (gate 13/13 green) |
| 5 — Complete | PASS |
