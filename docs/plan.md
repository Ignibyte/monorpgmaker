# Execution Plan — Beginning to End

> The master map: the full arc from nothing-playable to a shipped, sustainable open-source 2D RPG maker
> on MonoGame, for the **technical hobbyist**. Engine-build detail is in [roadmap.md](roadmap.md); the
> *why* in [agentic-overview.md](agentic-overview.md); positioning in
> [agentic-strategy.md](agentic-strategy.md); locked decisions in [decisions.md](decisions.md). Status:
> proposed 2026-06-17.

## The shape

Two things run at once: a **milestone sequence** (M0→M6, each gated by an exit criterion) and five
**continuous workstreams** that thread through them. A milestone is "done" only when its exit is met —
and **M0's exit is a literal GO/NO-GO on the whole thesis.**

**Workstreams** (always live, weighted differently per milestone):

| Workstream | What it is |
|---|---|
| **Engine** | the runtime, seams, generator, determinism (roadmap P-(-1)…P5) |
| **Tooling** | the `monorpg` CLI, scaffolders, gate-clean fixtures, the gate, the skills |
| **Editor** | maps + database (visual) and the inspector (steering) |
| **Content & Assets** | your tile assets → the tracer map → the reference game → the curated bundle |
| **Strategy & Community** | license / governance / funding, docs, the capability matrix, the module store |

**Critical path:** tracer (M0) → chassis (M1) → effect/event spine (M2) → **the reference game** (M3 —
the cold-start gate; the moat-skills are *derived from* it) → editor/inspector (M4) → launch (M5) →
console (M6).

## Milestones

### M0 — Tracer & decision  ·  *≈ roadmap P-(-1)*
- **Goal:** prove AI-directed C# authoring beats the click-path and *feels good* — or falsify it cheaply.
- **Active:** Engine (hand-wired), Content (paint the demo map with **your tiles** — first real art).
- **Deliverables:** painted map + move/collide + one authored event (walk-on → message → switch → door)
  as plain C# under a relaxed prototype gate; injected `IRandom`; a demo GIF; a draft README aimed at
  the technical hobbyist.
- **Exit (GO/NO-GO):** the "chest gives one potion, then empty" task is within ~3× of RPG Maker's
  click-path and feels good. NO-GO → retarget or rethink *before* spending the toolchain.
- **Retires:** the existential thesis risk (PR-claude-tracer-001).

### M1 — The chassis  ·  *≈ roadmap P0/P1*
- **Goal:** turn the tracer into the real, gate-clean substrate.
- **Active:** Engine + Tooling (heavy).
- **Deliverables:** `Abstractions` + `FixedPoint`; the split generator + `monorpg validate` + manifest +
  fallback wiring; the determinism spine (fixed-point, `IEnumerator` coroutine, replay harness on
  NativeAOT + a 2nd arch); `ISaveStateComponent`; the `$data` format + loader; **by-construction
  scaffolding** (`{{var}}` emitters + fixtures + analyzers); `.expect` / gate #13; the NetArchTest ring;
  the `monorpg` CLI v1.
- **Exit:** the M0 event re-expressed as a gate-clean module *via the scaffolder*; full `bin/gate.sh`
  green incl. AOT/multi-arch replay; the <1s incremental edit budget met.
- **Retires:** determinism-localhost-illusion, generator-SPOF, scaffolding-gap.

### M2 — Core systems as modules  ·  *≈ roadmap P2/P3*
- **Goal:** the systems a real game needs, authored agentically.
- **Active:** Engine + Tooling + Editor (begins) + Content (begins).
- **Deliverables:** the effect/trait/state/formula registry + shared outcome vocabulary + TraitDomain;
  movement + maps (persistent-spatial, LDtk-style enums, encounters); the events keystone (typed callable
  behaviors, Tiled-GUID identity) + dialogue + presentation (presenter-extension); the scene-graph +
  tilemap renderer; the map editor (paint with your assets) + database editor begun; the first skills
  *derived from real fixtures*; inspector v0 (scrubber + module DAG).
- **Exit:** a playable vertical slice (walk → event → dialogue → save/load) on real assets, fully agentic.

### M3 — Battle + the reference game  ·  *≈ roadmap P4*
- **Goal:** a complete small game + the flagship swappable battle — the worked example the moat rests on.
- **Active:** Engine + Content (heavy) + Editor.
- **Deliverables:** `IBattleSystem` + default turn-based; prove the battle-mode matrix (ATB / on-map) by
  seam swap; runtime UI (Data Sources binding, menus, scene stack); **the complete reference game** (a
  few maps, a quest, a battle) using your assets + curated CC0; the database editor + Modules panel; the
  published agent capability matrix; the curated asset bundle (RTP-equivalent).
- **Exit:** the reference game is complete, gate-clean, and shippable on desktop; skills cover every area
  (because they're derived from it).
- **Retires:** cold-start (no reference game / no skills / no assets).

### M4 — Editor, inspector & polish  ·  *≈ roadmap P5 (editor)*
- **Goal:** a usable end-to-end product.
- **Active:** Editor (heavy) + Strategy.
- **Deliverables:** the full inspector/steering (per-area scrubbers, save inspector, live `[ModuleParam]`
  tweak); project management + playtest launch; the GUI front-end (Avalonia or MonoGame); tutorials + the
  BYO-API-key flow; the no-rug-pull pledge live.
- **Exit:** a new user installs, points at assets, paints a map, directs an agent to build a battle/event,
  inspects/steers it, and playtests — on desktop, start to finish.

### M5 — Open-source launch & community
- **Goal:** ship to the world, sustainably.
- **Active:** Strategy (heavy) + Engine/Tooling (support).
- **Deliverables:** the **MIT** repo public; GitHub Sponsors / Open Collective + published use-of-funds;
  foundation-shaped governance seed (bus factor); the **verifiable module/asset store** (dual-truth
  bundles indexed by the manifest); the contributor on-ramp (the tracer + reference game as the worked
  examples).
- **Exit:** external contributors landing gate-clean modules; first community games in progress.

### M6 — Console shipping  ·  *≈ roadmap P5 (console); the whole reason for MonoGame*
- **Goal:** ship a game to console from the one codebase.
- **Active:** Engine (heavy).
- **Deliverables:** validate against the console back-ends (registered-developer programs); the
  `gamepadMapper`; per-platform storage/save; the content pipeline; certification (TRC/TCR/lotcheck). The
  fixed-point + AOT + scoped-reflection discipline from M1 is what makes this tractable.
- **Exit:** the reference game (or a partner game) shipped to ≥1 console.

### Post-1.0 — Genre reach & ecosystem
New seam kinds for new genres (the genre-envelope semver, D-0019); a thriving module/asset store; more
reference games; community-authored skills.

## Where your tile assets fit

Your assets are on the **critical path twice**: they paint the **M0 tracer map** (first real art, so the
demo isn't programmer-art) and they're the visual base of the **M3 reference game** (the worked example
the moat-skills are derived from) plus the curated starter bundle. To wire them in I need: **where** they
live (dev box `/srv/stacks`? local?), the **format** (Tiled/TMX tilesets, RPG Maker A1–A5/B–E sheets, raw
PNG + tile size, an atlas?), and the **license** (so bundled ones are redistributable). That also informs
the `$data` map schema designed in M1/M2.

## First action

Start **M0 — the tracer bullet** through the pipeline (`/work`). It's small, it's the highest-leverage
thing we can do, and it converts the whole design from "plausible on paper" to "validated or falsified"
in a few weeks.
