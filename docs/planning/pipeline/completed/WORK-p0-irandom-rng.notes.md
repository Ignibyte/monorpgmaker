# WORK-p0-irandom-rng — Notes

Per-phase working notes for the paired `.spec.md`. The spec is the contract;
this is the record of what happened.

## Phase 1 — Plan
- Request: P0 slice 2 — the `IRandom` deterministic randomness seam + a seeded,
  reproducible integer-only PRNG in `MonoRpgMaker.Abstractions` (pairs with
  slice 1's `FixedPoint`; D-0016). Capturable/restorable state for future
  save/replay. FULL gate green (mutation now covers Abstractions).
- Intake source: none.
- Classification / tier: work pipeline, **type = feature** (one shippable slice;
  a FixedPoint-sized primitive).
- Forge recall (lessons/failures surfaced):
  - **roadmap P1** lists `IRandom`/`IDeterministicRng` + **`RngState`** + the
    replay-to-same-hash gate → the seam must expose **capturable/restorable state**.
  - **agentic-features**: `RngState` is a future `ISaveStateComponent`; "prefer
    integer/fixed-point math" → the RNG is integer-only, `NextFixedPoint` derives.
  - **Greenfield:** no `IRandom`/`System.Random`/RNG exists in `src`.
  - **The gate mutates Abstractions** (AD-claude-gate-mutates-all-projects-001) — the
    RNG must be exhaustively mutation-tested (golden sequence + bounds + round-trip).
  - **Forge hygiene:** oathstar save/load docs (Decision 046, WORK-save-load-v1 —
    Rust) bled into recall; discarded per CLAUDE.md (they confirm the "serializable
    RNG state → deterministic round-trips" intent, nothing more).
  - **Process lesson applied** ([[PR-claude-forge-doc-staleness-001]]): slice chosen
    by reading the LOCAL roadmap.
- Ticket: **43a2f8ee-519c-432f-aec7-e4fda1bba366 (#4)** — created with explicit
  `project_id a9ee8162`; returned row confirms **monorpgmaker** (scoping fix holding;
  monorpgmaker now #1/#2/#3 done, #4 open).
- AAR id (from `aar-open`): dc1d06c6-fdfd-4fe1-81a1-eca4ca188d46
- EARS requirements reviewed: REQ-001..008 (reproducibility, golden sequence, bounded
  range, NextBool/NextFixedPoint, state round-trip, integer-only, purity, FULL gate).

## Phase 2 — Design

### Approach / architecture
Two new pure types in `Abstractions`:
- **`IRandom`** (interface) — the injected randomness seam: `ulong NextBits()` (raw
  64-bit generator output), `int NextInt()` (full range), `int NextInt(int
  minInclusive, int maxExclusive)` (bounded), `bool NextBool()`, `FixedPoint
  NextFixedPoint()` (`[0,1)`), `ulong State { get; }` (capturable).
- **`SplitMix64Random`** (`sealed class : IRandom`) — the default deterministic PRNG.
  Mutable `ulong _state`; ctor `(ulong seed)`. All generation is `ulong` integer
  arithmetic (wraps mod 2^64 — the C# default for `ulong`), **no float/double**:
  - `NextBits()`: `_state += 0x9E3779B97F4A7C15UL; ulong z = _state; z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL; z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL; return z ^ (z >> 31);`
  - `NextInt()`: `unchecked((int)(NextBits() >> 32))` — high 32 bits as a signed int.
  - `NextInt(min,max)`: guard `max > min` (else `ArgumentOutOfRangeException`);
    `ulong range = (ulong)((long)max - min); return (int)((long)min + (long)(NextBits() % range));`
    — long math handles the full int range; modulo (bias negligible for game ranges).
  - `NextBool()`: `(NextBits() >> 63) != 0UL` — the top bit.
  - `NextFixedPoint()`: `FixedPoint.FromRaw((int)(NextBits() & 0xFFFFUL))` — low 16
    bits → Raw in `[0,65536)` = `[0,1)`.
  - `State => _state`. **Restore** = `new SplitMix64Random(captured.State)` — for
    SplitMix64 the seed *is* the state, so one ctor serves both seeding and restore.

Each public `Next*` calls `NextBits()` exactly once (one deterministic state advance
per call — matters for replay-step accounting). `IRandom`/`SplitMix64Random` use only
`FixedPoint` (Abstractions) — no MonoGame — so the slice-1 NetArchTest ring already
guards purity.

### The 5 decisions — settled
1. **Algorithm = SplitMix64.** A complete, well-known PRNG (Java SplittableRandom; the
   xoshiro seeder); integer-only, trivially correct, ulong state, fewest mutants. Quality
   is "good game RNG," not cryptographic — swappable behind `IRandom` later.
2. **Bounded range = modulo**, bias documented as negligible for game-sized ranges
   (≤ ~2^-58). Rejection sampling rejected: its loop-back is near-uncoverable for small
   ranges (mutation/coverage headache) for a bias no game cares about. Rejection is the
   future option behind the seam if provable uniformity is ever needed.
3. **Return types:** `NextInt()` = high-32-bits-as-signed-`int` (full range, may be
   negative); `NextFixedPoint()` = low-16-bits via `FixedPoint.FromRaw`.
4. **State = `ulong`**, captured via the `State` getter, restored via the seed ctor
   (seed ≡ state for SplitMix64).
5. **Mutable `sealed class`** (a PRNG advances per call) — NOT a `readonly struct`.

### File manifest
| # | File | Change |
|---|---|---|
| 1 | `src/MonoRpgMaker.Abstractions/IRandom.cs` | **ADD** — the `IRandom` seam interface |
| 2 | `src/MonoRpgMaker.Abstractions/SplitMix64Random.cs` | **ADD** — the SplitMix64 `sealed class : IRandom` |
| 3 | `tests/MonoRpgMaker.Engine.Tests/SplitMix64RandomTests.cs` | **ADD** — the test suite |

No `.csproj` / `.slnx` / `bin/gate.sh` change — the Abstractions project, the Tests
`ProjectReference`, and the mutation-gate coverage of Abstractions were all wired in
slice 1.

### Regression Test Plan
| # | Test | Proves Requirement |
|---|---|---|
| RT1 | `SameSeed_ProducesIdenticalSequence` — two `SplitMix64Random(S)`, `NextBits()` ×8 equal | REQ-001 |
| RT2 | `KnownSeed_GoldenSequence` — `NextBits()[0..N]` == pinned values **cross-checked against an independent SplitMix64 computation** — the generation-path mutation-killer (catches any constant/shift/operator mutant) | REQ-002 |
| RT3 | `NextInt_Bounded_StaysInRange` — `NextInt(0,6)` ×1000 ∈ [0,6); `NextInt(-3,3)` ∈ [-3,3); `NextInt(int.MinValue,int.MaxValue)` valid | REQ-003 |
| RT4 | `NextInt_RangeOfOne_AlwaysReturnsMin` — `NextInt(5,6)` ×many == 5 | REQ-003 |
| RT5 | `NextInt_InvalidRange_Throws` — `max <= min` → `ArgumentOutOfRangeException` | REQ-003 |
| RT6 | `NextBool_GoldenSequence_AndBothValuesOccur` — pinned bool sequence; true and false both appear | REQ-004 |
| RT7 | `NextFixedPoint_InUnitInterval_AndGolden` — Raw ∈ [0,65536) over many draws; pinned golden raws | REQ-004 |
| RT8 | `State_Capture_Restore_ReproducesContinuation` — advance k, capture `State`, record next m, `new(...State)`, next m identical | REQ-005 |
| — | REQ-006 no `System.Random`/`DateTime`/float/double | gate:9 source-bans + review |
| — | REQ-007 Abstractions purity | the EXISTING slice-1 NetArchTest ring (inspects the whole assembly) |
| — | REQ-008 FULL gate green (incl. mutation on Abstractions) | gate run |

**Uncoverable / exclusions:** none — `IRandom`/`SplitMix64Random` are 100%
unit-coverable and fully mutatable.

### Risks / decisions
- **R1 (load-bearing) — golden values must be cross-checked against an INDEPENDENT
  SplitMix64 computation** (e.g. a quick Python reference), not just the impl's own
  output — else a buggy generator would pin its own wrong sequence. Validate does the
  cross-check before pinning.
- **R2 — modulo bias** in `NextInt(min,max)`: negligible for game ranges, documented;
  rejection sampling deferred behind the seam.
- **R3 — `NextBits()` on the seam** exposes the raw generator (a legitimate primitive)
  and enables full-64-bit golden coverage.
- **R4 — mutable class** (advances per call): callers inject/share one instance; not a
  value type. The `throw` on an invalid range is a programmer-precondition guard (§14).

## Phase 3 — Implement
- Built (build **0 warn / 0 err** `-warnaserror`; `dotnet format` clean):
  - `src/MonoRpgMaker.Abstractions/IRandom.cs` (**new**) — the seam interface:
    `NextBits`/`NextInt`/`NextInt(min,max)`/`NextBool`/`NextFixedPoint`/`State`,
    fully XML-doc'd (no `using`s — `FixedPoint` is same-namespace, crefs are FQN).
  - `src/MonoRpgMaker.Abstractions/SplitMix64Random.cs` (**new**) — `sealed class :
    IRandom`; `NextBits()` = SplitMix64 in an `unchecked` block (`_state += Gamma`;
    two mix rounds; final xor-shift); `NextInt()` = high-32-as-int; `NextInt(min,max)`
    = `ArgumentOutOfRangeException` guard + long-range modulo; `NextBool()` = top bit;
    `NextFixedPoint()` = low-16 → `FixedPoint.FromRaw`; `State => _state`. Constants
    `Gamma`/`MixA`/`MixB`. Documented not-thread-safe (single sim thread owns it).
  - No `.csproj` / `.slnx` / `bin/gate.sh` change — Abstractions, the Tests
    `ProjectReference`, and mutation-gate coverage were wired in slice 1.
- Deviations from design: **none**. (The explicit `unchecked` block around `NextBits`
  is the design's "ulong arithmetic wraps mod 2^64" made robust to any overflow-check
  setting — FixedPoint's discipline.) `CA5394` does not fire (it targets `System.Random`,
  not a custom generator).

## Inspect (Phase 3.5)
- Lenses run: 2 parallel general-purpose critics, each verifying CONCRETELY:
  1. SplitMix64 canonical correctness + determinism + `NextInt` bounds — cross-checked
     the generator against an **independent Python reference AND a compiled C# mirror**.
  2. §14 conventions + Abstractions purity + analyzer-correctness + seam design + simplification.
- Findings: **none.**
  - **Canonical SplitMix64:** `new SplitMix64Random(0).NextBits()` =
    `0xE220A8397B1DCDAF`, `0x6E789E6AA1B965F4`, `0x06C45D188009454F` — matches the
    reference SplitMix64(0) vector. Integer-only; one state-advance per `Next*`;
    capture/restore reproduces the continuation (verified `0x1E9A57BC80E6721D` both ways).
  - `NextInt(min,max)` is overflow-safe for the **full int range** (the long-widened
    range computation), `range==1` → always `min`, the guard throws on `max<=min`,
    modulo bias ~3e-19 (negligible). `NextFixedPoint` Raw ∈ [0,65535] = [0,1), never negative.
  - Purity holds (only `System` + `FixedPoint`; the slice-1 ring already covers the whole
    assembly — no new arch test). Build **0/0** `-warnaserror` confirmed (CA5394 correctly
    silent on a custom RNG; IDE0290 is suggestion-not-error). Seam design sound (`NextBits`
    a legit primitive; mutable class correct for a stateful PRNG).
- **Verdict:** no defects across both lenses — each confirmed by independent computation
  + a 0/0 build. No source fix; **no `failure-record`**; **no new prevention-rule**.
- **Golden anchors for Phase 4** (verified canonical, cross-checked vs an independent
  SplitMix64): `NextBits(seed 0)` = `{0xE220A8397B1DCDAF, 0x6E789E6AA1B965F4,
  0x06C45D188009454F}`; restore-continuation = `0x1E9A57BC80E6721D`. Validate pins these.
- Re-verify: no code changed in this phase; the Phase 3 build (0/0, format clean) stands.

## Phase 4 — Validate
- Tests added (1 new file `tests/MonoRpgMaker.Engine.Tests/SplitMix64RandomTests.cs`,
  10 cases): RT1 same-seed identical; RT2 known-seed golden `NextBits` (canonical
  SplitMix64(0) vector); RT3 bounded in-range (0,6)/(-3,3)/full-int-span; RT4
  range-of-one==min; RT5 invalid-range throws; RT6 `NextBool` golden prefix [T,F,F] +
  both-occur; RT7 `NextFixedPoint` golden Raw 52655 + unit-interval; RT8 state
  capture/restore round-trip; RT9 `NextInt()` golden -501176263 + determinism; RT10
  bounded golden sequence. **All golden values independently Python-cross-checked
  before pinning** (a bug can't pin itself).
- **Mutation iteration (§0 fix-at-source, no floor-lowering):**
  - First FULL gate: Abstractions MSI **79.75% < 80 (RED)** (Engine 91.30%; coverage fine).
  - Root cause (Stryker JSON report): `SplitMix64Random` survivors = 5× `>>`→`>>>` on
    `ulong` (**EQUIVALENT** — `>>` and `>>>` are identical on unsigned types, unkillable)
    + **1 killable**: the `NextInt(min,max)` exception's mutable message string.
  - Fix: replaced the custom `throw new ArgumentOutOfRangeException(…, "msg")` with the
    idiomatic guard helper `ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(maxExclusive,
    minInclusive)` — same semantics (RT5 still green), no magic string ⇒ the string mutant
    is eliminated. Behaviour-preserving; the inspect-verified guard contract is unchanged.
  - Result: Abstractions MSI **80.00%** — every **killable** mutant is dead. The remaining
    survivors are all provably **EQUIVALENT**: `SplitMix64Random`'s `>>>`-on-ulong;
    `FixedPoint`'s `>>>`-after-`(int)`-cast (differing high bits discarded),
    `ToString("")`≡`"G"` over this range, and `<`/`<=` boundary ties at 0 (slice-1 knowns).
- `dotnet test MonoRpgMaker.slnx`: **Passed — 72/72, 0 failed** (10 new).
- `bin/gate.sh` (FULL): **GATE GREEN [full] — 12/12.** Coverage **99.2%**, mutation
  Engine **91.30%** / Abstractions **80.00%** (both ≥ floor). Receipt written.
- Pre-existing exclusions: none. The tight Abstractions margin is equivalent-mutant noise
  (documented above), not a test gap.

## Phase 5 — Complete
- Docs updated:
  - `docs/roadmap.md` P0: status note now "slices 1–2 of N landed" (IRandom dropped from
    the remaining list; the equivalent-mutant margin noted with the PR reference).
  - No `CLAUDE.md` / `decisions.md` change — D-0016 governs; the IRandom contract is the forge AD.
- Forge capture (aar/failures/rules/decisions):
  - `aar-submit` closed AAR `dc1d06c6` (outcome=completed, effectiveness 4/5; 2 novel findings).
  - **AD** `AD-claude-irandom-rng-001` — the IRandom seam + SplitMix64Random contract.
  - **PR** `PR-claude-stryker-unsigned-shift-equivalent-001` — `>>`/`>>>`-on-unsigned
    equivalent mutants dilute MSI; prefer `ArgumentOutOfRangeException.ThrowIf*` guard helpers.
  - No `failure-record`: the first FULL gate's RED (Abstractions 79.75%) was a mutation-margin
    issue fixed in-phase (killing the one real survivor — the exception message string), not a
    shipped bug; the design/logic was correct throughout.
- Ticket closed: #4 (`43a2f8ee-519c-432f-aec7-e4fda1bba366`) → done.
- Archived: `active/` → `completed/` (spec + notes).
- **Commit note:** code + tests only (`IRandom.cs`, `SplitMix64Random.cs`,
  `SplitMix64RandomTests.cs`) + the roadmap note + pipeline docs — no infra/gate/gitignore change.
  Push to `origin` (Ignibyte/monorpgmaker) per the session's standing push approval.
