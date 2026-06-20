# Technical Architecture

## Stack

- **Language:** C# (`net10.0`).
- **Engine:** [MonoGame](https://monogame.net) `3.8.*` (DesktopGL back-end today).
- **Tests:** xUnit + coverlet.
- **Tooling sidecar:** `ignibyte-forge` (Rust MCP service) for codegraph,
  doc search, knowledge capture, and the ticket/sprint work pipeline.

## Solution layout

| Project | Kind | Responsibility |
|---|---|---|
| `MonoRpgMaker.Engine` | class library | The runtime engine: world model, entities, data, the MonoGame host. The one assembly both the player and editor depend on. |
| `MonoRpgMaker.Player` | desktop GL app | Boots a **bundled `$data`** map (#17, D-0023) through the engine, rendering LPC tileset sprites with a player-following camera. The shipped executable. |
| `MonoRpgMaker.Editor` | class library | The maker tool's tested logic: headless editing ops (`MapEditor`) + the map-paint view-model (`Tileset`, `MapPaintSession`). Gated (coverage + mutation). |
| `MonoRpgMaker.Studio` | Avalonia desktop app | The maker **GUI** (D-0022). First slice: a Tiled-like map-paint editor. A thin `[ExcludeFromCodeCoverage]` host over the Editor's logic; stays MonoGame-free. |
| `MonoRpgMaker.Engine.Tests` | xUnit | Engine + Editor unit/mutation tests. |

Shared MSBuild settings live in `Directory.Build.props` (nullable on, explicit
usings, latest C#) so the quality gate can tighten rules in one place.

## Engine domains

The engine is organised by namespace, not by layer:

- **`World`** — `Tile` (a cell: tileset id + collision), `TileMap` (a bounded
  grid with `GetTile`/`SetTile`/`IsBlocked`), `Direction` + `DirectionExtensions`.
- **`Entities`** — `IMovable`, the `Entity` base (grid position + `TryStep`
  collision-aware movement), and `Actor` (stats: HP, damage/heal, defeat).
- **`Data`** — `IRecord` and the generic `Database<T>` id-keyed table, plus
  record types (`ItemRecord`, `ActorRecord`). This is the RPG Maker "database".
- **`Core`** — `RpgGame`, the MonoGame `Game` subclass that owns the loop and
  wires input to the world. The Player subclasses it.

Engine types intentionally expose a few MonoGame value types (e.g. `Point`) in
their public API, so consumers (Player, Editor, tests) reference MonoGame
directly rather than receiving it transitively. The exception is
`MonoRpgMaker.Studio` (the Avalonia host), which stays **MonoGame-free**: its
view-model (`MapPaintSession`) exposes a framework-neutral surface
(`Cell`/`int`/`Tile`), so the host never references MonoGame (D-0022). This works
because Engine/Editor reference MonoGame with `PrivateAssets=All`, so it does not
flow to a consumer that doesn't ask for it.

## Why MonoGame

The point of the rebuild is **console reach**. MonoGame compiles the same C# to
desktop today and to console back-ends (Switch / Xbox / PlayStation) through
registered developer programs. Keeping all game logic in `MonoRpgMaker.Engine`
(framework-thin) keeps those ports tractable.

## The forge sidecar

`ignibyte-forge` is a loopback Rust MCP service (`http://127.0.0.1:8080/mcp/forge`)
shared across local projects. monorpgmaker is registered as its own
*project* + *repo* there, so its tickets, knowledge, and codegraph are isolated
by tenant. It provides:

- **Codegraph** — symbol + call graph over the C# source (`code-find`,
  `code-callers`, `code-callees`). A C# tree-sitter extractor was added to the
  forge for this project.
- **Doc search** — semantic (RRF lexical + vector) search over `docs/*.md`.
- **Knowledge** — capture/recall of lessons, failures, and architecture decisions.
- **Pipeline** — tickets / sprints / bulletins backing the phase-gated workflow.

The working tree is indexed in *local mode*: a `git commit` refreshes the
codegraph within the poller interval, no manual reindex needed.
