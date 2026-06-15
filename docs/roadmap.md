# Roadmap

Phased plan. Each phase ends with something runnable and tested.

## Phase 1 — Engine foundation (in progress)

- World model: `Tile`, `TileMap`, `Direction`. ✅
- Entities: `IMovable`, `Entity`, `Actor` with grid movement + collision. ✅
- Data: generic id-keyed `Database<T>`. ✅
- `RpgGame` host + `Player` boot. ✅
- Next: tileset loading + rendering a map to the screen; camera.

## Phase 2 — Maps & rendering

- Load tilesets from content; draw layered tilemaps.
- Player sprite + animated walk cycle.
- Map transitions (edge/teleport events).

## Phase 3 — Database & content

- Authorable database tables (items, actors, skills, enemies) with on-disk format.
- Save/load of a project.

## Phase 4 — Events & scripting

- Event objects on maps with command lists (dialogue, move routes, conditionals).
- An interpreter for the command list.

## Phase 5 — The editor

- Map painter (tile palette, layers, collision).
- Database editor.
- Event editor.
- GUI front-end (MonoGame or Avalonia) over the headless editing logic.

## Phase 6 — Console ports

- Validate the engine against MonoGame console back-ends.
- Per-platform packaging + input.

## Cross-cutting

- Keep all game logic in `MonoRpgMaker.Engine` (framework-thin) to keep console
  ports tractable.
- Maintain the forge codegraph/knowledge/pipeline as the project grows.
