# WORK-studio-tileset-picker — Notes

Per-phase working notes for the paired `.spec.md`. The spec is the contract;
this is the record of what happened.

## Phase 1 — Plan
- **Request:** Studio-v2 polish arc, first slice — a tileset picker for the Studio map editor (choose among the
  four committed LPC sheets instead of the single hardcoded `lpc-mountains.png`). The user chose this slice over
  undo/redo (undo/redo is the planned next slice).
- **Intake source:** none — a direct request via `/work`. (The trigger program's
  `INTAKE-event-trigger-system.md` is a different arc; this is the Studio-v2 arc, which has no umbrella intake
  doc. Offered one for parity at review.)
- **Classification / tier:** a **work pipeline** (feature), one vertical slice. Touches the `$data` schema +
  serializer + the gated editor session + two thin hosts + the runtime sheet selection, but it is one coherent
  end-to-end slice (a single map-level tileset name).
- **Forge recall (lessons/failures surfaced):**
  - `bulletin-list` → none.
  - `docs-search` (attributable, monorpgmaker): **D-0022** — Avalonia is the maker GUI; the studio is a thin
    `[ExcludeFromCodeCoverage]` host; all testable logic lives in the gated `MonoRpgMaker.Editor`; the public
    surface is framework-neutral (`Cell`/`int`, never an XNA `Point` — the **CS0234** trap because Engine's
    MonoGame ref is `PrivateAssets=All`). **WORK-studio-map-paint (#16)** explicitly listed
    "multi-tileset / a `$data` tileset reference (v1 = one fixed tileset)" under Out/deferred — this slice is
    that deferral.
  - Pattern to follow: the **pure-model + thin-host split** (PR-claude-editor-view-math-into-tested-layer) and
    the **single-source descriptor** (`BehaviourRegistry.Kinds`, #18/#19) so editor ⇄ runtime can't drift.
  - `knowledge-search` hits were opaque cross-tenant UUIDs (no project attribution per CLAUDE.md); deferred to
    `knowledge-context` at Phase-2 entry (needs the aar_id).
  - **Cross-tenant hygiene:** discarded any oathstar/Datastar/Tauri/Rust bleed; grounded on monorpgmaker code +
    docs only.
- **Grounding (code read, current state):**
  - `Engine.World/Tileset.cs` = pure geometry (`TileSize`/`Columns`/`TileCount` + source-rect math); **no sheet
    identity**. Built via `FromSheet(width, height, tileSize)` from pixel dims.
  - `Engine.World/TileMapData.cs` = `$data` shape: `Width`/`Height`/`Tiles[]`(`TilesetId`+`Blocking`)/`Events[]`
    — **no map-level tileset field.** `start.json` confirmed to have none (the 504 "tileset" substring hits are
    all per-cell `tilesetId` = 28×18 cells).
  - `Engine.World/MapSerializer.cs` = total round-trip via `MapJsonContext` source-gen; adding a `Tileset`
    property is additive + AOT-safe; deserialize already validates structurally (totality precedent).
  - `Editor/MapPaintSession.cs` (gated) = holds **one** `Tileset` for its lifetime (no switch); `Save`/`Load`
    via `MapSerializer`. Needs active-tileset-name state + `SelectTileset` + Save/Load carry.
  - Runtime: `Player/Game1.cs` hardcodes `lpc-mountains.png` → `SetTileset(...)`; `Core/RpgGame.cs` blits every
    cell from that one sheet; `Sim/StartMap.cs` authors mountains-specific indices (Floor=66, Wall=1).
  - Studio host: `MainWindow.cs` `DefaultSheet = "lpc-mountains.png"`; `MapCanvas`/`TilePalette` each hold one
    `Bitmap _sheet` for life.
  - **Resource wiring (sizes the slice):** Studio csproj already globs **all four** sheets as `AvaloniaResource`
    (`...lpc\*.png` → `avares://.../Assets/`) — **zero new Studio resource work.** Player csproj embeds **only
    `lpc-mountains.png`** — runtime-honor needs the other three embedded (a glob) + name-based load in `Game1`.
- **Scoping fork (A vs B) — RESOLVED to B:** (A) a pure editor-side preview picker is **semantically
  incoherent** — bare per-cell indices carry no sheet identity, so switching persists nothing and the Player
  still renders mountains. (B) a map-level tileset **name** in `$data` honored by editor **and** runtime is the
  only coherent slice, and it is **small** because Studio's four sheets are already avares-wired. **CHOSEN: B,
  tightly scoped.** **User confirmed full B (editor + runtime both honor) at the plan-review gate (2026-06-20).**
- **Ticket:** #20 — `a65351b4-f606-460a-a07f-4e0392e701c6` (project monorpgmaker `a9ee8162-3cfd-4c99-be5c-bbdb4be32b10`).
- **AAR id (from `aar-open`):** c9c2276e-582c-4d53-b990-5990a0549277
- **EARS requirements reviewed:** REQ-001 ($data round-trips the name) · REQ-002 (default-on-absent) · REQ-003
  (unknown → typed failure) · REQ-004 (`SelectTileset` switches + `Save` persists; unknown rejected) · REQ-005
  (single-source catalog) · REQ-006 (runtime renders the named sheet) · REQ-007 (Studio picker — manual smoke) ·
  REQ-008 (FULL gate green).

## Phase 2 — Design

### Approach / architecture (three layers, contract-first; D-0022/D-0023)
**1. Single source — `TilesetCatalog` (Engine.World, gated, XNA-free).** The four committed LPC sheets as
`TilesetInfo(string Name, string ResourceFile, int TileSize)` in one `IReadOnlyList<TilesetInfo> All`
(+ `DefaultName = "lpc-mountains"`, `Contains`, `TryGet`). The picker options, the loader's validation, and the
runtime's sheet selection ALL derive from `All` — they cannot drift (the `BehaviourRegistry.Kinds` pattern,
#18/#19). Co-located with the existing `Tileset`/`SourceRect` (which Studio already consumes), so it is
neutral-boundary-safe (no XNA/Avalonia).

**2. Data — the tileset name on the map + `$data`.** `TileMap.TilesetName` (default `lpc-mountains`) is the
map's tileset identity; it flows for free through `session.Map`, `MapLoadResult.Map`, and `sim.Map` (no new
plumbing on the result types). `TileMapData.Tileset` is its serialized form. `MapSerializer.Serialize` writes
`map.TilesetName`; `Deserialize` reads it — **empty/absent → default `lpc-mountains`** (REQ-002), **unknown
(not in catalog) → typed `MapLoadResult.Failure`** (REQ-003, consistent with the existing trigger validation),
else set `map.TilesetName`. Totality preserved (never throws).

**3. Editor (gated) + hosts (thin).** `MapPaintSession` tracks the **name** (`ActiveTileset` via `Map.TilesetName`,
`SelectTileset(name)`, `AvailableTilesets => TilesetCatalog.All`) and **drops its `Tileset` geometry property** —
geometry becomes a host concern (sharpens the data/render split). The Studio host owns pixels+geometry: a tileset
picker ComboBox → on pick/Load, load the named sheet's `Bitmap`, derive geometry via `Tileset.FromSheet`
(unchanged), and push both to the canvas + palette via a new `SetSheet`. The Player host loads the **map's**
named sheet (`Game1` reads `RpgGame.MapTilesetName` → `TilesetCatalog.TryGet(...).ResourceFile`).

**Resolved open questions:** Q1 geometry stays **host-derived from the real pixels** (`FromSheet`), the catalog
holds names/resource/tileSize only — drift-impossible, no pin test, tiny gated surface (alternative: catalog-
declared geometry + a PNG-IHDR pin test — rejected: declared geometry can drift, heavier test). Q2 catalog in
`Engine.World`, session surface stays `string`/`Cell` neutral, `session.Tileset` removed. Q3 switching
**re-interprets** existing per-cell indices against the new sheet (an index past the sheet's `TileCount` renders
empty via the existing `TryGetSourceRect` false path) — non-destructive, total. Q4 typed failure on deserialize;
`SelectTileset(unknown)` → `false`, no mutation. Q5 `$data` gains `tileset`; **regen `content/maps/start.json`**
to carry `"tileset":"lpc-mountains"` (forced by the golden `CommittedStartJson_MatchesBuild` test) — REQ-002 is
covered separately by a field-less inline JSON. Q6 name on `TileMap` → `sim.Map.TilesetName` → `RpgGame`
accessor → `Game1` sheet load; Player embeds all four sheets.

### File manifest
  | # | File | Change |
  |---|---|---|
  | 1 | `Engine/World/TilesetCatalog.cs` | **NEW** — `TilesetInfo(Name, ResourceFile, TileSize)` record + `TilesetCatalog` (`All` = 4 LPC entries [mountains first = default], `DefaultName`, `Contains`, `TryGet`). Gated, XNA-free, single source. |
  | 2 | `Engine/World/TileMap.cs` | +`public string TilesetName { get; set; } = TilesetCatalog.DefaultName;` (map's tileset identity; default = back-compat). |
  | 3 | `Engine/World/TileMapData.cs` | +`public string Tileset { get; set; } = string.Empty;` (the `$data` field; empty = absent). |
  | 4 | `Engine/World/MapSerializer.cs` | `Serialize` writes `data.Tileset = map.TilesetName`; `Deserialize` reads it — empty→default, unknown→`Failure`, else set `map.TilesetName`. (Totality kept.) |
  | 5 | `Editor/MapPaintSession.cs` | ctor `(string tilesetName, w, h)` (catalog-validate/default); **remove `Tileset` geometry prop**; +`ActiveTileset`/`AvailableTilesets`/`SelectTileset(name)`; `NewMap` preserves the active tileset; Save/Load carry the name via `Map`. Public surface stays neutral. |
  | 6 | `Studio/MapCanvas.cs` | hold `Tileset _geometry` (ctor param) + `SetSheet(Bitmap, Tileset)`; `Render` uses `_geometry` (not `session.Tileset`). |
  | 7 | `Studio/TilePalette.cs` | hold `Tileset _geometry` + `SetSheet(Bitmap, Tileset)` (resizes to the new sheet); `Render`/pointer use `_geometry`. |
  | 8 | `Studio/MainWindow.cs` | +tileset picker ComboBox (from `AvailableTilesets`); session built by name; on pick/Load → load the sheet by `ResourceFile`, derive geometry, push to canvas+palette + `SelectTileset` + sync the picker selection. |
  | 9 | `Engine/Core/RpgGame.cs` | +`protected string MapTilesetName => _sim.Map.TilesetName;` (host reads which sheet to load; render path unchanged). |
  | 10 | `Player/Game1.cs` | `LoadContent` loads the map's sheet by name (`TilesetCatalog.TryGet(MapTilesetName).ResourceFile`) instead of the hardcoded mountains. |
  | 11 | `Player/MonoRpgMaker.Player.csproj` | embed all four LPC sheets (glob, `LogicalName=%(Filename)%(Extension)`) instead of only mountains. |
  | 12 | `content/maps/start.json` | **REGEN** — now carries `"tileset":"lpc-mountains"` (Serialize emits it; keeps the golden `CommittedStartJson_MatchesBuild` test green). |
  | 13 | `tests/MonoRpgMaker.Engine.Tests/TilesetPickerTests.cs` | **NEW** — T1–T9 below. |
  | 14 | `tests/.../StudioEditorTests.cs` + `EventPlacementTests.cs` | **MOD** — update the two `MapPaintSession` ctor helpers (`MapPaintSessionTests.Session/Sheet`, `EventPlacementTests.Session`, line 227) to the name-based ctor. The `Tileset`-class geometry tests are untouched. |

### Regression Test Plan
  | # | Test | Proves Requirement |
  |---|---|---|
  | T1 | `Serialize` a map with `TilesetName="lpc-grass"` → JSON has `"tileset":"lpc-grass"`; `Deserialize` → `Map.TilesetName=="lpc-grass"` (round-trip) | REQ-001 |
  | T2 | `Deserialize` a `$data` JSON **omitting** the tileset field → `Ok`, `Map.TilesetName=="lpc-mountains"` (default-on-absent) | REQ-002 |
  | T3 | `Deserialize` `$data` with `"tileset":"bogus"` → `!Ok`, `Error` names the unknown tileset (typed failure, no throw) | REQ-003 |
  | T4a | `new MapPaintSession("lpc-mountains",w,h)`; `SelectTileset("lpc-grass")` → `true` + `ActiveTileset=="lpc-grass"`; `Save()` JSON contains `"lpc-grass"` | REQ-004 |
  | T4b | `SelectTileset("bogus")` → `false` + `ActiveTileset` unchanged (no mutation) | REQ-004 |
  | T5a | `TilesetCatalog.All` = exactly the 4 names {mountains,grass,dirt,water}, each `ResourceFile=="<name>.png"`, `TileSize==32` | REQ-005 |
  | T5b | `MapPaintSession.AvailableTilesets` is the SAME reference as `TilesetCatalog.All` (single source) | REQ-005 |
  | T6 | `TilesetCatalog.TryGet("lpc-water", out info)` → `info.ResourceFile=="lpc-water.png"`; a deserialized map's name feeds the resolution the Player loads | REQ-006 (unit) |
  | T7 | session `Save`→`Load` into a fresh session preserves the tileset name (grass saved → `ActiveTileset=="lpc-grass"`) | REQ-001/004 |
  | T8 | `NewMap` preserves the active tileset (select grass, `NewMap` → `ActiveTileset=="lpc-grass"`) | REQ-004 |
  | T9 | (regression) `CommittedStartJson_MatchesBuild` stays green after the start.json regen (it carries `"tileset":"lpc-mountains"`) | REQ-002 / back-compat |
  | — | host smoke (MANUAL): Studio → pick `lpc-grass` → palette+canvas redraw as grass → paint → Save to `content/maps/<x>.json`; run the Player on it → it renders grass | REQ-007 |
  | — | FULL `bin/gate.sh` green; Editor+Engine coverage + MSI floors (catalog, serializer branch, session name logic) via isolated exact-value asserts | REQ-008 |

  Excluded (host/composition, `[ExcludeFromCodeCoverage]`, manual smoke only): the Avalonia picker UI + bitmap
  swap (`MainWindow`/`MapCanvas`/`TilePalette`), and the Player's `Texture2D` load + `SetTileset` (`Game1`/
  `RpgGame` live GPU loop). The **name→resource** decision they consume is gated + tested (T6).

### Risks / decisions
- **Public API churn (load-bearing, bounded):** the `MapPaintSession` ctor changes to `(string, w, h)` and the
  `Tileset` geometry property is removed. Grounding confirmed only **2** ctor sites + their test helpers, and
  **no gated test** reads `session.Tileset` — so the blast radius is the two host files (excluded) + two test
  helpers. Reversible.
- **Golden start.json (decided):** Serialize emits `tileset` on every path, so `content/maps/start.json` must be
  regenerated to match `Serialize(StartMap.Build(), StartMap.Events())` (the `CommittedStartJson_MatchesBuild`
  oracle). Done as part of implement; REQ-002 default-on-absent is proven by a separate field-less inline JSON.
- **Geometry source (decided — Opt-Name):** geometry stays derived from the real loaded pixels; the catalog
  carries names only → no drift, no file-reading pin test, minimal gated surface. The considered alternative
  (catalog-declared geometry + a PNG-IHDR pin test using the `CallerFilePath` pattern) is recorded as rejected.
- **Switch on a non-empty map (decided):** re-interpret indices against the new sheet (out-of-range → empty) —
  non-destructive, total; the author repaints. No clear/warn.
- **MRM/§14 cleanliness:** `TilesetCatalog` lives in `World` (not sim), iterates a `List` (no Dictionary/HashSet
  enumeration ban), strings only (no float). Layering holds (World↛Sim; the type is XNA-free for Studio).

## Phase 3 — Implement
- **Built (per the manifest):**
  - `Engine/World/TilesetCatalog.cs` (NEW) — `TilesetInfo(Name, ResourceFile, TileSize)` readonly record struct +
    static `TilesetCatalog` (`All` = 4 LPC entries, mountains first/default; `DefaultName`; `Contains`; `TryGet`
    via `foreach` over the `All` list — MRM-clean, World not sim). XNA-free single source.
  - `Engine/World/TileMap.cs` +`TilesetName` (default `TilesetCatalog.DefaultName`); `TileMapData.cs` +`Tileset`
    (string, empty=absent); `MapSerializer.cs` — Serialize writes `map.TilesetName`; Deserialize defaults
    empty→`lpc-mountains`, `!Contains`→`MapLoadResult.Failure("unknown tileset …")`, else sets `map.TilesetName`
    (fail-fast before the build loop; totality kept).
  - `Editor/MapPaintSession.cs` — ctor `(string tilesetName, w, h)` catalog-validated/defaulted; **removed** the
    `Tileset` geometry property; +`ActiveTileset` (=> `Map.TilesetName`) / `AvailableTilesets` (=> `TilesetCatalog.All`)
    / `SelectTileset(name)`→bool; `NewMap` preserves the active tileset; `Save`/`Load` carry the name via `Map`.
    Public surface stayed neutral.
  - Studio host: `MapCanvas` + `TilePalette` now hold a `Tileset _geometry` + `SetSheet(Bitmap, Tileset)` and
    render from `_geometry` (not `session.Tileset`); `MainWindow` gained a tileset picker `ComboBox` (its options
    ARE `MapPaintSession.AvailableTilesets` — single source), `LoadSheetFor`/`OnTilesetPicked`/`ApplySheet`, and
    `SyncTilesetTo` on Load (guarded so the picker reflects a loaded map without re-triggering a reload).
  - Runtime: `RpgGame` +`protected string MapTilesetName => _sim.Map.TilesetName;`; `Game1.LoadContent` loads the
    map's sheet via `TilesetCatalog.TryGet(MapTilesetName).ResourceFile`; `Player.csproj` embeds all four LPC
    sheets (glob, `LogicalName=%(Filename)%(Extension)`).
  - `content/maps/start.json` regenerated — now carries `"tileset":"lpc-mountains"`; the two `MapPaintSession`
    ctor helpers (`StudioEditorTests`, `EventPlacementTests`) updated to the name-based ctor.
- **Build / verify:** whole solution `dotnet build -warnaserror` → **0 warnings / 0 errors** — the **neutral
  boundary HELD** (Studio compiles against the `Cell`/`string`/`TilesetInfo` API, no CS0234, no XNA leak).
  **382/382 tests pass** (the golden `CommittedStartJson_MatchesBuild` is green → the regen is byte-exact).
  **GATE GREEN [fast]** — 11/11 (format, build, test, vuln, licenses, gitleaks, shellcheck, no-suppressions,
  source-bans, doc-todos, .expect oracle 5×3).
- **Deviations from design (+ reason):**
  1. Removed the now-unused `Sheet()` helper in `StudioEditorTests.MapPaintSessionTests` — its only uses were the
     two ctor calls now on the name-based ctor; leaving it would trip the unused-private-member analyzer under
     `-warnaserror`. Anticipated by the manifest's "update the two ctor helpers". Not a behavioural change.
  2. `start.json` was regenerated via a .NET 10 file-based app (`dotnet run regen.cs` with `#:project` +
     `#:package MonoGame.Framework.DesktopGL`) rather than by hand — guarantees byte-exact System.Text.Json output
     for the golden test (the serializer uses XNA `Point` internally, so the regen app needs MonoGame at runtime).
     A tooling choice; the temp app was deleted. No design change.
- **NOT done (Phase 4 — Validate):** the new `tests/.../TilesetPickerTests.cs` (T1–T9) + the FULL gate
  (coverage + mutation). Inspect (Phase 3.5) runs next.

## Inspect (Phase 3.5)
- **Lenses run:** three independent parallel critics — (1) correctness/totality, (2) design/contract/MRM/
  neutral-boundary, (3) simplification/reuse/efficiency. Each verified concretely (read the code; ran arch tests
  / the suite). All three converged on the SAME single finding.
- **Findings:**
  | # | Severity | Finding (file:line) | Verdict | Fix |
  |---|---|---|---|---|
  | 1 | MAJOR | name→`ResourceFile` resolve-with-fallback duplicated **verbatim** in `Game1.cs:33` + `MainWindow.cs` `LoadSheetFor`, each with a literal `"lpc-mountains.png"` — the only two copies live in `[ExcludeFromCodeCoverage]` hosts (untested), and the literal is a 2nd/3rd source of the default sheet's file name. A drift seam vs the single-source contract (3-critic consensus). | **REAL** | **FIXED** — added gated `TilesetCatalog.Default` + `ResolveResourceFile(name)`; both hosts call it; the only `.png` strings now live in `TilesetCatalog.All`. |
  | 2 | NIT | whitespace-only `$data` tileset → typed `Failure`, not default (`IsNullOrEmpty`, not `IsNullOrWhiteSpace`) | **REJECTED** — totality holds (no throw); REQ-002 mandates default-on-*omission* only; failing closed on a garbage value is defensible | none |
  | 3 | NIT | serializer `Contains(name)` discards the resolved `TilesetInfo` (could `TryGet` once) | **REJECTED** — `Contains` is the right validation call (bool); 4-element list; idiomatic | none |
  | 4 | NIT | `ItemsSource = …Select(x => x.Name).ToArray()` pattern repeated (kinds + tilesets) | **REJECTED** — ctor-time, excluded host, cosmetic; sharing would over-abstract | none |
- **Explicitly cleared by the critics (verified, not just unflagged):** single-source integrity (REQ-005 — picker,
  `AvailableTilesets`, serializer validation, runtime selection all derive from `TilesetCatalog.All`; no sheet
  list hardcoded); **neutral boundary (D-0022/CS0234 holds)** — Studio's csproj has no MonoGame package + Engine's
  XNA is `PrivateAssets=All`, and the public surface Studio binds (`ActiveTileset`/`AvailableTilesets`/
  `SelectTileset`/`TilesetInfo`/`Tileset`/`SourceRect`) is `string`/`int`/`Cell` only; **MRM/§14** — `TilesetCatalog`
  in `World` (not sim), `foreach` over an `IReadOnlyList` (not Dictionary/HashSet), strings/ints only, layering
  green (Architecture tests 4/4); `TileMap.TilesetName` mutability justified (map identity the editor edits like
  cells); `TryGet` default-on-miss guarded by every caller; `start.json` regen byte-exact (golden test green).
- **Post-fix verify:** build `-warnaserror` 0/0; 382 tests pass; **GATE GREEN [fast]**.
- **Capture:** no `failure-record` — the finding was a DRY / single-source-discipline improvement (a drift *risk*),
  **not** a runtime bug (the critics confirmed totality + behaviour were correct as written). No new prevention-rule
  — it is an application of the existing single-source principle (D-0024 / the `BehaviourRegistry` pattern); the
  lesson is captured in the AAR at `/complete`.
- **Phase-4 kill-list (adopted):** T1 round-trip · T2 default-on-absent · T3 unknown→`Failure` · T4a `SelectTileset`
  switch+persist / T4b reject→`false`+no-mutation · T5a `TilesetCatalog.All` == exactly {mountains,grass,dirt,water}
  with `ResourceFile`/`TileSize` · T5b `AvailableTilesets` same reference (single source) · T6 `TryGet` name→info ·
  **T6b (NEW from the fix): `ResolveResourceFile("lpc-water")=="lpc-water.png"`; `ResolveResourceFile("bogus")==`
  `Default.ResourceFile=="lpc-mountains.png"`; `Default.Name==DefaultName`** (kills the new method's mutants) ·
  T7 Save→Load preserves the name · T8 `NewMap` preserves the active tileset · T9 golden `CommittedStartJson_MatchesBuild`.

## Phase 4 — Validate
- **Tests added:** `tests/MonoRpgMaker.Engine.Tests/TilesetPickerTests.cs` — **12 tests** (the adopted kill-list,
  isolated exact-value asserts): T1 round-trip · T2 default-on-absent (field-less inline JSON) · T3
  unknown→typed `Failure` · T5a `TilesetCatalog.All` == exactly {mountains,grass,dirt,water} + each
  `ResourceFile`/`TileSize` · T5b `AvailableTilesets` same reference (single source) · T6 `TryGet` known/unknown ·
  T6b `ResolveResourceFile` known + bogus→default + `Default.Name==DefaultName` · T4a `SelectTileset` switch +
  Save persists · T4b reject→`false` + no mutation · T7 session Save→Load preserves the name · T8 `NewMap`
  preserves the active tileset · ctor unknown-name→default (totality). **+12 (382 → 394).** (T9 golden start.json
  is the existing `RuntimePlayableTests.CommittedStartJson_MatchesBuild` — confirmed green, not duplicated.)
- **Two small validate-time fixes:** (1) hoisted T5a's expected-names array to a `static readonly` field
  (CA1861 under `-warnaserror`); (2) simplified `TilesetCatalog.Default` to `All[0]` — the prior
  `TryGet(DefaultName,…) ? info : All[0]` had an **unreachable** false branch that forcing-false leaves equal,
  i.e. a guaranteed mutation survivor; `All[0]` (order pinned by T5a) kills clean.
- **`dotnet test MonoRpgMaker.slnx`:** `Passed!  Failed: 0, Passed: 394, Skipped: 0`.
- **`bin/gate.sh` (FULL):** **GATE GREEN [full] — 13/13.** coverage **93.3%** ≥ 80; mutation **Engine 86.54 /
  Abstractions 82.22 / Analyzers 91.18 / Editor 84.95** (all ≥ 80 — the new catalog/serializer/session logic
  killed clean, Editor floor held); .expect oracle 5 rows / 3 modules — OK. Receipt written
  (`.git/monorpgmaker-gate-receipt`).
- **Pre-existing exclusions:** none.

## Phase 5 — Complete
- **Docs updated:** `docs/decisions.md` (+**D-0025** — map references its tileset by a catalog name in `$data`,
  single-source, editor + runtime honor it); `docs/roadmap.md` (tileset picker = Studio-v2 slice 2 shipped);
  `docs/product-phasing.md` (the map-paint editor row). The trigger-program intake
  (`INTAKE-event-trigger-system.md`) was left untouched — it's a different arc; the Studio-v2 arc has no umbrella
  intake doc.
- **Forge capture:** `aar-submit` → aar `c9c2276e` closed (outcome completed, effectiveness 5; 2 novel findings,
  12 verdicts; distillation/confidence/pattern jobs enqueued). `architecture-decision-record` →
  **AD-claude-tileset-catalog-001** (`2f426c4f`) = D-0025. `prevention-rule-record` →
  **PR-claude-host-resolution-single-source-001** (`877ff058`) — resolution logic shared by editor + runtime must
  live in the gated single-source layer, never re-spelled in `[ExcludeFromCodeCoverage]` hosts. **No
  `failure-record`** — the inspect finding was a drift-*risk* / DRY fix, not a runtime bug (totality + behaviour
  were correct as written).
- **Ticket closed:** #20 (`a65351b4`) → `done`; local doc moved `docs/planning/tickets/{open → closed}/`.
- **Archived:** `WORK-studio-tileset-picker.{spec,notes}.md` → `docs/planning/pipeline/completed/`.
