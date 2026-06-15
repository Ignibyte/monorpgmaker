# Feature Research — RPG Maker (MV/MZ) on MonoGame

Provenance: deep, multi-source, adversarially-verified research (2026-06-15).
24 of 25 verified claims confirmed (3-vote verification); 23 sources, mostly
primary (official RPG Maker MZ docs, the MV/MZ `corescript`, the MonoGame
tutorials, the Tiled TMX spec). Confidence is noted per item. This is the
reference behind [roadmap.md](roadmap.md) and the locked entries in
[decisions.md](decisions.md).

## The two halves

monorpgmaker is two co-equal products that share one content format: the
**maker** (editor) and the **runtime** (engine that plays an authored game).
Both are scoped below.

## The maker — three editor pillars

### 1. Database editor (high confidence)
The database is "all components that make up a game aside from the map and map
events," prepared as ~15 types. Categories to support (CRUD/forms — the single
largest editor workload): **Actors, Classes, Skills, Items, Weapons, Armor,
Enemies, Troops, States, Animations, Tilesets, Common Events, System 1, System 2,
Types, Terms.** Output is the read-only `$data*` JSON the runtime loads.

### 2. Map editor (high confidence)
Tile painting, layers, autotiles, regions, and per-tile collision. The proven,
well-documented reference model is **Tiled/TMX**:
- Tile-layer data = Global Tile IDs (GIDs), 32-bit LE integers with flip flags in
  the high bits; CSV or base64 (+ optional gzip/zlib/zstd).
- Autotiles/terrain transitions = **Wang sets** with an 8-position Wang ID
  (top, top-right, right, bottom-right, bottom, bottom-left, left, top-left),
  corner + edge matching. (Adapt to RPG Maker's A1–A5 / B–E autotile conventions.)
- Per-tile collision = a nested `<objectgroup>` of rectangle/ellipse/capsule/
  point/polygon/polyline shapes.

### 3. Event editor (high confidence)
The command palette is 3 tabs / ~16 categories / **~100 numbered commands** that
map **1:1 to runtime interpreter handlers**:
- Tab 1: Message, Game Progression, Flow Control, Party, Actor
- Tab 2: Movement, Character, Picture, Timing, Screen, Audio & Video
- Tab 3: Scene Control, System Settings, Map, Battle, Advanced

The dialogue commands live under **Message** (Show Text `101`, Show Choices `102`,
Input Number, Select Item, Show Scrolling Text); the interpreter primitives live
under **Flow Control** (Conditional Branch `111`, Loop, Break Loop, Exit Event
Processing, Common Events, Label, Jump to Label, Comment). Other load-bearing
codes: Transfer Player `201`, Set Movement Route `205`, Battle Processing `301`,
Script `355`, Plugin Command `356`.

Plus: project management, playtest launch, and resource/plugin management.

## The runtime — RPG Maker's six-module architecture (high confidence)

The MV/MZ engine is a canonical six-layer structure; this is the recommended
(not mandatory) C# port shape:

| Layer | Role | C# port |
|---|---|---|
| **core** | graphics/audio/input base classes (Graphics, Bitmap, Sprite, Tilemap, TilingSprite, Window, Stage, Input, TouchInput, WebAudio) | MonoGame wrappers over `SpriteBatch`/`Texture2D` + a scene-graph layer (below) |
| **managers** | static `*Manager` classes governing operation (Data, Scene, Battle, Save, Audio, Input) | static/service classes |
| **objects** | serializable `Game_*` runtime data (`$game*`) | the save-state model |
| **scenes** | `Scene_*` scene flow (map, menu, battle, title) | a scene stack |
| **sprites** | `Sprite_*` image display | view layer over the scene graph |
| **windows** | `Window_*` UI/input (menus, message) | UI widgets |

### Data / save split (high confidence — establish FIRST)
- **`$data*`** — loaded **read-only** from JSON in the data folder; edited only
  by the maker; **immutable during play** (Actors.json, Items.json, Map###.json…).
- **`$game*`** — live class instances **serialized to JSON on save**, except
  `$gameTemp`/`$gameMessage`/`$gameTroop`. RPG Maker compresses saves (JsonEx →
  LZString → base64); a MonoGame port replicates the read-only-DB vs serialized-
  save split (compression optional).

### Scene-graph / transform hierarchy (high confidence — MonoGame gap)
MV/MZ render through a **Pixi.Container display tree** where children inherit
parent coordinates + visibility. **MonoGame has no equivalent** — `SpriteBatch`
applies one global `Matrix` per batch, no per-node hierarchical transform/
visibility. For faithful parity (cascading move/fade/visibility on tilemap,
sprites, windows) the remake must build this layer (custom Pixi.Container-
equivalent, or adopt MonoGame.Extended / a scene-graph lib). **Open design choice.**

### Tilemap renderer (high confidence — highest-leverage MVP subsystem)
- Store a grid of **integer tile IDs** (`int[]`) referencing a separate tileset
  (memory efficiency); a one-dimensional `data` array via `setData(w,h,data)`.
- Tilesets are **sliced from one texture atlas** by integer division:
  `Columns = atlasW / tileW`, `Rows = atlasH / tileH`, `id = row*Columns + col`.
- Autotiles animate via an animation counter; maps can loop (h/v wrap); a `flags`
  array carries tile properties (higher/table/shadow).

### Input abstraction (high confidence — key for console porting)
`Input` is a static class over keyboard + gamepad via `keyMapper` /
**`gamepadMapper`** hash tables that map raw codes/buttons → logical key names,
exposing **`dir4`/`dir8`** (numpad-encoded directions). Preserve the
`gamepadMapper` indirection so Switch/Xbox/PlayStation input remaps to logical
actions without touching game logic.

### Other runtime systems
Message/dialogue window; menu system; **swappable turn-based battle** (behind an
interface — an explicit project requirement; defer but isolate); audio.

## Extensibility — non-programmer authorability + power-user escape hatch (high confidence)

Pair a **visual, no-code** event/database surface with an **optional typed plugin
API**. Three genre peers converge on this:
- **RPG Maker MZ** — plugin commands chosen from a plugin→command **dropdown**
  with creator-defined **typed Arguments** (`PluginManager.registerCommand`,
  `@command`/`@arg`).
- **RPG Developer Bakin** — no-code by default; optional **C# scripts per event**
  (multiple plugins per event).
- **Solarus** — data definition + attached **Lua script** per content type, one
  `main.lua` entry point.

Recommendation for monorpgmaker: visual events for non-programmers + a **typed
C# command/plugin registry** escape hatch for power users.

## Project / content layout (medium confidence)
Split a global config (RPG Maker `System.json`, ≈ Solarus `quest.dat`) from a
**resource manifest** (≈ Solarus `project_db.dat`), and organize content by type
(maps, tilesets, sprites, audio, …). Treat the Solarus→RPG Maker file analogy as
inspiration, not a literal spec.

## Recommended MVP slice (medium confidence — synthesis)
A vertical slice that proves the end-to-end pipeline:
1. JSON content format (`$data` read-only) + project/resource manifest.
2. MonoGame tilemap renderer (int-ID grid + atlas slicing) loading a JSON map.
3. Character sprite movement + animation with tile collision (+ the transform/
   scene-graph layer).
4. A minimal event interpreter: Show Text `101`, Show Choices `102`, Conditional
   Branch `111`, Transfer Player `201`, Set Movement Route `205`.
5. Message/dialogue window.
6. Save/load via serialized `$game` state.

Battle, full menus, and the editor GUI come after the runtime slice proves out.

## Recommended phased build order (medium confidence — synthesis)
See [roadmap.md](roadmap.md) for the project's adopted phases. The research order:
- **P0** data/content format + project loader (JSON `$data`, manifest, save serialization)
- **P1** runtime core (six-module skeleton, scene-graph layer, Graphics/Bitmap/Sprite/Window equivalents, Input mapper w/ dir4/dir8)
- **P2** tilemap render + character movement/animation/collision (MVP walkthrough)
- **P3** event interpreter (command→method dispatch) + message system + common events
- **P4** menu system + save/load UI
- **P5** swappable turn-based battle (behind an interface) + typed plugin API
- **P6** the editor (map, database, event editors; playtest launch)
- **P7+** console porting (input remap via gamepadMapper, storage/save APIs, content pipeline, certification)

Rationale: the data format + runtime core are dependencies of everything; the
editor is best built against a working runtime that consumes its own `$data`
JSON (no throwaway editor); battle is isolated behind an interface so it stays
swappable; console porting is last (its constraints are addressed incrementally
but only fully exercised once the desktop game ships).

## Caveats & open questions

- **Console-porting half is under-evidenced.** No claim survived verification with
  concrete evidence on Switch/Xbox/PlayStation storage/save APIs, content-pipeline
  differences, or certification (TRC/TCR/lotcheck) gotchas — MonoGame console
  support is gated behind NDA'd private SDKs / registered-developer programs.
  Treat console guidance (beyond the input-abstraction finding) as unverified
  engineering inference until platform-holder investigation.
- **Refuted detail:** the specific interpreter execution model (single
  `Game_Interpreter`, one command/frame, `_list`/`_index` pointer) was REFUTED
  (0-3). Build the interpreter from the **command-code → method 1:1 mapping**, not
  that execution shape.
- **Exact JSON schemas not extracted.** We confirmed the files + the `$data`/`$game`
  split, not field-level schemas of Actors.json / Map###.json / Troops.json /
  Animations.json / System.json / CommonEvents.json. Needed before designing the
  data format for compatibility or informed parity.
- **Battle internals not mapped.** `command301` exists and battle must be swappable;
  the BattleManager/Scene_Battle/Game_Action/Game_Battler flow + the minimal
  swap-interface boundary still need design.
- **Scene-graph C# choice open** (custom vs MonoGame.Extended vs a scene-graph lib),
  as does adapting TMX Wang autotiles to RPG Maker's A1–A5/B–E conventions.
- **Version target:** facts are MV (2015, PIXI v4) / MZ (2020, PIXI v5); MZ is
  current. Pick a target version. Tiled facts current to 1.12.x; Bakin to v1.4+.

## Key sources
- RPG Maker MZ official help — database (`/MZ_help-en/01_08.html`) + event commands (`/01_10.html`)
- MV/MZ corescript — https://github.com/rpgtkoolmv/corescript ; MZ dev portal — https://developer.rpgmakerweb.com/rpg-maker-mz/
- Game_Interpreter reference — https://kinoar.github.io/rmmv-doc-web/classes/game_interpreter.html
- MonoGame 2D tilemap tutorial — https://docs.monogame.net/articles/tutorials/building_2d_games/13_working_with_tilemaps/
- Tiled TMX format — https://docs.mapeditor.org/en/stable/reference/tmx-map-format/
- Plugin/extensibility — MZ plugin-command blog; RPG Developer Bakin (https://rpgbakin.com/en/about); Solarus file specs (https://docs.solarus-games.org/files-specs/)
