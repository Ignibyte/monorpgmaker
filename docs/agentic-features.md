# Agentic Feature Design (per area)

> How each RPG Maker feature area becomes agentic authoring, from the 10-agent research
> (2026-06-17). **Status: proposed, not yet built.** Cross-cutting mechanics (seams,
> primitives, the substrate) live in [agentic-substrate.md](agentic-substrate.md); the
> authoring skills in [agentic-skills.md](agentic-skills.md); the build order in
> [roadmap.md](roadmap.md). The thesis is [agentic-overview.md](agentic-overview.md).

Each area splits the same way: **what stays UI-authored DATA** (the human paints/fills it,
or the agent fills the same schema), **what the agent authors as C# modules** (behavior,
hooked into seams), and **what the inspector shows** (UI moves up the stack from authoring
to steering a deterministic, replayable sim).

| Area | Priority | The inversion, in one line |
|---|---|---|
| Extensibility + substrate | foundational | The chassis: Drupal-style seams, compile-time wiring, the AI-agnostic toolchain |
| Database → behavior | foundational | Numbers stay in grids; the moment a number must *do* something it crosses a seam into a C# effect/trait/state/formula module |
| Events & world logic | foundational | The ~100-command palette → C# `IEventBehavior` coroutines bound to placed triggers; switches/vars in one serializable store |
| Save / load & state | foundational | Each module persists its own versioned state via a seam + migration chain; determinism makes saves replayable |
| Maps & world | core | Geometry stays painted; behavior reacting to space = modules on spatial seams (cell-transition, region/zone triggers, encounters) |
| Movement & vehicles | core | The deterministic movement tick is core; custom routes/AI/pathing/vehicles are `IMoveBehavior` modules it invokes |
| Battle | core | The flagship swap: ATB/tactics = replacing one seam (scheduler/targeting/AI), not forking; pure replayable state machine |
| Dialogue & text | core | Content is data; *flow* (branching/conditions) is a C# coroutine; text codes are presentation-only modules |
| Menus & in-game UI | core | Skins/layouts/commands are data; custom windows/scenes are modules on the scene stack + input-action seam |
| Audio / animations / VFX | core | Pure presentation: sim emits a typed `PresentationEvent` stream; modules decide *when*; assets/curves are data |

---

## Extensibility + the agentic substrate (keystone)

The chassis every other area plugs into — covered in full in
[agentic-substrate.md](agentic-substrate.md). In short: a Drupal-style
core/Abstractions/Project layering, compile-time wiring via a Roslyn generator emitting a
`contract-manifest.json`, modules that declare identity/deps/order via attributes, and an
AI-agnostic toolchain (CLI + MCP + markdown skills + manifest). **Build this first** — every
other area's authoring loop rides on it.

## Database → behavior `[foundational]`

- **UI data:** every scalar and id-reference — names, param curves, prices, element ids,
  hit/crit rates, state durations; the trait/effect *lists* on a record (pick a module from
  a dropdown + fill its typed params); the damage record (pick a formula + coefficients).
  The author never writes a number in code.
- **Agent authors:** `IEffect` (use-effects), `ITrait` (stat contributors the engine
  aggregates), `IStateBehavior` (apply/tick/remove), `IDamageFormula` — pure functions over
  a context + the record's data, returning declarative outcomes (never in-place mutation).
- **Inspector:** registry browser; the resolved effect pipeline + aggregated trait stack on
  an actor (which source gave which multiplier); a **damage scrubber** (feed stats + seed,
  watch the number, live-edit coefficients).
- **Watch out:** trait fold rules (sum/product/max/or per `TraitDomain`) must be precisely
  specified or agent traits compose nondeterministically; keep a `StandardDamageFormula`
  whose coefficients are pure data so the 90% case stays zero-code. **The shared outcome
  vocabulary must be co-designed with battle — one registry, not two.**

## Events & world logic `[foundational, keystone]`

- **UI data:** trigger *placement + gating* — where a trigger lives (cell/footprint/region),
  its `TriggerKind` (action/touch/autorun/parallel), page conditions (flag predicates), page
  priority, and the *name* of the behavior it binds to. Switch/variable/quest *definitions*
  are database rows.
- **Agent authors:** the page *body* — a C# `IEventBehavior` coroutine calling **semantic**
  services (`ctx.Dialogue.Say`, `ctx.Movement.Walk`, `ctx.Battle.Process`, `ctx.Flags.Set`,
  `ctx.Quest.Advance`). Cutscenes, branching NPCs, quests, parallel logic become readable C#
  control flow, not numbered commands.
- **Inspector:** map overlay of placed triggers + active-page resolution; a per-behavior
  state-machine/step graph; a live flag/quest panel with provenance; a deterministic scrubber.
- **Watch out:** **the #1 risk is re-importing the palette as a fluent API** — `EventContext`
  must expose semantic verbs, branching uses native C#, and an analyzer forbids a method
  mirroring a single MV command. Decide the deterministic coroutine mechanism here (reused by
  dialogue/battle). Keep placed-event identity stable across editor moves (self-switches).

## Save / load & game state `[foundational]`

- **UI data:** save-system *config* only (slot count, autosave policy, header/preview
  metadata, save-allowed-here). No save bytes are UI-authored — the save is pure runtime
  `$game` state.
- **Agent authors:** per module, an `ISaveStateComponent` — a stable `StateKey`, a
  `CurrentVersion`, a serializable state record, `Capture`/`Restore`, and a `Migrate` chain.
  Built-in `$game` state (switches/vars/party/map) are just components too.
- **Inspector:** every registered component with key/version/size/hash; a decoded-save
  tree/diff; a **migration dry-run**; a replay scrubber that flags the first divergent
  component hash.
- **Watch out:** the generator must fail the build if `CurrentVersion` outruns the latest
  migration (the dual-truth-drift guard); `[SaveState]` forbids engine-handle types at
  compile time; prefer integer/fixed-point sim math for console determinism.

## Maps & world structure `[core]`

- **UI data:** the tile grid + Wang flags, the Region layer, a **named Zone table**
  (stable string keys + tags), terrain tags, per-tile collision, and map properties
  (parallax/scroll, BGM/BGS, default weather, encounter table). All painted.
- **Agent authors:** `IMapModule` + spatial triggers — what happens on enter/leave/transfer,
  what a region/zone/terrain step *does* (poison floor, ice slip, gate a door), encounter
  biasing beyond the flat table, parallax/weather state machines. Modules query painted data;
  they never index the tile array.
- **Inspector:** region/zone subscription overlay (coverage gaps); a map-event timeline
  scrubbing Enter→Step→Region→Encounter→Leave; "why did this fire?" attribution.
- **Watch out:** `ICellTransitionHook` is a hot path (every step, every entity) — generated
  indexed dispatch + a player-only-by-default granularity decision; expose Zone-key authoring
  well or agents fall back to magic ints.

## Movement, pathfinding, collision & vehicles `[core]`

- **UI data:** tile passability/attributes (painted); per-event placement, trigger kind,
  priority layer, through flag, default speed/frequency, and the *choice* of which named
  behavior preset to attach; vehicle start positions + water-class; the linear "Set Movement
  Route" as a small `MoveCommand` data list.
- **Agent authors:** `IMoveBehavior` modules returning the next intended `MoveCommand`
  (patrol-then-chase, fleeing AI, flow-field pathing, custom vehicle/follower logic). The
  engine alone owns commit, collision, and interpolation.
- **Inspector:** per-entity cell/interp/facing + which behavior emitted the last command; a
  passability overlay; an A\* open/closed debug layer; scrub/replay to reproduce a desync.
- **Watch out:** purity enforced by a **compile-time** analyzer (an unseeded RNG silently
  desyncs); pick the closed `MoveCommand` primitive set carefully (load-bearing); surface
  pathfinder give-ups in the inspector so they don't read as "stuck event" bugs.

## Battle `[core]`

- **UI data:** Enemies/Troops/Skills/Items/States/Animations database rows + per-battle
  tunables (encounter rate, escape ratio, starting TP). No code.
- **Agent authors:** seam modules — an alternate `ITurnScheduler` (ATB/CTB), custom `IEffect`
  / `IDamageFormula`, an `IBattleAi` policy, an `ITargetSelector` (grid AoE), or a whole
  `IBattleSystem` (action-battle). Battle is a pure state machine `Step`-ing a serializable
  `BattleState` and emitting a `BattleEvent` stream; it consumes the **same effect registry as
  the database**.
- **Inspector:** battle-state visualizer (phase, scheduler queue/ATB gauges, pending actions);
  a `BattleEvent` timeline scrubber; a damage probe; a determinism diff; live param re-sim.
- **Watch out:** **seam granularity is the central risk** — validate the cut by actually
  building ATB-as-scheduler-swap *and* tactics-grid-as-system before locking contracts. Decide
  whether base `BattleState` carries an optional grid layer. Keep time fixed-tick.

## Dialogue, text & messaging `[core]`

- **UI data:** the message database — speaker, face ref, window position/style, the localized
  body (a LocKey + per-locale strings), choice labels. Presentation params (speed, skin) are
  data.
- **Agent authors:** dialogue *flow* as a C# coroutine over the **shared** event coroutine
  model (`ShowText`/`ShowChoices`/`Await…`); custom `ITextCode` modules (presentation-only);
  custom `IMessageView`. Never touches `SpriteBatch`.
- **Inspector:** resolved message queue + current page; expanded text (variables filled); the
  choice tree as a graph; a coroutine-step scrubber; localization-coverage view.
- **Watch out:** awaits must resume on sim steps, not the .NET scheduler (determinism + AOT);
  enforce that `ITextCode` is presentation-only (no smuggled waits/branches); keep the
  literal-string fast path feeling native while backing it with a LocKey. Mutation/non-message
  commands route back to `EventContext`, not the message seam.

## Menus & in-game UI `[core]`

- **UI data:** window skins (9-slice + margins), theme tokens, per-scene layout slots,
  menu-command lists (stable id + text + icon + an *enumerable named predicate* for
  enable/visible), localized strings, which built-in window type fills a slot.
- **Agent authors:** `Scene`/`Window` modules subclassing engine base types — custom
  layouts/`Refresh`, novel selectable windows, scene flow, action handlers that mutate `$game`
  via `IGameStateMutator`. Wired through the source-generated `UiRegistry` with explicit order.
- **Inspector:** live scene-stack tree → child windows → selectable indices; each window's
  binding snapshot; layout-slot overlay; a `(seed, input-action)` replay scrubber; hot-reload
  of theme/layout data.
- **Watch out:** ship a *minimal* core `Window` set (don't pre-build the whole MZ zoo);
  curate the predicate/onOk id set deliberately (the data/code line); windows read only a
  read-only `$game` projection and logical `UiAction` (never raw `Keys`) — analyzer-enforced.

## Audio, animations & visual effects `[core]`

- **UI data:** `AnimationRecord` timelines (frames, hit markers, per-frame SE/flash hooks),
  the audio asset registry, screen-effect/weather presets, picture assets + transforms,
  curves/easings. All id-keyed `$data`.
- **Agent authors:** the *decision logic* — a module hooks a sim seam (`OnSkillHit`,
  `OnHpThreshold`…) and calls `IPresentation.ShowAnimation/PlayMe/ShakeScreen(presetId)`; or a
  `CompositeEffect` sequencing several `PresentationEvent`s with relative offsets. Never writes
  the mixer/interpolator/draw calls.
- **Inspector:** a presentation timeline scrubbing the recorded `PresentationEvent` stream
  (each event's sim-tick origin); mute/solo buses; preview/tweak a preset and re-emit; diff two
  runs' streams.
- **Watch out:** **never let the sim wait on the presenter** ("wait for animation to finish"
  re-couples determinism) — the sim models its own logical wait; `NullPresenter` must yield an
  identical sim trace. No presentation state in `$game` (pictures/weather are pure view).

---

## v2 additions (from the adversarial review)

Two **new additive seam kinds** (D-0019) close the genre-reach gaps the review found (rhythm /
deckbuilder / farming-sim each broke a different boundary):
- **Presenter-extension** (`IPresentationLayer`, A/V + Maps) — a Project registers a *new* visual
  layer driven by its own save-state, so the presenter is no longer a closed wall.
- **Persistent-spatial-state** (Maps) — a per-cell/region store that ticks + persists (farming soil,
  fog-of-war), distinct from transient triggers.

Single-bind services (`IBattleSystem`/`IPresenter`) become **layerable** (ordered decorators), and the
`InputFrame` records a **sub-tick offset** so rhythm-style timing stays replayable.

**Patterns adopted** (RPG Architect + the landscape):
- **Database:** author-defined stats + a **formula engine** — the author writes formula text, the agent
  *compiles* it to a deterministic `IDamageFormula` (RPG Architect's feel, AOT-safe; no runtime eval).
- **Menus:** a **Data Sources** binding model — windows read named domains (battle/inventory/party),
  validated at compile time; never staged through global variables.
- **Events:** `IEventBehavior` as **typed callable units** (`ctx.Call(id, args) → outcome`, GB Studio
  custom events) + **Tiled-template stable GUID identity** for placed events (resolves the
  self-switch-orphan / move-duplicate-renumber problem).
- **Spatial tags:** **LDtk-style typed enums** flow both ways (editor ↔ code) so agents bind to named
  zones/terrain, never magic ints.
- **Save:** per-entity local state with an explicit **persist toggle** (transient vs `$game`).

**Correctness + determinism per area:** every behavior module now ships a `<module>.expect` table
(gate #13 / `MRM0006`) and uses `FixedPoint` sim math (D-0016/D-0017).
