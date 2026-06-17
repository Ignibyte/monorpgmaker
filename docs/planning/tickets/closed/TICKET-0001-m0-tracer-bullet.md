---
title: TICKET-0001-m0-tracer-bullet
status: closed
ticket: b1454ae5-81fa-4933-813e-95eaf873a8f9
ticket_number: 1
type: spike
created: 2026-06-17
intake:
pipeline_spec: docs/planning/pipeline/active/WORK-m0-tracer-bullet-v1.spec.md
---

# TICKET-0001-m0-tracer-bullet

## Summary

Hand-wired vertical slice (M0 / D-0015) proving the AI-directed authoring loop *before* the chassis:
a painted tile map + 4-dir move/collide + ONE authored event (walk onto a lever → show a message
once → set a `door_open` switch → a door tile opens), plain C# on the existing Engine scaffold,
deliberately pre-chassis, under a relaxed prototype gate.

## Why

The product lives or dies on whether directing an AI to write game logic in C# beats RPG Maker's
click-path and *feels good*. v1's order would have tested that ~6 months in, atop a compiler
toolchain. M0 surfaces the verdict at ~week 3 and yields the first gate-clean feature the golden
patterns/skills/fixtures are derived from (D-0015; PR-claude-tracer-001).

## EARS Requirements

| ID | EARS Requirement | Verification |
|---|---|---|
| REQ-001 | If the player attempts to move into a tile marked impassable, then the system shall reject the move and leave the player's cell unchanged. | unit test (Entity/TileMap collision) |
| REQ-002 | When the player steps onto the lever tile, the system shall display the lever message exactly once across repeated entries and set the `door_open` switch to true. | unit test (event + game state) |
| REQ-003 | While the `door_open` switch is false the door tile shall be impassable; when it becomes true the door tile shall be passable. | unit test (passability resolution) |
| REQ-004 | When the Player application is launched, the system shall render the painted map and a player sprite and move the player one tile per arrow-key press within map bounds. | smoke check (manual run of MonoRpgMaker.Player) |
| REQ-005 | The slice's simulation code shall use no `System.Random` or `DateTime`; any randomness shall be obtained from an injected `IRandom`. | source-bans gate + review |
| REQ-006 | The prototype shall build clean under `dotnet build -warnaserror` and `dotnet format --verify-no-changes` (the relaxed prototype gate). | prototype gate run |

## Scope

- **In:** a small hand-painted map (int grid + minimal placeholder tileset: floor / wall / door /
  lever); a player entity with 4-dir grid movement + tile collision (reuse/extend `Entity.TryStep` +
  `TileMap.IsBlocked`); a minimal in-memory `GameState` (a switch store); ONE authored event (the
  lever); switch-driven door passability; rendering of map + player + a minimal on-screen message;
  keyboard input; the `MonoRpgMaker.Player` app boots the slice; unit tests for the deterministic sim;
  the prototype-gate invocation.
- **Out:** the source generator, `Abstractions` assembly, contract-manifest, replay-hash gate,
  fixed-point math, the scaffolder + `{{var}}` templates, the `EventContext`/seam taxonomy, battle,
  menus, dialogue text-codes, save/load persistence, the database/map editor GUI, real art assets, the
  AI-authoring tooling (CLI/MCP/skills), and the coverage/mutation gates.

## Notes

- Forge ticket: b1454ae5-81fa-4933-813e-95eaf873a8f9 (#1)
- Related docs: docs/plan.md (M0), docs/roadmap.md (P-(-1)), docs/decisions.md (D-0015 / D-0016),
  docs/agentic-features.md (events)
- Promoted from intake: none
- Active pipeline: WORK-m0-tracer-bullet-v1
