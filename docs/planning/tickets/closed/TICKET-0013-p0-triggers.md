---
title: TICKET-0013-p0-triggers
status: done
ticket: 0e3ab789-cc7f-48f5-ae0e-fc0854e8ee85
ticket_number: 13
type: feature
created: 2026-06-19
intake:
pipeline_spec: docs/planning/pipeline/completed/WORK-p0-triggers.spec.md
---

# TICKET-0013-p0-triggers

## Summary

The extensibility spine (the Drupal-hooks idea, agentic-substrate §2): (A) a new **ActionButton**
trigger kind + a player **Facing**, and (B) an explicit, **ordered, ambiguity-checked** hook
lifecycle — replacing WorldSim's implicit insertion-order dispatch, keeping replay bit-identical.
MVP; Autorun/Parallel + the full module-registration system are deferred follow-ups.

## EARS Requirements

| ID | EARS Requirement | Verification |
|---|---|---|
| REQ-001 | `EventTrigger` shall include an `ActionButton` kind in addition to `StepOn`. | unit test |
| REQ-002 | The player shall track a `Facing` direction, set to the attempted direction on every `MovePlayer` call (including a blocked move — turn in place). | unit test |
| REQ-003 | `WorldSim.PressAction()` shall dispatch `ActionButton`-triggered events at the faced cell (`Player.Cell + Facing` step) and apply their outcomes; a step shall NOT fire `ActionButton` and `PressAction` shall NOT fire `StepOn`. | unit test |
| REQ-004 | `IMapEvent` shall expose an `Order` (default `0`); when multiple events share a (cell, trigger) group, WorldSim shall dispatch them in ascending `Order`. | unit test |
| REQ-005 | When two or more events share the same cell + trigger with equal `Order`, sim build/load shall surface a typed ambiguity error — never a silent order, never a throw on the validation path (§14). | unit test |
| REQ-006 | The dispatch order shall be deterministic + total (MRM-clean; no float/clock/RNG/nondeterministic enumeration) so replay stays bit-identical. | build `-warnaserror` + review |
| REQ-007 | Existing `StepOn` behaviour shall be preserved. | `TracerSliceTests` green |
| REQ-008 | The slice shall pass the FULL `bin/gate.sh` (coverage + mutation; Engine MSI ≥ 80). | gate run (FULL) |

## Scope

- **In:** `ActionButton` trigger; player `Facing` + `WorldSim.PressAction()`; `IMapEvent.Order` (non-breaking
  default-interface member); an ordered, ambiguity-checked dispatch (typed result); tests. FULL gate green.
- **Out (deferred follow-ups):** Autorun/Parallel triggers; the full agent-authored module-registration /
  subscription system; the visual event-placement UI; runtime save/load (#15); the unified-entity model (#14).

## Notes

- Forge ticket: 0e3ab789-cc7f-48f5-ae0e-fc0854e8ee85 (#13, project monorpgmaker `a9ee8162-3cfd-4c99-be5c-bbdb4be32b10`)
- Related: `docs/agentic-substrate.md` (§2 hook lifecycle; §1 the seam rings), `docs/product-phasing.md` (#13)
- Builds on: `IMapEvent`/`EventTrigger` (Abstractions, net10 — default-interface-members OK), `WorldSim`
  dispatch, `Direction`/`DirectionExtensions.ToStep` (Engine.World)
- Reuse PRs: PR-claude-total-parser-substring-overlap-001, PR-claude-long-product-dimension-guard-001,
  PR-claude-analyzer-mutation-isolated-asserts-001
- Active pipeline: docs/planning/pipeline/completed/WORK-p0-triggers.spec.md
