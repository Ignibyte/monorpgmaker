# WORK-studio-undo-redo — Notes

Per-phase working notes for the paired `.spec.md`. The spec is the contract;
this is the record of what happened.

## Phase 1 — Plan
- **Request:** Studio-v2 polish arc, next slice — undo/redo for the map editor (a command/history spine in the
  gated `MapPaintSession` + Undo/Redo buttons + keyboard shortcuts in the Studio host). The user chose to
  continue here after #20 (tileset picker).
- **Intake source:** none — a direct request via `/work` (the Studio-v2 arc has no umbrella intake doc).
- **Classification / tier:** a **work pipeline** (feature), one vertical slice. Foundational but self-contained:
  the command/history spine + the undoable-command set + the thin host wiring.
- **Forge recall (lessons/failures surfaced):**
  - `bulletin-list` → none. **GREENFIELD** — no existing `Command`/`History`/`Undo` types in `src` (verified).
  - `docs-search` mostly **bled the oathstar predecessor** (Rust/web `editor.rs` / rooms / regions /
    `POST /editor/maps` / "FF-merge" — discarded per CLAUDE.md). The one monorpgmaker hit (#16) confirms the
    reuse anchors: `MapEditor.Paint/Clear`, and the patterns PR-claude-analyzer-mutation-isolated-asserts +
    PR-claude-readonly-collection-for-true-immutability.
  - `knowledge-search` hits were opaque cross-tenant UUIDs (D-0022 / neutral-boundary / isolated-asserts
    recur); deferred to `knowledge-context` at Phase-2 entry (needs the aar_id).
- **Grounding (current `MapPaintSession`, post-#20):** the mutating ops that are undoable candidates are
  `Paint(cell)` (per-stroke), `SelectTileset(name)` (→ `Map.TilesetName`), `AddEvent`, `UpdateSelected`,
  `RemoveSelected`/`RemoveEventAt`. `SelectTile`(active paint tile) + `SelectEvent` are tool/view state (NOT
  undoable). `NewMap`/`Load` are resets. The canvas drives a stroke as press → move* → release (`_painting`).
- **Scoping forks (resolved; to confirm at review):** (1) MODEL = **command/delta stack** (inverse per op), not
  snapshots. (2) UNIT = per-**stroke** paint + the 4 event ops + tileset switch; selection excluded;
  `NewMap`/`Load` reset. (3) API = `Undo`/`Redo`/`CanUndo`/`CanRedo` + `HistoryChanged`, neutral, gated.
- **Ticket:** #21 — `a27cc647-db9f-4037-a279-fa7e38fb9663` (project monorpgmaker `a9ee8162-3cfd-4c99-be5c-bbdb4be32b10`).
- **AAR id (from `aar-open`):** 72dc6f7d-48df-46bb-bdfe-b1864f29d692 (user confirmed deltas + per-stroke + full scope at the plan-review gate, 2026-06-20).
- **EARS requirements reviewed:** REQ-001 (undo restores) · REQ-002 (redo re-applies) · REQ-003 (new op clears
  redo) · REQ-004 (per-stroke unit + empty-stroke no-push) · REQ-005 (event + tileset ops round-trip) · REQ-006
  (NewMap/Load reset) · REQ-007 (empty no-op totality) · REQ-008 (Studio buttons + keys — smoke) · REQ-009
  (FULL gate green).

## Phase 2 — Design

### Approach / architecture (gated Editor; D-0022 neutral; §14)
**A small `EditHistory` collaborator + delegate-pair commands.** New gated `Editor/EditHistory.cs`:
- `internal sealed record EditCommand(Action Apply, Action Revert)` — the undo unit (Apply = do/redo, Revert =
  undo). Delegate-pairs (not a class-per-command) keep it lean; the *concrete* history methods below are what
  Stryker mutates + the behavioural tests kill.
- `EditHistory` holds `Stack<EditCommand> _undo / _redo` (Editor is NOT sim — no Dictionary/HashSet-enumeration
  ban; Stacks anyway). `Execute(cmd)` = `cmd.Apply(); _undo.Push(cmd); _redo.Clear(); raise`. `Undo()` = guard
  `_undo.Count>0`, pop, `Revert()`, push `_redo`, raise, return bool. `Redo()` = symmetric. `CanUndo`/`CanRedo`
  = `Count>0`. `Clear()` = clear both + raise. `event EventHandler? Changed` fires on every mutation.

**`MapPaintSession` holds one `_history` and routes every undoable op through it — signatures UNCHANGED** (so the
#19/#20 tests + the Studio host keep compiling/passing). Each op builds its inverse inline:
- `Paint(Cell)->bool` — applies the tile immediately (returns in-bounds as today). A real change `(old != active)`
  is recorded: inside an open **stroke** it accumulates a `TileChange(Cell, Before, After)`; with no open stroke
  it **auto-wraps** as a one-cell command (so a lone `session.Paint(...)` — what the existing tests call — is one
  undo). A no-change paint pushes nothing.
- `BeginStroke()` / `EndStroke()` (NEW, neutral, void) — the host brackets a click-drag. `BeginStroke` opens
  `_openStroke=new List<TileChange>()` (defensively committing any already-open stroke); `EndStroke` commits ONE
  command reverting/replaying all accumulated changes iff ≥1, else discards.
- `SelectTileset(string)->bool` — on a real change records `Apply: Map.TilesetName=new` / `Revert: =old`.
- `AddEvent(Cell)->EventView` — records ONLY when it actually adds (the place-or-**select** case is selection, not
  a document edit): `Apply: insert the captured EventData at its index` / `Revert: remove that instance`.
  `_eventSeq` is NOT decremented on undo (ids stay monotonic; redo re-adds the **same** instance/id).
- `UpdateSelected(...)->void` — on a real change records old vs new `(Trigger,Kind,Params)` of the target instance.
- `RemoveSelected()->bool` / `RemoveEventAt(Cell)->bool` — capture `(EventData, index)`; `Revert` re-inserts at
  the same index (order preserved); `Apply` removes the instance.
- `Undo()`/`Redo()` (session) wrap `_history.Undo()/Redo()` and **clear `_selected`** (selection is view state;
  this prevents a dangling reference to a removed event). `CanUndo`/`CanRedo`/`HistoryChanged` re-expose `_history`.
- `NewMap` / `Load` (on Ok) call `_history.Clear()` — no undo across a reset.

**Studio host (thin, excluded):** `MainWindow` adds Undo/Redo toolbar buttons + KeyBindings (Ctrl+Z→Undo,
Ctrl+Y & Ctrl+Shift+Z→Redo) and subscribes `session.HistoryChanged` → set the buttons' `IsEnabled` from
`CanUndo`/`CanRedo`, `InvalidateVisual`, `RefreshInspector`. `MapCanvas` calls `BeginStroke` on pointer-press and
`EndStroke` on pointer-release (paint mode only).

### File manifest
  | # | File | Change |
  |---|---|---|
  | 1 | `Engine`/`Editor/EditHistory.cs` (NEW, `MonoRpgMaker.Editor`) | `EditCommand` record + `EditHistory` (undo/redo stacks; `Execute`/`Undo`/`Redo`/`CanUndo`/`CanRedo`/`Clear`; `Changed` event). Gated, neutral. |
  | 2 | `Editor/MapPaintSession.cs` | hold `_history` + `_openStroke`; +`BeginStroke`/`EndStroke`; route Paint/SelectTileset/AddEvent/UpdateSelected/RemoveSelected/RemoveEventAt through inverse-recording (signatures unchanged); +`Undo`/`Redo`/`CanUndo`/`CanRedo`/`HistoryChanged` (clear `_selected` on undo/redo); `NewMap`/`Load` clear history. +`TileChange` record struct. |
  | 3 | `Studio/MainWindow.cs` | +Undo/Redo buttons + KeyBindings (Ctrl+Z / Ctrl+Shift+Z / Ctrl+Y); subscribe `HistoryChanged` → button enablement + `InvalidateVisual` + `RefreshInspector`. |
  | 4 | `Studio/MapCanvas.cs` | bracket a paint stroke — `BeginStroke()` on pointer-press, `EndStroke()` on pointer-release (paint mode). |
  | 5 | `tests/MonoRpgMaker.Engine.Tests/UndoRedoTests.cs` (NEW) | T1–T10 below. |

### Regression Test Plan
  | # | Test | Proves Requirement |
  |---|---|---|
  | T1 | stroke-paint a cell → `CanUndo` true; `Undo()` true → `TileAt` back to original | REQ-001 |
  | T2 | after Undo → `CanRedo` true; `Redo()` true → `TileAt` = the painted tile | REQ-002 |
  | T3 | edit A, edit B, `Undo()`, then a new edit C → `CanRedo` false (redo stack cleared) | REQ-003 |
  | T4a | `BeginStroke` + paint 3 distinct cells + `EndStroke` → one `Undo()` reverts ALL three | REQ-004 |
  | T4b | a stroke that paints the SAME tile already there (no change) → `CanUndo` false (nothing pushed) | REQ-004 |
  | T5a | `SelectTileset("lpc-grass")` → `Undo` restores `ActiveTileset=="lpc-mountains"` → `Redo` restores grass | REQ-005 |
  | T5b | `AddEvent` → `Undo` → `EventCount==0` + `SelectedEvent` null; `Redo` → `EventCount==1`, same id | REQ-005 |
  | T5c | `AddEvent`+`UpdateSelected(text="X")` → `Undo` restores the old params → `Redo` re-applies "X" | REQ-005 |
  | T5d | two events, `RemoveEventAt(first)` → `Undo` re-inserts at index 0 (order + count restored) → `Redo` removes | REQ-005 |
  | T6 | make an edit, then `NewMap` → `CanUndo`/`CanRedo` false; (separately) a successful `Load` → both false | REQ-006 |
  | T7 | fresh session → `Undo()`/`Redo()` return false, no throw, `CanUndo`/`CanRedo` false | REQ-007 |
  | T8 | a lone `Paint(cell)` with NO `BeginStroke` (the #19/#20 call shape) is auto-wrapped → one `Undo` reverts it | REQ-001/004 |
  | T9 | `HistoryChanged` fires on Execute / Undo / Redo / Clear (subscribe + count raises) | REQ-001/006 mutation |
  | T10 | (regression) the existing #19/#20 `MapPaintSession`/event/tileset tests stay green (additive history) | REQ-009 |
  | — | host smoke (MANUAL): Studio → paint a stroke → Undo button + Ctrl+Z revert it; Redo replays; buttons grey when empty | REQ-008 |
  | — | FULL `bin/gate.sh` green; Editor coverage + MSI ≥ 80 (EditHistory + the per-op inverse logic) | REQ-009 |

  Excluded (host, manual smoke): the Avalonia buttons / KeyBindings / stroke pointer-bracketing
  (`MainWindow`/`MapCanvas`, `[ExcludeFromCodeCoverage]`). The history logic they drive is gated + tested (T1–T9).

### Risks / decisions
- **Signature preservation (load-bearing):** every existing op keeps its signature + observable result; history is
  additive. The #19/#20 tests are the canary (T10) — they must stay green with zero edits.
- **Selection cleared on undo/redo (decided):** undo/redo restore the *document* (tiles/events/tileset name), not
  selection; clearing `_selected` avoids a dangling reference to an undone-away event. A future refinement could
  preserve selection when the undone op doesn't touch it — out of scope.
- **Same-instance re-add (decided):** remove/add commands re-insert the SAME `EventData` instance at its captured
  index, so later commands referencing it (e.g. an `UpdateSelected` recorded earlier) stay valid; `_eventSeq` is
  never decremented (monotonic ids; redo reuses the id).
- **Stroke contract:** the host must pair `BeginStroke`/`EndStroke`; a lone `Paint` auto-wraps (total), and
  `BeginStroke` defensively commits an already-open stroke. Abandoning a stroke (never calling `EndStroke`) leaves
  its applied tiles ungrouped — a host-misuse edge, not reachable from the real canvas wiring.
- **`Tile` value-equality** (`old != _active` / `.Equals`) drives "real change" detection — `Tile` is a record
  struct (value equality); implement confirms.
- **MRM/§14:** `EditHistory`/`MapPaintSession` are `Editor` (not sim); `Action` delegates + `Stack`/`List` are
  fine; the new public surface is `void`/`bool`/`Cell` + an `EventHandler` event — no XNA `Point` (neutral).

## Phase 3 — Implement
- **Built (per the manifest):**
  - `Editor/EditHistory.cs` (NEW) — `internal sealed record EditCommand(Action Apply, Action Revert)` +
    `internal sealed class EditHistory`: two `Stack<EditCommand>`; `Record`/`Undo`/`Redo`/`CanUndo`/`CanRedo`/
    `Clear` + `event Changed`. Gated, neutral.
  - `Editor/MapPaintSession.cs` — +`_history` + `_openStroke` + a private `TileChange(Cell, Tile Before, Tile
    After)` record struct. `BeginStroke`/`EndStroke` bracket a drag; `Paint` applies + records a real change
    (accumulate into the open stroke, else auto-wrap a one-cell command); `SelectTileset`/`AddEvent`/
    `UpdateSelected`(skip-if-unchanged)/`RemoveSelected`/`RemoveEventAt` route through inverse commands with their
    signatures UNCHANGED; a `RemoveEvent` helper captures the list index for an order-preserving re-insert;
    `Undo`/`Redo` clear `_selected`; `CanUndo`/`CanRedo`/`HistoryChanged` re-expose `_history`; `NewMap`/`Load`
    `Clear()` the history.
  - `Studio/MapCanvas.cs` — `BeginStroke()` on pointer-press / `EndStroke()` on pointer-release (paint mode).
  - `Studio/MainWindow.cs` — Undo/Redo toolbar buttons (enabled per `CanUndo`/`CanRedo`) + a `KeyDown` handler
    (Ctrl **or** ⌘ + Z / Shift+Z / Y) + `HistoryChanged` → `RefreshHistory` (button enablement + `InvalidateVisual`
    + `RefreshInspector`).
- **Build / verify:** whole solution `-warnaserror` → **0 / 0** — the **neutral boundary HELD** (the new
  `Undo`/`Redo`/`BeginStroke`/`EndStroke`/`CanUndo`/`CanRedo`/`HistoryChanged` surface is XNA-free). **394/394
  tests pass** — the #19/#20 `MapPaintSession`/event/tileset suite stayed green **untouched** (the
  signature-preservation canary). **GATE GREEN [fast]** — 11/11.
- **Deviations from design (+ reason):**
  1. **`EditHistory.Record(cmd)` logs WITHOUT applying** (design said `Execute` applies). The ops perform their
     edit directly (paints need immediate visual feedback; `AddEvent` returns the placed view), then record the
     inverse — re-applying on record would **double-add** for non-idempotent ops like `AddEvent`. "Do, then record
     the inverse" is the clean uniform pattern; redo replays via `Apply`, undo via `Revert`.
  2. `ParamsEqual` params typed as concrete `Dictionary<string,string>` (not `IReadOnlyDictionary`) — **CA1859**
     under `-warnaserror`; both call sites pass concrete dictionaries.
  3. Key handling via a window `KeyDown` handler (not an Avalonia `KeyBinding`/`ICommand`) — simpler, and it
     supports Ctrl (Win/Linux) **and** ⌘/Meta (macOS). Host, excluded, manual smoke.
  4. `Paint` inlines the in-bounds guard + `GetTile` (to capture the *before* tile for the delta) and reuses
     `MapEditor.Paint` for the write.
- **NOT yet done (Phase 4 — Validate):** `tests/.../UndoRedoTests.cs` (T1–T10) + the FULL gate (coverage +
  mutation). Inspect (Phase 3.5) runs next.

## Inspect (Phase 3.5)
- **Lenses run:** three independent critics were spawned (correctness/totality, design/MRM/neutral,
  simplification/reuse) but **all three died on transient API errors (529 Overloaded / 500)** after reading the
  code — no synthesised reports. Fell back to a rigorous author self-review + a **throwaway repro** (5 cases) that
  CONCRETELY exercised the round-trip + edges via `dotnet test`, then deleted it (§15 transcript-is-truth).
- **Findings:**
  | # | Severity | Finding (file:line) | Verdict | Fix |
  |---|---|---|---|---|
  | 1 | **MAJOR** | `MapPaintSession.ApplyTiles` iterated the batched stroke FORWARD for BOTH redo and undo. A cell recorded twice in one stroke (reachable via `BeginStroke`/`SelectTile(X)`/`Paint(c)`/`SelectTile(Y)`/`Paint(c)`/`EndStroke`) **unwinds to the intermediate tile on undo, not the original**. | **REAL** — repro-proven (undo left tile 5, expected original empty). `BF-undo-batch-reverse-order-001`. | **FIXED** — `ApplyTiles` unwinds in **REVERSE** on undo (last change reverted first); redo stays forward. Repro green. |
- **Cleared (verified — repro 4/4 + the 394 suite, not just unflagged):** round-trip exactness (stroke = one undo,
  lone-paint auto-wrap, `AddEvent` ordering through undo×3/redo×3, `RemoveEvent` index re-insert, redo-clear,
  `NewMap` reset, empty no-op totality); **signature preservation** (394 green untouched; `UpdateSelected`
  skip-if-unchanged is safe — the #19 params-copy test uses *changed* values → records+copies); **neutral
  boundary** (build 0/0; `EditHistory`/`EditCommand` `internal`; the new surface is `void`/`bool`/`Cell` +
  `EventHandler`); **MRM/§14** (Editor not sim → `Stack` + `foreach`-over-Dictionary in `ParamsEqual` allowed,
  the `-warnaserror` build proves the analyzer didn't fire); **immutability** (the stroke `List` is captured AFTER
  `_openStroke` is nulled in `EndStroke`, so it's never mutated again; closures capture finalised locals); the
  **do-then-Record** deviation is sound (no double-add — the `EventOrder` repro is green).
- **Post-fix verify:** build `-warnaserror` 0/0; 394 tests; **GATE GREEN [fast]**.
- **Capture:** `failure-record` **BF-undo-batch-reverse-order-001** (`ae1f4fee`); `prevention-rule-record`
  **PR-claude-batched-undo-reverse-unwind-001** (`847d8020`) — a batched undo unit must unwind in reverse.
- **Phase-4 kill-list (adopted):** T1–T10 from the design **+ T-edge (the regression guard): a stroke painting
  the SAME cell twice with different tiles → undo restores the ORIGINAL** (the exact case the repro caught) — use
  isolated exact-value asserts.

## Phase 4 — Validate
- **Tests added:** `tests/MonoRpgMaker.Engine.Tests/UndoRedoTests.cs` — **16 tests** (isolated exact-value
  asserts): T1 undo-restores · T2 redo-reapplies · T3 new-edit-clears-redo · T4a stroke=one-undo · T4b
  no-change/empty-stroke-no-push · **T-EDGE same-cell-twice → undo restores the ORIGINAL** (the inspect
  regression guard, kills the forward-unwind mutant) · T5a tileset undo/redo · T5b AddEvent undo/redo + same-id +
  selection cleared · T5c UpdateSelected undo/redo · T5c-skip unchanged-records-nothing · T5d RemoveEvent
  order-preserving re-insert · T6 NewMap-clears + Load-clears · T7 empty no-op totality · T8 lone-Paint auto-wrap ·
  T9 HistoryChanged fires on record/undo/redo/clear. (T10 — the #19/#20 suite stays green — confirmed by the full
  run, untouched.)
- **`dotnet test MonoRpgMaker.slnx`:** `Passed!  Failed: 0, Passed: 417, Skipped: 0`.
- **`bin/gate.sh` (FULL):** **GATE GREEN [full] — 13/13.** coverage ≥ 80; mutation **Engine 86.54 / Abstractions
  82.22 / Analyzers 91.18 / Editor 85.98** (up from 84.95 — the `EditHistory` + per-op inverse + the reverse-unwind
  killed clean) — all ≥ 80; .expect oracle 5 rows / 3 modules — OK. Receipt written
  (`.git/monorpgmaker-gate-receipt`).
- **Pre-existing exclusions:** none.

## Phase 5 — Complete
- **Docs updated:** `docs/roadmap.md` (undo/redo = Studio-v2 slice 3 shipped; multi-map remains);
  `docs/product-phasing.md` (the map-paint editor row). **No `decisions.md` entry** — undo/redo is a feature with
  an internal command/history pattern, not a new cross-cutting architecture decision (D-0022..D-0025 stand). The
  trigger-program intake was left untouched (different arc).
- **Forge capture:** `aar-submit` → aar `72dc6f7d` closed (outcome completed, effectiveness 4; 2 novel findings,
  12 verdicts; distillation/confidence/pattern jobs enqueued). Materialized the inspect captures —
  `failure-record` **BF-undo-batch-reverse-order-001** (`ae1f4fee`, the forward-unwind bug) +
  `prevention-rule-record` **PR-claude-batched-undo-reverse-unwind-001** (`847d8020`, a batched undo unwinds in
  reverse). No new architecture-decision-record.
- **Ticket closed:** #21 (`a27cc647`) → `done`; local doc moved `docs/planning/tickets/{open → closed}/`.
- **Archived:** `WORK-studio-undo-redo.{spec,notes}.md` → `docs/planning/pipeline/completed/`.
