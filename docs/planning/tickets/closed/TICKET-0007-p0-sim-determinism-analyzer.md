---
title: TICKET-0007-p0-sim-determinism-analyzer
status: done
ticket: 26b2386e-f4df-4df7-b0f5-d7c5ba7add27
ticket_number: 7
type: feature
created: 2026-06-18
intake:
pipeline_spec: docs/planning/pipeline/completed/WORK-p0-sim-determinism-analyzer.spec.md
---

# TICKET-0007-p0-sim-determinism-analyzer

## Summary

Stand up the **sim-determinism ban analyzer** — a compile-time Roslyn
`DiagnosticAnalyzer` that makes determinism-hostile constructs *un-compilable in
simulation code*: `float`/`double`, `MathF`/transcendental float math,
`Microsoft.Xna.Framework.Vector2` (float; XNA `Point` stays allowed),
`foreach`-over-`Dictionary<,>`, and the non-deterministic `System.Random` +
`DateTime`/`DateTimeOffset` APIs. The host/renderer (`Engine.Core`, `Player`,
`Editor`) is carved out. This is the analyzer **D-0016** mandates "before any sim
module" — the first of the P0 determinism analyzers.

## Why

The three sim primitives (`FixedPoint` #3, `IRandom` #4, `GridPoint` #5) and the
event seam (#6) have landed; `FixedPoint` is now the *only* sim numeric type by
**D-0016** — but **nothing yet makes a sim `float` fail to compile.** Verified
ground truth: today neither these constructs **nor** `System.Random`/`DateTime`
are banned anywhere — `BannedSymbols.txt` bans only `Process`/`Exit`/`FailFast`,
gate:9 greps only those + `unsafe`, and `ArchitectureTests.cs:34-35` even
*explicitly allows* `Vector2` in sim. Replay-to-same-hash is the product moat and
a binding gate (D-0016); bit-identical float across FMA/libm/JIT-vs-AOT/CPU-arch
is impossible, so determinism must be enforced *by construction*, not by review.
This slice closes that gap and is the roadmap's next "Remaining P0" item
(roadmap L61-62, L76-78).

## EARS Requirements

| ID | EARS Requirement | Verification |
|---|---|---|
| REQ-001 | When sim code (a type in `MonoRpgMaker.Engine.{World,Entities,Data,Sim}` or `MonoRpgMaker.Abstractions`) uses `float` or `double`, the analyzer shall emit an `MRM` determinism diagnostic. | analyzer verifier test (positive) |
| REQ-002 | When sim code references `MathF` or a transcendental float-returning `System.Math` member, the analyzer shall emit the diagnostic. | analyzer verifier test |
| REQ-003 | When sim code references `Microsoft.Xna.Framework.Vector2`, the analyzer shall emit the diagnostic; XNA `Point` (integer) shall not be flagged. | analyzer verifier test (Vector2 positive, Point negative) |
| REQ-004 | When sim code uses `foreach` to enumerate a `Dictionary<,>` (or its `Keys`/`Values`), the analyzer shall emit the diagnostic. | analyzer verifier test |
| REQ-005 | When sim code references `System.Random` or `System.DateTime`/`DateTimeOffset`, the analyzer shall emit the diagnostic. | analyzer verifier test |
| REQ-006 | When host code (`MonoRpgMaker.Engine.Core`, `MonoRpgMaker.Player`, `MonoRpgMaker.Editor`) uses any of the above constructs, the analyzer shall not emit a diagnostic. | analyzer verifier test (host carve-out, negative) |
| REQ-007 | Where a symbol is the sanctioned `FixedPoint`↔`double` diagnostic boundary (e.g. `FixedPoint.ToDouble()`), the analyzer shall not emit a diagnostic. | analyzer verifier test + review |
| REQ-008 | The analyzer diagnostics shall build as errors under `dotnet build -warnaserror` (gate:2), so a determinism-hostile construct in sim fails the build. | gate:2 run + a deliberately-violating fixture compiled in isolation |
| REQ-009 | `ArchitectureTests.cs` shall no longer claim `Vector2` is permitted in simulation namespaces. | review + `dotnet test` |
| REQ-010 | The slice shall pass the FULL `bin/gate.sh` (format, build, tests, coverage ≥ floor, mutation MSI ≥ floor on the mutated projects). | gate run (FULL) |

## Scope

- **In:**
  - A new Roslyn analyzer project (e.g. `src/MonoRpgMaker.Analyzers`, `netstandard2.0`)
    holding the `DiagnosticAnalyzer`(s).
  - The ban rules above, **scoped to the sim namespaces** (the boundary mirrors the
    existing NetArchTest sim list).
  - Wiring the analyzer into the projects that contain sim code (`Engine`,
    `Abstractions`) as an analyzer reference, errors under `-warnaserror`.
  - Analyzer **verifier tests** (`Microsoft.CodeAnalysis.*.Testing`) covering each
    rule, the host carve-out, and the `FixedPoint` exemption.
  - Updating the now-contradictory `ArchitectureTests.cs` sim comment (and deciding
    whether to keep/tighten the NetArchTest alongside the analyzer).
  - Gate integration (build/test/coverage/mutation treatment of the analyzer assembly).
  - FULL `bin/gate.sh` green.
- **Out:**
  - The **outcome-return purity** analyzer (D-0017's other analyzer — separate slice).
  - The generator/validator/scaffolding (`monorpg scaffold`/`validate`,
    `contract-manifest.json`, `MRM0001..` DAG validation).
  - Any `Point`→`GridPoint` migration (the ~13-file `Point` reach stays; `Point` is allowed).
  - Splitting `MonoRpgMaker.Engine` into separate sim/host assemblies (the analyzer
    scopes by namespace precisely so the split is unnecessary).
  - Rewriting existing sim code (there is no current sim `float` usage; the ban is
    by-construction going forward).

## Notes

- Forge ticket: 26b2386e-f4df-4df7-b0f5-d7c5ba7add27 (#7, project monorpgmaker `a9ee8162-3cfd-4c99-be5c-bbdb4be32b10`)
- Related docs: `docs/decisions.md` (D-0016 determinism-by-construction, D-0017 float-ban analyzer, D-0021 scoping note via D-0016's "sim/determinism path only"), `docs/roadmap.md` (P0 "Remaining P0"), `docs/agentic-substrate.md` (determinism substrate)
- Promoted from intake: none (chosen from the LOCAL roadmap per `PR-claude-forge-doc-staleness-001`)
- Active pipeline: docs/planning/pipeline/active/WORK-p0-sim-determinism-analyzer.spec.md
