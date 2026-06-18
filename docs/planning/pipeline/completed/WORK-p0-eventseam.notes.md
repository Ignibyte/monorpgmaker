# WORK-p0-eventseam — Notes

Per-phase working notes for the paired `.spec.md`. The spec is the contract;
this is the record of what happened.

## Phase 1 — Plan
- Request: P0 slice 3b — move the event-seam contracts (`IMapEvent`, `EventTrigger`, new
  `IEventContext`) into `MonoRpgMaker.Abstractions`; migrate the tracer to consume them +
  slice-3a's `GridPoint`. The runtime types (`GameState`, `EventContext`) stay in Engine.
- Intake source: none.
- Classification / tier: work pipeline, **type = feature** (architectural extraction; one slice).
- **Surface read in /work (all tiny):** `IMapEvent.cs` 16L (`Point Cell`, `EventTrigger Trigger`,
  `Run(EventContext)`), `EventContext.cs` 29L (`GameState State` + `ShowMessage`), `EventTrigger.cs`
  8L (pure enum `StepOn`), `GameState.cs` 44L (`Get`/`Set` switch, `GetCount`/`Add` counter — pure).
  Events use exactly: `State.Get` (switch), `State.Set` (switch), `State.Add` (counter),
  `ShowMessage` ⇒ that's the `IEventContext` verb set. `WorldSim.FireStepOn(Point)` compares
  `mapEvent.Cell == cell` ⇒ needs the `Point`↔`GridPoint` adapter.
- Forge recall: **D-0014** (Abstractions = the published seam; "Project → Abstractions only");
  **agentic-substrate §4** names "the `EventContext` semantic-verb surface" as a seam ⇒ `IEventContext`
  is that surface. [[AD-claude-gridpoint-coordinate-001]] (GridPoint is the coordinate the seam adopts).
- Ticket: **745c603e-fceb-44b3-b2c0-b002bb871c7e (#6)** — created with explicit `project_id a9ee8162`;
  returned row confirms **monorpgmaker** (#1–#5 done, #6 open).
- AAR id (from `aar-open`): b032ba16-e7a3-4e2f-adc4-60bce34ca148
- EARS requirements reviewed: REQ-001..007.

## Phase 2 — Design

### Approach / architecture
The event seam splits into pure **contracts** (Abstractions) + runtime **impl** (Engine):
- **Abstractions (the published seam, pure):** `EventTrigger` (enum), `IMapEvent`
  (`GridPoint Cell`, `EventTrigger Trigger`, `Run(IEventContext)`), `IEventContext` (the
  5-verb surface). Uses only `GridPoint` + `string`/`int`/`bool` → the NetArchTest ring holds.
- **Engine (runtime):** `GameState` unchanged; `EventContext : IEventContext` delegates the
  verbs to a private `GameState` + message sink; `GridPointExtensions` is the `Point`↔`GridPoint`
  adapter; the tracer events implement `Abstractions.IMapEvent`; `WorldSim` holds
  `Abstractions.IMapEvent` and converts at the `Point` boundary via the adapter.
- This adds the first **Engine→Abstractions `ProjectReference`** (deferred since slice 1). The
  World/Entities/host stay on XNA `Point`; only the seam adopts `GridPoint`, bridged by the adapter.

### The 3 decisions — settled
1. **`IEventContext` includes `GetCounter`** — a complete switch+counter read/write surface (the
   published contract should be complete; EC1 covers it even though the tracer events only `Add` today).
2. **`EventContext` drops its public `State` property** — verb-surface-only over a private
   `GameState`; `WorldSim` already exposes its own `State`, so nothing references `EventContext.State`.
3. **Adapter = `World/GridPointExtensions.cs`** — `ToGridPoint(this Point)` / `ToXnaPoint(this GridPoint)`.

### File manifest
| # | File | Change |
|---|---|---|
| 1 | `src/MonoRpgMaker.Abstractions/EventTrigger.cs` | **ADD** — move the pure enum (ns `MonoRpgMaker.Abstractions`) |
| 2 | `src/MonoRpgMaker.Abstractions/IMapEvent.cs` | **ADD** — `GridPoint Cell`, `EventTrigger Trigger`, `void Run(IEventContext)` |
| 3 | `src/MonoRpgMaker.Abstractions/IEventContext.cs` | **ADD** — `GetSwitch`/`SetSwitch`/`GetCounter`/`AddCounter`/`ShowMessage` |
| 4 | `src/MonoRpgMaker.Engine/Sim/EventTrigger.cs` | **DELETE** (moved) |
| 5 | `src/MonoRpgMaker.Engine/Sim/IMapEvent.cs` | **DELETE** (moved) |
| 6 | `src/MonoRpgMaker.Engine/MonoRpgMaker.Engine.csproj` | **MODIFY** — `ProjectReference` → Abstractions (restore → lock) |
| 7 | `src/MonoRpgMaker.Engine/World/GridPointExtensions.cs` | **ADD** — `ToGridPoint`/`ToXnaPoint` |
| 8 | `src/MonoRpgMaker.Engine/Sim/EventContext.cs` | **MODIFY** — `: IEventContext`; private `GameState` + sink; verbs delegate; drop public `State` |
| 9 | `src/MonoRpgMaker.Engine/Sim/Tracer/LeverEvent.cs` | **MODIFY** — `Abstractions.IMapEvent`; `GridPoint` ctor; verb calls; drop XNA using |
| 10 | `src/MonoRpgMaker.Engine/Sim/Tracer/ChestEvent.cs` | **MODIFY** — same shape (give-once) |
| 11 | `src/MonoRpgMaker.Engine/Sim/WorldSim.cs` | **MODIFY** — `List<Abstractions.IMapEvent>`; `FireStepOn` compares `Cell == cell.ToGridPoint()` |
| 12 | `src/MonoRpgMaker.Engine/Sim/Tracer/TracerRoom.cs` | **MODIFY** — build events with `GridPoint` cells |
| 13 | `tests/MonoRpgMaker.Engine.Tests/TracerSliceTests.cs` | **MODIFY** — `Point`→`GridPoint` at event construction; assertions unchanged |
| 14 | `tests/MonoRpgMaker.Engine.Tests/EventContextTests.cs` | **ADD** — EC1 (Phase 4) |
| 15 | `tests/MonoRpgMaker.Engine.Tests/GridPointExtensionsTests.cs` | **ADD** — AD1 (Phase 4) |

### Regression Test Plan
| # | Test | Proves Requirement |
|---|---|---|
| EC1 | `EventContext` verb delegation — `SetSwitch`/`GetSwitch` round-trip *and write through to the backing `GameState`*; `AddCounter`/`GetCounter`; `ShowMessage` routes text to the sink | REQ-003 |
| AD1 | adapter round-trip — `new Point(3,5).ToGridPoint()==new GridPoint(3,5)`; `new GridPoint(3,5).ToXnaPoint()==new Point(3,5)`; negative coords | REQ-005 |
| GP-reuse | existing `TracerSliceTests` (updated only for `GridPoint` construction) — lever opens the door **once**; chest **gives one potion then reads empty**; step-on fires at the right cell | REQ-004 (behaviour parity) |
| — | REQ-001/002 (contracts in Abstractions; `Cell` is `GridPoint`) | build + review |
| — | REQ-006 (Abstractions stays MonoGame/Engine-free) | NetArchTest ring |
| — | REQ-007 FULL gate green | gate run |

**Uncoverable / exclusions:** none new. (`RpgGame` host loop stays `[ExcludeFromCodeCoverage]` as before; it's untouched.)

### Risks / decisions
- **R1 — missed consumers.** `/work` grep confirmed the seam's only consumers are
  `WorldSim`/`LeverEvent`/`ChestEvent`/`TracerRoom`/`TracerSliceTests` (all in the manifest);
  `RpgGame` reads `WorldSim.CurrentMessage`, not `IMapEvent`. The `-warnaserror` build catches any miss.
- **R2 — Engine `packages.lock.json`.** Adding a `ProjectReference` to *pure* Abstractions (no NuGet
  deps) shouldn't change the lock; `dotnet restore` + the gate confirm — regen if it does.
- **R3 — mutation.** New contracts are interfaces/enum (**no mutants** → Abstractions MSI 82.22%
  unaffected). EC1 + AD1 kill the new Engine mutants (the `EventContext` verb delegations + the
  adapter), so Engine MSI holds ~91.30%. Watch: a verb delegating to the *wrong* `GameState` method
  is a killable mutant — EC1's write-through assertions catch it.
- **R4 — behaviour parity.** The tracer semantics are unchanged; the updated `TracerSliceTests`
  are the regression guard — green = the migration preserved semantics.

## Phase 3 — Implement
- Built (solution build **0 warn / 0 err** `-warnaserror`; `dotnet format` clean; tracer
  behaviour **16/16** green):
  - **Abstractions (new, pure):** `EventTrigger.cs` (moved enum), `IMapEvent.cs`
    (`GridPoint Cell` + `Run(IEventContext)`), `IEventContext.cs`
    (`GetSwitch`/`SetSwitch`/`GetCounter`/`AddCounter`/`ShowMessage`).
  - **Engine:** `Sim/EventContext.cs` now `: IEventContext` (verbs delegate to a private
    `GameState` + sink; public `State` dropped); `World/GridPointExtensions.cs`
    (`ToGridPoint`/`ToXnaPoint` adapter); `MonoRpgMaker.Engine.csproj` +`ProjectReference`→
    Abstractions (the consumption edge); `Sim/Tracer/LeverEvent.cs` + `ChestEvent.cs`
    implement `Abstractions.IMapEvent` (GridPoint ctor, verb calls); `Sim/WorldSim.cs` holds
    `Abstractions.IMapEvent` and `FireStepOn` compares via `cell.ToGridPoint()`;
    `Sim/Tracer/TracerRoom.cs` builds events with `.ToGridPoint()`.
  - **DELETED:** Engine `Sim/EventTrigger.cs` + `Sim/IMapEvent.cs` (moved to Abstractions).
  - `tests/TracerSliceTests.cs`: **compile-fix only** (`Point`→`GridPoint` at the 2
    event-construction sites + `using MonoRpgMaker.Abstractions`); behaviour assertions
    unchanged. EC1/AD1 land in Phase 4.
- Deviations from design: **none**. `dotnet restore` updated **4** `packages.lock.json`
  (Engine records the new `Abstractions` *project* dependency; Player/Editor/Tests pick it up
  transitively) — anticipated by the design's "restore → lock", and required so the gate's
  locked-mode restore matches the new reference graph. (Abstractions adds no NuGet packages, so
  only the project-ref entries changed.)
- Parity: the 16 tracer/sim/guard tests pass unchanged ⇒ the migration preserved semantics;
  `RpgGame`/`Player`/`Editor` compile (whole-solution `-warnaserror`), confirming no missed consumer.

## Inspect (Phase 3.5)
- Lenses run: 2 general-purpose critics (proportionate to a 15-file migration), each verifying
  concretely (`git diff` + grep whole repo + `-warnaserror` build + run tests): **C1**
  correctness/parity/missed-consumers; **C2** architecture/purity/§14/mutation-readiness.
- Findings:
  | # | Severity | Finding | Verdict | Fix |
  |---|---|---|---|---|
  | C1 | — | **NO FINDINGS** — parity-clean: all 5 verbs delegate to the right `GameState` methods (GetSwitch→Get, SetSwitch→Set, GetCounter→GetCount, AddCounter→Add, ShowMessage→sink); Lever/Chest logic + messages + consts byte-identical; `WorldSim.FireStepOn`'s `Cell == cell.ToGridPoint()` faithful; **zero** lingering refs to the old `Engine.Sim.IMapEvent`/`EventTrigger` or the dropped `EventContext.State` (Player/Editor/RpgGame untouched); 76/76 green. | CLEAN | — |
  | C2 | — | **NO FINDINGS** — Abstractions pure (ring 3/3 green; no MonoGame/Engine; one-way edge; adapter correctly in Engine); `dotnet restore --locked-mode` EXIT=0; 0/0 `-warnaserror`; XML-doc'd + nullable-clean. The 7 new Engine mutants (5 verb + 2 adapter block-removals) are all killed by EC1+AD1 as designed; Abstractions stays 82.22% (contracts = interfaces/enum → no mutants). | CLEAN | — |
  | A1 | low (validate must-do) | `ToXnaPoint` has **zero** production callers and `GetCounter` has no caller — their block-removal mutants are killed ONLY by AD1's reverse assertion + EC1's counter round-trip; if Phase 4 omits them they survive and drop Engine MSI. | ACCEPTED | Phase 4 MUST write **EC1** with the `GetCounter` round-trip + the `SetSwitch`/`AddCounter` **write-through** assertions, and **AD1** testing **both** directions (incl. `ToXnaPoint`). The design's plan already specifies these — emphasized for validate. |
  | A2 | info | The lock change also **removed** a stale `Abstractions → MonoGame.Framework.DesktopGL` line in the Tests lock (Abstractions never referenced MonoGame) — a correctness improvement, not churn. | NOTED | — |
- **Verdict:** no production defects (both critics clean). The migration preserves semantics +
  purity. Mutation coverage is sound **provided** Phase 4 writes EC1/AD1 as designed (the
  write-through + both-direction assertions are load-bearing for the no-caller `GetCounter`/`ToXnaPoint`).
- Re-verify: no code changed in inspect; the Phase 3 build (0/0) + 16 parity tests stand.

## Phase 4 — Validate
- Tests added (2 new files):
  - `tests/MonoRpgMaker.Engine.Tests/EventContextTests.cs` — **EC1**
    `Verbs_DelegateToBackingState_AndSink`: the 5 verbs delegate to the backing `GameState`
    with **write-through asserted** via `state.Get`/`GetCount` (the load-bearing kills for the
    no-caller `GetCounter` + the `SetSwitch`/`AddCounter` no-op mutants) + `ShowMessage` routes to the sink.
  - `tests/MonoRpgMaker.Engine.Tests/GridPointExtensionsTests.cs` — **AD1**
    `Adapter_ConvertsBothDirections`: `ToGridPoint` + `ToXnaPoint` round-trip, **both directions**
    incl. negatives (the reverse is `ToXnaPoint`'s only killer).
  - (`TracerSliceTests`, migrated in Phase 3, carries the behaviour parity + the other 4 verbs +
    `ToGridPoint` via `MovePlayer`.)
- `dotnet test MonoRpgMaker.slnx`: **Passed — 78/78, 0 failed** (2 new).
- `bin/gate.sh` (FULL): **GATE GREEN [full] — 12/12.** Coverage **99.2%**. Mutation Engine
  **91.30%** (held — the 7 new verb/adapter block-removal mutants all killed) / Abstractions
  **82.22%** (unchanged — new contracts are interfaces/enum → no mutants). Locked-mode restore
  matched the 4 updated `packages.lock.json`. Receipt written.
- Pre-existing exclusions: none.

## Phase 5 — Complete
- Docs updated:
  - `docs/roadmap.md` P0 status note → "slices 1–3 + 3b landed"; the event seam
    (`IMapEvent`/`EventTrigger`/`IEventContext`) is in Abstractions, the Engine→Abstractions
    edge live; remaining P0 = the float-ban analyzer (+ sim/host scoping) and the generator/scaffolding.
  - No `CLAUDE.md` / `decisions.md` change — D-0014 governs; the seam contract is the forge AD.
- Forge capture (aar/failures/rules/decisions):
  - `aar-submit` closed AAR `b032ba16` (outcome=completed, effectiveness 5/5; 2 novel findings).
  - **AD** `AD-claude-event-seam-abstractions-001` — the contracts/runtime split + the Point↔GridPoint adapter.
  - **PR** `PR-claude-no-caller-member-needs-dedicated-mutation-test-001` — no-caller verbs/adapter
    directions need dedicated round-trip + write-through unit tests (the inspect's catch).
  - No `failure-record`: both critics returned clean; the no-caller-mutation point was a **preempted**
    gap (the design's EC1/AD1 already specified the load-bearing assertions), not a shipped bug.
- Ticket closed: #6 (`745c603e-fceb-44b3-b2c0-b002bb871c7e`) → done.
- Archived: `active/` → `completed/` (spec + notes).
- **Commit note:** file moves (`git rm` of the 2 old Engine seam files) + new Abstractions contracts +
  adapter + EventContext rework + tracer migration + EC1/AD1 + 4 `packages.lock.json` + roadmap/pipeline
  docs. `git add -A` stages the deletes. Push to `origin` (Ignibyte) per the standing approval.
