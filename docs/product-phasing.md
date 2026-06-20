# Product Vision & Phasing

A single "here's what we want to do + is it done yet" tracker. It captures the **direction
discussions** (the feel/principles) and a **phased capability status**. It pairs with:
- [`roadmap.md`](roadmap.md) — the chassis slices in detail (P0/P1/P2…).
- [`decisions.md`](decisions.md) — the binding decisions (D-00xx) referenced below.
- [`game-overview.md`](game-overview.md) / [`agentic-substrate.md`](agentic-substrate.md) — the full thesis.

This file is the *map of intent*; the slice-by-slice "how" lives in the pipeline docs under
`docs/planning/`. Keep the status column honest — update it when a capability lands.

**Status legend:** ✅ done · 🟡 partial (M0 / embryonic) · 🔒 decided, not built · ⬜ not started / open direction

---

## Governing principles (what we want it to *feel* like)

1. **UI where direct manipulation wins; AI for behavior.** Visual surfaces are kept only for
   spatial maps and tabular data; the event/command "programming in a UI trenchcoat" is replaced
   by prompting an agent to write C# modules. Humans do the *spatial / tabular / taste* work and
   *direct*; the agent writes *what things do*. (D-0011)

2. **Drupal-style extensibility — base-state-first.** Core entities ship a *working base state*;
   code **modules hook into the event lifecycle** to extend / re-mean them and add functionality —
   you layer on top, you don't fight the defaults. A power user who only wants the bare-minimum UI
   setup gets a functioning, no-code baseline; code is opt-in layering. (D-0019 layerable services;
   the generator "always emits a compilable, *degraded* composition" = always a working baseline.)

3. **Drupal's power *without* Drupal's hook-ordering hell.** Hooks fan out to **ordered**
   subscribers; ordering ambiguity / a cycle / a missing dep is a **build error** (`MRM0001`–`0005`),
   topologically sorted — never silent last-wins at runtime. (agentic-substrate §2)

4. **Unified stateful entity (✅ decided #14 — composition + definition/instance).** Everything — player, NPC, enemy, the
   chest, the item in your bag, a quest token — is one **stateful `Entity`** on a single composition
   surface, so the agent writes *one* kind of behavior module and attaches it to anything. Two open
   forks (see below) must be resolved before this is real.

5. **Determinism by construction is the *enabler*, not a tax.** A world where everything is mutable
   + stateful is only debuggable if it's **bit-for-bit replayable**; that is now enforced (FixedPoint,
   seeded `IRandom`, the analyzers, outcomes-not-mutation). (D-0016 / D-0017)

6. **Correctness is human-accepted, per module.** The agent authors deterministic, well-formed logic;
   the human **semantically accepts** it via the `.expect` fill-in grid (gate #13). No self-grading.
   (D-0017 §7 — ✅ landed for the event handlers.)

### Open architecture forks — ✅ RESOLVED by #14 (`AD-claude-unified-entity-model-001`)
- **Inheritance → composition. ✅ DECIDED: composition.** Capabilities ("placed on grid," "has stats,"
  "movable," "holds inventory") are **attachable `IComponent`s**, not base-class fields, so a bag item (no
  cell) can be an entity. (v1: `IComponent` + `StatsComponent`/`PositionComponent`.)
- **Definition / archetype vs instance. ✅ DECIDED: flyweight.** An immutable `EntityDefinition : IRecord`
  (the editor-authored template) + a stateful `EntityInstance` that *references* it by id and carries the
  per-instance component state (what #15 save/load serializes). Modules hook instances.
- **Tiles stay data. ✅ AFFIRMED.** The tilemap stays the lightweight spatial substrate, not entities —
  full ECS/data-oriented stays deferred (a perf call).

---

## Phased capability status

### A. Determinism & correctness chassis  (the substrate — largely ✅)
| Capability | Status | Notes |
|---|---|---|
| `FixedPoint` (Q16.16) — the only sim numeric type | ✅ | #3 |
| `IRandom` + `SplitMix64Random` — seeded, reproducible RNG | ✅ | #4 |
| `GridPoint` — pure integer coordinate | ✅ | #5 |
| Event seam (`IMapEvent` / `IEventContext` / `EventTrigger`) | ✅ | #6 |
| Sim-determinism analyzer (`MRM1001–1005`: float / Vector2 / Dict-foreach / Random / clocks) | ✅ | #7 |
| Outcome vocabulary — events **return** declarative outcomes (`SetSwitch`/`AddCounter`/`ShowMessage`) | ✅ | #8 |
| Outcome-return purity analyzer (`MRM1006`) | ✅ | #9 |
| `.expect` oracle — executable correctness contracts, gate #13 / `MRM0006` | ✅ | #10 |
| The gate — 13 checks, no baselines, source-fix only | ✅ | |

### B. Authoring substrate  (the AI-leverage — next)
| Capability | Status | Notes |
|---|---|---|
| `monorpg` CLI host | 🟡 | exists as the Editor Exe (the `check-expectations` verb, #10); more verbs to come |
| Scaffolder (`monorpg scaffold event <Name>`) — gate-clean-by-construction skeleton + `.expect` stub | ✅ | #11 — emits a sealed `IMapEvent` + `.expect` stub; the rendered `.cs` compiles + is MRM-clean (proven). v1 = the `event` kind |
| Generator (dumb syntax-keyed emit; always a compilable degraded composition) | 🔒 | D-0018; premature until there are multi-module compositions |
| Validator (`monorpg validate` — DAG, topo-sort, `contract-manifest.json`, `MRM0001–0005`) | 🔒 | D-0018; premature until there's a module dependency graph |
| Skills/fixtures derived 1:1 from gate-clean goldens | ⬜ | |

### C. Runtime gameplay  (M0 "tracer" — embryonic)
| Capability | Status | Notes |
|---|---|---|
| Tile map (grid; tile = tileset id + collision flag) | 🟡 | the runtime now holds **many embedded maps** by id + a `game.json` manifest (#22, D-0026) with **`Warp` transitions** between them (a `GameSession` switch carrying `$game` state); layers / autotiles still deferred |
| Tile/sprite rendering | 🟡 | **LPC tileset sprites + a following camera** (#17); the active map's tileset sheet now **reloads on a warp** (#23, D-0022/D-0025 — the host signals, `Game1` reloads through the catalog) so each map renders with its own sheet (start⇄town = `lpc-mountains`⇄`lpc-grass`); the player is a placeholder quad — no walk-cycle animation yet |
| Player movement (4-dir, collision vs tiles + doors) | ✅ | arrow keys; works |
| Triggers | 🟡 | #13 `ActionButton`/`PressAction`; **#18 — placed events are now `$data`** (`{id, cell, trigger, behaviour{kind, params}}`, total deserialize) the runtime materialises + dispatches; autorun / parallel still deferred |
| Event behavior (what an event runs) | 🟡 | a contract-first **`BehaviourRegistry`** binds `kind`→`IMapEvent` (#18, D-0024 — the single binding point); the first built-ins **`ShowText`** (no-code dialogue) + **`Warp`** (#22 — the map-to-map transition, the first cross-map behaviour, D-0026), all gate:13-validated; the library now adds **`Chest`** (a give-once container) + **`GiveItem`** (#25 — items as `GameState` counters keyed `item.<id>`), placeable via the kind-aware inspector, each via the contract-first recipe; #26 adds **`Lever`** + **doors** (switch-driven door tiles from `$data`, opened by a lever); only **shop** (a buy/sell UI) remains, then the agent flow |
| Doors (switch-driven passability) | ✅ | the lever→door demo |
| Switches + variables (`GameState`) | 🟡 | named bool switches + int counters; now **persisted** via in-game save/load (#24 — F5 saves, F9 restores the active map + `$game` state + player position) |
| Messages | 🟡 | single "current message" banner; no message box, queue, choices, or portraits |
| Database (id-keyed `Database<T>`; `Item`/`Actor` records) | 🟡 | type + record shapes exist; empty + not wired into the map |
| Actors / combat | 🟡 | `Actor` carries HP + "defeated"; no combat, enemies, party, stats, or leveling |

### D. Extensibility & entity model  (the discussions — mostly direction)
| Capability | Status | Notes |
|---|---|---|
| Behavior = agent-authored modules hooked into seams | 🟡 | the event seam proves the pattern; the general module/hook registry + tooling is embryonic |
| Additive / layerable outcome vocabulary (one registry, not two) | 🟡 | the `Outcome` DU is additive (#8); P2 extends it with combat/effect cases |
| Hook lifecycle (ordered subscribers; ambiguity = build error) | ✅ | #13 — `HookSchedule`: ascending-`Order` per (cell,trigger); equal order = a typed ambiguity error at `WorldSim.TryCreate`. The module-registration layer is still future |
| Trait / effect spine (`IEffect` / `ITrait` / `IStateBehavior` / `IDamageFormula`) | ⬜ | P2 — "items extended to have other meanings" |
| **Unified stateful entity** (everything = entity; composition; definition↔instance) | 🟡 | #14 — first cut: `EntityInstance` (immutable/copy-on-write) + `IComponent` + `EntityDefinition`; forks resolved (`AD-claude-unified-entity-model-001`). Migrating the player/events + retiring the legacy on-grid `Entity` base is deferred |
| Base-state-first / no-code baseline | 🔒 | principle (generator-always-compilable + visual surfaces); not yet deliverable |

### E. Editor / UI surfaces  (kept where direct manipulation wins)
| Capability | Status | Notes |
|---|---|---|
| Headless map-edit ops (`MapEditor` paint/clear) | ✅ | unit + mutation tested |
| Visual **map paint** editor (Avalonia — `MonoRpgMaker.Studio`) | 🟡 | first cut (#16): Tiled-like LPC sprite painting + New/Save/Load `$data`; **the Studio-v2 arc** added event placement (#19), a **per-map tileset picker** (#20 — choose among the 4 LPC sheets, recorded in `$data` + rendered by editor AND Player via a single-source `TilesetCatalog`, D-0025), **undo/redo** (#21 — a command/history spine; paint strokes + event/tileset ops, Undo/Redo buttons + Ctrl/⌘+Z), and **multi-map authoring** (#22 — a gated `MapProject` map set + a kind-aware, param-key-driven Warp inspector; build many maps + place `Warp` transitions, D-0026). The **Studio-v2 arc is complete**; autotiles / layers remain. Avalonia locked (D-0022) |
| Visual **database** editor (items/actors/skills/enemies) | ⬜ | planned |
| Event **placement + trigger** UI (light-UI; Tiled-template GUID identity) | 🟡 | first cut (#19): an **Event mode** in the painter — click a tile → place/select an event; an inspector sets trigger + kind + params (ShowText) + delete → writes the #18 `$data` events the runtime loads + dispatches. The kind dropdown is the `BehaviourRegistry`'s **single descriptor** (the editor offers exactly what the runtime materialises — no drift). Closes author→place→play in the UI. The inspector is **kind-aware** (#22): it renders a field per the selected kind's registry param-keys — a `Warp` event edits a target-map dropdown + cell, a `ShowText` event a text box (single-source, can't drift). More built-in kinds + the agent-authored-kind flow follow; *custom* behaviour stays agentic |
| `.expect` **semantic-acceptance grid** (human fill-in) | 🟡 | the contract + runner exist (#10); authoring is manual text today, a grid later |
| Direct/steer surface (prompt → inspect → replay the sim) | ⬜ | |

### F. Platform & polish
| Capability | Status | Notes |
|---|---|---|
| Desktop (Windows / macOS / Linux, MonoGame DesktopGL) | ✅ | the Player runs |
| Save / load (serialized `$game` state, RNG state, migrations) | 🟡 | #15 — a deterministic, total `SaveState` (switches/counters + player position/facing + RNG state) via STJ source-gen; round-trip + **replay-equivalent** vs the tracer. `EntityInstance`/`$data` serialization + the file UI + migration logic deferred |
| Audio | ⬜ | |
| Menus / inventory / party UI | ⬜ | |
| Console targets + AOT / multi-arch replay-hash gate | ⬜ | the determinism foundation is the precondition |

---

## Next 4–5 tickets (proposed game plan)

The arc: **enabler → foundation → extensibility → entity → persistence.** Each builds on the last
and moves toward the "stateful entities + agent-authored modules" vision. **✅ #11–#15 are all DONE +
merged** — the P0 chassis arc is complete; the follow-ups noted under each are the next candidates.

1. **#11 — Scaffolder (`monorpg scaffold event <Name>`). ✅ DONE.** A deterministic
   `{{var}}` emitter that mirrors a committed gate-clean fixture → an event-handler skeleton (returns
   `IReadOnlyList<Outcome>`, reads the context, `// fill:` holes) + a co-located `.expect` stub. Makes
   new handlers cheap + correct-by-construction — the "AI builds the shape, you fill the thinking"
   leverage. Reuses #7–#10; adds a `scaffold` verb to the `monorpg` CLI.

2. **#12 — Content / `$data` format + map loading. ✅ DONE (v1: the tile map).** A serializable JSON
   `$data` `TileMap` format + a pure deterministic `MapSerializer` in `Engine.World` (STJ source-gen; a
   total typed `MapLoadResult` that never throws on parse; int/bool ⇒ replay bit-identical) + a committed
   sample; proven by round-trip (incl. the real `TracerRoom` map) + malformed→typed-error + sample-load. The
   host owns file IO. **Deferred follow-ups:** placed-events + the database from `$data`; the `TracerRoom`
   runtime rewire to load its map from `$data`.

3. **#13 — Trigger kinds + the ordered hook lifecycle. ✅ DONE (v1).** Added `ActionButton` +
   `WorldSim.PressAction` (faced cell) and an ordered, ambiguity-checked dispatch (`HookSchedule`:
   ascending-`Order` per (cell,trigger); equal order = a typed ambiguity error at `WorldSim.TryCreate`;
   `IMapEvent.Order` is a non-breaking default-interface member; replay stays bit-identical) — Drupal's
   extensibility without its ordering hell. **Deferred:** autorun / parallel triggers; the module-registration
   system.

4. **#14 — Unified-entity model: AD + first cut. ✅ DONE (AD + core).** Wrote the AD
   (`AD-claude-unified-entity-model-001`: composition over inheritance · definition↔instance flyweight ·
   tiles stay data) + a minimal first cut: `EntityInstance` (immutable/copy-on-write, a deterministic typed
   `IComponent` set) + `EntityDefinition : IRecord` + `Stats`/`Position` components. **Deferred:** migrating the
   player/`Actor`/events into `EntityInstance` + retiring the legacy on-grid `Entity` base.

5. **#15 — Save / load + the stateful spine. ✅ DONE.** A deterministic, total `SaveState` (switches,
   counters, player position/facing, the captured RNG state) via STJ source-gen + a total typed
   `SaveLoadResult`; proven round-trip + byte-deterministic + **replay-equivalent** against the tracer (a
   saved $game state restored into a fresh sim re-opens the door). **Deferred:** entity-instance/`$data`
   serialization; the save-slot/file UI; migration logic. The determinism foundation pays off, end-to-end.

**Stretch / parallel:** the **visual map-paint editor** once #12 lands; the **trait/effect spine**
(`IEffect` / `ITrait` / `IStateBehavior`) once #14 lands; the AOT / multi-arch replay-hash gate.
