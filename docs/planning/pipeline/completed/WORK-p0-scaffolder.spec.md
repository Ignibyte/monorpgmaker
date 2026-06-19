---
pipeline_id: 3e8fde91-89d9-444f-92b7-ed5c64e3c975
title: WORK-p0-scaffolder
ticket: d9016842-2086-4492-b65e-e01363e5295a
type: work
intake:
notes: WORK-p0-scaffolder.notes.md
status: Phase 5 — Complete PASS
---

# WORK-p0-scaffolder

> Pipeline spec (always-loaded contract). Detail per phase lives in the paired `.notes.md`.

## Work Spec
- **Title:** P0 #11 — the **scaffolder** (D-0017 / §3): a `scaffold event <Name>` verb on the Editor CLI
  that **deterministically** emits a **gate-clean-by-construction** event-handler skeleton (+ a `.expect`
  stub) mirroring the committed tracer fixture — the "AI builds the shape, you fill the thinking" leverage.
- **Scope:**
  *In* — the `scaffold event <Name>` verb in `MonoRpgMaker.Editor`; a deterministic `{{var}}` template
  (mirroring `LeverEvent`/`ChestEvent`) → a `<Name>.cs` skeleton (`sealed class <Name> : IMapEvent`,
  `Run(IEventContext) → IReadOnlyList<Outcome>`, `// fill:` holes) + a co-located `<Name>.expect` stub;
  pure deterministic emission; the golden + compiles-analyzer-clean + bad-input tests. FULL gate green.
  *Out* — the Roslyn generator + `monorpg validate` DAG (D-0018, deferred); non-`event` seam-kinds
  (v1 = `event`); the GUI; the CI template-vs-fixture meta-gate.
- **Systems:** **editor** (the new CLI verb + the template + the emitter) · consumes the **abstractions**
  seam (`IMapEvent`/`IEventContext`/`Outcome`) it emits against · tests.

## Acceptance Criteria (EARS)
| ID | EARS Requirement | Verification |
|---|---|---|
| REQ-001 | When `scaffold event <Name>` runs, it shall emit a handler skeleton — a `sealed class <Name> : IMapEvent` whose `Run(IEventContext)` returns `IReadOnlyList<Outcome>` — with the author's logic as `// fill:` holes. | output review + unit test |
| REQ-002 | The verb shall also emit a co-located `<Name>.expect` stub that parses cleanly under the `.expect` oracle. | parser test on the stub |
| REQ-003 | The emitted skeleton, with `// fill:` holes replaced by a trivial `return [];`, shall **compile** and be analyzer-clean (no `MRM1001`–`1006`). | verify test (scaffold → compile → analyzers) |
| REQ-004 | Emission shall be pure + deterministic — the same `<Name>` yields byte-identical output (no clock/RNG/ambient state). | unit test (render twice → equal) |
| REQ-005 | A golden test shall pin the rendered skeleton for a known `<Name>` (the template can't silently rot). | golden test |
| REQ-006 | On bad input (missing `<Name>`, non-identifier name, existing target), the verb shall fail with a non-zero exit + a clear message — no partial output. | CLI test |
| REQ-007 | The slice shall pass the FULL `bin/gate.sh` (Editor coverage ≥ 80 + mutation MSI ≥ 80). | gate run (FULL) |

## Locked-In Decisions
- **D-0017 / §3 govern.** A deterministic `{{var}}` emitter (no LLM, no eval) mirroring the committed tracer
  fixture; **gate-clean by construction**; only `// fill:` holes remain. The engine's tracer handlers double
  as the fixture (§1).
- **Hosted as a `scaffold event <Name>` verb** in the Editor CLI (alongside `check-expectations`); **v1 =
  the `event` seam-kind only.**
- **The skeleton is gate-clean by construction:** it returns `Outcome`s (so raw mutation won't compile — #9),
  uses no banned constructs (#7), and its `.expect` stub parses (#10).
- **Emission is pure string templating** (no `DateTime`/`Random`) — mutation-tested (the Editor is in gate:12).
- **Out:** the Roslyn generator / validate DAG (D-0018); non-`event` kinds; the GUI; the meta-gate.

### Resolved in Phase 2 (full design + manifest + test plan in the notes)
- Embedded template strings in `EventScaffold` (`{{Name}}`/`{{Namespace}}`); CLI `scaffold event <Name> <out-dir>
  [--namespace ns]`, `<Name>` a validated identifier, default ns `MonoRpgMaker.Engine.Sim.Tracer`; writes
  `<Name>.cs` + `<Name>.expect`, refuses to overwrite. The skeleton is **immediately** gate-clean (`Run` =
  `// fill:` + `return [];`), the `.expect` stub is one `=> (none)` row (matches it). Pure `Render` (mutation-tested)
  + `ScaffoldCli` IO + a `MonorpgCli` router; `Program` stays a thin excluded shell. Can't-rot = a golden + the
  rendered `.cs` compiled & MRM-analyzed clean in a sim namespace + the `.expect` parsed.

## Linked Artifacts
- Design docs: `docs/decisions.md` (D-0017), `docs/agentic-substrate.md` (§3 scaffolding, §1 fixtures),
  `docs/product-phasing.md` (#11), `docs/roadmap.md` (P0)
- Ticket doc: docs/planning/tickets/open/TICKET-0011-p0-scaffolder.md
- Forge ticket: d9016842-2086-4492-b65e-e01363e5295a (#11, project monorpgmaker `a9ee8162-3cfd-4c99-be5c-bbdb4be32b10`)
- Builds on: #10 (Editor CLI + `.expect` oracle), #8 (Outcome DU), #7/#9 (analyzers satisfied by construction)
- Reuse ADs: AD-claude-expect-oracle-shape-001 · PRs: PR-claude-analyzer-mutation-isolated-asserts-001, PR-claude-analyzer-verifier-resolve-types-001
- Ground-truth anchors: `src/MonoRpgMaker.Editor/Expectations/OracleCli.cs` (the CLI verb dispatch to extend),
  `src/MonoRpgMaker.Engine/Sim/Tracer/LeverEvent.cs` + `ChestEvent.cs` (the fixture shape to mirror),
  `src/MonoRpgMaker.Engine/Sim/Tracer/Expectations/LeverEvent.expect` (the `.expect` stub shape)
- AAR: a70b828b-24d4-45e8-b81a-dfb044597f93

## Phase Plan
| Phase | Status |
|---|---|
| 1 — Plan | PASS |
| 2 — Design | PASS |
| 3 — Implement | PASS |
| 3.5 — Inspect | PASS |
| 4 — Validate | PASS |
| 5 — Complete | PASS |
