# WORK-p0-triggers — Notes

## Phase 1 — Plan
- **Request:** P0 #13 — (A) an `ActionButton` trigger + player `Facing`, (B) an ordered, ambiguity-checked
  hook lifecycle replacing WorldSim's implicit insertion-order dispatch. **Autonomous goal run** (#13 of
  #11–#15): all phases auto-approved → `/commit` + merge.
- **Classification:** work pipeline, feature. Seam additions (EventTrigger/IMapEvent.Order) + the WorldSim
  dispatch + a dispatcher/schedule + Facing. One slice, medium — MVP (Autorun/Parallel + module-registration
  deferred).
- **Ground (verified):** `EventTrigger = {StepOn}`; `IMapEvent` has no order; `WorldSim.FireStepOn` iterates
  `_events` in insertion order; `Actor` has no Facing; **`Direction` + `DirectionExtensions.ToStep()→Point`**
  in Engine.World; `GridPoint` has `+`/`-`. **Abstractions targets net10.0 ⇒ default-interface-members work**
  (so `int Order => 0;` is non-breaking).
- **Recall:** agentic-substrate §1 (Abstractions is the semver'd seam; **compile-time wiring, not reflection**,
  D-0021; sim deterministic) + §2 (the hook lifecycle: ordered subscribers, ambiguity = build error);
  feature-research (extensibility = visual events + a typed plugin registry). Reuse the totality pattern
  (`PR-claude-total-parser-substring-overlap-001`, `PR-claude-long-product-dimension-guard-001`).
- **Ticket:** 0e3ab789-cc7f-48f5-ae0e-fc0854e8ee85 (#13) — project a9ee8162 confirmed monorpgmaker.
- **AAR id:** b1e4e334-b1bb-4247-8ab3-2e48533bdc89
- **EARS reviewed:** REQ-001..008.

## Phase 2 — Design

### Architecture / approach
- **Facing already exists** — `Entity.Facing` (default `Down`) and `Entity.TryStep` sets `Facing = direction`
  BEFORE the block check, so `WorldSim.MovePlayer → Player.TryStep` already turns-in-place on a blocked move.
  **REQ-002 needs NO new code** — a Phase-4 test guards it.
- **Seam (Abstractions, additive):** `EventTrigger += ActionButton`; `IMapEvent` gains `int Order => 0;` as a
  **default-interface member** (net10 ⇒ supported; LeverEvent/ChestEvent/the scaffolded skeleton keep
  compiling at Order 0 — the #10 oracle + scaffolder stay green).
- **`HookSchedule` (Engine.Sim) — ordered + ambiguity-checked, total + MRM-clean:**
  - `static ScheduleResult Build(IReadOnlyList<IMapEvent> events)` — detects AMBIGUITY (2+ events sharing the
    same `(Cell, Trigger, Order)`) and returns a typed `Failure(reason)`; else `Success(schedule)`. **No
    throw on the validation path** (mirrors `ParseResult`/`MapLoadResult`).
  - **MRM-safe ambiguity scan (NO Dictionary/HashSet foreach):** copy to a `List`, `List.Sort` by
    `(Cell.X, Cell.Y, Trigger, Order)`, scan adjacent pairs — equal `(Cell,Trigger,Order)` ⇒ ambiguous.
    Deterministic + analyzer-clean (sort a list; never enumerate a dict for output).
  - `IReadOnlyList<IMapEvent> EventsAt(GridPoint cell, EventTrigger trigger)` — filter the stored list by
    cell+trigger, `Sort` by ascending `Order` (unique within a group ⇒ total order), return.
- **`WorldSim` — `TryCreate` is the entry; ambiguity is a typed result:**
  - `static WorldSimResult TryCreate(map, player, events, doors)` — `ArgumentNullException.ThrowIfNull` on each
    (programmer error STAYS a throw); `HookSchedule.Build(events)` → on `Failure` return
    `WorldSimResult.Failure(reason)`; else `new WorldSim(map, player, schedule, doors)` (PRIVATE ctor).
  - `FireStepOn(cell)` → `foreach (e in _schedule.EventsAt(cell, StepOn)) _applier.Apply(e.Run(ctx))` (ordered).
  - `bool PressAction()` — clears `CurrentMessage`; faced cell = `Player.Cell + Player.Facing.ToStep()`;
    `foreach (e in _schedule.EventsAt(facedCell.ToGridPoint(), ActionButton)) Apply(...)`; returns whether any
    fired. A step never fires `ActionButton`; `PressAction` never fires `StepOn` (the trigger gates the group).
  - `WorldSimResult` (`Sim?`/`Error?`/`Ok`/`Success`/`Failure`) mirrors the result pattern.
- **Determinism:** per-(cell,trigger) order is total (equal Order ⇒ Error, no ties survive) ⇒ replay
  bit-identical; MRM analyzer stays green over WorldSim/HookSchedule.

### File manifest
| File | Change |
|---|---|
| `src/MonoRpgMaker.Abstractions/EventTrigger.cs` | MODIFY — add `ActionButton` (after `StepOn`) |
| `src/MonoRpgMaker.Abstractions/IMapEvent.cs` | MODIFY — add `int Order => 0;` default-interface member (XML-doc) |
| `src/MonoRpgMaker.Engine/Sim/HookSchedule.cs` | NEW — `HookSchedule` (`Build`/`EventsAt`) + `ScheduleResult` (list-sort ambiguity + ordering) |
| `src/MonoRpgMaker.Engine/Sim/WorldSim.cs` | MODIFY — private ctor takes `HookSchedule`; `static TryCreate → WorldSimResult`; `FireStepOn`/new `PressAction` via the schedule; add `WorldSimResult` |
| `src/MonoRpgMaker.Engine/Sim/Tracer/TracerRoom.cs` | MODIFY — `Build()` unwraps `WorldSim.TryCreate(...).Sim` (known-good; guard-throw if somehow ambiguous) |
| `tests/MonoRpgMaker.Engine.Tests/TracerSliceTests.cs` | MODIFY — the 4 null-guard tests (`new WorldSim(null!,…)`) → `WorldSim.TryCreate(null!,…)` (still `ArgumentNullException`) |
| `tests/MonoRpgMaker.Engine.Tests/HookDispatchTests.cs` | NEW (Phase 4) — the plan below |

### Regression Test Plan
| T | REQ | Test (xUnit; isolated exact-count asserts) |
|---|---|---|
| T1 | 001 | An `ActionButton`-trigger event is reachable + dispatched (covered by T3); the enum value exists. |
| T2 | 002 | After a BLOCKED `MovePlayer` into a wall, `Player.Facing` == the attempted direction (turn-in-place). |
| T3 | 003 | `PressAction` with an `ActionButton` event at the faced cell → its outcome applies (observable via `CurrentMessage`/state); returns true. |
| T3b | 003 | A `StepOn` event at the faced cell does NOT fire on `PressAction`; an `ActionButton` event is NOT fired by a step onto its cell. |
| T4 | 004 | Two events at the SAME cell+trigger with Order 1 and Order 0 → applied in ascending Order (assert the observable order, e.g. two `ShowMessage`s / the final `CurrentMessage`). |
| T5 | 005 | Two events, same cell+trigger, EQUAL Order → `TryCreate` returns `!Ok` + Error names ambiguity. **Control:** same Order but DIFFERENT cell → `Ok`; same Order, same cell, DIFFERENT trigger → `Ok`. |
| T6 | 005 | `HookSchedule.Build` directly: ambiguous set → `Failure`; `EventsAt` returns ascending-Order list; unknown cell → empty. |
| T7 | 003/004 | `TryCreate(null!, …)` (each of 4 args) → `ArgumentNullException` (the migrated null-guard tests). |
| T8 | 007 | `TracerSliceTests` stay green (StepOn movement/lever/chest/door unchanged). |
| — | 006/008 | MRM-clean build `-warnaserror`; FULL `bin/gate.sh` green (Engine MSI ≥ 80). |
- **Engine MSI ≥ 80:** the ambiguity adjacent-pair branch (T5/T6 — distinct + control cases kill the
  `==`/group-equality mutants); the `EventsAt` filter (cell AND trigger match) + the `Order` sort (T4 ascending,
  T3b trigger-gating); `TryCreate` Ok vs Failure (T3 ok-path, T5 fail-path); `PressAction` faced-cell offset +
  the "any fired" return (T3/T3b). Isolated exact-count asserts (PR-claude-analyzer-mutation-isolated-asserts-001).

### Risks / decisions
- **WorldSim ctor → private + `TryCreate`** is the one structural change. Blast radius is small + known: `TracerRoom.Build`
  (unwrap) + the 4 `TracerSliceTests` null-guard lines (→ TryCreate). Enforces "ambiguity always caught" (no
  silent-tiebreak back door). `Game1.cs` uses `TracerRoom.Build()` (unaffected).
- **MRM over HookSchedule** — the list-sort+scan avoids any Dictionary/HashSet enumeration; the implementer
  confirms via `-warnaserror`. If a `HashSet` collision-check is cleaner AND the analyzer permits lookup-only
  use, that's acceptable; the list approach is the safe default.
- **DEFERRED follow-up (note for a future ticket):** Autorun/Parallel triggers (need an update/tick +
  active-condition design); the agent-authored module-registration/subscription system.

## Phase 3 — Implement
- **Built (to the manifest):**
  - Abstractions: `EventTrigger += ActionButton`; `IMapEvent` gains `int Order => 0;` (default-interface
    member — additive, LeverEvent/ChestEvent/the scaffolded skeleton compile unchanged at Order 0).
  - `Sim/HookSchedule.cs` — `HookSchedule.Build` (list-`Sort` by (Cell.X,Cell.Y,Trigger,Order) + adjacent-pair
    scan → typed `ScheduleResult` Failure on equal (cell,trigger,order); no Dictionary/HashSet) + `EventsAt`
    (filter by cell+trigger, `Sort` by Order). `ScheduleResult` mirrors `ParseResult`/`MapLoadResult`.
  - `Sim/WorldSim.cs` — ctor now **private** (takes the `HookSchedule`); `static TryCreate → WorldSimResult`
    (null-guards throw; ambiguity → typed Failure); `FireStepOn` → unified `FireHooks(cell, trigger)`; new
    `PressAction()` (faced cell = `Player.Cell + Player.Facing.ToStep()`, fires ActionButton in order,
    SyncDoors, returns whether any fired). `WorldSimResult` added.
  - `Sim/Tracer/TracerRoom.cs` — `Build()` unwraps `WorldSim.TryCreate(...).Sim` (known-good; guard-throw).
- **REQ-002 needed NO code** — `Entity.Facing` + `TryStep` (sets Facing before the block check) already turn
  the player in place on a blocked move; a Phase-4 test guards it.
- **Required test migration (build-green, not new authoring):** 6 direct WorldSim constructions → `TryCreate(…).Sim!`
  — the 4 null-guard tests (→ TryCreate, still `ArgumentNullException`) PLUS **2 target-typed `new(...)` helpers**
  (`Sim`, `ChestSim`) the initial `new WorldSim(` grep missed (they used `new(...)`).
- **Build `-warnaserror` 0/0** (MRM-clean — the list-sort dispatch is analyzer-clean). The **11 existing
  tracer/sim tests pass** (StepOn/lever/chest/door/null-guards preserved).
- **Deviations:** unified `FireStepOn` → `FireHooks(cell, trigger)` (DRY — shared by StepOn + ActionButton);
  `PressAction` also `SyncDoors()` (a door-affecting action button re-syncs, consistent with `MovePlayer`).
  The `SimGuardTests.WorldSim_Ctor_NullArgs_Throw` name is now slightly stale (tests `TryCreate`) — left as-is
  (harmless; inspect may rename).

## Inspect (Phase 3.5)
- **Lenses:** 2 critics — (1) correctness/determinism (general-purpose, **built + ran 12 probes** with a
  recording `IMapEvent`), (2) design/purity/mutation (read-only).
- **Both promises VERIFIED:** ambiguity detection **correct + total** (catches every equal-(Cell,Trigger,Order)
  collision incl. interleaved + 3-way; controls — differ-in-cell / differ-in-trigger / differ-in-order → Ok),
  and the dispatch is **deterministic** (200 random permutations → identical order). The **`List.Sort`
  instability is a non-issue**: equal (Cell,Trigger,Order) tuples are exactly the rejected-ambiguous set, and
  `EventsAt` re-sorts a group whose Orders are unique post-Build (zero ties). PressAction fires ActionButton at
  the faced cell, never on a step; a step never fires ActionButton; Facing-on-blocked-move works. Totality
  holds — only null-guards throw; `int.MinValue/MaxValue` Orders sort fine (comparator uses `CompareTo`, not
  subtraction → no overflow).

  | # | Sev | Finding | Verdict |
  |---|---|---|---|
  | F1 | LOW | Passing the **same `IMapEvent` instance twice** at one cell+trigger+order is rejected with the generic "ambiguous … share order" message rather than a "duplicate instance" diagnostic. | **ACCEPTED — not a defect.** Rejection is the correct, safe behaviour (a duplicate would double-dispatch with no defined order); the message refinement (`ReferenceEquals` branch) is optional authoring polish — deferred. |
  | — | — | MRM-clean (list-sort, no Dictionary/HashSet foreach); nullable + XML-doc; `ScheduleResult`/`WorldSimResult` mirror `ParseResult`; `FireHooks` unification + `PressAction.SyncDoors` justified; refactor complete (Game1→TracerRoom.Build→TryCreate; RpgGame takes a pre-built sim; no stray `new WorldSim`/`new(...)`); seam additive (LeverEvent/ChestEvent/scaffolder inherit Order 0; #10 oracle builds). | **VERIFIED CLEAN** (both critics) |

- **Phase-4 Engine-MSI ≥ 80 kill-list (critic 2, 17 mutants):** the ambiguity `&&` chain (each of
  Cell/Trigger/Order equality needs a control flipping ONLY that field); the 4-key sort comparator (each
  tie-break level); `EventsAt`'s `Cell && Trigger` filter (both conjuncts) + the Order sort; `TryCreate` Ok vs
  Failure; `PressAction` faced-cell offset + the `fired` bool + **`SyncDoors`** (needs an **ActionButton event
  that flips a door switch** to kill — Phase 4 must add that scenario, else it's ~1 equivalent survivor);
  `MovePlayer` still `FireHooks(StepOn)` + `SyncDoors`. Isolated exact-count asserts
  (PR-claude-analyzer-mutation-isolated-asserts-001).
- **Optional rename (defer):** `SimGuardTests.WorldSim_Ctor_NullArgs_Throw` now tests `TryCreate`.
- **Forge:** no `failure-record` — no real bug found (both promises held under execution).

## Phase 4 — Validate
- **Tests added (`HookDispatchTests`, 19 + a `RecordingEvent` double):** Build ambiguity (same cell+trigger+order
  → !Ok) + **3 controls** (differ-in-cell / -trigger / -order → Ok, each killing one `&&` conjunct) + 3-way +
  empty + null-throws; `EventsAt` ascending-order + unknown-cell-empty + cell-AND-trigger filter; WorldSim
  ordered StepOn dispatch (orders [2,0,1] → log "0","1","2"); `TryCreate` ambiguous → Failure; PressAction at
  the faced cell fires (a) but not a StepOn there (b), a step doesn't fire ActionButton (c), and not the
  player's own cell (d, kills the `+Facing.ToStep()` offset); Facing-on-blocked-move; **PressAction re-syncs
  doors** (an ActionButton event sets a switch → the door opens — kills the SyncDoors mutant); ActionButton
  enum exists.
- **`dotnet test`: 255 passed, 0 failed** (+19).
- **`bin/gate.sh`: GREEN [full]** — 13 gates. Coverage **94.9%**; MSI **Engine 87.67%** (↓ from 92.11 — the new
  HookSchedule/WorldSim dispatch added surface; still ≥ 80) **/ Abstractions 82.22% / Analyzers 91.18% / Editor
  83.97%**. Receipt written. The 11 existing tracer tests stay green (StepOn preserved).
- **Equivalent mutants (documented):** the ambiguity error-message string; the same-instance-twice diagnostic
  text. None affect the ≥80 floor.
- **Pre-existing exclusions:** none. No new packages.

## Phase 5 — Complete
- Docs / forge / ticket / archive:
