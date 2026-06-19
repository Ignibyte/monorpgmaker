# WORK-p0-saveload — Notes

## Phase 1 — Plan
- **Request:** P0 #15 — the save/load capstone: a deterministic, total `$game` save serializer + replay-
  equivalence. **Autonomous goal run** (#15 of #11–#15 — THE FINAL ONE): all phases auto-approved → `/commit`
  + merge → the goal is COMPLETE.
- **Classification:** work pipeline, feature. A `SaveState` DTO + `SaveSerializer` + a `GameState`
  snapshot/restore + tests. One slice, medium — reuses the #12 serialization machinery.
- **Ground (verified):** `GameState` = private `Dictionary` switches/counters; `Set`/`Add` `[StateMutator]`,
  `Get`/`GetCount` read; **no read accessor** (the slice adds one). **`IRandom.State` (ulong) IS capturable**
  + `new SplitMix64Random(state)` reproduces the continuation — but the tracer doesn't USE RNG yet (RNG
  round-trips in isolation). `Actor.Cell`/`Facing` are private-set (position restored via the `Actor` ctor;
  facing needs a seam OR defer).
- **Recall:** the #12 `MapSerializer`/`MapLoadResult` pattern (STJ source-gen + totality + sorted-keys
  determinism); the totality + long-product + readonly-collection + isolated-asserts PRs; D-0016 (determinism
  is the enabler — a save is only meaningful if replay is bit-identical).
- **Ticket:** c0095793-531e-4828-8097-93be968ffe53 (#15) — project a9ee8162 confirmed monorpgmaker.
- **AAR id:** b124ed64-9643-48a3-8f33-2ca72393f686
- **EARS reviewed:** REQ-001..009.

## Phase 2 — Design

### Architecture / approach
- **THE snapshot/MRM decision — RESOLVED (option 1):** the determinism analyzer's `IsUnorderedEnumeration`
  flags ONLY `Dictionary`/`Dictionary.Keys`/`Dictionary.Values`/`HashSet`; `DeterminismDiagnostics` states
  outright that **`SortedDictionary`/`List`/arrays are allowed**. So switch `GameState`'s `_switches`/`_counters`
  to **`SortedDictionary<string,_>(StringComparer.Ordinal)`** — enumeration is then sorted + deterministic +
  analyzer-clean, the save snapshot is sorted for free, and `Get`/`Set`/`GetCount`/`Add` are unchanged
  (TryGetValue + indexer — behaviour-preserving; the internal order was never observable). All new types live in
  the gated `Engine.Sim` (MRM-enforced); the serializer consumes sorted snapshots (no Dictionary-foreach) so it
  is MRM-clean.
- **`GameState` (modified):** `_switches`/`_counters` → `SortedDictionary(Ordinal)`; add sorted read accessors
  `IReadOnlyList<KeyValuePair<string,bool>> SwitchEntries` + `…<string,int> CounterEntries` (built `new
  List<…>(_sortedDict).AsReadOnly()` — sorted copy, no mutable leak, PR-claude-readonly-collection); add a
  `static GameState Restore(IReadOnlyList<KeyValuePair<string,bool>> switches, IReadOnlyList<KeyValuePair<string,int>>
  counters)` (a FRESH GameState — `Set` each switch, `Add` each counter from 0 so the value == the saved value;
  `[StateMutator]`-clean). Stays persistence-agnostic (no `SaveState` dependency).
- **`SaveState` DTO** (mirror #12's TileMapData; public get/set for STJ): `int Version`, `SwitchEntry[] Switches`,
  `CounterEntry[] Counters`, `int PlayerX`, `int PlayerY`, `int Facing` (the `Direction` enum int), `ulong
  RngState`. `SwitchEntry{string Key; bool Value}`, `CounterEntry{string Key; int Value}`. STJ serializes `ulong`.
- **`SaveSerializer`** (static, mirror MapSerializer): a `[JsonSerializable(typeof(SaveState))] SaveJsonContext`
  (WriteIndented, camelCase). `Serialize(GameState, Point playerCell, Direction facing, ulong rngState) → string`
  (maps the sorted `SwitchEntries`/`CounterEntries` to the DTO arrays via index loops — no foreach/LINQ).
  `Deserialize(string) → SaveLoadResult` — total: catch `JsonException` → typed Failure; validate null doc /
  `Version != CurrentVersion` / null `Switches`/`Counters` → Failure; never throws on the parse path.
- **`SaveLoadResult`** mirrors `MapLoadResult` (`Ok` + the parsed `SaveState` | `Error`). The consumer restores:
  `GameState.Restore(map(save.Switches), map(save.Counters))`; player position/facing/rng are read off the
  SaveState (the host reconstructs the Actor — see risks).
- **Determinism:** sorted keys ⇒ byte-identical output for an equal state (REQ-002 is trivially true with
  SortedDictionary). Framework-thin (string in/out; the host owns the file). MRM-clean.

### File manifest
| File | Change |
|---|---|
| `src/MonoRpgMaker.Engine/Sim/GameState.cs` | MODIFY — `SortedDictionary(Ordinal)` storage; `SwitchEntries`/`CounterEntries` sorted accessors; `static Restore(...)` |
| `src/MonoRpgMaker.Engine/Sim/SaveState.cs` | NEW — `SaveState` + `SwitchEntry` + `CounterEntry` DTOs |
| `src/MonoRpgMaker.Engine/Sim/SaveSerializer.cs` | NEW — `Serialize`/`Deserialize` + the `SaveJsonContext` source-gen context |
| `src/MonoRpgMaker.Engine/Sim/SaveLoadResult.cs` | NEW — the typed result (mirrors `MapLoadResult`) |
| `tests/MonoRpgMaker.Engine.Tests/SaveSerializerTests.cs` | NEW (Phase 4) — the plan below |

### Regression Test Plan
| T | REQ | Test (xUnit; isolated exact-count asserts) |
|---|---|---|
| T1 | 001 | Serialize a populated GameState (+ position/facing/rng) → parse with `JsonDocument` → version, `switches`/`counters` arrays, `playerX`/`playerY`, `facing`, `rngState` present + correct. |
| T2 | 002 | Two GameStates with the SAME switches inserted in DIFFERENT orders → `Serialize` → **byte-identical** strings (SortedDictionary ⇒ sorted). |
| T3 | 003 | Round-trip: Serialize → Deserialize → `GameState.Restore` → `Get`/`GetCount` identical for every key (incl. a false/zero default for an absent key). |
| T4 | 004 | Malformed → typed Failure, no throw — each its own `[Fact]`: `"{bad"`, `"null"`, unsupported `version`, null `switches`. |
| T5 | 005 | RNG round-trip: draw N from a `SplitMix64Random`, capture `State`, Serialize+Deserialize it, `new SplitMix64Random(restored)` → the IDENTICAL next `NextBits()` sequence as the original's continuation. |
| T6 | 006 | Replay-equivalence: drive tracer A to open the door (step the lever → `LeverEvent.DoorSwitch`), `Serialize` A's GameState, restore into a fresh tracer B (`Set` the saved switches, `SyncDoors`) → `B.State.Get(LeverEvent.DoorSwitch)`==true AND B's door tile == the open tile (matches A). |
| T7 | 002 | `GameState.SwitchEntries` is sorted (Ordinal) + can't be cast to a mutable `List` (the AsReadOnly guard). |
| — | 007/008/009 | MRM-clean build `-warnaserror`; existing GameState/tracer tests green; FULL gate (Engine MSI ≥ 80). |
- **Engine MSI ≥ 80:** Serialize's field/index-loop mapping; Deserialize's validation branches (null doc /
  version / null arrays / JsonException); `Restore`'s Set/Add loops; `SwitchEntries`/`CounterEntries` sort+copy;
  `SaveLoadResult` Ok/Fail. Isolated exact-count asserts (PR-claude-analyzer-mutation-isolated-asserts-001).

### Risks / decisions
- **`SortedDictionary` in `GameState`** — behaviour-preserving (only the never-observed internal order changes);
  confirmed analyzer-allowed (the diagnostic names it explicitly). The existing GameState tests guard it.
- **Player position/facing RESTORE is DEFERRED** — the SaveState CAPTURES position/facing (they round-trip +
  are asserted), but restoring them into a live `Actor` (Cell/Facing are private-set) is a host concern (the
  host reconstructs the Actor at the saved cell via its ctor). The replay-equivalence proof (REQ-006) is on the
  GameState (the door), which is the determinism payoff; a full continued-inputs tracer reconstruction is a
  deferred follow-up (TracerRoom encapsulates its parts).
- **DEFERRED follow-ups:** EntityInstance/$data serialization (polymorphic components); the file/slot UI;
  migration logic (the `Version` field is present, unused); wiring RNG into the sim loop; the Actor
  position/facing restore seam.

## Phase 3 — Implement
- **Built (to the manifest):**
  - `Sim/GameState.cs` (modified) — `_switches`/`_counters` → `SortedDictionary(StringComparer.Ordinal)`;
    `SwitchEntries`/`CounterEntries` sorted read accessors (`new List<…>(_sorted).AsReadOnly()` — sorted copy,
    no mutable leak); `static Restore(switches, counters)` (fresh GameState; Set + Add-from-0).
  - `Sim/SaveState.cs` — `SaveState` (version + `SwitchEntry[]`/`CounterEntry[]` + position + facing + RNG
    `ulong`) + the entry DTOs.
  - `Sim/SaveLoadResult.cs` — the typed result, mirroring `MapLoadResult`.
  - `Sim/SaveSerializer.cs` — `Serialize`/`Deserialize` (total; catch `JsonException` + 4 distinct validation
    branches: null doc / version / null switches / null counters) + the `SaveJsonContext` source-gen context.
    Index loops map the sorted snapshots to the DTO arrays (no foreach/LINQ).
- **MRM-clean:** `dotnet build -warnaserror` → **0 MRM / 0 warnings / 0 errors** (the analyzer gates
  `Engine.Sim`; the `SortedDictionary`, the index loops, and the generated `SaveJsonContext` are all clean —
  the analyzer explicitly permits `SortedDictionary`). Format clean. **No package/lock change** (STJ in-framework).
- The `SortedDictionary` storage change is **behaviour-preserving**: the **38 existing GameState/tracer/outcome
  tests pass** (same `Get`/`Set`/`GetCount`/`Add` API; the never-observed internal order is the only change).
- **Deviations:** none.

## Inspect (Phase 3.5)
- **Lenses:** 2 critics — (1) correctness/totality/determinism (general-purpose, **built + ran 18 probes**),
  (2) design/pattern/mutation (read-only).

  | # | Sev | Finding | Verdict | Resolution |
  |---|---|---|---|---|
  | F1 | HIGH | **End-to-end totality hole** — `Deserialize` validated the null switch/counter ARRAYS but not the ELEMENTS or keys. A crafted save `{"switches":[null]}` / `{"switches":[{"key":null}]}` passed as **Ok** (STJ honours JSON `null` over the DTO default), then `GameState.Restore` crashed with NRE / ArgumentNullException. Critic 1 **proved by running**; the read-only critic scored it clean (**MISSED it**). | **REAL — FIXED** | `Deserialize` now rejects any null entry or null key (index loops → typed Failure) so the Ok result is safe to Restore. Verified: `[null]`/`[{key:null}]` → !Ok, no throw; valid saves still round-trip. `F-claude-deserialize-validated-container-not-elements-001` + `PR-claude-validate-deserialized-elements-not-just-container-001`. |
  | — | LOW | **Totality holds vs ALL other hostile inputs** — `""`/`"{bad"`/`null`/`[]`/`{}`/unsupported-version/null-arrays + **ulong overflow / negative / float `rngState` + float `playerX`** all → caught `JsonException` → typed Failure (the #12/#14 hole class is ABSENT — no non-`JsonException` escapes). **Byte-deterministic** output (different insert orders → identical save). `SwitchEntries` cast-to-`List` throws (AsReadOnly). **RNG round-trip** (restored generator → identical Next* sequence). **Replay-equivalence** (restore a saved door switch into a fresh tracer → the door re-opens). | **VERIFIED CLEAN** (by execution) | none |
  | F2 | LOW | **Duplicate counter keys** in a crafted save → `Restore` `Add`-accumulates (k:1,k:2 → 3). | **ACCEPTED — defer** | Legit saves are unique-key (SortedDictionary snapshot); switches are last-wins (`Set`); only a hand-crafted save trips it. De-dup would need a sort-scan (HashSet is MRM-banned) — not worth it for v1. Noted. |
  | — | clean | Pattern faithful to #12 MapSerializer/MapLoadResult; §14 (nullable, XML-doc, typed result); MRM-clean (SortedDictionary **confirmed analyzer-permitted** — the diagnostic names it; index loops; the generated context). | **VERIFIED CLEAN** (both critics) | none |

- **Phase-4 Engine-MSI ≥ 80 kill-list:** Serialize's two index loops (bounds + Key/Value copies) + the field
  assignments (Version/PlayerX/PlayerY/Facing/RngState); Deserialize's **6** validation branches (null-doc /
  version `!=` / null-switches / null-counters / **null-switch-entry** / **null-counter-entry**) + the
  JsonException catch; Restore's Set/Add loops + the 2 ThrowIfNull; `SwitchEntries`/`CounterEntries` (AsReadOnly
  + sort); `SaveLoadResult` Ok/Fail. Equivalent: the message-prefix strings. Isolated exact-count asserts.
- **Forge:** `failure-record F-claude-deserialize-validated-container-not-elements-001`; `prevention-rule-record
  PR-claude-validate-deserialized-elements-not-just-container-001`.

## Phase 4 — Validate
- **Tests added (`SaveSerializerTests`, 7 methods → 15 cases):** serialize-shape (all fields via `JsonDocument`);
  **determinism** (same state, different insert order → byte-identical); round-trip (Restore → every value +
  absent-key defaults); **malformed → typed Failure, no throw** (9 `[Theory]` rows incl. the inspect-fix
  **null-entry/null-key** cases); **RNG round-trip** (restored generator → identical `NextBits` continuation);
  **replay-equivalence** (drive the tracer onto the lever → save → restore into a FRESH tracer → `SyncDoors` →
  the door re-opens + the whole map matches); `SwitchEntries` sorted + cast-immutable.
- **`dotnet test`: 280 passed, 0 failed** (+15).
- **`bin/gate.sh`: GREEN [full]** — 13 gates. Coverage **92.7%**; MSI **Engine 83.90%** (the save/load logic
  killed cleanly) **/ Abstractions 82.22% / Analyzers 91.18% / Editor 83.97%** (all ≥ 80). Receipt written.
  The 38 existing GameState/tracer tests stay green (the SortedDictionary change is behaviour-preserving).
- **Equivalent mutants (documented):** the Deserialize message-prefix strings. No floor impact.
- **Pre-existing exclusions:** none. No new packages (System.Text.Json + SortedDictionary are in-framework).
- **The determinism capstone is proven:** a saved $game state restored into a fresh sim is replay-equivalent.

## Phase 5 — Complete
- Docs / forge / ticket / archive:
