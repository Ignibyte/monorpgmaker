# WORK-p0-gridpoint — Notes

Per-phase working notes for the paired `.spec.md`. The spec is the contract;
this is the record of what happened.

## Phase 1 — Plan
- Request: P0 slice 3 — the pure `GridPoint(int X, int Y)` coordinate primitive in
  `MonoRpgMaker.Abstractions` (the MonoGame-free coordinate the seam adopts instead of
  XNA `Point`). The clean foundation for the deferred event-seam extraction.
- Intake source: none.
- Classification / tier: work pipeline, **type = feature** (one shippable primitive;
  a FixedPoint/IRandom-sized slice).
- Forge recall (lessons surfaced):
  - **D-0014 "don't hack core"** + agentic-overview §3: `Abstractions` is the published
    seam surface (MonoGame → Engine → Abstractions → Project); it must stay pure so the
    "Project → Abstractions only" ring holds. ⇒ the seam can't use XNA `Point` → needs `GridPoint`.
  - **agentic-substrate §4** names `EventContext` a "semantic-verb surface" seam — supports
    abstracting it (slice 3b's `IEventContext`).
  - **Sizing:** XNA `Point` is in ~13 files (World/Entities/Sim/host/Editor) ⇒ the full
    extraction is too big for one clean land; scoped down to `GridPoint`-only (3b does the move).
  - **Mutation note** ([[PR-claude-stryker-unsigned-shift-equivalent-001]]): keep `GridPoint`
    int-only with no unsigned `>>` so the `>>>`-equivalent-mutant trap (that pinned the RNG at
    80.00%) doesn't recur; a ternary abs gives killable mutants.
  - **Process lesson** ([[PR-claude-forge-doc-staleness-001]]): slice chosen from the LOCAL roadmap.
- Ticket: **68ec5a99-73a9-4b8c-a609-63478cb702fc (#5)** — created with explicit
  `project_id a9ee8162`; returned row confirms **monorpgmaker** (scoping fix holding; #1–#4 done, #5 open).
- AAR id (from `aar-open`): 9f2f6421-9fec-45ba-8b67-d75fbf3a9322
- EARS requirements reviewed: REQ-001..006 (equality, +/- operators, Zero, distance helpers,
  purity, FULL gate).

## Phase 2 — Design

### Approach / architecture
One new pure type in `Abstractions`:
```csharp
public readonly record struct GridPoint(int X, int Y)
{
    public static readonly GridPoint Zero = new(0, 0);
    public static GridPoint operator +(GridPoint a, GridPoint b) => new(a.X + b.X, a.Y + b.Y);
    public static GridPoint operator -(GridPoint a, GridPoint b) => new(a.X - b.X, a.Y - b.Y);
    public int ManhattanDistanceTo(GridPoint other) => Abs(X - other.X) + Abs(Y - other.Y);
    public int ChebyshevDistanceTo(GridPoint other) => Math.Max(Abs(X - other.X), Abs(Y - other.Y));
    private static int Abs(int n) => n < 0 ? -n : n;
}
```
The record primary ctor gives `X`/`Y` + value equality + `GetHashCode` + `ToString` +
`Deconstruct` + `==`/`!=` for free. `using System;` for `Math.Max`. Integer-only, no
`float`/`double`, no MonoGame → the slice-1 NetArchTest purity ring already covers it.

### The 3 decisions — settled
1. **Minimal surface:** `X`/`Y`, `Zero`, `+`/`-`, `ManhattanDistanceTo`,
   `ChebyshevDistanceTo`. Nothing else (no `WithX`/neighbours) — the seam adopts more in 3b if needed.
2. **`Abs` = ternary** `n < 0 ? -n : n` (matches `FixedPoint.Abs`; killable `<` mutant; no
   `>>>`-on-unsigned trap). `Abs(int.MinValue)` wraps deterministically (documented).
3. **`int` distance arithmetic** — grid coords are map-sized; the representable range is
   documented; NOT widened to `long`.
4. **`Math.Max` for Chebyshev** (not a hand-rolled `dx >= dy ? dx : dy`) — Stryker doesn't
   mutate `Math.Max`'s internals, so this avoids a max-selection *equivalent* survivor, protecting
   the Abstractions MSI margin (the RNG slice closed at exactly 80.00%).

### File manifest
| # | File | Change |
|---|---|---|
| 1 | `src/MonoRpgMaker.Abstractions/GridPoint.cs` | **ADD** — the `readonly record struct` (above) |
| 2 | `tests/MonoRpgMaker.Engine.Tests/GridPointTests.cs` | **ADD** — the test suite |

No `.csproj` / `.slnx` / `bin/gate.sh` change — Abstractions, the Tests `ProjectReference`,
the mutation-gate coverage, and the NetArchTest purity ring were all wired in slice 1.

### Regression Test Plan
| # | Test | Proves Requirement |
|---|---|---|
| GP1 | `Components_Equality_Hash` — `(3,5).X==3 && .Y==5`; `(3,5)==(3,5)` with equal `GetHashCode`; `(3,5)!=(3,6)` | REQ-001 |
| GP2 | `Add_Subtract_AreComponentwise` — `(1,2)+(3,-4)==(4,-2)`; `(4,-2)-(1,2)==(3,-4)`; `p + Zero == p` | REQ-002 |
| GP3 | `Zero_IsOrigin` — `Zero.X==0 && Zero.Y==0`; `Zero==new(0,0)` | REQ-003 |
| GP4 | `Manhattan_And_Chebyshev` — Manhattan((0,0)→(3,-4))==7, Chebyshev==4; Chebyshev with **dx>dy** ((0,0)→(5,2))==5, **dy>dx** ((0,0)→(2,5))==5, **dx==dy** ((0,0)→(3,3))==3; distance-to-self==0; symmetric (a→b == b→a); negative coords. **PLUS a NON-ORIGIN pair (both axes non-zero on both endpoints): Manhattan((4,2),(1,5))==6, Chebyshev((4,2),(1,5))==3** — REQUIRED to kill the `_ - other._`→`+` subtraction mutants that origin-anchored cases miss (inspect finding #1). | REQ-004 |
| — | REQ-005 purity (no MonoGame/float) | the EXISTING slice-1 NetArchTest ring + gate:9 |
| — | REQ-006 FULL gate green (mutation on Abstractions ≥ floor) | gate run |

**Uncoverable / exclusions:** none — `GridPoint` is 100% unit-coverable and mutatable.

### Risks / decisions
- **R1 — the Abstractions MSI margin.** The assembly sits at exactly 80.00% after the RNG.
  `GridPoint` adds mostly-killable mutants (`+`/`-` components, `Abs`'s `-n`, the distance
  subtractions) with only the one inherent `Abs`-at-0 equivalent (`<`→`<=`, like
  `FixedPoint.Abs`) — `Math.Max` avoids the Chebyshev equivalent. Its high kill rate should
  hold/raise the combined ≥80%. Validate confirms; if it dips, the cause is documented equivalents
  ([[PR-claude-stryker-unsigned-shift-equivalent-001]] family), fixed by adding exact-value tests.
- **R2 — `int` distance overflow** at extreme coords (`X - other.X` overflow): accepted (grid
  coords are map-sized), documented; not widened to `long`.
- **R3 — forward-looking:** no consumer yet; the seam adopts `GridPoint` in slice 3b.

## Phase 3 — Implement
- Built (build **0 warn / 0 err** `-warnaserror`; `dotnet format` clean):
  - `src/MonoRpgMaker.Abstractions/GridPoint.cs` (**new**) — `readonly record struct
    GridPoint(int X, int Y)`; `static readonly Zero`; `operator +`/`-` (componentwise);
    `ManhattanDistanceTo` (Abs sum), `ChebyshevDistanceTo` (`Math.Max` of Abs); private
    ternary `Abs` with `unchecked(-n)` for the documented `int.MinValue` wrap. Fully
    XML-doc'd; `using System` (for `Math.Max`). Pure, integer-only, no MonoGame.
  - No `.csproj` / `.slnx` / `bin/gate.sh` change — Abstractions, the Tests
    `ProjectReference`, the mutation-gate coverage, and the NetArchTest purity ring were
    wired in slice 1.
- Deviations from design: **none**. (The `unchecked(-n)` in `Abs` is the design's
  "`Abs(int.MinValue)` wraps" made robust to any overflow-check setting; the operators +
  distance use plain `int` per the design's small-coord choice. `Math.Max` for Chebyshev
  per the design — avoids a max-selection equivalent mutant.)

## Inspect (Phase 3.5)
- Lenses run: 1 general-purpose critic (proportionate to a ~25-line pure record struct),
  verifying concretely (read + scratch-compute + a 0/0 `-warnaserror` build):
  correctness (distance/operators/Abs incl. negatives + `int.MinValue`) + purity/§14 +
  analyzer + mutation-readiness against the 80% MSI floor.
- Findings:
  | # | Severity | Finding (file:line) | Verdict | Fix |
  |---|---|---|---|---|
  | 1 | **HIGH** | The planned GP4 distance tests anchor every case at the origin `(0,0)`; when one operand is `0`, `Abs(0-k)==Abs(0+k)`, so the four `_ - other._`→`_ + other._` mutants (Manhattan + Chebyshev dx/dy, GridPoint.cs:26,29) survive — **killable, not equivalent**. Projection: GP4-as-planned → Abstractions MSI **77.42% (gate:12 RED)**; + one non-origin pair → **81.72% (green)**. | **REAL (test-gap)** | **Validate must-do — no production change (`GridPoint.cs` is correct):** add a both-endpoints-non-zero-on-both-axes pair to GP4 — `Manhattan((4,2),(1,5))==6`, `Chebyshev((4,2),(1,5))==3` — killing all four subtraction mutants. **Test plan updated above.** PR captured at complete. |
  | 2 | low | `Abs` `< 0` → `<= 0` (GridPoint.cs:32) | **ACCEPTED (equivalent)** | `n=0` gives `0` either way (like `FixedPoint.Abs`) — unkillable equivalent. `Math.Max` correctly avoids the Chebyshev max-selection equivalent (the critic confirmed `FixedPoint`'s hand-rolled `Min`/`Max` `<=`/`>=` DID survive). |
- **Verdict:** `GridPoint.cs` is **correct, pure, and analyzer-clean** (no production fix —
  distance/operators/`Abs(int.MinValue)`-wrap all verified concretely; the NetArchTest ring
  covers purity; CA2225 doesn't fire). The one HIGH was a **test-completeness gap caught BEFORE
  the gate** (it would have RED'd at 77.42%, like the RNG slice) — fixed by the non-origin GP4 pair.
- Re-verify: no production code changed; the Phase 3 build (0/0, format clean) stands. The
  test-plan fix lands in Phase 4.

## Phase 4 — Validate
- Tests added (1 new file `tests/MonoRpgMaker.Engine.Tests/GridPointTests.cs`, 4 cases):
  GP1 `Components_Equality_Hash`; GP2 `Add_Subtract_AreComponentwise` (+ Zero identity);
  GP3 `Zero_IsOrigin`; GP4 `Manhattan_And_Chebyshev` (both orderings + negatives + self +
  symmetric + **the inspect-mandated NON-ORIGIN pair** `Manhattan((4,2),(1,5))==6` /
  `Chebyshev((4,2),(1,5))==3` that kills the `_ - other._`→`+` subtraction mutants).
- `dotnet test MonoRpgMaker.slnx`: **Passed — 76/76, 0 failed** (4 new).
- `bin/gate.sh` (FULL): **GATE GREEN [full] — 12/12.** Coverage **99.2%**. Mutation
  Engine **91.30%** / Abstractions **82.22%** — **up from 80.00%**: the non-origin pair +
  GridPoint's high kill rate restored margin above the floor; the only GridPoint survivor is
  the inherent `Abs`-at-0 equivalent. Receipt written.
- Pre-existing exclusions: none.
- The inspect-caught test gap (origin-anchored distance tests → projected 77.42% RED) was
  fixed pre-gate by the non-origin pair → 82.22% green. PR captured at complete.

## Phase 5 — Complete
- Docs updated:
  - `docs/roadmap.md` P0 status note → "slices 1–3 of N landed" (GridPoint added; Abstractions
    MSI 82.22%); **slice 3b** (the event-seam extraction proper) called out as the immediate next.
  - No `CLAUDE.md` / `decisions.md` change — D-0014/D-0016 govern; the GridPoint contract is the forge AD.
- Forge capture (aar/failures/rules/decisions):
  - `aar-submit` closed AAR `9f2f6421` (outcome=completed, effectiveness 5/5; 2 novel findings).
  - **AD** `AD-claude-gridpoint-coordinate-001` — the pure-coordinate contract (the seam adopts it in 3b).
  - **PR** `PR-claude-distance-test-needs-nonzero-both-axes-001` — origin-anchored distance tests
    leave the `a-b`→`a+b` mutants alive; always test a both-axes-non-zero pair (the inspect critic's catch).
  - No `failure-record`: the inspect finding was a caught **test** gap (origin-anchored cases), fixed
    pre-gate; `GridPoint.cs` was correct throughout — no shipped bug.
- Ticket closed: #5 (`68ec5a99-73a9-4b8c-a609-63478cb702fc`) → done.
- Archived: `active/` → `completed/` (spec + notes).
- **Commit note:** code + tests only (`GridPoint.cs`, `GridPointTests.cs`) + the roadmap note +
  pipeline docs — no infra/gate change. Push to `origin` (Ignibyte/monorpgmaker) per the standing approval.
