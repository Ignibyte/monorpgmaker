# Decisions

A running log of locked architecture decisions (ADRs). Newest first. Mirror
significant ones into the forge knowledge store via `architecture-decision-record`.

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
