---
title: TICKET-0004-p0-irandom-rng
status: done
ticket: 43a2f8ee-519c-432f-aec7-e4fda1bba366
ticket_number: 4
type: feature
created: 2026-06-18
intake:
pipeline_spec: docs/planning/pipeline/active/WORK-p0-irandom-rng.spec.md
---

# TICKET-0004-p0-irandom-rng

## Summary

Add the **`IRandom`** deterministic randomness seam + a concrete seeded,
reproducible PRNG to `MonoRpgMaker.Abstractions` — the second of the two
foundational determinism primitives (pairs with slice 1's `FixedPoint`).

## Why

D-0016 mandates one injected, deterministic randomness source for all sim logic —
`System.Random` is non-reproducible and banned. `IRandom` is the seam every future
sim module draws from; its **capturable/restorable state** is what makes the future
`RngState` save component + the replay-to-same-hash gate byte-reproducible across
CPU / JIT / NativeAOT / arch.

## EARS Requirements

| ID | EARS Requirement | Verification |
|---|---|---|
| REQ-001 | When two `IRandom` instances are created from the same seed, the system shall produce identical output sequences from them. | unit test |
| REQ-002 | When created from a fixed known seed, the RNG shall produce a fixed, pinned ("golden") sequence of outputs. | unit test |
| REQ-003 | `NextInt(minInclusive, maxExclusive)` shall return values within `[minInclusive, maxExclusive)` for every call, deterministically, including a range of one (always `minInclusive`). | unit test |
| REQ-004 | `NextBool()` shall return a deterministic boolean and `NextFixedPoint()` shall return a `FixedPoint` in `[0,1)` (Raw in `[0, 65536)`). | unit test |
| REQ-005 | When the RNG state is captured and a new RNG restored from it, the restored RNG shall reproduce the identical continuation of the sequence. | unit test |
| REQ-006 | The RNG generation path shall use no `System.Random`, `DateTime`, `float`, or `double` (integer-only, deterministic). | source-bans + review + gate |
| REQ-007 | `IRandom` and the concrete RNG shall reside in `MonoRpgMaker.Abstractions` with no MonoGame / Engine / host dependency. | NetArchTest ring |
| REQ-008 | The slice shall pass the FULL `bin/gate.sh` (incl. mutation on Abstractions ≥ floor). | gate run |

## Scope

- In: the `IRandom` interface (seam) + one concrete deterministic seeded PRNG (an
  integer-only well-known algorithm), both in `Abstractions`; `NextInt` /
  bounded `NextInt(min,max)` / `NextBool` / `NextFixedPoint`; capturable/restorable
  state (from-seed + from-state); exhaustive unit + mutation tests; FULL gate.
- Out (later slices): the `RngState` `ISaveStateComponent` + save/load system; the
  fixed-step sim loop; per-entity RNG slicing (D-0019 open); wiring `IRandom` into
  the still-scripted tracer sim; cryptographic quality.

## Notes

- Forge ticket: 43a2f8ee-519c-432f-aec7-e4fda1bba366 (#4), project monorpgmaker
  (`a9ee8162-3cfd-4c99-be5c-bbdb4be32b10`)
- Related docs: docs/decisions.md (D-0016, D-0019), docs/agentic-substrate.md
  (§5 primitives, §7 determinism), docs/agentic-features.md (RngState), docs/roadmap.md
  (P0, P1)
- Builds on: docs/planning/pipeline/completed/WORK-p0-abstractions-fixedpoint.*
- Promoted from intake: none
- Active pipeline: docs/planning/pipeline/active/WORK-p0-irandom-rng.spec.md
