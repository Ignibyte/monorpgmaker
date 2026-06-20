---
pipeline_id: 3c911ff4-564e-44fe-9f76-8d855d7528a0
title: WORK-save-load-host
ticket: 6ea08b5c-3302-4762-91c1-1c7be5b4f4a8
type: work
intake: none (direct /work request under a standing /goal — slice B of 3)
notes: WORK-save-load-host.notes.md
status: Phase 5 — Complete PASS
---

# WORK-save-load-host

> Pipeline spec (always-loaded contract). Detailed per-phase work lives in the
> paired `.notes.md`. Phase advance = updating the `status:` frontmatter above.

## Work Spec
- **Title:** In-game save/load host — persist + restore the game (state + active map + player position). Brings the
  #22 REQ-006 save end-to-end (it was serializer-only).
- **Scope:**
  - *In (gated)* — a `GameSession.TryRestore(SaveState save)` instance method: using the session's OWN map
    registry, switch the active map to `save.MapId`, build a fresh `WorldSim` with the `GameState` restored from
    `save.Switches`/`save.Counters` (via `GameState.Restore`) and the player placed at
    `(save.PlayerX, save.PlayerY)`, swap `Active`/`ActiveMapId` IN PLACE (like `ApplyPendingWarp`), return `true`.
    An unknown saved map id → `false` + the active map unchanged (a safe no-op, never a throw, no corruption).
    Reuses `LoadMap` + the injectable `GameState` (D-0026). The SAVE side reads the already-public
    `Active.State` / `Active.Player.Cell` / `ActiveMapId` (no new gated code).
  - *In (host — Player, `[ExcludeFromCodeCoverage]`, manual smoke)* — `RpgGame` detects a SAVE key + a LOAD key in
    `Update` → `protected virtual OnSaveRequested()` / `OnLoadRequested()` hooks (no-op defaults) the host `Game1`
    overrides; `Game1` does the FILE IO (a user-writable local-app-data path) + `SaveSerializer.Serialize/
    Deserialize` + `Session.TryRestore`. `RpgGame` exposes `protected GameSession Session` so the host reads/
    restores the live session.
  - *Out* — a save-slot UI / menu (one key, one file); autosave; populating `rngState` (no `IRandom` in the sim
    yet → saved as `0`; the format carries it, combat populates it later); restoring player FACING (the `Actor`
    ctor takes no facing + `Facing` is private-set — position + state + map are restored; facing is a follow-up);
    cloud/console save backends.
- **Systems:** runtime/player (`RpgGame`/`Game1` host) · save/load (`SaveSerializer`/`GameState`/`GameSession`) ·
  sim (`GameSession`/`WorldSim`) · input (the save/load keys).

## Acceptance Criteria (EARS)

| ID | EARS Requirement | Verification |
|---|---|---|
| REQ-001 | When given a `SaveState`, `GameSession.TryRestore` shall rebuild the session in place at the saved map id with the restored switches/counters and the player at the saved cell, returning `true`. | unit |
| REQ-002 | When the `SaveState` names a map not in the session's registry, `TryRestore` shall return `false` and leave the active map + state unchanged (a safe no-op, never a throw / no corruption). | unit |
| REQ-003 | A save→load round-trip (`SaveSerializer.Serialize` → `Deserialize` → `TryRestore`) shall restore the active map id, the `GameState` (switches + counters), and the player position. | unit (end-to-end at the sim layer) |
| REQ-004 | The Player shall save the game to a user-writable file on a key and load it back on a key, restoring the session. | manual smoke |
| REQ-005 | The FULL `bin/gate.sh` shall be green (coverage + mutation + the `.expect` oracle). | gate |

## Locked-In Decisions
- **`TryRestore` is IN-PLACE** (mutates `Active` + `ActiveMapId`, like `ApplyPendingWarp`) so `RpgGame`'s readonly
  `_session` reference stays valid — the host just calls `Session.TryRestore(save)`. Reuses `LoadMap` +
  `GameState.Restore` + the injectable `GameState` (D-0026).
- **Totality:** an unknown saved map id → `false` no-op (the prior active map stays); `SaveSerializer.Deserialize`
  already rejects malformed/old saves upstream, so `TryRestore` receives a structurally-valid `SaveState`.
- **Saves are USER DATA** (not embedded content) → a user-writable local-app-data path; **D-0023 governs CONTENT,
  not saves**. The engine never touches the filesystem (`TryRestore` takes a `SaveState`; `SaveSerializer` is pure
  strings) — the host (`Game1`) owns the file IO (D-0022).
- **`rngState` saved as `0`** (no `IRandom` in the sim yet; the save FORMAT already carries it — combat populates
  it later). **Facing not restored** (the `Actor` ctor takes no facing) — position + state + map are restored.

## Linked Artifacts
- Design docs: `docs/decisions.md` (D-0022, D-0023, D-0026), `docs/roadmap.md`.
- Intake doc: none (direct `/work` under a standing `/goal`).
- Ticket doc: `docs/planning/tickets/open/TICKET-0024-save-load-host.md`
- Forge ticket: 6ea08b5c-3302-4762-91c1-1c7be5b4f4a8 (#24)

## Phase Plan
| Phase | Status |
|---|---|
| 1 — Plan | PASS (autonomous — standing /goal) |
| 2 — Design | PASS (GameSession.TryRestore in-place + RpgGame Session/F5-F9 hooks + Game1 file IO) |
| 3 — Implement | PASS (GameSession.TryRestore + RpgGame F5/F9 hooks + Game1 save.json IO; build 0/0; GATE GREEN [fast]) |
| 3.5 — Inspect | PASS (1 critic; MAJOR catch-narrowness fixed + doc + multi-entry test; no-corruption verified) |
| 4 — Validate | PASS (488 tests; cov 93.1%; MSI Engine 86.05/Abs 82.22/Analyzers 91.18/Editor 85.45; GATE GREEN [full]) |
| 5 — Complete | PASS (docs touched; AAR closed; ticket #24 done; archived) |
