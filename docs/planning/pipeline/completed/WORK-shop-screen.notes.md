# WORK-shop-screen — Notes

Per-phase working notes for the paired `.spec.md`. The spec is the contract;
this is the record of what happened.

## Phase 1 — Plan
- **Request:** Shop E2 — the host shop SCREEN (render `ActiveShop` + modal input + the game's first text rendering;
  a bundled demo). The 2nd of 3 shop slices (user chose the content-pipeline `SpriteFont`).
- **Intake source:** none (direct `/work` under a standing `/goal`).
- **Classification / tier:** work pipeline, MEDIUM-LARGE slice — mostly `[ExcludeFromCodeCoverage]` host (manual-
  smoke) + a small gated seam (cursor + the movement guard + the `Shop` event). P2 ui/rendering.
- **Explore (the architecture — verified file:line):**
  - `RpgGame` (Engine.Core, **`[ExcludeFromCodeCoverage]`** line 23): Update→`HandleMovement` (160-178; `Pressed`
    edge-detect; Up/Down/Left/Right→Step, Space/Enter→Act, F5/F9); Draw (two `SpriteBatch.Begin` passes — a world
    camera pass + a screen-fixed UI pass calling `DrawMessageBanner` 260-270, which draws a **bare banner, NO TEXT**
    — messages only hit the window title 199). `TileSize=32`, viewport 20×15. Virtual hooks (`OnTilesetChanged`
    74-76 etc.); `Content.RootDirectory="Content"` (48); `_spriteBatch`/`_pixel` in `LoadContent` (56-61).
  - `Game1` (Player, **`[ExcludeFromCodeCoverage]`**): `OnTilesetChanged` loads embedded tilesets
    (`Texture2D.FromStream`); has the built-in `Game.Content` ContentManager; NO `LoadContent` override yet.
  - **`Player.csproj` ALREADY references `MonoGame.Content.Builder.Task` 3.8.4.1 + has a (empty) `Content.mgcb`** —
    the content pipeline is wired, just unused. So **adding the font needs NO new package + NO `packages.lock.json`
    regen** (the big de-risk for the gate's `--no-restore --locked-mode` build). Art is embedded (`EmbeddedResource`
    globs); the font will be the first pipeline (.xnb) asset.
  - **NO initial-state seed:** `GameSession.Create` always starts with an empty `GameState` (no gold); `GameState`
    has no seeding ctor; `game.json` carries no initial state. → the demo gold is HOST-SIDE (`Game1` seeds it after
    boot); a real starting-state feature is deferred.
  - The golden `RuntimePlayableTests.CommittedStartJson_MatchesBuild` regens when `StartMap.Events()` gains a `Shop`
    event. `SpriteFont`: `Content.Load<SpriteFont>("font")` + `SpriteBatch.DrawString(font, text, pos, color)`.
- **Ticket:** #28 — `b658b5af-4b7f-4316-8d49-b71fadfd7e50`.
- **AAR id (from `aar-open`):** `d8ab8063-2626-437f-9c5a-21e412c5bf54`.
- **EARS reviewed:** REQ-001 cursor clamp · REQ-002 the movement guard · REQ-003 `MoveShopCursor` · REQ-004 the
  bundled `Shop` event · REQ-005 (manual-smoke) the screen + font + input + gold seed · REQ-006 FULL gate.
- **Design questions for Phase 2:** (1) the font-delivery seam (a protected `RpgGame._spriteFont` field +
  `Game1.LoadContent` loads it, mirroring `SetTileset` — graceful if missing). (2) the `.spritefont` (Arial 14, ASCII
  32–126) + the `Content.mgcb` `/build:` entry. (3) the cursor clamp semantics (`Math.Clamp`? but `Math.Clamp` on
  empty → no offers → no-op). (4) the modal input keys (Up/Down/Enter/S/Escape). (5) the `Shop` event placement (a
  cell clear of the sign/warp/lever/door) + the golden regen (the #20/#22 file-based app). (6) the host-side gold
  seed in `Game1` (+ a demo potion to show selling). (7) confirm the content build compiles under the gate.

## Phase 2 — Design
- **Approach / architecture:** A thin GATED seam drives the UI; the screen itself is host code. Gated: `ShopState`
  gains a `Cursor` (private set) + `MoveCursor` (the only mutator — clamp `[0, Offers.Count-1]`, no-op on empty);
  `WorldSim.MovePlayer` no-ops while `ActiveShop != null` (the map freezes while shopping — robust even if the host
  forgets); `WorldSim.MoveShopCursor` delegates. A bundled `Shop` event (shopkeeper) goes in `StartMap.Events()`
  (gated via the golden). Host (`[ExcludeFromCodeCoverage]`): the content-pipeline `SpriteFont` (a `.spritefont` +
  the `Content.mgcb` entry — the Builder.Task is already present); `Game1.LoadContent` loads it (graceful on
  `ContentLoadException`) + hands it to `RpgGame` via `SetFont` (mirrors `SetTileset`) + seeds demo `gold`/`potion`;
  `RpgGame` routes modal input when a shop is open (Up/Down → cursor, Enter → Buy, S → Sell, Escape → Close — and
  the global Escape→Exit is guarded behind no-shop) + `DrawShopScreen` + finally renders the message TEXT.
- **File manifest (10):**
  | # | File | Change | Gate |
  |---|---|---|---|
  | 1 | MOD `Engine/Sim/ShopState.cs` | `+ int Cursor {get; private set;}` + `MoveCursor(int delta)` (`Math.Clamp`, no-op empty) | **gated** |
  | 2 | MOD `Engine/Sim/WorldSim.cs` | `MovePlayer` → `if (ActiveShop is not null) return false;` at top; `+ MoveShopCursor(int) => ActiveShop?.MoveCursor(delta)` | **gated** |
  | 3 | MOD `Engine/Sim/StartMap.cs` | `+` a `Shop` `EventData` (`shopkeeper`, (6,12), `items "potion:5:2,ether:20:8"`) in `Events()` | gated (golden) |
  | 4 | NEW `Player/Content/font.spritefont` | Arial 14, ASCII 32–126 (the probe-tested XNA XML) | host |
  | 5 | MOD `Player/Content/Content.mgcb` | append `#begin font.spritefont` + FontDescription importer/processor + `/build:font.spritefont` | host |
  | 6 | MOD `Engine/Core/RpgGame.cs` | `+ SpriteFont? _spriteFont` + `SetFont`; `HandleMovement` shop-modal guard + `HandleShopInput`; Update `Exit()` guarded behind `Sim.ActiveShop is null`; `DrawShopScreen` + message-text in `DrawMessageBanner` | host (excluded) |
  | 7 | MOD `Player/Game1.cs` | `LoadContent` loads `font` (catch `ContentLoadException`) + `SetFont`; seed `gold=100`, `item.potion=1` | host (excluded) |
  | 8 | REGEN `content/maps/start.json` | `= Serialize(Build, Events, Doors)` with the shopkeeper | gated (golden) |
  | 9 | MOD `tests/.../EventFoundationTests.cs` | `Events_IsWelcomeSign` `3 → 4` + a shopkeeper assert | test |
  | 10 | NEW `tests/.../ShopScreenTests.cs` | the gated matrix | test |
- ### Regression Test Plan
  | # | Test | Proves Requirement |
  |---|---|---|
  | T1 | a `ShopState` (2 offers): `Cursor` starts 0; `MoveCursor(+1)`→1, `MoveCursor(+5)`→clamp 1, `MoveCursor(-5)`→0; a 0-offer shop `MoveCursor(+1)`→stays 0 (no throw) | REQ-001 |
  | T2 | a `WorldSim` with an open shop (step onto a `StepOn` `ShopEvent`) → `MovePlayer(any)` returns `false` + `Player.Cell` unchanged | REQ-002 |
  | T3 | `MoveShopCursor(+1)` on an open shop → `ActiveShop.Cursor` 1; on a no-shop `WorldSim` → no-op (no throw) | REQ-003 |
  | T4 | `StartMap.Events()` has `Single(e => e.Kind == "Shop")` with the items; the golden `start.json` matches | REQ-004 |
  | T5 | *(manual-smoke — see checklist below; NOT gate-verified)* the Player renders + drives the shop + the message text | REQ-005 |
  | T6 | FULL gate green — the content build compiles the font; coverage ≥80 (cursor/guard/MoveShopCursor tested); mutation; the golden | REQ-006 |
  - **Uncoverable (host, `[ExcludeFromCodeCoverage]`):** `RpgGame` (`HandleShopInput`/`DrawShopScreen`/the Draw +
    Update changes) + `Game1` (`LoadContent`/the gold seed) — the live MonoGame loop (window/GPU/input), no
    unit-testable contract; private members inherit the class-level exclusion. The `.spritefont`/`.mgcb` are build
    assets. So the gated surface is items 1–3 only — all unit-tested (Engine MSI is 81.62, near the floor).
  - **Manual-smoke checklist (REQ-005):** launch the Player → walk to the shopkeeper at (6,12) → action-button →
    the shop opens (100 gold; `potion 5/2`, `ether 20/8`); Down moves the cursor; Enter buys potion (gold 95, potion
    2); S sells (gold 97, potion 1); Escape closes (does NOT quit); the welcome sign + warp now show TEXT.
- **Risks / decisions:** (1) **the Escape conflict** — Update's global `Exit()` is guarded behind
  `Sim.ActiveShop is null` so Escape closes the shop first (only quits with no shop open). (2) `ShopState.Cursor` is
  `private set` + `MoveCursor` the only mutator (the host can't corrupt it). (3) font-missing degrades gracefully
  (`catch (ContentLoadException)` — the game runs textless). (4) the content build under the gate's `dotnet build
  --no-restore` — the `MonoGame.Content.Builder.Task` is already referenced + the `.spritefont` is the probe-tested
  one (mgcb compiled Arial headless → font.xnb), so the gate build should produce it; **the one residual gate risk**
  — if the Task fails headless in the gate, the build (gate:2) goes red + I fix at source (e.g. the
  `dotnet-mgcb` tool restore). (5) **D-0023 deviation:** the font uses the content pipeline (.xnb) while the art
  stays embedded — recorded here, NOT a new decision (the Builder.Task/`Content.mgcb` pre-existed). (6) Engine MSI
  watch — the cursor clamp + the move-guard mutants must die (the T1/T2/T3 isolated asserts).

## Phase 3 — Implement
- **Built:**
  - *Gated:* `ShopState` `+ Cursor` (private set) `+ MoveCursor` (`Math.Clamp`, no-op empty); `WorldSim.MovePlayer`
    no-ops while `ActiveShop != null` `+ MoveShopCursor`; `StartMap.Events()` `+` the `shopkeeper` `Shop` event
    (6,12).
  - *Host (`[ExcludeFromCodeCoverage]`):* `Player/Content/font.spritefont` (Arial 14) + the `Content.mgcb`
    `/build:` entry; `RpgGame` `+ _spriteFont`/`SetFont`, the `HandleShopInput` modal routing (Up/Down cursor, Enter
    buy, S sell, Escape close) + the Update `Exit()` guarded behind no-shop, `DrawShopScreen` + the message TEXT in
    `DrawMessageBanner`; `Game1.LoadContent` loads the `SpriteFont` (catch `ContentLoadException`) + seeds
    `gold=100`/`item.potion=1`.
  - *Content / tests:* regen `content/maps/start.json` (4 events); `EventFoundationTests` `3→4` + a shopkeeper
    assert; `ShopScreenTests.cs` (NEW, 6 gated).
- **Deviations from design (+ reason):** none material. The shop modal owns input via `HandleShopInput`; the global
  Escape→Exit is guarded behind `Sim.ActiveShop is null` (Escape closes the shop first) as designed.
- **Build / verify:** whole solution `-warnaserror` 0/0 **including the content-pipeline font** — the MGCB task
  compiled `/System/Library/Fonts/Supplemental/Arial.ttf` during `dotnet build` and `font.xnb` lands in the output
  (`bin/.../Content/`); **the content build also passes the gate's `dotnet build --no-restore --locked-mode`** (the
  one flagged risk — CLEARED; no `packages.lock.json` regen, the Builder.Task pre-existed). **GATE GREEN [fast]**;
  539 tests; oracle 12/8 (unchanged); the golden `start.json` matches.
- **NOT yet (Phase 4):** the FULL gate (coverage + mutation — the gated cursor/guard/`MoveShopCursor` mutants must
  die; the host screen/input/font are `[ExcludeFromCodeCoverage]`, manual-smoke).

## Inspect (Phase 3.5)
- **Lenses run:** 1 adversarial correctness critic — found **1 MAJOR + 1 MINOR + 1 NIT** (all REAL) + cleared the
  rest CONCRETELY (ran `--filter ShopScreen`/`--filter Events` [11/11] + a throwaway clamp repro [confirmed
  `Math.Clamp(0,0,-1)` THROWS — the empty-guard is load-bearing]; built the solution + the font; read the host
  code line-by-line; verified the seed key-convention makes the smoke math correct).
- **Findings:**
  | # | Severity | Finding | Verdict | Fix |
  |---|---|---|---|---|
  | 1 | **MAJOR** | `font.spritefont` had CharacterRegions ASCII 32–126 + **no `DefaultCharacter`**; E2 newly `DrawString`s `Sim.CurrentMessage`, and the reachable bundled lever message had an em-dash `—` (U+2014) → `DrawString` THROWS on the unrepresentable glyph → **pulling the lever crashes the host** (a smoke step). | **REAL crash E2 introduced** (pre-E2 the text only hit the window title). | **FIXED** — added `<DefaultCharacter>?</DefaultCharacter>` (any out-of-range char → `?`, never a crash) + ASCII-ized the bundled lever message (— → -) + regen `start.json`. Captured BF-…-spritefont-nonascii-crash-001 + PR-…-spritefont-defaultchar-001. |
  | 2 | **MINOR** | the Update `Exit()` was **level-triggered** (`IsKeyDown(Escape) && ActiveShop is null`); after Escape closes the shop, the NEXT frame (Escape still held, shop now null) → `Exit()`. So one Escape closes AND quits. | **REAL UX defect** (host). | **FIXED** — edge-detect the quit: `Pressed(keyboard, Keys.Escape)` (valid in Update — runs before `_previous` updates). Escape now fires once (close OR quit). |
  | 3 | NIT | `WorldSim.PressAction` lacked the `ActiveShop` guard that `MovePlayer` has (asymmetric — the host prevents it, but the belt-and-braces robustness was missing). | **REAL (minor) gap.** | **FIXED** — added `if (ActiveShop is not null) return false;` at PressAction's top + a gated `PressAction_BlockedWhileShopping` test (a faced sign that must NOT fire — kills the guard mutant). |
- **Cleared (verified):** the cursor clamp (private set, starts 0, empty-guard BEFORE `Math.Clamp` — the
  Clamp-throws-on-empty confirmed by repro, so the guard is load-bearing; the bounds `[0, Count-1]`); the movement
  guard (at MovePlayer's VERY TOP — clears nothing on a blocked move); `MoveShopCursor` (null-conditional no-op);
  the host modal input (the `!`-safety — a local `shop` captured + null-guarded, not a re-deref; no same-frame
  double-action after fix #2); the render (offer loop no-OOB, gold via `GetCount`, `_spriteFont` null-guard,
  message non-null); `Game1.LoadContent` (base-first / `catch (ContentLoadException)` / the seed key-convention →
  the smoke math is correct); the golden + count (4 events, the shopkeeper at (6,12) no-collision); MRM/§14 +
  determinism; the content build (font.xnb under `--no-restore`).
- **Post-fix:** GATE GREEN [fast] (build 0/0; oracle 12/8; 540 tests; the new PressAction test + the regen pass).
- **Capture:** `failure-record` BF-…-spritefont-nonascii-crash-001 + `prevention-rule-record`
  PR-…-spritefont-defaultchar-001 (always set a SpriteFont `DefaultCharacter` when rendering authored text).
- **Phase-4 kill-list (gated surface, covered by 7 ShopScreenTests):** the cursor clamp bounds (`0` low, `Count-1`
  high) + the empty-offers guard (a hard kill — Clamp throws without it); the `MovePlayer` guard (flip / `return
  true` / delete — the cell-unchanged + false assert); the new `PressAction` guard (the faced-sign-doesn't-fire
  test); `MoveShopCursor` (the `?.` + the delta).

## Phase 4 — Validate
- **Tests (+7; 533 → 540):** `ShopScreenTests.cs` (7) — REQ-001 cursor clamp + empty-no-op · REQ-002 MovePlayer
  blocked + **PressAction blocked** (the inspect add — a faced sign that must NOT fire) · REQ-003 MoveShopCursor
  moves + no-shop-noop · REQ-004 the StartMap shopkeeper. Plus `EventFoundationTests` 3→4 + a shop assert.
- **`dotnet test MonoRpgMaker.slnx`: 540 passed, 0 failed.**
- **`bin/gate.sh`: GATE GREEN [full] — 13/13.** Coverage **92.8%** (floor 80). Mutation: Engine **82.02%** (UP from
  E1's 81.62 — the gated cursor/guard/PressAction tests killed their mutants well, so the new gated code RAISED the
  near-floor Engine MSI) · Abstractions 82.22% · Analyzers 91.18% · Editor 82.28%. Oracle **12 rows / 8 modules**
  (unchanged — no new `.expect`). **The content-pipeline font compiled in the gate's `dotnet build`.** Receipt written.
- **Pre-existing exclusions:** none. The HOST screen/input/font (`RpgGame` shop input/draw, `Game1` load+seed) are
  `[ExcludeFromCodeCoverage]` (the live MonoGame loop — no unit-testable contract), so they are **manual-smoke,
  NOT gate-verified**. **Manual-smoke checklist (REQ-005):** launch the Player → walk to the shopkeeper at (6,12) →
  action-button → the shop opens (100 gold; `potion 5/2`, `ether 20/8`); Down moves the cursor; Enter buys potion
  (gold 95, potion 2); S sells (gold 97, potion 1); Escape closes (does NOT quit); the welcome sign + warp + lever
  now render TEXT.

## Phase 5 — Complete
- **Docs updated:** `docs/decisions.md` **D-0027** (in-game text via the content-pipeline SpriteFont, host-only) +
  `docs/roadmap.md` (the #28 shop screen + the first in-game text) + `docs/product-phasing.md` (the Event-behavior
  row += the screen + the text).
- **Forge capture:** `aar-submit` d8ab8063 (completed, effectiveness 4). `architecture-decision-record`
  AD-claude-content-pipeline-font-001 (D-0027; id `e394145b-…`). The `failure-record`
  BF-claude-spritefont-nonascii-crash-001 + `prevention-rule-record` PR-claude-spritefont-defaultchar-001 were
  recorded in inspect (the critic's MAJOR font-crash + the held-Escape MINOR).
- **Ticket closed:** #28 (`b658b5af-…`) → done; `TICKET-0028-shop-screen.md` moved open/ → closed/.
- **Archived:** spec + notes moved active/ → completed/; spec status `Phase 5 — Complete PASS`.
- **The shop:** E1 (economy) + **E2 (this — the screen + the game's FIRST text rendering) DONE.** **Only E3
  remains** — Studio Shop-event AUTHORING (an offers editor in the inspector, so an author can place a `Shop` with
  custom items/prices), completing the shop AND the whole no-code built-in library (ShowText / Warp / GiveItem /
  Chest / Lever+doors / Shop). Engine MSI is **82.02** (the gated cursor/guard tests raised it from 81.62).
