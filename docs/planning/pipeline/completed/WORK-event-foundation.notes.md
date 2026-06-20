# WORK-event-foundation — Notes

## Phase 1 — Plan
- **Request:** the event-trigger foundation (Phase A, Slice 1 of D-0024 / INTAKE-event-trigger-system) — placed
  events in `$data`, a behaviour registry, a built-in `ShowText` you walk up + talk to. **AUTONOMOUS** through
  merge (like #16/#17) — no pauses.
- **Classification:** larger feature, ONE shippable vertical (the foundation + the first built-in). Slice 2 (more
  built-ins), Phase B (Studio UI placement), Phase C (agent flow) are separate pipelines.
- **Promoted from** docs/planning/intake/INTAKE-event-trigger-system.md (status → promoted).
- **Contract discipline (D-0024) — non-negotiable:** implement `IMapEvent` → register the `kind` → declare its
  `.expect`. No one-off triggers; built-in + agent kinds interchangeable through the one registry.
- **Ground (verified):** `IMapEvent` (Cell/Trigger/Order/`Run→Outcomes`) + the closed `Outcome` set
  (`SetSwitch`/`AddCounter`/`ShowMessage`) — `ShowText` REUSES `ShowMessage`. `TileMapData{Width,Height,Tiles[]}`
  → add `Events[]`. `WorldSim.TryCreate(...,events,...)` + `PressAction()` → `CurrentMessage` → `RpgGame` banner.
  `StartMap.LoadWorld` passes empty events today. `.expect` DSL + `HandlerRegistry` (name→`Func<IMapEvent>`) → gate:13.
- **Ticket:** 4afa20c7-7974-45e1-afca-87a0c2579bb5 (#18). **AAR id:** fa3b7ace-3aa9-4cd2-a2d7-5d17799f3668
- **EARS:** REQ-001..006. **Proceeding autonomously to design (no pause).**

## Phase 2 — Design

### Architecture / approach — contract-first, the one repeatable pattern (D-0024)
**Structural placement data (Engine.World — total deserialize):**
- `TileMapData` gains `EventData[] Events { get; set; } = []`. `EventData = { string Id, int X, int Y, string
  Trigger, string Kind, Dictionary<string,string> Params }`. Source-gen via the existing `MapJsonContext`
  (`[JsonSerializable(typeof(TileMapData))]` walks the graph; STJ source-gen supports `Dictionary<string,string>`).
- `MapLoadResult` gains `IReadOnlyList<EventData> Events` (the loaded placements; `[]` when absent).
- `MapSerializer`: `Serialize(TileMap map, IReadOnlyList<EventData> events)` (the no-event overload stays as
  `events: []`); `Deserialize` is **TOTAL** — validates each `EventData` ELEMENT (non-null; `Id`/`Kind`/`Trigger`
  non-empty; `Trigger` ∈ {StepOn, ActionButton}) → a typed `MapLoadResult.Failure`, never a throw (reuse
  PR-claude-validate-deserialized-elements-not-just-container). `Events` null/absent → `[]` (older maps still load).
  Structural only — the *semantic* binding (kind → behaviour) is the registry's job (Sim).

**Semantic binding — the SINGLE registry + the built-in (Engine.Sim):**
- `BehaviourRegistry` — `kind` (string) → a factory `Func<EventData, BehaviourResult>` held in a `Dictionary`
  **read via `TryGetValue` (never foreach — MRM-clean)**. `TryMaterialize(EventData) → BehaviourResult` (an
  `IMapEvent` or a typed error): unknown `kind` → error; bad `Trigger`/cell → error; missing `Params["text"]` →
  error. This is the ONE binding point for built-in AND future agent kinds (an agent module registers its `kind`
  here the same way — noted, not built). Registers the built-in `"ShowText"`.
- `ShowTextEvent : IMapEvent` — `ctor(GridPoint cell, EventTrigger trigger, string text)`; `Run` →
  `[new ShowMessage(text)]` (REUSE the closed-set outcome; declarative — D-0017). The whole behaviour.

**Contract proof — the `.expect` oracle (gate:13), done right for a growing library:**
- New built-in Expectations home: `src/MonoRpgMaker.Engine/Sim/Events/Expectations/ShowText.expect`
  (`=> ShowMessage("Welcome, traveller!")`). `HandlerRegistry` (Editor) gains `["ShowText"] = () => new
  ShowTextEvent(GridPoint.Zero, EventTrigger.ActionButton, "Welcome, traveller!")`.
- `OracleCli.CheckExpectations` → scan **recursively** (`SearchOption.AllDirectories`); gate:13's
  `check-expectations` arg → `src/MonoRpgMaker.Engine/Sim` (covers `Tracer/Expectations` + `Events/Expectations` +
  future). A justified, minimal gate change — the contract discipline wants every built-in oracle-validated, and
  built-ins shouldn't squat in the tracer folder. (Fallback if recursion is risky: co-locate in `Tracer/Expectations`
  + no gate change — design picked the recursive-scan home.)
- `.expect` fixes one text sample; **unit tests** cover the parameterisation (`text T → [ShowMessage(T)]`).

**Runtime (Engine.Sim + host):**
- `StartMap`: `Build() → TileMap` (tiles, unchanged) + NEW `Events() → EventData[]` (the placements — one `ShowText`
  at an interior cell, `ActionButton`, a friendly line). `LoadWorld(json)`: `Deserialize` → on Ok, `TryMaterialize`
  each `result.Events` via the registry → the `IMapEvent` list → `WorldSim.TryCreate(map, player, events, [])`; a
  materialisation failure → a typed `WorldSimResult.Failure` (never a throw); `Game1`'s fallback still opens.
- `WorldSim` exposes `IReadOnlyList<GridPoint> EventCells` (read accessor over the schedule) so the host can mark them.
- `RpgGame` (`[ExcludeFromCodeCoverage]`): Space **and** Enter → `_sim.PressAction()` (mirror `Step()`'s
  title/banner handling); draw a distinct **marker quad** at each `EventCells` cell (under the camera transform).
- `content/maps/start.json` regenerated to carry the `ShowText` event; the #17 freshness test updated to compare
  against `Serialize(StartMap.Build(), StartMap.Events())`.

### File manifest
| File | Change |
|---|---|
| `src/MonoRpgMaker.Engine/World/TileMapData.cs` | MODIFY — `EventData[] Events`; NEW `EventData` DTO (Id/X/Y/Trigger/Kind/Params) |
| `src/MonoRpgMaker.Engine/World/MapLoadResult.cs` | MODIFY — carry `IReadOnlyList<EventData> Events` |
| `src/MonoRpgMaker.Engine/World/MapSerializer.cs` | MODIFY — serialize events; **total** element validation; `MapJsonContext` covers `EventData` |
| `src/MonoRpgMaker.Engine/Sim/BehaviourRegistry.cs` | NEW — `kind`→factory, `TryMaterialize` (typed), registers ShowText |
| `src/MonoRpgMaker.Engine/Sim/Events/ShowTextEvent.cs` | NEW — `IMapEvent` → `[ShowMessage(text)]` |
| `src/MonoRpgMaker.Engine/Sim/Events/Expectations/ShowText.expect` | NEW — the contract sample |
| `src/MonoRpgMaker.Engine/Sim/StartMap.cs` | MODIFY — `Events()`; `LoadWorld` materialises via the registry |
| `src/MonoRpgMaker.Engine/Sim/WorldSim.cs` | MODIFY — `EventCells` read accessor |
| `src/MonoRpgMaker.Editor/Expectations/HandlerRegistry.cs` | MODIFY — `+ "ShowText"` sample |
| `src/MonoRpgMaker.Editor/Expectations/OracleCli.cs` | MODIFY — recursive `*.expect` scan |
| `bin/gate.sh` | MODIFY — gate:13 scans `src/MonoRpgMaker.Engine/Sim` |
| `src/MonoRpgMaker.Engine/Core/RpgGame.cs` | MODIFY — Space/Enter→`PressAction` + event marker |
| `content/maps/start.json` | REGEN — carries the `ShowText` event |
| `tests/MonoRpgMaker.Engine.Tests/EventFoundationTests.cs` | NEW — the plan below |
| `tests/MonoRpgMaker.Engine.Tests/RuntimePlayableTests.cs` | MODIFY — the #17 freshness test for the new shape |

### Regression Test Plan
| T | REQ | Test (isolated exact-count asserts) |
|---|---|---|
| T1 | 001 | `Serialize(map, [event])` → `Deserialize` → `Ok`; `Events` round-trips (count + Id/cell/Trigger/Kind/`Params["text"]`). |
| T2 | 001 | Total: `Deserialize` of events with a null element / empty `Kind` / bad `Trigger` string / missing array → `!Ok`, `Error` set, **no throw**; events absent → `Ok` with `Events == []`. |
| T3 | 002 | `BehaviourRegistry.TryMaterialize` of a `ShowText` placement → an `IMapEvent` at the right cell/trigger; **unknown kind** → typed error; **missing `text`** → typed error (no throw, no crash). |
| T4 | 003 | `ShowTextEvent(cell, ActionButton, "Hi").Run(ctx)` → exactly `[new ShowMessage("Hi")]`; a second text `T` → `[ShowMessage(T)]` (parameterisation). |
| T5 | 004 | `StartMap.LoadWorld(Serialize(Build(), Events()))` → `Ok`; the `WorldSim` has the event; facing its cell + `PressAction()` → `CurrentMessage == the text`; not facing it → no message. |
| T6 | 001/005 | Freshness (updated #17): `content/maps/start.json` == `Serialize(StartMap.Build(), StartMap.Events())` (newline-normalised) — committed `$data` carries the event + isn't stale. |
| — | 005 | The Player renders the marker + Space shows the dialogue — **MANUAL smoke** (host `[ExcludeFromCodeCoverage]`). |
| — | 003/006 | gate:13 reproduces `ShowText.expect` from the real handler; FULL gate — Engine MSI ≥ 80 (schema/registry/ShowText), Editor MSI ≥ 80, coverage, build, oracle. |

### Risks / decisions
- **`$data` shape change vs #17 `start.json` freshness** — regenerate `start.json` WITH the event and update the
  freshness test to `Serialize(Build(), Events())`; `Events` defaults to `[]` so the change is additive/back-compat.
- **gate:13 scan-dir change** — `OracleCli` → recursive; gate:13 → `Sim`. Verify it still finds the tracer tables
  (Lever/Chest) + the new ShowText, and that a registered module still requires ≥1 row.
- **`Dictionary<string,string> Params` in gated Engine.World/Sim** — a Dictionary FIELD + `TryGetValue` is
  MRM-clean; **never foreach it** (MRM bans Dict enumeration). Verify `-warnaserror`/MRM on the schema + registry.
- **Total deserialize over nested events** — validate elements (PR-claude-validate-deserialized-elements-not-just-container);
  a hostile placement (null entry, bad trigger) must fail typed, not throw.
- **MSI** — Engine gains the schema/registry/ShowText (well-testable; should rise); Editor gains only a
  HandlerRegistry entry + an OracleCli tweak (re-measure ≥ 80). Player host exempt.
- **Marker/EventCells** — `WorldSim` exposing event cells is a small read accessor; the marker is host-only.
- **DEFERRED (unchanged):** Studio UI placement (Phase B); agent flow (Phase C); Autorun/Parallel; multi-map/Warp;
  GiveItem/Shop/Door (Slice 2, same recipe); richer dialogue.

## Phase 3 — Implement
- **Built (contract-first):**
  - `World/TileMapData.cs` (+`EventData{Id,X,Y,Trigger,Kind,Params:Dictionary<string,string>}` + `Events[]`),
    `World/MapLoadResult.cs` (+`Events`), `World/MapSerializer.cs` (`Serialize(map,events)` + **total** element
    validation of placements — null/empty-id-kind-trigger/bad-trigger → typed Failure).
  - `Sim/BehaviourRegistry.cs` (the ONE binding point: `kind`→factory via `TryGetValue`; `TryMaterialize` →
    `BehaviourResult`; unknown trigger/kind/missing-`text` → typed error) + `Sim/Events/ShowTextEvent.cs`
    (`IMapEvent` → `[ShowMessage(text)]`) + `Sim/Events/Expectations/ShowText.expect`.
  - `Editor/Expectations/HandlerRegistry.cs` (+ShowText sample), `OracleCli.cs` (recursive `*.expect` scan),
    `bin/gate.sh` (gate:13 → `src/MonoRpgMaker.Engine/Sim`, covers Tracer + the new Events).
  - `Sim/StartMap.cs` (`Events()` = one `ShowText` at (6,4); `CreateWorld` materialises via the registry),
    `Sim/WorldSim.cs` (+`EventCells`), `Sim/HookSchedule.cs` (+`Events`), `Core/RpgGame.cs` (Space/Enter →
    `Act`→`PressAction` + a gold event marker). `content/maps/start.json` regenerated with the event.
- **Build:** full solution `-warnaserror` → **0/0** (Engine.World/Sim MRM-clean — Dictionary read-only via
  `TryGetValue`, `foreach` only over Lists, `Enum.TryParse`+`IsDefined`). **gate:13 oracle: 5 rows / 3 modules
  reproduced — OK** (Lever, Chest, **ShowText**).
- **Deviations:** the event cell (6,4) is a walkable floor cell — you face it from an adjacent tile + press Space
  (a "solid/blocking sign tile" so you bump into it is a noted polish follow-up). `start.json` carries events
  now, so the #17 freshness test must be updated in Phase 4 to `Serialize(Build(), Events())`.
- **NOT yet run:** the new tests + the FULL gate — inspect next.

## Inspect (Phase 3.5)
Two parallel critics (both built+ran real code). Lenses: correctness/totality/build-run; design/contract/MSI.

**Critic 1 — correctness/totality/build-run: ZERO defects (all 7 items PASS, real output).**
- Schema round-trip (events survive Serialize→Deserialize with all fields) + events-absent → `Events==[]`.
- **Totality:** every malformed Deserialize (empty id/kind/trigger, unknown trigger "Foo", `events:[null]`,
  `events:null`, `params:null`) → no throw, `!Ok`, typed Error. Registry: unknown kind/trigger/missing-text/null-params
  → no throw, typed error.
- ShowText contract: `Run` → exactly `[ShowMessage(text)]`, value-equal, parameterised.
- **End-to-end:** `LoadWorld(Serialize(Build(),Events()))` → Ok; `EventCells` has (6,4); route (2,2)→…→(6,3) facing
  Down → `PressAction()` → `CurrentMessage == "Welcome to monorpgmaker! Use the arrow keys to explore."`; facing
  empty → no message. Committed `start.json` → LoadWorld Ok + (6,4) (not stale).
- Oracle: 5 rows / 3 modules reproduced, exit 0 (Lever/Chest/ShowText). GUI Space→dialogue = MANUAL smoke.

**Critic 2 — design/contract/MSI: contract discipline PASS.**
- (a) `ShowTextEvent` is a real `IMapEvent` reusing `ShowMessage` (no new outcome). (b) `BehaviourRegistry` is the
  SINGLE binding point — `grep "new ShowTextEvent"` → only the factory + the HandlerRegistry oracle sample;
  `StartMap.CreateWorld` materialises EVERY placement only through `TryMaterialize` (both BuildWorld + LoadWorld).
  (c) the factory delegate is kind-agnostic → a future agent kind registers identically. (d) no one-off triggers.
- §14 clean; MRM clean (every Dictionary read via `TryGetValue`; `foreach` only over `IReadOnlyList`).

**Findings + verdicts:**
| # | Sev | Finding | Verdict |
|---|---|---|---|
| 1 | — | correctness/totality/contract/end-to-end | **PASS — no fix** (critic 1 verified) |
| 2 | (expected) | the #17 freshness test fails (start.json now has events) | **Phase 4** — update `RuntimePlayableTests.cs:130` → `Serialize(Build(), Events())` |
| 3 | risk | OracleCli `SearchOption.AllDirectories` mutant NOT killed (OracleCli is mutation-covered, not excluded) | **Phase 4** — add a nested-subdir `.expect` test |
| 4 | (expected) | the new Engine types have ZERO tests | **Phase 4** — the 22-mutant kill-list (below) |
| 5 | nit | trigger validated twice (MapSerializer structural + registry semantic) | accepted — defense-in-depth; a future refactor could defer to the registry |
| 6 | nit | event cell (6,4) is walkable floor (face it, don't bump it) | accepted — marker-based interaction; "blocking sign tile" is a polish follow-up |

**No source fixes (no bugs).** No failure-record. **Captured:** PR-claude-enum-parse-test-numeric-oob-001.
**Phase-4 plan (the kill-list):** MapSerializer event-validation (each malformed branch + the `?? []` + the 2-event
round-trip) · BehaviourRegistry (the **numeric-"99" trigger** for `IsDefined`, unknown-kind, ShowText present/missing-text,
`Params ?? Empty`, BehaviourResult) · ShowTextEvent.Run · **StartMap.CreateWorld failure-return** (a crafted `$data`
that passes structural validation but fails materialisation — empty params) · WorldSim.EventCells · the OracleCli
**nested-dir recursion** test + a HandlerRegistry ShowText assert · and the #17 freshness fix.

## Phase 4 — Validate
- **Tests added:** `tests/MonoRpgMaker.Engine.Tests/EventFoundationTests.cs` (the inspect 22-mutant kill-list across
  `MapSerializerEventTests` / `BehaviourRegistryTests` / `ShowTextEventTests` / `StartMapEventTests` /
  `HandlerRegistryShowTextTests`), the **nested-dir** `OracleCliTests.Run_NestedExpect_FoundRecursively` (kills the
  `AllDirectories` mutant), and the updated #17 freshness test (`Serialize(Build(), Events())`). The three
  easy-to-miss mutants covered: the **numeric "99" trigger** (kills `Enum.IsDefined`), the **crafted-bad-`$data`**
  `CreateWorld` failure (empty-params passes structural, fails materialisation), the **nested-dir** recursion.
  **+24 tests (335 → 359).**
- **`dotnet test MonoRpgMaker.slnx`:** `Passed! Failed: 0, Passed: 359` (no regressions; the freshness test passes).
- **`bin/gate.sh` (FULL):** **GATE GREEN [full]** — 13/13.
  - coverage **92.9%** ≥ 80.
  - mutation: **Engine 86.26%** (schema/registry/ShowText/StartMap/WorldSim in scope, floor held),
    **Editor 84.62%** (the OracleCli recursive-scan + HandlerRegistry ShowText mutants killed — the inspect risk
    resolved), Abstractions 82.22, Analyzers 91.18. Player host exempt.
  - gate:13 `.expect` oracle: **5 rows / 3 modules reproduced — OK** (Lever, Chest, **ShowText**).
  - gate:4 vuln + gate:5 license clean (no new deps). Receipt written.
- **Pre-existing failures:** none.

## Phase 5 — Complete
- Docs / forge / ticket / archive:
