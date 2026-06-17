# WORK-m0-tracer-bullet-v1 — Notes

Per-phase working notes for the paired `.spec.md`. The spec is the contract;
this is the record of what happened.

## Phase 1 — Plan
- Request: M0 tracer bullet (D-0015 / roadmap P-(-1)) — hand-wired painted map + 4-dir move/collide +
  ONE authored event (lever → message-once → set `door_open` switch → open door), plain C# on the
  Engine scaffold, deliberately pre-chassis, relaxed prototype gate, determinism via injected
  `IRandom`. Tile art: placeholder for now.
- Intake source: none.
- Classification / tier: **spike** (forge ticket type) · work-tier pipeline · one shippable slice.
- Forge recall (lessons/failures surfaced): `knowledge-search` surfaced **D-0015** (tracer-first),
  **D-0016** (determinism-by-construction), **D-0017** (scaffolding) + **PR-claude-tracer-001 /
  -determinism-001 / -scaffolding-001 / -verification-001**. `docs-search` confirmed roadmap P-(-1) +
  D-0015. Codegraph live: `RpgGame` host at `src/MonoRpgMaker.Engine/Core/RpgGame.cs:21` over the
  `World` / `Entities` / `Data` seed. No bulletins. M0 is deliberately pre-chassis, so the
  determinism/scaffolding prevention rules apply to M1, not this spike (recorded for Phase 2).
- Ticket: b1454ae5-81fa-4933-813e-95eaf873a8f9 (#1).
- AAR id (from `aar-open`): 51172405-c52c-4894-b39b-fac1b02815fc.
- EARS requirements reviewed: REQ-001..006 (collision · lever→message-once→switch · door passability ·
  render+input smoke · determinism bans · prototype gate).

## Phase 2 — Design

### Approach / architecture
A new **Graphics-free simulation layer** `MonoRpgMaker.Engine.Sim` carries all M0 game logic (so it's
unit-testable and arch-test-guarded against `Graphics`, like `World`/`Entities`/`Data`); the host
`RpgGame` (Core, already `[ExcludeFromCodeCoverage]`) renders + drives it; the `Player` picks the scene.

- **`GameState`** — a switch store (`bool Get(string)` / `void Set(string,bool)` over a
  `Dictionary<string,bool>`, default false). The embryonic `$game`.
- **`IMapEvent`** (`Point Cell`, `EventTrigger Trigger`, `void Run(EventContext)`) + **`EventTrigger`**
  (`StepOn`) + **`EventContext`** (`GameState State`, `void ShowMessage(string)`). `EventContext` is the
  tiny embryonic semantic surface the future `EventContext` seam grows from — the event sets a flag and
  narrates; it does NOT poke tiles.
- **`DoorRule`** (`Point Cell`, `string Switch`, `Tile ClosedTile`, `Tile OpenTile`) — the door is a
  *derived view* of a switch.
- **`WorldSim`** — owns `Map` + `Player` (Actor) + `GameState` + `IReadOnlyList<IMapEvent>` + door rules;
  `bool MovePlayer(Direction)` = clear `CurrentMessage` → `Player.TryStep` → on a successful step fire
  `StepOn` events at the new cell → `SyncDoors()`; `SyncDoors` applies each `DoorRule` via `Map.SetTile`
  (keeps `TileMap` pure). Exposes `string? CurrentMessage` for the host to render.
- **`Sim.Tracer.LeverEvent`** (the hand-authored exemplar) — `Run`: `if (!State.Get("door_open")) {
  ctx.ShowMessage("…"); ctx.State.Set("door_open", true); }` → message + switch happen exactly once.
- **`Sim.Tracer.TracerRoom.Build()`** — paints the room (border walls, floor, a lever tile, a closed
  door), places the Player, registers the `LeverEvent` + the `DoorRule`, returns the `WorldSim`. (M0
  authored content lives in Engine so it's testable; **M1 relocates it to the Project** per the D-0015
  extraction.)
- **Host (`RpgGame`)** — ctor takes a `WorldSim`; `Update` edge-detects arrow keys (one tile per press)
  → `Sim.MovePlayer`; `Draw` paints tiles (a 1×1 pixel texture tinted per `TilesetId`), the player quad,
  and `CurrentMessage` (best-effort `SpriteFont`). `Player.Game1 : base(TracerRoom.Build())`.
- §14: typed/observable returns (`MovePlayer`→bool), nullable-clean (`CurrentMessage`), no hidden
  statics, sim is deterministic (no `Random`/`DateTime` — REQ-005), sim/render separation enforced by an
  extended arch test.

### File manifest
| # | File | Change |
|---|---|---|
| 1 | `src/MonoRpgMaker.Engine/Sim/GameState.cs` | **add** — switch store |
| 2 | `src/MonoRpgMaker.Engine/Sim/EventTrigger.cs` | **add** — enum `{ StepOn }` |
| 3 | `src/MonoRpgMaker.Engine/Sim/IMapEvent.cs` | **add** — event contract |
| 4 | `src/MonoRpgMaker.Engine/Sim/EventContext.cs` | **add** — `State` + `ShowMessage` surface |
| 5 | `src/MonoRpgMaker.Engine/Sim/DoorRule.cs` | **add** — switch→door record |
| 6 | `src/MonoRpgMaker.Engine/Sim/WorldSim.cs` | **add** — sim: `MovePlayer`/`SyncDoors`/`CurrentMessage` |
| 7 | `src/MonoRpgMaker.Engine/Sim/Tracer/LeverEvent.cs` | **add** — the hand-authored event exemplar |
| 8 | `src/MonoRpgMaker.Engine/Sim/Tracer/TracerRoom.cs` | **add** — paints the room + wires the slice |
| 9 | `src/MonoRpgMaker.Engine/Core/RpgGame.cs` | **modify** — host an injected `WorldSim`; edge-detected input; render tiles/player/message |
| 10 | `src/MonoRpgMaker.Player/Game1.cs` | **modify** — `: base(TracerRoom.Build())` |
| 11 | `src/MonoRpgMaker.Player/Content/Content.mgcb` + `Content/Fonts/Default.spritefont` | **add** — best-effort message font |
| 12 | `tests/MonoRpgMaker.Engine.Tests/ArchitectureTests.cs` | **modify** — add `Engine.Sim` to the no-`Graphics` rule |
| 13 | `tests/MonoRpgMaker.Engine.Tests/TracerSliceTests.cs` | **add** — the regression tests below |

### Regression Test Plan
| # | Test | Proves Requirement |
|---|---|---|
| T1 | `WorldSim`: stepping into a border wall → `MovePlayer` false, `Player.Cell` unchanged | REQ-001 |
| T2 | `WorldSim`: stepping into open floor → `MovePlayer` true, `Player.Cell` advances one tile | REQ-001 |
| T3 | Lever: first `StepOn` → `CurrentMessage` == the lever text AND `State.Get("door_open")` true | REQ-002 |
| T4 | Lever: step on → off → on again → on the 2nd entry `CurrentMessage` is null (guard fires once) | REQ-002 |
| T5 | Door: `Map.IsBlocked(door)` true initially; after the lever fires (switch + `SyncDoors`) it is false | REQ-003 |
| T6 | Door: a second `SyncDoors` keeps the door open (idempotent) | REQ-003 |
| T7 | `WorldSim`: a move that fires no event clears the previous `CurrentMessage` | REQ-002 (lifecycle) |
| T8 | `WorldSim`: a `StepOn` event fires only when the player enters its exact cell | REQ-002 (dispatch) |
| — | **Smoke** (manual run of `MonoRpgMaker.Player`): map + player render; arrows move one tile/press; walking onto the lever shows the message and opens the door (passable + color change) | REQ-004 |
| — | **Source-bans gate + review**: no `System.Random`/`DateTime` in `Engine.Sim` | REQ-005 |
| — | **Prototype gate**: `dotnet format --verify-no-changes` + `dotnet build -warnaserror` + source-bans green | REQ-006 |

Uncoverable: `RpgGame`'s live render/input loop (REQ-004) — no GPU/window in CI; it stays
`[ExcludeFromCodeCoverage]` (host/composition root). All `WorldSim`/event/door logic is covered (T1–T8).

### Risks / decisions
- **D1** New sim + the M0 authored content both live in `Engine.Sim` so they're unit-testable; the
  Engine/Abstractions/Project split + relocation of authored content is an explicit **M1** step (D-0015
  extraction). Reversible; documented.
- **D2** The door is a switch-derived view applied by `WorldSim.SyncDoors` (`SetTile` swap) — keeps
  `TileMap` pure and the event pure (set-flag-and-narrate, not tile-poking). The cleaner "set switch →
  system reacts" shape.
- **D3** `IRandom` deferred to **M1**: M0 has no randomness, and injecting an unused `IRandom` would be
  dead code / an `-warnaserror` unused-field break. REQ-005 is met by the *absence* of
  `System.Random`/`DateTime` + the source-bans gate; the determinism *seam* lands in M1 (D-0016).
- **D4** Message text via a best-effort `SpriteFont`; if the macOS `mgcb` font build is troublesome, M0
  falls back to a colored banner + `Debug` output (the message *logic* is proven by T3/T4 regardless).
- **D5** Input edge-detected (one tile per press) for the spike's *feel* — the scaffold currently moves
  every frame a key is held.
- **D6** `RpgGame` refactored to host an injected `WorldSim` (generic host; the Player supplies the
  scene), replacing the bare `Map`/`Player` properties.

## Phase 3 — Implement
- Built:
  - `Engine.Sim` (Graphics-free): `GameState`, `EventTrigger`, `IMapEvent`, `EventContext`, `DoorRule`,
    `WorldSim` (`MovePlayer` clears message → `TryStep` → fire StepOn events → `SyncDoors`; `CurrentMessage`).
  - `Engine.Sim.Tracer`: `LeverEvent` (guarded once: message + set `door_open`), `TracerRoom.Build`
    (15×9 walled room, interior wall at x=7 with a door at (7,4), lever at (4,4), player at (2,4),
    wired `WorldSim` with the lever event + the door rule).
  - `Core.RpgGame` refactored to host an injected `WorldSim`: edge-detected arrow input → `MovePlayer`;
    `Draw` renders tiles (1×1 pixel-texture quads colored per `TilesetId`), the player quad, and a
    message banner. `Player.Game1 : base(TracerRoom.Build())`.
  - **Verified:** `dotnet build` Engine + Player `-warnaserror` → 0/0; `dotnet format --verify-no-changes` → clean.
- Deviations from design (+ reason):
  - **D4 realized (message rendering):** message text shows via the banner + `Window.Title` +
    `Debug.WriteLine` — NOT a content-pipeline `SpriteFont`. Keeps the prototype build green and avoids
    macOS `mgcb` font friction (manifest item 11 dropped). The message *logic* is unchanged and is proven
    by T3/T4.
  - **Tests deferred to Phase 4** per the pipeline (implement = application code only): `TracerSliceTests`
    (T1–T8) and the `ArchitectureTests` `Engine.Sim` no-`Graphics` extension are written + run in validate.
    The new sim code is already Graphics-free (clean Engine build, no `Graphics` imports).
  - **`IRandom` not introduced** (D3): M0 has no randomness; injecting an unused seam would be dead code
    under `-warnaserror`. The determinism seam lands in M1.

## Inspect (Phase 3.5)
- Lenses run: 3 parallel general-purpose critics — (1) correctness + determinism + AC, (2) §14
  conventions + sim/render separation, (3) simplification / reuse / allocations. The correctness critic
  traced REQ-001/002/003/005 (incl. the ON→OFF→ON message-once path and the same-call door open) and
  found **no correctness bugs**; determinism + Graphics-free separation re-confirmed by grep.
- Findings:
  | # | Severity | Finding (file:line) | Verdict | Fix |
  |---|---|---|---|---|
  | 1 | high | `WorldSim` ctor doesn't guard reference args (null `map` NREs in `SyncDoors`) — Sim/WorldSim.cs:21 | REAL | Added `ArgumentNullException.ThrowIfNull` for map/player/events/doors (matches the `TileMap` guard idiom) |
  | 2 | high | `EventContext` ctor doesn't guard `state`/`showMessage` — Sim/EventContext.cs:15 | REAL | Added `ThrowIfNull` for both |
  | 3 | medium | `GameState.Get`/`Set` don't guard `key`; `Get(null)` silently returns false, masking caller bugs — Sim/GameState.cs | REAL | Added `ThrowIfNull(key)` to both |
  | 4 | low | `FireStepOn` allocates an `EventContext`+closure on every successful step — Sim/WorldSim.cs | REAL | Hoisted `_eventContext` to a `readonly` field built once in the ctor (State is immutable; the closure captures only `this`) — zero per-step alloc |
  | 5 | low | Misleading "door gap" comment + fragile reliance on ctor `SyncDoors` to overwrite a `Wall`-painted door cell — Sim/Tracer/TracerRoom.cs:46 | REAL | Wall loop now skips the door row (a genuine gap); the `DoorRule` solely owns the door cell; comment corrected |
  | 6 | low | `LeverEvent` expression-bodied ctor "inconsistent with repo" | REJECTED | The repo uses expression-bodied ctors (`MapEditor.cs:13`) — it is consistent |
  | 7 | low | Message banner persists across non-move frames (UX) | REJECTED | Acceptable RPG-Maker-style behavior; host is `[ExcludeFromCodeCoverage]`, out of test scope |
- Re-verified after fixes: Engine + Player build `-warnaserror` → 0/0; `dotnet format --verify-no-changes` → clean.
- Forge capture: `PR-claude-guards-001` (guard public preconditions). No `failure-record` — nothing shipped; all findings caught + fixed in-phase.

## Phase 4 — Validate
- Tests added: `tests/MonoRpgMaker.Engine.Tests/TracerSliceTests.cs` — T1/T2 collision (REQ-001),
  T3 lever message+switch / T4 message-once / T7 message lifecycle / T8 dispatch precision (REQ-002),
  T5 door passable-after-switch / T6 idempotent `SyncDoors` (REQ-003); plus extended
  `ArchitectureTests` to add `MonoRpgMaker.Engine.Sim` to the no-`Graphics` rule (REQ — sim/render
  separation).
- `dotnet test MonoRpgMaker.slnx`: **Passed! — 0 failed, 25 passed, 0 skipped (75 ms).**
- `bin/gate.sh --fast`: **GATE GREEN [fast]** — 10/10 static gates pass (1 format · 2 build
  `-warnaserror` · 3 test · 4 vuln-deps · 5 licenses · 6 gitleaks · 7 shellcheck · 8 no-suppressions ·
  9 source-bans · 10 doc-todos). REQ-005 (no `System.Random`/`DateTime`) ✓ via source-bans; REQ-006 ✓
  (the prototype gate itself).
- Gate level (D-0015): M0 uses the **relaxed prototype gate** (`--fast`). Coverage (gate:11) + Stryker
  mutation (gate:12) are **WAIVED** for this pre-chassis spike and return at M1. The FULL gate (which
  writes the `/commit` receipt) has NOT run — flagged for `/commit`: the sim is well-covered and the
  host is `[ExcludeFromCodeCoverage]`, so coverage should clear; whether to run the full gate for M0 or
  treat it as a spike is a `/commit`-phase decision.
- REQ-004 (live render/input) is a manual smoke — run `dotnet run --project src/MonoRpgMaker.Player`
  (deferred to Phase 5 / the timing spike).
- Pre-existing exclusions: none — all 25 tests pass. The in-flight tooling changes
  (csproj/packages.lock/BannedSymbols) are consistent with the gate (it ran green over them).

## Phase 5 — Complete
- Docs updated:
- Forge capture (aar/failures/rules/decisions):
- Ticket closed:
- Archived:
