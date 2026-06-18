---
title: TICKET-0005-p0-gridpoint
status: done
ticket: 68ec5a99-73a9-4b8c-a609-63478cb702fc
ticket_number: 5
type: feature
created: 2026-06-18
intake:
pipeline_spec: docs/planning/pipeline/active/WORK-p0-gridpoint.spec.md
---

# TICKET-0005-p0-gridpoint

## Summary

Add a pure `GridPoint(int X, int Y)` coordinate value type to
`MonoRpgMaker.Abstractions` — the MonoGame-free coordinate the published seam
(`IMapEvent.Cell`, spatial queries) will use instead of XNA `Point`, so
`Abstractions` stays pure. The clean foundation for the deferred event-seam extraction.

## Why

The event-seam extraction (moving `IMapEvent`/`EventTrigger` into `Abstractions`)
needs a coordinate type — but `Abstractions` must stay MonoGame-free (D-0014
"don't hack core"), so it cannot use XNA `Point`. `Point` is pervasive (~13 files
across World/Entities/Sim/host), so migrating it wholesale is too big for one clean
slice. `GridPoint` is the small, pure prerequisite shipped first; the seam adopts it
and the tracer migrates in slice **3b**.

## EARS Requirements

| ID | EARS Requirement | Verification |
|---|---|---|
| REQ-001 | The `GridPoint` type shall expose `X` and `Y` and value equality (two `GridPoint`s with equal components are `Equal` with equal `GetHashCode`). | unit test |
| REQ-002 | `GridPoint + GridPoint` and `GridPoint - GridPoint` shall add/subtract componentwise. | unit test |
| REQ-003 | The `GridPoint` type shall provide a `Zero` value `(0,0)`. | unit test |
| REQ-004 | `ManhattanDistanceTo(other)` shall return `|dx|+|dy|` and `ChebyshevDistanceTo(other)` shall return `max(|dx|,|dy|)` — non-negative, deterministic, integer-only (no `float`, no `Math.Abs` throw). | unit test |
| REQ-005 | `GridPoint` shall use no MonoGame / `float` / `double`; `MonoRpgMaker.Abstractions` stays MonoGame-free. | NetArchTest ring + source-bans + review |
| REQ-006 | The slice shall pass the FULL `bin/gate.sh` (incl. mutation on Abstractions ≥ floor). | gate run |

## Scope

- In: the `GridPoint` `readonly record struct` (X/Y, `+`/`-`, `Zero`, Manhattan/
  Chebyshev distance), unit + mutation tests, FULL gate green. Tested in isolation.
- Out (slice 3b): moving `IMapEvent`/`EventTrigger` into Abstractions, abstracting
  `EventContext` as `IEventContext`, the Engine→Abstractions consumption edge, the
  `Point`↔`GridPoint` adapter, and migrating the tracer + tests. Also deferred: the
  float-ban analyzer and the generator/validator/scaffolding.

## Notes

- Forge ticket: 68ec5a99-73a9-4b8c-a609-63478cb702fc (#5), project monorpgmaker
  (`a9ee8162-3cfd-4c99-be5c-bbdb4be32b10`)
- Related docs: docs/decisions.md (D-0014, D-0016), docs/agentic-overview.md (§3
  don't-hack-core layering), docs/agentic-substrate.md (§4 seam taxonomy), docs/roadmap.md (P0)
- Builds on: docs/planning/pipeline/completed/WORK-p0-abstractions-fixedpoint.*,
  WORK-p0-irandom-rng.*
- Promoted from intake: none
- Active pipeline: docs/planning/pipeline/active/WORK-p0-gridpoint.spec.md
