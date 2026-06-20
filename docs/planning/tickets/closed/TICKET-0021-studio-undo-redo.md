---
title: TICKET-0021-studio-undo-redo
status: done
ticket: a27cc647-db9f-4037-a279-fa7e38fb9663
ticket_number: 21
type: feature
created: 2026-06-20
closed: 2026-06-20
intake:
pipeline_spec: docs/planning/pipeline/completed/WORK-studio-undo-redo.spec.md
---

# TICKET-0021-studio-undo-redo

## Summary

**Undo/redo for the Studio map editor**: a command/history spine in the gated `MapPaintSession` so editor
operations are undoable and redoable, with Undo/Redo toolbar buttons + keyboard shortcuts in the Studio host.
The third Studio-v2 slice (after #19 event placement, #20 tileset picker). Foundational — once the stack
exists, current **and future** editor ops plug into it.

## Why

The painter has no safety net: a wrong paint stroke, a mis-placed event, or a tileset switch can't be taken
back. Undo/redo is table-stakes for an editor, and building the command/history spine now means later editor
features (multi-map, more event kinds) get undo for free.

## EARS Requirements

| ID | EARS Requirement | Verification |
|---|---|---|
| REQ-001 | After an undoable edit, `MapPaintSession.CanUndo` shall be true, and `Undo()` shall restore the map + events to their exact prior state. | unit |
| REQ-002 | After an `Undo()`, `CanRedo` shall be true, and `Redo()` shall re-apply the undone edit. | unit |
| REQ-003 | Performing a new undoable edit shall clear the redo stack (no redo across a divergent edit). | unit |
| REQ-004 | A paint stroke (begin → paint(s) → end) shall be a single undo unit; a stroke that changed no cell shall push nothing. | unit |
| REQ-005 | Each event edit (`AddEvent`/`UpdateSelected`/`RemoveSelected`/`RemoveEventAt`) and a tileset switch (`SelectTileset`) shall be individually undoable + redoable, restoring the exact prior state. | unit |
| REQ-006 | `NewMap` and a successful `Load` shall clear both the undo and redo stacks (no undo across a reset). | unit |
| REQ-007 | When nothing is undoable/redoable, `Undo()`/`Redo()` shall be a no-op (`CanUndo`/`CanRedo` false; never throws). | unit |
| REQ-008 | The Studio shall offer Undo/Redo (toolbar buttons + Ctrl+Z / Ctrl+Shift+Z / Ctrl+Y), enabled per `CanUndo`/`CanRedo`, repainting after each. | manual smoke |
| REQ-009 | The FULL `bin/gate.sh` shall be green, with the new command/history logic meeting coverage + Editor mutation floors. | gate |

## Scope

- **In:** a command/history model in `MapPaintSession` (undo + redo stacks; each undoable op records its inverse
  delta — not a whole-map snapshot); undoable units = a paint **stroke**, `AddEvent`, `UpdateSelected`,
  `RemoveSelected`/`RemoveEventAt`, `SelectTileset`; reset boundaries (`NewMap`/`Load` clear history); a neutral
  `Undo`/`Redo`/`CanUndo`/`CanRedo` + `HistoryChanged` API; the Studio Undo/Redo buttons + key bindings + stroke
  bracketing; gated tests.
- **Out (deferred):** a visible undo-history UI / named levels; coalescing beyond per-stroke (merging adjacent
  same-cell paints, batching rapid text edits); persisting history across sessions/Save; undoing a Save
  (non-mutating). **Selection is not undoable** (`SelectTile`/`SelectEvent` are tool/view state).

## Notes

- Forge ticket: a27cc647-db9f-4037-a279-fa7e38fb9663 (#21, project monorpgmaker `a9ee8162-3cfd-4c99-be5c-bbdb4be32b10`)
- Related docs: `docs/decisions.md` (D-0022 Avalonia thin host + neutral boundary); `docs/roadmap.md` (the editor track).
- Builds on: `MapPaintSession` (#16/#19/#20), `MapEditor.Paint/Clear` (#16). Branch `feat/studio-undo-redo`,
  stacked on #20 (`be84b11`).
- Reuse: PR-claude-readonly-collection-for-true-immutability (immutable command data), PR-claude-analyzer-mutation-isolated-asserts (isolated exact-count asserts).
- Promoted from intake: none (direct request via `/work`; the Studio-v2 polish arc).
- Active pipeline: docs/planning/pipeline/active/WORK-studio-undo-redo.spec.md
