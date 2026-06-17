---
title: TICKET-0002-m0-chest-give-once
status: done
ticket: 3e56871a-eb6f-4d61-9b81-ba0262073253
ticket_number: 2
type: spike
created: 2026-06-17
intake:
pipeline_spec: docs/planning/pipeline/active/WORK-m0-chest-give-once.spec.md
---

# TICKET-0002-m0-chest-give-once

## Summary

Author the "chest gives one potion, then empty" authored-event variant as
hand-written C# (a `ChestEvent : IMapEvent` mirroring `Sim/Tracer/LeverEvent.cs`),
place a chest in the tracer room, and wire it into the `MonoRpgMaker.Player`
desktop app — then use it to run the **M0 GO/NO-GO** timing/feel measurement of
the AI-directed authoring loop vs RPG Maker's click-path.

## Why

The tracer (ticket #1) proved the authoring loop *works*; this spike measures
whether it is *fast and pleasant enough* — within ~3× RPG Maker's click-path and
"feels good" for the technical-hobbyist user. That GO/NO-GO gates whether we
commit to **P0 (the chassis)**: cheap to falsify now, expensive after a six-month
toolchain is built on an unproven thesis (roadmap P-(-1); WORK-m0-tracer-bullet-v1
locked decision #6).

## EARS Requirements

| ID | EARS Requirement | Verification |
|---|---|---|
| REQ-001 | When the player triggers the chest for the first time, the system shall grant exactly one potion (observable via the session's potion tally) and display the grant message exactly once. | unit test |
| REQ-002 | When the player triggers the chest after it has been opened, the system shall display the "empty" message and grant no additional potion, so the potion tally rises by exactly one across any number of repeated triggers. | unit test |
| REQ-003 | Given the same initial world and the same player command sequence, the chest's outcome (potion tally + messages) shall be identical on every run. | unit test (deterministic replay) |
| REQ-004 | When the Player application is launched, the system shall render the chest in the tracer room and let the player trigger it through movement/interaction. | smoke check (manual run of MonoRpgMaker.Player) |
| REQ-005 | The chest simulation code shall use no `System.Random` or `DateTime`; any randomness shall be obtained from an injected `IRandom`. | source-bans gate + review |
| REQ-006 | The slice shall pass the FULL `bin/gate.sh` (format, build `-warnaserror`, tests, the static gates, coverage ≥ floor, mutation ≥ floor). | gate run (FULL) |
| REQ-007 | The pipeline shall record a GO/NO-GO measurement comparing AI-directed authoring of the chest variant against RPG Maker's click-path (target ~3× + "feels good"), captured at `/pipeline:complete`. | doc check |

## Scope

- In: `ChestEvent` (hand-C# `IMapEvent`); a chest placed in the tracer room; Player
  wiring so it renders + triggers; an observable "one potion" grant; unit tests
  for first-grant / second-empty / idempotent-repeat / determinism; the documented
  timing/feel measurement procedure + recorded GO/NO-GO.
- Out: full inventory/item database, item use/effects, real art, the source
  generator / `Abstractions` / `FixedPoint` / replay-hash gate (P0 chassis),
  save/load persistence of chest state, the subjective human GO/NO-GO judgment
  itself (manual, recorded at `/pipeline:complete`).

## Notes

- Forge ticket: 3e56871a-eb6f-4d61-9b81-ba0262073253 (#2), project monorpgmaker
  (`a9ee8162-3cfd-4c99-be5c-bbdb4be32b10`)
- Related docs: docs/roadmap.md (P-(-1)/P0), docs/decisions.md (D-0015/16/17),
  docs/planning/pipeline/completed/WORK-m0-tracer-bullet-v1.{spec,notes}.md
- Promoted from intake: none
- Active pipeline: docs/planning/pipeline/active/WORK-m0-chest-give-once.spec.md
