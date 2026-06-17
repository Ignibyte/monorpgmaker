---
pipeline_id: 81bc05d3-7de8-4d19-887e-6d364acd0959
title: WORK-p0-abstractions-fixedpoint
ticket: a0d9d476-1127-4f2a-9add-eb7660d56384
type: work
intake:
notes: WORK-p0-abstractions-fixedpoint.notes.md
status: Phase 5 — Complete PASS
---

# WORK-p0-abstractions-fixedpoint

> Pipeline spec (always-loaded contract). Detailed per-phase work lives in the
> paired `.notes.md`. Phase advance = updating the `status:` frontmatter above.

## Work Spec
- **Title:** P0 slice 1 — stand up the `MonoRpgMaker.Abstractions` seam assembly and
  the `FixedPoint` (Q16.16) deterministic sim numeric primitive (D-0016). First of
  several P0 (chassis) slices grown from the green M0 tracer.
- **Scope:** *In* — a new **`src/MonoRpgMaker.Abstractions/MonoRpgMaker.Abstractions.csproj`**
  (net10.0, pure: NO MonoGame reference; inherits `Directory.Build.props`); the
  **`FixedPoint`** readonly struct (Q16.16, single `Int32` raw, 16 fractional bits):
  `FromInt`/raw construction + `Zero`/`One`; `+ - * /` and unary `-` (mul/div via an
  `Int64` intermediate, integer-only); `IEquatable`/`IComparable` + comparison
  operators; `Abs`/`Min`/`Max`/`Floor`/`Ceiling`/`Round`/`ToInt`/`ToString` + a
  debug-only `ToDouble`; add the project to `MonoRpgMaker.slnx`; **NetArchTest**
  layering rules (the "Project → Abstractions only" ring); unit tests; FULL gate.
  *Out* — the sim float/MathF/Vector2/foreach-over-Dictionary ban analyzer; the
  `IRandom`/`IDeterministicRng` seam + RNG impl; extracting `IMapEvent`/`EventTrigger`
  into Abstractions; `FixedPoint` transcendentals; and converting the (int-grid)
  tracer sim to `FixedPoint` (it is **not** modified here — `FixedPoint` is
  forward-looking infrastructure tested in isolation).
- **Systems:** engine · **abstractions (new)** · build/solution · tests/architecture.

## Acceptance Criteria (EARS)
Each acceptance criterion uses EARS syntax, one observable behavior, with a verification method.

| ID | EARS Requirement | Verification |
|---|---|---|
| REQ-001 | When a `FixedPoint` is created from an integer within the representable range, the system shall round-trip it (`FromInt(n).ToInt() == n`). | unit test |
| REQ-002 | The `FixedPoint` type shall implement `+`, `-`, `*`, `/`, and unary `-` using integer-only operations (multiply/divide via an `Int64` intermediate to preserve the Q16.16 scale), with no `float`/`double` in the arithmetic path. | unit test (e.g. 0.5×0.5=0.25, 10/4=2.5) + source-bans/review |
| REQ-003 | The `FixedPoint` type shall implement `IEquatable<FixedPoint>` and `IComparable<FixedPoint>` plus `==`,`!=`,`<`,`>`,`<=`,`>=` consistent with the represented value. | unit test |
| REQ-004 | The `FixedPoint` type shall provide `Abs`, `Min`, `Max`, `Floor`, `Ceiling`, `Round`, `ToInt`, and `ToString`, each deterministic. | unit test |
| REQ-005 | The `MonoRpgMaker.Abstractions` assembly shall reference neither MonoGame (`Microsoft.Xna.Framework`) nor `MonoRpgMaker.Engine` nor the host projects. | project graph + NetArchTest |
| REQ-006 | The architecture tests shall assert the "Project → Abstractions only" ring (Abstractions has no outward dependency on Engine / hosts / MonoGame). | NetArchTest (xUnit) |
| REQ-007 | The slice shall pass the FULL `bin/gate.sh` (format, build `-warnaserror`, tests, coverage ≥ floor, mutation ≥ floor, static gates). | gate run (FULL) |

## Locked-In Decisions
- **D-0016 conformance:** Q16.16 fixed-point; **integer-backed** (`Int32` raw +
  `Int64` intermediate for `*`/`/`); no `float`/`double` in the math path → bit-identical
  across CPU / JIT / NativeAOT / arch. This is *the* reason FixedPoint exists.
- **Home = a new pure `MonoRpgMaker.Abstractions` assembly** (no MonoGame ref) — the
  published seam surface; the start of the **"Project → Abstractions only" ring** (D-0014).
- New project **inherits `Directory.Build.props`** (Nullable, latest-recommended
  analyzers, BannedApiAnalyzers, `packages.lock.json`); it adds **no** MonoGame reference.
- **FULL gate** (P0 is post-tracer; the strict 12-gate applies, as for the chest slice).
- **One slice only** — primitive + seam home + layering. Explicitly deferred to
  *separate* P0 slices: the float-ban analyzer (+ its sim/host scoping problem), the
  `IRandom` seam + RNG impl, the event-seam extraction, and FixedPoint transcendentals.
- The int-grid tracer sim is **not** modified; FixedPoint is tested in isolation.
- **OPEN — Phase 2 design decisions (not pre-empted here):**
  1. Overflow policy for `+`/`-`/`*` — `unchecked` wrap vs `checked` vs saturating.
  2. Rounding semantics for `/` and `Round` — truncate-toward-zero vs round-half-away
     vs round-half-to-even. Pick one deterministic rule and test it exactly.
  3. Whether `Engine` adds a (currently-unused) `ProjectReference` to Abstractions now
     to cement the ring, or defers it until the sim first uses `FixedPoint`
     (recommendation: defer — the layering test inspects the Abstractions assembly
     directly and needs no consumer edge).
  4. Keep the debug `ToDouble` only after confirming gate:9 source-bans don't grep
     `double` (the float ban is a *later* slice; `double` is not in BannedSymbols.txt).

## Linked Artifacts
- Design docs: docs/decisions.md (D-0016, D-0014), docs/agentic-substrate.md
  (§2 wiring tier, §5 primitives, §7 determinism), docs/roadmap.md (P0)
- Intake doc: none
- Ticket doc: docs/planning/tickets/open/TICKET-0003-p0-abstractions-fixedpoint.md
- Forge ticket: a0d9d476-1127-4f2a-9add-eb7660d56384 (#3, project monorpgmaker
  `a9ee8162-3cfd-4c99-be5c-bbdb4be32b10`)
- Extraction base: docs/planning/pipeline/completed/WORK-m0-tracer-bullet-v1.* +
  WORK-m0-chest-give-once.*
- AAR: 928e71d0-7fce-4fe9-9be7-a7aabbdcfb35

## Phase Plan
| Phase | Status |
|---|---|
| 1 — Plan | PASS |
| 2 — Design | PASS |
| 3 — Implement | PASS |
| 3.5 — Inspect | PASS |
| 4 — Validate | PASS |
| 5 — Complete | PASS |
