---
pipeline_id: b8ef9e35-d0da-4bb6-9c6a-9f7f7b1c75e7
title: WORK-studio-undo-redo
ticket: a27cc647-db9f-4037-a279-fa7e38fb9663
type: work
intake: none (Studio-v2 polish arc; direct request via /work)
notes: WORK-studio-undo-redo.notes.md
status: Phase 5 — Complete PASS
---

# WORK-studio-undo-redo

> Pipeline spec (always-loaded contract). Detailed per-phase work lives in the
> paired `.notes.md`. Phase advance = updating the `status:` frontmatter above.

## Work Spec
- **Title:** Studio v2.3 — **undo/redo** for the map editor: a command/history spine in the gated
  `MapPaintSession`, with Undo/Redo buttons + keyboard shortcuts in the Studio host.
- **Scope:**
  - *In* — a command/history model in `MapPaintSession`: an **undo stack + a redo stack**, each undoable op
    recording its **inverse (a delta)**, not a whole-map snapshot; a new op clears the redo stack. Undoable
    **units**: a paint **stroke** (the host brackets press→release into one command capturing the touched cells'
    before/after tiles), `AddEvent`, `UpdateSelected`, `RemoveSelected`/`RemoveEventAt`, and the tileset switch
    (`SelectTileset`). `NewMap` / a successful `Load` are **history-reset** boundaries (clear both stacks). A
    neutral **`Undo()` / `Redo()` / `CanUndo` / `CanRedo` + `HistoryChanged`** API (commands stay internal;
    public surface `string`/`Cell`/`int`). The Studio host: Undo/Redo **buttons** (enabled per `CanUndo`/
    `CanRedo`) + **key bindings** (Ctrl+Z / Ctrl+Shift+Z / Ctrl+Y) + **stroke bracketing** on pointer
    press/release. Gated tests.
  - *Out (deferred)* — a visible undo-history UI / named levels; coalescing beyond per-stroke (merging adjacent
    same-cell paints; batching rapid inspector text edits); persisting history across sessions/Save; undoing a
    `Save` (it doesn't mutate the document). **Selection is not undoable** (`SelectTile`/`SelectEvent` are
    tool/view state, not document edits).
- **Systems:** editor (`MapPaintSession` — gated; the command/history spine) · studio host (buttons / keys /
  stroke bracketing) · input · map/tilemap + events (the edited document).

## Acceptance Criteria (EARS)

| ID | EARS Requirement | Verification |
|---|---|---|
| REQ-001 | After an undoable edit, `MapPaintSession.CanUndo` shall be true, and `Undo()` shall restore the map + events to their exact prior state. | unit |
| REQ-002 | After an `Undo()`, `CanRedo` shall be true, and `Redo()` shall re-apply the undone edit (state returns to post-edit). | unit |
| REQ-003 | Performing a new undoable edit shall clear the redo stack (`CanRedo` false — no redo across a divergent edit). | unit |
| REQ-004 | A paint stroke (begin → one or more paints → end) shall be a single undo unit (one `Undo()` reverts the whole stroke); a stroke that changed no cell shall push nothing onto the history. | unit |
| REQ-005 | Each event edit (`AddEvent`/`UpdateSelected`/`RemoveSelected`/`RemoveEventAt`) and a tileset switch (`SelectTileset`) shall be individually undoable + redoable, restoring the exact prior state. | unit |
| REQ-006 | `NewMap` and a successful `Load` shall clear both the undo and redo stacks. | unit |
| REQ-007 | When nothing is undoable/redoable, `Undo()`/`Redo()` shall be a no-op (`CanUndo`/`CanRedo` false; never throws). | unit |
| REQ-008 | The Studio shall offer Undo/Redo (toolbar buttons + Ctrl+Z / Ctrl+Shift+Z / Ctrl+Y), enabled per `CanUndo`/`CanRedo`, repainting after each. | manual smoke |
| REQ-009 | The FULL `bin/gate.sh` shall be green, the new command/history logic meeting coverage + Editor mutation floors. | gate |

## Locked-In Decisions
- **Model = command/delta stack** (each undoable op records its inverse), **not** whole-map snapshots — leaner,
  exact-assertable, and mutation-friendly (isolated exact-count asserts have something precise to bite).
- **Undoable = document edits only:** paint strokes + the event ops + the tileset switch. **Selection is NOT
  undoable** (`SelectTile` active-paint-tile, `SelectEvent`) — it's tool/view state.
- **Per-stroke paint:** the host brackets a click-drag (press→release) into one command; a no-op stroke pushes
  nothing.
- **`NewMap` / `Load` reset history** (you cannot undo across a New/Load).
- **Neutral, gated API** (D-0022): `Undo`/`Redo`/`CanUndo`/`CanRedo` + a `HistoryChanged` notification; commands
  are internal; the public surface stays `string`/`Cell`/`int` (no XNA `Point`). The Studio host stays a thin
  `[ExcludeFromCodeCoverage]` shell.
- **Reuse:** `MapEditor.Paint`; immutable command data (PR-claude-readonly-collection-for-true-immutability);
  isolated exact-count asserts (PR-claude-analyzer-mutation-isolated-asserts).

### Open for design (Phase 2 decides)
- The **command representation**: an `IEditCommand { Do/Undo }` interface vs a before/after **delta record** the
  session applies. (Leaning a small internal command abstraction the stacks hold.)
- **Stroke bracketing mechanics**: session `BeginStroke`/`EndStroke` (the canvas calls them on press/release and
  `Paint` accumulates into the open stroke) vs the canvas accumulating cells then issuing one `PaintCells`
  command. (Leaning `BeginStroke`/`EndStroke`.)
- Whether `UpdateSelected` **coalesces** rapid inspector edits or **each apply = one command** (recommend each
  apply = one — simple, predictable; the inspector's `LostFocus`/selection-change already batches typing).
- The **`HistoryChanged` shape** (a C# `event` the host subscribes to, vs a polled `CanUndo`/`CanRedo` the host
  reads after each action) — recommend an `event` so buttons + repaint stay in sync without the host polling.

## Linked Artifacts
- Design docs: `docs/decisions.md` (D-0022 Avalonia thin host + neutral boundary), `docs/roadmap.md` (editor track).
- Intake doc: none (direct request; the Studio-v2 polish arc).
- Ticket doc: `docs/planning/tickets/open/TICKET-0021-studio-undo-redo.md`
- Forge ticket: a27cc647-db9f-4037-a279-fa7e38fb9663 (#21)

## Phase Plan
| Phase | Status |
|---|---|
| 1 — Plan | PASS |
| 2 — Design | PASS |
| 3 — Implement | PASS (build 0/0; 394 tests; GATE GREEN [fast]) |
| 3.5 — Inspect | PASS (1 MAJOR reverse-unwind bug found by repro + fixed; gate green) |
| 4 — Validate | PASS (417 tests; GATE GREEN [full]: mut Editor 85.98) |
| 5 — Complete | PASS |
