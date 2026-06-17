# WORK-m0-chest-give-once — Notes

Per-phase working notes for the paired `.spec.md`. The spec is the contract;
this is the record of what happened.

## Phase 1 — Plan
- Request: M0 GO/NO-GO spike — author a "chest gives one potion, then empty"
  hand-C# event variant on the tracer slice; wire into `MonoRpgMaker.Player`;
  unit-test the give-once→empty logic; purpose = time the AI-directed authoring
  loop vs RPG Maker's click-path (~3× + "feels good") and record the GO/NO-GO
  before committing to P0.
- Intake source: none.
- Classification / tier: work pipeline, **type = spike** (one shippable slice;
  the realization of WORK-m0-tracer-bullet-v1 locked decision #6 / roadmap P-(-1)).
- Forge recall (lessons/failures surfaced):
  - Tracer (#1 / WORK-m0-tracer-bullet-v1) is green and is the M1 extraction base;
    the `Engine.Sim` seams to mirror are `IMapEvent` / `EventContext` / `GameState`
    / `WorldSim` / `DoorRule`, with `LeverEvent` the exemplar (check switch → grant
    + narrate → set switch — the give-once shape already exists there).
  - `PR-claude-guards-001` (from the tracer's Inspect): guard public preconditions
    with `ArgumentNullException.ThrowIfNull` — apply to the new `ChestEvent`/state.
  - `GameState` is bool-switch-only today → the "one potion" representation is a
    real Phase 2 design choice (switch-only narration vs. a minimal counter).
  - **Forge hygiene:** knowledge/docs search bleed across tenants (oathstar
    Datastar/Tauri/Rust hits appear); discarded per CLAUDE.md. Tickets + code are
    project/repo-scoped and clean.
- Ticket: **3e56871a-eb6f-4d61-9b81-ba0262073253 (#2)** — created with explicit
  `project_id a9ee8162-3cfd-4c99-be5c-bbdb4be32b10`; the returned row's
  `project_id` confirms it landed under **monorpgmaker** (first live test of the
  cross-project scoping fix — PASS). Per-project numbering confirmed (monorpgmaker
  #1 done, #2 open).
- AAR id (from `aar-open`): 52131509-9182-4db3-904e-6fae94d500da
- EARS requirements reviewed: REQ-001..007 (first-grant-once, empty-after,
  determinism, Player smoke, source-bans, FULL gate, GO/NO-GO doc).

## Phase 2 — Design

### Approach / architecture
Mirror the lever/door split exactly — the chest is a hand-authored `IMapEvent`
that **sets state + narrates**, and a separate switch-driven rule reacts to the
state. Nothing new in the host's control flow.

- **`ChestEvent : IMapEvent`** (in `Sim/Tracer`, beside `LeverEvent`): `Trigger
  => EventTrigger.StepOn`. `Run`: if `State.Get(OpenedSwitch)` → `ShowMessage("The
  chest is empty.")` and return; else `State.Add(PotionCount, 1)` +
  `State.Set(OpenedSwitch, true)` + `ShowMessage("You open the chest and take a
  potion.")`. This is the "check a flag, grant, narrate" shape the scaffolder will
  emit — identical in spirit to `LeverEvent`. No `ThrowIfNull(context)` guard in
  `Run` (matches `LeverEvent`; `EventContext` is non-null by construction —
  `WorldSim` owns it and already guards its inputs).
- **`GameState` += a named-int counter store** (the embryonic RPG-Maker
  *variables*, mirroring the bool *switches*): `int GetCount(string key)` (0 when
  unset) and `void Add(string key, int amount)`, both `ThrowIfNull(key)`. This is
  the smallest representation that makes "the potion **tally** rises by exactly
  one" (REQ-001/002) directly observable. Not put on `Actor` (inventory is a
  future module, not an entity concern) and not resolved through `Data.ItemRecord`
  /`Database<T>` (those are catalog definitions with no grant mechanism — M1 can
  later resolve the `"potions"` key to an `ItemRecord`).
- **`TracerRoom`**: add non-blocking `ChestClosed` (TilesetId 5) + `ChestOpen`
  (TilesetId 6) tiles; place a chest at **(11,4)** — beyond the switch-driven door
  on row 4, so the authored micro-sequence is *pull lever → door opens → walk
  through → open chest → take potion → empty on re-entry* (a richer thing to time
  for the GO/NO-GO). Add `new ChestEvent(chestCell)` to events and **reuse
  `DoorRule`** (`DoorRule(chestCell, ChestEvent.OpenedSwitch, ChestClosed,
  ChestOpen)`) for the visual closed→open swap on `SyncDoors` — both tiles
  non-blocking, so it's a pure visual swap (no passability effect).
- **`RpgGame.ColorFor`**: add arms for TilesetId 5 (closed chest — amber/brown)
  and 6 (open chest — dim/emptied). Host-only render mapping.
- **`Player/Game1`**: **no change** — it boots `TracerRoom.Build()`, so the chest
  auto-wires into the desktop app (REQ-004).

§14: typed; nullable-clean; deterministic (scripted, no RNG/`DateTime`); immutable
(`sealed ChestEvent`, readonly tiles); framework-thin (lives in `Engine.Sim`;
`Point` is the established XNA dependency the tracer already uses); no new statics.

### File manifest
| # | File | Change |
|---|---|---|
| 1 | `src/MonoRpgMaker.Engine/Sim/Tracer/ChestEvent.cs` | **ADD** — `ChestEvent : IMapEvent`, give-once-then-empty potion grant (`OpenedSwitch="chest_opened"`, `PotionCount="potions"`) |
| 2 | `src/MonoRpgMaker.Engine/Sim/GameState.cs` | **MODIFY** — add named-int counter store: `GetCount(key)` (default 0) + `Add(key, amount)`, both `ThrowIfNull(key)` |
| 3 | `src/MonoRpgMaker.Engine/Sim/Tracer/TracerRoom.cs` | **MODIFY** — `ChestClosed`/`ChestOpen` tiles; place chest at (11,4); add `ChestEvent` + a chest `DoorRule` for the visual swap |
| 4 | `src/MonoRpgMaker.Engine/Core/RpgGame.cs` | **MODIFY** — `ColorFor` arms for chest closed (5) + open (6); host, `[ExcludeFromCodeCoverage]` |
| 5 | `tests/MonoRpgMaker.Engine.Tests/TracerSliceTests.cs` | **MODIFY** — add `TracerChestTests` + `GameState` counter tests |

### Regression Test Plan
| # | Test | Proves Requirement |
|---|---|---|
| CT1 | `Chest_FirstStepOn_GrantsOnePotion_AndShowsTakeMessage` — step onto a chest cell; assert `GetCount("potions")==1`, `Get("chest_opened")` true, message contains "potion" | REQ-001 |
| CT2 | `Chest_SteppedOnRepeatedly_ShowsEmpty_AndTallyStaysOne` — on→off→on(→on); 2nd message == "The chest is empty.", `GetCount("potions")==1` after all repeats | REQ-002 |
| CT3 | `Chest_SameCommandSequence_OnTwoFreshSims_IdenticalOutcome` — same command list on two `WorldSim`s ⇒ equal potion tally + equal message sequence | REQ-003 |
| CT4 | `TracerRoom_PlacesChest_AndWiresChestEvent` — `TracerRoom.Build()` has a chest tile at (11,4) + a `ChestEvent` at that cell, and walking the built sim onto it grants one potion (proves the Player-booted scene is wired) | REQ-004 (render itself = manual smoke) |
| GS1 | `GameState_GetCount_DefaultsToZero` | REQ-001 (counter default) |
| GS2 | `GameState_Add_AccumulatesCount` — `Add("k",1)` twice ⇒ 2 (kills the `+`/amount mutants) | REQ-001/002 (counter) |
| GS3 | `GameState_Add_NullKey_Throws` | guard ([[PR-claude-guards-001]]) |
| — | REQ-005 — no `System.Random`/`DateTime` | source-bans gate (BannedApiAnalyzers) + review |
| — | REQ-006 — slice passes FULL `bin/gate.sh` | gate run at Phase 4 |
| — | REQ-007 — GO/NO-GO measurement recorded | doc check at `/pipeline:complete` |

**Uncoverable path:** `RpgGame.ColorFor` chest arms + the render — `RpgGame` is
`[ExcludeFromCodeCoverage]` (the host/composition root owning the live MonoGame
loop); validated by the REQ-004 manual smoke. `Game1` is unchanged.

### Risks / decisions
- **D1 — Trigger = `StepOn`** (walk-on chest), mirroring the lever. Interact/
  confirm-to-open (the RPG Maker norm) is deferred: it needs a new `EventTrigger`
  value + `Actor` facing + host input. Reversible.
- **D2 — Potion = a named-int counter on `GameState`** ("variables"). Smallest
  observable grant; not `Actor` inventory, not a `Database`/`ItemRecord` lookup.
- **D3 — Reuse `DoorRule`** for the chest's visual swap. *Load-bearing but
  reversible semantic stretch:* `DoorRule` reads as door/passability; here both
  tiles are non-blocking so it's a pure visual swap. **M1 should generalize it to
  a `TileSwitchRule`** (door + chest + …). Recorded so M1 doesn't cement the name.
- **R1 — new counter = mutation surface** → killed by GS1–GS3 exact-count asserts.
- **R2 — hard-coded message strings** → asserted by exact substring/equality so a
  copy change is a deliberate test update (enumerate-string-arms discipline).
- **R3 — chest at (11,4) reachability** in `TracerRoom` (beyond the door, row-4
  floor) → verified by CT4.

## Phase 3 — Implement
- Built (build: 0 warn / 0 err under `-warnaserror`; `dotnet format --verify-no-changes` clean):
  - `Sim/Tracer/ChestEvent.cs` (**new**) — `ChestEvent : IMapEvent`, `StepOn`;
    `OpenedSwitch="chest_opened"`, `PotionCount="potions"`. First trigger →
    `State.Add(PotionCount,1)` + `State.Set(OpenedSwitch,true)` + "You open the
    chest and take a potion."; later → "The chest is empty." Mirrors `LeverEvent`
    (no `Run` context guard — `EventContext` is non-null by construction, same as
    the lever).
  - `Sim/GameState.cs` — added the named-int counter store: `GetCount(key)` (0
    default) + `Add(key, amount)`, both `ArgumentNullException.ThrowIfNull(key)`;
    class doc updated (switches + variables).
  - `Sim/Tracer/TracerRoom.cs` — `ChestClosed`(5)/`ChestOpen`(6) non-blocking
    tiles; chest at **(11,4)** beyond the door on row 4; added `ChestEvent` to
    events + a reused `DoorRule(chestCell, ChestEvent.OpenedSwitch, ChestClosed,
    ChestOpen)` for the closed→open visual swap (cell owned by `SyncDoors`,
    mirroring the door — no explicit `SetTile`).
  - `Core/RpgGame.cs` — `ColorFor` arms for tile 5 (closed chest, amber) + 6
    (open chest, dim). Host is `[ExcludeFromCodeCoverage]`.
  - `Player/Game1.cs` — **unchanged** (boots `TracerRoom.Build()`; chest
    auto-wires into the desktop app).
- Deviations from design (+ reason): **none** — built exactly to the Phase 2
  manifest. (Letting `SyncDoors` own the chest cell was the design's stated intent,
  not a deviation.)

## Inspect (Phase 3.5)
- Lenses run: 3 parallel general-purpose critics, each verifying concretely (read
  the code + `dotnet build -warnaserror` + trace the exact cells/branches):
  1. correctness + determinism + AC traceability (REQ-001..005; reachability of
     (11,4); `SyncDoors` init ordering; give-once→empty→repeat dispatch)
  2. §14 conventions + sim/render separation + the `DoorRule`-reuse decision +
     NetArchTest layering
  3. simplification / reuse / allocations
- Findings:
  | # | Severity | Finding (file:line) | Verdict | Fix |
  |---|---|---|---|---|
  | 1 | low | `GameState.Add` does a double dictionary lookup (`_counters[key] = GetCount(key) + amount;`) — GameState.cs | **NO-ACTION** | Mirrors the adjacent `Get`/`Set` plain-dictionary idiom; the `GetCount` reuse keeps the counter pair symmetric/readable; `GameState` is not a hot path (once per chest interaction). `CollectionsMarshal.GetValueRefOrAddDefault` would be a premature, inconsistency-introducing micro-opt. Consciously declined for M0. |
  | 2 | info | New public surface ships without tests (`ChestEvent`, `GameState.GetCount`/`Add`) | **EXPECTED** | Tests are Phase 4 (`/pipeline:validate`): CT1–CT4 + GS1–GS3 are already in the Phase 2 plan. Not an implement-phase defect. |
  | 3 | info | Potion is a stringly-keyed counter (`"potions"`), not a `Data.ItemRecord` ref; `Add` permits negatives (no clamp) | **ACCEPTED (M0)** | Faithful enough for the spike — `ChestEvent` only ever grants `+1`, switch-guarded. M1 can resolve `"potions"`→`ItemRecord` and add inventory/clamp semantics. Recorded, no change. |
- **Verdict: no correctness, convention, separation, determinism, or layering
  defects.** All three lenses clean — each independently confirmed by building at
  `-warnaserror` + tracing. No source fix required; **no `failure-record`** (nothing
  shipped or broke); **no new prevention-rule** (no class of mistake).
- Re-verify: zero source edits in this phase, so the Phase 3 build (0 warn / 0 err,
  `dotnet format` clean) still stands.

## Phase 4 — Validate
- Tests added (8 new methods in `tests/MonoRpgMaker.Engine.Tests/TracerSliceTests.cs`):
  - `TracerChestTests`: **CT1** `Chest_FirstStepOn_GrantsOnePotion_AndShowsTakeMessage`
    (REQ-001), **CT2** `Chest_SteppedOnRepeatedly_ShowsEmpty_AndTallyStaysOne`
    (REQ-002), **CT3** `Chest_SameCommandSequence_OnTwoFreshSims_IdenticalOutcome`
    (REQ-003), **CT4** `TracerRoom_Chest_IsPlacedWired_AndGrantsWhenReached`
    (REQ-004 — walks the player lever→door→chest in the actual `TracerRoom.Build()`
    scene, asserts the closed→open swap + the grant at (11,4)).
  - `GameStateCounterTests`: **GS1** `GetCount_UnsetKey_IsZero`, **GS2**
    `Add_AccumulatesCount` (kills the `+`/amount mutants), **GS3**
    `Add_NullKey_Throws` + `GetCount_NullKey_Throws` (both guards).
  - REQ-005 (no `System.Random`/`DateTime`) is proven by gate:9 source-bans; REQ-007
    (GO/NO-GO doc) lands at Phase 5.
- `dotnet test MonoRpgMaker.slnx`: **Passed — 40/40, 0 failed** (8 new), 67 ms.
- `bin/gate.sh` (FULL): **GATE GREEN [full] — 12/12 passed.** Coverage **99.0%**
  (floor 80), mutation MSI **91.30%** (floor 80), build 0 warn/0 err (`-warnaserror`),
  format clean, and source-bans / secrets / licenses / shellcheck / no-suppressions /
  doc-todos / vuln-deps all green. Receipt written
  (`.git/monorpgmaker-gate-receipt`) — `/commit` is satisfiable.
- Pre-existing exclusions: none (no pre-existing failures). `RpgGame` remains
  `[ExcludeFromCodeCoverage]` — the only excluded surface, by design (host root).

## Phase 5 — Complete
- Docs updated:
  - `docs/roadmap.md` P-(-1): added a ticket #2 corroboration note under the existing
    **GO** status (40 tests / 99.0% cov / 91.30% MSI). Did **not** reopen the GO — see REQ-007.
  - No `CLAUDE.md` / `decisions.md` change (no convention shift; the gate-level and
    TileSwitchRule decisions live in this spec + the forge AD).
- Forge capture (aar/failures/rules/decisions):
  - `aar-submit` closed AAR `52131509-9182-4db3-904e-6fae94d500da` (outcome=completed,
    effectiveness 4/5; 2 novel findings enqueued).
  - **AD** `AD-claude-tile-switch-rule-001` — reuse `DoorRule` now; generalize to
    `TileSwitchRule` in M1.
  - **PR** `PR-claude-forge-doc-staleness-001` — forge docs/knowledge search reflect the
    COMMITTED index, which lags the local working tree; read the local file before trusting a
    recalled status (this run was framed off a stale "pending" GO).
  - No `failure-record`: inspect found no real code bug; the stale-status misframing is
    captured as the PR above.
- **REQ-007 — GO/NO-GO resolution (honest):** the make-or-break GO was **already decided GO
  before this session** — `docs/roadmap.md` P-(-1) status (author ran the tracer slice, judged
  the loop good: "thesis validated, P0 greenlit"). The forge index consulted in `/work` was
  stale and still read "pending", so this chest variant is **corroboration** (a second
  gate-clean authored-event exemplar), not the gating measurement. **No measurement fabricated.**
  Optional formal procedure if a number is ever wanted:
  1. From a tracer-only baseline, start a stopwatch.
  2. Author the give-once chest via the AI-directed loop (intent → C# → gate green); record
     wall-clock + edit count.
  3. Author the equivalent "chest gives one potion, then empty" event in RPG Maker MZ via its
     editor click-path; record wall-clock.
  4. GO iff AI-loop time ≤ ~3× the RPG Maker time AND the loop "feels good" to the maker.
  Result slot (fill only if run): AI-loop = ____ · RPG Maker = ____ · ratio = ____ · feels-good = ____.
- Ticket closed: #2 (`3e56871a-eb6f-4d61-9b81-ba0262073253`) → done.
- Archived: `active/` → `completed/` (this spec + notes pair).
