---
title: TICKET-0017-runtime-playable
status: closed
ticket: 403f50c5-aa6c-408c-b6fa-e540d17f425e
ticket_number: 17
type: feature
created: 2026-06-19
intake:
pipeline_spec: docs/planning/pipeline/active/WORK-runtime-playable.spec.md
---

# TICKET-0017-runtime-playable

## Summary

Close the **author → save → play** loop: the Player runtime loads a **bundled `$data` start map**, renders it
with **LPC tileset sprites** (not flat colors), and you walk a character around it with **collision** + a
**following camera**. The runtime payoff of #12 (`$data`) + #16 (Studio). **Locks D-0023** (the repo is the game;
no runtime project loader).

## EARS Requirements

| ID | EARS Requirement | Verification |
|---|---|---|
| REQ-001 | The runtime shall load its bundled `$data` start map and surface a typed host message (never crash) on a malformed/missing resource. | unit + manual |
| REQ-002 | The moved `Engine.World.Tileset` shall map a tile index to the correct sheet source-rect (the #16 suite passes in the new namespace). | unit test |
| REQ-003 | The player shall step on the loaded map and be **blocked** by `Blocking` tiles (`TryStep`/`IsBlocked`). | unit test |
| REQ-004 | The `Camera` shall centre on the player and clamp to the map's pixel bounds (no over-scroll at any edge; no scroll when the map is smaller than the viewport). | unit test |
| REQ-005 | The start map shall be a valid `$data` map of the expected dimensions using LPC tile indices (load round-trips). | unit test |
| REQ-006 | The Player shall render the map as LPC **sprite** tiles + a player sprite + a following camera, and be walkable. | manual smoke |
| REQ-007 | The slice shall pass the FULL `bin/gate.sh` (Engine coverage + MSI ≥ 80 incl. `Camera` + the moved `Tileset`; Player exempt as host; build `-warnaserror`; art credited). | gate (FULL) |

## Scope

- **In:** move `Tileset`/`SourceRect` → `Engine.World`; a bundled `$data` start map (LPC indices, embedded); a pure
  `Camera` (centre + clamp) in the gated Engine; a `WorldSim` built over a loaded map (no events); the Player host
  loading the LPC `Texture2D` + blitting sprite tiles + camera + a player sprite.
- **Out (deferred):** multi-map / transitions; NPCs/events on the map + in-runtime triggering; Studio→Player
  live-reload; animated walk-cycles; database-driven actor; the engine-as-package split; audio; the rest of the GUI.

## Notes

- Forge ticket: 403f50c5-aa6c-408c-b6fa-e540d17f425e (#17, project monorpgmaker)
- Builds on: `RpgGame`/`Game1`, `WorldSim`, `Entity.TryStep`, `MapSerializer`/`TileMap`, #16 `Tileset`, the LPC art.
- Reuse PRs: PR-claude-analyzer-mutation-isolated-asserts-001, PR-claude-editor-view-math-into-tested-layer-001, MapSerializer totality.
- Active pipeline: docs/planning/pipeline/active/WORK-runtime-playable.spec.md
