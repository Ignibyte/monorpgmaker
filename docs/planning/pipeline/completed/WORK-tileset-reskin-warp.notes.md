# WORK-tileset-reskin-warp — Notes

Per-phase working notes for the paired `.spec.md`. The spec is the contract;
this is the record of what happened.

## Phase 1 — Plan
- **Request:** Cross-map tileset re-skin on warp — when a `GameSession` warp switches to a map with a different
  `TilesetName`, the runtime currently keeps the boot map's sheet (RpgGame loads it once in `Game1.LoadContent`).
  Make the host reload the sheet on an actual tileset change; re-skin the bundled town to `lpc-grass` to prove it.
  Slice A of 3 under a standing `/goal` (A re-skin → B save/load host → C built-in library).
- **Intake source:** none (direct `/work` request).
- **Classification / tier:** work pipeline, SMALL slice (no split). P2 runtime/rendering polish; the #22 deferred item.
- **Forge recall (lessons/failures surfaced):** `knowledge-context` (Plan) surfaced — opaque ids, but the top
  hits align with: **D-0025** (per-map tileset catalog), **AD-claude-multimap-model-001** (#22's switch), **D-0022**
  (neutral host); prevention rules incl. the #20 host-resolution-single-source seam + the detach-before-resubscribe
  rule; recent failures = the #21 undo reverse-order + the #22 Studio subscription leak. Nothing blocking — confirms
  the load-bearing constraints are D-0022 (host owns the art / signals-then-reloads) + D-0025 (single-source catalog).
- **Ticket:** #23 — `765e62a0-a48d-4f6a-b9b5-a50d840381bf`.
- **AAR id (from `aar-open`):** `2344d98f-c1bd-46db-9118-df63754d1a53`.
- **EARS requirements reviewed:** REQ-001 re-skin on a tileset-name change · REQ-002 resolve through the catalog ·
  REQ-003 no churn when unchanged · REQ-004 the town uses a different sheet (visible re-skin) · REQ-005 FULL gate +
  golden `town.json`.
- **Design questions for Phase 2:** (1) the seam shape — a `protected virtual void OnTilesetChanged(string)` hook
  `Game1` overrides (recommended; mirrors #20) vs a polled flag; where `RpgGame` checks it (Draw/Update). (2) the
  GATED change-detect seam — extract the pure "changed since last applied?" comparison (RpgGame holds the
  last-applied name) into a unit-testable shape. (3) the town grass indices — `TownBuild` → `lpc-grass` + valid
  floor/wall indices (geometry is host-derived via `Tileset.FromSheet`, so any in-range index renders).

## Phase 2 — Design
- **Approach / architecture:** A gated **`TilesetTracker`** (`Engine.Core`, alongside `RpgGame`/`Camera`) holds the
  last-applied tileset name (`string?`, initially null) and `bool TryAdvance(string current)` — returns **true +
  records** `current` when it differs from the last-applied (Ordinal), **false** when unchanged. This is the
  unit-testable heart of the re-skin decision (REQ-001/003); deterministic (string compare, no time/RNG). The host
  `RpgGame` (`[ExcludeFromCodeCoverage]`) holds a `TilesetTracker` + a `protected virtual void OnTilesetChanged(
  string tilesetName)` (no-op default); at the **top of `Draw`** (before `DrawMap`) it calls
  `if (_tilesetTracker.TryAdvance(MapTilesetName)) OnTilesetChanged(MapTilesetName);`. `Game1` **overrides**
  `OnTilesetChanged` to reload the sheet (`TilesetCatalog.ResolveResourceFile` → embedded `Stream` →
  `Texture2D.FromStream` → `SetTileset`) and **drops the one-shot sheet load from `LoadContent`** — the first
  `Draw`'s `TryAdvance(null → start-name)` fires the initial load through the SAME hook (one path, no double-load).
  D-0022 holds: `RpgGame` only signals; the host owns the embedded art + GPU. The bundled **town re-skins to
  `lpc-grass`** (`StartMap.TownBuild` → `TilesetName = "lpc-grass"` + grass-valid floor/wall indices — **lpc-grass
  is 96×192 = 3×6 = 18 tiles (0–17)**, so the mountains 66/1 are OUT OF RANGE and must change). `town.json`
  regenerates.
- **File manifest (7):**
  | # | File | Change |
  |---|---|---|
  | 1 | NEW `src/MonoRpgMaker.Engine/Core/TilesetTracker.cs` | gated; `string? _applied`; `bool TryAdvance(string current)` (Ordinal, record-on-change, null-guard `current`); XML-doc |
  | 2 | MOD `src/MonoRpgMaker.Engine/Core/RpgGame.cs` | hold a `TilesetTracker`; `protected virtual void OnTilesetChanged(string)` (no-op); `Draw` top: `if (_tilesetTracker.TryAdvance(MapTilesetName)) OnTilesetChanged(MapTilesetName);` |
  | 3 | MOD `src/MonoRpgMaker.Player/Game1.cs` | override `OnTilesetChanged` → reload via `TilesetCatalog`→`Texture2D.FromStream`→`SetTileset`; remove the one-shot sheet load from `LoadContent` (keep `base.LoadContent`) |
  | 4 | MOD `src/MonoRpgMaker.Engine/Sim/StartMap.cs` | `TownBuild`: `TilesetName = "lpc-grass"` + grass floor/wall indices in 0–17 (implement picks a solid-grass floor + a blocking edge; verify in-range) |
  | 5 | REGEN `content/maps/town.json` | = `MapSerializer.Serialize(TownBuild(), TownEvents())` (the embedded town; golden if a test compares it) |
  | 6 | NEW `tests/MonoRpgMaker.Engine.Tests/TilesetTrackerTests.cs` | the `TryAdvance` matrix + the null-guard |
  | 7 | MOD `tests/MonoRpgMaker.Engine.Tests/MultiMapWarpTests.cs` | the `StartMap_TownBuild_*` tests: add `TilesetName=="lpc-grass"`; keep dims + border/interior asserts |
- ### Regression Test Plan
  | # | Test | Proves Requirement |
  |---|---|---|
  | T1 | `TilesetTracker` fresh → `TryAdvance("lpc-mountains")` == true (initial null→name fires the load) | REQ-001 (initial) |
  | T2 | after `TryAdvance("a")`, a second `TryAdvance("a")` == false (unchanged → no reload) | REQ-003 |
  | T3 | after `"a"`, `TryAdvance("b")` == true (a real change re-skins) | REQ-001 |
  | T4 | after `"a"`→`"b"`, `TryAdvance("a")` == true (change-back fires — kills a "remember-all-seen" mutant) | REQ-001 |
  | T5 | `TryAdvance(null!)` throws `ArgumentNullException` (the active map always names a tileset) | totality/guard |
  | T6 | `StartMap.TownBuild().TilesetName == "lpc-grass"` | REQ-004 |
  | T7 | town border `(0,0)` blocking + interior `(1,1)` floor still hold (the #22 asserts unchanged) | REQ-004 regression |
  | T8 | the committed `town.json` matches `MapSerializer.Serialize(TownBuild(), TownEvents())` (golden, if it exists) | REQ-005 |
  | T9 | FULL `bin/gate.sh` green (coverage + mutation + oracle 6×4) | REQ-005 |
  - **Uncoverable (host, `[ExcludeFromCodeCoverage]`, manual smoke):** `RpgGame.OnTilesetChanged` + the `Draw`
    check + `Game1`'s reload — exercised by running the Player and warping start⇄town to SEE the grass re-skin.
- **Risks / decisions:** (1) **First-Draw load timing** — the `Draw`-top check runs before `DrawMap` in the same
  frame, and `GraphicsDevice` is ready in `Draw`, so `SetTileset`-then-`DrawMap` is correct; a failed reload is a
  no-op that RETAINS the prior sheet (not `ColorFor` — that only fires when no sheet is loaded yet). In practice
  `TilesetCatalog.ResolveResourceFile` always resolves to a catalog sheet (defaulting to `lpc-mountains`) and the
  bundled sheets all exist, so a failed re-skin can't occur for bundled content; no crash either way. (2) **Grass indices are a smoke-visual** — any in-range (0–17) index renders;
  the re-skin PROOF is the SHEET swap, not the exact tiles (the gated assert is the NAME). (3) **town.json MUST
  regen** or the embedded town is stale (and a golden test, if present, fails) — regen in implement. (4) The
  `TilesetTracker` null-guard on `current` is a genuine precondition (a map always names a tileset) — throw, not a
  typed result.

## Phase 3 — Implement
- **Built:**
  - `Engine/Core/TilesetTracker.cs` (NEW, gated) — `string? _applied` + `bool TryAdvance(string current)` (Ordinal,
    record-on-change, null-guard). The unit-testable change-detect heart.
  - `Engine/Core/RpgGame.cs` — `+ TilesetTracker _tilesetTracker` field + `protected virtual void
    OnTilesetChanged(string)` (no-op); `Draw` top (after the spriteBatch null-guard, before `ViewOffset`):
    `if (_tilesetTracker.TryAdvance(MapTilesetName)) OnTilesetChanged(MapTilesetName);`.
  - `Player/Game1.cs` — `OnTilesetChanged` OVERRIDE does the reload (`TilesetCatalog.ResolveResourceFile` →
    embedded `Stream` → `Texture2D.FromStream` → `SetTileset`); the `LoadContent` override was DELETED (it only did
    `base.LoadContent()` + the sheet load → the load moved to the hook, fired on the first `Draw`; the single load
    path, no double-load). `base.LoadContent` (RpgGame) owns the sprite-batch/pixel setup.
  - `Engine/Sim/StartMap.cs` — `TownBuild` → `TilesetName = "lpc-grass"` + town-local `grassFloor (4, false)` /
    `grassWall (0, true)` (the mountains 66/1 are out of range for the 18-tile grass sheet); 16×12 size + border
    layout unchanged.
  - REGEN `content/maps/town.json` (now `tileset: "lpc-grass"`); `start.json`/`game.json` untouched.
  - Tests: `TilesetTrackerTests.cs` (NEW — initial/same/changed/changed-back/null-guard) + `MultiMapWarpTests`
    `StartMap_TownBuild_UsesGrassTileset`.
- **Deviations from design (+ reason):**
  1. `Game1.LoadContent` override was removed ENTIRELY (design said "drop the sheet-load block") — it only did
     `base.LoadContent()` after that block, so the whole override was redundant; `RpgGame.LoadContent` (base) is
     inherited. Cleaner.
  2. Grass `floor=4 / wall=0` are a smoke-visual choice (any in-range 0–17 index renders; the gated proof is the
     tileset NAME). Can be refined by viewing the sheet, but irrelevant to the re-skin contract.
- **Build / verify:** Engine `-warnaserror` 0/0; whole solution 0/0; **GATE GREEN [fast]** (oracle 6×4 unchanged).
  The host re-skin (`RpgGame.OnTilesetChanged` + the `Draw` check + `Game1`'s reload) is `[ExcludeFromCodeCoverage]`
  — manual smoke (run the Player, warp start⇄town, see the grass).
- **NOT yet (Phase 4):** run the suite + the FULL gate (coverage + mutation — `TilesetTracker` mutants must die).

## Inspect (Phase 3.5)
- **Lenses run:** 1 independent correctness/design critic (general-purpose) over the small diff — **survived** and
  verified CONCRETELY (ran `TilesetTrackerTests` 5/5 green + a throwaway byte-equality check that committed
  `town.json` == `MapSerializer.Serialize(TownBuild(), TownEvents())`, then deleted it). No BLOCKER/MAJOR.
- **Findings:**
  | # | Severity | Finding | Verdict | Fix |
  |---|---|---|---|---|
  | 1 | MINOR | notes claimed a failed reload "falls back to `ColorFor`" — inaccurate: a failed re-skin is a NO-OP that retains the PRIOR sheet (`ColorFor` only fires when `_tileset` is null = the first load). | **REAL (doc-accuracy, not a code defect)** — `TilesetCatalog.ResolveResourceFile` always resolves to a catalog sheet (defaulting to mountains) + the bundled sheets exist, so a failed re-skin can't occur for bundled content. | **FIXED** — corrected the notes wording. |
  | 2 | NIT | notes "3×9... = 18" arithmetic typo | REAL | **FIXED** — "3×6 = 18". |
- **Cleared (verified, not just unflagged):** the `TryAdvance` matrix (change→true+record · same→false · initial
  null→true · change-back→true · null→throw — 5/5 green); D-0022 host seam (`RpgGame` contains NO
  `Assembly`/`GetManifestResourceStream`/`Texture2D.FromStream` — grep-confirmed; the `Draw`-top check sits after
  the spriteBatch null-guard + before `DrawMap`; deleting `Game1.LoadContent` is safe — base `RpgGame.LoadContent`
  still creates `_spriteBatch`/`_pixel`); `town.json` golden byte-for-byte in sync (`start.json`/`game.json`
  untouched); no dead code in `Game1` (all four usings + `TileSize` still referenced); grass indices 4/0 in-range
  for the 18-tile sheet; #22 regression (no town test asserts a tile INDEX — only size + `.Blocking`, both
  preserved); `TilesetTracker` determinism/MRM-clean (single `string?` + Ordinal compare).
- **Post-fix:** doc-only fixes (no `.cs` changed) — build + **GATE GREEN [fast]** still hold.
- **Capture:** none — the two findings are doc-accuracy, not code defects (no `failure-record`/`prevention-rule`).
- **Phase-4 kill-list (TilesetTracker mutants — all covered by the 5 existing tests, NO gap):** ① equality-flip /
  always-reload → `SameName_FalseSecondTime`; ② negate equality → `SameName`; ③ remove `_applied = current` →
  `SameName`; ④ seen-set / remember-all → `ChangedBack_True`; ⑤ initial-state pre-seed → `Initial_True`; ⑥ strip
  the null-guard → `Null_Throws`; ⑦ invert booleans → `Initial`/`Changed` vs `SameName`. Plus REQ-004
  `StartMap_TownBuild_UsesGrassTileset`; REQ-005 the golden `town.json` + the FULL gate.

## Phase 4 — Validate
- **Tests added (+6; 471 → 477):** `TilesetTrackerTests.cs` (5 — initial null→true · same→false · changed→true ·
  changed-back→true · null→throw; REQ-001/003, isolated asserts) + `MultiMapWarpTests.StartMap_TownBuild_
  UsesGrassTileset` (REQ-004). The #22 town tests (dims/border/interior/far-corner) + the golden `town.json` cover
  the unchanged size+blocking + the regen (REQ-005).
- **`dotnet test MonoRpgMaker.slnx`: 477 passed, 0 failed.**
- **`bin/gate.sh`: GATE GREEN [full] — 13/13** (first try). Coverage **93.0%** (floor 80). Mutation: Engine
  **85.68%** (↑ from 85.43 — the `TilesetTracker` mutants all died via the 5 tests) · Abstractions 82.22% ·
  Analyzers 91.18% · Editor 85.45% (floor 80). Oracle 6 rows / 4 modules. Receipt written.
- **Pre-existing exclusions:** the host re-skin (`RpgGame.OnTilesetChanged` + the `Draw` check + `Game1`'s reload)
  is `[ExcludeFromCodeCoverage]` (the composition root + the live GPU/embedded-resource load) — verified by manual
  smoke (run the Player, walk start→town, the map re-skins to grass), not gated.

## Phase 5 — Complete
- **Docs updated:** `docs/roadmap.md` (the multi-map paragraph += the #23 cross-map re-skin — start⇄town now
  switches `lpc-mountains`⇄`lpc-grass`) + `docs/product-phasing.md` (the tile/sprite-rendering row — the sheet
  reloads per active map on a warp). No `decisions.md` entry — the re-skin is an application of D-0022/D-0025/D-0026.
- **Forge capture:** `aar-submit` 2344d98f (completed, effectiveness 4; 0 novel findings, 13 verdicts). No
  failure-record / prevention-rule / AD — inspect found only doc-accuracy nits (no code defect), no new decision.
- **Ticket closed:** #23 (`765e62a0-…`) → done; `TICKET-0023-tileset-reskin-warp.md` moved open/ → closed/.
- **Archived:** spec + notes moved active/ → completed/; spec status `Phase 5 — Complete PASS`.
- **Follow-ups (open — the remaining 2 of 3 under the standing /goal):** B) the in-game save/load host; C) the
  rest of the built-in library (chest / door / shop / give-item).
