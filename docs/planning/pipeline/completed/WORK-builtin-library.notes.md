# WORK-builtin-library — Notes

Per-phase working notes for the paired `.spec.md`. The spec is the contract;
this is the record of what happened.

## Phase 1 — Plan
- **Request:** Expand the placeable built-in behaviour library (the trigger program's no-code library) beyond
  `ShowText` + `Warp`. The user listed chest / door / shop / give-item. Slice C of 3 under a standing `/goal`
  (A re-skin ✓ → B save/load ✓ → C the library).
- **Intake source:** none (direct `/work`).
- **Classification / tier:** work pipeline, MEDIUM slice (no split needed for the chosen cut). P2 events/behaviours.
- **Forge recall + Explore (the scoping ground truth):** an `Explore` agent mapped the surface —
  - The tracer `ChestEvent`/`LeverEvent` (`Sim/Tracer/`) are self-contained (compose `AddCounter`/`SetSwitch`/
    `ShowMessage`) but **not registered** (only `ShowText`+`Warp` are placeable). The `Outcome` hierarchy is CLOSED
    (4 cases); a new case would need BOTH `Outcome.cs` AND an `OutcomeApplier` arm AND an `ExpectationParser` arm.
  - **`Chest` + `GiveItem` are CLEANLY buildable contract-first — ZERO engine changes** (they compose the existing
    outcomes; items = `GameState` counters keyed `item.<id>`; the inspector is fully param-key-generic so they
    auto-render). The `ExpectationParser` already parses `AddCounter`/`SetSwitch`/`ShowMessage` — no parser arm.
  - **`Door`** needs `DoorRule`-from-`$data` (a door tile only syncs via a `DoorRule`; `GameSession.LoadMap` passes
    `Array.Empty<DoorRule>()`, so a placed door wouldn't open in the multi-map runtime) → an engine change → the
    immediate NEXT slice. **`Shop`** needs a buy/sell screen + inventory/currency UI → deferred.
  - `knowledge-context` surfaced D-0024 (the contract-first recipe — the load-bearing pattern) + the
    bootstrap-coverage rule (the new gated behaviours must be covered by gated tests, not just the oracle).
- **Ticket:** #25 — `c33b2570-1ba3-452b-baac-dca68a1854b6`.
- **AAR id (from `aar-open`):** `b4b5859b-ced4-4e2b-b460-71dc8a964510`.
- **EARS reviewed:** REQ-001 GiveItem grants + message · REQ-002 Chest give-once · REQ-003 registered + .expect +
  handler (D-0024) · REQ-004 bad params → typed Failure · REQ-005 the inspector renders the new ParamKeys
  (single-source) · REQ-006 FULL gate.
- **Design questions for Phase 2:** (1) the exact param sets — `GiveItem` `["item","amount","message"]`; `Chest`
  `["item","amount","switch","message"]` (+ a default empty message, or an `emptyMessage` param). (2) Build NEW
  parameterized `GiveItemEvent`/`ChestEvent` classes (the tracer `ChestEvent` is hardcoded — leave it as the #13
  oracle demo, or supersede it). (3) The counter-key convention (`item.<id>`). (4) The `.expect` lines (compose
  `AddCounter`/`SetSwitch`/`ShowMessage` — already parseable). (5) Confirm the registry factory int-parses `amount`
  (totality) like Warp's x/y.

## Phase 2 — Design
- **Approach / architecture:** Two new `IMapEvent` behaviours in `Engine/Sim/Events`, each composing the EXISTING
  outcome vocabulary (`AddCounter`/`SetSwitch`/`ShowMessage`) — **ZERO engine changes** (`Outcome`/`OutcomeApplier`/
  `ExpectationParser` untouched). Each is the D-0024 single-descriptor recipe: a `BehaviourRegistry` `Registration`
  (name + ParamKeys + a totality factory that int-parses `amount`) + an `.expect` (gate:13) + a `HandlerRegistry`
  entry. The Studio inspector auto-renders the new ParamKeys (param-key-generic — confirmed by Explore; NO Studio
  change). Items are `GameState` counters keyed `item.<id>`.
  - **`GiveItemEvent`** (kind `GiveItem`, params `[item, amount, message]`): `Run => [AddCounter("item."+id,
    amount), ShowMessage(msg)]` (grant-on-trigger). Mirrors `ShowTextEvent` (`Run(IEventContext) => [...]`).
  - **`ContainerEvent`** (kind `Chest`, params `[item, amount, switch, message]` — named `ContainerEvent` to avoid
    clashing with the #13 tracer `Sim/Tracer/ChestEvent`): give-once — `if (ctx.GetSwitch(openSwitch))` →
    `[ShowMessage("The chest is empty.")]` else `[AddCounter("item."+id, amount), SetSwitch(openSwitch, true),
    ShowMessage(msg)]`. Confirmed `IEventContext.GetSwitch(key)` exists (`EventContext` → `_state.Get`).
- **File manifest (6 + a test-file):**
  | # | File | Change |
  |---|---|---|
  | 1 | NEW `Engine/Sim/Events/GiveItemEvent.cs` | `IMapEvent`; Cell/Trigger; `Run => [AddCounter("item."+id, amount), ShowMessage(msg)]`; null-guard strings |
  | 2 | NEW `Engine/Sim/Events/ContainerEvent.cs` | `IMapEvent` (kind `Chest`); give-once via `GetSwitch`; the 3-outcome / empty-message `Run` |
  | 3 | MOD `Engine/Sim/BehaviourRegistry.cs` | `+ new("GiveItem", ["item","amount","message"], …)` + `new("Chest", ["item","amount","switch","message"], …)` — int-parse `amount`, missing/invalid → `Failure` |
  | 4 | NEW `Engine/Sim/Events/Expectations/GiveItem.expect` + `Chest.expect` | the oracle contracts (compose `AddCounter(item.potion,1)`/`SetSwitch`/`ShowMessage` — already parseable) |
  | 5 | MOD `Editor/Expectations/HandlerRegistry.cs` | `["GiveItem"]` + `["Chest"]` factories matching the `.expect` rows EXACTLY |
  | 6 | NEW `tests/MonoRpgMaker.Engine.Tests/BuiltinLibraryTests.cs` | the Run + registry + totality + ParamKeys matrix |
- ### Regression Test Plan
  | # | Test | Proves Requirement |
  |---|---|---|
  | T1 | `GiveItemEvent.Run` == `[AddCounter("item.potion", 2), ShowMessage("msg")]` (isolated: the `item.<id>` key + amount + message) | REQ-001 |
  | T2 | `ContainerEvent.Run` with the open-switch UNSET == the 3 outcomes (AddCounter + SetSwitch(true) + ShowMessage); with it SET == `[ShowMessage("The chest is empty.")]` (give-once) | REQ-002 |
  | T3 | `BehaviourRegistry.TryMaterialize` a `GiveItem` placement → a `GiveItemEvent`; a `Chest` placement → a `ContainerEvent` | REQ-003 |
  | T4 | each → `BehaviourResult.Failure` (isolated): GiveItem missing `item` / non-int `amount` / missing `message`; Chest missing `switch` | REQ-004 |
  | T5 | `MapPaintSession.AvailableKinds` (== `BehaviourRegistry.Kinds`) contains `GiveItem` `[item,amount,message]` + `Chest` `[item,amount,switch,message]` (the inspector single-source) | REQ-005 |
  | T6 | FULL `bin/gate.sh` green; gate:13 oracle now **9 rows / 6 modules** (GiveItem +1, Chest +2) | REQ-006 |
  - **No uncoverable/host paths** — both behaviours are gated `Engine.Sim` logic (not `[ExcludeFromCodeCoverage]`);
    fully unit-tested. `IEventContext` for `Run` tests = `new EventContext(state)` (state pre-set for the give-once
    SET case) — the #22 pattern.
- **Risks / decisions:** (1) the new chest class is **`ContainerEvent`** (kind `Chest`) to avoid a class-name clash
  with the tracer `Sim/Tracer/ChestEvent` (which stays as the #13 oracle demo). (2) Chest's empty message is
  **hardcoded** `"The chest is empty."` (keeps ParamKeys at 4; an `emptyMessage` param is a trivial follow-up). (3)
  the #22/#20 kind-set tests are **predicate-based** (`Assert.Single(Kinds, k => k.Name == "…")`) — they TOLERATE
  the 2 new kinds, so NO #22-test edits needed. (4) the dotted counter key `item.potion` parses (`TryArgs` splits
  on comma; the dot is fine — confirm in implement). (5) gate:13 grows 6→9 rows / 4→6 modules.

## Phase 3 — Implement
- **Built (zero engine changes — composed existing outcomes):**
  - `Engine/Sim/Events/GiveItemEvent.cs` (NEW) — `Run => [AddCounter("item."+id, amount), ShowMessage(msg)]`.
  - `Engine/Sim/Events/ContainerEvent.cs` (NEW, kind `Chest`) — give-once via `context.GetSwitch(openSwitch)`:
    empty-message when set, else `[AddCounter, SetSwitch(true), ShowMessage]`.
  - `Engine/Sim/BehaviourRegistry.cs` — `+ GiveItem ["item","amount","message"]` + `Chest ["item","amount",
    "switch","message"]` registrations (int-parse `amount`; missing/invalid → `BehaviourResult.Failure`).
  - `Engine/Sim/Events/Expectations/GiveItem.expect` + `Chest.expect` (NEW) — compose
    `AddCounter(item.potion,1)`/`SetSwitch`/`ShowMessage` (the dotted counter key parses fine).
  - `Editor/Expectations/HandlerRegistry.cs` — `["GiveItem"]` + `["Chest"]` matching the `.expect` rows.
  - `tests/.../BuiltinLibraryTests.cs` (NEW, 11) — the Run outcomes (both, incl. give-once), registry
    materialisation, totality (missing item / non-int amount / missing switch), the ParamKeys single-source.
- **Deviations from design (+ reason):** none material. `ContainerEvent` named as designed (avoids the tracer
  `ChestEvent` clash); the chest empty message is hardcoded `"The chest is empty."` (as scoped).
- **Build / verify:** whole solution `-warnaserror` 0/0; **GATE GREEN [fast]**; gate:13 oracle **9 rows / 6
  modules** (GiveItem +1, Chest +2 — handlers reproduce the contracts); 499 tests. The #22/#20 kind-set tests are
  predicate-based and **stayed green** (no edits needed). Both behaviours are gated sim logic (not excluded).
- **NOT yet (Phase 4):** the FULL gate (coverage + mutation — the GiveItem/Chest mutants must die via the 11 tests).

## Inspect (Phase 3.5)
- **Lenses run:** 1 independent correctness/contract critic — **survived** + verified CONCRETELY (traced
  handler↔`.expect` char-for-char via `ExpectationRunner.Check`; ran the oracle [9/6 OK], build [0/0],
  `--filter BuiltinLibrary` [11/11], `git diff`). **No BLOCKER/MAJOR/MINOR/NIT code defect.**
- **Findings:**
  | # | Severity | Finding | Verdict | Fix |
  |---|---|---|---|---|
  | 1 | watch-item | the `Chest` registration lacked a non-int-`amount` test, so the `int.TryParse→true` mutant could survive Phase-4 mutation (the `GiveItem` path + the conjunct-removals are covered — the latter are compile-errors, not survivors). | **REAL coverage gap (NOT a code defect)** — the critic explicitly excludes "missing tests". | **FIXED proactively** — added `Registry_Chest_NonIntAmount_Fails` (closes the gap; avoids a Phase-4 red→green loop). |
- **Cleared (verified, not just unflagged):** the behaviour outcomes (`GiveItem` `[AddCounter("item."+id,
  amount), ShowMessage]` in order; `Chest` give-once — 3 outcomes when the switch is unset incl. `SetSwitch(...,
  true)`, the empty message when set); the **handler↔`.expect` EXACT match** (traced char-for-char + oracle 9/6);
  registry **totality** (`Failure` not a throw; the conjunct-removals are compile-errors, not survivors); **D-0017
  / NO engine change** (`git diff` = only `HandlerRegistry` +2 + `BehaviourRegistry` +19; `Outcome.cs`/
  `OutcomeApplier.cs`/`ExpectationParser.cs` BYTE-unchanged); the dotted key `item.potion` is dot-safe (GameState
  counters are string-keyed); REQ-005 single-source (no exact-COUNT assertion — every kind-set test is
  predicate-based, tolerant of 2→4); MRM/§14 + `ContainerEvent` naming (no tracer clash) + determinism; the 3
  deviations sound.
- **Post-fix:** the added test passes (12/12 filter); no `.cs` source changed (a test addition) — build + GATE
  GREEN [fast] hold.
- **Capture:** none — no code defect (a clean contract-first slice; the lone item was a proactive test).
- **Phase-4 kill-list (covered by the 12 tests):** `GiveItem`/`Chest` Run outcomes (the `item.` prefix · the
  outcome order · the give-once gate · `SetSwitch` value true · the empty branch); the 2 registrations' totality
  (`int.TryParse` + the Success↔Failure ternary — `GiveItem` missing-item/non-int + `Chest` missing-switch/non-int);
  the `ParamKeys` single-source (GiveItem `[item,amount,message]` + Chest `[item,amount,switch,message]`).

## Phase 4 — Validate
- **Tests added (+12; 488 → 500):** `BuiltinLibraryTests.cs` (12) — `GiveItem` Run (grants the `item.<id>` counter
  + the message) · `Chest` give-once (first-open 3 outcomes incl. `SetSwitch` true / reopen empty) · registry
  materialises both · totality (`GiveItem` missing-item + non-int-amount; `Chest` missing-switch + non-int-amount —
  the last added in inspect) · the ParamKeys single-source for both. Isolated exact-value asserts.
- **`dotnet test MonoRpgMaker.slnx`: 500 passed, 0 failed.**
- **`bin/gate.sh`: GATE GREEN [full] — 13/13** (first try). Coverage **93.0%** (floor 80). Mutation: Engine
  **85.43%** · Abstractions 82.22% · Analyzers 91.18% · Editor **84.45%** (floor 80 — the 2 `HandlerRegistry`
  lambdas added a few mutants; still clears). Oracle **9 rows / 6 modules** (GiveItem + Chest). Receipt written.
- **Pre-existing exclusions:** none — both behaviours are gated `Engine.Sim` logic (fully unit-tested, not
  `[ExcludeFromCodeCoverage]`). Studio smoke (not blocking): the inspector's kind dropdown now offers `Chest` +
  `GiveItem` (it reads `BehaviourRegistry.Kinds`); placing one writes the `$data` the runtime materialises.

## Phase 5 — Complete
- **Docs updated:** `docs/roadmap.md` (the multi-map paragraph += the #25 built-in library — `Chest` + `GiveItem`
  placeable; door/shop remaining) + `docs/product-phasing.md` (the Event-behavior row — the library += Chest/
  GiveItem, items-as-counters). No `decisions.md` entry — Chest/GiveItem apply D-0024 (contract-first) + D-0017.
- **Forge capture:** `aar-submit` b4b5859b (completed, effectiveness 4). No failure-record / prevention-rule / AD —
  inspect found NO code defect (a clean contract-first slice; only a proactive Chest-non-int test added).
- **Ticket closed:** #25 (`c33b2570-…`) → done; `TICKET-0025-builtin-library.md` moved open/ → closed/.
- **Archived:** spec + notes moved active/ → completed/; spec status `Phase 5 — Complete PASS`.
- **Remaining built-ins (follow-ups):** **Door** — the immediate NEXT slice (needs `DoorRule`-from-`$data`:
  `GameSession.LoadMap` passes `Array.Empty<DoorRule>()`, so doors don't sync in the multi-map runtime — an engine
  change). **Shop** — a buy/sell screen + inventory/currency UI (a later, larger feature).
