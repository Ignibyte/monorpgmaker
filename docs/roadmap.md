# Roadmap

**Revised 2026-06-17 (v2) — reordered after the adversarial review.** The thesis is tested FIRST with
a hand-wired tracer bullet; the chassis is built as an *extraction* from it; and the determinism +
by-construction-scaffolding guarantees are locked before any sim module. See
[agentic-substrate.md](agentic-substrate.md), [agentic-features.md](agentic-features.md), and
[agentic-strategy.md](agentic-strategy.md). The prior data / scene-graph / render foundations are
folded into the phases, not dropped.

## Where we are (done)

- Scaffold: `Engine` (`World` tiles/maps/direction, `Entities` movable actors, `Data` id-keyed
  `Database<T>`, `Core` `RpgGame` host), `Player`, `Editor` (headless logic), xUnit tests. Builds
  clean; `bin/gate.sh` green; forge codegraph + pipeline live.

## P-(-1) — Tracer bullet: prove the thesis FIRST

A hand-wired vertical slice under a RELAXED **prototype gate** (format + build + source-bans only — NO
generator, NO Abstractions assembly, NO replay-hash gate):

- A painted map + move/collide + ONE authored event (walk-on → message → set switch → open door) as
  plain C# against minimal interfaces; determinism via injected `IRandom` only.
- **The make-or-break spike:** time the "chest gives one potion, then is empty" task through the
  AI-directed loop vs. RPG Maker's click-path. If it isn't within ~3× and doesn't *feel* good for the
  technical-hobbyist user, the thesis is falsified here — cheaply, at week ~3, not on top of a
  six-month toolchain.
- Yields the demo + contributor on-ramp, AND the **first gate-clean feature** the golden
  patterns / skills / fixtures are derived from.

> Then build P0/P1 as an EXTRACTION / refactor of this working slice, so the generator and the locked
> decisions are designed against a known-good emit target. **MVP = the validated authoring loop, not
> the chassis.**

> **Status (2026-06-17): GO.** The tracer slice is built (`Engine.Sim` + `Sim.Tracer` + the `RpgGame`
> host), committed (`feat(m0)` 3655787, on the `build:` tooling commit), and **`bin/gate.sh` GREEN
> [full]** — 32 tests, coverage 98.8%, mutation MSI 93.94% (ticket #1 done). The author ran the slice
> (`dotnet run --project src/MonoRpgMaker.Player`) and judged the loop **good** — the make-or-break
> thesis is **validated**. **P0 is greenlit:** build the chassis as an extraction from this slice.
>
> **Update (2026-06-17, ticket #2 — `WORK-m0-chest-give-once`):** the make-or-break task itself —
> the "chest gives one potion, then empty" variant — is now authored as a second hand-written
> authored-event exemplar (`Sim/Tracer/ChestEvent.cs`: give-once via a minimal `GameState` counter +
> switch guard; visual open via a reused `DoorRule`). It **corroborates the GO** — `bin/gate.sh`
> GREEN [full]: 40 tests, coverage 99.0%, mutation MSI 91.30%. A formal chest-vs-RPG-Maker stopwatch
> was not run separately (the holistic GO on the tracer already greenlit P0); the procedure is in the
> pipeline notes (`WORK-m0-chest-give-once.notes.md`) if a formal number is ever wanted.

## P0 — The chassis (extracted from the tracer) + by-construction scaffolding

> **Status (2026-06-18): slices 1–3 + 3b + the sim-determinism analyzer + the shared outcome vocabulary + the outcome-return analyzer + the .expect oracle landed** —
> `MonoRpgMaker.Abstractions` holds the three pure primitives — **`FixedPoint`** (Q16.16, #3),
> **`IRandom` + `SplitMix64Random`** (the seeded integer-only deterministic RNG seam, #4), **`GridPoint`**
> (the pure `(int X, int Y)` coordinate, #5) — and the **event seam** (**`IMapEvent`**, **`EventTrigger`**,
> **`IEventContext`**, #6 / `WORK-p0-eventseam`). The first **Engine→Abstractions consumption edge** is
> live (the tracer consumes the seam via a `Point`↔`GridPoint` adapter; the World/host keep XNA `Point`).
> **The sim-determinism ban analyzer is now live** (#7 / `WORK-p0-sim-determinism-analyzer`): the new
> `MonoRpgMaker.Analyzers` (`netstandard2.0`) `SimDeterminismAnalyzer` (**MRM1001–1005**) makes
> `float`/`double`/`MathF` · `Vector2`-family · `foreach`-over-`Dictionary`/`HashSet` · `System.Random` ·
> `DateTime`/`DateTimeOffset`/`Environment.TickCount` **un-compilable in simulation code** — namespace-scoped
> to `Engine.{World,Entities,Data,Sim}` + `Abstractions`, host/renderer carved out — surfaced as warnings
> that become build **errors under `-warnaserror`**. `FixedPoint.ToDouble` is the sanctioned
> **`[DeterminismExempt]`** boundary (the reusable carve-out marker). **The shared outcome vocabulary is now
> live** (#8 / `WORK-p0-outcome-vocabulary`): a closed sealed-record DU **`Outcome`** in Abstractions
> (**`SetSwitch`** / **`AddCounter`** / **`ShowMessage`**) that map events **return** instead of mutating via
> verbs — `IMapEvent.Run` now returns `IReadOnlyList<Outcome>`, `IEventContext` slimmed to the read verbs
> (`GetSwitch`/`GetCounter`), and an Engine **`OutcomeApplier`** applies them; D-0017's "raw state mutation
> won't type-check" model is realized on the event seam (the tracer migrated, behaviour identical). The DU is
> **additive + closed to the Project layer** (non-public base ctor) so P2's `IEffect`/`ITrait`/… extend the
> *same* vocabulary — one registry, not two. **The outcome-return purity analyzer is now live** (#9 /
> `WORK-p0-outcome-return-analyzer`): **MRM1006** (`OutcomeReturnPurityAnalyzer`, a 2nd analyzer reusing
> `SimScope`) makes a state mutator called from an **outcome-returning handler** (a method returning
> `IReadOnlyList<Outcome>`) **un-compilable** — `GameState.Set`/`Add` carry a new **`[StateMutator]`** marker
> (Abstractions, mirroring `[DeterminismExempt]`); the applier (returns `void`) is never a handler, so it
> applies freely. This **completes the D-0017 analyzer pair** (float/determinism ban #7 + outcome-return now) —
> the return-then-apply model (#8) is now compile-time enforced (v1 marks `GameState.Set`/`Add`; broader
> mutator-paths extend the marker as P2 handler kinds land). **The `.expect` oracle is now live** (#10 /
> `WORK-p0-expect-oracle`): executable correctness contracts as a new **gate #13 / `MRM0006`** — an authored
> `<module>.expect` table (input state → expected `Outcome` rows) is run against the **real** handler and a
> mismatch fails the gate, closing the self-grading gap (§10) for the event handlers (intent is now
> *executable*, authored separately from the implementing agent). The maker tool **`MonoRpgMaker.Editor` is now
> a CLI host** (`Exe`) owning the oracle (`ExpectationParser`/`Runner`/`HandlerRegistry`/`OracleCli`) + is now
> coverage+mutation-gated; `Lever`/`Chest` ship `.expect` tables. `bin/gate.sh` GREEN [full] (**13 gates**):
> coverage **98.0%**, mutation MSI **Engine 90.67% / Abstractions 82.22% / Analyzers 91.18% / Editor 81.08%**;
> the NetArchTest "Project → Abstractions only" ring still holds. **The scaffolder landed** (#11 /
> `WORK-p0-scaffolder`): `monorpg scaffold event <Name>` on the Editor CLI deterministically emits a
> gate-clean-by-construction handler skeleton (sealed `IMapEvent`, `Run → IReadOnlyList<Outcome>`, `// fill:`
> holes) + a co-located `.expect` stub — proven gate-clean (the rendered `.cs` compiles + is MRM-clean, the
> `.expect` parses). **The `$data` map format landed** (#12 / `WORK-p0-map-data`): a JSON `TileMap` `$data`
> format + a pure deterministic `MapSerializer` (STJ source-gen; a total typed `MapLoadResult`; the host owns
> file IO) — v1 is the tile map. **Remaining P0:** placed-events + the database from `$data` + the
> generator/validator DAG (the rest of the by-construction emitters) below.

- `MonoRpgMaker.Abstractions`; the **`FixedPoint` (Q16.16)** primitive as the *only* sim numeric type.
- **Generator/validator split:** a Roslyn generator that does ONLY dumb, syntax-keyed emit and ALWAYS
  emits a compilable (degraded) composition; a separate `monorpg validate` step (over metadata) does
  DAG validation + topo-sort + `contract-manifest.json`, surfacing structured `MRM0001..` errors
  *before* the C# compile. The manifest is the source of truth; a non-generator fallback wiring path is
  first-class and gate-diffed. A measured **<1s incremental edit budget** is a gate.
- **By-construction scaffolding (the aic guarantee-A leverage):** `monorpg scaffold <kind>` = a
  deterministic `{{var}}` emitter mirroring a committed **gate-clean fixture** per seam-kind
  (signatures return the outcome vocabulary so raw state mutation won't type-check; `FixedPoint`/
  `IRandom` wired; a co-located seeded test). Fixtures run as goldens; templates are CI-diffed against
  them; the agent fills only the `// fill:` holes.
- Analyzers: the float / `MathF` / `Vector2` / `foreach`-over-`Dictionary`(+`HashSet`) + `System.Random` /
  `DateTime` determinism ban **landed** as `MonoRpgMaker.Analyzers` (MRM1001–1005, #7); **outcome-return
  purity** **landed** as **MRM1006** (`OutcomeReturnPurityAnalyzer` + the `[StateMutator]` marker, #9) — the
  D-0017 analyzer pair is complete. NetArchTest Project → Abstractions only.

## P1 — Determinism + state spine (+ content/data format)

- `IRandom` / `IDeterministicRng`; fixed-step sim loop + strict sim/render split.
- **Lock the coroutine mechanism = `IEnumerator`-yield / source-generated state machine** (no Task /
  async-await in sim flow — analyzer-enforced).
- The `$data` content format + loader + project/resource manifest.
- `ISaveStateComponent` + versioned state + migration; built-in `$game` components; `RngState`.
- The replay harness wired into the gate — run on **NativeAOT + a second CPU arch (ARM64 + x64)** so
  replay-to-same-hash is portable by construction, not a localhost illusion.

## P2 — Effect spine + spatial sim (+ scene-graph & tilemap)

- `IEffect` / `ITrait` / `IDamageFormula` / `IStateBehavior` + registry + the **shared outcome
  vocabulary** (also the type used by `.expect` rows and scaffold-emitted return signatures) +
  `TraitDomain` fold rules; `StandardDamageFormula` (coefficients as pure data — the zero-AI path).
- The **author-stat + formula-engine** pattern: author formula text → the agent *compiles* it to a
  deterministic `IDamageFormula` module (RPG Architect's feel, AOT-safe).
- Movement (`MovementTick`, `IPassability`, `IPathfinder`, `MoveCommand`, `IMoveBehavior`, vehicles);
  maps (`IMapContext`, lifecycle, cell-transition, region/zone triggers via **LDtk-style typed enums**,
  encounters, atmosphere; **the persistent-spatial-state seam**); the scene-graph + tilemap renderer.

## P3 — Events keystone + dialogue + presentation

- `IEventBehavior` coroutines as **typed callable units** (`ctx.Call(behaviorId, args) → outcome`,
  GB-Studio-style), `EventContext` semantic verbs, the trigger/page model with **Tiled-template stable
  GUID identity**, the `GameState` flag store + `FlagsGen`.
- Dialogue over the same coroutine model; `TextPipeline`; `ITextCode`; `ILocalization`.
- Presentation: `IPresentation` + `PresentationEvent` stream + `IPresenter` / `NullPresenter`; the
  **presenter-extension seam** (`IPresentationLayer`) so new visual layers are additive.
- **Bring the inspector forward:** the replay scrubber + module-DAG view land here as dev tools, not at
  P5.

> **The reference game** takes shape across P2–P3 — the gate-clean worked example the skills/fixtures
> are derived from (cold-start, see [agentic-strategy.md](agentic-strategy.md)).

## P4 — Battle (flagship swap) + runtime UI

- `IBattleSystem` + serializable `BattleState` + `BattleEvent` stream; the default
  `TurnBasedBattleSystem` of swappable, **layerable** peers; consumes the P2 effect registry.
- Prove RPG Architect's **battle-mode matrix** (turn-based / ATB-cooldown / on-map) reachable by seam
  swaps over one `BattleState` BEFORE locking battle contracts.
- Menus/windows via the **Data Sources** binding model; scene stack; `UiRegistry`; `InputActionMap`
  (record a **sub-tick offset** in the `InputFrame` so rhythm-style timing stays replayable).

## P5 — Full inspector / steering + console-readiness

- The complete editor inspector (per-area scrubbers, save inspector, live `[ModuleParam]` tweak); the
  **agent capability benchmark** as a published deliverable.
- AOT/trimming pass; the reflection-ban carve-outs (save-migration reader, content pipeline, STJ source
  generator) proven not to re-enter the sim; final console-determinism review.

## The visual editor track (maps, database, Modules panel)

The map editor (TMX/Wang, regions, named zones, collision) and database editor (the ~16 category grids)
are built against the `$data` schemas as they land (P1–P2), reusing the same JSON the runtime consumes.
The **Modules panel** (the honest successor to MZ's Plugin Manager — a cycle is a red build, not a
silent runtime break) plus the inspector (brought forward to P3) complete the GUI. The old "event
editor" pillar is **replaced** by agentic authoring (D-0011).

**Shipped (first slice):** a Tiled-like **map-paint editor** — `MonoRpgMaker.Studio` (Avalonia; #16) — paints
LPC sprite tiles onto a `$data` map (a `Tileset` sheet model + `MapPaintSession`; New / Save / Load via
`MapSerializer`). **Avalonia is locked as the GUI framework (D-0022)**; the studio is a thin host over the gated
Editor logic. The database editor, Modules panel, and inspector follow.

**Playable runtime (#17):** the **author → save → play loop is closed** — the Player loads a bundled `$data` start
map (embedded, D-0023), renders it with **LPC tileset sprites** (the `Tileset` model moved to `Engine.World`,
shared with the editor), and the player **walks it with tile collision + a following `Camera`**. Paint a map in
the Studio → it's `$data` → the runtime renders and walks it. (Player sprite is a placeholder; walk-cycle
animation + multi-map deferred.)

**Event/trigger foundation (#18, D-0024):** the unified trigger system began — placed events live in `$data`
(`{id, cell, trigger, behaviour{kind, params}}`), a `BehaviourRegistry` is the **single binding point** from a
`kind` to an `IMapEvent` behaviour, and the first **built-in** `ShowText` (no-code dialogue) runs in the playable
map — walk to the sign, press Space → the line shows. Built **contract-first**: every behaviour implements
`IMapEvent` + carries a `.expect` (gate:13), built-in and future agent kinds interchangeable through the one
registry. Next: the built-in library (chest/door/shop/warp) and the agent-authored custom behaviours.

**Event placement in the editor (#19):** the Studio UI to *place* events — the first slice of the **Studio-v2**
arc. An **Event mode** in the map painter: click a tile to place/select an event, an inspector configures its
trigger + kind + params (ShowText) + delete, and Save writes the same `$data` events (#18) the runtime loads +
dispatches. Paint a map → place a sign → Save → run the Player → Space → your line (no more hand-edited JSON). The
kind dropdown is the `BehaviourRegistry`'s **single descriptor** — the editor offers exactly what the runtime
materialises, so they can't drift.

**Tileset picker (#20):** the second Studio-v2 slice — choose among the committed LPC sheets (grass / dirt / water
/ mountains) per map; the choice is recorded as a map-level tileset name in `$data` and **both the editor and the
Player render the chosen sheet**, validated through a single-source `TilesetCatalog` (D-0025). Absent → defaults to
mountains (existing maps unchanged); unknown → a typed load failure.

**Undo/redo (#21):** the third Studio-v2 slice — a command/history spine in the gated `MapPaintSession`: a paint
**stroke**, each event op (place / edit / remove), and the tileset switch are undoable + redoable, with Undo/Redo
toolbar buttons + Ctrl/⌘+Z (Ctrl+Shift+Z / Ctrl+Y for redo). Each undoable op records its inverse (a delta), and a
batched stroke unwinds in reverse; `New`/`Load` reset the history.

**Multi-map + the built-in `Warp` (#22):** the Studio-v2 arc's finale *and* the start of the trigger program's
Slice 2. The runtime now holds **many embedded maps** by id + a `game.json` manifest, and a new gated
**`GameSession`** orchestrator switches the active map on a **`Warp`** event — loading the target, placing the
player at the target cell, and carrying `$game` state across (D-0026). `Warp` is the first cross-map behaviour,
built contract-first like `ShowText` (D-0024): a declarative `Warp` outcome the sim-host applies (D-0017). The
Studio authors the set (a gated `MapProject` map list + a kind-aware, param-key-driven Warp inspector), and saves
record the active map. With this the **Studio-v2 arc is complete** (event placement #19, tileset picker #20,
undo/redo #21, multi-map #22); next is the rest of the built-in library (chest / door / shop / give-item) + the
agent-authored-kind flow.

## Cross-cutting

- All game logic in `MonoRpgMaker.Engine` (framework-thin); sim deterministic (injected `IRandom`,
  `FixedPoint`) and separate from rendering. Authors extend only through `MonoRpgMaker.Abstractions`
  seams; wiring is compile-time, never runtime reflection (scoped to the sim path — D-0021).
- Source of truth = **code-is-truth after first emit**; `intent.md` is a scaffold seed + changelog;
  re-steering is a localized reviewed edit, not a stochastic rewrite (D-0013, amended).
- A module is "done" only when its `.cs`, `$data`, `.expect` rows, and a seeded replay test pass
  `bin/gate.sh`. The product ships this as a scoped `plan → scaffold → author → inspect → gate`
  pipeline.
- Open-source under **MIT** + a no-rug-pull pledge; BYO-API-key; capture lessons + decisions to the
  forge.
