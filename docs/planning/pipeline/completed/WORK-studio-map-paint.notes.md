# WORK-studio-map-paint — Notes

## Phase 1 — Plan
- **Request:** Studio v1 — a visual map-paint editor in Avalonia (the maker tool's first GUI). The user chose
  the direction + the framework. **NORMAL pipeline** — drafted, then **paused for the user's review** before
  design (not autonomous).
- **Classification:** larger feature, scoped to ONE shippable MVP slice (paint + new/save/load over `$data`).
  The database/Modules/event editors are separate future pipelines.
- **Ground (verified):** `MonoRpgMaker.Editor` is gated (coverage+mutation) + NOT MRM-gated (float OK); has
  `MapEditor.Paint/Clear`. `MapSerializer` (#12) round-trips `$data` via a total `MapLoadResult`. Rendering is
  flat-color (no tileset images). The **Player is the GUI-host precedent** — a WinExe referencing Engine, NOT in
  gate:12's hardcoded mutation list (Engine/Abstractions/Analyzers/Editor) and NOT coverage-measured (not
  test-referenced). So: testable view-model → `Editor` (gated); the Avalonia `Studio` exe → a thin host (exempt).
- **Recall:** discarded the **oathstar** web-`/editor` bleed (Rust/`<canvas>`/`include_bytes!` — predecessor, not
  this repo). The monorpgmaker editor track (roadmap): map + database editors against the `$data` schemas; the
  event-editor pillar is replaced by agentic authoring (D-0011). The **pure-model + thin-seam** split is the
  right testable-canvas pattern.
- **Ticket:** 979ed134-7fce-4757-9533-d0fb4e2e71e3 (#16) — project a9ee8162 confirmed monorpgmaker.
- **AAR id:** ec48739d-bd84-4852-aff4-93d975b54162
- **EARS reviewed:** REQ-001..009.
- **Plan-review outcome (user calls):** (1) Avalonia LOCKED as the editor GUI framework. (2) **Re-scoped to
  Tiled-like with real LPC tiles** — added a tileset MODEL (sheet + tile size + columns; TilesetId→source rect)
  + sprite-tile rendering + a sheet palette, painting from committed **LPC Base Assets** (Sharm; CC-BY-SA 3.0 /
  GPL 3.0 / OGA-BY 3.0). The art is STAGED under `assets/tilesets/lpc/` (lpc-grass/dirt/water/mountains.png +
  `CREDITS.md` + `LPC-CREDITS.TXT`). v1 = one fixed tileset, plain index paint; autotiles/layers/multi-tileset
  deferred. Spec updated to REQ-001..009. **Proceeding to design; will PAUSE at design for the user's review.**

## Phase 2 — Design

### Architecture / approach — three layers
1. **Pure, gated logic in `MonoRpgMaker.Editor`** (coverage+mutation-gated; Avalonia-free + tested):
   - **`Tileset`** (`int TileSize`, `int Columns`, `int TileCount`) + a plain `SourceRect(int X,int Y,int Width,int
     Height)` int record. `bool TryGetSourceRect(int tilesetId, out SourceRect)` — false for `id < 0` (the
     `Tile.Empty` sentinel ⇒ "no sprite") or `id >= TileCount`; else `col = id % Columns`, `row = id / Columns`,
     rect = `(col*TileSize, row*TileSize, TileSize, TileSize)`. A factory `Tileset.FromSheet(sheetW, sheetH,
     tileSize)` (`Columns = sheetW/tileSize`, `TileCount = Columns * (sheetH/tileSize)`). **No Avalonia type** —
     the Studio maps `SourceRect`→Avalonia `Rect`, so the Editor stays testable + framework-free.
   - **`MapPaintSession`** (a plain model — NOT `INotifyPropertyChanged`; the code-behind drives it + redraws):
     holds a `MapEditor` (⇒ the current `TileMap`) + the active `Tile`. API: `TileMap Map` (read);
     `Cell? CellAt(double px, double py, int cellSize)` (pixel→cell, null when out of the map); `void
     SelectTile(int tilesetId)` (sets the active `Tile`, default blocking=false — or a blocking toggle, see
     risks); `bool Paint(Cell)` (via `MapEditor.Paint`, false oob); `void NewMap(int w, int h)`; `string Save()`
     (`MapSerializer.Serialize`); `MapLoadResult Load(string json)` (`MapSerializer.Deserialize`; on Ok swaps to
     a fresh `MapEditor` over the loaded map; returns the typed result — NEVER throws on bad data).
2. **A new `MonoRpgMaker.Studio` Avalonia exe — a thin `[ExcludeFromCodeCoverage]` HOST** (the Player pattern;
   references Engine + Editor + Avalonia; NOT in gate:12, NOT test-referenced ⇒ not coverage-measured):
   `Program.cs` (the Avalonia app builder) · `App.axaml`/`.cs` · `MainWindow.axaml`/`.cs` (a toolbar:
   New/Save/Load via `StorageProvider` file dialogs; hosts the palette + canvas) · **`MapCanvas : Control`**
   (`Render`: load the LPC sheet `Bitmap` once via `avares://`, for each non-empty cell
   `DrawImage(bitmap, srcRect, destRect)` from `session.Tileset.TryGetSourceRect`, a thin red overlay for
   blocking cells; `OnPointerPressed/Moved` → `session.CellAt` → `session.Paint` → `InvalidateVisual`) ·
   **`TilePalette : Control`** (draws the sheet, click → tile index → `session.SelectTile`, highlights the
   active). The default sheet = **`lpc-mountains.png`** (12×9=108). The PNGs ship as **`AvaloniaResource`**.
3. **The committed LPC art** (`assets/tilesets/lpc/`, already staged; CC-BY-SA/OGA-BY + `CREDITS.md`).

**Data flow:** pointer → `CellAt` → `Paint` (MapEditor mutates the TileMap) → `InvalidateVisual` → `MapCanvas.Render`
re-blits sprites via `Tileset.TryGetSourceRect` + the `Bitmap`. Save/Load = `session` strings (tested) wrapped by
the Studio's file dialogs (host IO).

### File manifest
| File | Change |
|---|---|
| `src/MonoRpgMaker.Editor/Tileset.cs` | NEW — `Tileset` + `SourceRect` record + `TryGetSourceRect`/`FromSheet` (pure, tested) |
| `src/MonoRpgMaker.Editor/MapPaintSession.cs` | NEW — the Avalonia-free view-model over `MapEditor`/`MapSerializer` |
| `src/MonoRpgMaker.Studio/MonoRpgMaker.Studio.csproj` | NEW — Avalonia exe (Avalonia, Avalonia.Desktop, Avalonia.Themes.Fluent, pinned); refs Engine+Editor; `AvaloniaResource` for `../../assets/tilesets/lpc/*.png` |
| `src/MonoRpgMaker.Studio/Program.cs` | NEW — `[ExcludeFromCodeCoverage]` app entry (`BuildAvaloniaApp().StartWithClassicDesktopLifetime`) |
| `src/MonoRpgMaker.Studio/App.axaml` + `.axaml.cs` | NEW — Avalonia app + Fluent theme; `[ExcludeFromCodeCoverage]` |
| `src/MonoRpgMaker.Studio/MainWindow.axaml` + `.axaml.cs` | NEW — toolbar (New/Save/Load) + palette + canvas; `[ExcludeFromCodeCoverage]` |
| `src/MonoRpgMaker.Studio/MapCanvas.cs` | NEW — the custom sprite-blitting canvas Control; `[ExcludeFromCodeCoverage]` |
| `src/MonoRpgMaker.Studio/TilePalette.cs` | NEW — the sheet palette Control; `[ExcludeFromCodeCoverage]` |
| `MonoRpgMaker.slnx` | MODIFY — add the Studio `<Project Path=…>` under `/src/` |
| `.config/nuget-license-overrides.json` | MODIFY (if needed) — override any Avalonia transitive nuget-license can't classify (MIT itself is already allow-listed) |
| `packages.lock.json` (per project) | MODIFY — regen after adding Avalonia (the gate restores `--locked`) |
| `tests/MonoRpgMaker.Engine.Tests/StudioEditorTests.cs` | NEW — the plan below (Tileset + MapPaintSession; in the Editor-gated scope) |

### Regression Test Plan
| T | REQ | Test (xUnit; isolated exact-count asserts) |
|---|---|---|
| T1 | 002 | `Tileset.FromSheet(384,288,32)` → Columns 12, TileCount 108; `TryGetSourceRect(0)`→(0,0,32,32); `(13)`→(32,32,…) (row wrap); `(-1)`→false (empty sentinel); `(108)`→false (oob). |
| T2 | 001 | `CellAt(px,py,cellSize)` → the right cell for interior points + cell boundaries; a point past width/height → null; negative → null. |
| T3 | 003 | `SelectTile(5)` then `Paint` writes a `Tile` with `TilesetId==5`. |
| T4 | 004 | `Paint(in-bounds)` sets that cell (via MapEditor) to the active Tile; `Paint(out-of-bounds)` → false, map unchanged. |
| T5 | 005 | `NewMap(4,3)` → `Map.Width==4`, `Height==3`, all cells `Tile.Empty`. |
| T6 | 006 | Paint a few cells → `Save()` → `Load(saved)` → `Ok`; the session's `Map` equals the painted map (cell-by-cell). |
| T7 | 007 | `Load("{ bad")` / `Load("null")` → `!Ok`, `Error` non-null, **no throw**, the existing map unchanged. |
| — | 008 | The Avalonia Studio renders LPC sprites + paints/saves/loads — **MANUAL smoke** (the view is `[ExcludeFromCodeCoverage]`; the live render loop is genuinely uncoverable). |
| — | 009 | FULL `bin/gate.sh` — Editor coverage + MSI ≥ 80 (T1–T7 cover `Tileset` + `MapPaintSession`); Avalonia licensed + pinned; Studio exempt. |
- **Editor MSI ≥ 80:** `TryGetSourceRect` branches (oob-low/oob-high/ok + the col/row math) — T1; `CellAt` bounds
  — T2; `SelectTile`/`Paint` in/oob — T3/T4; `NewMap` — T5; `Load` Ok-swap vs Failure-no-swap — T6/T7.

### Risks / decisions
- **Avalonia transitive licenses (the main gate risk):** Avalonia is MIT (already allow-listed) but pulls
  SkiaSharp/HarfBuzzSharp/MicroCom/etc.; some may not expose an SPDX id nuget-license recognises → gate:5 flags
  them → add `overrides.json` entries (their real licenses are MIT/BSD-ish). Implement runs the license gate +
  adds overrides; **may take an iteration**. Also: regen the lock + a gate:4 vuln pass for the Avalonia graph.
- **`-warnaserror` over a new framework:** Avalonia code-behind + the generated XAML `.g.cs` must pass nullable
  + `latest-recommended` analyzers + the BannedApiAnalyzers. Expect minor fixes (nullable annotations); file IO
  (`File.ReadAll/WriteAllText` via `StorageProvider`) is not banned. `GenerateDocumentationFile=false` ⇒ no
  XML-doc burden on the host.
- **net10 + Avalonia:** pin a current Avalonia 11.x (it targets netstandard2.0/net6/8; consuming from net10 is
  fine). If the newest Avalonia lags net10, pin the latest that resolves.
- **REQ-008 is a manual smoke** — the live Avalonia render/paint loop is genuinely uncoverable; the whole view
  layer is `[ExcludeFromCodeCoverage]` + Studio is excluded from coverage/mutation (the Player precedent). All
  *logic* (Tileset + MapPaintSession) is auto-tested in the Editor scope, so the floors hold on real coverage.
- **Blocking flag (v1 decision):** `SelectTile` paints `Blocking=false` by default; a per-tile blocking toggle in
  the palette/toolbar is a small add — design leans toward a simple "blocking" checkbox the session reads when
  painting. (Confirm at implement; not load-bearing.)
- **DEFERRED (unchanged):** autotiles; layers/regions/metadata; multi-tileset / `$data` tileset refs;
  database/Modules/event editors; undo/redo; multi-map; placing events/entities; the UI skin.

## Phase 3 — Implement
- **Built (production only; tests are Phase 4):**
  - `Editor/Tileset.cs` — `SourceRect` record + `Tileset` (`TileSize`/`Columns`/`TileCount`, `FromSheet`, `TryGetSourceRect`). Pure int math.
  - `Editor/MapPaintSession.cs` — the Avalonia-free session: `Cell` record + `CellAt`/`SelectTile`/`Paint`/`NewMap`/`Save`/`Load` over `MapEditor`+`MapSerializer`, plus `Width`/`Height`/`TileAt`/`Active`/`Map`.
  - `Studio/` (new Avalonia exe, host): `MonoRpgMaker.Studio.csproj`, `app.manifest`, `Program.cs`, `App.axaml`(+`.cs`), `MainWindow.cs` (code-only), `MapCanvas.cs`, `TilePalette.cs`. All view code `[ExcludeFromCodeCoverage]`.
  - `MonoRpgMaker.slnx` — added Studio under `/src/`. `packages.lock.json` regen; **locked-mode restore passes**.
- **Build:** `dotnet build MonoRpgMaker.slnx -warnaserror` → **0 warnings, 0 errors** (whole solution). avares resources embedded at `/Assets/lpc-*.png` (verified in the built dll) so the smoke can load them.
- **Deviations from design (with reason):**
  1. **`MapPaintSession` public API is framework-NEUTRAL** — exposes a `Cell` record + `Width`/`Height`/`TileAt(int,int)` instead of XNA `Point`. The Engine's MonoGame ref is `PrivateAssets=All`, so XNA does NOT flow to Studio (a referencing host); the design's `Point`-based surface failed to compile in Studio (CS0234). Neutralizing the surface keeps Studio **Avalonia-only** (no MonoGame native runtime dragged in) — a cleaner host boundary. The session's internals still use XNA `Point`.
  2. **`MainWindow` is code-only (no `.axaml`)** — minimizes compiled-binding XAML risk under `-warnaserror`; `App.axaml` stays XAML for the Fluent theme. Idiomatic for a tool window.
  3. **Avalonia pinned to 11.3.17, not 11.2.x** — 11.2.1 pulled a **high-severity-vulnerable** `Tmds.DBus.Protocol 0.20.0` (CVE-2026-39959 / GHSA-xrw6-gwf8-vvr9, patched at 0.21.3+) that would fail gate:4; 11.3.17 pulls a patched transitive (`dotnet list package --vulnerable` → clean). Added `Avalonia.Fonts.Inter` + `app.manifest` (standard desktop scaffolding).
- **NOT yet run:** the FULL gate — the new `Tileset`/`MapPaintSession` have no tests yet, so gate:12 mutation (Editor) would fail until Phase 4 writes them. Inspect next.

## Inspect (Phase 3.5)
Two parallel critics (both built+ran real code). Lenses: correctness/totality/runtime-smoke; design/purity/mutation-readiness.

**Critic 1 — correctness/totality/runtime-smoke: NO defects.** 11 scratch facts + a headless Avalonia smoke all passed:
- The 0-column edge (`FromSheet(16,16,32)` → columns 0) throws a **clean `ArgumentOutOfRangeException`** at construction — a 0-column `Tileset` is never built, so `% Columns` divide-by-zero is **unreachable** (the hypothesized [high] is absent).
- `CellAt` edges correct (right/bottom exclusive: `(640,0)`/`(0,480)`→null; `(639,479)`→last cell).
- **Totality holds:** `Load("{bad"|"null"|"[]"|width:0|"")` → no throw, `!Ok`, `Error` set, and the **existing map is UNCHANGED** (swap only on `Ok`).
- **Runtime smoke PASSES + is automatable:** a `/tmp` console referencing the built Studio dll + `Avalonia.Headless`+`Avalonia.Skia` (`UseHeadlessDrawing=false`) loaded `avares://MonoRpgMaker.Studio/Assets/lpc-mountains.png` → `Bitmap` decoded **384×288**, exit 0. URI ↔ embedded `/Assets/lpc-mountains.png` ↔ runtime decode all line up.

**Critic 2 — design/purity/MSI: PASS.** Player-pattern faithful (all 5 view types `[ExcludeFromCodeCoverage]`; Studio not in gate:12, not test-referenced); neutral-API boundary holds (no Xna in `MapPaintSession` public signatures; Studio compiles with no MonoGame ref); §14 clean; reuse faithful (`Load` delegates to `MapSerializer`). Delivered a 25-test MSI kill-list → **the Phase-4 test plan**.

**Findings + verdicts:**
| # | Sev | Finding | Verdict | Fix |
|---|---|---|---|---|
| 1 | high? | `FromSheet` undersized sheet → divide-by-zero in `TryGetSourceRect` | **REJECTED** — ctor guards columns>0, throws before any 0-column tileset exists; div-by-zero unreachable (critic verified) | none |
| 2 | low | Palette pixel→index math lives in the `[ExcludeFromCodeCoverage]` view (escapes mutation/coverage) + doesn't clamp `row` | **CONFIRMED** | Added tested `Tileset.TryGetTileIndex(px,py,out idx)` (mirrors `CellAt`; clamps col<Columns AND index<TileCount); `TilePalette` now delegates to it |
| 3 | low | `MapPaintSession.Map` getter is a latent Xna-recompile trap for hosts | **CONFIRMED (low)** — but compile-enforced (a host using it won't compile), so misuse fails loudly | Kept (tests use it); XML-doc'd it "engine-internal — hosts use Width/Height/TileAt" |
| 4 | info | `_ScratchInspect.cs` left in tree (parallel-critic race) | **CONFIRMED** | Deleted (critic 1 had already removed it; re-verified absent) |
| 5 | info | `Active` default is tile #0 (not Empty) | accepted — intentional; Phase-4 adds a kill-test | none |
| — | info | No alloc/determinism concerns; static brushes cached; only `_editor`/`_active`/`_painting` mutable | clean | none |

**Fixes verified:** full solution rebuild `-warnaserror` → **0/0**; `grep xna src/MonoRpgMaker.Studio` → empty (still MonoGame-free); scratch file gone.

**Captured:** PR-claude-editor-view-math-into-tested-layer-001.
**Deferred (noted):** an automated headless Avalonia smoke (REQ-008) is feasible (critic 1 proved it) but pulls `Avalonia.Headless`+`Avalonia.Skia` (+ their license/lock surface) into the test project — kept as a focused follow-up; REQ-008 stays a manual smoke for this slice, with the resource-load already verified once.

## Phase 4 — Validate
- **Tests added:** `tests/MonoRpgMaker.Engine.Tests/StudioEditorTests.cs` — 37 tests realizing the inspect 25-mutant kill-list:
  - `TilesetTests` — ctor guards (tileSize/columns/tileCount=0 → throw), `FromSheet` geometry (12/108, 1/1),
    `TryGetSourceRect` (−1/108→false, 107→true, index 0/12-row-wrap/107 exact rects), `TryGetTileIndex`
    (negative, col-beyond-sheet, row-past-sheet, origin, second column, second row).
  - `MapPaintSessionTests` — `CellAt` (cellSize 0 throw, −X/−Y null, right/bottom edge exclusive, origin, last
    cell, fractional), `SelectTile` (id+blocking, default false), `Paint` (in-bounds writes / off-map no-op),
    `NewMap` (4×3 all-empty), `Save` round-trip, `Load_Valid_SwapsMap`, `Load_Invalid_DoesNotSwap_NonDestructive`
    (`[Theory]` over `{bad`/`null`/`[]`/width:0/`""` → no throw, `!Ok`, witness intact).
- **`dotnet test MonoRpgMaker.slnx`:** `Passed! Failed: 0, Passed: 317, Total: 317` (280 prior + 37 new; no regressions).
- **`bin/gate.sh` (FULL):** **GATE GREEN [full]** — 13/13.
  - coverage **92.9%** ≥ 80 (the `[ExcludeFromCodeCoverage]` Studio views don't count).
  - mutation MSI: Engine **83.90%**, Abstractions **82.22%**, Analyzers **91.18%**, **Editor 84.84%** (with the new
    `Tileset`+`MapPaintSession` in scope — MSI ≥ 80 held). Studio not in the mutation list (host).
  - gate:4 vuln clean (Avalonia 11.3.17); gate:5 license clean (`[]`, **no overrides** — every Avalonia transitive
    is MIT/BSD-3-Clause, both already allow-listed); gate:13 `.expect` oracle OK (4 rows / 2 modules).
  - Receipt `.git/monorpgmaker-gate-receipt` written (FULL green — satisfies `/commit`).
- **Pre-existing failures:** none.

## Phase 5 — Complete
- Docs / forge / ticket / archive:
