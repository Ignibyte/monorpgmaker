---
pipeline_id: ce74a207-4d5b-4833-b6a4-c42a25ec9dbe
title: WORK-studio-event-placement
ticket: 8374d948-68fe-4b80-b90b-3bcd0af0139c
type: work
intake: docs/planning/intake/INTAKE-event-trigger-system.md
notes: WORK-studio-event-placement.notes.md
status: Phase 5 — Complete PASS
---

# WORK-studio-event-placement

> Pipeline spec (always-loaded contract). Detailed per-phase work lives in the
> paired `.notes.md`. Phase advance = updating the `status:` frontmatter above.

## Work Spec
- **Title:** Studio v2.1 — **event placement in the map editor**: drop/select an event on a tile, configure its
  behaviour (ShowText: type the line), and Save — it writes the **same `$data` events (#18 schema)** the runtime
  already loads + dispatches. Closes the author→**place**→play loop in the UI (no more hand-edited JSON). The
  first slice of the 3-slice Studio-v2 arc; = Slice 3 of `INTAKE-event-trigger-system`.
- **Scope:**
  *In* — the **event-model logic in `MapPaintSession`** (`MonoRpgMaker.Editor`, gated): hold the current map's
  placed events; **add** an event at a cell (default kind ShowText, deterministic auto-id, default trigger);
  **select/find** the event at a cell; **update** its fields (trigger/kind/params); **remove** it; **Save** via
  the #18 `MapSerializer.Serialize(map, events)`; **Load** reads `MapLoadResult.Events` into the session — all via
  a **framework-neutral API** (Cell/int, no XNA `Point` in public signatures). A **single shared kind-descriptor**
  source (kinds + their param-keys) that the editor reads and the runtime registry agrees with (ShowText→`text`).
  The **Avalonia host** (Studio, `[ExcludeFromCodeCoverage]`): an event tool/mode, click-to-place/select, an
  inspector panel (trigger + kind dropdowns + the kind's params + delete), and a marker at each event cell.
  *Out (deferred)* — **painter polish** (undo/redo, new-map sizing, the **tileset picker** + the `$data`
  tileset-reference it needs) = **Slice 2**; **multi-map / map list** = **Slice 3**; the **agent custom-kind
  flow** = Phase C; **kinds beyond ShowText** (arrive with Slice 2's built-ins); placing entities/NPC-sprites;
  the fantasy UI skin.
- **Systems:** **editor** (the gated event-model logic in `MapPaintSession`) · **studio** (the Avalonia host) ·
  consumes `MapSerializer`/`EventData`/`MapLoadResult` (Engine.World, #18) + `BehaviourRegistry` (Engine.Sim,
  the kind-descriptor source) · save/load · tests.

## Acceptance Criteria (EARS)

| ID | EARS Requirement | Verification |
|---|---|---|
| REQ-001 | When asked to add an event at a cell, the `MapPaintSession` shall hold a new placement (default kind ShowText, a deterministic auto-generated id, a default trigger) and expose it through a framework-neutral API (no XNA types). | unit |
| REQ-002 | The `MapPaintSession` shall find/select the event at a given cell, update its fields (trigger/kind/params), and remove it; selecting an empty cell shall yield no selection. | unit |
| REQ-003 | When the session is saved, it shall serialize the map **with** its events (`Serialize(map, events)`), and Load shall read `MapLoadResult.Events` into the session; a save→load round-trip shall preserve every placement (id/cell/trigger/kind/params). | unit |
| REQ-004 | The available behaviour kinds and their parameter-keys shall come from a single shared source (a kind descriptor) so the editor's options match the runtime registry; ShowText shall expose its `text` param. | unit |
| REQ-005 | A ShowText event placed + configured in the editor model, saved, then loaded by the runtime materialisation path shall produce a real `IMapEvent` (the editor's output is runtime-valid). | unit |
| REQ-006 | The Studio shall provide an event tool (place/select on the canvas), an inspector (trigger + kind dropdowns, the kind's params, delete), and a marker at each event cell; placing a sign + Save + running the Player shows the line on Space. | manual smoke (host) |
| REQ-007 | The FULL `bin/gate.sh` shall be green (Editor coverage + mutation ≥ 80 incl. the new event logic; Engine green; the Studio host `[ExcludeFromCodeCoverage]`). | gate (FULL) |

## Locked-In Decisions
- The event-model logic lives in **`MapPaintSession` (Editor, gated)** with a **framework-neutral public API**
  (Cell/int) — **no XNA `Point` in public signatures** (#16 CS0234 lesson; the neutral-boundary rule).
- Save/Load use the **#18 events-aware `MapSerializer`** (`Serialize(map, events)` / `MapLoadResult.Events`).
- A **single shared kind-descriptor** source feeds the editor and agrees with the runtime `BehaviourRegistry`
  (no hardcoded `"text"` in two places).
- **Deterministic** id auto-generation (no `Guid`/clock in gated logic — determinism/MRM).
- The Avalonia **event-tool + inspector + marker** are the thin **`[ExcludeFromCodeCoverage]`** host.
- The Studio **edits placement data** (EventData); the runtime materialises via the registry (**D-0024**). Honor
  **D-0022** (Avalonia locked) + **D-0023** (the repo is the game; the Studio's dev-time file IO is fine).

## Linked Artifacts
- Design docs: `docs/roadmap.md` (the visual editor track), `docs/product-phasing.md` (the editor rows)
- Intake doc: `docs/planning/intake/INTAKE-event-trigger-system.md` (Slice 3 of the program)
- Ticket doc: `docs/planning/tickets/open/TICKET-0019-studio-event-placement.md`
- Forge ticket: 8374d948-68fe-4b80-b90b-3bcd0af0139c (#19, project monorpgmaker `a9ee8162-3cfd-4c99-be5c-bbdb4be32b10`)
- Builds on: #16 (Studio / `MapPaintSession`), #18 (the `$data` events schema + `BehaviourRegistry` + `WorldSim.EventCells`)
- Reuse PRs: PR-claude-editor-view-math-into-tested-layer, the neutral-boundary, PR-claude-validate-deserialized-elements, PR-claude-analyzer-mutation-isolated-asserts
- AAR: (recorded in notes at `aar-open`)

## Phase Plan
| Phase | Status |
|---|---|
| 1 — Plan | PASS |
| 2 — Design | PASS |
| 3 — Implement | PASS |
| 3.5 — Inspect | PASS |
| 4 — Validate | PASS (gate 13/13 green) |
| 5 — Complete | PASS |
