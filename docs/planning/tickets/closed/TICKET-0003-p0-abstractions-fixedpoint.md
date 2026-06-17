---
title: TICKET-0003-p0-abstractions-fixedpoint
status: done
ticket: a0d9d476-1127-4f2a-9add-eb7660d56384
ticket_number: 3
type: feature
created: 2026-06-17
intake:
pipeline_spec: docs/planning/pipeline/active/WORK-p0-abstractions-fixedpoint.spec.md
---

# TICKET-0003-p0-abstractions-fixedpoint

## Summary

Stand up the **`MonoRpgMaker.Abstractions`** assembly (the pure, published seam
surface) and put the **`FixedPoint` (Q16.16)** deterministic sim numeric primitive
in it. First of several P0 (chassis) slices.

## Why

P0 is the chassis, grown from the green M0 tracer. `FixedPoint` is the foundation
primitive (D-0016): the *only* numeric type sim code may use, **integer-backed** so
replay hashes are bit-identical across CPU / JIT / NativeAOT / arch. Everything in
the sim depends on it, and the generator / validator / scaffolding (later P0 slices)
build on `Abstractions`.

## EARS Requirements

| ID | EARS Requirement | Verification |
|---|---|---|
| REQ-001 | When a `FixedPoint` is created from an integer within the representable range, the system shall round-trip it (`FromInt(n).ToInt() == n`). | unit test |
| REQ-002 | The `FixedPoint` type shall implement `+`, `-`, `*`, `/`, and unary `-` using integer-only operations (multiply/divide via an `Int64` intermediate to preserve the Q16.16 scale), with no `float`/`double` in the arithmetic path. | unit test + source-bans/review |
| REQ-003 | The `FixedPoint` type shall implement `IEquatable<FixedPoint>` and `IComparable<FixedPoint>` plus `==`,`!=`,`<`,`>`,`<=`,`>=` consistent with the represented value. | unit test |
| REQ-004 | The `FixedPoint` type shall provide `Abs`, `Min`, `Max`, `Floor`, `Ceiling`, `Round`, `ToInt`, and `ToString`, each deterministic. | unit test |
| REQ-005 | The `MonoRpgMaker.Abstractions` assembly shall reference neither MonoGame (`Microsoft.Xna.Framework`) nor `MonoRpgMaker.Engine` nor the host projects. | project graph + NetArchTest |
| REQ-006 | The architecture tests shall assert the "Project → Abstractions only" ring (Abstractions has no outward dependency on Engine / hosts / MonoGame). | NetArchTest (xUnit) |
| REQ-007 | The slice shall pass the FULL `bin/gate.sh` (format, build `-warnaserror`, tests, coverage ≥ floor, mutation ≥ floor, static gates). | gate run |

## Scope

- In: the `MonoRpgMaker.Abstractions` project (pure, no MonoGame); the `FixedPoint`
  struct (Q16.16, full arithmetic/comparison/helpers); add it to `MonoRpgMaker.slnx`;
  NetArchTest layering rules; unit tests; FULL gate green.
- Out (later P0 slices): the sim float/MathF/Vector2/foreach-over-Dictionary ban
  analyzer; the `IRandom`/`IDeterministicRng` seam + a deterministic RNG impl;
  extracting the event seam interfaces (`IMapEvent`/`EventTrigger`) into Abstractions;
  `FixedPoint` transcendentals (sqrt/trig); and wiring `FixedPoint` into the
  (still int-grid) tracer sim.

## Notes

- Forge ticket: a0d9d476-1127-4f2a-9add-eb7660d56384 (#3), project monorpgmaker
  (`a9ee8162-3cfd-4c99-be5c-bbdb4be32b10`)
- Related docs: docs/decisions.md (D-0016, D-0014), docs/agentic-substrate.md
  (§5 primitives, §7 determinism), docs/roadmap.md (P0)
- Promoted from intake: none
- Active pipeline: docs/planning/pipeline/active/WORK-p0-abstractions-fixedpoint.spec.md
