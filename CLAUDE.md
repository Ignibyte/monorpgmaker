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
**11 gates**:

```
1 format   2 build(-warnaserror)  3 test   4 vuln-deps  5 gitleaks
6 shellcheck  7 no-suppressions  8 source-bans(SAST)  9 doc-todos
[FULL] 10 coverage (coverlet line floor)   11 mutation (Stryker MSI)
```

- `bin/gate.sh` — FULL (all 11, incl. coverage + mutation). Required before `/commit`.
- `bin/gate.sh --fast` — the 9 static gates, for a quick loop (`GATE GREEN [fast]`).
  Only a FULL green writes `.git/monorpgmaker-gate-receipt`, so `--fast` can't satisfy `/commit`.
- Floors are baked-in minimums env can only raise: `NET_COV_MIN=80`,
  `MUT_MSI_MIN=80` (actuals ~94% / ~84%); they ratchet up over time.
- On a FULL green the gate writes the receipt; `enforce-commit-gate.sh` blocks
  `git commit` of `.cs` unless that fingerprint matches the worktree.

Tools: .NET 10 SDK; `dotnet tool install -g dotnet-stryker` (mutation); coverlet
ships with the test project; `brew install gitleaks shellcheck`. Run the gate
before `/commit`; fix every red at source.

## The forge sidecar (knowledge + codegraph)

monorpgmaker is a tenant of the **shared oathstar-forge** sidecar
(`../oathstar-forge`), an MCP service registered as `forge` in `.mcp.json`
(tools appear as `mcp__forge__*`). Start it with
`../oathstar-forge/scripts/start-all.sh`.

- **Recall** before planning/coding: `knowledge-search`, `knowledge-context`
  (lessons/failures/prevention rules), `docs-search` (design docs),
  `code-find`/`code-callers`/`code-callees` (the C# codegraph).
- **IMPORTANT — pass `repo: "monorpgmaker"`** to the code tools. They default to
  the `oathstar` repo; without the arg you'll search the wrong project.
- **Capture** at phase close: `aar-submit` (lessons), `failure-record`,
  `prevention-rule-record` / `architecture-decision-record`.
- Tickets/sprints/bulletins are there too (`ticket-*`, `bulletin-list`), scoped
  to monorpgmaker's project by the bearer.

The codegraph indexes the working tree in local mode, so a `git commit` here
refreshes it within the poller interval (no manual reindex). `.mcp.json` is
owner-managed — don't overwrite it (gitignored: holds the bearer).

## Code conventions (binding: CONSTITUTION §14)

C#: `dotnet format` is law; analyzer-clean at `-warnaserror` (`latest-recommended`);
nullable enabled; typed results over exceptions on input paths; XML-doc public
items. Keep game logic in `MonoRpgMaker.Engine` (framework-thin) and simulation
state separate from rendering; keep simulation deterministic (inject RNG). No
secrets in source. Reuse before adding (`code-find` first).
