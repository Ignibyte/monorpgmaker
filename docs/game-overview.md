# Game Overview

**monorpgmaker** is a remake of the RPG Maker authoring tool and runtime on top of
MonoGame — an engine we can ship to **consoles** — with one defining twist: instead of
authoring game logic through a heavy visual UI, **an AI agent writes it as code and the
human directs.** We keep the familiar RPG Maker vocabulary and the visual surfaces where
they win (painting maps, populating the database) and replace the event/command UI with
agentic authoring. See **[agentic-overview.md](agentic-overview.md)** for the full thesis
and architecture.

## Audience

Two users, one codebase:

- **Makers** author a game by *directing*: they paint maps and populate the database in
  the editor (visual, data work), and **prompt an AI agent** to build behavior — battles,
  events, menus, custom systems — as C# modules against the engine. They don't hand-write
  the systems, and they don't click through a command palette; they direct an agent that
  writes the code, then inspect and steer the result.
- **Players** run the authored project, packaged per platform.

## Core concepts

The vocabulary mirrors RPG Maker so existing makers feel at home:

- **Map** — a grid of **tiles**. Each tile references a tileset image and carries a
  collision flag. Maps are the spaces the player walks. *(Authored in the visual map
  editor.)*
- **Tileset** — the source image a tile is cut from.
- **Entity / Actor** — anything that occupies a tile. Actors carry stats (HP, etc.) and
  face a direction; the party, NPCs, and enemies are all actors.
- **Database** — id-keyed tables of game data: items, actors, skills, enemies. Authored
  once (in the editor or by the agent), referenced everywhere by id.
- **Event** — a placed, triggered interaction on a map (a chest, an NPC, a trigger). The
  *trigger* (action button, player touch, autorun, parallel) and placement stay
  light-UI; what an event *runs* is an agent-authored **module**, not a visual command
  list.
- **Module** — a unit of behavior the agent authors and **hooks into the engine** (a
  battle system, a quest, a cutscene, a custom menu). Modules are the agentic replacement
  for RPG Maker's visual command lists: composed and hooked in, never forked into the
  engine. See [agentic-overview.md](agentic-overview.md).

## Target platforms

- **Now:** Windows / macOS / Linux via MonoGame DesktopGL.
- **Next:** the console back-ends MonoGame supports through registered developer programs
  (Switch / Xbox / PlayStation).

Shipping to consoles is the whole reason for the MonoGame rebuild — see
[technical-architecture.md](technical-architecture.md).

## Non-goals (for now)

- 3D. monorpgmaker is a 2D, tile-based maker.
- Importing RPG Maker `.rxdata`/`.json` projects. A future converter is possible but not
  a launch goal.
