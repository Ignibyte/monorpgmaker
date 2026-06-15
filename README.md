# monorpgmaker

An RPG Maker–style game maker built on [MonoGame](https://monogame.net), so games
authored with it can ship to desktop **and consoles** from one C# codebase.

## Why

RPG Maker is great for authoring 2D RPGs but its runtime is hard to ship to
consoles. MonoGame targets Windows / macOS / Linux today and has console
back-ends (Switch / Xbox / PlayStation) through registered developer channels.
Rebuilding the editor + runtime on MonoGame gives us one engine that runs
everywhere.

## Layout

```
src/
  MonoRpgMaker.Engine   class library — the runtime engine (maps, entities, data, the game loop)
  MonoRpgMaker.Player   desktop GL app — runs an authored project
  MonoRpgMaker.Editor   the maker tool (headless editing logic today; GUI later)
tests/
  MonoRpgMaker.Engine.Tests   xUnit tests for the engine
docs/                    design docs (indexed for semantic search by the forge sidecar)
```

The engine is organised by domain: `World` (tiles + maps), `Entities`
(movable actors), `Data` (the id-keyed game database), and `Core` (the
MonoGame host).

## Build & test

```bash
dotnet build MonoRpgMaker.slnx
dotnet test  tests/MonoRpgMaker.Engine.Tests
```

Targets `net10.0`. The Player uses the MonoGame Content Pipeline (MGCB).

## Tooling — the forge sidecar

This repo is wired to **oathstar-forge** (`../oathstar-forge`), a local Rust
MCP sidecar that gives Claude Code agents:

- a **codegraph** over the C# source (`code-find`, `code-callers`, `code-callees`),
- **semantic search** over these design docs (`docs-search`),
- a **knowledge store** (lessons / failures / decisions) and a **ticket/sprint
  backlog** for the work pipeline.

Start it with `../oathstar-forge/scripts/start-all.sh`; the tools appear in
Claude Code as `mcp__forge__*` once `.mcp.json` is in place (copy it from
`.mcp.json.example`). See `docs/technical-architecture.md`.
