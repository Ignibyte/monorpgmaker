---
pipeline_id: 6f0b1c79-fb0c-4e7f-80e2-4685bf1a36fc
title: WORK-p0-entity
ticket: 21c9bd7d-b47a-4f2b-9b9e-79288937d90e
type: work
intake:
notes: WORK-p0-entity.notes.md
status: Phase 5 — Complete PASS
---

# WORK-p0-entity

> Pipeline spec (always-loaded contract). Detail per phase lives in the paired `.notes.md`.

## Work Spec
- **Title:** P0 #14 — unified-entity model: the **AD** (composition over inheritance; definition↔instance
  flyweight; tiles stay data) + a **minimal behaviour-preserving first cut** (an `EntityDefinition` + a stateful
  instance type with a deterministic typed component set + one worked proof). The design-gated foundation for
  "everything = a stateful entity."
- **Scope:**
  *In* — the AD; `EntityDefinition` (stable-id template); an instance type (Id + definition ref + a
  `List`-backed deterministic typed component set: `Get<T>`/`Has<T>`/`With`, immutable copy-on-write); one proof
  (spawn from a definition, attach + read a component, two instances independent). Existing behaviour preserved.
  FULL gate green.
  *Out (deferred)* — migrating the player/events/database/combat into the model; the visual entity editor;
  `$data`-backed definition authoring; save/load (#15).
- **Systems:** **engine/entities** (the new instance + component types) · consumes/affirms **engine/data**
  (definitions = `IRecord` flyweights) · the existing on-grid `Entity`/`Actor`/`WorldSim` UNTOUCHED · tests.

## Acceptance Criteria (EARS)
| ID | EARS Requirement | Verification |
|---|---|---|
| REQ-001 | The engine shall provide an immutable `EntityDefinition` (stable-id template) distinct from a stateful instance. | unit test |
| REQ-002 | The engine shall provide an entity instance type carrying a stable Id, a definition reference, and a deterministic typed component set (composition). | unit test |
| REQ-003 | Spawning an instance from a definition shall reference that definition + carry its initial components. | unit test |
| REQ-004 | A component shall be attach/read by type (`Get<T>`/`Has<T>`) with deterministic iteration order. | unit test |
| REQ-005 | `With(component)` shall return a NEW instance, leaving the original unchanged; two instances of one definition shall have independent state. | unit test |
| REQ-006 | The model shall be deterministic + MRM-clean (no float/clock/RNG/Dictionary-foreach in the sim path). | build `-warnaserror` + review |
| REQ-007 | The existing on-grid `Entity`/`Actor` + `WorldSim`/`TracerRoom` behaviour shall be preserved. | existing tests green |
| REQ-008 | The slice shall pass the FULL `bin/gate.sh` (coverage + mutation; Engine MSI ≥ 80). | gate run (FULL) |

## Locked-In Decisions (the AD)
- **Composition over inheritance** — capabilities (placed-on-grid / has-stats / movable / holds-inventory) are
  ATTACHABLE components, not base-class fields.
- **Definition ↔ instance (flyweight)** — an immutable `EntityDefinition` (the template, an `IRecord`-style
  record) + a stateful instance that REFERENCES it by id and carries mutable per-instance component state.
- **Tiles stay data** (affirm fork 3) — the `TileMap` stays the spatial substrate; full ECS deferred.
- **A deterministic typed component set** — `List`-backed, insertion-order iteration (NO Dictionary-foreach),
  `Get<T>`/`Has<T>`/`With`; immutable records + copy-on-write. MRM-clean.
- **v1 = the core + ONE proof.** The migration of the existing `Entity`/`Actor`/events/database/combat is
  DEFERRED (follow-ups). `IComponent` stays in Engine for v1 (Abstractions-promotion path noted in the AD).
  Definitions hand-built in code for v1 (`$data`-backed deferred).
- **Out:** migration; visual editor; `$data` definition authoring; save/load (#15).

### OPEN — Phase 2 (design decides)
- **(a)** Final component-store shape — `List<IComponent>` + `Get<T>` linear scan + `With` (append-or-replace);
  immutable record instance.
- **(b)** The NEW instance type NAME — the `Entity` class name is TAKEN; pick a non-clashing name
  (e.g. `EntityInstance` / `GameEntity`) and leave the old base untouched.
- **(c)** Home — `Engine.Entities` vs a `Engine.Entities/Components` sub-area; `IComponent` in Engine for v1.
- **(d)** Reuse `IRecord` for the definition vs a new `EntityDefinition`; hand-built definition for v1.

## Linked Artifacts
- Design docs: `docs/product-phasing.md` ("Open architecture forks"; principle 4), `docs/decisions.md` (D-0014, D-0016), `docs/agentic-substrate.md`
- Ticket doc: docs/planning/tickets/open/TICKET-0014-p0-entity.md
- Forge ticket: 21c9bd7d-b47a-4f2b-9b9e-79288937d90e (#14, project monorpgmaker `a9ee8162-3cfd-4c99-be5c-bbdb4be32b10`)
- Builds on: `Entity`/`Actor`/`IMovable` (Engine.Entities), `Database<T>`/`IRecord`/`ActorRecord`/`ItemRecord` (Engine.Data)
- Reuse: PR-claude-analyzer-mutation-isolated-asserts-001; the prior AD shapes
- Ground-truth anchors: `src/MonoRpgMaker.Engine/Entities/{Entity,Actor,IMovable}.cs`, `src/MonoRpgMaker.Engine/Data/Database.cs`
- AAR: 0c9aff97-6020-4031-b3af-c8bc682a5191

## Phase Plan
| Phase | Status |
|---|---|
| 1 — Plan | PASS |
| 2 — Design | PASS |
| 3 — Implement | PASS |
| 3.5 — Inspect | PASS |
| 4 — Validate | PASS |
| 5 — Complete | PASS |
