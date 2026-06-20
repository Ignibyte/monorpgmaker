# Decisions

A running log of locked architecture decisions (ADRs). Newest first. Mirror
significant ones into the forge knowledge store via `architecture-decision-record`.
Decisions D-0005…D-0010 are grounded in [feature-research.md](feature-research.md).
Decisions D-0011…D-0014 (2026-06-17) reframe the *authoring model* toward agentic C#
authoring — see [agentic-overview.md](agentic-overview.md); they supersede D-0007 and
invert D-0008 while keeping the engine / data / map foundations.
Decisions D-0015…D-0020 (2026-06-17, v2) HARDEN the design after an adversarial review
(tracer-bullet-first sequencing, determinism by construction, by-construction scaffolding +
executable correctness contracts, the generator/validator split, additive seam kinds, and the
open-source positioning) — see [agentic-substrate.md](agentic-substrate.md) and
[agentic-strategy.md](agentic-strategy.md).
Decision D-0021 (2026-06-17) sets the bundled-asset license policy.
Decision D-0022 (2026-06-19) locks the maker-tool GUI framework (Avalonia).
Decision D-0023 (2026-06-19) locks the game-distribution model (the repo is the game).
Decision D-0024 (2026-06-20) locks the unified trigger/event model (contract-first, one repeatable pattern).
Decision D-0025 (2026-06-20) locks the per-map tileset reference (a `$data` name + a single-source catalog).
Decision D-0026 (2026-06-20) locks the multi-map model (embedded per-map `$data` by id + a manifest; a `GameSession` applies the `Warp` switch).

---

## D-0026 — Multi-map: embedded per-map `$data` by id + a manifest; a `GameSession` applies the `Warp` switch

**Decision:** A game holds **many maps** as embedded per-map `$data` (`content/maps/<id>.json`) addressed by a
stable **id**, plus an embedded **`game.json` manifest** `{ startMap, mapIds[] }` — console-safe bundled content
(D-0023; no roamed paths, no runtime project loader). A new gated **`GameSession`** orchestrator (`Engine.Sim`)
above `WorldSim` owns the `id → $data` registry + the active `WorldSim` and **applies** a map switch: on a pending
`Warp` it loads the target by id, builds a fresh `WorldSim`, places the player at the target cell, and **carries
the `GameState`** across (switches/counters persist — `WorldSim`'s `GameState` is now injectable). The built-in
**`Warp`** behaviour only **returns** a declarative `Warp(MapId, Cell)` outcome (D-0017); the `OutcomeApplier`
signals a pending warp and the `GameSession` (the sim-host) performs the switch — the behaviour never mutates.
Load-by-id is **total** (unknown id / off-map target cell / malformed `$data` / unmaterialisable events → a typed
failure or safe no-op, never a throw). Save records the active **`MapId`** (absent → the start map). The Studio
authors the set through a gated, neutral **`MapProject`** (a map list + the kind-aware, param-key-driven Warp
inspector reading the registry's `BehaviourKindInfo.ParamKeys`), and the saved set is runtime-valid (cross-layer).

**Why:** A one-map runtime isn't a world. Per-map files + a manifest mirror the existing single `start.json` and
scale (each map already names its own tileset, D-0025) without a one-file bottleneck. Putting the switch in a
sim-host orchestrator — not in `WorldSim` (immutable per-map) and not in the behaviour (D-0017 forbids it) — keeps
the behaviour pure and the simulation deterministic; `Warp` is just the first cross-map behaviour on the same
contract-first recipe as `ShowText` (D-0024). (#22 — the Studio-v2 arc finale + the trigger program's Slice 2.)

---

## D-0025 — A map references its tileset by a catalog name in `$data`; one source, editor + runtime honor it

**Decision:** A map's `$data` carries a **map-level tileset name** (`TileMapData.Tileset` → `TileMap.TilesetName`)
naming one sheet from a single-source **`TilesetCatalog`** (the committed LPC sheets — `lpc-mountains` /
`lpc-grass` / `lpc-dirt` / `lpc-water`). The catalog (`Name` → `ResourceFile` + `TileSize`) is the **one source**
the Studio picker, the `MapSerializer` load-time validation, and the runtime sheet selection all read, so editor
and runtime **cannot drift**. An absent reference defaults to `lpc-mountains` (existing maps stay valid); an
unknown name is a typed `MapLoadResult.Failure` (never a throw). **Tileset geometry stays host-derived from each
loaded sheet's real pixels** (`Tileset.FromSheet`) — the catalog carries identity only, not dimensions, so it
cannot drift from the art. One tileset per map (v1); multi-tileset / per-layer / autotiles / import are out.

**Why:** Choosing the art per map is only coherent if the choice is *recorded* and *both* the editor and the
Player honor it — a pure editor preview would persist nothing (the per-cell `tilesetId` is a bare index with no
sheet identity). Routing every consumer through one catalog is the same single-descriptor discipline as the
behaviour registry (D-0024): the editor offers exactly what the runtime renders. Keeping geometry derived from
real pixels (not declared in the catalog) makes drift impossible without a pin test. (#20, the Studio-v2 arc.)

---

## D-0024 — The unified trigger/event model: contract-first, one repeatable pattern

**Decision:** Every interactive thing in a game — NPCs, shopkeepers, room warps, chests, doors, signs — is the
**same construct: a trigger placed on a tile + a behaviour bound to it.** Placement is **data** (`$data`: `id`,
`cell`, `trigger`, `behaviour { kind, params }`); behaviour comes from one of two **interchangeable** sources on
the same model — **built-in behaviours** (engine-shipped, no-code, data-configured: `ShowText` / `Warp` / `Shop`
/ `GiveItem` / `Door` — the no-code baseline, **dialogue included**) or **agent-authored modules** (custom C# for
real logic). Both implement the published **`IMapEvent`** seam (#13) and return declarative `Outcome`s; a single
**registry** binds `kind` → behaviour. It is built **contract-first as ONE repeatable pattern** — no one-off
triggers: the recipe is always *implement `IMapEvent`, register the kind, declare its `.expect` (gate:13).*

**Why:** A unified placed-trigger model is a major simplification over RPG Maker's five-special-cases-per-feature.
The built-in/agent split gives the **no-code baseline you don't fight** for the common 80% (you never summon an
agent to say "Hello") **and** full flexibility for the custom 20% — chosen per trigger. Contract-first + the
registry keeps it reusable and repeatable: built-in and agent behaviours are interchangeable at the interface,
and the `.expect` oracle guards every one. Sequencing (see `INTAKE-event-trigger-system`): foundation
(triggers-as-`$data` + registry + dispatch, proven with built-in `ShowText`) → the built-in library → the Studio
placement UI → the agent authoring flow. (Owner direction, 2026-06-20.)

---

## D-0023 — The repo IS the game: one repo = one game = one binary; no runtime project loader

**Decision:** A monorpgmaker game **is a repository.** One repo = one game = one shipped binary. There is **no
runtime "open project" / project-manager / dynamic project loader** — the engine loads ONLY its own **bundled**
content (embedded resources; console-safe — never a roamed filesystem path). The Studio editor does dev-time file
IO (import / export / save maps) as a convenience, but the **runtime never opens arbitrary projects**. A new game
is a **clone of the template repo**. First cut (#17): the Player boots a bundled `content/maps/start.json`
(embedded) rather than a hardcoded scene.

**Why:** Compiled behavior makes runtime project-switching **incoherent** — a game's events/rules compile into its
binary, so a running game-A binary cannot "open project B." Consoles sandbox the filesystem and want a fixed,
bundled binary, not a runtime that roams a disk for a project folder. And the **game-is-a-repo inherits the whole
apparatus for free** — the gate, the pipeline, CI, versioning, branching — because the game *is* a repo. It also
deletes an entire category of work (a project manager, asset-root resolution, project-format versioning, an
open-project UI). Deferred (flag, not built): **engine-as-referenced-package vs fork-the-template** — how the
engine is consumed when you clone for a new game.

---

## D-0022 — Avalonia is the maker-tool GUI framework; the studio is a thin host over tested editor logic

**Decision:** The monorpgmaker **maker-tool GUI is built in [Avalonia](https://avaloniaui.net)** (XAML desktop).
The first GUI shipped is `MonoRpgMaker.Studio` — a Tiled-like **map-paint editor** (#16). The studio is a thin
`[ExcludeFromCodeCoverage]` **host** (the Player pattern): all testable logic — the `Tileset` sheet model and the
`MapPaintSession` (pointer→cell, palette, paint, new/save/load over `MapSerializer`) — lives in the gated
`MonoRpgMaker.Editor` and carries the coverage + mutation floors; the Avalonia view (window, `MapCanvas`,
`TilePalette`) is excluded and not test-referenced. The host stays **MonoGame-free**: `MapPaintSession` exposes a
framework-neutral surface (`Cell`/`int`/`Tile`), never a MonoGame `Point`, because Engine/Editor reference
MonoGame with `PrivateAssets=All` (it does not flow to a consumer). Tileset sheets ship as `AvaloniaResource`
(`avares://`).

**Why:** Avalonia's native widgets scale to the rest of the maker GUI the roadmap calls for — the ~16-category
database editor, the Modules panel, the inspector — where an immediate-mode UI would strain. Resolves the
long-open "MonoGame/Avalonia later" placeholder. The host/logic split keeps the GUI honest under the gate (the UI
cannot hide untested branching — see PR-claude-editor-view-math-into-tested-layer-001) and the neutral boundary
keeps the maker tool independent of the runtime's rendering framework.

---

## D-0021 — Bundled starter assets: curated LPC under OGA-BY/CC-BY; commercial games OK with attribution

**Decision:** Ship a curated **Liberated Pixel Cup (LPC)** starter asset set as monorpgmaker's free
cold-start bundle, licensed **OGA-BY 3.0 / CC-BY 3.0** (attribution-only). Prefer the OGA-BY subset
(ElizaWy's "LPC Revised" + the Sharm/Redshrike OGA-BY assets) and **avoid the GPL-only and CC-BY-SA-only
LPC assets**. Games built with the engine **and** the bundled art **may be commercial/paid — they owe
only attribution**; the engine code stays MIT (code and art license *separately* — bundling CC-BY/OGA-BY
art does not relicense the engine or the game). Automate attribution (generate a `CREDITS` from each
asset's metadata + an editor credits panel). The free bundle is separate from any commercial asset packs
an author uses for their own games.

**Why:** The cold-start asset story is on the critical path (D-0020). OGA-BY 3.0 is CC-BY 3.0 with the
anti-DRM / technical-measures clause removed → **console-safe**, decisive for the Switch/Xbox/PS target
(D-0001), where plain CC-BY/CC-BY-SA's anti-DRM clause is hostile. Attribution-only (vs share-alike
CC-BY-SA or copyleft GPL) lets authors **sell** their games — a stronger *and accurate* adoption pitch
than "must be free." LPC's 32×32 tiles + 64×64 standard character frames fit the engine's tilemap +
sprite model directly. (Not legal advice; verify per-asset and run a license review before a commercial
launch.)

---

## D-0020 — Target user = technical hobbyist; open-source under MIT

**Decision:** Commit the target user as the **technical hobbyist / developer-adjacent creator**
who reads C#, wants console shipping, and uses the agent + inspector as a force multiplier — and
**drop the "non-coder stays in control" claim.** Open-source under **MIT** with an explicit
no-rug-pull pledge; BYO-API-key; foundation-shaped governance; a zero-AI default path. See
[agentic-strategy.md](agentic-strategy.md).

**Why:** The inspector is read-only and steering AI-written C# needs code literacy; the
architecture serves the code-literate user. Claiming the no-code base both alienates it
(re-introduces code + a metered per-token loop) and over-constrains the natural OSS contributor.
The wedge vs the paid RPG Architect is trust + console-safety + a verifiable extension store, not
out-configuring it.

---

## D-0019 — Additive seam kinds; layerable services; genre-envelope semver

**Decision:** Before locking Abstractions semver, add two ADDITIVE seam kinds — a
**presenter-extension** seam (`IPresentationLayer`: a registered new visual layer driven by its
own save-state) and a **persistent-spatial-state** seam (a per-cell/region store that ticks +
persists). Make `IBattleSystem`/`IPresenter` **layerable** (ordered decorator chain +
manifest-registered custom inspector panels), and record a **sub-tick offset** in the `InputFrame`
replay unit. Reframe the promise as "additively stable WITHIN a stated genre envelope; a new genre
ships as a new seam kind in a minor version."

**Why:** The 12 seams were reverse-engineered from turn-RPG; genre-defining state currently lands
on single-bind services + a closed presenter where extension = wholesale replacement (the
relocated wall). Rhythm, deckbuilder, and farming-sim each break a different boundary. The chassis
is genre-agnostic, so the fix is adding kinds, not redesign — but it must precede the semver lock.

---

## D-0018 — Generator/validator split; the manifest is the source of truth

**Decision:** Split the Roslyn generator (dumb, syntax-keyed, ALWAYS emits a compilable degraded
composition) from a separate **`monorpg validate`** step (over compiled metadata) that does DAG
validation + topo-sort + `contract-manifest.json` and surfaces structured `MRM` errors *before*
the C# compile. The **manifest is the source of truth**; the generator is one producer of it; a
non-generator fallback wiring path is first-class and gate-diffed against the generated one. A
measured **<1s incremental edit budget** is a gate.

**Why:** Cross-assembly `[DependsOn]` into the prebuilt Engine forces a `CompilationProvider` pull
(the documented incrementality killer), and a generator throw surfaces as `CS8785` + a cascade of
`CS0246` pointing into engine surface the agent cannot touch — un-self-diagnosable. Pre-compile
structured validation yields the clean `MRM` diagnostics the design promised; degraded-but-
compilable emit makes the build error the diagnostic, not a 40-error cascade.

---

## D-0017 — By-construction scaffolding + executable correctness contracts

**Decision:** Adopt the aic guarantee-A discipline. `monorpg scaffold <kind>` is a **deterministic
`{{var}}` template emitter** (no LLM, no eval) emitting a **gate-clean-by-construction** skeleton
that mirrors a committed **fixture** per seam-kind (signatures return the shared outcome vocabulary
so raw state mutation won't type-check; `FixedPoint`/`IRandom` wired; a co-located seeded test);
the agent fills only the `// fill:` holes. Every shipped skill is **derived from its fixture**
(Reference Pattern + a Quality Checklist mapping 1:1 to gate checks), CI-diffed so it can't drift.
Add **outcome-return** + **float-ban** analyzers. Make intent executable: a **`<module>.expect`**
table (input → outcome rows) authored in a step DISTINCT from the implementing agent, enforced as
**gate #13 (MRM0006 expectation-mismatch)**; reject modules whose only tests are the agent's own
output-mutations.

**Why:** v1 built only the strict gate (guarantee B); the LLM still free-wrote 100% of every module
body and the purity/float rules had no analyzer — aic's *rejected* copy-and-adapt mode. And the 12
gates verify mechanics, never correctness: the same agent authoring code AND its test from the same
misread intent is a self-grading loop the mutation gate only hardens. By-construction emission + an
independent executable oracle close both holes.

---

## D-0016 — Determinism by construction (fixed-point, coroutine, AOT/multi-arch, scoped reflection)

**Decision:** Lock, before any sim module: **Q16.16 fixed-point** for ALL sim-state math via a
`FixedPoint` primitive in Abstractions (the only numeric type sim handlers may use; an analyzer
bans `float`/`double`/`MathF`/`Vector2`/transcendentals + `foreach`-over-`Dictionary` in sim code);
the **coroutine mechanism = `IEnumerator`-yield / source-generated state machine** (no Task/async
in sim flow). Run the **replay-to-same-hash gate on NativeAOT + a second CPU arch (ARM64 + x64)**.
Scope the no-runtime-reflection rule to the **sim/determinism path only**, with enforced carve-outs
for save-migration (a stored-schema-driven reader), the MonoGame content pipeline, and
System.Text.Json (its own source generator) + a no-reentry analyzer. Promotes prior open decisions
#2/#7 to locks.

**Why:** replay-to-same-hash is the moat and a binding gate, but bit-identical *float* across
FMA/libm/JIT-vs-NativeAOT/CPU-arch is impossible — the gate would be green on dev and divergent on
the console it serves. Locking determinism-by-construction at the cheapest moment makes the hash
portable by construction. "No reflection anywhere" was unverified against save-migration / content /
STJ, where dynamic shape-reading is genuinely needed; a scoped, enforced boundary keeps the
guarantee real.

---

## D-0015 — Tracer-bullet-first build order; MVP = the validated loop

**Decision:** Insert a hand-wired **tracer-bullet phase before P0** (painted map + move/collide +
ONE authored event as plain C#, NO generator/Abstractions/replay-gate, a relaxed prototype gate)
plus a **timing spike** vs RPG Maker. Build P0/P1 as an **extraction** from the working slice.
Redefine the MVP as "the validated authoring loop + the author-event skill that reproduces it," not
"the chassis."

**Why:** v1 sequenced a ~6-month compiler-toolchain-plus-engine *before* testing the one bet the
product lives on (does AI-directed C# authoring beat the click-path and feel good), and built the
pattern/skill substrate *before* any gate-clean feature to derive patterns from — backwards
relative to the aic exemplar (patterns followed mature code). The tracer de-risks the thesis at
~week 3, yields the demo + contributor on-ramp, AND yields the first reference feature the golden
patterns are derived from.

---

## D-0014 — Drupal-style layering; compile-time hook wiring

**Decision:** Structure the system as MonoGame → `MonoRpgMaker.Engine` (core; never
edited by game authors) → a published `MonoRpgMaker.Abstractions` seam surface → a
Project of UI-authored data + agent-authored modules that hook in. Enforce "don't hack
core" via NetArchTest (Project → Abstractions only). Wire extension seams at **compile
time with C# source generators**, not runtime reflection; require **explicit module
ordering / dependencies** (no implicit "everyone gets called").

**Why:** "Don't hack core" bounds the agent's blast radius, keeps engine upgrades
non-breaking, and makes verification tractable. Compile-time wiring is AOT/console-safe,
fast, and deterministic (runtime reflection is none of those). Explicit ordering avoids
RPG Maker MV / Drupal plugin-order hell. Reframes D-0005's six-module structure as
*internal* engine organization under this extension model; the `$data`/`$game` split
stands.

---

## D-0013 — AI-agnostic authoring substrate; dual-truth source of truth

**Decision:** The agentic authoring layer is vendor-neutral: a `monorpg` **CLI**
(scaffold / list-hooks / validate / test / run), an **MCP server** (contracts + codegraph
+ scaffold/validate), portable **markdown skills**, and a **machine-readable
hook/contract manifest**. Any agent (Claude Code, Cursor, Aider, GPT/Gemini, local) is a
*client*, not a dependency. Source of truth for modules is **dual-truth**: an intent-spec
drives; generated C# is a committed, gated artifact; regeneration is a reviewed pipeline
run. The product ships a scoped **spec → generate → inspect → gate** pipeline.

**Why:** AI-agnostic is a hard product requirement; open substrate (CLI / MCP / markdown
/ manifest) avoids vendor lock-in. Dual-truth + determinism (injected RNG, strict
sim/render split) make AI-authored behavior verifiable (invariants + replay) and
re-steerable without one-shot trust.

> **Amended by D-0016/D-0017/D-0018 (2026-06-17, v2):** "dual-truth; regeneration is a reviewed
> pipeline run" overstated it — the spec→C# step is an LLM, so a from-scratch regenerate would
> non-deterministically discard inspect-phase fixes. Corrected model: **scaffold (deterministic,
> once) → author (LLM fills holes) → code-is-truth thereafter**; `intent.md` is a scaffold seed +
> changelog (a drift-detector gate fails if its declared deps/order/state-version disagree with the
> manifest); re-steering edits the existing `.cs` as a localized reviewed change. The vendor-neutral
> substrate stands; "capable agent" is floored by a published capability matrix + a zero-AI default
> path (D-0020).

---

## D-0012 — Agentic C# authoring is the primary behavior surface (inverts D-0008)

**Decision:** Agent-authored C# is the **primary** way to author behavior (battle,
events, menus, custom systems). The visual editor is **maps + the database + an inspector
/ steering view** for AI-authored systems. This inverts D-0008, which made visual no-code
primary and typed C# the optional escape hatch.

**Why:** RPG Maker's heavy UI is its downfall for anything off-path; inverting the primacy
is the product's reason to exist. UI is retained only where direct manipulation wins
(spatial maps, tabular data) and otherwise moves "up the stack" to inspecting/steering a
deterministic, replayable simulation.

---

## D-0011 — Behavior is agent-authored modules, not a visual command palette (supersedes D-0007 as the authoring model)

**Decision:** The event / world-logic layer is authored by **prompting an agent to write
C# modules that hook into engine seams**, not via a visual ~100-command palette.
Supersedes D-0007 *as the authoring model*. A runtime command/effect dispatch MAY still
exist as one emit target, but the editor command palette is not how authors build logic.

**Why:** The event system is RPG Maker's signature feature and its most painful
"programming in a UI trenchcoat" — the prime candidate for agentic authoring (see
[agentic-overview.md](agentic-overview.md)). Replacing the palette-as-authoring-surface is
the core of the pivot.

---

## D-0010 — Console porting is the last phase; preserve the input mapper

**Decision:** Defer console porting to P7+. From the start, route all input
through a `gamepadMapper`-style indirection (raw button → logical action), the
abstraction RPG Maker uses, so platform input remaps without touching game logic.

**Why:** MonoGame console back-ends are gated behind NDA'd SDKs / registered-
developer programs, and the porting specifics (storage/save APIs, content
pipeline, certification) are under-researched. Porting is exercised only once the
desktop game ships, but the input abstraction is cheap to keep correct now.

---

## D-0009 — Map data model informed by Tiled/TMX

**Decision:** Model maps as an integer tile-ID grid (`int[]`) over a sliced
tileset atlas; use Tiled/TMX as the reference for the harder features — Global
Tile IDs with flip flags, **Wang-set autotiles** (8-position Wang ID), and
shape-based per-tile collision — adapted to RPG Maker's A1–A5/B–E conventions.

**Why:** TMX is the best-documented, battle-tested model for exactly these
features, and the int-ID-grid + atlas-slicing approach is the MonoGame-idiomatic,
memory-efficient tilemap representation.

---

## D-0008 — Battle behind a swappable interface; visual events + typed C# plugins

**Decision:** Implement the default turn-based battle system behind an interface
so battle systems are swappable (an explicit product requirement). For
authorability, pair a **visual, no-code** event/database surface with an
**optional typed C# plugin/command registry** for power users.

**Why:** Swappable battle is required and best isolated early behind a boundary.
The visual-events + typed-plugin model is what MZ (dropdown plugin commands +
typed args), Bakin (per-event C# scripts), and Solarus (script-per-content)
independently converge on — non-programmers author visually; power users extend
in C#.

> **Inverted by D-0012** (2026-06-17): agentic C# authoring is *primary*; the visual
> surface is maps + database + an inspector. The swappable-battle-behind-an-interface
> decision here STANDS — only the visual-primary / C#-optional framing is inverted.

---

## D-0007 — Event interpreter = command-code → handler dispatch (1:1)

**Decision:** Build the event interpreter as a dispatch from numbered command
codes to handler methods, 1:1 with the editor's ~100-command palette (Show Text
`101`, Conditional Branch `111`, Transfer Player `201`, Set Movement Route `205`,
Battle Processing `301`, Script `355`, Plugin Command `356`, …).

**Why:** The command-code→method mapping is the verified, reliable architecture.
The specific MV execution-model detail (single `Game_Interpreter`, one command/
frame, `_list`/`_index`) was **refuted** in research — do not build to it.

> **Superseded by D-0011** (2026-06-17) as the *authoring model*: behavior is
> agent-authored modules hooking into engine seams, not the visual command palette.
> A command/effect dispatch may persist as a runtime detail.

---

## D-0006 — Build a scene-graph / transform layer over MonoGame

**Decision:** Implement a Pixi.Container-equivalent scene-graph/transform
hierarchy (children inherit parent coordinates + visibility) for the tilemap,
sprite, and window layers. Evaluate custom vs MonoGame.Extended vs a scene-graph
library in P1.

**Why:** RPG Maker renders through a Pixi display tree; **MonoGame has no
equivalent** (`SpriteBatch` applies one global matrix per batch, no per-node
hierarchy). Faithful parity (cascading move/fade/visibility) needs this layer.

---

## D-0005 — Mirror RPG Maker's runtime architecture; data format first

**Decision:** Structure the runtime on RPG Maker's six-module layering
(core / managers / objects / scenes / sprites / windows) with a strict split of
**immutable `$data` (read-only JSON, maker-authored)** from **serialized `$game`
save state**. Build the data/content format + loader (P0) before systems on top.
Target MZ-era semantics; start with informed parity (not byte-level file compat).

**Why:** This is the canonical, verified architecture; the data format is a
dependency of every system, and the `$data`/`$game` split is the foundational
content/save design. Building it first avoids retrofitting the hardest-to-change
layer. Field-level JSON schema parity is an open question (see feature-research.md).

---

## D-0004 — Forge codegraph gets a C# extractor

**Decision:** Add a C# tree-sitter extractor to `ignibyte-forge`'s
`forge-codeindex` (alongside the existing Rust + JS ones) so `code-find` /
`code-callers` work on the monorpgmaker source.

**Why:** The whole value of "code graphing" here is navigating the C# engine.
The forge only extracted Rust + JS; without C# the codegraph would be empty for
this project.

---

## D-0003 — Share the existing forge instance (multi-tenant)

**Decision:** Register monorpgmaker as its own *project* + *repo* in the running
`ignibyte-forge` instance rather than standing up a dedicated clone.

**Why:** The forge is multi-tenant by design (`project_id` on tickets/knowledge/
code, multi-row `doc_sources`/`code_repos`). A dedicated key scopes our tickets
and knowledge to our own project; one Postgres/Redis to run. Less ops, same
isolation for the things that matter.

---

## D-0002 — Target `net10.0`

**Decision:** All projects target `net10.0`.

**Why:** The dev box has the .NET 10 SDK + runtime (not the 9 runtime). MonoGame
`3.8.*` ships net8-compatible assemblies a net10 app consumes fine. Revisit if a
console back-end pins an older TFM.

---

## D-0001 — Rebuild RPG Maker on MonoGame

**Decision:** Build the maker + runtime as a C# / MonoGame solution rather than
extending an existing RPG Maker runtime.

**Why:** Console shipping is the goal. MonoGame gives one C# engine that targets
desktop now and consoles through registered developer programs. Keeping game
logic in a framework-thin engine assembly keeps those ports tractable.
