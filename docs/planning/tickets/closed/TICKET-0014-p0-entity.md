---
title: TICKET-0014-p0-entity
status: done
ticket: 21c9bd7d-b47a-4f2b-9b9e-79288937d90e
ticket_number: 14
type: feature
created: 2026-06-19
intake:
pipeline_spec: docs/planning/pipeline/completed/WORK-p0-entity.spec.md
---

# TICKET-0014-p0-entity

## Summary

The design-gated unified-entity slice. **Primary deliverable: an AD** resolving the two open
architecture forks (composition over inheritance; definition↔instance flyweight) + affirming
fork 3 (tiles stay data). Then a **minimal, behaviour-preserving first cut**: an immutable
`EntityDefinition` + a stateful entity-instance type carrying a deterministic typed component
set (composition), with one worked proof. The full migration of the player/events/database/
combat is deferred to follow-ups.

## EARS Requirements

| ID | EARS Requirement | Verification |
|---|---|---|
| REQ-001 | The engine shall provide an immutable `EntityDefinition` (a stable-id template) distinct from a stateful entity instance. | unit test |
| REQ-002 | The engine shall provide an entity instance type carrying a stable Id, a reference to its definition, and a deterministic typed component set (composition, not inheritance). | unit test |
| REQ-003 | Spawning an instance from a definition shall produce an instance referencing that definition with its initial components. | unit test |
| REQ-004 | A component shall be attachable + readable by type (`Get<T>`/`Has<T>`) with deterministic iteration order (no nondeterministic enumeration). | unit test |
| REQ-005 | `With(component)` shall return a NEW instance carrying the component, leaving the original unchanged (copy-on-write); two instances of one definition shall have independent state. | unit test |
| REQ-006 | The model shall be deterministic + MRM-clean (no float/clock/RNG/Dictionary-foreach in the sim path). | build `-warnaserror` + review |
| REQ-007 | The existing on-grid `Entity`/`Actor` + `WorldSim`/`TracerRoom` behaviour shall be preserved. | existing tests green |
| REQ-008 | The slice shall pass the FULL `bin/gate.sh` (coverage + mutation; Engine MSI ≥ 80). | gate run (FULL) |

## Scope

- **In:** the **AD** (composition + definition↔instance; affirm tiles-stay-data); a minimal unified-entity core
  (`EntityDefinition` + an instance type + a deterministic typed component set with `Get<T>`/`Has<T>`/`With`) +
  one worked proof. Existing behaviour preserved. FULL gate green.
- **Out (deferred follow-ups):** migrating the player/events/database/combat into the unified model; the visual
  entity editor; `$data`-backed definition authoring; runtime save/load (#15).

## Notes

- Forge ticket: 21c9bd7d-b47a-4f2b-9b9e-79288937d90e (#14, project monorpgmaker `a9ee8162-3cfd-4c99-be5c-bbdb4be32b10`)
- The AD is the centerpiece — recorded via `architecture-decision-record` at complete.
- Related: `docs/product-phasing.md` ("Open architecture forks", principle 4 unified entity), `docs/decisions.md`
  (D-0014 Drupal layering / compile-time wiring; D-0016 determinism), `docs/agentic-substrate.md`
- Builds on: `Entity`/`Actor` (Engine.Entities), `Database<T>`/`IRecord`/`ActorRecord`/`ItemRecord` (Engine.Data)
- Reuse PRs: PR-claude-analyzer-mutation-isolated-asserts-001; the prior AD shapes (outcome/oracle/scaffolder/map-data/hook-lifecycle)
- Active pipeline: docs/planning/pipeline/completed/WORK-p0-entity.spec.md
