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

**Decision:** Add a C# tree-sitter extractor to `oathstar-forge`'s
`forge-codeindex` (alongside the existing Rust + JS ones) so `code-find` /
`code-callers` work on the monorpgmaker source.

**Why:** The whole value of "code graphing" here is navigating the C# engine.
The forge only extracted Rust + JS; without C# the codegraph would be empty for
this project.

---

## D-0003 — Share the existing forge instance (multi-tenant)

**Decision:** Register monorpgmaker as its own *project* + *repo* in the running
`oathstar-forge` instance rather than standing up a dedicated clone.

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
