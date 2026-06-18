---
pipeline_id: 8bb3d32c-c762-45f1-96b5-40191b4763f5
title: WORK-p0-gridpoint
ticket: 68ec5a99-73a9-4b8c-a609-63478cb702fc
type: work
intake:
notes: WORK-p0-gridpoint.notes.md
status: Phase 5 — Complete PASS
---

# WORK-p0-gridpoint

> Pipeline spec (always-loaded contract). Detailed per-phase work lives in the
> paired `.notes.md`. Phase advance = updating the `status:` frontmatter above.

## Work Spec
- **Title:** P0 slice 3 — the pure `GridPoint(int X, int Y)` coordinate primitive in
  `MonoRpgMaker.Abstractions` (the MonoGame-free coordinate the seam adopts instead of
  XNA `Point`; the clean foundation for the deferred event-seam extraction).
- **Scope:** *In* — a `readonly record struct GridPoint(int X, int Y)` in `Abstractions`
  (pure, no MonoGame); `operator +` / `operator -` (componentwise); a `Zero`; pure
  integer spatial helpers `ManhattanDistanceTo` (`|dx|+|dy|`) and `ChebyshevDistanceTo`
  (`max(|dx|,|dy|)`) using a branchless/ternary abs (not `Math.Abs`); the record gives
  value equality / `GetHashCode` / `ToString` / deconstruction for free; unit +
  mutation tests; FULL gate green. **Tested in isolation** — forward-looking infra
  (like `FixedPoint`/`IRandom`), NOT wired into the tracer, no XNA `Point` interop in
  Abstractions. *Out* — moving `IMapEvent`/`EventTrigger` into Abstractions, the
  `IEventContext` abstraction, the Engine→Abstractions consumption edge, the
  `Point`↔`GridPoint` adapter, and the tracer migration (all **slice 3b**); the
  float-ban analyzer; the generator/validator/scaffolding.
- **Systems:** **abstractions (new type)** · (forward-looking: the events/sim seam +
  World layer — deferred to 3b).

## Acceptance Criteria (EARS)
| ID | EARS Requirement | Verification |
|---|---|---|
| REQ-001 | The `GridPoint` type shall expose `X` and `Y` and value equality (equal components ⇒ `Equal` with equal `GetHashCode`). | unit test |
| REQ-002 | `GridPoint + GridPoint` and `GridPoint - GridPoint` shall add/subtract componentwise. | unit test |
| REQ-003 | The `GridPoint` type shall provide a `Zero` value `(0,0)`. | unit test |
| REQ-004 | `ManhattanDistanceTo(other)` shall return `|dx|+|dy|` and `ChebyshevDistanceTo(other)` shall return `max(|dx|,|dy|)` — non-negative, deterministic, integer-only (no `float`, no `Math.Abs` throw on `int.MinValue`). | unit test |
| REQ-005 | `GridPoint` shall use no MonoGame / `float` / `double`; `MonoRpgMaker.Abstractions` stays MonoGame-free. | NetArchTest ring + source-bans + review |
| REQ-006 | The slice shall pass the FULL `bin/gate.sh` (incl. mutation on Abstractions ≥ floor). | gate run (FULL) |

## Locked-In Decisions
- **`GridPoint` is a pure `readonly record struct (int X, int Y)` in `Abstractions`** —
  the MonoGame-free coordinate the seam will adopt instead of XNA `Point` (D-0014: the
  seam surface must be pure so "Project → Abstractions only" holds).
- **Integer-only** (no `float`/`double`) — deterministic (D-0016). Distance helpers use
  a branchless/ternary abs (NOT `Math.Abs`, which throws on `int.MinValue`).
- **Use the record struct's generated members** (value equality / `GetHashCode` /
  `ToString` / deconstruction) — don't hand-roll them.
- **Tested in isolation** — forward-looking infra; NOT wired into the tracer. The
  existing Engine.Sim seam + XNA `Point` stay; `GridPoint` is adopted in slice 3b. No
  XNA `Point` interop in Abstractions (the adapter lands in Engine in 3b).
- The gate mutates Abstractions, so `GridPoint`'s operators + distance helpers need
  exact-value killing tests. (Note: int-only ⇒ no `>>`/`>>>`-on-unsigned equivalent-mutant
  trap this time — see [[PR-claude-stryker-unsigned-shift-equivalent-001]].)
- **One slice.** Deferred to **3b**: the `IMapEvent`/`EventTrigger`/`IEventContext` move,
  the Engine→Abstractions edge, the `Point`↔`GridPoint` adapter, and the tracer migration.
  Also deferred: the float-ban analyzer, the generator/validator/scaffolding.
- **OPEN — Phase 2 design decisions:** the exact helper set (keep it minimal — `+`/`-`,
  `Zero`, Manhattan/Chebyshev — vs. also `WithX`/`WithY`/neighbours); the abs form
  (branchless `(n^(n>>31))-(n>>31)` vs. ternary `n<0?-n:n` — both killable; ternary
  matches `FixedPoint.Abs`); overflow handling for distance at extreme coords (accept
  `int` wrap vs. widen through `long` — grid coords are small, `int` likely fine).

## Linked Artifacts
- Design docs: docs/decisions.md (D-0014, D-0016), docs/agentic-overview.md
  (§3 don't-hack-core), docs/agentic-substrate.md (§4 seam taxonomy), docs/roadmap.md (P0)
- Intake doc: none
- Ticket doc: docs/planning/tickets/open/TICKET-0005-p0-gridpoint.md
- Forge ticket: 68ec5a99-73a9-4b8c-a609-63478cb702fc (#5, project monorpgmaker
  `a9ee8162-3cfd-4c99-be5c-bbdb4be32b10`)
- Builds on: WORK-p0-abstractions-fixedpoint, WORK-p0-irandom-rng (completed)
- AAR: 9f2f6421-9fec-45ba-8b67-d75fbf3a9322

## Phase Plan
| Phase | Status |
|---|---|
| 1 — Plan | PASS |
| 2 — Design | PASS |
| 3 — Implement | PASS |
| 3.5 — Inspect | PASS |
| 4 — Validate | PASS |
| 5 — Complete | PASS |
