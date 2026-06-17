# CLAUDE.md — monorpgmaker

Guidance for Claude Code working in this repo.

## What this is

**monorpgmaker** is a remake of the RPG Maker authoring tool + runtime on
**MonoGame**, so games authored with it can ship to desktop **and consoles**
from one C# codebase. The solution (`MonoRpgMaker.slnx`, `net10.0`):

- `src/MonoRpgMaker.Engine` — the runtime engine (the framework-thin core both
  the player and editor depend on). Organised by domain: `World` (tiles, maps,
  direction), `Entities` (movable actors), `Data` (the id-keyed game database),
  `Core` (`RpgGame`, the MonoGame host).
- `src/MonoRpgMaker.Player` — the desktop GL app that runs an authored project.
- `src/MonoRpgMaker.Editor` — the maker tool (headless editing logic today; GUI later).
- `tests/MonoRpgMaker.Engine.Tests` — xUnit tests.

Design docs live in `docs/`; locked decisions in `docs/decisions.md`. The vision
+ feature direction is `docs/game-overview.md`, `docs/roadmap.md`, and
`docs/technical-architecture.md`.

## How we work — the pipeline (binding: CONSTITUTION.md)

Non-trivial work flows through a phase-gated pipeline. Enforcement hooks in
`.claude/hooks/` make the gates real (they no-op outside a pipeline session).

```
/work                  pre-flight: forge up? tooling? bulletins? recall context
  → /pipeline:plan       forge ticket/doc + active spec/notes
  → /pipeline:design     design + regression test plan
  → /pipeline:implement  code (recall from forge first — §18.3)
  → /pipeline:inspect    adversarial critic review of the diff, then fix (§18.1)
  → /pipeline:validate   write + RUN tests; bin/gate.sh green
  → /pipeline:complete   docs + capture knowledge to forge + archive
  → /commit              full bin/gate.sh, then commit/PR
```

Pre-ticket intake lives in `docs/planning/intake/`. Forge-backed backlog lives
in `docs/planning/tickets/{open,closed}/`. Pipeline docs live in
`docs/planning/pipeline/{active,completed,_templates}/` as a `<title>.spec.md`
plus `.notes.md` pair for the single active item. Acceptance criteria use EARS.

If the user waives the pipeline ("just do it"), do the work directly — but still
recall from the forge first and capture lessons after.

## The gate — `bin/gate.sh` (binding: CONSTITUTION §0)

The single source of truth for shippable. Strict, no baselines, source-fix only.
**12 gates**:

```
1 format   2 build(-warnaserror)  3 test   4 vuln-deps  5 licenses(SPDX allowlist)
6 gitleaks  7 shellcheck  8 no-suppressions  9 source-bans(SAST)  10 doc-todos
[FULL] 11 coverage (coverlet line floor)   12 mutation (Stryker MSI)
```

- `bin/gate.sh` — FULL (all 12, incl. coverage + mutation). Required before `/commit`.
- `bin/gate.sh --fast` — the 10 static gates, for a quick loop (`GATE GREEN [fast]`).
  Only a FULL green writes `.git/monorpgmaker-gate-receipt`, so `--fast` can't satisfy `/commit`.
- Floors are baked-in minimums env can only raise: `NET_COV_MIN=80`,
  `MUT_MSI_MIN=80` (actuals ~94% / ~84%); they ratchet up over time.
- On a FULL green the gate writes the receipt; `enforce-commit-gate.sh` blocks
  `git commit` of `.cs` unless that fingerprint matches the worktree.

Tools: .NET 10 SDK (pinned in `global.json`); the CLI tools (Stryker, nuget-license,
mgcb) are pinned in `.config/dotnet-tools.json` — `dotnet tool restore` provisions
them; coverlet ships with the test project; `brew install gitleaks shellcheck`.
Dependencies are pinned by committed `packages.lock.json` (the gate restores
locked); the source-bans are enforced at compile time by BannedApiAnalyzers
(`BannedSymbols.txt`) and architecture layering by NetArchTest. Run the gate
before `/commit`; fix every red at source.

## The forge sidecar (knowledge + codegraph)

monorpgmaker is a tenant of the **shared forge** sidecar
([`ignibyte-forge`](https://github.com/Ignibyte/ignibyte-forge)), a local
loopback MCP service at `http://127.0.0.1:8080/mcp/forge`, registered as `forge`
in `.mcp.json` (tools appear as `mcp__forge__*`). On this machine it runs
always-on; if `localhost:8080/health` is down, restart it with
`launchctl kickstart -k gui/$(id -u)/com.chadpeppers.forge-mcp`.

- **Recall** before planning/coding: `knowledge-search`, `knowledge-context`
  (lessons/failures/prevention rules), `docs-search` (design docs),
  `code-find`/`code-callers`/`code-callees` (the C# codegraph).
- **IMPORTANT — pass `repo: "monorpgmaker"`** to the code tools. They default to
  the `oathstar` repo; without the arg you'll search the wrong project.
- **`knowledge-search` / `docs-search` have NO project filter and bleed across
  tenants** (verified: a `docs-search` returned oathstar's Datastar/Tauri/Rust
  docs; `source_path` collides — both repos have `docs/technical-architecture.md`).
  Knowledge hits are opaque UUIDs with no project/source attribution. Mitigate:
  bias queries with monorpgmaker-distinct terms (MonoGame, C#, `MonoRpgMaker.*`,
  `FixedPoint`, `IRandom`) and **discard any hit about Datastar / Tauri / Rust /
  Axum / SSE-HTML / `oathstar-*` crates — that's the predecessor, not this repo.**
- **Capture** at phase close: `aar-submit` (lessons), `failure-record`,
  `prevention-rule-record` / `architecture-decision-record`.
- Tickets/sprints/bulletins are there too (`ticket-*`, `sprint-*`,
  `bulletin-list`). **The forge bearer now spans projects** (cross-project/admin
  caller), so `ticket-list` / `ticket-next` return rows from *every* tenant — not
  just this repo (verified: one `ticket-list` came back with both projects below).
  Scope by **`project_id`** yourself:
  - monorpgmaker (this repo — monogame/csharp) =
    **`a9ee8162-3cfd-4c99-be5c-bbdb4be32b10`**
  - oathstar (the predecessor — Rust/Tauri/Datastar web, *not* this repo) =
    `faacfd78-05cb-4f17-804e-1daa709e5ed2`
  - `ticket-create`: pass `project_id` = monorpgmaker's id explicitly (don't rely
    on the default bound project). `ticket-list` / `ticket-next` take no project
    filter — fetch, then keep only rows whose `project_id` matches monorpgmaker.
    Prefer `ticket-get` by `id` (uuid) over `number`; the per-project `number` is
    ambiguous under a cross-project bearer.

The codegraph indexes the working tree in local mode, so a `git commit` here
refreshes it within the poller interval (no manual reindex). `.mcp.json` is
owner-managed — don't overwrite it (gitignored: holds the bearer).

## Code conventions (binding: CONSTITUTION §14)

C#: `dotnet format` is law; analyzer-clean at `-warnaserror` (`latest-recommended`);
nullable enabled; typed results over exceptions on input paths; XML-doc public
items. Keep game logic in `MonoRpgMaker.Engine` (framework-thin) and simulation
state separate from rendering; keep simulation deterministic (inject RNG). No
secrets in source. Reuse before adding (`code-find` first).
