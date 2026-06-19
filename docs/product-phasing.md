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

4. **Unified stateful entity (direction, not yet decided).** Everything — player, NPC, enemy, the
   chest, the item in your bag, a quest token — is one **stateful `Entity`** on a single composition
   surface, so the agent writes *one* kind of behavior module and attaches it to anything. Two open
   forks (see below) must be resolved before this is real.

5. **Determinism by construction is the *enabler*, not a tax.** A world where everything is mutable
   + stateful is only debuggable if it's **bit-for-bit replayable**; that is now enforced (FixedPoint,
   seeded `IRandom`, the analyzers, outcomes-not-mutation). (D-0016 / D-0017)

6. **Correctness is human-accepted, per module.** The agent authors deterministic, well-formed logic;
   the human **semantically accepts** it via the `.expect` fill-in grid (gate #13). No self-grading.
   (D-0017 §7 — ✅ landed for the event handlers.)

### Open architecture forks (must decide before the unified-entity layer)
- **Inheritance → composition.** Today `Entity` bakes grid placement into the base (`Cell` / `TryStep`,
  "lives on the map grid"). For a bag item (no cell) to be an entity, "placed on grid," "has stats,"
  "movable," "holds inventory" become **attachable capabilities/behaviors**, not base-class fields.
- **Definition / archetype vs instance.** "Potion" the type = shared **immutable database record**;
  "the potion in slot 3" = a **stateful entity instance** that *references* it (flyweight). The editor
  authors *definitions*; the running game spawns *stateful instances*; modules hook the instances.
- **Tiles stay data (probably).** Thousands of tiles per map ⇒ the tilemap stays the lightweight
  spatial substrate, not entities — going full ECS/data-oriented is a perf call we defer.

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
| Tile map (grid; tile = tileset id + collision flag) | 🟡 | one hand-built room; no multi-map, layers, or transitions |
| Tile/sprite rendering | 🟡 | flat colored rectangles only — no real tileset/sprite images, no camera scroll |
| Player movement (4-dir, collision vs tiles + doors) | ✅ | arrow keys; works |
| Triggers | 🟡 | only `StepOn`; no action-button / autorun / parallel; no *authored* event placement data |
| Event behavior (what an event runs) | 🟡 | 2 hand-coded examples (lever, chest); not yet agent-authored / scaffolded |
| Doors (switch-driven passability) | ✅ | the lever→door demo |
| Switches + variables (`GameState`) | 🟡 | named bool switches + int counters; **no persistence** (resets each run) |
| Messages | 🟡 | single "current message" banner; no message box, queue, choices, or portraits |
| Database (id-keyed `Database<T>`; `Item`/`Actor` records) | 🟡 | type + record shapes exist; empty + not wired into the map |
| Actors / combat | 🟡 | `Actor` carries HP + "defeated"; no combat, enemies, party, stats, or leveling |

### D. Extensibility & entity model  (the discussions — mostly direction)
| Capability | Status | Notes |
|---|---|---|
| Behavior = agent-authored modules hooked into seams | 🟡 | the event seam proves the pattern; the general module/hook registry + tooling is embryonic |
| Additive / layerable outcome vocabulary (one registry, not two) | 🟡 | the `Outcome` DU is additive (#8); P2 extends it with combat/effect cases |
| Hook lifecycle (ordered subscribers; ambiguity = build error) | 🔒 | designed (agentic-substrate §2); not built |
| Trait / effect spine (`IEffect` / `ITrait` / `IStateBehavior` / `IDamageFormula`) | ⬜ | P2 — "items extended to have other meanings" |
| **Unified stateful entity** (everything = entity; composition; definition↔instance) | ⬜ | open direction — resolve the two forks above first; `Entity` base today is on-grid only |
| Base-state-first / no-code baseline | 🔒 | principle (generator-always-compilable + visual surfaces); not yet deliverable |

### E. Editor / UI surfaces  (kept where direct manipulation wins)
| Capability | Status | Notes |
|---|---|---|
| Headless map-edit ops (`MapEditor` paint/clear) | ✅ | unit + mutation tested |
| Visual **map paint** editor (MonoGame/Avalonia front-end) | ⬜ | planned (technical-architecture); a later phase, after the runtime proves out |
| Visual **database** editor (items/actors/skills/enemies) | ⬜ | planned |
| Event **placement + trigger** UI (light-UI; Tiled-template GUID identity) | ⬜ | planned; *what it runs* stays agentic |
| `.expect` **semantic-acceptance grid** (human fill-in) | 🟡 | the contract + runner exist (#10); authoring is manual text today, a grid later |
| Direct/steer surface (prompt → inspect → replay the sim) | ⬜ | |

### F. Platform & polish
| Capability | Status | Notes |
|---|---|---|
| Desktop (Windows / macOS / Linux, MonoGame DesktopGL) | ✅ | the Player runs |
| Save / load (serialized `$game` state, RNG state, migrations) | ⬜ | the RNG state is already capturable; no save format yet |
| Audio | ⬜ | |
| Menus / inventory / party UI | ⬜ | |
| Console targets + AOT / multi-arch replay-hash gate | ⬜ | the determinism foundation is the precondition |

---

## Next 4–5 tickets (proposed game plan)

The arc: **enabler → foundation → extensibility → entity → persistence.** Each builds on the last
and moves toward the "stateful entities + agent-authored modules" vision. Only #11 is the clear
locked next; the rest is a proposed order the user can resequence.

1. **#11 — Scaffolder (`monorpg scaffold event <Name>`). ✅ DONE.** A deterministic
   `{{var}}` emitter that mirrors a committed gate-clean fixture → an event-handler skeleton (returns
   `IReadOnlyList<Outcome>`, reads the context, `// fill:` holes) + a co-located `.expect` stub. Makes
   new handlers cheap + correct-by-construction — the "AI builds the shape, you fill the thinking"
   leverage. Reuses #7–#10; adds a `scaffold` verb to the `monorpg` CLI.

2. **#12 — Content / `$data` format + map loading.** *Foundation; medium–large.* A serializable map +
   database format; load a map (its tiles, placed events, and the database) from data instead of the
   hand-wired `TracerRoom`. Unblocks multiple maps + transitions, the visual editor, and save/load.

3. **#13 — Trigger kinds + the ordered hook lifecycle.** *Extensibility spine; medium.* Expand
   `EventTrigger` beyond `StepOn` (action-button / autorun / parallel); introduce ordered-subscriber
   hook dispatch where an ordering ambiguity / cycle is a **build error** (`MRM0001–0003`) — Drupal's
   extensibility without its ordering hell.

4. **#14 — Unified-entity model: AD + first cut.** *The big one; large, design-gated.* Write the
   architecture decision first (composition over inheritance · definition↔instance · capabilities as
   attachable behaviors), then refactor `Entity`: an instance references a definition + carries state
   + attached capabilities, and NPCs **and items** become first-class entities. **AD before code.**

5. **#15 — Save / load + the stateful spine.** *Proves it; medium.* Serialize `$game` state (switches,
   counters, entity-instance state, the already-capturable RNG state) and load it back deterministically;
   a save format + a migration reader. Demonstrates "stateful across the board," end-to-end.

**Stretch / parallel:** the **visual map-paint editor** once #12 lands; the **trait/effect spine**
(`IEffect` / `ITrait` / `IStateBehavior`) once #14 lands; the AOT / multi-arch replay-hash gate.
