---
pipeline_id: 1cfd254d-1f96-49ba-a2d8-ff9563dabd1e
title: WORK-m0-chest-give-once
ticket: 3e56871a-eb6f-4d61-9b81-ba0262073253
type: work
intake:
notes: WORK-m0-chest-give-once.notes.md
status: Phase 5 — Complete PASS
---

# WORK-m0-chest-give-once

> Pipeline spec (always-loaded contract). Detailed per-phase work lives in the
> paired `.notes.md`. Phase advance = updating the `status:` frontmatter above.

## Work Spec
- **Title:** M0 GO/NO-GO — a hand-authored "chest gives one potion, then empty"
  event variant on the tracer slice, used to time the AI-directed authoring loop
  vs RPG Maker and record the GO/NO-GO before committing to P0 (D-0015 / roadmap
  P-(-1); realizes WORK-m0-tracer-bullet-v1 locked decision #6).
- **Scope:** *In* — a `ChestEvent : IMapEvent` hand-written in C# mirroring
  `Sim/Tracer/LeverEvent.cs` (first trigger grants exactly one potion + narrates;
  later triggers narrate "empty"); an observable "one potion" grant (mechanism
  chosen in Phase 2); a chest tile placed in the tracer room (`TracerRoom`);
  wiring into `MonoRpgMaker.Player` so the chest renders + is triggerable; unit
  tests (TracerSliceTests pattern) for first-grant / second-empty /
  idempotent-repeat / determinism; the documented timing/feel measurement
  procedure. *Out* — a full inventory/item database, item use/effects, the source
  generator / `Abstractions` / `FixedPoint` / replay-hash (P0 chassis), save/load
  persistence of chest state, real art, and the subjective human GO/NO-GO
  judgment itself (manual, recorded at `/pipeline:complete`).
- **Systems:** events · entities/actors · runtime/player · rendering · input ·
  engine · (data, if a potion counter/inventory is chosen in Phase 2).

## Acceptance Criteria (EARS)
Each acceptance criterion uses EARS syntax, one observable behavior, with a verification method.

| ID | EARS Requirement | Verification |
|---|---|---|
| REQ-001 | When the player triggers the chest for the first time, the system shall grant exactly one potion (observable via the session's potion tally) and display the grant message exactly once. | unit test (event + state) |
| REQ-002 | When the player triggers the chest after it has been opened, the system shall display the "empty" message and grant no additional potion, so the potion tally rises by exactly one across any number of repeated triggers. | unit test (give-once transition) |
| REQ-003 | Given the same initial world and the same player command sequence, the chest's outcome (potion tally + messages) shall be identical on every run. | unit test (deterministic replay) |
| REQ-004 | When the Player application is launched, the system shall render the chest in the tracer room and let the player trigger it through movement/interaction. | smoke check (manual run of MonoRpgMaker.Player) |
| REQ-005 | The chest simulation code shall use no `System.Random` or `DateTime`; any randomness shall be obtained from an injected `IRandom`. | source-bans gate + review |
| REQ-006 | The slice shall pass the FULL `bin/gate.sh` (format, build `-warnaserror`, tests, vuln/license/secret/shellcheck/suppression/source-ban/doc-todo static gates, coverage ≥ floor, mutation ≥ floor). | gate run (FULL) |
| REQ-007 | The pipeline shall record a GO/NO-GO measurement comparing AI-directed authoring of the chest variant against RPG Maker's click-path (target ~3× + "feels good"), captured at `/pipeline:complete`. | doc check |

## Locked-In Decisions
- **Pre-chassis (D-0015):** hand-written C# exemplar; NO source generator, NO
  `Abstractions` assembly, NO `FixedPoint`, NO replay-hash gate — this chest is the
  target shape the scaffolder will later emit. Keep sim cleanly separable from
  rendering/input (the M1 extraction depends on it).
- **FULL gate (supersedes the tracer's relaxed-gate allowance for this slice):**
  the strict 12-gate `bin/gate.sh` is now wired and `/commit` + merge require a
  FULL green receipt; the give-once logic is small and fully testable; host wiring
  stays `[ExcludeFromCodeCoverage]`. Floors: `NET_COV_MIN=80`, `MUT_MSI_MIN=80`.
- **Mirror the existing seams** — `ChestEvent : IMapEvent`, reuse
  `EventContext`/`GameState`/`WorldSim`; place via `TracerRoom`. No parallel event
  mechanism (ride the `IMapEvent` path exactly like `LeverEvent`/`DoorRule`).
- **Determinism discipline (D-0016 habit):** the grant-once is scripted and
  deterministic; inject `IRandom` if randomness is ever added; no
  `System.Random`/`DateTime` in sim.
- **OPEN — Phase 2 design decision (not pre-empted here):** how to represent "one
  potion" — (a) a pure `GameState` bool switch (`chest_opened`) with a narrated
  grant, or (b) extend runtime state with a minimal potion counter / inventory
  field the test can read. `GameState` today is bool-switch-only
  (`Get`/`Set`), so (b) means a small additive state surface. Phase 2 picks the
  smallest representation that makes REQ-001/002 observable.
- **OPEN — Phase 2:** the trigger model — reuse `EventTrigger.StepOn` (walk onto
  the chest) like the lever, vs. an interact/confirm trigger (face + press).
  `StepOn` is the lower-risk default; Phase 2 confirms.
- **The timing/feel GO/NO-GO is the human's manual judgment**, recorded at
  `/pipeline:complete`. This pipeline delivers the artifact + the documented
  procedure, not the subjective call.

## Linked Artifacts
- Design docs: docs/roadmap.md (P-(-1) status + P0), docs/decisions.md
  (D-0015 / D-0016 / D-0017), docs/agentic-substrate.md (§5 primitives,
  §7 determinism + correctness)
- Intake doc: none
- Ticket doc: docs/planning/tickets/open/TICKET-0002-m0-chest-give-once.md
- Forge ticket: 3e56871a-eb6f-4d61-9b81-ba0262073253 (#2, project monorpgmaker
  `a9ee8162-3cfd-4c99-be5c-bbdb4be32b10`)
- Base being extended: docs/planning/pipeline/completed/WORK-m0-tracer-bullet-v1.spec.md + .notes.md
- AAR: 52131509-9182-4db3-904e-6fae94d500da

## Phase Plan
| Phase | Status |
|---|---|
| 1 — Plan | PASS |
| 2 — Design | PASS |
| 3 — Implement | PASS |
| 3.5 — Inspect | PASS |
| 4 — Validate | PASS |
| 5 — Complete | PASS |
