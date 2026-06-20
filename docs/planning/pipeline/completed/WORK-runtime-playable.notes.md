# WORK-runtime-playable — Notes

## Phase 1 — Plan
- **Request:** make a painted map playable in the runtime (Player loads bundled `$data` → LPC sprite rendering →
  walkable + camera). **AUTONOMOUS** — proceed through the whole pipeline + deliver (commit→PR→merge) without
  pausing ("/work next thing till completion").
- **Classification:** larger feature, ONE shippable vertical (the runtime's first real "play a map" slice). The
  deferred items (transitions, events-in-runtime, walk-cycles, the engine-as-package split) are separate pipelines.
- **Ground (verified from code):** `RpgGame(WorldSim)` host (`[ExcludeFromCodeCoverage]`) — flat-color `ColorFor`
  rects + 1×1 pixel, input→`_sim.MovePlayer`, window=map*32, no camera. `Game1` = `base(TracerRoom.Build())`.
  `Entity.TryStep(Direction,TileMap)` does collision (`map.IsBlocked`). `MapSerializer.Deserialize`→total
  `MapLoadResult`. #16 `Tileset` is in `Editor`. Art `lpc-mountains.png` 384×288/32px/108 tiles (OGA-BY/D-0021).
- **Ticket:** 403f50c5-aa6c-408c-b6fa-e540d17f425e (#17). **AAR id:** 254e90c0-ccd9-49c5-99c1-66eb08d3b604
- **D-0023 locked** (the repo is the game; no runtime project loader) — recorded at complete.
- **EARS:** REQ-001..007.
- **Proceeding autonomously to design (no pause).**

## Phase 2 — Design

### Architecture / approach
**Gated Engine logic (deterministic, tested, MSI≥80):**
- **Move `Tileset` + `SourceRect` `Editor` → `Engine.World`** (namespace `MonoRpgMaker.Engine.World`). Int-only ⇒
  MRM-clean. One source of truth for the Studio editor AND the runtime renderer. Fix consumers' usings:
  `Editor/MapPaintSession.cs` (already `using …Engine.World`), `Studio/TilePalette.cs` + `MapCanvas.cs` (add the
  using for `SourceRect`), and the #16 `TilesetTests` (now resolve `Tileset` from `Engine.World`). The tests stay
  in `MonoRpgMaker.Engine.Tests` (one project) — only the mutation *attribution* shifts Editor→Engine.
- **`Engine.World.Camera`** (NEW, pure int math): `static Point ViewOffset(Point playerPixel, Point viewportSize,
  Point mapPixelSize)` — `desired = playerPixel - viewport/2`; `offset = Clamp(desired, 0, max(0, mapPixel -
  viewport))` per axis (`Math.Clamp` on ints, MRM-permitted). Map ≤ viewport ⇒ `max(0, negative)=0` ⇒ offset 0
  (no scroll). Pure/deterministic/fully tested.
- **`Engine.World.StartMap`** (NEW): `Build() → TileMap` (the canonical start room — ~28×18 so it EXCEEDS the
  viewport and the camera visibly scrolls; a walkable `Floor` index + a `Blocking` wall border, both LPC
  `lpc-mountains` indices the implementer picks by reading the sheet); `PlayerStart` (a known floor `Point`);
  `BuildWorld() → WorldSim` (`Build()` + an `Actor` at `PlayerStart` + `WorldSim.TryCreate(map, player,
  [], [])` — empty events/doors ⇒ always Ok); `LoadWorld(string json) → WorldSimResult` (`MapSerializer.Deserialize`
  → on Ok build the world at `PlayerStart`, else surface the typed failure — never throws). This is the runtime's
  data-load entry.
- `content/maps/start.json` (committed DATA) = `MapSerializer.Serialize(StartMap.Build())` — generated once by the
  implementer and committed.

**Player host (`RpgGame` in `Engine.Core` + `Game1` in Player — both `[ExcludeFromCodeCoverage]`, Stryker SKIPS
excluded code so they're mutation-exempt; the established RpgGame pattern):**
- `Game1()` : `base(LoadStartWorld())` where `LoadStartWorld()` reads the embedded `start.json` manifest-resource
  stream → `StartMap.LoadWorld(json)` → `Ok ? Sim : StartMap.BuildWorld()` (fallback ⇒ the window always opens,
  REQ-001). Resource-stream reading is host code (no GraphicsDevice needed at ctor time).
- `RpgGame`: a FIXED viewport (e.g. 20×15 tiles = 640×480 back-buffer, no longer map-sized). A `protected` seam so
  the host supplies the tileset texture: `Game1` overrides `LoadContent` → `base.LoadContent()` then
  `Texture2D.FromStream(GraphicsDevice, <embedded lpc-mountains.png>)` + `SetTileset(tex, Tileset.FromSheet(384,
  288,32))`. `DrawMap`: when a tileset is set, `SpriteBatch.Draw(tex, destRect, new Rectangle(sr.X,sr.Y,sr.W,sr.H),
  Color.White)` via `Tileset.TryGetSourceRect(tile.TilesetId, out sr)`, skipping empty (`TilesetId<0`); flat-color
  `ColorFor` stays as a fallback. Camera: `SpriteBatch.Begin(transformMatrix: Matrix.CreateTranslation(-offset.X,
  -offset.Y, 0))` from `Camera.ViewOffset(...)` so map+player shift together. Player sprite: a clear PLACEHOLDER
  (the existing inset quad, distinct color) for v1 — the LPC character sprite is DEFERRED (avoids blocking the
  autonomous run on asset hunting; the payoff — walk your LPC-tiled map — holds).
- The Player embeds `content/maps/start.json` + `assets/tilesets/lpc/lpc-mountains.png` as `EmbeddedResource`.

### File manifest
| File | Change |
|---|---|
| `src/MonoRpgMaker.Engine/World/Tileset.cs` | NEW — moved from Editor (`namespace …Engine.World`); `Tileset` + `SourceRect` unchanged |
| `src/MonoRpgMaker.Editor/Tileset.cs` | DELETE (moved) |
| `src/MonoRpgMaker.Engine/World/Camera.cs` | NEW — `ViewOffset` (centre + clamp, int) |
| `src/MonoRpgMaker.Engine/World/StartMap.cs` | NEW — `Build`/`PlayerStart`/tile consts/`BuildWorld`/`LoadWorld` |
| `src/MonoRpgMaker.Editor/MapPaintSession.cs` | MODIFY — `Tileset`/`SourceRect` resolve from `Engine.World` (uses existing using; verify `Cell` stays local) |
| `src/MonoRpgMaker.Studio/TilePalette.cs` | MODIFY — add `using …Engine.World;` for `SourceRect` |
| `src/MonoRpgMaker.Studio/MapCanvas.cs` | VERIFY — already `using …Engine.World;` |
| `content/maps/start.json` | NEW — committed `$data` (serialized `StartMap.Build()`) |
| `src/MonoRpgMaker.Player/MonoRpgMaker.Player.csproj` | MODIFY — `EmbeddedResource` start.json + lpc-mountains.png |
| `src/MonoRpgMaker.Player/Game1.cs` | MODIFY — load embedded world + override `LoadContent` for the texture |
| `src/MonoRpgMaker.Engine/Core/RpgGame.cs` | MODIFY — fixed viewport; `SetTileset` seam; sprite-blit `DrawMap`; camera matrix |
| `tests/MonoRpgMaker.Engine.Tests/StudioEditorTests.cs` | MODIFY — `Tileset` tests resolve `Engine.World` |
| `tests/MonoRpgMaker.Engine.Tests/RuntimePlayableTests.cs` | NEW — Camera, StartMap, LoadWorld, TryStep-on-loaded-map |

### Regression Test Plan
| T | REQ | Test (isolated exact-count asserts) |
|---|---|---|
| T1 | 002 | `Tileset` — the moved #16 suite (FromSheet 12/108; rect 0/12-wrap/107; oob; TryGetTileIndex) resolves `Engine.World.Tileset`. |
| T2 | 004 | `Camera.ViewOffset`: player-centred in a big map → centred offset; clamp **left** (→0), **right** (→mapW−viewW), **top** (→0), **bottom** (→mapH−viewH); map ≤ viewport → `(0,0)` (no scroll). |
| T3 | 005 | `StartMap.Build()` → expected `Width`/`Height`; `PlayerStart` is in-bounds + **not** `Blocking`; the border cells are `Blocking`; an interior floor cell is the `Floor` index. |
| T4 | 005 | Round-trip: `MapSerializer.Serialize(StartMap.Build())` → `StartMap.LoadWorld(json)` → `Ok`; `Sim.Map` equals `Build()` cell-by-cell. |
| T5 | 001 | `StartMap.LoadWorld("{bad")`/`("null")`/`("")` → `!Ok`, `Error` set, **no throw** (`Record.Exception` null). |
| T6 | 003 | On `StartMap.Build()`'s map: a player at `PlayerStart` `TryStep` into the open floor → true (moved); `TryStep` into a border wall → false (blocked, cell unchanged). |
| — | 006 | The Player renders LPC sprite tiles + a player marker + a following camera, walkable — **MANUAL smoke** (`dotnet run --project src/MonoRpgMaker.Player`); host `[ExcludeFromCodeCoverage]`. |
| — | 007 | FULL `bin/gate.sh`: **Engine** MSI≥80 with Camera+Tileset+StartMap added; **Editor** MSI≥80 after losing Tileset; build `-warnaserror`; art credited. |

### Risks / decisions
- **Mutation-surface shift (Tileset Editor→Engine):** the Tileset tests move with the code, so Engine GAINS a
  well-tested unit (stays ≥80) and Editor LOSES it — Editor's remaining surface (MapPaintSession + scaffolder +
  oracle + MapEditor) must still hold ≥80 (it was ~83.97% pre-#16 without Tileset). The gate confirms; if Editor
  dips, add a MapPaintSession/scaffolder kill-test.
- **Stryker + `[ExcludeFromCodeCoverage]`:** the host rendering lives in the already-excluded `RpgGame`/`Game1`;
  Stryker skips `[ExcludeFromCodeCoverage]` (RpgGame already relies on this — Engine MSI passes with its untested
  draw code), so the new rendering is mutation-exempt. Verify it still holds.
- **MRM-cleanliness:** `Camera`/`StartMap`/`Tileset` are int/Tile/TileMap only (no float/clock/RNG/Dict-foreach) —
  must build `-warnaserror` clean under the analyzers; `Math.Clamp(int…)` is permitted.
- **`Texture2D.FromStream` needs a GraphicsDevice** → host-only (`LoadContent`), excluded — fine.
- **Camera only matters if the map exceeds the viewport** → start map ~28×18 vs a fixed ~20×15 viewport so
  scrolling is visible (REQ-006).
- **Player sprite = placeholder for v1** (a clear quad); the real LPC character sprite is a deferred follow-up — a
  deliberate call to keep the autonomous run unblocked.
- **DEFERRED (unchanged):** multi-map/transitions; events-on-map in-runtime; Studio→Player live-reload; animated
  walk-cycles; database-driven actor; engine-as-package; audio; the rest of the GUI.

## Phase 3 — Implement
- **Built:** `Engine.World/Tileset.cs` (moved from Editor); `Engine.World/Camera.cs` (ViewOffset, int);
  `Engine.Sim/StartMap.cs` (Build/PlayerStart/BuildWorld/LoadWorld); `content/maps/start.json` (28×18, committed);
  Player csproj (EmbeddedResource start.json + lpc-mountains.png); `Game1.cs` (load embedded world + texture,
  fallback-safe); `Engine.Core/RpgGame.cs` (fixed 20×15 viewport, `SetTileset`, sprite `DrawMap`, camera
  translation, PointClamp, screen-fixed banner). Consumers fixed: `Studio/TilePalette.cs` (+`Engine.World` using).
- **Build:** full solution `-warnaserror` → **0/0**; embedded resources confirmed in the Player dll
  (`start.json`, `lpc-mountains.png`).
- **Deviations from design (with reason):**
  1. **`StartMap` lives in `Engine.Sim`, not `Engine.World`** — it builds a `WorldSim`, so World→Sim would invert
     the layering; it sits alongside the `TracerRoom` precedent (Engine.Sim). Only `Camera` (pure `Point` math,
     no Sim dep) went to `Engine.World`.
  2. **`Tileset.TryGetTileIndex` changed `double`→`int`** — MRM1001 fired on the move (floating-point is banned in
     the gated `Engine.World`; it was legal in the Editor). Palette clicks don't need sub-pixel precision, so the
     signature is `int` and the Studio's `TilePalette` casts `(int)pixel.X`. The #16 tests pass int literals → still
     green. `CellAt` (double, in `MapPaintSession`, NOT moved) keeps its double signature (Editor isn't gated).
  3. **Tile indices `Floor=66`, `Wall=1`** (LPC mountains best-guess) — cosmetic; trivially tweakable post-smoke.
  4. **`StartMap.LoadWorld` is the runtime's data-load path** (total); `Game1` reads the embedded `start.json` →
     `LoadWorld` → `Ok ? Sim : BuildWorld()` fallback (REQ-001 never crash). `content/maps/start.json` was
     generated once by a throwaway test (`MapSerializer.Serialize(StartMap.Build())`), then deleted.
- **NOT yet run:** the FULL gate + the manual smoke — inspect next.

## Inspect (Phase 3.5)
Two parallel critics (both built+ran real code). Lenses: correctness/totality/build-run; design/purity/mutation.

**Critic 1 — correctness/build-run: NO defects (all 7 items PASS, real output).**
- Camera.ViewOffset: centre (680,760); clamp left/right/top/bottom exact; small map → (0,0); exact-viewport → (0,0);
  **swept −50..2050 both axes → no negative offset, no over-scroll.**
- StartMap.Build: 28×18; all 4 corners Blocking (TilesetId 1); PlayerStart (2,2) NOT blocking (TilesetId 66 floor).
- BuildWorld: Map 28 wide, Player.Cell (2,2), no throw. LoadWorld round-trip Ok + cell-equal + Player (2,2).
- **Totality:** LoadWorld("{bad"/"null"/""/"[]") → no throw, !Ok, Error set (parse path total).
- **Committed start.json freshness: the committed `$data` equals `StartMap.Build()` cell-by-cell (NOT stale).**
- Movement: Right→(3,2) true; Left→(1,2); Left again into border wall (0,2) → **false, Cell stays (1,2)** (collision).
- Regression: `dotnet build src/MonoRpgMaker.Studio -warnaserror` → 0/0 (the Tileset move didn't break the Studio).
- GUI play-smoke stays MANUAL (MonoGame needs a GL context — not runnable headless).

**Critic 2 — design/MSI: sound; host exclusion + layering confirmed; risks flagged.**
- (1a) the #16 `TilesetTests` resolve `Engine.World.Tileset` (using present) → now count toward **Engine** MSI. ✓
- (1b) **Editor MSI after losing Tileset:** projected ~84% (≥80 likely; was ~83.97% pre-#16) — **only gate:12 confirms; read that line.**
- (1c) **Engine MSI is RED right now** — Camera/StartMap ship untested; **Phase 4 MUST add the kill-list** (below).
- (2) host exclusion ✓ — RpgGame + Game1 both `[ExcludeFromCodeCoverage]` (Stryker skips); Player not in the mutation list.
- (3) layering ✓ — StartMap in Engine.Sim (builds a WorldSim), Camera in Engine.World pure (no Sim dep). MRM-clean
  (analyzer allows `Math.Max/Clamp(int)` + XNA `Point`). **GAP [low]:** no NetArchTest rule for the intra-engine ring
  (`World/Entities/Data ⊁ Engine.Sim`) — not violated here; add the rule in Phase 4 as cheap hardening.
- (4) §14 ✓ — nullable-clean, XML-doc'd, LoadWorld typed-no-throw, BuildWorld's `?? throw` a defensible backstop
  (empty events ⇒ always Ok; matches TracerRoom — near-unkillable, ~1 mutant drag, accepted).

**Findings + verdicts:**
| # | Sev | Finding | Verdict |
|---|---|---|---|
| 1 | — | correctness (camera/startmap/loadworld/movement/freshness/studio) | **PASS — no fix** (critic 1 verified) |
| 2 | (expected) | Engine MSI red until Camera/StartMap tests land | **Phase 4** — the 13-mutant kill-list is the plan |
| 3 | risk | Editor MSI must hold ≥80 after losing Tileset | **watch** — gate:12 confirms; read the Editor line |
| 4 | low | no `World⊁Sim` arch-test rule (guardrail gap) | **CONFIRMED** — add the NetArchTest rule in Phase 4 |
| 5 | low | `TryGetTileIndex` double→int is an API change, not a pure move | **noted** (Phase 3 deviations); behaviourally equiv, tests pass |
| 6 | nit | start.json dual-source-of-truth (freshness) | **add a freshness test** in Phase 4 (json == Serialize(Build())) |

**No source fixes (no real bugs).** No failure-record (nothing failed). **Phase-4 plan (the kill-list):** Camera (small-map
floor; asymmetric Max; left/right/top/bottom clamp; the /2 centre; the − subtraction) · StartMap.Build (4-edge border +
interior + full Tile equality; 28/18 consts) · BuildWorld (player-at-PlayerStart) · LoadWorld (good + bad-json both
sides, no-throw) · the moved Tileset (#16 suite, now Engine-scored) · the start.json freshness assert · the `World⊁Sim`
NetArchTest rule.

## Phase 4 — Validate
- **Tests added:** `tests/MonoRpgMaker.Engine.Tests/RuntimePlayableTests.cs` (the inspect 13-mutant kill-list):
  `CameraTests` (small-map no-scroll, asymmetric axes, left/top clamp, right clamp, bottom clamp, centre) +
  `StartMapTests` (Build dims + border-blocking/interior-floor + PlayerStart-is-floor, BuildWorld player-at-start,
  LoadWorld round-trip, LoadWorld malformed `[Theory]`, walk + blocked-by-wall, **committed start.json freshness**).
  Plus the `World/Entities/Data ⊁ Engine.Sim` NetArchTest rule (`ArchitectureTests.cs`) — nothing violates it.
  The moved #16 `TilesetTests` now score under Engine. **+18 tests (317 → 335.)**
- **One test bug fixed:** `ClampBottom` expected 1360 but the correct clamp is `mapH−viewH = 2000−480 = 1520`
  (my assertion error — the Camera is correct; a good catch by the failing test, not a code bug).
- **`dotnet test MonoRpgMaker.slnx`:** `Passed! Failed: 0, Passed: 335` (no regressions).
- **`bin/gate.sh` (FULL):** **GATE GREEN [full]** — 13/13.
  - coverage **93.0%** ≥ 80.
  - mutation: **Engine 86.33%** (UP from 83.90 — the Tileset move + Camera/StartMap tests *raised* Engine's MSI),
    **Editor 84.30%** (held ≥ 80 after LOSING Tileset — the inspect risk resolved, ~84% projected → 84.30 actual),
    Abstractions 82.22, Analyzers 91.18. Player host exempt ([ExcludeFromCodeCoverage], Stryker skips).
  - gate:4 vuln + gate:5 license clean (no new deps); gate:13 `.expect` oracle OK.
  - Receipt `.git/monorpgmaker-gate-receipt` written.
- **Pre-existing failures:** none.

## Phase 5 — Complete
- Docs / forge / ticket / archive:
