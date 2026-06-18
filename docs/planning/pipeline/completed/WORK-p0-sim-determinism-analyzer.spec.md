---
pipeline_id: 683cd465-f600-4ef2-89f0-39724d33ff30
title: WORK-p0-sim-determinism-analyzer
ticket: 26b2386e-f4df-4df7-b0f5-d7c5ba7add27
type: work
intake:
notes: WORK-p0-sim-determinism-analyzer.notes.md
status: Phase 5 — Complete PASS
---

# WORK-p0-sim-determinism-analyzer

> Pipeline spec (always-loaded contract). Detailed per-phase work lives in the
> paired `.notes.md`. Phase advance = updating the `status:` frontmatter above.

## Work Spec
- **Title:** P0 — the **sim-determinism ban analyzer**: a compile-time Roslyn
  `DiagnosticAnalyzer` that makes determinism-hostile constructs *un-compilable in
  simulation code* (`float`/`double`, `MathF`/transcendentals,
  `Microsoft.Xna.Framework.Vector2`, `foreach`-over-`Dictionary<,>`, `System.Random`,
  `DateTime`/`DateTimeOffset`), carved out so the MonoGame host/renderer is unaffected.
  The analyzer **D-0016** mandates "before any sim module."
- **Scope:**
  *In* — A new **`netstandard2.0` analyzer project** (e.g. `src/MonoRpgMaker.Analyzers`)
  holding the analyzer(s); the ban rules **scoped to the sim namespaces**
  (`MonoRpgMaker.Engine.{World,Entities,Data,Sim}` incl. `.Sim.Tracer`, plus
  `MonoRpgMaker.Abstractions`); the analyzer wired into the sim-bearing projects
  (`Engine`, `Abstractions`) so violations are **errors under `-warnaserror`** (gate:2);
  **verifier tests** (`Microsoft.CodeAnalysis.*.Testing`) for every rule + the host
  carve-out + the `FixedPoint` exemption; updating the now-contradictory
  `ArchitectureTests.cs:34-35` sim comment; gate integration (build/test/coverage/
  mutation treatment of the analyzer assembly). FULL gate green.
  *Out* — the **outcome-return purity** analyzer (separate D-0017 slice); the
  generator/validator/scaffolding (`monorpg scaffold`/`validate`, `contract-manifest.json`,
  `MRM0001..` DAG validation); any `Point`→`GridPoint` migration (`Point` stays allowed);
  splitting `Engine` into separate sim/host assemblies; rewriting existing sim code
  (no sim `float` exists today — the ban is by-construction going forward).
- **Systems:** **engine** (the sim namespaces the analyzer scopes to + the host carve-out)
  · **tooling/gate** (analyzer project, `-warnaserror`, coverage/mutation wiring) ·
  **tests** (analyzer verifier tests + the `ArchitectureTests` update) · **abstractions**
  (`FixedPoint` is the sanctioned sim numeric; the exemption boundary lives here).

## Acceptance Criteria (EARS)
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
| REQ-010 | The slice shall pass the FULL `bin/gate.sh` (format, build `-warnaserror`, tests, coverage ≥ floor, mutation MSI ≥ floor on the mutated projects). | gate run (FULL) |

## Locked-In Decisions
- **D-0016 governs.** Q16.16 `FixedPoint` is the *only* sim numeric type; this slice is
  exactly the analyzer D-0016 calls for ("bans `float`/`double`/`MathF`/`Vector2`/
  transcendentals + `foreach`-over-`Dictionary` in sim code"). Determinism is enforced
  **by construction** because bit-identical float across FMA/libm/JIT-vs-NativeAOT/CPU-arch
  is impossible — review is not enough.
- **A custom, namespace-scoped analyzer is the mechanism — NOT `BannedApiAnalyzers`.**
  Three independent reasons make this non-negotiable: (a) sim **and** host live in the
  **same assembly** (`MonoRpgMaker.Engine` holds both `Engine.Sim/World/Entities/Data` and
  `Engine.Core` the host), so per-project `AdditionalFiles` cannot separate them;
  (b) `foreach`-over-`Dictionary` is a **syntactic** pattern, not a banned symbol;
  (c) every banned construct is **legal in host, banned in sim** — only a namespace-aware
  analyzer expresses "banned here, fine there." (D-0016 scopes the rule to the
  sim/determinism path only.)
- **Sim boundary = the existing NetArchTest list.** Banned scope =
  `MonoRpgMaker.Engine.{World,Entities,Data,Sim}` (+ `.Sim.Tracer`) **and**
  `MonoRpgMaker.Abstractions`. Host carve-out = `MonoRpgMaker.Engine.Core`,
  `MonoRpgMaker.Player`, `MonoRpgMaker.Editor`. One source of the boundary, kept in lockstep
  with `ArchitectureTests`.
- **`Point` stays allowed; `Vector2` is banned.** XNA `Point` is integer (deterministic);
  `Vector2` is float-backed. No `Point`→`GridPoint` migration is forced by this slice.
- **Diagnostics are gate errors.** They surface as warnings in the IDE/dev loop and become
  build errors under `-warnaserror` (gate:2); IDs in the **`MRM####`** family (D-0017/D-0018).
  The gate:9 grep stays a cheap backstop; the analyzer is the primary (mirrors the
  `BannedApiAnalyzers` pattern already in `Directory.Build.props`).
- **`System.Random`/`DateTime` is a real gap, not "existing."** Verified: nothing bans them
  today. They are determinism-hostile and fold naturally into the same sim-scoped analyzer
  (host may legitimately use a clock/RNG for non-replayed cosmetics, which only namespace
  scoping preserves) — but the *mechanism* (this analyzer vs global `BannedApiAnalyzers`) is
  a Phase-2 call (see OPEN).
- **One slice.** The analyzer infra (project + verifier-test harness) is the bulk of the
  cost; doing all ~6 rules at once amortizes it and ships the determinism story whole. If
  Phase 2 judges it too large, the fallback split is *infra + float/double/MathF/Vector2*
  (slice A) then *foreach-over-Dictionary + Random/DateTime* (slice B) — recorded, not taken.

### Phase-2 design decisions — RESOLVED (rationale in `.notes.md` Phase 2)
- **Random/DateTime** → folded into the analyzer (MRM1004/1005), namespace-scoped (not global bans).
- **Packaging** → new `src/MonoRpgMaker.Analyzers` (`netstandard2.0`); analyzer `ProjectReference`
  (`OutputItemType=Analyzer`, `ReferenceOutputAssembly=false`) on **Engine + Abstractions only**;
  `Microsoft.CodeAnalysis.CSharp` 4.8.0; `AnalyzerReleases.{Shipped,Unshipped}.md` for RS2008.
- **Coverage/mutation** → analyzer tests **fold into** `tests/MonoRpgMaker.Engine.Tests` (the coverage
  gate parses one cobertura); gate:12 mutates `MonoRpgMaker.Analyzers` too; pure helpers carry the
  decision logic for mutation-kill (contingency owned by Validate, no silent skip).
- **FixedPoint exemption** → a `[DeterminismExempt]` attribute (Abstractions) on `ToDouble()`/`ToString()`.
- **Diagnostic ids** → the `MRM1xxx` band (MRM1001-1005), Category `Determinism`, Warning → error via `-warnaserror`.
- **NetArchTest** → kept for the Graphics-layer rule; only the stale `Vector2` comment is corrected (no redundant arch test).

## Linked Artifacts
- Design docs: `docs/decisions.md` (D-0016 determinism-by-construction — the governing
  decision; D-0017 float-ban + outcome-return analyzers; D-0014 don't-hack-core layering),
  `docs/roadmap.md` (P0 "Remaining P0", L61-62 + L76-78), `docs/agentic-substrate.md`
  (determinism substrate / the sim numeric + RNG primitives)
- Intake doc: none (slice chosen from the LOCAL roadmap per `PR-claude-forge-doc-staleness-001`)
- Ticket doc: docs/planning/tickets/open/TICKET-0007-p0-sim-determinism-analyzer.md
- Forge ticket: 26b2386e-f4df-4df7-b0f5-d7c5ba7add27 (#7, project monorpgmaker
  `a9ee8162-3cfd-4c99-be5c-bbdb4be32b10`)
- Builds on: WORK-p0-eventseam, WORK-p0-gridpoint, WORK-p0-irandom-rng,
  WORK-p0-abstractions-fixedpoint (completed)
- Ground-truth anchors: `BannedSymbols.txt` (bans only Process/Exit/FailFast today),
  `Directory.Build.props:35-41` (BannedApiAnalyzers + AdditionalFiles wiring to mirror),
  `bin/gate.sh:185-202` (gate:9 SAST backstop), `tests/MonoRpgMaker.Engine.Tests/ArchitectureTests.cs:32-50`
  (the sim-namespace boundary + the stale "may use … Vector2" comment)
- AAR: d02b3f1e-d79a-4eac-a046-e23e01886dc9

## Phase Plan
| Phase | Status |
|---|---|
| 1 — Plan | PASS |
| 2 — Design | PASS |
| 3 — Implement | PASS |
| 3.5 — Inspect | PASS |
| 4 — Validate | PASS |
| 5 — Complete | PASS |
