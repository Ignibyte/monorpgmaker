# WORK-p0-map-data — Notes

## Phase 1 — Plan
- **Request:** P0 #12 — a serializable JSON `$data` format for a `TileMap` + a pure deterministic
  serializer/loader; MVP = the tile map only. **Autonomous goal run** (#12 of #11–#15): all phases
  auto-approved → `/commit` + merge.
- **Classification:** work pipeline, feature. A serializer + DTO + sample + tests. One slice, medium —
  scoped to the tile map (events/database from data are deferred follow-ups).
- **Forge recall:** the decisions favour **informed parity / MZ-era** (our own schema, not byte-compat);
  feature-research: organise content by type (maps, tilesets…); agentic-substrate §1: **"Project = DATA +
  MODULES"** (UI-authored `$data` + agent-authored modules) and names the **STJ source-generator** as a
  sanctioned no-reflection carve-out. **Discarded** the oathstar Tiled/TMX/Rust recall bleed (predecessor,
  not this repo). Ground: `TileMap` = flat row-major `Tile[]` (`Index=Y*W+X`); `Tile(int TilesetId, bool
  Blocking)`, `Tile.Empty=(-1,false)`. Totality pattern from the `.expect` parser (#10) +
  `PR-claude-total-parser-substring-overlap-001`.
- **Ticket:** a9c6b9f7-fc66-4f7a-b7d8-5cc99e427e16 (#12) — project a9ee8162 confirmed monorpgmaker.
- **AAR id:** 57a27055-9109-4cf5-968c-f9a9b8450423
- **EARS reviewed:** REQ-001..007.

## Phase 2 — Design

### Architecture / approach
- **Home:** `Engine.World` (next to `TileMap`/`Tile` — the serializer is tightly coupled to them). All new
  types are a **sim namespace** ⇒ the MRM determinism analyzer gates them; they are **int/bool only** so
  MRM-clean by construction.
- **Schema (per-cell objects, camelCase):**
  ```json
  { "width": 2, "height": 2, "tiles": [ { "tilesetId": 0, "blocking": false }, … ] }   // row-major, W*H entries
  ```
  Clearest, maps 1:1 to `Tile(TilesetId, Blocking)`, trivial golden. Int/bool only ⇒ deterministic.
- **System.Text.Json is in the shared framework** (net10.0) — **no package ref / no lock change**. Use the
  **source-generator** (`[JsonSerializable(typeof(TileMapData))] partial JsonSerializerContext` +
  `JsonSourceGenerationOptions(PropertyNamingPolicy=CamelCase)`) — AOT-safe, the sanctioned no-reflection
  carve-out; `WriteIndented = true` for human-readable `$data`. Serialize is deterministic (source-gen emits
  properties in declaration order).
- **`MapSerializer` (static, pure, framework-thin — NO file IO; the host reads the file):**
  - `Serialize(TileMap) → string` — loop cells row-major into a `TileMapData`, `JsonSerializer.Serialize`.
  - `Deserialize(string) → MapLoadResult` — `try JsonSerializer.Deserialize` (catch `JsonException` → typed
    `Failure`), then VALIDATE (each a killable branch): deserialized `null`; `width<=0`; `height<=0`; `tiles`
    null; `width*height != tiles.Length`. On success, build a `TileMap` via `SetTile`. **Never throws on the
    parse path** (§14; mirrors the `.expect` parser totality, PR-claude-total-parser-substring-overlap-001).
- **`MapLoadResult`** mirrors `ParseResult`: `Map?` + `Error?` (string reason), `Ok => Error is null`, static
  `Success(TileMap)` / `Failure(string)`.
- **Determinism / framework-thin:** int/bool ⇒ replay bit-identical; MRM stays green (no float/`Vector2`/
  `Dictionary`-foreach/clock/RNG). `Deserialize` takes a **string** — the Player/host owns `File.ReadAllText`.
- **Integration (LEAN MVP):** ship the serializer + a **committed sample `$data` fixture** + a **fidelity
  round-trip over the real `TracerRoom.Build().Map`** (proves a genuine authored map survives the format).
  **DEFER** rewiring `TracerRoom`'s runtime to load from `$data` (needs host-side file loading + an
  embedded-resource decision) → a follow-up ticket. `TracerRoom`/`WorldSim` are untouched ⇒ existing
  `TracerSliceTests` stay green (behaviour-preserving by non-modification).

### File manifest
| File | Change |
|---|---|
| `src/MonoRpgMaker.Engine/World/TileMapData.cs` | NEW — DTOs: `TileMapData { int Width; int Height; TileData[] Tiles }` + `TileData { int TilesetId; bool Blocking }` (public get/set for STJ; XML-doc) |
| `src/MonoRpgMaker.Engine/World/MapLoadResult.cs` | NEW — typed result: `Map?`/`Error?`/`Ok`/`Success`/`Failure` (mirrors `ParseResult`) |
| `src/MonoRpgMaker.Engine/World/MapSerializer.cs` | NEW — `static Serialize/Deserialize` + the `MapJsonContext` source-gen `JsonSerializerContext` (same file) |
| `tests/MonoRpgMaker.Engine.Tests/Fixtures/sample-map.json` | NEW — a committed small hand-authored sample `$data` map (the v1 "shipped" sample; a project-content home arrives with host loading) |
| `tests/MonoRpgMaker.Engine.Tests/MonoRpgMaker.Engine.Tests.csproj` | MODIFY — `<None Include="Fixtures/sample-map.json" CopyToOutputDirectory="PreserveNewest" />` |
| `tests/MonoRpgMaker.Engine.Tests/MapSerializerTests.cs` | NEW (Phase 4) — the plan below + a `AssertTileMapsEqual` structural-equality helper |

### Regression Test Plan
| T | REQ | Test (xUnit; isolated exact-count asserts) |
|---|---|---|
| T1 | 001 | `Serialize` a known 2×2 map → parse the output with `JsonDocument` → `width`=2, `height`=2, `tiles` has 4 entries with the right `tilesetId`/`blocking` (robust to formatting; kills the Serialize field/loop mutants) |
| T2 | 002 | Round-trip a hand-built **NON-SQUARE 4×3** map (varied: Floor/Wall/Lever/`Tile.Empty`) → `Serialize`→`Deserialize`→ `Ok` + **structurally equal** (dims + every cell) — kills the row-major `Index`/bounds mutants (W≠H) |
| T3 | 002/004 | **Fidelity:** round-trip `TracerRoom.Build().Map` (a real authored map) → equal |
| T4 | 003 | `Deserialize("{ not json")` → `!Ok`, `Error` non-null, **no throw** |
| T5 | 003 | `Deserialize("null")` → `!Ok` |
| T6 | 003 | width `0` and width `-1` → `!Ok` (both) |
| T7 | 003 | height `0` → `!Ok` |
| T8 | 003 | missing/`null` `tiles` → `!Ok` |
| T9 | 003 | count mismatch (`width*height != tiles.Length`) → `!Ok` |
| T10 | 004 | Load the committed `sample-map.json` (`File.ReadAllText` + `Deserialize`) → `Ok` + expected dims + specific cells |
| T11 | 005 | (gate build `-warnaserror`) `MapSerializer`/DTOs compile MRM-clean — implicit; the determinism is int/bool-by-design |
| — | 006 | framework-thin: `MapSerializer` has no `System.IO` usage — review |
| — | 007 | FULL `bin/gate.sh` green (coverage + Engine MSI ≥ 80) |
- **Engine MSI ≥ 80 (PR-claude-analyzer-mutation-isolated-asserts-001):** each validation branch (T4–T9) is
  its own `[Fact]`; the non-square round-trip (T2) kills the build-loop/`Index` mutants; T1 kills the Serialize
  mutants; `MapLoadResult` Ok/Fail covered by the ok-path (T2) + fail-path (T4) tests.

### Risks / decisions
- **STJ source-gen in a sim namespace** — confirmed MRM-clean (int/bool only); the source generator is part of
  the in-framework System.Text.Json (no package, no lock change). If the analyzer unexpectedly flags it,
  fall back to a hand-written reader/writer (still no reflection) — but not expected.
- **Sample lives as a TEST fixture for v1** (`CopyToOutputDirectory`) — the "ship to project content" home
  arrives with host-side `$data` loading (deferred follow-up). Noted so it isn't read as the final location.
- **DEFERRED follow-up (note for a future ticket):** placed-events + the database from `$data`; the
  `TracerRoom` runtime rewire to load its map from `$data`; tileset images.

## Phase 3 — Implement
- **Built (to the manifest):**
  - `World/TileMapData.cs` — `TileMapData { int Width; int Height; TileData[] Tiles = [] }` + `TileData { int
    TilesetId; bool Blocking }` (public get/set for STJ; int/bool only).
  - `World/MapLoadResult.cs` — the typed result mirroring `ParseResult` (`Map?`/`Error?`/`Ok`/`Success`/`Failure`).
  - `World/MapSerializer.cs` — `Serialize(TileMap)→string` (row-major) + `Deserialize(string)→MapLoadResult`
    (catch `JsonException`→typed Failure; 5 distinct validation branches: null doc / width≤0 / height≤0 / null
    tiles / count mismatch; then build via `SetTile`; **never throws on parse**) + the `MapJsonContext`
    **source-gen** `JsonSerializerContext` (`WriteIndented`, camelCase).
  - `tests/.../Fixtures/sample-map.json` — a committed 3×2 sample (wall/floor/lever row-major); the test
    csproj copies it to output (`CopyToOutputDirectory=PreserveNewest`).
- **STJ source-gen is MRM-clean:** `dotnet build -warnaserror` (the determinism analyzer runs over the
  generated `MapJsonContext` in the `Engine.World` sim namespace) → **0 MRM / 0 warnings / 0 errors**. The
  build compiling `MapJsonContext.Default.TileMapData` also proves the generator emitted the metadata, so the
  no-reflection carve-out is wired. **No package / lock change** (System.Text.Json is in-framework).
- Solution `-warnaserror` 0/0; `dotnet format` clean.
- **Deviation (note for Phase 4, not material):** the `data.Tiles is null` branch is only reachable via an
  EXPLICIT `"tiles": null` — an OMITTED `tiles` leaves the `= []` default (caught by the count-mismatch branch
  instead). So the null-tiles test must use explicit `"tiles": null`; the omitted case feeds the count test.

## Inspect (Phase 3.5)
- **Lenses:** 2 critics — (1) correctness/totality/determinism (general-purpose, **built + ran** hostile
  inputs), (2) design/purity/mutation (read-only).

  | # | Sev | Finding | Verdict | Resolution |
  |---|---|---|---|---|
  | F1 | HIGH | **Int-overflow defeats the count guard → totality hole.** `Width*Height` computed in int; `{"width":65536,"height":65536,"tiles":[]}` → 65536² overflows to 0, so `0 != 0` is false → the guard PASSES → the build loop indexes the empty `Tiles` → **IndexOutOfRangeException escapes** the `JsonException` catch. Critic 1 **dismissed this by reasoning** ("the count check rejects it first"); I **ran it** → confirmed the throw. | **REAL — FIXED** | `(long)data.Width * data.Height != data.Tiles.Length` (4294967296 != 0 → typed Failure). Re-ran: no throw + normal round-trip intact. `F-claude-int-overflow-defeated-count-guard-001` + `PR-claude-long-product-dimension-guard-001`. |
  | — | LOW | **Totality holds vs all OTHER hostile inputs** — `""`/`"   "`/`null`/`[]`/`{}`/`{bad`/missing-fields/explicit-`"tiles":null`/count-mismatch/dims/**deep-nest ×100k**(MaxDepth `JsonException`)/**float `tilesetId:1.5`**(Int32-bind `JsonException`) all → caught `JsonException` → typed Failure. Non-square **4×3 round-trip exact** (no x/y or W/H transposition). Serialize **byte-identical**. | **VERIFIED CLEAN** (by execution) | none |
  | — | note | No `TilesetId >= -1` validation / no dimension cap. | **ACCEPTED** | `Tile` accepts any int (`IsEmpty` when `<0`); the long-product count guard bounds the allocation to the JSON-sized `Tiles.Length` — over-validation is scope creep. |
  | — | clean | MRM-clean (source-gen, int/bool); nullable + XML-doc faithful `ParseResult` mirror; the 5 validation branches distinct/reachable/ordered; fixture correct (3×2 row-major); framework-thin (no `System.IO`). | **VERIFIED CLEAN** (both critics) | none |

- **Phase-4 Engine-MSI ≥ 80 kill-list (critic 2 + the fix):** T1 serialize-shape (`JsonDocument`); **T2 non-square
  4×3 round-trip** (kills the `(y*Width)+x` index mutants — CRITICAL); T3 tracer-fidelity (`TracerRoom.Build().Map`);
  T4 bad-JSON (catch + Failure prefix); T5 null-doc; T6 width≤0 (`0` AND `-1`); T7 height≤0; T8 explicit-null-tiles;
  T9 count-mismatch; **T-overflow** (`{"width":65536,"height":65536,"tiles":[]}` → Failure, no throw — kills a
  naive-int-product mutant); T10 sample-load. Each malformed case its OWN `[Fact]` (isolated asserts,
  PR-claude-analyzer-mutation-isolated-asserts-001).
- **Forge:** `failure-record F-claude-int-overflow-defeated-count-guard-001`; `prevention-rule-record
  PR-claude-long-product-dimension-guard-001`.

## Phase 4 — Validate
- **Tests added (`MapSerializerTests`, 12 `[Fact]`/`[Theory]` → 13 cases + an `AssertTileMapsEqual` helper):**
  T1 serialize-shape (`JsonDocument`), **T2 non-square 4×3 round-trip** (kills the `(y*Width)+x` index mutants),
  T3 tracer-fidelity (`Serialize(TracerRoom.Build().Map)` round-trips), T4/T4b malformed-JSON (reason + no-throw),
  T5 null-doc, T6 non-positive width (`0` **and** `-1`), T7 non-positive height, T8 explicit-null-tiles,
  T9 count-mismatch, **T-overflow** (`65536×65536` → typed Failure, **no throw** — guards the long-product fix),
  T10 sample-fixture load. Each malformed case is its own isolated test asserting the DISTINCT reason.
- **`dotnet test`: 236 passed, 0 failed** (+13).
- **`bin/gate.sh`: GREEN [full]** — 13 gates. Coverage **94.8%**; MSI **Engine 92.11%** (↑ from 90.67 — the
  serializer's branches/loops killed cleanly) **/ Abstractions 82.22% / Analyzers 91.18% / Editor 83.97%** (all
  ≥ 80). Receipt written.
- **Equivalent mutants (documented, not killable):** `width <= 0` ⇔ `width < 1` and `height <= 0` ⇔ `height < 1`
  for ints; `Error is null` ⇔ `Error == null`. None affect the ≥80 floor.
- **Pre-existing exclusions:** none. No new packages (System.Text.Json is in-framework).

## Phase 5 — Complete
- Docs / forge / ticket / archive:
