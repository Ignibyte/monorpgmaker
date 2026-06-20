---
title: TICKET-0019-studio-event-placement
status: closed
ticket: 8374d948-68fe-4b80-b90b-3bcd0af0139c
ticket_number: 19
type: feature
created: 2026-06-19
intake: docs/planning/intake/INTAKE-event-trigger-system.md
pipeline_spec: docs/planning/pipeline/active/WORK-studio-event-placement.spec.md
---

# TICKET-0019-studio-event-placement

## Summary

Studio v2.1 — **event placement in the map editor**. In the Studio map painter you drop/select an event on a
tile, configure its behaviour (ShowText: type the line), and Save — it writes the **same `$data` events (#18
schema)** the runtime already loads + dispatches. The author→**place**→play loop, now in the UI (no more
hand-edited JSON). The first slice of the 3-slice Studio-v2 arc; = Slice 3 of `INTAKE-event-trigger-system`.

## Why

#18 gave the runtime a contract-first event foundation (placed events in `$data`, a `BehaviourRegistry`, the
built-in `ShowText`), but placement is still **hand-authored JSON**. This makes the foundation **usable** from the
tool — the visual half of the loop — and is the substrate the rest of Studio-v2 (painter polish, multi-map) and
the agent custom-kind flow build on. Contract-first (D-0024): the Studio edits **placement data** (`EventData`);
the runtime materialises behaviours via the registry.

## EARS Requirements

| ID | EARS Requirement | Verification |
|---|---|---|
| REQ-001 | When asked to add an event at a cell, the `MapPaintSession` shall hold a new placement (default kind ShowText, a deterministic auto-id, a default trigger) via a framework-neutral API (no XNA types). | unit |
| REQ-002 | The `MapPaintSession` shall find/select the event at a cell, update its fields (trigger/kind/params), and remove it; an empty cell shall yield no selection. | unit |
| REQ-003 | When saved, the session shall serialize the map **with** its events; Load shall read `MapLoadResult.Events`; a save→load round-trip shall preserve every placement (id/cell/trigger/kind/params). | unit |
| REQ-004 | The behaviour kinds + their param-keys shall come from a single shared source (a kind descriptor) so the editor matches the runtime registry; ShowText shall expose its `text` param. | unit |
| REQ-005 | A ShowText event placed + configured in the editor model, saved, then loaded by the runtime materialisation path shall produce a real `IMapEvent`. | unit |
| REQ-006 | The Studio shall provide an event tool, an inspector (trigger + kind dropdowns, params, delete), and a marker at each event cell; place a sign + Save + run the Player → the line shows on Space. | manual smoke (host) |
| REQ-007 | The FULL `bin/gate.sh` shall be green (Editor coverage + mutation ≥ 80 incl. the event logic; Engine green; Studio host exempt). | gate (FULL) |

## Scope

- **In:** the event-model logic in `MapPaintSession` (Editor, gated, **neutral API**) — hold events, add/select/
  update/remove, save-with-events, load-events; a single shared **kind-descriptor** source; the Avalonia
  **event-tool + inspector + marker** (the `[ExcludeFromCodeCoverage]` host).
- **Out:** painter polish (undo/redo, new-map sizing, the **tileset picker** + the `$data` tileset-reference) =
  **Slice 2**; **multi-map / map list** = **Slice 3**; the **agent custom-kind flow** = Phase C; kinds beyond
  ShowText (arrive with Slice 2's built-ins); placing entities/NPC-sprites; the fantasy UI skin.

## Notes

- Forge ticket: 8374d948-68fe-4b80-b90b-3bcd0af0139c (#19)
- Related docs: `docs/roadmap.md`, `docs/product-phasing.md`, `docs/planning/intake/INTAKE-event-trigger-system.md`
- Promoted from intake: `INTAKE-event-trigger-system` (Slice 3; the intake stays — Slices for painter polish +
  multi-map remain)
- Active pipeline: `docs/planning/pipeline/active/WORK-studio-event-placement.spec.md`
