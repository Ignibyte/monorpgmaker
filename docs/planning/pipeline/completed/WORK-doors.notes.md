# WORK-doors — Notes

Per-phase working notes for the paired `.spec.md`. The spec is the contract;
this is the record of what happened.

## Phase 1 — Plan
- **Request:** Doors — switch-driven passability from `$data` in the multi-map runtime + a placeable `Lever`
  built-in. The library's door (after Chest/GiveItem) under a standing `/goal`; Shop remains after this.
- **Intake source:** none (direct `/work`).
- **Classification / tier:** work pipeline, MEDIUM slice. P2 events/runtime/map.
- **Forge recall + Explore (the ground truth):** an `Explore` agent mapped the surface —
  - `TileMapData` = `{Width, Height, Tiles[TileData], Events[EventData], Tileset}`; `TileData = {TilesetId,
    Blocking}`. So a `DoorData = {X, Y, Switch, ClosedTile:TileData, OpenTile:TileData}` mirrors the existing DTOs.
    `MapSerializer.Serialize(TileMap, IReadOnlyList<EventData>)` / `Deserialize → MapLoadResult` via the
    `MapJsonContext` STJ source-gen (nested types auto-included).
  - `WorldSim` ALREADY consumes doors: `TryCreate(map, player, events, IEnumerable<DoorRule> doors, GameState?)` +
    `SyncDoors()` (`Map.SetTile(door.Cell, State.Get(door.Switch) ? door.OpenTile : door.ClosedTile)`) at build +
    after each move/action. `DoorRule = readonly record struct (Point Cell, string Switch, Tile ClosedTile, Tile
    OpenTile)`. **The gap:** `GameSession.LoadMap:146` AND `StartMap.CreateWorld:161` pass `Array.Empty<DoorRule>()`,
    and `$data` has no doors. `TracerRoom.Build:71` shows the live pattern (a `DoorRule[]` → `TryCreate`).
  - The tracer `Sim/Tracer/LeverEvent` (a give-once switch-setter) is the template — but a NEW placeable `Lever`
    class must be NAMED to avoid the clash (like `ContainerEvent` vs the tracer `ChestEvent`).
  - The golden `RuntimePlayableTests.CommittedStartJson_MatchesBuild` compares `start.json` to
    `Serialize(StartMap.Build(), StartMap.Events())` — adding a door means `Serialize` gains the doors + the golden
    passes `StartMap.Doors()` + a regen (the #20/#22 file-based dotnet-run pattern; no regen app committed).
- **Ticket:** #26 — `c403e740-3568-46f5-836b-0580b8ba1b1a`.
- **AAR id (from `aar-open`):** `21e27a25-8b49-4ffc-b6c8-cc72b080a8da`.
- **EARS reviewed:** REQ-001 doors round-trip · REQ-002 GameSession passes doors (sync) · REQ-003 the Lever built-in
  · REQ-004 totality (malformed door / bad lever params) · REQ-005 the bundled lever→door demo · REQ-006 FULL gate
  + golden start.json.
- **Design questions for Phase 2:** (1) the `DoorData` DTO + `MapSerializer` (de)serialise + validation (off-map
  cell / empty switch → `MapLoadResult.Failure`). (2) the **defaulted** `Serialize(..., doors = null)` param (keeps
  the call-sites + `MapPaintSession.Save` green). (3) thread doors through BOTH `GameSession.LoadMap` + the
  single-map `StartMap.CreateWorld` (`DoorData → DoorRule`). (4) the new Lever class NAME (avoid the tracer clash —
  e.g. `LeverBehaviour`/`SwitchEvent`; kind `Lever`). (5) the bundled door + Lever placement in `StartMap` (a door
  cell with closed/open tiles + a `Lever` event setting that switch) + the golden regen.

## Phase 2 — Design
- **Approach / architecture:** Doors become embedded `$data`: a `DoorData` DTO (`{X, Y, Switch, ClosedTile,
  OpenTile}`, tiles as the existing `TileData`) on `TileMapData.Doors`; `MapSerializer` round-trips it (defaulted
  `doors` param on `Serialize`; `Deserialize` validates each — off-map cell / empty switch → the typed
  `MapLoadResult.Failure`) and exposes `MapLoadResult.Doors`. The sim-host threads them: a shared
  **`DoorRule.FromData(IReadOnlyList<DoorData>) → DoorRule[]`** converter (Sim → World is allowed; one place, no
  dup) feeds `GameSession.LoadMap` AND the single-map `StartMap.CreateWorld` into `WorldSim.TryCreate` (replacing
  `Array.Empty`). `WorldSim.SyncDoors` (existing, switch-driven) flips the tile. The placeable **`Lever`** built-in
  is a give-once switch-setter `SwitchEvent` (named per the `ContainerEvent`→Chest precedent, avoids the tracer
  `LeverEvent` clash): `Run` unset → `[SetSwitch(switch, true), ShowMessage(message)]`, set → `[]`. One
  `Registration` + `.expect` + handler (D-0024); composes existing outcomes (D-0017 — no new `Outcome`/applier/
  parser). The bundled start map gets a door (a wall→floor cell driven by `door_open`) + a `Lever` placement; regen
  `start.json`.
- **File manifest (12 + a test):**
  | # | File | Change |
  |---|---|---|
  | 1 | MOD `World/TileMapData.cs` | + `DoorData` DTO (`X,Y,Switch,ClosedTile:TileData,OpenTile:TileData`) + `TileMapData.Doors` |
  | 2 | MOD `World/MapSerializer.cs` | `Serialize(map, events, IReadOnlyList<DoorData>? doors = null)`; `Deserialize` parses + VALIDATES doors (off-map/empty-switch → `Failure`) |
  | 3 | MOD `World/MapLoadResult.cs` | + `IReadOnlyList<DoorData> Doors`; `Success(map, events, doors = null)` |
  | 4 | MOD `Sim/DoorRule.cs` | + `static DoorRule[] FromData(IReadOnlyList<DoorData>)` (the shared converter) |
  | 5 | MOD `Sim/GameSession.cs` | `LoadMap` → `WorldSim.TryCreate(..., DoorRule.FromData(map.Doors), state)` |
  | 6 | NEW `Sim/Events/SwitchEvent.cs` | give-once switch-setter (kind `Lever`); `Run` unset→[SetSwitch,ShowMessage], set→[] |
  | 7 | MOD `Sim/BehaviourRegistry.cs` | + `new("Lever", ["switch","message"], factory)` |
  | 8 | NEW `Sim/Events/Expectations/Lever.expect` | `=> SetSwitch(door_open, true); ShowMessage("You pull the lever.")` + `door_open=true => (none)` |
  | 9 | MOD `Editor/Expectations/HandlerRegistry.cs` | `["Lever"]` matching the `.expect` |
  | 10 | MOD `Sim/StartMap.cs` | + `Doors()` (door at (10,9): closed `Wall(1,true)` / open `Floor(66,false)`, switch `door_open`) + a `Lever` event at (8,9); thread `Doors()` through `CreateWorld`/`BuildWorld`/`BuildSession` |
  | 11 | REGEN `content/maps/start.json` | `= Serialize(Build(), Events(), Doors())` (game.json/town.json unchanged) |
  | 12 | MOD `tests/.../RuntimePlayableTests.cs` | golden `CommittedStartJson_MatchesBuild` → `Serialize(Build(), Events(), Doors())` |
  | 13 | NEW `tests/.../DoorsTests.cs` | the matrix |
- ### Regression Test Plan
  | # | Test | Proves Requirement |
  |---|---|---|
  | T1 | a `TileMap` + a door → `Serialize` → `Deserialize` → `MapLoadResult.Doors` round-trips the cell/switch/closed+open tiles | REQ-001 |
  | T2 | `GameSession.Create` over a map with a door: switch UNSET → the door cell is the CLOSED (blocking) tile; `Active.State.Set(switch,true)` + a move → `SyncDoors` → the OPEN tile | REQ-002 |
  | T3 | `SwitchEvent.Run` unset → `[SetSwitch(s,true), ShowMessage(m)]`; set → `[]`; the registry materialises a `Lever` placement | REQ-003 |
  | T4 | a `$data` door off-map (`X≥Width`) or empty switch → `MapLoadResult.Failure` (isolated); a `Lever` missing `switch`/`message` → `BehaviourResult.Failure` | REQ-004 |
  | T5 | the bundled start map (`StartMap.BuildSession`/`Create`) carries the door; acting the bundled `Lever` sets the switch → the door opens (end-to-end) | REQ-005 |
  | T6 | FULL gate green; golden `start.json` matches; gate:13 oracle → **11 rows / 7 modules** (Lever = 2 rows) | REQ-006 |
  - **No uncoverable paths** — all gated `Engine.Sim`/`World` logic (the door tiles/Lever are not host). The `#22/#23/#24/#25`
    suites stay green: `Serialize`'s defaulted `doors` keeps call-sites (`MapPaintSession.Save`, the #22 tests) compiling; the
    StartMap town/dimension tests are unaffected; the golden test is updated.
- **Risks / decisions:** (1) the `DoorData→DoorRule` converter is ONE shared `DoorRule.FromData` (no dup across
  GameSession + StartMap). (2) the Lever class is **`SwitchEvent`** (kind `Lever`) — avoids the tracer `LeverEvent`
  clash (the `ContainerEvent`→Chest precedent). (3) `SyncDoors` OVERWRITES the door cell's base tile each step, so
  `Build` can paint (10,9) as plain Floor — the DoorRule's closed tile (Wall) applies at sync while `door_open` is
  unset; no special base tile needed. (4) the golden `start.json` MUST regen (the door + lever change it). (5)
  `Serialize`/`MapLoadResult.Success` defaulted `doors` keeps every existing caller green. (6) gate:13 → 11 rows /
  7 modules. (7) totality: a malformed door is rejected at `Deserialize` (typed `Failure`), so `DoorRule.FromData`
  only ever sees valid `DoorData`.

## Phase 3 — Implement
- **Built:**
  - `World/TileMapData.cs` — `+ DoorData` DTO (`X,Y,Switch,ClosedTile,OpenTile`) + `TileMapData.Doors`.
  - `World/MapSerializer.cs` — `Serialize(..., IReadOnlyList<DoorData>? doors = null)` (defaulted); `Deserialize`
    parses + VALIDATES each door (off-map / empty-switch / missing-tile → typed `MapLoadResult.Failure`).
  - `World/MapLoadResult.cs` — `+ Doors`; `Success(map, events, doors = null)`.
  - `Sim/DoorRule.cs` — `+ static DoorRule[] FromData(IReadOnlyList<DoorData>)` (the ONE shared converter).
  - `Sim/GameSession.cs` (LoadMap) + `Sim/StartMap.cs` (CreateWorld) — pass `DoorRule.FromData(map.Doors)` /
    `FromData(doors)` to `WorldSim.TryCreate` (replacing `Array.Empty`); `StartMap.CreateWorld` gains a `doors`
    param; `BuildWorld`/`LoadWorld`/`BuildSession` thread `Doors()` / `loaded.Doors`.
  - `Sim/Events/SwitchEvent.cs` (NEW, kind `Lever`) — give-once: unset → `[SetSwitch(s,true), ShowMessage(m)]`, set
    → `[]`. `BehaviourRegistry` + `Lever.expect` + `HandlerRegistry` entry (D-0024; composes existing outcomes).
  - `Sim/StartMap.cs` — `+ Doors()` (a door at (10,9): closed `Wall` / open `Floor`, switch `door_open`) + a
    `Lever` event at (8,9). REGEN `content/maps/start.json` (1 door, 3 events).
  - Tests: `DoorsTests.cs` (NEW, 11); the golden `CommittedStartJson_MatchesBuild` += `Doors()`; the #22
    `Events_IsWelcomeSign` 2→3 + a lever assert.
- **Deviations from design (+ reason):** none material. The Lever class is `SwitchEvent` (kind `Lever`) as designed
  (the `ContainerEvent`→Chest precedent); the bundled door (10,9) / lever (8,9) clear the sign/warp/start.
- **Build / verify:** whole solution `-warnaserror` 0/0; **GATE GREEN [fast]**; gate:13 oracle **11 rows / 7
  modules** (Lever +2); 511 tests; the golden `start.json` matches the regen. The `Serialize` defaulted `doors`
  kept the #22–#25 call-sites green.
- **NOT yet (Phase 4):** the FULL gate (coverage + mutation — the door serialize/validate + `DoorRule.FromData` +
  `SwitchEvent` + the registration mutants must die).

## Inspect (Phase 3.5)
- **Lenses run:** 1 adversarial correctness + data-integrity critic — **survived** + verified CONCRETELY (ran
  `dotnet test --filter Doors`, **2 throwaway repros** [door blocking at boot / walkable after the switch is set],
  read the bounds char-by-char, confirmed NO town.json golden + the old-map back-compat, oracle 11/7). **No
  BLOCKER/MAJOR/MINOR code defect.**
- **Findings:**
  | # | Severity | Finding | Verdict | Fix |
  |---|---|---|---|---|
  | 1 | watch-item | the door-bounds test hit both axes together (`(9,9)` on 5×5), so the per-axis high-bound (`>`→`>=`) + low-bound (negative) mutants could survive; no null-tile test (`:159` guard), no absent-`doors`-key test (the `?? []` mutant), no Lever-missing-`message` test. | **REAL mutation-coverage gaps (NOT code defects)** — the critic excludes "missing tests". | **FIXED proactively** — `Door_OffMapAxis_Fails` `[Theory]` ×4 `(Width,y)/(x,Height)/(-1,y)/(x,-1)` + `Door_NullTile_Fails` + `Deserialize_NoDoorsKey_EmptyDoors` + `Registry_Lever_MissingMessage_Fails` (closes the kill-list; pre-empts a Phase-4 loop). |
  | 2 | observation | the bundled door (10,9) has open floor on all sides → walk-around-able, gates nothing. | **REAL but BY-DESIGN** — the slice proves the door MECHANISM (switch-driven tile), not a chokepoint puzzle. | none — a real barrier (a wall of doors) is a Studio-door-authoring concern (deferred). |
- **Cleared (verified, not just unflagged):** door validation totality (bounds EXACT — both axes, both low `<0` +
  high `>=Width/Height`, not `>`; empty switch; null tile → typed `Failure`, never a throw); the round-trip
  (cell/switch/closed+open+blocking); the empty-doors JSON (start.json golden matches; **no** town.json golden so
  the un-regenerated town is harmless; old-map back-compat via `data.Doors ?? []`); `DoorRule.FromData` (closed/open
  NOT swapped, null-guarded); the door-sync end-to-end (BOTH call-sites incl. `LoadWorld→loaded.Doors`; the
  boot-closed/set-open repro); `SwitchEvent` (outcome order · value `true` · give-once) + registry totality + the
  **`.expect`↔handler EXACT** (oracle 11/7) + no `SwitchEvent`/tracer-`LeverEvent` namespace clash; MRM/§14 +
  determinism (for-loops, int/bool only, XML-doc public).
- **Post-fix:** the 4 added tests pass (18/18 filter); no `.cs` source changed (test-only) — build + GATE GREEN
  [fast] hold.
- **Capture:** none — no code defect (a clean contract-first + serializer slice; the items were proactive tests).
- **Phase-4 kill-list (now covered by 18 tests):** the door bounds (4 comparisons, per-axis, via the `[Theory]`);
  the empty-switch + null-tile guards; the `?? []` coalesce; `DoorRule.FromData` (closed/open order + the
  `Blocking`/`TilesetId` reads); the `SyncDoors` ternary; `SwitchEvent` (`GetSwitch` negation · `SetSwitch` value ·
  order); the `Lever` registration (both `&&` conjuncts).

## Phase 4 — Validate
- **Tests (+18; 500 → 518):** `DoorsTests.cs` (18) — REQ-001 round-trip + no-doors-key back-compat · REQ-002
  GameSession door closed-unset / open-set · REQ-003 `SwitchEvent` Run set+message / give-once / registry
  materialises · REQ-004 totality (off-map + a per-axis `[Theory]`×4, no-switch, null-tile, Lever missing-switch +
  missing-message) · REQ-005 bundled has-closed-door + door-opens. Plus the golden + `Events_IsWelcomeSign` (2→3).
- **`dotnet test MonoRpgMaker.slnx`: 518 passed, 0 failed.**
- **`bin/gate.sh`: GATE GREEN [full] — 13/13** (first try). Coverage **92.9%** (floor 80). Mutation: Engine
  **83.92%** · Abstractions 82.22% · Analyzers 91.18% · Editor **84.03%** (floor 80 — the door serialize/validate +
  `FromData` + `SwitchEvent` + the registration added mutants; the kill-list tests killed them, MSI clears). Oracle
  **11 rows / 7 modules** (Lever +2). Receipt written.
- **Pre-existing exclusions:** none — all gated `Engine.World`/`Engine.Sim` logic (the door $data + sync + Lever are
  fully unit-tested). Smoke (not blocking): the bundled start map's `Lever` (8,9) opens the door (10,9); the Studio
  inspector's kind dropdown offers `Lever` (reads `BehaviourRegistry.Kinds`); the door TILE is authored in `$data`
  (Studio door-painting deferred).

## Phase 5 — Complete
- **Docs updated:** `docs/roadmap.md` (the built-in-library paragraph += #26 doors — switch-driven door tiles from
  `$data` + a placeable `Lever`; only Shop remains) + `docs/product-phasing.md` (the Event-behavior row += Lever/
  doors). No `decisions.md` entry — doors-from-`$data` apply D-0023 (embedded `$data`) + D-0026 (GameSession passes
  to WorldSim) + D-0024/D-0017 (the Lever); they all stand.
- **Forge capture:** `aar-submit` 21e27a25 (completed, effectiveness 4). No failure-record / prevention-rule / AD —
  inspect found NO code defect (a clean slice on the recipe + the existing `WorldSim.SyncDoors` mechanism; only 4
  proactive mutation tests added).
- **Ticket closed:** #26 (`c403e740-…`) → done; `TICKET-0026-doors.md` moved open/ → closed/.
- **Archived:** spec + notes moved active/ → completed/; spec status `Phase 5 — Complete PASS`.
- **The built-in library now:** `ShowText` · `Warp` · `GiveItem` · `Chest` · `Lever`+doors — five no-code
  behaviours via the contract-first recipe (D-0024). **Only SHOP remains** of the originally-listed built-ins — and
  it is a genuinely LARGER, separate feature: a buy/sell menu UI (inventory display + a currency counter + a shop
  screen in the Player + Studio authoring), not a tile-event behaviour. To be **SURFACED to the user for scoping**
  (a major UI slice), not auto-continued under the library goal.
