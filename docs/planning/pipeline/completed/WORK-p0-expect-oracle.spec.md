---
pipeline_id: eac76b76-6703-4c33-9bbd-efdc76831710
title: WORK-p0-expect-oracle
ticket: d1725d2b-edc1-4c8f-bd1d-044bb8b4a7c8
type: work
intake:
notes: WORK-p0-expect-oracle.notes.md
status: Phase 5 — Complete PASS
---

# WORK-p0-expect-oracle

> Pipeline spec (always-loaded contract). Detailed per-phase work lives in the
> paired `.notes.md`. Phase advance = updating the `status:` frontmatter above.

## Work Spec
- **Title:** P0 — the **`.expect` oracle** (D-0017 / §7): make intent **executable**. An authored
  `<module>.expect` table (input game-state → expected `Outcome` rows) is run against the **real**
  handler at a new **gate #13**; a mismatch is a structured **MRM0006**. Closes the self-grading gap
  (the 12 gates verify mechanics, never correctness) — the oracle is authored *separately* from the
  implementing agent.
- **Scope:**
  *In* — a `.expect` format + parser; a **runner** (seed a `GameState` from a row's input switches/
  counters → build an `EventContext` → call `IMapEvent.Run` → **value-equal** compare the returned
  `IReadOnlyList<Outcome>` to the expected, in order); the **MRM0006** mismatch report (module + row +
  expected-vs-actual); `.expect` tables for **`LeverEvent`** + **`ChestEvent`** (their give-once
  contracts); **gate #13** wired into `bin/gate.sh`. FULL gate green.
  *Out* — the generator/validator DAG (`MRM0001`–`0005` — premature, no module dependency graph yet);
  the scaffolder; battle/P2 handlers + effect outcomes; the multi-arch/AOT replay-hash gate; a GUI
  fill-in grid for authoring; any `MRM0006` code-fix (intent isn't auto-fixable).
- **Systems:** **a new oracle component** (format + parser + runner — home TBD in design) ·
  **engine/sim** (consumes the real `LeverEvent`/`ChestEvent` + `GameState`/`EventContext`/`Outcome`) ·
  **the gate** (`bin/gate.sh` gate #13) · tests · the authored `.expect` data files.

## Acceptance Criteria (EARS)
| ID | EARS Requirement | Verification |
|---|---|---|
| REQ-001 | A `<module>.expect` file shall express, per row, an input game-state (named switches/counters) and the ordered `Outcome` sequence the handler is expected to return. | format review + parser unit test |
| REQ-002 | When the oracle evaluates a row, it shall seed a `GameState` from the row's input, invoke the handler's `Run` through an `EventContext`, and value-equal compare the returned outcomes (kind + payload + order) to the expected. | runner unit test |
| REQ-003 | When a handler's returned outcomes match the row, the oracle shall report success; when they differ (count/kind/payload/order), it shall emit an `MRM0006` expectation-mismatch naming the module, the row, and the expected-vs-actual outcomes. | runner unit test (match + each mismatch kind) |
| REQ-004 | The oracle shall ship `.expect` tables for `LeverEvent` and `ChestEvent` covering their give-once behavior, and the real handlers shall reproduce every row. | the authored tables + a green oracle run |
| REQ-005 | `bin/gate.sh` shall run the `.expect` oracle as a distinct gate step (gate #13) that fails the gate (non-zero) on any `MRM0006` mismatch. | gate run: green on match; red on a seeded-mismatch row |
| REQ-006 | `MRM0006` shall use the `MRM0xxx` validator band (distinct from the `MRM1xxx` analyzer band). | review |
| REQ-007 | The oracle's evaluation path shall be deterministic (no float/clock/RNG; comparison over value-equal records) and respect the layering. | review + the determinism analyzer (green) |
| REQ-008 | The slice shall pass the FULL `bin/gate.sh` (coverage + mutation on the new parser/runner code). | gate run (FULL) |

## Locked-In Decisions
- **D-0017 / §7 govern.** Intent is executable via an authored `.expect` table; a mismatch is `MRM0006`;
  the table is authored *separately from the implementing agent* (the anti-self-grading guarantee). The
  oracle requires the **real** handler to deterministically reproduce **every** row.
- **Scope = the two tracer handlers** (`LeverEvent` + `ChestEvent`) — the only handlers that exist. Build
  the format + parser + runner + the two tables + gate #13 around them.
- **The comparison leverages the value-equal `Outcome` DU (#8)** — exact kind + payload + order match;
  no bespoke equality.
- **`MRM0006` is in the `MRM0xxx` validator band** (the `MRM1xxx` band is the Roslyn analyzers').
- **Well-founded:** the handlers are already deterministic + pure (analyzers #7/#9), so "reproduce every
  row" is a sound requirement.
- **The oracle lives in `MonoRpgMaker.Editor`** (user-approved at plan): the `.expect` parser + runner are a
  maker-tool content-validation responsibility; **gate #13 invokes the Editor's oracle** as a distinct step.

### Resolved in Phase 2 (full design + manifest + test plan in the notes; user-approved "full rigor")
- **(a) format** = a line grammar `<input> => <outcomes>` (`key=true/false`→switch, `key=<int>`→counter; outcomes
  `SetSwitch`/`AddCounter`/`ShowMessage` or `(none)`; `#` comments). **(b)** a name→factory registry (default cell).
  **(c)** the Editor becomes an `Exe` (`Program.Main` → testable `OracleCli`); gate:13 = `dotnet run --project
  …Editor -- check-expectations <dir>`, ALWAYS band; **the Editor joins the gate:12 mutation loop** (oracle logic
  MSI-gated; `MapEditor` gains tests). **(d)** MRM0006 = `module:line — expectation mismatch` + given/expected/actual.
  **(e)** `.expect` files at `src/MonoRpgMaker.Engine/Sim/Tracer/Expectations/`.

## Linked Artifacts
- Design docs: `docs/agentic-substrate.md` (§7 the oracle / gate #13; §2 MRM0006 validator band; §10 the
  self-grading risk), `docs/decisions.md` (D-0017, D-0020), `docs/roadmap.md` (P0 remaining)
- Intake doc: none (LOCAL roadmap; user chose ".expect oracle first" at the P0 fork)
- Ticket doc: docs/planning/tickets/open/TICKET-0010-p0-expect-oracle.md
- Forge ticket: d1725d2b-edc1-4c8f-bd1d-044bb8b4a7c8 (#10, project monorpgmaker `a9ee8162-3cfd-4c99-be5c-bbdb4be32b10`)
- Builds on: WORK-p0-outcome-vocabulary (#8 — value-equal `Outcome` + pure handlers), WORK-p0-sim-determinism-analyzer (#7) + WORK-p0-outcome-return-analyzer (#9)
- Reuse ADs: AD-claude-outcome-vocabulary-shape-001
- Ground-truth anchors: `src/MonoRpgMaker.Engine/Sim/Tracer/LeverEvent.cs` + `ChestEvent.cs` (the handlers + their give-once contracts), `src/MonoRpgMaker.Engine/Sim/GameState.cs` + `EventContext.cs` (seeding + the read context), `src/MonoRpgMaker.Abstractions/Outcome.cs` (the value-equal DU to compare), `bin/gate.sh` (the `run_gate` step structure for gate #13)
- AAR: 2f99c72b-8b2e-4ea6-bb50-c4c8de89c906

## Phase Plan
| Phase | Status |
|---|---|
| 1 — Plan | PASS |
| 2 — Design | PASS |
| 3 — Implement | PASS |
| 3.5 — Inspect | PASS |
| 4 — Validate | PASS |
| 5 — Complete | PASS |
