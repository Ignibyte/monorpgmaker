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

> **Status (2026-06-18): slices 1–3 of N landed** — `MonoRpgMaker.Abstractions` now holds the three
> pure primitives: **`FixedPoint`** (Q16.16, ticket #3 / `WORK-p0-abstractions-fixedpoint`),
> **`IRandom` + `SplitMix64Random`** (the seeded integer-only deterministic RNG seam, #4 /
> `WORK-p0-irandom-rng`), and **`GridPoint`** (the pure `(int X, int Y)` coordinate the seam adopts
> instead of XNA `Point`, #5 / `WORK-p0-gridpoint`). `bin/gate.sh` GREEN [full]: coverage 99.2%,
> mutation MSI Engine 91.30% / Abstractions 82.22% (GridPoint's killable mutants restored margin over
> the floor; the residual survivors are documented equivalents). The mutation gate floors MSI on
> **every** production project, and the NetArchTest "Project → Abstractions only" ring is live.
> **Immediate next — slice 3b (the event-seam extraction proper):** move `IMapEvent`/`EventTrigger`
> into Abstractions, abstract `EventContext` as `IEventContext`, add the `Point`↔`GridPoint` adapter +
> the Engine→Abstractions edge, and migrate the tracer (`LeverEvent`/`ChestEvent`/`WorldSim`/
> `TracerRoom`) + tests (the ~13-file `Point` migration `GridPoint` was built to unblock). **Then:** the
> sim float/`MathF`/`Vector2`/`foreach`-over-`Dictionary` ban analyzer (+ its sim/host scoping), and the
> generator/validator/scaffolding below.

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
- Analyzers: outcome-return purity + the float / `MathF` / `Vector2` / `foreach`-over-`Dictionary` ban
  in sim code (alongside the existing `System.Random` / `DateTime` ban). NetArchTest Project →
  Abstractions only.

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
