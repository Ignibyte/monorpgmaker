# Game Overview

**monorpgmaker** is a remake of the RPG Maker authoring tool and runtime on top
of MonoGame. The goal is parity with the core RPG Maker workflow — paint maps,
populate a database of actors/items/skills, script events — while running on an
engine we can ship to consoles.

## Audience

Two users, one codebase:

- **Makers** author a game in the editor (maps, database, events) without writing code.
- **Players** run the authored project, packaged per platform.

## Core concepts

The vocabulary mirrors RPG Maker so existing makers feel at home:

- **Map** — a grid of **tiles**. Each tile references a tileset image and carries
  a collision flag. Maps are the spaces the player walks.
- **Tileset** — the source image a tile is cut from.
- **Entity / Actor** — anything that occupies a tile. Actors carry stats (HP,
  etc.) and face a direction; the party, NPCs, and enemies are all actors.
- **Database** — id-keyed tables of game data: items, actors, skills, enemies.
  Authored once, referenced everywhere by id.
- **Event** — a scripted interaction placed on the map (a chest, an NPC, a
  trigger). Events run command lists (dialogue, movement, battles).

## Target platforms

- **Now:** Windows / macOS / Linux via MonoGame DesktopGL.
- **Next:** the console back-ends MonoGame supports through registered
  developer programs (Switch / Xbox / PlayStation).

Shipping to consoles is the whole reason for the MonoGame rebuild — see
[technical-architecture.md](technical-architecture.md).

## Non-goals (for now)

- 3D. monorpgmaker is a 2D, tile-based maker.
- Importing RPG Maker `.rxdata`/`.json` projects. A future converter is possible
  but not a launch goal.
