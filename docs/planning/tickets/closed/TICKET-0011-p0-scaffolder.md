---
title: TICKET-0011-p0-scaffolder
status: done
ticket: d9016842-2086-4492-b65e-e01363e5295a
ticket_number: 11
type: feature
created: 2026-06-19
intake:
pipeline_spec: docs/planning/pipeline/completed/WORK-p0-scaffolder.spec.md
---

# TICKET-0011-p0-scaffolder

## Summary

The **D-0017 / §3 scaffolder** (the aic guarantee-A "by-construction" leverage) — the
capstone that makes the chassis pay off: *the AI builds the shape, the agent fills the
thinking.* A `scaffold event <Name>` verb on the existing `MonoRpgMaker.Editor` CLI — a
**deterministic `{{var}}` template emitter** (no LLM, no eval) that emits a
**gate-clean-by-construction** event-handler skeleton mirroring the committed tracer
fixture (`LeverEvent`/`ChestEvent` shape): a `sealed class <Name> : IMapEvent` whose
`Run(IEventContext)` returns `IReadOnlyList<Outcome>`, reads state via the context, with
`// fill:` holes for the author's logic; plus a co-located `<Name>.expect` stub.

## EARS Requirements

| ID | EARS Requirement | Verification |
|---|---|---|
| REQ-001 | When `scaffold event <Name>` runs, it shall emit a handler skeleton — a `sealed class <Name> : IMapEvent` whose `Run(IEventContext)` returns `IReadOnlyList<Outcome>` — with the author's branch logic as `// fill:` holes. | output review + unit test |
| REQ-002 | The verb shall also emit a co-located `<Name>.expect` stub that parses cleanly under the `.expect` oracle (#10). | parser test on the stub |
| REQ-003 | The emitted skeleton, with its `// fill:` holes replaced by a trivial `return [];`, shall **compile** and be analyzer-clean (no `MRM1001`–`1006`). | verify test (scaffold → compile → analyzers) |
| REQ-004 | Emission shall be pure + deterministic — the same `<Name>` yields byte-identical output (no clock / RNG / ambient state). | unit test (render twice → equal) |
| REQ-005 | A golden test shall pin the rendered skeleton for a known `<Name>`, so the template can't silently rot. | golden test |
| REQ-006 | When given bad input (missing `<Name>`, a non-identifier name, or an existing target), the verb shall fail with a non-zero exit + a clear message — no partial output. | CLI test |
| REQ-007 | The slice shall pass the FULL `bin/gate.sh` (Editor coverage ≥ 80 + mutation MSI ≥ 80). | gate run (FULL) |

## Scope

- **In:** the `scaffold event <Name>` verb in the Editor CLI; a deterministic `{{var}}` template
  (mirroring the tracer fixture) → a `<Name>.cs` handler skeleton + a `<Name>.expect` stub; the
  golden + compiles-analyzer-clean tests; bad-input handling.
- **Out:** the Roslyn generator + `monorpg validate` DAG (D-0018, deferred — premature, no module
  graph); non-`event` seam-kinds (v1 = `event` only); the editor GUI; the CI template-vs-fixture
  meta-gate (a golden suffices for v1).

## Notes

- Forge ticket: d9016842-2086-4492-b65e-e01363e5295a (#11, project monorpgmaker `a9ee8162-3cfd-4c99-be5c-bbdb4be32b10`)
- Related: `docs/decisions.md` (D-0017), `docs/agentic-substrate.md` (§3 scaffolding / §1 "engine defaults double as fixtures"), `docs/product-phasing.md` (#11)
- Builds on: #10 (the Editor CLI host + `.expect` oracle), #8 (Outcome DU), #7/#9 (the analyzers it satisfies by construction)
- Reuse ADs: AD-claude-expect-oracle-shape-001; PRs: PR-claude-analyzer-mutation-isolated-asserts-001, PR-claude-analyzer-verifier-resolve-types-001
- Promoted from intake: none (autonomous goal run #11–#15)
- Active pipeline: docs/planning/pipeline/completed/WORK-p0-scaffolder.spec.md
