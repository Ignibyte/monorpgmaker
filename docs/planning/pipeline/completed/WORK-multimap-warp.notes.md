# WORK-multimap-warp — Notes

Per-phase working notes for the paired `.spec.md`. The spec is the contract;
this is the record of what happened.

## Phase 1 — Plan
- **Request:** Multi-map — map-to-map transitions; the last Studio-v2 slice + the start of the trigger program's
  Slice 2 (the built-in `Warp`). The user chose to proceed to this slice after #20/#21.
- **Intake source:** none — a direct request via `/work`.
- **Classification / tier:** a **larger feature**, scoped to **one vertical slice (Slice A)** via a recommended +
  confirmed **split**. Slice A = the runtime multi-map model + the switch + `Warp` end-to-end. Slice B (deferred)
  = the Studio authoring UI. Touches `Abstractions` (the outcome) + the `Engine.Sim` core + the `Player` host.
- **Forge recall:** `bulletin-list` → none. `knowledge-search` surfaced the locked decisions + the undo failure I
  just recorded (opaque UUIDs; pulled in detail at design via `knowledge-context`). **`docs-search` deliberately
  skipped** — multi-map is the worst oathstar room/region/`editor.rs` bleed zone (`source_path` collides across
  tenants); grounded on the C# codegraph + `docs/decisions.md` instead.
- **Grounding (current single-map runtime, read):**
  - `WorldSim` (Engine.Sim) holds ONE `Map` (get-only) + `Player` + `State` (a **fresh `GameState` in its ctor**)
    + the `HookSchedule`; built via the private ctor through `WorldSim.TryCreate(map, player, events, doors)`.
    `MovePlayer`/`PressAction` → `FireHooks` → `OutcomeApplier.Apply(event.Run(_eventContext))`.
  - `Outcome` (Abstractions) is a **closed** record hierarchy (`SetSwitch`/`AddCounter`/`ShowMessage`; private-
    protected ctor; "extended additively here"). `OutcomeApplier` switches on the case + **throws on an unknown
    kind**. So `Warp` = a new additive case + a new applier arm that **signals** (doesn't mutate).
  - `ShowTextEvent : IMapEvent` is the recipe to mirror: `Cell`/`Trigger`/`Run(ctx) → [new ShowMessage(text)]`;
    materialised by `BehaviourRegistry` from a `ShowText` placement; an `.expect` contract guards it.
  - **The crux:** `WorldSim` is immutable per-map (get-only `Map`, fresh `GameState`), so a warp can't mutate it
    in place — it needs a layer ABOVE that owns the registry + builds a NEW `WorldSim` for the target, carrying
    the `GameState`. The applier surfaces a *pending warp* (like `CurrentMessage`); the orchestrator applies it.
- **Scope fork (resolved → ALL-IN-ONE):** the split (Slice A runtime / Slice B Studio UI) was offered + the user
  **declined it at the plan gate** — ship BOTH the runtime multi-map + `Warp` AND the Studio multi-map authoring
  UI in one (large) pipeline. Build + prove the runtime first; the Studio UI (the gated map-set/project layer +
  the map-list + the Warp inspector) layers on top. The spec's EARS gained REQ-007/008/009 (the Studio half).
- **Ticket:** #22 — `1ba1efe9-d4d7-418e-95e1-520805d30a56` (project monorpgmaker `a9ee8162-3cfd-4c99-be5c-bbdb4be32b10`).
- **AAR id (from `aar-open`):** 1f2ab922-44f0-4f37-9a3b-8306e2210d8d (user chose ALL-IN-ONE at the plan gate, 2026-06-20).
- **EARS requirements reviewed:** REQ-001 (registry load-by-id, total) · REQ-002 (`WarpEvent` returns the outcome)
  · REQ-003 (switch + player placement + state carry) · REQ-004 (registry + `.expect`) · REQ-005 (bad-target
  totality) · REQ-006 (save records the active map — scope TBD) · REQ-007 (FULL gate).

## Phase 2 — Design

> Built in two layers so implement proves the RUNTIME first (gate-green), then layers the STUDIO on top.

### Architecture — RUNTIME half
**Grounding:** `BehaviourRegistry` = a `Registration[]` single source `(Name, ParamKeys, Factory(cell,trigger,params)→BehaviourResult)`;
`Factories`/`Kinds` derive from it; `TryMaterialize` is total. `WorldSim` holds one get-only `Map` + a fresh
`GameState`; `OutcomeApplier(state, Action<string> showMessage)` switches on the closed `Outcome` set + THROWS on
unknown. `ShowTextEvent` is the recipe; `ShowText.expect` = `=> ShowMessage("…")`, validated via
`HandlerRegistry` (the oracle's module→factory table) at gate:13. `SaveState` = Switches/Counters/Player/Rng (no
map id yet).

1. **Warp outcome (additive):** `Abstractions/Outcome.cs` += `public sealed record Warp(string MapId, GridPoint Cell) : Outcome;`
   (the closed hierarchy extends here; `GridPoint` is the Abstractions primitive).
2. **`WarpEvent : IMapEvent`** (`Engine.Sim/Events/WarpEvent.cs`, mirrors `ShowTextEvent`): `(cell, trigger,
   string mapId, GridPoint target)`; `Run(ctx) => [new Warp(mapId, target)]` — pure, no mutation (D-0017).
3. **Registry:** one `Registration` `("Warp", ["map","x","y"], factory)` — the factory reads `map` + parses `x`/`y`
   (int.TryParse) and returns `BehaviourResult.Failure` on a missing/unparseable param (total). Editor + runtime
   derive from it (single source, D-0024).
4. **`.expect` + oracle:** `Events/Expectations/Warp.expect` (`=> Warp("town", (3, 4))`); `HandlerRegistry` += a
   `"Warp"` factory. **The `ExpectationParser` must learn the `Warp` outcome + its `(x, y)` GridPoint arg** (today
   it parses string/bool/int args for SetSwitch/AddCounter/ShowMessage) — a small parser extension.
5. **Switch seam:** `OutcomeApplier` gains a 3rd ctor arg `Action<Warp> requestWarp` and a `Warp` arm that calls
   it (signals, never mutates). `WorldSim`: make `GameState` injectable — `TryCreate(map, player, events, doors,
   GameState? state = null)` → `State = state ?? new GameState()`; add `Warp? PendingWarp` (cleared at the top of
   `MovePlayer`/`PressAction`, set via the applier sink `warp => PendingWarp = warp`).
6. **`GameSession`** (`Engine.Sim/GameSession.cs`, NEW, gated) — the orchestrator above `WorldSim`. Holds the map
   **registry** (`IReadOnlyDictionary<string,string> id→$data-json`, host-supplied — keeps the gated layer free of
   embedded-resource IO + makes it unit-testable) + the start id + the **active `WorldSim`**. `Create(maps,
   startId)` → typed result (unknown start id → failure). `MovePlayer(dir)`/`PressAction()` delegate to `Active`
   then **apply any `Active.PendingWarp`**: load the target map's json by id (unknown id → safe no-op/typed,
   never throw), `MapSerializer.Deserialize` → materialise events via `BehaviourRegistry` → `WorldSim.TryCreate`
   **carrying `Active.State`** + placing the player at the target cell (off-map cell → clamp/no-op, total); swap
   `Active`. Deterministic (no clock/RNG). `Active` exposed for the host to render.
7. **Manifest model:** `Engine.World/GameManifest.cs` (NEW) `{ string StartMap; string[] MapIds }` + a total
   JSON (de)serializer (STJ source-gen, like `MapJsonContext`). The bundled `content/game.json` + a SECOND
   `content/maps/<id>.json` (with a `Warp` placement back to start) prove it.
8. **Runtime host:** `StartMap` adapts to a multi-map bootstrap (build the `GameSession` from the manifest +
   maps; keep a programmatic fallback). `Player/Game1.cs` reads the embedded `game.json` + every embedded map
   (`id→json`) → `GameSession.Create` → drives it; `Player.csproj` embeds `game.json` + globs `content/maps/*.json`.
   `Core/RpgGame.cs` drives the `GameSession` (renders `session.Active.Map`/`Player`; re-inits the camera offset
   on a switch).
9. **Save (REQ-006):** `SaveState` += `string MapId`; `SaveSerializer.Serialize(state, cell, facing, rng, mapId)`
   (a defaulted/overloaded param to keep #15 call-sites green) writes it; `Deserialize` reads it (absent →
   start map, back-compat). The host saves `session` active id; a load resumes that map.

### Architecture — STUDIO half (layers on the runtime)
10. **`MapProject`** (`Editor/MapProject.cs`, NEW, gated, NEUTRAL surface): an ordered set of maps
    (`id → MapPaintSession`, reusing #16/#19/#20/#21) + a start-map id + an active id. API: `MapIds`/`ActiveId`/
    `Active` (the current session) · `NewMap(id)` · `SelectMap(id)` · `Save()` → the manifest json + `id→map json`
    (a bundle the host writes as `game.json` + per-map files — what the runtime reads) · `Load(manifest, id→json)`.
    Reuse `MapPaintSession`; do not rebuild it. String ids / `Cell` only (no XNA/Avalonia).
11. **Studio host** (`Studio/MainWindow.cs`, thin, excluded): a **map list** (+ New / switch) bound to
    `MapProject`; switching re-points the canvas/palette/inspector at `project.Active` + reloads its sheet (#20).
    Save writes the set. **The Warp inspector — kind-aware, single-source (the #20 drift lesson):** the inspector
    renders a field per the selected kind's `ParamKeys` (from `BehaviourKindInfo`, NOT hardcoded) — `text` → a
    text box; `map` → a **dropdown of the project's map ids**; `x`/`y` → numeric inputs. So `ShowText` shows the
    text box and `Warp` shows map+x+y, both driven by the registry descriptor (can't drift).
12. **Cross-layer:** a test materialises the editor's saved set + each map's `Warp` placements through the runtime
    (`GameSession`) — the registry resolves the targets (REQ-009).

### File manifest
  | # | File | Change |
  |---|---|---|
  | 1 | `Abstractions/Outcome.cs` | +`Warp(string MapId, GridPoint Cell)` case (additive). |
  | 2 | `Engine/Sim/Events/WarpEvent.cs` | NEW — `WarpEvent : IMapEvent` (mirrors `ShowTextEvent`). |
  | 3 | `Engine/Sim/BehaviourRegistry.cs` | +`("Warp", ["map","x","y"], factory)` registration (total param parse). |
  | 4 | `Engine/Sim/Events/Expectations/Warp.expect` | NEW — `=> Warp("town", (3, 4))`. |
  | 5 | `Editor/Expectations/HandlerRegistry.cs` | +`["Warp"]` handler factory. |
  | 6 | `Editor/Expectations/ExpectationParser.cs` | learn the `Warp` outcome + its `(x, y)` GridPoint arg. |
  | 7 | `Engine/Sim/OutcomeApplier.cs` | +`Action<Warp>` ctor arg + the `Warp` arm (signal). |
  | 8 | `Engine/Sim/WorldSim.cs` | injectable `GameState`; +`PendingWarp` (clear/set). |
  | 9 | `Engine/Sim/GameSession.cs` | NEW — the orchestrator (registry + active sim + switch + carry). |
  | 10 | `Engine/World/GameManifest.cs` | NEW — `{ StartMap, MapIds }` + total (de)serializer. |
  | 11 | `Engine/Sim/StartMap.cs` | adapt to the multi-map bootstrap (GameSession from manifest + maps). |
  | 12 | `content/game.json` + `content/maps/<second>.json` | NEW — the bundled two-map set with a Warp. |
  | 13 | `content/maps/start.json` | +a `Warp` placement to the second map (regen). |
  | 14 | `Engine/Core/RpgGame.cs` | drive `GameSession`; re-init camera on switch. |
  | 15 | `Player/Game1.cs` + `Player.csproj` | read manifest + embed all maps (glob); build + drive the session. |
  | 16 | `Engine/Sim/SaveState.cs` + `SaveSerializer.cs` | +`MapId` (additive; back-compat default). |
  | 17 | `Editor/MapProject.cs` | NEW — the gated multi-map set (reuses `MapPaintSession`). |
  | 18 | `Studio/MainWindow.cs` (+ a map-list control) | map list + switch; the kind-aware Warp inspector. |
  | 19 | `tests/.../MultiMapWarpTests.cs` | NEW — REQ-001..006/009 (runtime). |
  | 20 | `tests/.../MapProjectTests.cs` | NEW — REQ-007/008 (the set + the param-key inspector single-source). |
  | 21 | `tests/.../{OracleCli,SaveSerializer,RuntimePlayable,StartMap}Tests` | MOD — Warp oracle row; SaveState MapId; multi-map bootstrap. |

### Regression Test Plan
  | # | Test | Proves |
  |---|---|---|
  | T1 | `GameSession.Create(maps, startId)` loads the start map; `Create` with an unknown start id → typed failure (no throw) | REQ-001 |
  | T2 | `WarpEvent.Run` → `[Warp(mapId, cell)]`; the registry materialises a `Warp` placement (map/x/y) → a `WarpEvent`; a bad/missing param → `BehaviourResult.Failure` | REQ-002/004 |
  | T3 | step/action a Warp tile → `Active` becomes the target map, the player sits at the target cell, and a switch set before the warp is still set after (GameState carried) | REQ-003 |
  | T4 | `Warp.expect` reproduces via the oracle (gate:13 — 6 rows / 4 modules); the `ExpectationParser` parses `Warp("town",(3,4))` | REQ-004 |
  | T5 | a Warp to an unknown map id, and to an off-map target cell → safe (no throw; `Active` unchanged / clamped) | REQ-005 |
  | T6 | `SaveSerializer` round-trips `MapId`; a save after a warp records the active map; absent `MapId` → start map (back-compat) | REQ-006 |
  | T7 | `MapProject`: `NewMap`/`SelectMap` + `Save`→`Load` round-trips the manifest + every map's `$data` (ids + active preserved) | REQ-007 |
  | T8 | the inspector's field set for a kind == its `BehaviourKindInfo.ParamKeys` (ShowText→["text"], Warp→["map","x","y"]) — single-source, gated | REQ-008 |
  | T9 | cross-layer: the editor's saved set → each map materialises through `GameSession`; a Warp placement's target resolves in the registry | REQ-009 |
  | — | host smoke (MANUAL): run the Player on the two-map set → walk onto the Warp tile → the second map renders; Studio → add a map, place a Warp, Save → run | REQ-003/007/008 |
  | — | FULL `bin/gate.sh` green; Engine + Editor coverage + MSI floors; gate:13 oracle (now incl. Warp) | REQ-010 |

  Keep green untouched: the #18/#19/#20/#21 suites (the registry/session/event/tileset/undo tests) — additive
  changes. Excluded (host, manual smoke): `RpgGame`/`Game1`/`Studio` (the live loop + Avalonia); the gated
  `GameSession`/`GameManifest`/`MapProject`/`WarpEvent`/registry/serializer carry the coverage.

### Risks / decisions
- **Size (load-bearing):** ~21 files across Abstractions + Engine.Sim core + Player + Editor + Studio. Implement
  in TWO passes — (A) the runtime (items 1–16), gate-green end-to-end via the two-map smoke + T1–T6; then (B) the
  Studio (17–18) + T7–T9. The runtime is the spine; if the Studio UX runs long it can be trimmed without
  unwinding the runtime.
- **`WorldSim.GameState` injection** is the one change to a load-bearing type — additive (a defaulted optional
  param), so #15/#17/#18 `TryCreate` call-sites stay green.
- **`SaveSerializer.Serialize` signature** gains `mapId` — default it (or overload) to keep #15 call-sites; the
  save JSON gains a `mapId` field → the #15 exact-JSON save tests update (small, like #20's start.json).
- **`.expect` parser extension** for the `Warp` `(x,y)` arg — the one non-obvious bit; keep the syntax simple
  (`Warp("town", (3, 4))`), and gate:13 proves it.
- **Determinism / D-0017:** the behaviour only RETURNS the `Warp`; the `GameSession` (sim-host) applies the
  switch — deterministically, carrying `GameState`. No clock/RNG in the switch.
- **MRM/§14:** `GameSession`/`GameManifest` are sim/world; registry lookups are `TryGetValue` (no
  Dictionary-enumeration), strings/ints only, injected RNG; neutral Editor surface; XML-doc.

## Phase 3 — Implement
- **Built — PASS A (runtime, proven gate-green first):**
  - `Abstractions/Outcome.cs` +`Warp(string MapId, GridPoint Cell)`. `Engine/Sim/Events/WarpEvent.cs` (NEW,
    mirrors `ShowTextEvent`). `BehaviourRegistry` +`("Warp", ["map","x","y"], factory)` (total param parse).
    `Events/Expectations/Warp.expect` + `HandlerRegistry["Warp"]` + `ExpectationParser` learned the `Warp` outcome.
  - Switch seam: `OutcomeApplier` +`Action<Warp>` sink + the `Warp` arm (signal, no mutation); `WorldSim`
    injectable `GameState` (`TryCreate(..., GameState? state = null)`) + `PendingWarp` (clear/set).
  - `Engine/Sim/GameSession.cs` (NEW, gated) — the orchestrator: `id→json` registry + active sim;
    `Create(maps, startId, startCell)`; `MovePlayer`/`PressAction` delegate then apply `PendingWarp` (load target,
    carry `GameState`, place player, total on bad id/cell). `Engine/World/GameManifest.cs` (NEW) `{StartMap,MapIds}`
    + total serializer.
  - `StartMap` +`TownBuild`/`TownEvents`/`BuildSession` + a `Warp` in `Events()`. `RpgGame` drives a `GameSession`
    (`Sim => _session.Active`). `Game1` reads the embedded `game.json` + maps → `GameSession.Create` (fallback to
    `StartMap.BuildSession`). `Player.csproj` embeds `game.json` + globs `content/maps/*.json`. Regenerated
    `start.json` (with the Warp) + NEW `town.json` + `game.json`.
  - `SaveState` +`MapId`; `SaveSerializer.Serialize(..., mapId = "")` (defaulted) + writes it.
- **Built — PASS B (Studio):**
  - `Editor/MapProject.cs` (NEW, gated, neutral): `id→MapPaintSession` set + manifest + active; `NewMap`/
    `SelectMap`/`SetStartMap`/`Save→SavedProject`/`Load`. Reuses `MapPaintSession`.
  - `Studio/MainWindow.cs` rewritten to drive a `MapProject` (`Session => _project.Active`): a **map list**
    (New / switch, re-binds the canvas/palette/history/sheet), and a **kind-aware inspector** — the param fields
    are rebuilt from the selected kind's `BehaviourKindInfo.ParamKeys` (`text`→box, `map`→a ComboBox of the
    project's ids, `x`/`y`→numeric), single-source (the #20 lesson). Save writes the SET to a folder (`game.json`
    + per-map). `TilePalette` +`SetSession` (switch with the active map).
- **Build / verify:** whole solution `-warnaserror` **0/0** (neutral boundary held — `MapProject` + Studio compile
  against the gated/neutral surface). gate:13 oracle **6 rows / 4 modules** (Warp auto-discovered + reproduces).
  **GATE GREEN [fast]** — 11/11.
- **Deviations from design (+ reason):**
  1. `Warp.expect` uses a **flat 3-arg** syntax `Warp("town", 3, 4)` (not `(3,4)`) — reuses the parser's `TryArgs`
     (a tuple's inner comma would break the naive comma split). Cleaner + total.
  2. `SaveSerializer.Serialize` gained a **defaulted** `mapId = ""` (kept #15's 4-arg call-sites green).
  3. `GameSession.Create` takes a **start cell** (the design didn't pin where it comes from); the player `Actor`
     is rebuilt per map (Slice A carries `GameState` switches/counters, not player stats — additive later).
  4. The town map uses the **same mountains tileset** so no re-skin-on-warp is needed (RpgGame loads one sheet);
     the switch is proven by the map changing. (A different-sheet warp + re-skin is a follow-up.)
  5. The Studio **Save uses a folder picker** (writes `game.json` + per-map files = the runtime's embedded layout).
  6. Updated 3 existing assertions (registry/`Events()` now carry the `Warp` entry) + the `OutcomeApplier` ctor
     (3rd arg + a null-guard assert) — additive suite maintenance.
- **NOT yet done (Phase 4 — Validate):** `MultiMapWarpTests` + `MapProjectTests` (REQ-001..009 gated) + the FULL
  gate (coverage + mutation). Inspect (Phase 3.5) runs next.

## Inspect (Phase 3.5)
- **Lenses run:** 2 independent parallel critics — (1) correctness/totality, (2) design/MRM/reuse — **both survived
  the API this time** and verified concretely (critic-1 ran its own 17-assertion throwaway repro green; critic-2
  ran the Architecture tests 4/4). Plus an author throwaway warp-switch repro (deleted). **No BLOCKERs/MAJOR code
  defects** — the warp switch, state-carry, totality, D-0017 purity, and determinism are all verified correct.
- **Findings:**
  | # | Severity | Finding | Verdict | Fix |
  |---|---|---|---|---|
  | 1 | MINOR | `ZzReproWarp.cs` (+ a critic's `ZzReproInspect.cs`) untracked throwaways would ride into the suite | **REAL** | **FIXED** — deleted both (the `PR-claude-sweep-subagent-throwaway-files` rule in action). |
  | 2 | MINOR | Studio `HistoryChanged` subscription **leak** — `RebindActiveSession` `+=` on each map switch without `-=` (handlers accumulate, original map fires N-fold). Host, `[ExcludeFromCodeCoverage]`, idempotent handler so not a correctness bug, but a real leak. | **REAL** | **FIXED** — detach-before-attach via `_subscribedSession`. `BF-studio-history-subscription-leak-001` / `PR-claude-detach-before-resubscribe-on-swap-001`. |
  | 3 | "MAJOR" (critic-2) | "REQ-006 save-after-warp dead-wired" — the serializer records `MapId` but no host save menu calls it | **RE-SCOPED (not a code bug)** — the **serializer level is met + unit-testable**; the in-game save/load **host** is deferred, consistent with #15 (which is also serializer-only — no save menu exists). Spec REQ-006 updated to say so. | spec re-scope |
  | 4 | MINOR | `GameSession.PlacePlayer` clamps off-map but not a **blocking** target cell (a warp onto a wall clips the player) | **ACCEPTED (deferred polish)** — total (no throw, no soft-lock; `TryStep` lets the player walk off); the bundled warps target floor. Phase 4 pins the current behaviour. | none |
  | 5 | NIT | the `.expect` parser + registry accept an empty-quoted Warp map id | **ACCEPTED** — total end-to-end (resolves to a no-op); Phase 4 pins it | none |
  | 6 | NIT | "re-init camera on switch" doc overstates (the per-frame `ViewOffset` recompute follows the new map for free) | **ACCEPTED** — doc prose, no code defect | none |
  | 7 | NIT | `GameSession.LoadMap` duplicates `StartMap.CreateWorld`'s deserialize→materialise→`TryCreate` | **ACCEPTED** — defensible (variable placement + state injection, different layers); a follow-up could extract a shared `Materialize` helper | none |
- **Cleared (verified, not just unflagged):** contract-first/single-source (Warp = one `Registration` + `.expect`
  + `HandlerRegistry` entry; the Studio inspector renders fields from `BehaviourKindInfo.ParamKeys`, can't drift —
  the #20 lesson); neutral boundary (`MapProject` XNA-free; build 0/0; arch 4/4); MRM/§14 (`GameSession`/
  `GameManifest` in Sim/World, registry via `TryGetValue`, strings/ints, deterministic switch, injected
  `GameState`); `MapProject` Save/Load **total + runtime-valid** (the saved set ↔ `GameSession.Create` cross-layer
  contract holds); all four deviations sound.
- **Post-fix verify:** build `-warnaserror` 0/0; **GATE GREEN [fast]**.
- **Capture:** `failure-record` **BF-studio-history-subscription-leak-001** (`43342db8`); `prevention-rule-record`
  **PR-claude-detach-before-resubscribe-on-swap-001** (`2ebfc44d`). REQ-006 re-scoped (serializer-ready; host save
  deferred).
- **Phase-4 kill-list (adopted from critic-1):** ① `GameSession.Create` start-load; unknown start id **and**
  malformed start `$data` → `Failure`/no-throw. ② `WarpEvent.Run`==`[Warp]` (purity); registry materialises a Warp
  placement; missing/non-int `map`/`x`/`y` → `BehaviourResult.Failure`. ③ StepOn **and** ActionButton warp →
  Active=target + player at the target cell + a pre-warp switch **and** counter still set (state carry, both
  triggers). ④ applied-exactly-once / **no chain** in one step (PendingWarp cleared each step). ⑤ totality matrix
  (each no-op/clamp, asserting **no throw**): unknown target map · off-map cell (incl. 1×1) · malformed target
  `$data` · target whose events fail to materialise. ⑥ blocking-target → pin the current behaviour. ⑦ `Warp.expect`
  reproduces (gate:13); parser parses `Warp("town",3,4)` + returns a typed error (no throw) for the tuple / unquoted
  / non-int / wrong-arity forms. ⑧ `SaveSerializer` round-trips `MapId` (save-after-warp records it; absent → start).
  ⑨ cross-layer (REQ-009): the editor's saved set materialises through `GameSession`; each Warp target resolves.
  ⑩ `MapProject` set round-trip (NewMap no-dup / Select / Save→Load) + the inspector field-set == the kind's
  `ParamKeys` (single-source, REQ-008).

## Phase 4 — Validate
- **Tests added (+61; 410 → 471):**
  - `tests/.../MultiMapWarpTests.cs` — REQ-001..006/009 + the parser + manifest + bootstrap: Create load/unknown/
    malformed-start; WarpEvent purity (map + cell, isolated); registry materialise + missing-map/non-int-x/non-int-y;
    state carry (switch **and** counter, StepOn **and** ActionButton); apply-exactly-once / no-chain; the totality
    matrix (unknown map · off-map cell incl. 1×1 + the exact (1,1) fallback · malformed `$data` · unmaterialisable
    events — each asserting no throw); the blocking-target pin; save `MapId` round-trip + back-compat + after-warp;
    the cross-layer editor→runtime warp; `GameManifest` round-trip + empty-start/null-mapIds/malformed; the
    `StartMap` two-map bootstrap (`TownBuild`/`TownEvents`/`BuildSession`); Create/Save/WarpEvent null guards; the
    `.expect` Warp grammar + the malformed-is-typed-error theory.
  - `tests/.../MapProjectTests.cs` — REQ-007 set CRUD (new / dup-reject / select / start-pointer), Save→Load
    round-trip (ids + start + per-map content), Load totality (malformed manifest · missing map · invalid map
    `$data` · start-not-in-set · ctor empty-id), and REQ-008 the ParamKeys single-source (Warp=[map,x,y], ShowText=[text]).
- **`dotnet test MonoRpgMaker.slnx`: 471 passed, 0 failed.**
- **`bin/gate.sh`: GATE GREEN [full] — 13/13.** Coverage **93.0%** (floor 80). Mutation: Engine **85.43%** ·
  Abstractions **82.22%** · Analyzers **91.18%** · Editor **85.45%** (floor 80). Oracle 6 rows / 4 modules. Receipt written.
- **Mutation recovery (the one red→green loop):** the first FULL gate was RED at Engine **79.69%**. Root cause: the
  new `StartMap.TownBuild`/`TownEvents`/`BuildSession` bundled-bootstrap methods landed as **NoCoverage** (only the
  `[ExcludeFromCodeCoverage]` `Game1` called them — the gated tests use programmatic maps). Fixed by adding gated
  coverage for the bootstrap + the `GameManifest` (de)serialize + `PlacePlayer` exact-fallback + the null guards →
  Engine 79.69 → **85.43**.
- **Pre-existing exclusions:** the `SaveSerializer.Deserialize` error-string survivors are pre-existing (#15), not
  this slice's regression — left as-is (Engine still 85.43 ≥ 80).

## Phase 5 — Complete
- **Docs updated:** `docs/decisions.md` (**D-0026** — the multi-map model: embedded per-map `$data` by id + a
  `game.json` manifest; the `GameSession` map-switch seam carrying `GameState`); `docs/roadmap.md` (the multi-map +
  `Warp` paragraph — the Studio-v2 arc #19/#20/#21/#22 complete + trigger Slice 2's first built-in landed);
  `docs/product-phasing.md` (tile-map → many maps + transitions; the built-in library += `Warp`; the Studio-v2 row
  → multi-map authoring; the event-placement row → the kind-aware inspector).
- **Forge capture:** `aar-submit` 1f2ab922 (completed, effectiveness 4; 4 novel findings, 12 verdicts).
  `architecture-decision-record` **AD-claude-multimap-model-001** (21ad51c3). `prevention-rule-record`
  **PR-claude-bootstrap-methods-need-gated-coverage-001** (bce934dd — new gated methods only called by excluded
  hosts land as mutation NoCoverage + sink the MSI; cover them with a gated test). (Inspect already filed
  `BF-studio-history-subscription-leak-001` + `PR-claude-detach-before-resubscribe-on-swap-001`.)
- **Ticket closed:** #22 (`1ba1efe9-…`) → done; `TICKET-0022-multimap-warp.md` moved open/ → closed/.
- **Archived:** spec + notes moved active/ → completed/; spec status `Phase 5 — Complete PASS`.
- **Follow-ups (open):** the rest of the built-in library (chest / door / shop / give-item); the in-game save/load
  HOST wiring (REQ-006 is serializer-ready); cross-map tileset re-skin on warp; a shared `Materialize` helper to
  dedupe `GameSession.LoadMap` vs `StartMap.CreateWorld`; carrying player stats (not just `GameState`) across a warp.
