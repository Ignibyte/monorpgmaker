# WORK-p0-outcome-vocabulary — Notes

Per-phase working notes for the paired `.spec.md`. The spec is the contract;
this is the record of what happened.

## Phase 1 — Plan
- **Request:** P0 next slice — the **minimal shared outcome vocabulary**: the outcome/effect
  type map events RETURN (in Abstractions) instead of mutating via `IEventContext` write-verbs
  (D-0017). The linchpin for the outcome-return analyzer (next slice) + `monorpg scaffold`.
  Direction chosen by the user at `/work` ("Outcome vocabulary first").
- **Intake source:** none. Slice direction confirmed by the user (not auto-approved — present for review).
- **Classification / tier:** work pipeline, **type = feature**. A **seam-migration** slice
  (comparable to the event-seam slice #6, larger than a bare primitive) — but one coherent,
  value-complete unit ("events return outcomes"). One slice.
- **Forge recall (lessons/design surfaced):**
  - **D-0017** is governing: "signatures **return** the shared outcome vocabulary so raw state
    mutation won't type-check"; the outcome type is **also** the type `.expect` rows + scaffold
    signatures use. **D-0019** (additive seam kinds): the chassis is genre-agnostic; new genres
    **add** kinds, they don't redesign → the event-effect subset can start the one vocabulary now.
  - **agentic-substrate**: "Declarative outcome vocabulary (handlers *return* outcomes) —
    pure-over-context, replayable; **one shared vocab**; enforced by an outcome-return analyzer."
    Example outcome shape: `poison, maxHp=4000, 1 tick ⇒ HpDelta=-200` (numeric payload =
    FixedPoint, not float).
  - **DESIGN TENSION (flagged for review):** agentic-substrate says *"lock the shared outcome
    vocabulary members FIRST in P2"* and agentic-features says *"the shared outcome vocabulary
    must be co-designed with battle — one registry, not two."* This slice pulls the **event subset**
    forward. **Mitigation:** the event effects (switch/counter/message) are orthogonal to battle
    effects (HpDelta/state), and the shape is **additive + forward-compatible**, so P2 *extends*
    the same vocabulary. The Phase-2 shape MUST be checked against the P2 effect-registry direction.
  - **Current seam (read, ground truth):** `IMapEvent.Run(IEventContext) → void`; `IEventContext`
    has READ verbs (`GetSwitch`/`GetCounter`) + WRITE verbs (`SetSwitch`/`AddCounter`/`ShowMessage`);
    `EventContext` (Engine) delegates to `GameState` + a message sink; `LeverEvent` returns after a
    `GetSwitch` guard then `ShowMessage`+`SetSwitch`; `ChestEvent` emits **3** effects (`AddCounter`+
    `SetSwitch`+`ShowMessage`) → a handler returns a **sequence**, and still **reads** state to branch.
  - **Reuse:** the migration sets up the **outcome-return analyzer** (next slice) to reuse everything
    slice #7 built (`MonoRpgMaker.Analyzers`, `SimScope`, MRM band, `[DeterminismExempt]` —
    `AD-claude-sim-determinism-analyzer-001`).
  - **Forge hygiene:** no oathstar (Rust/Datastar) bleed used; design grounded in the local docs.
- **Ticket:** **c744fb96-12f1-4152-9238-272e52c35dee (#8)** — created with explicit `project_id
  a9ee8162`; returned row confirms **monorpgmaker** (#1–#7 done, #8 open). Local doc:
  `docs/planning/tickets/open/TICKET-0008-p0-outcome-vocabulary.md`.
- **AAR id (from `aar-open`):** 7da6a87b-ee57-42a3-bb00-87b629322bee
- **EARS requirements reviewed:** REQ-001..007 (Run returns outcomes; the 3 outcome types;
  behavior parity; read-only context; additive extensibility; Abstractions purity + determinism; FULL gate).
- **Status:** awaiting human review of scope + EARS before Phase 2 (the user asked to review; not auto-approved).

## Phase 2 — Design

### Resolved OPEN-Phase-2 decisions
1. **Shape → sealed-record discriminated union.** `public abstract record Outcome` (closed to the
   Project layer via a **non-public base ctor** — authored events *construct + return* cases but can't
   invent new ones) + `public sealed record SetSwitch(string Key, bool Value) : Outcome`,
   `AddCounter(string Key, int Amount)`, `ShowMessage(string Text)`.
2. **Forward-compat VERIFIED (one registry, not two).** `agentic-features.md` "Database → behavior":
   P2's `IEffect`/`ITrait`/`IStateBehavior`/`IDamageFormula` are "pure functions … **returning
   declarative outcomes**" → they return the **same `Outcome` DU**, extended additively with combat
   cases (`HpDelta`, `ApplyState`, …). This slice establishes `Outcome` + the 3 event cases; P2 grows it.
3. **Naming →** `Outcome` (the docs' umbrella term) + case records named **1:1 with the old verbs**
   (`SetSwitch`/`AddCounter`/`ShowMessage`) so the migration reads `ctx.SetSwitch(k,v)` → `new SetSwitch(k,v)`.
4. **Cardinality →** `IReadOnlyList<Outcome>` (ChestEvent returns 3; a no-op event returns `[]`). Handlers
   return arrays / C# collection-expressions.
5. **`IMapEvent.Run` →** `IReadOnlyList<Outcome> Run(IEventContext context)` — **keeps** the read-context
   param (handlers branch on `GetSwitch`/`GetCounter`).
6. **`IEventContext` →** slims to **reads only**: `bool GetSwitch(string)`, `int GetCounter(string)`.
7. **Applier →** new **`OutcomeApplier`** (`Engine.Sim`): `Apply(IReadOnlyList<Outcome>)` switches each
   outcome to `GameState` (`Set`/`Add`) + the message sink (`ShowMessage`). Holds `GameState` + `Action<string>`.
8. **`EventContext` fate →** read-only adapter (`GetSwitch`→`state.Get`, `GetCounter`→`state.GetCount`);
   **drops** the sink + the 3 write methods. The sink moves to `OutcomeApplier`.

### Approach / architecture
- **Abstractions (pure):** the `Outcome` DU + the slim read-only `IEventContext` + `IMapEvent.Run`
  returning outcomes. Payloads are `string`/`bool`/`int` — no float, no MonoGame → the MRM determinism
  analyzer + the "Project → Abstractions only" ring stay green; the records are immutable + value-equal.
- **Engine (`Sim`):** `OutcomeApplier` interprets the returned outcomes against `GameState` + the sink;
  `EventContext` is the read adapter; `LeverEvent`/`ChestEvent` return outcome sequences; `WorldSim.FireStepOn`
  runs the event then applies the result, **before** `SyncDoors` (so a `SetSwitch(door_open)` opens the door
  the same tick — order preserved).
- **Determinism / parity:** the applier `foreach` is over an ordered `IReadOnlyList` (not a Dictionary →
  MRM-clean); the same state writes + the same message are produced, just routed return-then-apply. The
  seeded tracer replay (`TracerSliceTests`) is the regression guard — observable behavior is unchanged.

### File manifest
| # | File | Change |
|---|---|---|
| 1 | `src/MonoRpgMaker.Abstractions/Outcome.cs` | **NEW.** `abstract record Outcome` (non-public base ctor) + sealed records `SetSwitch`/`AddCounter`/`ShowMessage`; XML-doc'd. |
| 2 | `src/MonoRpgMaker.Abstractions/IEventContext.cs` | **MODIFY.** Remove `SetSwitch`/`AddCounter`/`ShowMessage`; keep `GetSwitch`/`GetCounter`. |
| 3 | `src/MonoRpgMaker.Abstractions/IMapEvent.cs` | **MODIFY.** `Run` returns `IReadOnlyList<Outcome>` (was `void`). |
| 4 | `src/MonoRpgMaker.Engine/Sim/OutcomeApplier.cs` | **NEW.** Apply `IReadOnlyList<Outcome>` → `GameState` (`Set`/`Add`) + sink (`ShowMessage`); switch + defensive default; null guards. |
| 5 | `src/MonoRpgMaker.Engine/Sim/EventContext.cs` | **MODIFY.** Read-only adapter (drop sink + 3 write methods); `GetSwitch`→`Get`, `GetCounter`→`GetCount`. |
| 6 | `src/MonoRpgMaker.Engine/Sim/Tracer/LeverEvent.cs` | **MODIFY.** `Run` → `[]` if door already open; else `[new ShowMessage(…), new SetSwitch(DoorSwitch, true)]`. |
| 7 | `src/MonoRpgMaker.Engine/Sim/Tracer/ChestEvent.cs` | **MODIFY.** `Run` → `[new ShowMessage("The chest is empty.")]` if opened; else `[new AddCounter(PotionCount,1), new SetSwitch(OpenedSwitch,true), new ShowMessage("You open the chest and take a potion.")]`. |
| 8 | `src/MonoRpgMaker.Engine/Sim/WorldSim.cs` | **MODIFY.** `_eventContext = new EventContext(State)` (read-only) + `_applier = new OutcomeApplier(State, message => CurrentMessage = message)`; `FireStepOn` → `_applier.Apply(mapEvent.Run(_eventContext))`. |
| 9 | `tests/MonoRpgMaker.Engine.Tests/OutcomeTests.cs` | **NEW.** Record equality + payloads for the 3 cases. |
| 10 | `tests/MonoRpgMaker.Engine.Tests/OutcomeApplierTests.cs` | **NEW.** Apply each kind → `GameState`/sink write-through (the re-homed write-verb tests); multi-outcome order; null guards. |
| 11 | `tests/MonoRpgMaker.Engine.Tests/EventOutcomeTests.cs` | **NEW.** `LeverEvent`/`ChestEvent` `Run` return the exact outcome sequences (give-once). |
| 12 | `tests/MonoRpgMaker.Engine.Tests/EventContextTests.cs` | **MODIFY (rewrite).** `EventContext` is read-only (`GetSwitch`/`GetCounter` delegate to `GameState`); the write-verb assertions move to `OutcomeApplierTests`. |
| 13 | `tests/MonoRpgMaker.Engine.Tests/TracerSliceTests.cs` | **MODIFY.** Update `EventContext_Ctor_NullArgs_Throw` (ctor now takes only `GameState`) + add `OutcomeApplier` null guards; the lever/chest/door/determinism behavior tests stay **unchanged** (parity). |

### Regression Test Plan
| # | Test | Proves Requirement |
|---|---|---|
| T1 | `Outcome` records are value-equal and carry their payloads (`SetSwitch`/`AddCounter`/`ShowMessage`). | REQ-002 |
| T2 | `LeverEvent.Run` returns `[ShowMessage, SetSwitch]` on first step, `[]` when the door is already open. | REQ-001 |
| T3 | `ChestEvent.Run` give-once: first → `[AddCounter, SetSwitch, ShowMessage(take)]`; opened → `[ShowMessage(empty)]`. | REQ-001 |
| T4 | `OutcomeApplier` applies each kind — `SetSwitch`→`state.Set` (write-through), `AddCounter`→`state.Add` (accumulate), `ShowMessage`→sink — and a multi-outcome list in order. | REQ-003 |
| T5 | Behavior parity via the seeded tracer replay (`TracerSliceTests`, unchanged): lever opens the door once; chest grants one potion then reads empty; door passability; same-commands determinism. | REQ-003 |
| T6 | `IEventContext` exposes only `GetSwitch`/`GetCounter`; `EventContext` delegates them to `GameState`. | REQ-004 |
| T7 | `OutcomeApplier` null-arg guards (null state, null sink, null outcomes). | mutation/robustness |
| T8 | Abstractions stays MonoGame-free + float-free (NetArchTest ring + the MRM analyzer green); a new `Outcome` case is purely additive (review). | REQ-005 + REQ-006 |
| T9 | FULL `bin/gate.sh` green — coverage ≥80, mutation MSI ≥80 incl. the new `OutcomeApplier`. | REQ-007 |

**Uncoverable / equivalent:** the `OutcomeApplier` `default:` throw is unreachable in P0 (the closed `Outcome`
hierarchy has no fourth case to construct) → an equivalent mutant; it becomes live when P2 adds outcome kinds
the event applier doesn't handle. Documented; not `[ExcludeFromCodeCoverage]` (the switch arms ARE covered).

### Risks / decisions
- **R1 — forward-compat lock-in:** *mitigated* — verified P2 modules return the SAME `Outcome` DU
  (agentic-features "return declarative outcomes"; roadmap P2 IEffect + shared outcome vocab). The DU is the
  registry P2 extends; combat cases are additive. Low risk.
- **R2 — applier `default:` equivalent mutant** (see above): watch Engine MSI at validate (Engine 91.30% has
  margin); if it threatens the floor, restructure (the other arms are killable by T4/T7).
- **R3 — behavior parity is the guard:** the lever/chest/door/determinism `TracerSliceTests` MUST pass
  **unchanged**; any change there means the migration altered behavior (a bug, not a test update).
- **R4 — intended test rewrites (not pre-existing failures):** `EventContextTests` + the `EventContext` ctor
  guard require rewriting because the write-verbs are deliberately gone. Documented as in-scope migration.
- **Reversible-but-load-bearing:** the closed-hierarchy base ctor (could open later); imperative case names
  (vs past-participle); `IReadOnlyList<Outcome>` (vs `ImmutableArray`); the applier as a separate class
  (vs inlined in WorldSim).

## Phase 3 — Implement
- **Built (per the manifest):**
  - **Abstractions:** `Outcome.cs` — `abstract record Outcome` with a **non-public base ctor** (closed to
    the Project layer) + sealed records `SetSwitch(string Key, bool Value)` / `AddCounter(string Key, int Amount)` /
    `ShowMessage(string Text)`. `IEventContext` slimmed to reads (`GetSwitch`/`GetCounter`). `IMapEvent.Run`
    now returns `IReadOnlyList<Outcome>`.
  - **Engine.Sim:** `OutcomeApplier` — `Apply(IReadOnlyList<Outcome>)` switches each kind to `GameState.Set`/
    `Add` + the `Action<string>` sink; null guards; defensive `default` throw. `EventContext` → read-only
    1-arg adapter (dropped the sink + the 3 write methods). `LeverEvent`/`ChestEvent` return outcome
    sequences (give-once, same order). `WorldSim` wires `_eventContext = new EventContext(State)` +
    `_applier = new OutcomeApplier(State, message => CurrentMessage = message)`; `FireStepOn` runs-then-applies
    **before** `SyncDoors` (order preserved).
  - **Tests (compile-fix only — full authoring is Phase 4):** `EventContextTests` → a minimal read-context
    test; `TracerSliceTests` ctor guard → the 1-arg `new EventContext(null!)`.
  - **Verified:** solution GREEN under `-warnaserror` (the MRM determinism analyzer stays clean on the new
    sim code — `Outcome` records are float-free; the applier `foreach` is over an ordered `IReadOnlyList`, not
    a Dictionary; Abstractions stays pure). **All 146 existing tests pass** — the unchanged tracer behavior
    tests (lever opens once / chest gives one potion then empty / message lifecycle / determinism) confirm
    **behavior parity**: the migration preserved observable behavior.
- **Deviations from design (+ reason):**
  1. `EventContextTests` rewritten to a minimal **read-context** test now (compile-fix); the write-verb
     behavior (now `SetSwitch`/`AddCounter`/`ShowMessage` outcomes) is covered by the **Phase-4**
     `OutcomeApplier` tests, exactly as the design's manifest stated. The `EventContext` ctor guard was
     renamed `…_NullArgs_Throw` → `…_NullState_Throws` (the ctor is 1-arg now); the null-sink guard moves to
     the Phase-4 `OutcomeApplier` tests.
  2. No other deviations — the 13-file manifest was followed verbatim; the closed-record `private protected`
     ctor + C# 12 collection expressions (`[]`, `[a, b, c]`) compile clean.
  3. **Deferred to Phase 4** (per the implement skill — no test authoring beyond compile here): the full T1–T9
     suite (`OutcomeTests`, `OutcomeApplierTests`, `EventOutcomeTests`, and the `OutcomeApplier`/read-context
     guard expansion).

## Inspect (Phase 3.5)
- **Lenses run:** 2 critics — (1) correctness + behavior-parity + determinism (ran the suite); (2) design /
  purity / forward-compat / simplification / deferred-test honesty.
- **No source fixes** — the migration is behavior-preserving and correct; both critics confirmed parity
  (146/146 pass). Findings:

  | # | Sev | Finding | Verdict | Resolution |
  |---|---|---|---|---|
  | F1 | — | **Behavior parity** — outcome order matches the old verb order (Lever: ShowMessage→SetSwitch; Chest: AddCounter→SetSwitch→ShowMessage); message lifecycle (clear→apply) preserved; `SyncDoors` runs AFTER apply so the door opens the same tick; give-once guards intact | **VERIFIED CLEAN** (Critic 1, pinned by T4/T5/CT2/CT4) | none |
  | F2 | — | **Closed-record pattern** — `private protected Outcome()` genuinely closes the DU to external assemblies (Project layer gets CS0122; no copy-ctor/reflection escape) while allowing same-assembly + future-P2 cases | **VERIFIED CLEAN** (Critic 2) | none |
  | F3 | — | **Abstractions purity** — Outcome/IEventContext/IMapEvent carry only string/bool/int; the applier `foreach` is over an ordered `IReadOnlyList` (not Dictionary) → MRM analyzer + the Project→Abstractions ring stay green | **VERIFIED CLEAN** (both, build `-warnaserror` 0/0) | none |
  | F4 | — | **Simplification** — `OutcomeApplier` (separate, testable, DI'd) + `EventContext` read-adapter (keeps the Project off `GameState`) + collection expressions + imperative case names are all justified | **VERIFIED CLEAN** (Critic 2) | none |
  | F5 | CRIT(critic)→**TEST DEBT** | The old `EventContextTests` write-through asserts ("kills the SetSwitch/AddCounter no-op mutant"), the null-sink guard, and ShowMessage→sink routing were dropped in the Phase-3 rewrite; they exist only end-to-end (tracer tests) now | **REAL but DEFERRED (planned)** — the Phase-2 manifest already lists `OutcomeApplierTests`/`OutcomeTests`; not a code bug | **Carried to Phase 4** as the explicit checklist below; validate MUST add them or Engine mutation MSI could dip |
  | F6 | HIGH(critic)→**P2 GATE** | When P2 adds battle outcomes (`HpDelta`/…), the EVENT applier's `default:` would throw if it saw one — battle outcomes must route to a separate applier (or the applier extends) | **REAL but FORWARD (P2)** — correct for P0 (events never emit battle outcomes); documented | P2-design gate item: "verify outcome→applier routing (event vs battle)" |
  | F7 | LOW/NIT | Outcome *order* isn't pinned by a dedicated test (inert today — disjoint keys); `Apply` is non-atomic on the unreachable `default` throw; Outcome.cs:8 doc is forward-looking | **NITS** | order-pin folded into the Phase-4 `OutcomeApplierTests`; atomicity is a future-P2 note; doc is intentional |

- **Phase-4 validate checklist (must-add so no coverage is silently lost):**
  - `OutcomeTests` — record value-equality + payload access for the 3 cases (T1).
  - `OutcomeApplierTests` — each kind write-through (`SetSwitch`→`state.Set`, `AddCounter`→`state.Add`, `ShowMessage`→sink exactly once), **multi-outcome applied in list order**, and null guards (state / sink / outcomes) — these re-home the dropped write-through mutant-kills + the null-sink guard.
  - `EventOutcomeTests` — `LeverEvent`/`ChestEvent` `Run` return the exact outcome sequences (give-once).
  - Confirm Engine mutation MSI ≥ 80 after the additions (the `OutcomeApplier` switch arms + null guards killed; the unreachable `default` throw is the documented equivalent mutant).
- **Forge capture:** `prevention-rule-record` PR-claude-migration-rehome-dropped-tests-001 (re-home the old tests' mutant-kills when migrating a behavior to a new shape — a green end-to-end suite masks dropped isolation coverage until the mutation gate catches it). No `failure-record` (no bug — the migration is clean).

## Phase 4 — Validate
- **Tests added (15, honoring the Inspect F5 checklist):**
  - `OutcomeTests` (4) — record value-equality + payload access + distinct-subtype for `SetSwitch`/
    `AddCounter`/`ShowMessage` (REQ-002).
  - `OutcomeApplierTests` (7) — **re-homes the dropped write-through coverage**: `SetSwitch`→`state.Set`
    (Value honored, not hardcoded), `AddCounter`→`state.Add` (accumulates), `ShowMessage`→sink exactly once;
    a multi-outcome list applies every kind; **application order = list order** (last-write-wins + sink order);
    empty list is a no-op; **null guards** on state / sink / outcomes (REQ-003).
  - `EventOutcomeTests` (4) — `LeverEvent`/`ChestEvent.Run` return the **exact** outcome sequences (record
    equality on the list): lever first → `[ShowMessage, SetSwitch]`, reopen → `[]`; chest first →
    `[AddCounter, SetSwitch, ShowMessage]`, opened → `[ShowMessage("empty")]` — pins the guards, strings,
    keys, amount, and order (REQ-001).
  - The behavior-parity guard — the existing `TracerSliceTests` (lever/chest/door/determinism) — stayed
    **unchanged and green** (REQ-003 end-to-end).
- **`dotnet test MonoRpgMaker.slnx`: Passed — 161 passed, 0 failed** (+15; was 146).
- **`bin/gate.sh`: GREEN [full]** — all 12 gates. Coverage **97.6%**; mutation MSI **Engine 90.67% /
  Abstractions 82.22% / Analyzers 92.35%** (all ≥ 80). Receipt written.
- **Engine MSI 91.30% → 90.67%:** the new `OutcomeApplier` switch arms + null guards are killed by
  `OutcomeApplierTests`; the only survivor it adds is the **unreachable `default:` throw** (the documented
  P0-equivalent mutant — no fourth `Outcome` case to construct against the closed DU). Well above the 80
  floor; no floor lowered, nothing skipped (§0). REQ-005/REQ-006 hold (Abstractions purity + the MRM
  analyzer stayed green through gate:2/12).
- **In-phase fix:** a **CA1861** warning (constant-array arg `new[] { "first", "second" }` to `Assert.Equal`)
  in `OutcomeApplierTests` would have failed gate:2 `-warnaserror`; rewritten to per-index asserts. Not
  pre-existing.
- **Pre-existing exclusions:** none. No new packages → lock files unchanged. `StrykerOutput/` gitignored.

## Phase 5 — Complete
- Docs updated:
- Forge capture (aar/failures/rules/decisions):
- Ticket closed:
- Archived:
