# WORK-save-load-host — Notes

Per-phase working notes for the paired `.spec.md`. The spec is the contract;
this is the record of what happened.

## Phase 1 — Plan
- **Request:** In-game save/load host — the #22 REQ-006 deferred half. Wire SAVE (write the game to a user file)
  + LOAD (restore the session) end-to-end: a gated `GameSession.TryRestore(SaveState)` + the Player host keys/IO.
  Slice B of 3 under a standing `/goal` (A re-skin ✓ → B save/load → C built-in library).
- **Intake source:** none (direct `/work`).
- **Classification / tier:** work pipeline, MEDIUM slice (no split). P2 runtime/save-load.
- **Forge recall (lessons/failures surfaced):** `knowledge-context` (Plan) surfaced (opaque ids) the expected
  recent decisions — `AD-claude-multimap-model-001` (#22 GameSession + injectable state), D-0022 (neutral host),
  D-0025; prevention rules incl. **`PR-claude-bootstrap-methods-need-gated-coverage-001`** (RELEVANT — `TryRestore`
  is gated logic, must be covered by a gated test, not just called by the excluded host) + the
  detach-before-resubscribe rule; recent failures = the #22 subscription leak + the #21 undo. Nothing blocking.
- **Ticket:** #24 — `6ea08b5c-3302-4762-91c1-1c7be5b4f4a8`.
- **AAR id (from `aar-open`):** `582c60ee-d2cb-48ee-b457-d100bcf39f39`.
- **Surface confirmed (for design):** `GameState.Restore(IReadOnlyList<KeyValuePair<string,bool>> switches,
  IReadOnlyList<KeyValuePair<string,int>> counters)` → `GameState`; `SaveState` carries `Switches` (`SwitchEntry[]`
  Key+bool), `Counters` (`CounterEntry[]` Key+int), `PlayerX/Y`, `Facing`, `RngState`, `MapId`;
  `GameSession.Active`/`ActiveMapId` are `private set` (so an in-place `TryRestore` can mutate them) + `LoadMap`
  (private static) is reusable; `Actor` ctor is `(string name, Point cell, int maxHp)` — **no facing param**
  (facing-restore deferred).
- **EARS reviewed:** REQ-001 TryRestore rebuilds at the saved map + position · REQ-002 unknown map → false/no-op ·
  REQ-003 save→load round-trip restores state+map+position · REQ-004 the Player saves/loads on keys (smoke) ·
  REQ-005 FULL gate.
- **Design questions for Phase 2:** (1) `TryRestore` exact shape (in-place; map `save.Switches`/`Counters` →
  `KeyValuePair` lists via a loop — MRM-clean, no LINQ in sim — then `GameState.Restore` + `LoadMap` + set
  `Active`/`ActiveMapId`). (2) The host seam — `RpgGame` exposes `protected GameSession Session` + `Update` F5/F9
  → `OnSaveRequested`/`OnLoadRequested` hooks; `Game1` overrides (file IO + Serialize/Deserialize/TryRestore).
  (3) The save file path (a local-app-data dir + a filename). (4) Confirm `rngState` 0 + facing-deferred are fine.

## Phase 2 — Design
- **Approach / architecture:** A gated **`GameSession.TryRestore(SaveState save)`** (instance, IN-PLACE): map
  `save.Switches`/`save.Counters` → `KeyValuePair<…>[]` via FOR loops (MRM-clean), `GameState.Restore` them,
  `LoadMap(_maps, save.MapId, restored, new GridPoint(save.PlayerX, save.PlayerY))`; on `!Ok` return `false`
  WITHOUT mutating (the active map/state stay — no corruption); else set `Active`/`ActiveMapId`, return `true`.
  Reuses the existing private `LoadMap` + the injectable `GameState` (D-0026). The host (`[ExcludeFromCodeCoverage]`)
  drives it: `RpgGame` exposes `protected GameSession Session` + detects F5/F9 in the input handler →
  `OnSaveRequested`/`OnLoadRequested` (no-op virtuals); `Game1` overrides them — save: `SaveSerializer.Serialize(
  Session.Active.State, Session.Active.Player.Cell, Session.Active.Player.Facing, 0UL, Session.ActiveMapId)` → a
  user file; load: read → `SaveSerializer.Deserialize` → `Session.TryRestore`. The engine never touches the
  filesystem (D-0022); saves are user data at a local-app-data path (D-0023 governs CONTENT, not saves).
- **File manifest (4):**
  | # | File | Change |
  |---|---|---|
  | 1 | MOD `src/MonoRpgMaker.Engine/Sim/GameSession.cs` | `+ public bool TryRestore(SaveState save)` (null-guard; KVP map via loops; `GameState.Restore`; `LoadMap`; success-gated `Active`/`ActiveMapId` set) |
  | 2 | MOD `src/MonoRpgMaker.Engine/Core/RpgGame.cs` | `+ protected GameSession Session => _session;` `+ protected virtual void OnSaveRequested()`/`OnLoadRequested()` (no-op); F5/F9 in the input handler (before `_previous = keyboard`) → the hooks |
  | 3 | MOD `src/MonoRpgMaker.Player/Game1.cs` | override `OnSaveRequested`/`OnLoadRequested` — file IO at `LocalApplicationData/monorpgmaker/save.json` (create dir; catch `IOException`) + `SaveSerializer` + `Session.TryRestore`; `[ExcludeFromCodeCoverage]` |
  | 4 | NEW `tests/MonoRpgMaker.Engine.Tests/SaveLoadRestoreTests.cs` | the `TryRestore` matrix |
- ### Regression Test Plan
  | # | Test | Proves Requirement |
  |---|---|---|
  | T1 | TryRestore a `SaveState{MapId="town", PlayerX/Y, Switches=[door=true], Counters=[gold=7]}` on a 2-map session → true; `ActiveMapId=="town"`; `Active.Player.Cell==(x,y)`; `Active.State.Get("door")`; `GetCount("gold")==7` (isolated asserts) | REQ-001 |
  | T2 | TryRestore `SaveState{MapId="ghost"}` → false; `ActiveMapId` still "start"; a switch set pre-restore SURVIVES (no-corruption — the prior `Active`/state is untouched) | REQ-002 |
  | T3 | round-trip: a session warped to "town" + state → `SaveSerializer.Serialize(Active.State, cell, facing, 0, ActiveMapId)` → `Deserialize` → a FRESH 2-map session `.TryRestore(save)` → restores map+position+state | REQ-003 |
  | T4 | `TryRestore(null!)` → `ArgumentNullException` | REQ-001 guard |
  | T5 | FULL `bin/gate.sh` green (coverage + mutation — the `TryRestore` mutants die via T1–T4; the bootstrap-coverage lesson: TryRestore is GATED-tested, not just host-called) | REQ-005 |
  - **Uncoverable (host, `[ExcludeFromCodeCoverage]`, manual smoke):** `RpgGame` F5/F9 + the hooks + `Game1`'s file
    IO — exercised by running the Player (F5 writes save.json; F9 restores), not gated.
- **Risks / decisions:** (1) **No-corruption on a failed restore** — `TryRestore` checks `loaded.Ok` BEFORE
  mutating `Active`/`ActiveMapId`, so an unknown map id leaves the session fully intact (T2 pins it). (2) **Empty/
  unknown `MapId`** → `false` (no real save writes empty — the host always saves a real `ActiveMapId`; a
  hypothetical pre-multimap empty save just won't restore). (3) **Facing not restored** (the `Actor` ctor takes no
  facing) — position + state + map are; facing is a follow-up. (4) **`rngState` 0** (no `IRandom` yet; the format
  carries it for combat). (5) The save FILE IO is host/excluded — gated tests never touch the filesystem.

## Phase 3 — Implement
- **Built:**
  - `Engine/Sim/GameSession.cs` — `+ public bool TryRestore(SaveState save)`: null-guard; `save.Switches`/
    `Counters` → `KeyValuePair<…>[]` via FOR loops (MRM-clean); `GameState.Restore`; `LoadMap(_maps, save.MapId,
    restored, (PlayerX,PlayerY))`; on `!Ok` return false WITHOUT mutating (no corruption); else set `Active`/
    `ActiveMapId`. Mirrors `ApplyPendingWarp`.
  - `Engine/Core/RpgGame.cs` — `+ protected GameSession Session => _session;` + `protected virtual
    OnSaveRequested()`/`OnLoadRequested()` (no-op); F5/F9 in `HandleMovement` (after the action check, before
    `_previous` is captured) → the hooks.
  - `Player/Game1.cs` — override `OnSaveRequested` (`SaveSerializer.Serialize(Session.Active.State, Player.Cell,
    Player.Facing, 0UL, Session.ActiveMapId)` → `LocalApplicationData/monorpgmaker/save.json`; `Directory.Create` +
    `try/catch IOException`) + `OnLoadRequested` (read if exists → `Deserialize` → `Session.TryRestore`) +
    `SavePath()`.
  - `tests/.../SaveLoadRestoreTests.cs` (NEW, 9) — switch-to-map / place-player / restore-switch / restore-counter
    (REQ-001) · unknown-map false / active-unchanged / no-corruption (REQ-002) · round-trip restores map + state
    (REQ-003) · null-guard.
- **Deviations from design (+ reason):** none material. Facing + `rngState` deferred exactly as scoped (the
  `Actor` ctor takes no facing; no `IRandom` in the sim → `rngState` 0).
- **Build / verify:** whole solution `-warnaserror` 0/0; **GATE GREEN [fast]** (source-bans clean on the host File
  IO; oracle 6×4 unchanged). The host save/load (`RpgGame` F5/F9 + `Game1` file IO) is `[ExcludeFromCodeCoverage]`
  — manual smoke (F5 writes `save.json`, F9 restores).
- **NOT yet (Phase 4):** the FULL gate (coverage + mutation — the `TryRestore` mutants must die via the 9 tests).

## Inspect (Phase 3.5)
- **Lenses run:** 1 independent correctness/data-integrity critic — **survived** + verified CONCRETELY (2 throwaway
  repros: 3-entry key/value fidelity + a known-but-corrupt-map no-mutation; verified the BCL exception hierarchy;
  ran `--filter SaveLoadRestore`). Caught a real host robustness bug.
- **Findings:**
  | # | Severity | Finding | Verdict | Fix |
  |---|---|---|---|---|
  | 1 | MAJOR | `Game1`'s `catch (IOException)` is too narrow — `UnauthorizedAccessException` (an ACL/read-only/sandboxed save dir, the most common file-write failure) does NOT derive from `IOException`, so it escapes + crashes the host from `Update()`, defeating the "a bad disk can't crash the game" intent. | **REAL** | **FIXED** — added `catch (UnauthorizedAccessException)` to both `OnSaveRequested`/`OnLoadRequested`. `BF-host-save-catch-too-narrow-001` / `PR-claude-file-io-catch-unauthorized-not-just-io-001`. |
  | 2 | MINOR | `SaveState.MapId`'s doc claimed "empty = the start map; back-compat" but `TryRestore` returns `false` on an empty/unknown id (no real save writes empty — the host always writes `ActiveMapId`). | **REAL (doc/behaviour mismatch)** | **FIXED** — corrected the doc to match (`TryRestore` returns false, leaving the session unchanged). |
  | 3 | MINOR | single-entry tests let the loop-bound + value-constant `TryRestore` mutants survive. | **REAL (coverage gap)** | **FIXED** — added `TryRestore_RestoresMultipleEntries` (3 switches incl. a `false` + 3 counters, asymmetric values). |
- **Cleared (verified, not just unflagged):** the **no-corruption ordering** (the `if (!loaded.Ok) return false;`
  is strictly BEFORE the `Active`/`ActiveMapId` assignments; `LoadMap` fully computes — incl. `WorldSim.TryCreate`
  — before returning; `restored` is built fresh via `GameState.Restore` and never reads/writes the live
  `Active.State` — a known-but-corrupt-map repro confirmed `ActiveMapId` stayed "start" + a pre-set flag survived);
  key/value fidelity (3-entry repro, no swap/off-by-one); the null-guard; determinism (no clock/RNG); D-0022/D-0023
  (TryRestore touches no `System.IO`/`Environment` — only `Game1` does, to a user path; `RpgGame` only signals
  F5/F9); MRM-clean (for-loops, no LINQ/float/Random); the deviations (facing/rngState/empty-MapId) sound.
- **Post-fix:** build + **GATE GREEN [fast]** (the broadened catch + the doc + the multi-entry test).
- **Capture:** `failure-record` **BF-host-save-catch-too-narrow-001** (`88498072`); `prevention-rule-record`
  **PR-claude-file-io-catch-unauthorized-not-just-io-001** (`cf654002`).
- **Phase-4 kill-list (TryRestore — now all covered by the 10 tests):** the success-gate (`!loaded.Ok`) →
  unknown-map false / active-unchanged / no-corruption; the `Active`/`ActiveMapId` assignment + ordering; the KVP
  key/value + **loop bounds** (the new multi-entry test); the `GridPoint(X,Y)` (asymmetric (3,4)); the null-guard.

## Phase 4 — Validate
- **Tests added (+11; 477 → 488):** `SaveLoadRestoreTests.cs` (11) — switch-to-map / place-player / restore-switch
  / restore-counter / **multi-entry** (3 switches incl. a `false` + 3 counters — kills the loop-bound +
  value-constant mutants) (REQ-001) · unknown-map false / active-unchanged / no-corruption (REQ-002) · round-trip
  restores map + state (REQ-003) · null-guard. Isolated exact-value asserts.
- **`dotnet test MonoRpgMaker.slnx`: 488 passed, 0 failed.**
- **`bin/gate.sh`: GATE GREEN [full] — 13/13** (first try). Coverage **93.1%** (floor 80). Mutation: Engine
  **86.05%** (↑ from 85.68 — the `TryRestore` mutants all died) · Abstractions 82.22% · Analyzers 91.18% · Editor
  85.45% (floor 80). Oracle 6 rows / 4 modules. Receipt written.
- **Pre-existing exclusions:** the host save/load (`RpgGame` F5/F9 + `Game1`'s file IO incl. the new
  `UnauthorizedAccessException` catch) is `[ExcludeFromCodeCoverage]` — manual smoke (F5 writes
  `LocalApplicationData/monorpgmaker/save.json`, F9 restores).

## Phase 5 — Complete
- **Docs updated:** `docs/roadmap.md` (the multi-map paragraph += the #24 in-game save/load — F5 saves, F9 restores
  via `GameSession.TryRestore`; REQ-006 closed) + `docs/product-phasing.md` (the Switches+variables row — now
  persisted via save/load). No `decisions.md` entry — the save/load host applies D-0016 (#15 save spine) + D-0026 +
  D-0022.
- **Forge capture:** `aar-submit` 582c60ee (completed, effectiveness 4; 2 novel findings, 13 verdicts). Inspect
  filed `failure-record` **BF-host-save-catch-too-narrow-001** (`88498072`) + `prevention-rule`
  **PR-claude-file-io-catch-unauthorized-not-just-io-001** (`cf654002`). No AD.
- **Ticket closed:** #24 (`6ea08b5c-…`) → done; `TICKET-0024-save-load-host.md` moved open/ → closed/.
- **Archived:** spec + notes moved active/ → completed/; spec status `Phase 5 — Complete PASS`.
- **Follow-ups (open — the remaining 1 of 3 under the standing /goal):** C) the rest of the built-in library
  (chest / door / shop / give-item).
