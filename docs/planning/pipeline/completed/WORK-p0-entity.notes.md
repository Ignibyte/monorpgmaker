# WORK-p0-entity — Notes

## Phase 1 — Plan
- **Request:** P0 #14 — the design-gated unified-entity slice. **AD-first** (resolve forks 1+2, affirm 3) +
  a minimal behaviour-preserving first cut. **Autonomous goal run** (#14 of #11–#15): all phases
  auto-approved → `/commit` + merge.
- **Classification:** work pipeline, feature, **design-gated** (the AD is the centerpiece). One slice, medium —
  kept minimal by deferring the migration.
- **Ground (verified):** `Entity` (abstract, on-grid Cell/Facing/TryStep; `IMovable`) → `Actor` (HP).
  **DEFINITIONS already exist** — `Database<T:IRecord>` (`Dictionary<int,T>`) + `ItemRecord`/`ActorRecord`
  (Engine.Data). The INSTANCE side is missing. `Database<T>.All => _byId.Values` is nondeterministic ⇒ the new
  instance store iterates DETERMINISTICALLY (List).
- **Recall (authoritative):** product-phasing **"Open architecture forks"** — Fork 1 inheritance→composition
  (grid/stats/movable/inventory = attachable capabilities); Fork 2 definition↔instance (immutable record
  flyweight + a stateful instance referencing it; editor authors definitions, runtime spawns instances, modules
  hook instances; sets up #15 save/load); Fork 3 tiles-stay-data (defer ECS — perf). D-0014 (compile-time
  wiring, not reflection; explicit ordering); principle 5 (determinism is the ENABLER for a mutable+stateful
  world — only debuggable if bit-replayable).
- **Ticket:** 21c9bd7d-b47a-4f2b-9b9e-79288937d90e (#14) — project a9ee8162 confirmed monorpgmaker.
- **AAR id:** 0c9aff97-6020-4031-b3af-c8bc682a5191
- **EARS reviewed:** REQ-001..008.

## Phase 2 — Design

### Architecture / approach
- All new types in **`Engine.Entities`** (no name clash — `IComponent`/`EntityInstance`/`EntityDefinition`/
  `StatsComponent`/`PositionComponent` are all free). The existing on-grid `Entity`/`Actor`/`IMovable` +
  `WorldSim`/`TracerRoom` are **UNTOUCHED** (behaviour-preserving; the AD documents the eventual migration as
  deferred). The first cut is standalone (not yet wired into the player).
- **`IComponent`** — a marker interface (Engine.Entities). Stays in Engine for v1; the AD notes the
  Abstractions-promotion path (agent-authored components later).
- **`EntityDefinition`** — `sealed record EntityDefinition(int Id, string Name) : IRecord` (reuses
  `Engine.Data.IRecord` so it can live in a `Database<EntityDefinition>`). Immutable = the flyweight template.
- **Components** — `sealed record` CLASSES (reference types, value-equality, immutable), int/`GridPoint` only
  (MRM-clean): `StatsComponent(int CurrentHp, int MaxHp)`, `PositionComponent(GridPoint Cell)` (uses
  `MonoRpgMaker.Abstractions.GridPoint`).
- **`EntityInstance`** — `sealed class`: `int Id`, `int DefinitionId`, a PRIVATE `IReadOnlyList<IComponent>`
  (List, insertion-order) exposed read-only as `Components` (deterministic; supports #15 save/load + the
  order test). API (plain `foreach` + `is T` — NO Dictionary, NO GetType/reflection, NO LINQ):
  - `static EntityInstance Spawn(int id, EntityDefinition definition, IEnumerable<IComponent> components)` —
    `ThrowIfNull(definition, components)`; `DefinitionId = definition.Id`; copies the components into a List.
  - `T? Get<T>() where T : class, IComponent` — first `is T` match, else null.
  - `bool Has<T>() where T : class, IComponent` — `Get<T>() is not null`.
  - `EntityInstance With<T>(T component) where T : class, IComponent` — `ThrowIfNull(component)`; new list =
    the existing **non-`T`** components (in order) **then** `component` (replace-same-type-or-append); returns a
    NEW `EntityInstance(Id, DefinitionId, next)` — the original is UNCHANGED (copy-on-write).
- **Determinism/MRM:** int/`GridPoint` records; the component set is a `List` iterated in insertion order; no
  Dictionary-foreach/reflection/LINQ/float/clock/RNG ⇒ replay bit-identical, MRM analyzer green over Engine.Entities.

### File manifest
| File | Change |
|---|---|
| `src/MonoRpgMaker.Engine/Entities/IComponent.cs` | NEW — the marker interface (+ XML-doc; Abstractions-promotion note) |
| `src/MonoRpgMaker.Engine/Entities/EntityDefinition.cs` | NEW — `sealed record EntityDefinition(int Id, string Name) : IRecord` |
| `src/MonoRpgMaker.Engine/Entities/StatsComponent.cs` | NEW — `sealed record StatsComponent(int CurrentHp, int MaxHp) : IComponent` |
| `src/MonoRpgMaker.Engine/Entities/PositionComponent.cs` | NEW — `sealed record PositionComponent(GridPoint Cell) : IComponent` |
| `src/MonoRpgMaker.Engine/Entities/EntityInstance.cs` | NEW — the instance: Id/DefinitionId/Components + Spawn/Get/Has/With |
| `tests/MonoRpgMaker.Engine.Tests/EntityInstanceTests.cs` | NEW (Phase 4) — the plan below |

### Regression Test Plan
| T | REQ | Test (xUnit; isolated exact-count asserts) |
|---|---|---|
| T1 | 001 | `EntityDefinition(7,"Slime")` — `Id`==7, `Name`=="Slime", and it IS an `IRecord` (assignable). |
| T2 | 002/003 | `Spawn(1, def, [stats, pos])` → `Id`==1, `DefinitionId`==def.Id, `Components.Count`==2. |
| T3 | 004 | `Get<StatsComponent>()` returns the attached stats; `Get<PositionComponent>()` when absent → null; `Has<StatsComponent>()` true, `Has<PositionComponent>()` false. |
| T4 | 005 | `With(new StatsComponent(5,10))` → the NEW instance's `Get<Stats>().CurrentHp`==5 AND the ORIGINAL's ==10 (copy-on-write; kills any in-place-mutation mutant). |
| T5 | 005 | `With` on an EXISTING type → exactly one `StatsComponent` after (replace); `With` on an ABSENT type → both components present (append). |
| T6 | 005 | Two `Spawn`s from one definition; `With` on instance A → instance B's stats UNCHANGED (independent state). |
| T7 | 004 | `Components` iteration order is deterministic insertion order; after a replace-`With`, the kept components stay in order then the new one last. |
| T8 | 002 | `Spawn(null def)` / `Spawn(.., null components)` / `With(null)` → `ArgumentNullException`. |
| — | 006/007/008 | MRM-clean build `-warnaserror`; existing tracer/sim tests green; FULL gate (Engine MSI ≥ 80). |
- **Engine MSI ≥ 80:** `Get`'s `foreach`+`is T` (T3 hit/miss); `With`'s non-`T` filter + append (T4/T5
  replace-vs-append; T4's original-unchanged kills a mutate-in-place mutant); `Spawn`'s `DefinitionId`
  assignment (T2); the `ThrowIfNull` guards (T8). Isolated exact-count asserts
  (PR-claude-analyzer-mutation-isolated-asserts-001).

### Risks / decisions
- **Intra-Engine dep** `Entities → Data` (`EntityDefinition : IRecord`) — fine (the layering rule is
  Project→Abstractions-only; intra-Engine references are allowed; NetArchTest unaffected).
- **`Get`/`With` use `where T : class`** — components are record CLASSES (reference types) so `T?` nullable
  returns + `is T` work; a record STRUCT component would need a different shape (not used in v1).
- **DEFERRED follow-ups (note for the AD + future tickets):** migrate the player/`Actor`/events/combat into the
  unified `EntityInstance` model; `Database<EntityDefinition>` + `$data`-backed definition authoring; the visual
  entity editor; #15 save/load serializes `EntityInstance` state.

## Phase 3 — Implement
- **Built (5 files, all `Engine.Entities`):** `IComponent` (marker); `EntityDefinition(int Id, string Name) :
  IRecord` (the flyweight definition); `StatsComponent(int CurrentHp, int MaxHp)` + `PositionComponent(GridPoint
  Cell)` (`: IComponent`, immutable records); `EntityInstance` (sealed class — `Id`/`DefinitionId`/`Components`
  + `Spawn`/`Get<T>`/`Has<T>`/`With<T>`). `Get`/`With` use plain `foreach` + `is T` (no Dictionary/reflection/
  LINQ); `With` is replace-same-type-or-append + copy-on-write (the original list is never mutated).
- **MRM-clean:** `dotnet build -warnaserror` → **0 MRM / 0 warnings / 0 errors** (the analyzer gates
  Engine.Entities; the component scan + int/GridPoint records are analyzer-clean). Format clean.
- The existing on-grid `Entity`/`Actor`/`IMovable`/`WorldSim`/`TracerRoom` are **UNTOUCHED** (compile +
  behaviour preserved). The first cut is standalone (not yet wired into the player — deferred per the AD).
- **Deviations:** none.

## Inspect (Phase 3.5)
- **Lenses:** 2 critics — (1) correctness/immutability/determinism (general-purpose, **built + ran**), (2)
  design/AD-shape/mutation (read-only). Both independently flagged the same CRITICAL.

  | # | Sev | Finding | Verdict | Resolution |
  |---|---|---|---|---|
  | F1 | CRITICAL | **`Components` leaked the live backing `List`** — typed `IReadOnlyList<IComponent>` but the runtime object was `List<IComponent>`. Critic 1 **proved by execution**: `((List<IComponent>)a.Components).Add(...)` mutated the instance (Count 1→2); `.Clear()` destroyed its state (`Get<Stats>()`→null). Breaks the immutable-for-replay contract. | **REAL — FIXED** | store + expose **`ReadOnlyCollection<IComponent>`** (`.AsReadOnly()` in Spawn + With). Verified: cast-to-`List` now throws `InvalidCastException`; copy-on-write intact. `F-claude-ireadonlylist-over-list-castable-to-mutable-001` + `PR-claude-readonly-collection-for-true-immutability-001`. |
  | — | LOW | **Both forks PROVEN** (composition — attachable components, no hierarchy; definition↔instance — id-flyweight, immutable def). Copy-on-write value semantics correct (`With` new-reads-5 / original-reads-10; replace-vs-append; defensive `Spawn` copy; two-instance independence). `Get`/`Has`/`With` correct + order-deterministic. MRM-clean (foreach+`is T`, no Dictionary/reflection/LINQ/float). Naming avoids the legacy `Entity` clash. | **VERIFIED CLEAN** (both critics) | none |
  | — | MEDIUM(defer) | `where T : class` is safe for v1 (components are record CLASSES); a future record-STRUCT component would box/need a different shape. | **DEFERRED — note in the AD** | none (v1 is class-only) |

- **Phase-4 Engine-MSI ≥ 80 kill-list (critic 2, +the fix):** `Get`'s `foreach`+`is T` (present-type hit / absent
  → null); `Has`'s `is not null`; `With`'s `is not T` filter (a KEPT other-component) + `next.Add(component)`
  append + **copy-on-write** (original unchanged); `Spawn`'s `DefinitionId = definition.Id` + the defensive copy;
  the 3 `ThrowIfNull` guards; **+ the immutability test** (`Components` can't be cast to a mutable `List`).
  Equivalent: the `Count + 1` capacity hint; the ambiguity/error strings. Isolated exact-count asserts.
- **Forge:** `failure-record F-claude-ireadonlylist-over-list-castable-to-mutable-001`; `prevention-rule-record
  PR-claude-readonly-collection-for-true-immutability-001`.

## Phase 4 — Validate
- **Tests added (`EntityInstanceTests`, 10):** definition-is-an-IRecord; Spawn references the definition +
  carries components; Get/Has by type (hit + null-miss); **With copy-on-write** (original unchanged); With
  replace (one of that type, new value) vs append (keeps the other); two-instance independence; deterministic
  insertion order (+ replace keeps order); null guards (×3); **`Components` can't be cast to a mutable `List`**
  (`Assert.Throws<InvalidCastException>` — guards the ReadOnlyCollection inspect fix).
- **`dotnet test`: 265 passed, 0 failed** (+10).
- **`bin/gate.sh`: GREEN [full]** — 13 gates. Coverage **95.0%**; MSI **Engine 87.18%** (the new EntityInstance
  killed cleanly) **/ Abstractions 82.22% / Analyzers 91.18% / Editor 83.97%** (all ≥ 80). Receipt written.
  The existing tracer/sim tests stay green (`Entity`/`Actor` untouched).
- **Fixed in-phase:** an `xUnit2013` warning (`Assert.Equal(1, …Count)` → `Assert.Single`).
- **Equivalent mutants (documented):** the `_components.Count + 1` capacity hint (List auto-grows). No floor impact.
- **Pre-existing exclusions:** none. No new packages (`ReadOnlyCollection` is in-framework).

## Phase 5 — Complete
- Docs / forge / ticket / archive:
