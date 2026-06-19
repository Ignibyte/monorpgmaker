---
title: TICKET-0015-p0-saveload
status: done
ticket: c0095793-531e-4828-8097-93be968ffe53
ticket_number: 15
type: feature
created: 2026-06-19
intake:
pipeline_spec: docs/planning/pipeline/completed/WORK-p0-saveload.spec.md
---

# TICKET-0015-p0-saveload

## Summary

The capstone of #11–15: serialize the deterministic runtime (`$game`) state to JSON and load it
back — proving the determinism foundation pays off (save → load → **replay-equivalent**). A
`SaveState` (version + sorted switches/counters + player position/facing + RNG state) + a total,
deterministic `SaveSerializer`, reusing the #12 STJ source-gen pattern.

## EARS Requirements

| ID | EARS Requirement | Verification |
|---|---|---|
| REQ-001 | `SaveSerializer.Serialize` shall produce a JSON `$game` save capturing the version, the switches + counters, the player position + facing, and the RNG state. | unit test |
| REQ-002 | `Serialize` shall be deterministic — the same logical state produces byte-identical output regardless of switch/counter insertion order (sorted keys). | unit test |
| REQ-003 | A `Serialize → Deserialize` round-trip shall restore a `GameState` with identical `Get`/`GetCount` for every key. | round-trip test |
| REQ-004 | When the input is malformed (bad JSON, missing fields, count overflow), `Deserialize` shall return a typed `SaveLoadResult` error, never throw (§14). | unit test per case |
| REQ-005 | The captured RNG state shall reproduce the generator's continuation — a `SplitMix64Random` restored from it yields the identical `Next*` sequence. | unit test |
| REQ-006 | Restoring a saved `GameState` into a fresh tracer sim shall reproduce the saved behaviour (replay-equivalence — e.g. a saved door switch re-opens the door). | tracer test |
| REQ-007 | The serializer + snapshot shall be deterministic + MRM-clean (sorted, no Dictionary-foreach; no float/clock/RNG in serialize); framework-thin (string in/out). | build `-warnaserror` + review |
| REQ-008 | Existing tracer/sim behaviour shall be preserved. | existing tests green |
| REQ-009 | The slice shall pass the FULL `bin/gate.sh` (coverage + mutation; Engine MSI ≥ 80). | gate run (FULL) |

## Scope

- **In:** a `SaveState` DTO (version + sorted switch/counter `{key,value}` arrays + position + facing + RNG
  `ulong`); a `SaveSerializer` (STJ source-gen; total typed `SaveLoadResult`; deterministic sorted output);
  a `GameState` sorted-snapshot read accessor + restore; tests (round-trip, deterministic, malformed, RNG,
  replay-equivalence). FULL gate green.
- **Out (deferred follow-ups):** a save-slot/file UI; metadata/thumbnails; schema-migration logic (version
  field only); `EntityInstance`/`$data`-definition serialization (polymorphic — its own ticket); cloud saves;
  wiring RNG into the actual sim loop.

## Notes

- Forge ticket: c0095793-531e-4828-8097-93be968ffe53 (#15, project monorpgmaker `a9ee8162-3cfd-4c99-be5c-bbdb4be32b10`)
- Builds on: #12 `MapSerializer` (the STJ source-gen + `MapLoadResult` totality pattern); `GameState`,
  `IRandom`/`SplitMix64Random` (`State` capturable), `Actor`/`WorldSim`/`TracerRoom`
- Reuse PRs: PR-claude-total-parser-substring-overlap-001, PR-claude-long-product-dimension-guard-001,
  PR-claude-readonly-collection-for-true-immutability-001, PR-claude-analyzer-mutation-isolated-asserts-001
- Active pipeline: docs/planning/pipeline/completed/WORK-p0-saveload.spec.md
