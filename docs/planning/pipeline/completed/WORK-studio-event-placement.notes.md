# WORK-studio-event-placement — Notes

Per-phase working notes for the paired `.spec.md`. The spec is the contract;
this is the record of what happened.

## Phase 1 — Plan
- **Request:** Studio v2.1 — event placement in the map editor. Place/select an event on a tile in the Studio
  painter, configure ShowText (type the line), Save → it writes the **#18 `$data` events** the runtime already
  loads + dispatches. The first slice of the 3-slice Studio-v2 arc (event placement → painter polish → multi-map);
  = Slice 3 of the trigger program.
- **Intake source:** `docs/planning/intake/INTAKE-event-trigger-system.md` (Slice 3 — "UI placement (Studio)").
  Already `status: promoted` for Slice 1 (#18); this run adds the Slice-3 ticket pointer in the body (no
  re-promote).
- **Classification / tier:** larger **feature**, one vertical slice (a work pipeline). Editor-logic + Avalonia
  host; builds straight on #16 (Studio/`MapPaintSession`) + #18 (the `$data` events + `BehaviourRegistry`).
- **Forge recall (lessons/failures surfaced):**
  - **#16 neutral-boundary (load-bearing):** `MapPaintSession` (Editor) is Avalonia-free; its **public API must
    stay framework-neutral** (`Cell`/`int`, never XNA `Point`) — the Engine's MonoGame ref is `PrivateAssets=All`,
    so a `Point` in a public surface fails to compile in Studio (CS0234). The new event API stays neutral.
  - Reuse: PR-claude-editor-view-math-into-tested-layer (the tested view-model layer), the neutral-boundary,
    PR-claude-validate-deserialized-elements (total element validation — already in #18's `MapSerializer`),
    PR-claude-analyzer-mutation-isolated-asserts (isolated exact-count test asserts).
  - **Layering verified:** `MonoRpgMaker.Editor` → `MonoRpgMaker.Engine` (ProjectReference) — so the Editor can
    read `EventData` (Engine.World) **and** `BehaviourRegistry` (Engine.Sim). The kind-descriptor can live on the
    registry (Engine.Sim) or in Abstractions — **design decides** (keep MRM-clean; don't break the ref direction).
  - `MapPaintSession` today: `Save()` = `Serialize(Map)` (events-blind); `Load()` swaps the map but **drops**
    `MapLoadResult.Events`. Both must learn about events.
- **Ticket:** #19 — `8374d948-68fe-4b80-b90b-3bcd0af0139c` (project monorpgmaker).
- **AAR id (from `aar-open`):** `a5e9a2f1-3ac4-4cc2-b69e-ae990f3546c3`.
- **EARS requirements reviewed:** REQ-001 (add-at-cell, neutral) · REQ-002 (select/update/remove) · REQ-003
  (save/load with events round-trip) · REQ-004 (single kind-descriptor source) · REQ-005 (editor output is
  runtime-valid → real `IMapEvent`) · REQ-006 (the Studio event-tool + inspector + marker — manual smoke) ·
  REQ-007 (FULL gate green, Editor MSI≥80).
- **OPEN for design:** where the kind-descriptor lives (Abstractions vs a `BehaviourRegistry` accessor that
  enumerates kinds+param-keys — MRM-clean, no foreach-over-Dictionary); how the session stores events (hold
  `EventData` directly — a plain DTO — but keep the **public** accessors neutral; maybe a neutral `EventView`
  record for the inspector); deterministic id auto-gen (e.g. `event-1`/`event-2` by count — no Guid/clock); the
  marker visual + selection highlight (host).

## Phase 2 — Design

### Approach / architecture
Three layers, contract-first (D-0024): the editor edits **placement data** (`EventData`); the runtime
materialises behaviours via the registry. Nothing in the editor constructs an `IMapEvent`.

**1. The kind-descriptor (single source) — `Engine.Sim/BehaviourRegistry.cs`.** Today only the factory is
registered. Refactor to ONE registration table so the editor's dropdown/params and the runtime's materialisation
can never drift:
- NEW `public sealed record BehaviourKindInfo(string Name, IReadOnlyList<string> ParamKeys);` (Engine.Sim — the
  Editor + Studio both ref Engine; the type carries NO XNA, so it's neutral-boundary-safe).
- A private `Registration(string Name, IReadOnlyList<string> ParamKeys, Func<…,BehaviourResult> Factory)` array —
  the SINGLE source. `ShowText` → `("ShowText", ["text"], <the existing factory>)`.
- `Factories` (the existing `Dictionary` read by `TryMaterialize`) is BUILT from the array (a `foreach` over the
  **array** — MRM-allowed, only Dictionary/HashSet *enumeration* is banned; writing `d[r.Name]=r.Factory` is a
  write, not an enumeration). `TryMaterialize` is UNCHANGED → #18 tests + the gate:13 oracle stay green.
- NEW `public static IReadOnlyList<BehaviourKindInfo> Kinds { get; }` — built from the same array.

**2. The neutral event API — `Editor/MapPaintSession.cs` (gated coverage+mutation; NOT MRM-gated, so LINQ/foreach
are fine; only the PUBLIC surface must stay XNA-free — the #16 CS0234 rule).** Internally hold
`List<EventData> _events` + `EventData? _selected` + a monotonic `int _eventSeq`. NEW neutral type
`public readonly record struct EventView(Cell Cell, string Id, string Trigger, string Kind, IReadOnlyDictionary<string,string> Params);`
(Cell, never Point). Public API (Cell/int/string only):
- `EventView AddEvent(Cell cell)` — if an event already occupies the cell, **select it** (one event per cell,
  v1 UX, no duplicate); else create `EventData{ Id=$"event-{++_eventSeq}", X,Y, Trigger="ActionButton",
  Kind="ShowText", Params={} }`, add, select it. Deterministic id (monotonic counter — NO Guid/clock).
- `EventView? SelectEvent(Cell cell)` — find the event at the cell (LINQ), set `_selected` (or null for an empty
  cell); returns `SelectedEvent`.
- `EventView? SelectedEvent` — a neutral snapshot of `_selected` (null when none).
- `void UpdateSelected(string trigger, string kind, IReadOnlyDictionary<string,string> @params)` — replace the
  selected event's Trigger/Kind/Params (Params copied into a fresh `Dictionary`); no-op if no selection (total).
- `bool RemoveSelected()` / `bool RemoveEventAt(Cell cell)` — remove + clear `_selected` if it was the one.
- `IReadOnlyList<Cell> EventCells` (for the canvas markers) · `int EventCount` · `IReadOnlyList<BehaviourKindInfo>
  AvailableKinds => BehaviourRegistry.Kinds` (the host reads kinds THROUGH the session — stays thin).
- `Save()` → `MapSerializer.Serialize(Map, _events)` (was events-blind). `Load(json)` → on Ok, swap the map AND
  `_events = new List<EventData>(result.Events)`, clear `_selected`, advance `_eventSeq` past any loaded
  `event-N` id (collision-free). `NewMap` clears `_events`/`_selected`/`_eventSeq`.

**3. The Studio host (`[ExcludeFromCodeCoverage]`).** `MainWindow` — a Paint⇄Event mode toggle (a `ToggleButton`
in the toolbar) + an **inspector** dock (right): a Trigger `ComboBox` (StepOn/ActionButton), a Kind `ComboBox`
(from `session.AvailableKinds`), a param editor (for ShowText: a multiline `TextBox` bound to `Params["text"]`),
and a Delete button; field edits → `session.UpdateSelected(...)`. `MapCanvas` — a `bool EventMode` + an
`event EventHandler? EventChanged`; in event mode a pointer-press → `AddEvent`/`SelectEvent(CellAt(...))` (not
Paint), raises `EventChanged` (the window refreshes the inspector); `Render` draws a gold inset marker at each
`session.EventCells` and a brighter highlight on `SelectedEvent`'s cell. Save/Load already wired — now carry
events.

**Layering verified:** Editor→Engine.Sim is permitted (the only arch rules are Engine↛hosts, Sim↛XNA.Graphics,
World/Entities/Data↛Sim, Abstractions↛all). Studio reads `BehaviourKindInfo` (Engine.Sim, no XNA) — neutral-safe.

### File manifest
  | # | File | Change |
  |---|---|---|
  | 1 | `src/MonoRpgMaker.Engine/Sim/BehaviourRegistry.cs` | +`BehaviourKindInfo` record; +`Kinds`; one-source `Registration` table feeding both `Factories` + `Kinds`. `TryMaterialize` unchanged. (gated, MRM-clean) |
  | 2 | `src/MonoRpgMaker.Editor/MapPaintSession.cs` | +`EventView`; hold `_events`/`_selected`/`_eventSeq`; +`AddEvent`/`SelectEvent`/`SelectedEvent`/`UpdateSelected`/`RemoveSelected`/`RemoveEventAt`/`EventCells`/`EventCount`/`AvailableKinds`; `Save`/`Load` carry events; `NewMap` clears. (gated, neutral API) |
  | 3 | `src/MonoRpgMaker.Studio/MainWindow.cs` | +Paint⇄Event toggle; +inspector dock (trigger/kind/params/delete) wired to the session. (host) |
  | 4 | `src/MonoRpgMaker.Studio/MapCanvas.cs` | +`EventMode`; place/select on click in event mode; draw markers + highlight selected; +`EventChanged`. (host) |
  | 5 | `tests/MonoRpgMaker.Engine.Tests/EventPlacementTests.cs` | NEW — the event-model tests + `BehaviourRegistry.Kinds` + the cross-layer validity test. |

### Regression Test Plan
  | # | Test | Proves Requirement |
  |---|---|---|
  | T1 | `AddEvent(c)` → `EventCount==1`, `SelectedEvent` = {Id "event-1", Cell c, Kind "ShowText", Trigger "ActionButton"}; a 2nd `AddEvent` → "event-2"; `AddEvent` at an occupied cell → selects the existing (count unchanged) | REQ-001 |
  | T2 | `SelectEvent(empty)`→null; `SelectEvent(occupied)`→set; `UpdateSelected(t,k,p)`→`SelectedEvent` reflects; `RemoveSelected`→`EventCount==0`+null; `RemoveEventAt(c)` | REQ-002 |
  | T3 | `AddEvent`+`UpdateSelected(text)`→`Save`→`Load` on a fresh session → `EventCount==1` + id/cell/trigger/kind/`Params["text"]` preserved; `NewMap` clears | REQ-003 |
  | T4 | `MapPaintSession.AvailableKinds` (== `BehaviourRegistry.Kinds`) has exactly one "ShowText" with `ParamKeys` == ["text"] | REQ-004 |
  | T5 | editor `Save` → `MapSerializer.Deserialize` → `BehaviourRegistry.TryMaterialize` each → Ok, an `IsType<ShowTextEvent>` (the editor's output is runtime-valid) | REQ-005 |
  | T6 | `Load` events whose ids include "event-3" then `AddEvent` → "event-4" (the counter advances past loaded ids — no collision) | REQ-001/003 |
  | — | host smoke (MANUAL): Studio → Event mode → click a cell → type a line → Save to `content/maps/<x>.json`; run the Player on it → Space → the line | REQ-006 |
  | — | FULL `bin/gate.sh` (Editor coverage+MSI≥80 incl. the event logic; Engine green; Studio exempt) | REQ-007 |

### Risks / decisions
- **Neutral-boundary (the #16 CS0234 trap):** every NEW public member on `MapPaintSession`/`EventView`/
  `AvailableKinds` uses `Cell`/`int`/`string`/`BehaviourKindInfo` — NO XNA `Point`. `BehaviourKindInfo` (Engine.Sim)
  carries no XNA. **Implement verifies the Studio still compiles** (the canary).
- **BehaviourRegistry refactor:** additive — `TryMaterialize` reads `Factories` unchanged, so #18 tests + the
  gate:13 oracle stay green. MRM-clean (foreach over the `Registration[]` array, never the Dictionary).
- **Editor MSI≥80:** the event methods add mutable surface — T1–T3,T6 kill them with isolated exact-count asserts
  (PR-claude-analyzer-mutation-isolated-asserts). **Engine MSI:** the registry refactor — T4/T6 + the #18 suite.
- **Deterministic id** (monotonic `_eventSeq`, advanced on Load) — no Guid/clock (T1/T6).
- **One event per cell** (v1 UX): `AddEvent` on an occupied cell selects rather than stacks — keeps `EventCells`
  unique + `SelectEvent` unambiguous (the runtime `HookSchedule` still permits multiples; the editor just doesn't
  author them yet). Reversible.
- **`Load` totality:** unchanged `MapSerializer` totality; `Load` replacing `_events` never throws.

## Phase 3 — Implement
- **Built (per the manifest):**
  - `Engine.Sim/BehaviourRegistry.cs` — one-source `Registration[]` table (name + param-keys + factory) feeding
    BOTH `Factories` (built via a `foreach` over the **array** — MRM-clean) and the new
    `public static IReadOnlyList<BehaviourKindInfo> Kinds`. `TryMaterialize` UNCHANGED. +`BehaviourKindInfo` record.
  - `Editor/MapPaintSession.cs` — +`EventView` (neutral: `Cell`/string/`IReadOnlyDictionary`); holds
    `_events`/`_selected`/`_eventSeq`; `AddEvent` (place-or-select, deterministic `event-{n}` id),
    `SelectEvent`/`SelectedEvent`, `UpdateSelected`, `RemoveSelected`/`RemoveEventAt`, `EventCells`/`EventCount`,
    static `AvailableKinds`; `Save`→`Serialize(Map,_events)`; `Load` reads `result.Events` + advances the id
    counter past loaded ids; `NewMap` clears.
  - `Studio/MapCanvas.cs` — `EventMode` + `EventChanged`; event-mode click → `AddEvent`; gold marker at each
    `EventCells` + a white border on the selected.
  - `Studio/MainWindow.cs` — a Paint⇄Event `ToggleButton` + an inspector dock (Trigger + Kind dropdowns, a
    multiline Text box, Delete) wired to the session via `RefreshInspector`/`ApplyInspector` (a `_refreshing`
    re-entrancy guard).
- **Build:** whole solution `-warnaserror` → **0/0** — the **neutral boundary HELD** (Studio compiles against the
  `Cell`/`EventView` API, no CS0234). #18 registry/oracle/ShowText tests **21/21 green**; gate:13 oracle **5 rows /
  3 modules — OK** (the registry refactor is additive).
- **Deviations from design (+ reason):**
  1. `AvailableKinds` is **static** (was instance) — CA1822 (`-warnaserror`): it reads the static registry. The
     host calls `MapPaintSession.AvailableKinds`; still read through the tested Editor layer, not Engine.Sim
     directly — the contract-first "single source via the editor" holds.
  2. `BuildKinds` returns `List<>` not `IReadOnlyList<>` (CA1859, a private helper) — `Kinds` stays
     `IReadOnlyList<>` publicly.
- **NOT yet done:** the new `EventPlacementTests` (Phase 4) + the FULL gate — inspect next.

## Inspect (Phase 3.5)
Two parallel critics. Lenses: correctness/totality/build-run; design/contract/MSI.

**Critic 1 — correctness/totality/build-run: ALL 6 items PASS (real output, throwaway run 5/5).**
event model (add/select/update/remove); **place-or-select dedup** (re-add at an occupied cell → count stays 2,
same Id); no-selection `UpdateSelected`/`RemoveSelected` → no throw, no-op; **deterministic id** (event-1/event-2,
no Guid); **Load advances the counter** (load "event-7" → next add "event-8"); save/load round-trip (fields
intact, load clears selection); **cross-validity** (editor Save → `TryMaterialize` → `ShowTextEvent`);
**single-source** (`AvailableKinds` is the SAME reference as `BehaviourRegistry.Kinds`, one ShowText/["text"]);
**neutral boundary** (no `Point` in any event-API public signature). **Zero defects.**

**Critic 2 — design/contract/MSI: production code clean on every claim** (single-source + safe static-init order;
neutral boundary; registry refactor additive + MRM-clean [foreach over the array, never the Dictionary]; §14;
params is a real copy not an alias; no Guid/clock).

**Findings + verdicts:**
| # | Sev | Finding | Verdict |
|---|---|---|---|
| 1 | — | correctness / totality / cross-validity / neutral-boundary (critic 1, all 6) | **PASS — no fix** (verified by running) |
| 2 | "BLOCKER" (critic 2) | "the Editor event API is untested → REQ-007 unmet" | **REJECTED as a phase error** — the implementer does not write the new tests; they are Phase 4 (validate), which runs next. Critic concedes "production code is sound; only the tests are missing." Its 18-item kill-list IS the Phase-4 plan. |
| 3 | useful | the #18 `BehaviourRegistryTests` assert `Factories` (via `TryMaterialize`) but **not** `Kinds` directly → `BuildKinds` could have an Engine-MSI survivor | **ACCEPTED into Phase 4** — add a direct `BehaviourRegistry.Kinds` test (one ShowText/["text"]) |
| 4 | nit | `EventCells`/`SelectedEvent` allocate per-get; one-event-per-cell is a v1 UX choice | accepted — editor, not a hot path; the runtime `HookSchedule` still allows multiples (design-noted) |

**No source fixes (no bugs).** No failure-record. No new prevention-rule (production held; no mistake made).
**Phase-4 kill-list (adopted):** AddEvent (default kind/trigger + the `++_eventSeq` + the **place-or-select
dedup** [count unchanged]) · SelectEvent (found/empty→null) · UpdateSelected (no-op + the 3 field writes + the
**params-copy-not-alias**) · RemoveSelected/RemoveEventAt (found/not + the **`ReferenceEquals` selected-clear**) ·
EventCells/EventCount · Save-with-events · Load (events-copy + **`MaxEventSeq` MAX-not-last** + non-"event-" id →
no-throw + "event-1") · NewMap-clears · cross-validity (REQ-005) · single-source (REQ-004) · **the direct
`Kinds` test** (Engine). The three easy-to-miss: params-copy-not-alias, MaxEventSeq MAX-not-last, place-or-select
dedup.

## Phase 4 — Validate
- **Tests added:** `tests/MonoRpgMaker.Engine.Tests/EventPlacementTests.cs` — `MapPaintSessionEventTests` (the
  18-mutant kill-list: add/default/dedup, select found/empty, update fields + **params-copy-not-alias** + no-op +
  null-guards, remove selected/by-cell + the **`ReferenceEquals` selected-clear**, EventCells/Count, Save-with-events,
  Save→Load round-trip, **`MaxEventSeq` MAX-not-last** + non-"event-" id, NewMap-clears, **cross-validity**
  REQ-005, **single-source** REQ-004) + `BehaviourKindsTests` (the direct `Kinds`/`BuildKinds` kill). **+23 tests
  (359 → 382).** Isolated exact-count asserts.
- **`dotnet test MonoRpgMaker.slnx`:** `Passed! Failed: 0, Passed: 382` (no regressions).
- **`bin/gate.sh` (FULL):** **GATE GREEN [full]** — 13/13. coverage **93.2%** ≥ 80; mutation **Editor 84.60%**
  (the event API — floor held), **Engine 86.38%** (the registry refactor — the direct `Kinds` test killed the
  `BuildKinds` mutant), Abstractions 82.22, Analyzers 91.18; gate:13 oracle **5 rows / 3 modules — OK**; Studio
  host exempt. Receipt written.
- **Pre-existing exclusions:** none.

## Phase 5 — Complete
- Docs updated:
- Forge capture (aar/failures/rules/decisions):
- Ticket closed:
- Archived:
