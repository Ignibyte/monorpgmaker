# Roadmap

The adopted phased plan, grounded in [feature-research.md](feature-research.md)
(verified RPG Maker MV/MZ + MonoGame research). Each phase ends with something
runnable and tested. Phase order front-loads the load-bearing, hardest-to-retrofit
foundations (data format, runtime core, the scene-graph layer MonoGame lacks) and
defers large-but-additive systems (battle, menus, the editor GUI).

## Where we are (done)

- Scaffold: `Engine` (`World` tiles/maps/direction, `Entities` movable actors,
  `Data` id-keyed `Database<T>`, `Core` `RpgGame` host), `Player`, `Editor`
  (headless logic), xUnit tests. Builds clean; `bin/gate.sh` green; forge codegraph
  + pipeline live. This is the seed `World`/`Entities`/`Data` layer the phases below
  grow into the six-module runtime.

## P0 — Content & save format (foundation; build first)

The data format is a dependency of everything. Establish the **read-only `$data`**
vs **serialized `$game` save** split before building systems on top.

- A JSON content format + loader: maps (`Map###.json`), database tables
  (Actors/Items/Skills/Enemies/Troops/…), `System.json`, `CommonEvents.json`.
- A project/resource manifest (global config split from a resource registry).
- Save serialization of `$game` state (compression optional).
- *Decide first:* target RPG Maker compatibility level (informed parity vs exact
  schema) — see decisions.md / open question on field-level schemas.

## P1 — Runtime core

- Six-module skeleton (core / managers / objects / scenes / sprites / windows).
- **Scene-graph / transform-hierarchy layer** (MonoGame has none — cascading
  coordinates + visibility for tilemap/sprites/windows). Choose: custom
  Pixi.Container-equivalent vs MonoGame.Extended vs a scene-graph lib.
- Graphics/Bitmap/Sprite/Window equivalents over `SpriteBatch`/`Texture2D`.
- `Input` mapper: keyboard + `gamepadMapper` → logical actions, `dir4`/`dir8`.

## P2 — Tilemap render + character movement (MVP map walkthrough)

- Tilemap renderer: int-ID grid (`int[]`) + tileset atlas slicing
  (`id = row*Columns + col`); layers; autotile animation; optional looping.
- Character sprite movement + walk animation + tile collision (extends the
  current `Entity`/`TileMap`).
- Camera.

## P3 — Event interpreter + message system

- Event objects on maps (pages, conditions, triggers).
- Interpreter as a **command-code → handler** dispatch (1:1 with the editor's
  palette). MVP commands: Show Text `101`, Show Choices `102`, Conditional Branch
  `111`, Transfer Player `201`, Set Movement Route `205`; then Common Events.
- Message/dialogue window.

> **MVP slice = P0–P3**: JSON map → render → move/collide → minimal interpreter →
> message window → save/load. Prove the end-to-end pipeline here.

## P4 — Menus + save/load UI

- Menu system (`Window_*` widgets, scene stack).
- Save/load UI over the P0 serialization.

## P5 — Battle + extensibility

- Default **turn-based battle behind a swappable interface** (BattleManager /
  Scene_Battle / Game_Action / Game_Battler; define the minimal swap boundary).
- **Typed C# plugin/command registry** — the power-user escape hatch beside the
  visual event system (the model MZ/Bakin/Solarus converge on).

## P6 — The editor (the maker)

Built against the working runtime, consuming the same `$data` JSON (no throwaway
editor). The three pillars:
- **Map editor** — tile palette, layers, autotiles (Wang-style), regions, per-tile
  collision (TMX is the reference model).
- **Database editor** — the ~16 category tables (Actors, Classes, Skills, Items,
  Weapons, Armor, Enemies, Troops, States, Animations, Tilesets, Common Events,
  System 1/2, Types, Terms).
- **Event editor** — the 3-tab / ~100-command palette + event pages.
- Project management + playtest launch.
- GUI front-end (Avalonia or MonoGame) over the headless `Editor` logic.

## P7+ — Console porting

- Validate the Engine against MonoGame console back-ends (registered-developer
  programs / NDA'd SDKs).
- Input remap via the `gamepadMapper`; per-platform storage/save APIs; content
  pipeline; certification (TRC/TCR/lotcheck). **Under-researched — needs
  platform-holder investigation** (see feature-research.md caveats).

## Cross-cutting

- Keep all game logic in `MonoRpgMaker.Engine` (framework-thin) so console ports
  stay tractable; keep simulation state separate from rendering; keep simulation
  deterministic (inject RNG).
- Every non-trivial slice flows through the pipeline (`/work` → … → `/commit`);
  capture lessons + decisions to the forge.
