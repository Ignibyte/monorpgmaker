# WORK-p0-sim-determinism-analyzer — Notes

Per-phase working notes for the paired `.spec.md`. The spec is the contract;
this is the record of what happened.

## Phase 1 — Plan
- **Request:** P0 next slice — the **sim-determinism ban analyzer** (the compile-time
  Roslyn analyzer D-0016 mandates "before any sim module"): ban `float`/`double` /
  `MathF`/transcendentals / `Vector2` / `foreach`-over-`Dictionary` / `System.Random` /
  `DateTime` in SIM code only, host carved out. Auto-approved through `/commit`.
- **Intake source:** none. Slice chosen from the **LOCAL roadmap** (`docs/roadmap.md`
  P0 "Remaining P0", L61-62 + L76-78), per `PR-claude-forge-doc-staleness-001`. The forge
  `ticket-list` returned **zero open monorpgmaker tickets** — all 4 open rows were oathstar
  (`project_id faacfd78…`), the predecessor; the cross-project bearer bleeds them in.
- **Classification / tier:** work pipeline, **type = feature** (new analyzer assembly +
  compile-time enforcement; one cohesive shippable slice). The **largest P0 slice so far**
  (new `netstandard2.0` analyzer project + ~6 diagnostic rules + a verifier-test harness +
  cross-project wiring + gate integration) but coherent as "the determinism-ban analyzer";
  kept as one slice (fallback split recorded in the spec, not taken).
- **Forge recall (lessons/failures surfaced):**
  - **D-0016** (decisions.md L110-128) is the governing decision and *names this analyzer
    verbatim*: "an analyzer bans `float`/`double`/`MathF`/`Vector2`/transcendentals +
    `foreach`-over-`Dictionary` in sim code." The reflection-ban/determinism rule is scoped
    to the **sim/determinism path only** — directly motivating the host carve-out.
  - **D-0017** (L89-107) lists the float-ban analyzer alongside the outcome-return analyzer;
    the **outcome-return analyzer is OUT** of this slice (separate). Diagnostics use the
    structured **`MRM`** error family (D-0018).
  - **Ground-truth corrections to the request's premise** (verified by direct read, the key
    planning finding): there is **no existing `System.Random`/`DateTime` ban** — and no
    float ban either. `BannedSymbols.txt` bans only `Process`/`Exit`/`FailFast`;
    `bin/gate.sh:192` gate:9 greps only `Process.Start|Environment.(Exit|FailFast)` + `unsafe`;
    `ArchitectureTests.cs:34-35` *explicitly allows* `Vector2` in sim. So this slice is the
    **first** determinism-ban analyzer, and it must also **update the stale arch-test comment**.
  - **The scoping crux (Explore agent):** sim **and** host live in the **same assembly**
    (`MonoRpgMaker.Engine` = `Engine.Sim/World/Entities/Data` + `Engine.Core` host) ⇒
    per-project `BannedApiAnalyzers` cannot separate them ⇒ a **custom namespace-scoped
    `DiagnosticAnalyzer`** is required (also because `foreach`-over-`Dictionary` is syntactic,
    and every banned construct is legal-in-host/banned-in-sim).
  - **`FixedPoint.ToDouble()` subtlety:** `FixedPoint` (Abstractions — a banned scope) exposes
    `double ToDouble()` for diagnostics; the analyzer must **exempt** this sanctioned boundary
    (mechanism is a Phase-2 call).
  - **Gate-mutation note** (`AD-claude-gate-mutates-all-projects-001`): gate:12 mutates every
    *production* project (`Engine` + `Abstractions` today). Whether an **analyzer** assembly is
    "production" (mutated) or "tooling" (verifier-tested only) is a Phase-2/Validate decision;
    mind the equivalent-mutant trap (`PR-claude-stryker-unsigned-shift-equivalent-001`).
  - **Forge hygiene:** `knowledge-search` hits were opaque UUIDs with no project attribution;
    no oathstar (Rust/Datastar/Tauri) content was used. Named rules above came from the
    completed P0 `.notes.md` pairs, which are reliable.
- **Ticket:** **26b2386e-f4df-4df7-b0f5-d7c5ba7add27 (#7)** — created with explicit
  `project_id a9ee8162`; the returned row confirms it landed under **monorpgmaker** (#1–#6
  done/landed, #7 open). Local doc: `docs/planning/tickets/open/TICKET-0007-p0-sim-determinism-analyzer.md`.
- **AAR id (from `aar-open`):** d02b3f1e-d79a-4eac-a046-e23e01886dc9
- **EARS requirements reviewed:** REQ-001..010 (float/double, MathF/transcendentals, Vector2
  (+Point-not-flagged), foreach-over-Dictionary, Random/DateTime, host carve-out, FixedPoint
  exemption, `-warnaserror` enforcement, ArchitectureTests comment, FULL gate).

## Phase 2 — Design

### Resolved OPEN-Phase-2 questions
1. **Random/DateTime mechanism → fold into the custom sim-scoped analyzer** (MRM1004 `System.Random`,
   MRM1005 `DateTime`/`DateTimeOffset`), NOT global `BannedApiAnalyzers`. The host may legitimately
   use a clock/RNG for non-replayed cosmetics; only namespace scoping preserves that, and one analyzer
   keeps the determinism rules single-source (D-0016 "sim/determinism path only"). `TimeSpan` (a pure
   duration) is **not** banned.
2. **Packaging → new `src/MonoRpgMaker.Analyzers` (`netstandard2.0`)**, the Roslyn requirement.
   `Microsoft.CodeAnalysis.CSharp` pinned **4.8.0** (≤ the SDK 10.0.301 Roslyn → no RS1041; forward-
   compatible at run) + `Microsoft.CodeAnalysis.Analyzers` **3.3.4** (the RS-rule pack), both
   `PrivateAssets="all"`. Consumed by **Engine + Abstractions only** via
   `<ProjectReference … OutputItemType="Analyzer" ReferenceOutputAssembly="false"/>` — **per-project,
   never in `Directory.Build.props`** (that would self-reference the analyzer + wire it into hosts).
   `EnforceExtendedAnalyzerRules=true`; ship `AnalyzerReleases.{Shipped,Unshipped}.md` so **RS2008**
   doesn't red the build under `-warnaserror`.
3. **Coverage + mutation → fold analyzer tests INTO `tests/MonoRpgMaker.Engine.Tests`.** The coverage
   gate parses a *single* cobertura (`net_cov`: `find … | head -1`); a 2nd test project would make
   gate:11 ambiguous. gate:12 mutates the analyzer by adding `MonoRpgMaker.Analyzers.csproj` to the
   existing mutation loop (same `testdir`). Mutation-resilience: the decision-dense logic lives in
   **pure `internal static` helpers** (`InternalsVisibleTo` the test project) with plain xUnit tests;
   the `DiagnosticAnalyzer` wiring is thin and covered by verifier tests. **Contingency (owned by
   Validate):** if Stryker cannot instrument the `netstandard2.0` analyzer, document it explicitly +
   compensate with branch coverage — never a silent skip, never a lowered floor (§0).
4. **FixedPoint exemption → a `[DeterminismExempt]` attribute in Abstractions** (the determinism
   contract surface), applied to `FixedPoint.ToDouble()` **and** `FixedPoint.ToString()` (ToString
   calls ToDouble → double-typed). The analyzer skips any node whose containing member/type bears it.
   Reusable for future sanctioned boundaries (save-migration readers etc., per D-0016 carve-outs).
   Narrow by design — the rest of `FixedPoint` is `int`/`long` and stays analyzed.
5. **MRM id layout → the `MRM1xxx` band** (leaving `MRM0xxx` for the D-0018 validator):
   **MRM1001** floating-point (`float`/`double`/`MathF`/transcendental `System.Math`), **MRM1002**
   `Vector2` (+ the float-backed XNA math family `Vector3`/`Vector4`/`Matrix`/`Quaternion`), **MRM1003**
   `foreach`-over-`Dictionary<,>`, **MRM1004** `System.Random`, **MRM1005** `DateTime`/`DateTimeOffset`.
   Category `Determinism`, default severity **Warning** → hard error via `-warnaserror` at the gate
   (matches the repo posture: visible in the dev loop, blocking at `/commit`).
6. **NetArchTest vs analyzer → keep the NetArchTest** for the Graphics-layer dependency rule (its
   purpose); the analyzer + verifier tests own *construct-level* bans. Only **fix the stale comment**
   (REQ-009); no redundant `Vector2` arch test.

### Approach / architecture
- **One `DiagnosticAnalyzer`** (`SimDeterminismAnalyzer`) in the new `netstandard2.0` analyzer assembly.
  `Initialize` → `ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None)` +
  `EnableConcurrentExecution()` → `RegisterCompilationStartAction`. On start, resolve the banned
  `INamedTypeSymbol`s once (`System.Single/Double/MathF/Math`, `Random`, `DateTime`, `DateTimeOffset`,
  `Dictionary<,>` + its `KeyCollection`/`ValueCollection`, the XNA `Vector*`/`Matrix`/`Quaternion`,
  `MonoRpgMaker.Abstractions.DeterminismExemptAttribute`) — types absent from a given compilation
  (e.g. no MonoGame ref) simply yield no Vector2 rule, no crash.
- **Scope gate (the crux).** Sim + host share the `MonoRpgMaker.Engine` assembly, so scoping is
  **per-node by containing namespace**, not per-assembly. A pure helper
  `SimScope.IsSimNamespace(string ns)` does **segment-aware** prefix matching
  (`ns == p || ns.StartsWith(p + ".")`) over the 5 sim prefixes — `MonoRpgMaker.Engine.{World,Entities,
  Data,Sim}` + `MonoRpgMaker.Abstractions` — so `…Engine.Core` (host) and a hypothetical `…Engine.Database`
  do **not** match `…Data`. The list mirrors `ArchitectureTests.cs` (single source of the boundary).
- **Detection.** Register operation/syntax actions, each guarded by `IsSimNamespace(containingNamespace)`
  and `!IsExempt(containingMember/type)`:
  - *MRM1001* — `float`/`double` introduced into sim: predefined-type syntax in declarations, float/double
    literals, casts to float/double, and call/member expressions typed `Single`/`Double` (catches
    `MathF.*`, `Math.Sqrt`). One diagnostic per offending node; verifier tests pin the spans.
  - *MRM1002* — any reference to a banned XNA float-math type (`Vector2` family / `Matrix` / `Quaternion`);
    XNA `Point` (integer) is **never** flagged.
  - *MRM1003* — `ForEachStatement` whose iterated expression's type is `Dictionary<,>` (or `.Keys`/`.Values`).
    Ordered/deterministic sources (`SortedDictionary`, `List<>`, arrays, `IOrderedEnumerable` via `.OrderBy`)
    are **not** flagged — the remediation (`foreach (… in d.OrderBy(…))`) passes by construction.
  - *MRM1004/1005* — references to `System.Random` / `DateTime` / `DateTimeOffset` in sim.
- **Exemption.** `IsExempt(symbol)` walks the containing member → type and returns true if either bears
  `[DeterminismExempt]`. Applied to `FixedPoint.ToDouble()`/`ToString()` so the sanctioned double
  boundary compiles clean while the rest of Abstractions stays analyzed.
- **Conventions (§14):** the analyzer is pure/deterministic (no statics holding state, no I/O); pure
  helpers are `internal static`; nullable-clean; XML-doc on the public analyzer + attribute. An analyzer
  `ProjectReference` with `ReferenceOutputAssembly=false` is **not** a runtime assembly dependency, so the
  Abstractions purity ring (`Abstractions_must_not_depend_on_MonoGame_Engine_or_hosts`) stays green.

### File manifest
| # | File | Change |
|---|---|---|
| 1 | `src/MonoRpgMaker.Analyzers/MonoRpgMaker.Analyzers.csproj` | **NEW.** `netstandard2.0`; `Microsoft.CodeAnalysis.CSharp` 4.8.0 + `Microsoft.CodeAnalysis.Analyzers` 3.3.4 (PrivateAssets); `EnforceExtendedAnalyzerRules=true`; `IsPackable=false`; `InternalsVisibleTo("MonoRpgMaker.Engine.Tests")`. |
| 2 | `src/MonoRpgMaker.Analyzers/SimDeterminismAnalyzer.cs` | **NEW.** The `DiagnosticAnalyzer`: CompilationStart → resolve symbols → namespace-scoped actions → report MRM1001-1005; generated-code-skip + concurrent. |
| 3 | `src/MonoRpgMaker.Analyzers/SimScope.cs` | **NEW.** Pure `internal static` helpers: segment-aware `IsSimNamespace`, `IsExempt`, banned-type classification. The mutation-dense logic. |
| 4 | `src/MonoRpgMaker.Analyzers/DeterminismDiagnostics.cs` | **NEW.** The five `DiagnosticDescriptor`s (MRM1001-1005), Category `Determinism`, Warning, help text. |
| 5 | `src/MonoRpgMaker.Analyzers/AnalyzerReleases.Shipped.md` + `AnalyzerReleases.Unshipped.md` | **NEW.** Release tracking (RS2008) — list MRM1001-1005 in Unshipped. |
| 6 | `src/MonoRpgMaker.Abstractions/DeterminismExemptAttribute.cs` | **NEW.** `[AttributeUsage(Method\|Property\|Class\|Struct)]` sanctioned-boundary marker; XML-doc'd. |
| 7 | `src/MonoRpgMaker.Abstractions/FixedPoint.cs` | **MODIFY.** `[DeterminismExempt]` on `ToDouble()` and `ToString()`. |
| 8 | `src/MonoRpgMaker.Engine/MonoRpgMaker.Engine.csproj` | **MODIFY.** Analyzer `ProjectReference` (OutputItemType=Analyzer, ReferenceOutputAssembly=false). |
| 9 | `src/MonoRpgMaker.Abstractions/MonoRpgMaker.Abstractions.csproj` | **MODIFY.** Same analyzer `ProjectReference`. (Analyzer ref ≠ runtime dep → purity ring holds.) |
| 10 | `tests/MonoRpgMaker.Engine.Tests/MonoRpgMaker.Engine.Tests.csproj` | **MODIFY.** Add `Microsoft.CodeAnalysis.CSharp.Analyzer.Testing` (xunit verifier) + a plain `ProjectReference` to the analyzer. |
| 11 | `tests/MonoRpgMaker.Engine.Tests/SimDeterminismAnalyzerTests.cs` | **NEW.** Verifier tests (per-rule positive, host negative, Point/TimeSpan/SortedDictionary negative, exemption) + a base that adds MonoGame + Abstractions refs to the snippet compilation. |
| 12 | `tests/MonoRpgMaker.Engine.Tests/SimScopeTests.cs` | **NEW.** Plain xUnit tests for the pure helpers (segment-aware matching incl. `Data`≠`Database`; exemption detection). |
| 13 | `tests/MonoRpgMaker.Engine.Tests/ArchitectureTests.cs` | **MODIFY.** Correct the stale "may use … Vector2" comment (REQ-009); keep the Graphics assertion. |
| 14 | `MonoRpgMaker.slnx` | **MODIFY.** Add the analyzer project under `/src/`. |
| 15 | `bin/gate.sh` | **MODIFY.** gate:12 loop adds `MonoRpgMaker.Analyzers.csproj` (shellcheck-clean, gate:7). |
| 16 | `**/packages.lock.json` (analyzer, Engine, Abstractions, test) | **REGEN.** Locked restore after the new refs; commit. |

### Regression Test Plan
| # | Test | Proves Requirement |
|---|---|---|
| T1 | `Float_and_double_in_sim_report_MRM1001` — double/float local+field+param+return in a `…Engine.Sim` snippet → MRM1001 at each span. | REQ-001 |
| T2 | `MathF_and_Math_transcendental_in_sim_report_MRM1001` — `MathF.Sin`, `Math.Sqrt` in sim → MRM1001. | REQ-002 |
| T3 | `Vector2_in_sim_reports_MRM1002_but_Point_does_not` — `Vector2` field → MRM1002; `Point` field → none. | REQ-003 |
| T4 | `Foreach_over_Dictionary_reports_MRM1003_ordered_sources_do_not` — `Dictionary<,>`/`.Keys` → MRM1003; `List<>`, `dict.OrderBy(…)`, `SortedDictionary<,>` → none. | REQ-004 |
| T5 | `Random_and_DateTime_in_sim_report_MRM1004_1005_TimeSpan_does_not` — `Random`/`DateTime`/`DateTimeOffset` → MRM1004/1005; `TimeSpan` → none. | REQ-005 |
| T6 | `Host_namespaces_report_nothing` — the same constructs in `…Engine.Core`, `…Player`, `…Editor` snippets → zero diagnostics. | REQ-006 |
| T7 | `DeterminismExempt_member_suppresses_diagnostics` — a sim method marked `[DeterminismExempt]` using `double` → none; the real `FixedPoint` compiles clean under the analyzer. | REQ-007 |
| T8 | `Sim_violation_is_error_under_warnaserror` — an in-memory compilation of a violating sim snippet asserts the diagnostic at **Error** effective-severity when warnaserror is on (verifier severity assertion; no violating file is committed). | REQ-008 |
| T9 | `ArchitectureTests` suite green + comment corrected (review). | REQ-009 |
| T10 | FULL `bin/gate.sh` GREEN — format, build `-warnaserror`, tests, coverage ≥80%, mutation MSI ≥80% incl. `MonoRpgMaker.Analyzers`. | REQ-010 |
| T11 | `SimScopeTests` (pure) — `IsSimNamespace` true for the 5 prefixes + `…Sim.Tracer`, false for host + the `Data`/`Database` collision; `IsExempt` true/false. | REQ-001/006 robustness + mutation kill |

**Uncoverable paths:** none material — the analyzer executes in-process under the verifier (fully
coverable); the host carve-out is covered by negative tests; no live MonoGame loop is involved. No
`[ExcludeFromCodeCoverage]` needed.

### Risks / decisions
- **R1 (top) — Stryker on a `netstandard2.0` Roslyn analyzer** may need config or fail to instrument.
  *Mitigation:* the pure helpers carry the logic (plain-unit-tested → high kill); verifier tests kill
  wiring mutants; equivalent mutants handled by refactor-to-killable (`PR-claude-stryker-unsigned-shift-
  equivalent-001`), never suppression. *Contingency (Validate):* document + compensate, no silent skip.
- **R2 — analyzer-author rules under `-warnaserror`** (RS2008 release tracking, RS1026, …). *Mitigation:*
  ship `AnalyzerReleases.{Shipped,Unshipped}.md`; `EnforceExtendedAnalyzerRules=true`; keep registrations RS-clean.
- **R3 — `Microsoft.CodeAnalysis.CSharp` version** too new → RS1041. *Mitigation:* pin 4.8.0 (≤ SDK Roslyn); Validate confirms.
- **R4 — coverage gate's single-cobertura (`head -1`)** drove the fold-in. A future 2nd test project must upgrade `net_cov` to aggregate. Recorded.
- **R5 — Abstractions purity ring** must survive the analyzer `ProjectReference` (`ReferenceOutputAssembly=false` ⇒ not an assembly dep). Validate asserts the NetArchTest stays green.
- **R6 — license + lock churn:** new deps are MIT/Apache (allowlist OK); regenerate + commit `packages.lock.json`; locked restore (gate) + gate:4/5 confirm.
- **R7 — false positives / granularity:** over-flagging float sub-expressions reads as noise; the test plan pins exact spans and the `Point`/`TimeSpan`/`SortedDictionary`/`List`/`.OrderBy` negatives guard against over-reach.
- **Reversible-but-load-bearing decisions:** fold-in vs separate test project (revisit with a `net_cov` upgrade); MRM1xxx band (ids unshipped); Warning+warnaserror vs Error (repo posture); `[DeterminismExempt]` attribute vs hardcoded allow-list (the durable, extensible choice).
- **Implement note:** the `Microsoft.CodeAnalysis.CSharp.Analyzer.Testing` verifier API shifted across 1.1.x — wire the xunit verifier per the resolved package version (built-in `DefaultVerifier` vs the `.XUnit` companion).

## Phase 3 — Implement
- **Built:**
  - New `src/MonoRpgMaker.Analyzers` (`netstandard2.0`): `SimDeterminismAnalyzer` (MRM1001-1005),
    `SimScope` (pure helpers), `DeterminismDiagnostics` (descriptors), `AnalyzerReleases.{Shipped,
    Unshipped}.md`, csproj (`Microsoft.CodeAnalysis.CSharp` 4.8.0 + `.Analyzers` 3.3.4,
    `EnforceExtendedAnalyzerRules`, `InternalsVisibleTo` the test project).
  - `DeterminismExemptAttribute` in Abstractions; applied to `FixedPoint.ToDouble()`.
  - Analyzer wired into Engine + Abstractions (`OutputItemType=Analyzer`, `ReferenceOutputAssembly=false`);
    added to `MonoRpgMaker.slnx`; gate:12 mutation loop now includes `MonoRpgMaker.Analyzers.csproj`;
    `ArchitectureTests` Vector2 comment corrected (REQ-009). Lock files regenerated.
  - **Detection** (operation/symbol-based, by resolved TYPE): declarations (field/property/method-return/
    parameter), locals (`var`-safe), object creations, `MathF`/`Math`-double invocations, static
    `MathF`/`Math`/`DateTime` member refs, **binary/unary float arithmetic**, and `foreach`-over-`Dictionary`
    (Keys/Values). Generated-code skip + concurrent execution.
  - **Verified by build:** solution GREEN under `-warnaserror` — no false positives on real sim code
    (`Math.Max(int)`, Dictionary fields, XNA `Point`, doc-comment crefs, FixedPoint internals all clean).
    Throwaway smoke fixtures (deleted) confirmed all five rules FIRE in sim, that `(int)(x*1.5)` flags
    MRM1001, and that `Math.Max(int)` / `Point` / integer arithmetic / `FixedPoint` arithmetic do NOT.
- **Deviations from design (+ reason):**
  1. **`BannedSymbols` nested inside `SimDeterminismAnalyzer.cs`** (not a separate file) — keeps the
     3-file analyzer manifest; it is the per-compilation symbol-resolution table.
  2. **Exempted `FixedPoint.ToDouble()` ONLY, not `ToString()`** — under type-based detection,
     `ToString()` (delegates to `ToDouble()` then formats) introduces no float type/literal/MathF/
     arithmetic, so it never triggers; the narrower exemption keeps more of `FixedPoint` analyzed.
  3. **Added a binary/unary arithmetic rule** (operation result-type `float`/`double` ⇒ MRM1001) beyond
     the design's declaration/cast/literal list — closes the inline `(int)(x * 1.5)` soundness gap;
     integer/`FixedPoint`/`GridPoint` arithmetic is not float-typed so it is not flagged. Dropped the
     standalone-literal + explicit-cast rules (redundant with declaration + arithmetic detection; avoids
     double-reporting on simple declarations).
  4. **Incidental pre-existing fix (documented):** a clean `-warnaserror` rebuild on SDK 10.0.301 surfaced
     a pre-existing **CA1859** in `EventContextTests.cs` (untouched by this slice — `git diff HEAD` empty;
     **verified pre-existing via a HEAD worktree build**). Fixed with a test-scoped `.editorconfig` rule
     (`dotnet_diagnostic.CA1859.severity = none` under `[tests/**.cs]`), mirroring the existing
     CA1707/CA1515 test suppressions and the repo's documented "tune inapplicable rules in editorconfig,
     not inline" convention (`.editorconfig` L12-14). Production CA1859 stays enforced.
  5. **Deferred to Phase 4** (per the implement skill — no test authoring in Phase 3): the verifier +
     unit tests (`SimDeterminismAnalyzerTests`, `SimScopeTests`) and the test project's
     `Microsoft.CodeAnalysis.CSharp.Analyzer.Testing` package + plain analyzer `ProjectReference`. The
     gate:12 analyzer-mutation entry becomes functional once that reference lands.

## Inspect (Phase 3.5)
- **Lenses run:** 4 independent critics — (1) analyzer correctness (false pos/neg), (2) D-0016
  design-fidelity / ban-coverage, (3) build/gate integrity (ran real builds), (4) simplification/reuse.
- **Findings + verdicts:**

  | # | Sev | Finding (file) | Verdict | Resolution |
  |---|---|---|---|---|
  | F1 | CRIT | **gate:5 license RED** — the `netstandard2.0` analyzer transitively pulls `Microsoft.NETCore.Platforms` 1.1.0 + `NETStandard.Library` 2.0.3, whose license URLs nuget-license can't resolve (`.config/nuget-license-overrides.json`) | **REAL** — verified new vs HEAD (HEAD `[]`, slice 2 violations); blocks `/commit` | **FIXED** — vetted id+version overrides → `MIT` (both are .NET Foundation MIT; stale metadata URLs). gate:5 now `[]` |
  | F2 | CRIT | **`float[]` / `float?` / `double[]` not flagged** — `ClassifyType` only matched `Single`/`Double` directly (SimDeterminismAnalyzer.cs) | **REAL** float-state hole | **FIXED** — `ClassifyType` now unwraps arrays + `Nullable<T>` to the core type. Smoke: `float[]`→MRM1001, `double?`→MRM1001, `int[]`→clean |
  | F3 | CRIT | **`HashSet<>` foreach not flagged** — only Dictionary; same unordered-enumeration hazard (D-0016 intent) | **REAL** determinism hole | **FIXED** — `IsDictionaryEnumeration`→`IsUnorderedEnumeration` + `HashSet`; MRM1003 broadened to "unordered collection". Smoke: `HashSet`→MRM1003, `SortedDictionary`/`List`→clean |
  | F4 | HIGH | **`Environment.TickCount`/`TickCount64` not flagged** — ambient OS clock (MRM1005 intent) | **REAL** ambient-clock hole | **FIXED** — added to `ClassifyStaticMember`→MRM1005. Smoke verified |
  | F5 | MED | **`AnalyzeLoop` unwrapped only one `IConversionOperation` layer** | **REAL** (minor robustness) | **FIXED** — `while`-loop unwrap |
  | F6 | LOW | **CA1859 `.editorconfig` suppression — pre-existing + right fix?** | **CONFIRMED correct** — Critic 3 independently reproduced CA1859 at pristine HEAD (worktree build); test-scoped editorconfig matches the CA1707/CA1515 convention + the editorconfig "tune here, not inline" comment | No change |
  | F7 | — | **gate:12 Stryker analyzer entry non-functional until the Phase-4 test reference lands** | **EXPECTED** by design; FULL gate would red at gate:12 today | Deferred to **Validate** (R1 contingency: confirm Stryker can drive the `netstandard2.0` analyzer; else document + compensate, no silent skip) |
  | F8 | HIGH | foreach over `IDictionary<,>` **interface** not flagged | **REAL but DEFERRED** | Banning interface-typed foreach would **false-positive** on `SortedDictionary`-via-interface (ordered). Concrete unordered types are covered; documented limitation |
  | F9 | HIGH | `ConcurrentDictionary`/`ImmutableDictionary` enumeration not flagged | **REAL but DEFERRED** | Rare in single-threaded deterministic game sim; the analyzer ratchets. Documented follow-up |
  | F10 | CRIT(critic)→LOW | compound assignment `x += 1.5` not flagged | **REJECTED as a new hole** | The target must be `float`/`double` to compile ⇒ already flagged at its declaration. No new hole. Documented |
  | F11 | HIGH/MED | lambda/delegate/indexer float params; `List<float>`/`Func<float>` (float in non-array/nullable generics) not flagged | **REAL but DEFERRED** | Niche; float surfaces when used directly. A full recursive generic-arg walk risks surprising diagnostics. Documented limitations |
  | F12 | MED | foreach over `dict.Where(...)` (LINQ-wrapped) not flagged | **REJECTED (inherent)** | Needs flow analysis; the `.OrderBy(...)` remediation passes by construction. Out of scope |
  | F13 | MED | `DateOnly`/`TimeOnly`/`Stopwatch` not banned | **REAL but DEFERRED** | Newer/niche ambient types, not in use. Low-priority follow-up |
  | F14 | MED | double-reporting (declaration + object-creation/arithmetic) | **REJECTED (intentional)** | Acceptable for a ban analyzer (Critic 4 concurs); both are true positives |
  | F15 | — | namespace scoping (`Data`≠`Database`), exemption walk, `Math.Max(int)`/`Point`/`Rectangle`/`Color`, MRM1xxx band (no MRM0xxx collision), concurrency, null-handling, idiom/XML-doc | **VERIFIED CORRECT** by Critics 1/2/4 | No change |

- **Deferred-limitations summary (recorded for a follow-up hardening slice):** `IDictionary<,>`/`ISet<>`
  interface-typed foreach (false-positive risk on ordered impls); `ConcurrentDictionary`/`ImmutableDictionary`/
  `ImmutableHashSet`; float nested in non-array generics (`List<float>`, `Func<float>`); lambda/delegate/indexer
  float params; LINQ-wrapped unordered enumeration; `DateOnly`/`TimeOnly`/`Stopwatch`. None are reachable by the
  current (empty) sim; the analyzer ratchets like the gate floors.
- **Post-fix verification:** solution GREEN under `-warnaserror` (0/0); `dotnet format --verify-no-changes` clean
  (gate:1); gate:5 license `[]`; NetArchTest purity ring intact (Critic 3: 3/3 Architecture tests pass); smoke
  fixtures (deleted) confirmed F2/F3/F4 fire and ordered-collection/`Point`/`int[]` negatives stay clean.
- **Forge capture:** `failure-record` BF-sim-determinism-analyzer-gaps-001 (the F2/F3/F4 false-negatives) +
  BF-netstandard2-license-gate-001 (the F1 license regression); `prevention-rule-record`
  PR-claude-type-ban-analyzer-core-type-001 (classify the core type; cover the determinism intent, not the literal
  list) + PR-claude-netstandard2-license-overrides-001 (pre-add MIT overrides for the netstandard2.0 platform packages).

## Phase 4 — Validate
- **Tests added** (folded into `tests/MonoRpgMaker.Engine.Tests`):
  - `SimScopeTests.cs` — pure unit tests for the segment-aware namespace matcher (true for the 5 sim
    prefixes + nested `…Sim.Tracer`; false for host + the `Data`/`Database` & `Sim`/`Simulation`
    collisions + null/empty). Kills the highest-logic-density mutants directly.
  - `SimDeterminismAnalyzerTests.cs` — verifier tests via a **hand-rolled net10 harness**: the snippet
    compiles against the exact running runtime (trusted-platform-assemblies) + MonoGame + Abstractions,
    avoiding the `Microsoft.CodeAnalysis.Testing` package's pre-net10 reference sets (which would
    version-mismatch the net10 `Abstractions.dll`). **Isolated, exact-diagnostic** tests per banned
    symbol + path: each float/clock/XNA type, Dictionary/Keys/Values/HashSet foreach, Math/MathF,
    inline arithmetic, `float[]`/`float?`, object creation, static ambient reads, the host carve-out
    (every path), the `[DeterminismExempt]` exemption, doc-cref + global-namespace negatives, the
    declaration+construction double-report, the symbol location, the message-names-the-member content,
    and the descriptor shape (Warning/enabled/Determinism/`{0}`+D-0016).
  - Test project wired: `Microsoft.CodeAnalysis.CSharp` 4.8.0 + a plain analyzer `ProjectReference`
    (the analyzer's internal `SimScope` is reachable via `InternalsVisibleTo`).
- **`dotnet test MonoRpgMaker.slnx`: Passed — 146 passed, 0 failed** (was 78 pre-slice; +68).
- **`bin/gate.sh`: GREEN [full]** — all 12 gates pass. Coverage **97.7%** ≥ 80; mutation MSI
  **Engine 91.30% / Abstractions 82.22% / Analyzers 92.35%** (all ≥ 80). Receipt written.
- **gate:12 R1 risk — RESOLVED:** Stryker **does** mutate the `netstandard2.0` analyzer (no tooling
  limitation); the analyzer MSI clears the floor at **92.35%** — no exclusion, no lowered floor, no
  silent skip. *Iteration:* the first analyzer mutation run was **55.88%** because the initial verifier
  tests asserted only the *distinct* diagnostic-ID set, so a mutant disabling one banned symbol survived
  when another construct in the same snippet produced the same id. Rewriting to **isolated, exact-count**
  tests per symbol/path + message-content + descriptor assertions took it to 92.35%. The ~13 residual
  survivors are genuinely equivalent/defensive (the `ConfigureGeneratedCodeAnalysis`/`EnableConcurrentExecution`
  removals, the `Locations[0]` ternary where a declared symbol's location is never empty, the `type is null`
  guard blocks, the unused not-banned `name` string).
- **Pre-existing exclusions:** none. (The Phase-3 CA1859 `.editorconfig` fix is already recorded as an
  incidental pre-existing repair; it is not a Phase-4 test exclusion.) `StrykerOutput/` is gitignored —
  no mutation artifacts are committed.

## Phase 5 — Complete
- Docs updated:
- Forge capture (aar/failures/rules/decisions):
- Ticket closed:
- Archived:
