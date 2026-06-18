---
pipeline_id: f74ec6ce-cfa3-44d7-adc6-6bc27540dc4b
title: WORK-p0-irandom-rng
ticket: 43a2f8ee-519c-432f-aec7-e4fda1bba366
type: work
intake:
notes: WORK-p0-irandom-rng.notes.md
status: Phase 5 — Complete PASS
---

# WORK-p0-irandom-rng

> Pipeline spec (always-loaded contract). Detailed per-phase work lives in the
> paired `.notes.md`. Phase advance = updating the `status:` frontmatter above.

## Work Spec
- **Title:** P0 slice 2 — the `IRandom` deterministic randomness seam + a seeded,
  reproducible integer-only PRNG in `MonoRpgMaker.Abstractions` (the second
  determinism primitive, pairing with slice 1's `FixedPoint`; D-0016).
- **Scope:** *In* — an **`IRandom`** interface (the injected randomness seam, the
  only randomness source sim code may use) and one **concrete deterministic seeded
  PRNG** (a well-known integer-only algorithm) implementing it, both in
  `Abstractions`; the output surface `NextInt()` / `NextInt(minInclusive,
  maxExclusive)` / `NextBool()` / `NextFixedPoint()` (→ slice-1 `FixedPoint` in
  `[0,1)`); **capturable/restorable state** (construct from a seed AND from a
  captured state, plus a State accessor) for future save/replay reproducibility;
  exhaustive unit + mutation tests; FULL gate green. *Out* — the `RngState`
  `ISaveStateComponent` + the save/load system; the fixed-step sim loop; per-entity
  RNG slicing (D-0019 open); wiring `IRandom` into the still-scripted tracer sim;
  cryptographic quality.
- **Systems:** **abstractions (new types)** · (forward-looking: save/load, sim,
  replay — all deferred).

## Acceptance Criteria (EARS)
| ID | EARS Requirement | Verification |
|---|---|---|
| REQ-001 | When two `IRandom` instances are created from the same seed, the system shall produce identical output sequences from them. | unit test |
| REQ-002 | When created from a fixed known seed, the RNG shall produce a fixed, pinned ("golden") sequence of outputs. | unit test |
| REQ-003 | `NextInt(minInclusive, maxExclusive)` shall return values within `[minInclusive, maxExclusive)` for every call, deterministically, including a range of one (always `minInclusive`). | unit test |
| REQ-004 | `NextBool()` shall return a deterministic boolean and `NextFixedPoint()` shall return a `FixedPoint` in `[0,1)` (Raw in `[0, 65536)`). | unit test |
| REQ-005 | When the RNG state is captured and a new RNG restored from it, the restored RNG shall reproduce the identical continuation of the sequence. | unit test |
| REQ-006 | The RNG generation path shall use no `System.Random`, `DateTime`, `float`, or `double` (integer-only, deterministic). | source-bans + review + gate |
| REQ-007 | `IRandom` and the concrete RNG shall reside in `MonoRpgMaker.Abstractions` with no MonoGame / Engine / host dependency. | NetArchTest ring |
| REQ-008 | The slice shall pass the FULL `bin/gate.sh` (incl. mutation on Abstractions ≥ floor). | gate run (FULL) |

## Locked-In Decisions
- **Determinism (D-0016):** the PRNG is **integer-only** — no `float`/`double`/
  `System.Random`/`DateTime` in the generation path — so output is bit-identical
  across CPU / JIT / NativeAOT / arch. Seeded, reproducible, serializable state.
- **`IRandom` is the injected randomness seam** — the only randomness source sim may
  use; lives in pure `Abstractions`. `System.Random` stays banned (gate:9 + review).
- **One well-known algorithm behind the seam** — "good game RNG," not cryptographic;
  it can be swapped later without touching callers (that's the point of the seam).
- **State is capturable/restorable** (from-seed + from-state construction + a State
  accessor) for the FUTURE `RngState` save component + replay — but the save system
  itself is **deferred**.
- **The gate mutates `Abstractions`** ([[AD-claude-gate-mutates-all-projects-001]]),
  so the RNG must be **exhaustively mutation-tested** (the golden-sequence, bounds,
  and state-round-trip tests are the killers).
- **One slice.** Deferred to later slices: the `RngState` component + save/load; the
  fixed-step sim loop; per-entity RNG slicing (D-0019 open); sim wiring; crypto.
- **OPEN — Phase 2 design decisions:**
  1. The exact PRNG algorithm (PCG32 vs xorshift64 vs SplitMix64) + how a `seed`
     expands to the initial state (e.g., SplitMix64-seed a PCG/xorshift).
  2. `NextInt(min,max)`: rejection sampling (unbiased — must stay deterministic +
     provably terminating/bounded) vs modulo (biased, simple). Pick + document.
  3. `NextInt()` return type (int vs uint) + the `NextFixedPoint()` derivation
     (e.g., top 16 bits of a `Next` output → Raw in `[0,65536)`).
  4. The State representation (e.g., `ulong`) + how `IRandom` exposes capture/restore.
  5. The impl is a **mutable class** (a PRNG advances its state per call) — unlike the
     immutable `readonly struct FixedPoint`.

## Linked Artifacts
- Design docs: docs/decisions.md (D-0016, D-0019), docs/agentic-substrate.md
  (§5 primitives, §7 determinism), docs/agentic-features.md (RngState save component),
  docs/roadmap.md (P0, P1)
- Intake doc: none
- Ticket doc: docs/planning/tickets/open/TICKET-0004-p0-irandom-rng.md
- Forge ticket: 43a2f8ee-519c-432f-aec7-e4fda1bba366 (#4, project monorpgmaker
  `a9ee8162-3cfd-4c99-be5c-bbdb4be32b10`)
- Builds on: docs/planning/pipeline/completed/WORK-p0-abstractions-fixedpoint.spec.md
- AAR: dc1d06c6-fdfd-4fe1-81a1-eca4ca188d46

## Phase Plan
| Phase | Status |
|---|---|
| 1 — Plan | PASS |
| 2 — Design | PASS |
| 3 — Implement | PASS |
| 3.5 — Inspect | PASS |
| 4 — Validate | PASS |
| 5 — Complete | PASS |
