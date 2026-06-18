---
title: TICKET-0006-p0-eventseam
status: done
ticket: 745c603e-fceb-44b3-b2c0-b002bb871c7e
ticket_number: 6
type: feature
created: 2026-06-18
intake:
pipeline_spec: docs/planning/pipeline/active/WORK-p0-eventseam.spec.md
---

# TICKET-0006-p0-eventseam

## Summary

The event-seam extraction proper (follows GridPoint slice 3a). Move the event-seam
**contracts** — `IMapEvent`, `EventTrigger`, and a new `IEventContext` — from
`MonoRpgMaker.Engine.Sim` into `MonoRpgMaker.Abstractions`, so agent-authored events
implement published interfaces (the generator's emit targets). The runtime types
(`GameState`, `EventContext`) stay in Engine; `EventContext` implements `IEventContext`.

## Why

Per D-0014 "don't hack core", authored events must depend only on the published
`Abstractions` seam, never engine internals. This adds the Engine→Abstractions
consumption edge deferred since slice 1, and consumes slice-3a's `GridPoint`
(`IMapEvent.Cell` becomes `GridPoint`, not XNA `Point`, keeping Abstractions pure).

## EARS Requirements

| ID | EARS Requirement | Verification |
|---|---|---|
| REQ-001 | `IMapEvent`, `EventTrigger`, and `IEventContext` shall live in `MonoRpgMaker.Abstractions`. | build + NetArchTest ring |
| REQ-002 | `IMapEvent.Cell` shall be a `GridPoint` (not XNA `Point`). | build + review |
| REQ-003 | `IEventContext` shall expose `GetSwitch`/`SetSwitch`/`GetCounter`/`AddCounter`/`ShowMessage`, and `EventContext` (Engine) shall implement it, delegating to `GameState` + the message sink. | unit test |
| REQ-004 | The tracer events shall implement `Abstractions.IMapEvent` with behaviour unchanged (lever opens the door once; chest gives one potion then reads empty). | TracerSliceTests (updated) |
| REQ-005 | A `Point`↔`GridPoint` adapter shall convert both ways, and `WorldSim` shall fire step-on events by comparing the player cell to `IMapEvent.Cell` through it. | unit test + tracer tests |
| REQ-006 | `MonoRpgMaker.Abstractions` shall remain MonoGame-free and shall not depend on the Engine. | NetArchTest ring |
| REQ-007 | The slice shall pass the FULL `bin/gate.sh` (Engine + Abstractions MSI ≥ floor). | gate run |

## Scope

- In: the three contracts into Abstractions; `EventContext : IEventContext`; the
  `Point`↔`GridPoint` adapter; the Engine→Abstractions reference; the tracer migration
  (`LeverEvent`/`ChestEvent`/`WorldSim`/`TracerRoom`) + `TracerSliceTests`; delete the old
  Engine `Sim/EventTrigger.cs` + `Sim/IMapEvent.cs`.
- Out: the sim float-ban analyzer; the generator/validator/scaffolding. `DoorRule` stays
  `Point`-based (not an event-seam type); the World/Entities/host stay on XNA `Point`.

## Notes

- Forge ticket: 745c603e-fceb-44b3-b2c0-b002bb871c7e (#6), project monorpgmaker
  (`a9ee8162-3cfd-4c99-be5c-bbdb4be32b10`)
- Related docs: docs/decisions.md (D-0014, D-0016), docs/agentic-substrate.md (§4 seam
  taxonomy / EventContext semantic-verb surface), docs/agentic-overview.md (§3
  don't-hack-core), docs/roadmap.md (P0; P3 event keystone)
- Builds on: WORK-p0-gridpoint, WORK-p0-irandom-rng, WORK-p0-abstractions-fixedpoint (completed)
- Active pipeline: docs/planning/pipeline/active/WORK-p0-eventseam.spec.md
