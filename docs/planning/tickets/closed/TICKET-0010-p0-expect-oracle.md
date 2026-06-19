---
title: TICKET-0010-p0-expect-oracle
status: done
ticket: d1725d2b-edc1-4c8f-bd1d-044bb8b4a7c8
ticket_number: 10
type: feature
created: 2026-06-18
intake:
pipeline_spec: docs/planning/pipeline/completed/WORK-p0-expect-oracle.spec.md
---

# TICKET-0010-p0-expect-oracle

## Summary

The **D-0017 / agentic-substrate §7 `.expect` oracle** — make intent **executable** and
close the self-grading gap (the 12 gates verify determinism / purity / layering /
invariants — none is *correctness*). Each handler module ships a `<module>.expect` table:
human-confirmable **input-state → expected `Outcome` rows** in the shared outcome
vocabulary, **authored in a step distinct from the implementing agent**. A new **gate #13**
runs every row against the **real** handler and requires it to deterministically reproduce
the expected outcomes; a mismatch is a structured **MRM0006** (expectation-mismatch) that
fails the gate — rejecting a module whose only tests are the agent's own output-mutations.

## Why

It builds directly on the just-landed chassis: the pure event handlers
(`IMapEvent.Run(IEventContext) → IReadOnlyList<Outcome>`) are runnable as functions, and the
**value-equal** `Outcome` DU (#8) makes "compare returned outcomes to expected" trivial. The
determinism analyzer (#7) + the outcome-return purity analyzer (#9) already guarantee the
handler is deterministic + pure, so the oracle's reproduce-every-row requirement is
well-founded. This is the highest-leverage remaining P0 correctness guarantee.

## EARS Requirements

| ID | EARS Requirement | Verification |
|---|---|---|
| REQ-001 | A `<module>.expect` file shall express, per row, an input game-state (named switches/counters) and the ordered `Outcome` sequence the handler is expected to return. | format review + parser unit test |
| REQ-002 | When the oracle evaluates a row, it shall seed a `GameState` from the row's input, invoke the handler's `Run` through an `EventContext`, and value-equal compare the returned outcomes (kind + payload + order) to the expected. | runner unit test |
| REQ-003 | When a handler's returned outcomes match the row, the oracle shall report success; when they differ (count/kind/payload/order), it shall emit an `MRM0006` expectation-mismatch naming the module, the row, and the expected-vs-actual outcomes. | runner unit test (match + each mismatch kind) |
| REQ-004 | The oracle shall ship `.expect` tables for `LeverEvent` and `ChestEvent` covering their give-once behavior, and the real handlers shall reproduce every row. | the authored tables + a green oracle run |
| REQ-005 | `bin/gate.sh` shall run the `.expect` oracle as a distinct gate step (gate #13) that fails the gate (non-zero) on any `MRM0006` mismatch. | gate run: green on match; red on a seeded-mismatch row |
| REQ-006 | `MRM0006` shall use the `MRM0xxx` validator band (distinct from the `MRM1xxx` analyzer band). | review |
| REQ-007 | The oracle's evaluation path shall be deterministic (no float/clock/RNG; the comparison is over value-equal records) and respect the layering. | review + the determinism analyzer (green) |
| REQ-008 | The slice shall pass the FULL `bin/gate.sh` (coverage + mutation on the new parser/runner code). | gate run (FULL) |

## Scope

- **In:** the `.expect` format + a parser; the runner (seed `GameState` → `EventContext` → `Run` →
  value-equal compare); the `MRM0006` mismatch report; `.expect` tables for `LeverEvent` + `ChestEvent`;
  the gate #13 wiring in `bin/gate.sh`. FULL gate green.
- **Out:** the generator/validator DAG (`MRM0001`–`MRM0005` — premature, no module dependency graph
  yet); the scaffolder; battle/P2 handlers + their effect outcomes; the multi-arch/AOT replay-hash gate;
  surfacing `.expect` as a GUI fill-in grid (authoring is manual text for now); any `MRM0006` code-fix
  (you can't auto-fix intent).

## Notes

- Forge ticket: d1725d2b-edc1-4c8f-bd1d-044bb8b4a7c8 (#10, project monorpgmaker `a9ee8162-3cfd-4c99-be5c-bbdb4be32b10`)
- Related docs: `docs/agentic-substrate.md` (§7 the `.expect` oracle / gate #13; §2 MRM0006 in the validator band; §10 the self-grading risk), `docs/decisions.md` (D-0017, D-0020), `docs/roadmap.md` (P0 remaining)
- Builds on: WORK-p0-outcome-vocabulary (#8 — the value-equal `Outcome` DU + the pure handlers), WORK-p0-sim-determinism-analyzer (#7) + WORK-p0-outcome-return-analyzer (#9) (the handler is deterministic + pure)
- Reuse ADs: AD-claude-outcome-vocabulary-shape-001
- Promoted from intake: none (LOCAL roadmap; user chose ".expect oracle first" at the P0 fork)
- Active pipeline: docs/planning/pipeline/active/WORK-p0-expect-oracle.spec.md
