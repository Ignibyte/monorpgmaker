# Decisions

A running log of locked architecture decisions (ADRs). Newest first. Mirror
significant ones into the forge knowledge store via `architecture-decision-record`.
Decisions D-0005…D-0010 are grounded in [feature-research.md](feature-research.md).

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

---

## D-0007 — Event interpreter = command-code → handler dispatch (1:1)

**Decision:** Build the event interpreter as a dispatch from numbered command
codes to handler methods, 1:1 with the editor's ~100-command palette (Show Text
`101`, Conditional Branch `111`, Transfer Player `201`, Set Movement Route `205`,
Battle Processing `301`, Script `355`, Plugin Command `356`, …).

**Why:** The command-code→method mapping is the verified, reliable architecture.
The specific MV execution-model detail (single `Game_Interpreter`, one command/
frame, `_list`/`_index`) was **refuted** in research — do not build to it.

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
