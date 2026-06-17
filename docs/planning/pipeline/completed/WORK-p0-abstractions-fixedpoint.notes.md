# WORK-p0-abstractions-fixedpoint — Notes

Per-phase working notes for the paired `.spec.md`. The spec is the contract;
this is the record of what happened.

## Phase 1 — Plan
- Request: P0 slice 1 — stand up `MonoRpgMaker.Abstractions` + the `FixedPoint`
  (Q16.16) deterministic sim numeric primitive (D-0016), with NetArchTest layering;
  FULL gate green. First of several P0 (chassis) slices.
- Intake source: none.
- Classification / tier: work pipeline, **type = feature** (new assembly + primitive;
  one shippable slice).
- Forge recall (lessons/failures surfaced):
  - **D-0016** (decisions.md) is the governing decision: Q16.16 `FixedPoint` in
    Abstractions = the only sim numeric type; floats banned in sim by a (later)
    analyzer; replay-to-same-hash on NativeAOT + ARM64/x64. `agentic-substrate` §5
    lists FixedPoint + IRandom as the first two engine primitives.
  - **Grounding:** `Directory.Build.props` applies shared settings to every project
    (Nullable enable, ImplicitUsings **disable**, latest-recommended analyzers,
    BannedApiAnalyzers + `packages.lock.json`); `GenerateDocumentationFile=false`
    (XML docs are convention, not CS1591-enforced). `BannedSymbols.txt` bans only
    Process/Exit/FailFast — Random/DateTime + the future float ban live in the
    gate:9 grep, so `double` is currently allowed (debug `ToDouble` is safe for now).
    Solution = Editor/Engine/Player + Tests; **no Abstractions/FixedPoint/IRandom
    exist yet** (greenfield). `ArchitectureTests.cs` already asserts Engine⊥Player/Editor
    and sim⊥Graphics — extend it with the Abstractions ring.
  - **Forge hygiene:** an oathstar `WORK-levels-v1` doc (Rust/datastar) bled into the
    recall — discarded per CLAUDE.md.
  - **Process lesson applied** ([[PR-claude-forge-doc-staleness-001]]): this slice was
    chosen by reading the **local** roadmap (P0 greenlit), not the stale forge index.
- Ticket: **a0d9d476-1127-4f2a-9add-eb7660d56384 (#3)** — created with explicit
  `project_id a9ee8162`; returned row confirms it landed under **monorpgmaker**
  (scoping fix still holding; monorpgmaker now #1/#2 done, #3 open).
- AAR id (from `aar-open`): 928e71d0-7fce-4fe9-9be7-a7aabbdcfb35
- EARS requirements reviewed: REQ-001..007 (round-trip, integer-backed arithmetic,
  equality/ordering, helpers, Abstractions purity, layering ring, FULL gate).

## Phase 2 — Design

### Approach / architecture
A new **pure assembly** `MonoRpgMaker.Abstractions` (net10.0, NO MonoGame ref;
inherits `Directory.Build.props` → Nullable, latest-recommended analyzers,
BannedApiAnalyzers, lock file). It holds **`FixedPoint`** — a `readonly struct`
backed by one `Int32` raw value (Q16.16: 16 fractional bits, `One = 1<<16 = 65536`):

- **Construction/constants:** `FromInt(int)` (`raw = value << 16`), `FromRaw(int)`,
  `Raw` getter, `Zero`, `One`.
- **Arithmetic (integer-only, all wrapped in explicit `unchecked` so behaviour is
  independent of any `CheckForOverflowUnderflow` setting — determinism by
  construction):** `+`/`-`/unary`-` on raw; `*` = `(int)(((long)a.Raw*b.Raw)>>16)`;
  `/` = `(int)(((long)a.Raw<<16)/b.Raw)`. The `Int64` intermediate preserves scale.
- **Equality/ordering:** `IEquatable<FixedPoint>` + `Equals(object)` + `GetHashCode`
  (= `Raw`) + `==`/`!=`/`<`/`>`/`<=`/`>=` + `IComparable<FixedPoint>` — all over `Raw`
  (required anyway by CA1815/CS0660-661 once `==` exists).
- **Helpers:** `ToInt()` (truncate toward zero = `Raw/65536`); `Floor`/`Ceiling`/
  `Round` returning `FixedPoint` (Floor=`Raw>>16`; Ceiling=`(Raw+65535)>>16`;
  Round=half-away-from-zero); `Abs` (branch, not `Math.Abs` — avoids its
  `int.MinValue` throw); `Min`/`Max`; `ToString` (culture-invariant); a debug-only
  `ToDouble()` = `(double)Raw/65536`.

`FixedPoint` is **forward-looking infrastructure** — the int-grid tracer sim is NOT
converted in this slice. Engine does **not** reference Abstractions yet (decision 3).

### The 4 open decisions — settled
1. **Overflow = `unchecked` wraparound** (the language default, made explicit). No
   `checked` (exceptions in arithmetic break replay/determinism) and no saturating
   (extra branches/mutation surface for no M0 need). Representable range ±32768;
   documented. *Reversible.*
2. **Rounding:** division **truncates toward zero**; `ToInt` truncates toward zero;
   `Floor`→−∞, `Ceiling`→+∞, `Round`= **half away from zero**. One deterministic rule
   each, pinned by pos+neg tests.
3. **Engine→Abstractions edge = DEFER.** The arch test inspects the Abstractions
   assembly directly (`typeof(FixedPoint).Assembly`), so no consumer edge is needed;
   adding an unused `ProjectReference` now would be dead weight. Engine wires it the
   slice it first uses `FixedPoint`.
4. **Keep `ToDouble`** — verified against `bin/gate.sh`: gate:9 only greps
   `Process.Start`/`Environment.Exit`/`unsafe`, and `BannedSymbols.txt` bans only
   those three. `double` is not banned anywhere today. (When the float-ban analyzer
   lands in a later slice it must exempt the `FixedPoint` primitive itself.)

### ⚠️ Scope addition — the mutation gate must cover the new assembly
`bin/gate.sh` gate:12 runs `dotnet stryker --project MonoRpgMaker.Engine.csproj` — it
mutates **only Engine**. `FixedPoint`'s arithmetic would ship **unmutated** (the FULL
gate would still go green because Engine's MSI is unchanged) — a hole in §0's "shippable
= mutation-guarded". Fix: extend `mutation_g` to loop Stryker over **Engine *and*
Abstractions**, flooring each at `MUT_MSI_MIN`. (Coverage gate:11 already includes
Abstractions automatically — coverlet instruments every assembly the tests load.)

### File manifest
| # | File | Change |
|---|---|---|
| 1 | `src/MonoRpgMaker.Abstractions/MonoRpgMaker.Abstractions.csproj` | **ADD** — net10.0 library; **no** MonoGame ref; inherits Directory.Build.props |
| 2 | `src/MonoRpgMaker.Abstractions/FixedPoint.cs` | **ADD** — the Q16.16 `readonly struct` (above) |
| 3 | `src/MonoRpgMaker.Abstractions/packages.lock.json` | **ADD** — generated by `dotnet restore` (BannedApiAnalyzers ref); committed for `--locked-mode` |
| 4 | `MonoRpgMaker.slnx` | **MODIFY** — add the Abstractions project under `/src/` |
| 5 | `tests/MonoRpgMaker.Engine.Tests/MonoRpgMaker.Engine.Tests.csproj` | **MODIFY** — `ProjectReference` to Abstractions (so tests + the arch test + Stryker can reach it) |
| 6 | `tests/MonoRpgMaker.Engine.Tests/ArchitectureTests.cs` | **MODIFY** — add `Abstractions_must_not_depend_on_MonoGame_Engine_or_hosts` (the Project→Abstractions ring) |
| 7 | `tests/MonoRpgMaker.Engine.Tests/FixedPointTests.cs` | **ADD** — the FixedPoint unit tests |
| 8 | `bin/gate.sh` | **MODIFY** — `mutation_g` mutates Engine **and** Abstractions, each floored (shellcheck-clean) |

### Regression Test Plan
| # | Test | Proves Requirement |
|---|---|---|
| FP1 | `FromInt_ToInt_RoundTrips` — theory over {0, 1, -1, 7, -7, 32767, -32768} ⇒ `ToInt()==n`; `FromInt(n).Raw == n<<16` | REQ-001 |
| FP2 | `Add_Sub_Negate_ExactRaw` — 1.5+2.25=3.75, 3.75−1.5=2.25, 1−3=−2, −(2.5)=−2.5, −(−2.5)=2.5 (assert `.Raw`) | REQ-002 |
| FP3 | `Multiply_ExactValues` — 0.5×0.5=0.25, 2.5×4=10, (−2)×3=−6, 1.5×1.5=2.25 (kills `*`→`/` and the `>>16` mutant) | REQ-002 |
| FP4 | `Divide_TruncatesTowardZero` — 10/4=2.5, 1/2=0.5, (−6)/3=−2, 10/3 ⇒ exact truncated raw (kills `/`→`*`, `<<16` mutant) | REQ-002 |
| FP5 | `Equality_And_Ordering` — `==`/`!=`/`<`/`>`/`<=`/`>=` over equal + ordered pairs; `Equals(object)` true/false/boxed; equal values ⇒ equal `GetHashCode` | REQ-003 |
| FP6 | `CompareTo_ReturnsSign` — `<0`, `0`, `>0` for less/equal/greater | REQ-003 |
| FP7 | `Abs_Min_Max` — Abs(±2.5)=2.5, Abs(0)=0; Min/Max for a<b, a>b, a==b | REQ-004 |
| FP8 | `Floor_Ceiling_Round_PosNeg` — Floor(2.5)=2, Floor(−2.5)=−3; Ceiling(2.5)=3, Ceiling(2.0)=2, Ceiling(−2.5)=−2; Round(2.5)=3, Round(2.4)=2, Round(−2.5)=−3 (kills the offset/shift mutants) | REQ-004 |
| FP9 | `ToString_And_ToDouble` — invariant ToString for a couple values; ToDouble(2.5)==2.5, ToDouble(−0.25)==−0.25 | REQ-004 |
| FP10 | `Overflow_Wraps_Deterministically` — a boundary op pinned to its wrapped raw (documents the unchecked contract) | REQ-002 (determinism) |
| AT1 | `Abstractions_must_not_depend_on_MonoGame_Engine_or_hosts` (NetArchTest over `typeof(FixedPoint).Assembly`) | REQ-005, REQ-006 |
| — | FULL `bin/gate.sh` (with the extended mutation_g) green: format/build/test/cov ≥80/MSI ≥80 on **both** Engine and Abstractions | REQ-007 |

**Uncoverable / exclusions:** none — `FixedPoint` is 100% unit-coverable and fully
mutatable; nothing here is a host/composition root. (`RpgGame` stays the only
`[ExcludeFromCodeCoverage]` type, untouched by this slice.)

### Risks / decisions
- **R1 (load-bearing) — the gate edit.** Extending `mutation_g` to a second Stryker
  run on Abstractions is the riskiest part: `dotnet stryker --project
  MonoRpgMaker.Abstractions.csproj` (run from the Engine.Tests dir) must find the
  FixedPoint tests and complete. Validate must confirm the two-project run works; if
  Stryker can't target Abstractions cleanly, fallback = keep Engine-only mutation and
  rely on FixedPoint's exhaustive unit tests + 100% line coverage (noted, not
  preferred). shellcheck (gate:7) must stay green on the edit.
- **R2 — new project lock file.** Abstractions needs a committed `packages.lock.json`
  or the `--locked-mode` restore fails; implement runs `dotnet restore` and commits it.
- **R3 — analyzer surprises on a struct with operators** (CA1815, CS0660/0661, CA2231)
  → satisfied by shipping `Equals(object)`/`GetHashCode`/`IEquatable` + the op-`==`
  pair together.
- **R4 — `Math.Abs(int.MinValue)` throws** → `Abs` uses a branch (`raw<0 ? unchecked(-raw) : raw`), not `Math.Abs`.

## Phase 3 — Implement
- Built (restore OK; build **0 warn / 0 err** `-warnaserror`; `dotnet format` clean;
  `shellcheck bin/gate.sh` OK; Abstractions `packages.lock.json` generated):
  - `src/MonoRpgMaker.Abstractions/MonoRpgMaker.Abstractions.csproj` (**new**) —
    net10.0 library, no MonoGame; inherits `Directory.Build.props`.
  - `src/MonoRpgMaker.Abstractions/FixedPoint.cs` (**new**) — the Q16.16 `readonly
    struct` exactly per design: `Raw`/`FromInt`/`FromRaw`/`Zero`/`One`; `+ - unary- * /`
    (all explicit `unchecked`; `*`/`/` widen through `long`); `== != < > <= >=`,
    `IEquatable`+`Equals(object)`+`GetHashCode(=Raw)`+`IComparable`; `Abs` (branch,
    not `Math.Abs`)/`Min`/`Max`/`Floor`/`Ceiling`/`Round` (half-away)/`ToInt`
    (trunc-toward-zero)/`ToString` (invariant)/`ToDouble` (debug). Analyzer-clean.
  - `src/MonoRpgMaker.Abstractions/packages.lock.json` (**new**) — generated by
    restore; committed for `--locked-mode`.
  - `MonoRpgMaker.slnx` — Abstractions added under `/src/`.
  - `tests/…/MonoRpgMaker.Engine.Tests.csproj` — `ProjectReference` to Abstractions.
  - `tests/…/ArchitectureTests.cs` — added
    `Abstractions_must_not_depend_on_MonoGame_Engine_or_hosts` (the Project→Abstractions ring).
  - `bin/gate.sh` — `mutation_g` now loops Stryker over **Engine AND Abstractions**,
    flooring each (shellcheck-clean).
- Deviations from design (+ reason): **none** — built exactly to the manifest. NOTE:
  the mutation-gate change (Stryker on Abstractions — risk **R1**) is written but
  **not yet exercised** — it runs for the first time at Phase 4's FULL gate. If
  `dotnet stryker --project MonoRpgMaker.Abstractions.csproj` doesn't complete cleanly,
  the recorded fallback is Engine-only mutation + exhaustive FixedPoint unit tests.

## Inspect (Phase 3.5)
- Lenses run: 3 parallel general-purpose critics, each verifying concretely:
  1. FixedPoint correctness + determinism — hand-verified every Q16.16 op on the
     .NET 10 runtime (scratch tests under /tmp, deleted; worktree confirmed clean).
  2. §14 conventions + Abstractions purity + analyzer-correctness — **empirically
     falsified the arch test** by injecting a MonoGame leak (it failed, naming the
     offender); probed each analyzer (CA1036 drives the operator set; CA2225 does
     not fire at latest-recommended).
  3. The `bin/gate.sh` mutation extension — logic + shellcheck + deliverability.
- Findings:
  | # | Severity | Finding (file:line) | Verdict | Fix |
  |---|---|---|---|---|
  | 1 | **HIGH** | `bin/gate.sh` (all of root `bin/`) is gitignored by the bare `.gitignore` `bin/` rule → the gate, and the approved mutation extension, were untracked/uncommittable; a clone has no gate (`git ls-files bin/gate.sh` empty) | **REAL** | **Fixed (user-approved):** scoped `.gitignore` to `src/**/bin/` + `tests/**/bin/`; root `bin/gate.sh` now tracked (`git add --dry-run` → add), build output still ignored. Captured `BF-gate-untracked-001` + `PR-claude-gitignore-hides-tooling-001`. |
  | 2 | low | `operator /` throws `DivideByZeroException` when divisor raw == 0 (FixedPoint.cs) | **ACCEPTED** | Deterministic (same on every platform) + matches int-division semantics; no wrap promised for `/`. Validate adds a `Divide_ByZero_Throws` test to pin it. |
  | 3 | info | Stryker on Abstractions reports 0% MSI until FixedPoint tests exist → gate:12 RED before Phase 4 | **EXPECTED** | Correct phase ordering; tests land in validate, then the gate greens. Do not `/commit` before Phase 4. |
  | 4 | low | Could `FixedPoint` be a `record struct`? | **REJECTED** | A record auto-gens Equals/== but NOT the comparison operators / `IComparable` / the custom invariant `ToString` (which a record's synthesized `ToString` would fight). `readonly struct` is correct. |
- **Verdict:** the FixedPoint math, Abstractions purity, and the gate logic are
  **verified-correct** (each confirmed concretely — incl. a live arch test and a
  0/0 `-warnaserror` rebuild). The one real defect was infra (the untracked gate),
  now fixed + captured.
- Re-verify: only `.gitignore` changed (no code/script edit), so the Phase 3 build
  (0/0 `-warnaserror`, format + shellcheck clean) still stands; `git check-ignore`
  confirms the new scoping.

## Phase 4 — Validate
- Tests added (1 new file `tests/MonoRpgMaker.Engine.Tests/FixedPointTests.cs`,
  5 classes, ~22 cases):
  - `FixedPointConstructionTests` — FP1 round-trip theory {0,1,-1,7,-7,32767,-32768}
    + the `× 65536` scale, `FromRaw`, `Zero`/`One`/`FractionalBits` constants.
  - `FixedPointArithmeticTests` — FP2 add/sub/negate exact `.Raw`, FP3 multiply
    exact, FP4 divide truncate-toward-zero, `Divide_ByZero_Throws`.
  - `FixedPointComparisonTests` — FP5 equality/operators/`Equals(object)`/null/non-FP
    /hash, ordering operators, FP6 `CompareTo` sign.
  - `FixedPointHelperTests` — FP7 `Abs`/`Min`/`Max` (+ties), `ToInt` truncate-toward-zero,
    FP8 `Floor`/`Ceiling`/`Round` (pos+neg+exact+half).
  - `FixedPointFormattingTests` — FP9 `ToDouble` + invariant `ToString`, FP10 overflow wrap.
  - The arch ring AT1 was added in Phase 3.
- Inspect-surfaced fix: CS1718 self-comparison (`lo < lo`) → a distinct equal-valued
  `eq` var (keeps the reflexive `<`/`<=` mutation-killer, avoids the `-warnaserror` error).
- `dotnet test MonoRpgMaker.slnx`: **Passed — 62/62, 0 failed** (22 new), 56 ms.
- `bin/gate.sh` (FULL): **GATE GREEN [full] — 12/12.** Coverage **99.1%** (floor 80).
  Mutation: **Engine 91.30%**, **Abstractions 82.46%** (floor 80) — the gate
  extension works: `dotnet stryker --project MonoRpgMaker.Abstractions.csproj`
  mutated the new assembly cleanly (**resolves risk R1**). Receipt written;
  `/commit` satisfiable. FixedPoint's 82.46% clears the floor; the survivors are
  low-value (debug `ToString` format-string mutants + equivalent boundary mutants on
  `Abs`/`Min`/`Max` ties) — not worth chasing below-floor.
- Pre-existing exclusions: none. `RpgGame` stays the only `[ExcludeFromCodeCoverage]`
  type (untouched by this slice).

## Phase 5 — Complete
- Docs updated:
  - `docs/roadmap.md` P0: added a "slice 1 of N landed" status note (gate-green
    99.1% cov / 91.30% Engine + 82.46% Abstractions MSI; the mutation gate now covers
    Abstractions; the remaining P0 slices listed).
  - No `CLAUDE.md` / `decisions.md` change — D-0016 already governs; the concrete
    FixedPoint contract is captured as a forge AD.
- Forge capture (aar/failures/rules/decisions):
  - `aar-submit` closed AAR `928e71d0` (outcome=completed, effectiveness 4/5; 4 novel
    findings).
  - **AD** `AD-claude-fixedpoint-contract-001` — the Q16.16 deterministic contract
    (integer-backed, unchecked wrap, truncate-toward-zero `/`+`ToInt`, half-away `Round`).
  - **AD** `AD-claude-gate-mutates-all-projects-001` — the mutation gate now floors MSI
    on every production project (Engine + Abstractions).
  - From inspect: **BF** `BF-gate-untracked-001` (the canonical gate was gitignored) +
    **PR** `PR-claude-gitignore-hides-tooling-001`.
- Ticket closed: #3 (`a0d9d476-1127-4f2a-9add-eb7660d56384`) → done.
- Archived: `active/` → `completed/` (spec + notes).
- **Commit note:** this slice also includes the `.gitignore` scoping fix, so
  `bin/gate.sh` (and the approved mutation extension) is version-controlled for the
  first time and is committable.
