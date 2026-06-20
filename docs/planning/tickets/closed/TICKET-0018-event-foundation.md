---
title: TICKET-0018-event-foundation
status: closed
ticket: 4afa20c7-7974-45e1-afca-87a0c2579bb5
ticket_number: 18
type: feature
created: 2026-06-20
intake: docs/planning/intake/INTAKE-event-trigger-system.md
pipeline_spec: docs/planning/pipeline/active/WORK-event-foundation.spec.md
---

# TICKET-0018-event-foundation

## Summary

**Phase A, Slice 1** of the unified trigger/event system (**D-0024**; `INTAKE-event-trigger-system`). Prove the
**contract-first trigger foundation** end-to-end in the *playable* runtime: a built-in **`ShowText`** dialogue you
walk up to and press **Space** to read — **zero agent, zero one-off code.** The substrate Phase B (Studio UI
placement) and Phase C (agent custom-module flow) sit on.

## EARS Requirements

| ID | EARS Requirement | Verification |
|---|---|---|
| REQ-001 | A map's `$data` shall carry optional placed events `{id, cell{x,y}, trigger, behaviour{kind, params}}`; the loader shall round-trip them and be **total** (malformed → typed `MapLoadResult` error, never a throw). | unit |
| REQ-002 | A behaviour **registry** shall materialise each placement into an `IMapEvent` by `kind`; an unknown kind / bad params → a typed failure, not a crash. | unit |
| REQ-003 | The built-in `ShowTextEvent` shall implement `IMapEvent` and `Run` → `[ShowMessage(text)]` (ActionButton); its contract shall be validated by gate:13 (a `ShowText` `.expect` sample) + unit tests for the parameterisation. | unit + gate:13 |
| REQ-004 | The runtime shall materialise the start map's `$data` events via the registry and dispatch them: pressing Space facing the event cell shall surface its message. | unit + manual |
| REQ-005 | The start map shall carry one `ShowText` event (a sign/NPC cell, ActionButton) rendered with a visible marker; pressing Space facing it shows the line. | manual smoke |
| REQ-006 | The slice shall pass the FULL `bin/gate.sh` (Engine coverage + MSI ≥ 80 incl. the schema/registry/ShowText; gate:13 incl. ShowText; Player host exempt). | gate (FULL) |

## Scope

- **In:** `$data` placed-events schema (total) · a `BehaviourRegistry` (`kind`→`IMapEvent`) · the built-in
  `ShowTextEvent` + its `.expect` + `HandlerRegistry` entry · `StartMap.LoadWorld` materialises events → `WorldSim` ·
  `RpgGame` wires Space→`PressAction` + an event marker · one `ShowText` placed in `content/maps/start.json`.
- **Out (later):** the Studio UI to place events (Phase B, next) · the agent custom-module flow (Phase C) ·
  Autorun/Parallel triggers · multi-map/Warp · the GiveItem/Shop/Door built-ins (Slice 2, same recipe) · richer
  dialogue (portraits/choices/scrolling).

## Notes

- Forge ticket: 4afa20c7-7974-45e1-afca-87a0c2579bb5 (#18, project monorpgmaker)
- **Contract discipline (D-0024) is non-negotiable** — every behaviour implements `IMapEvent` + registry + `.expect`; no one-offs.
- Builds on: `IMapEvent`/`Outcome`/`EventTrigger`/`IEventContext` (#13), `WorldSim`/`PressAction`, `MapSerializer`/`TileMapData` (#12), `StartMap`/`RpgGame` (#17), the `.expect` oracle + `HandlerRegistry` (#10).
- Reuse PRs: PR-claude-validate-deserialized-elements-not-just-container, PR-claude-analyzer-mutation-isolated-asserts-001.
- Source intake: docs/planning/intake/INTAKE-event-trigger-system.md (Slice 1).
