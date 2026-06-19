---
pipeline_id: 5b31f1ef-0e3e-46de-8a46-a7cd91c3ec2e
title: WORK-p0-saveload
ticket: c0095793-531e-4828-8097-93be968ffe53
type: work
intake:
notes: WORK-p0-saveload.notes.md
status: Phase 5 — Complete PASS
---

# WORK-p0-saveload

> Pipeline spec (always-loaded contract). Detail per phase lives in the paired `.notes.md`.

## Work Spec
- **Title:** P0 #15 — save/load + the stateful spine: a deterministic, total `$game` save serializer +
  replay-equivalence. The capstone proving the determinism foundation (#1–#14) pays off.
- **Scope:**
  *In* — a `SaveState` DTO (version + sorted switch/counter `{key,value}` arrays + player position/facing +
  RNG `ulong`); a `SaveSerializer` (STJ source-gen; total typed `SaveLoadResult`; deterministic sorted output);
  a `GameState` sorted-snapshot accessor + restore; tests (round-trip, deterministic, malformed, RNG,
  replay-equivalence). FULL gate green.
  *Out (deferred)* — save-slot/file UI; metadata; migration logic (version field only); `EntityInstance`/`$data`
  serialization; cloud; wiring RNG into the sim loop.
- **Systems:** **engine/save** (the `SaveState` DTO + `SaveSerializer`) · **engine/sim** (`GameState`
  snapshot/restore) · consumes `IRandom.State`, `Actor`/`WorldSim`/`TracerRoom` · the host owns the file · tests.

## Acceptance Criteria (EARS)
| ID | EARS Requirement | Verification |
|---|---|---|
| REQ-001 | `SaveSerializer.Serialize` shall produce a JSON save capturing version, switches + counters, player position + facing, and the RNG state. | unit test |
| REQ-002 | `Serialize` shall be deterministic — same logical state → byte-identical output regardless of insertion order (sorted keys). | unit test |
| REQ-003 | A `Serialize → Deserialize` round-trip shall restore a `GameState` with identical `Get`/`GetCount` for every key. | round-trip test |
| REQ-004 | Malformed input (bad JSON, missing fields, count overflow) → a typed `SaveLoadResult` error, never a throw (§14). | unit test per case |
| REQ-005 | The captured RNG state shall reproduce the generator's continuation (a `SplitMix64Random` restored from it yields the identical `Next*` sequence). | unit test |
| REQ-006 | Restoring a saved `GameState` into a fresh tracer sim shall reproduce the saved behaviour (replay-equivalence). | tracer test |
| REQ-007 | The serializer + snapshot shall be deterministic + MRM-clean (sorted, no Dictionary-foreach; no float/clock/RNG in serialize); framework-thin. | build `-warnaserror` + review |
| REQ-008 | Existing tracer/sim behaviour shall be preserved. | existing tests green |
| REQ-009 | The slice shall pass the FULL `bin/gate.sh` (coverage + mutation; Engine MSI ≥ 80). | gate run (FULL) |

## Locked-In Decisions
- **A `SaveState` DTO** = `version` + switches + counters as **SORTED `{key,value}` arrays** (StringComparer.Ordinal
  — byte-deterministic; NOT a nondeterministic `Dictionary` enumeration) + player position (X,Y) + facing + RNG
  `ulong`. Serialized via the **STJ source-gen** context (reuse the #12 pattern).
- **A total typed `SaveLoadResult`** (mirrors `MapLoadResult`) — `Deserialize` never throws on the parse path
  (catch `JsonException`; validate version/null/overflow with a `long` product where relevant).
- **`GameState` gains a sorted-snapshot read accessor** (sidesteps the Dictionary-exposure determinism/immutability
  holes — PR-claude-readonly-collection-for-true-immutability-001) + a **restore** path (`Set`/`Add`, already `[StateMutator]`).
- **RNG state captured** (`IRandom.State`) + restorable (`new SplitMix64Random(state)`).
- **Engine framework-thin** — `Serialize`/`Deserialize` are string in/out; the host owns the file.
- **Out:** file UI; slots/metadata; migration logic (version field only); EntityInstance serialization; cloud;
  RNG-into-sim-loop.

### OPEN — Phase 2 (design decides)
- **(a)** The `GameState` read-accessor shape — a `ToSnapshot()` returning sorted `{key,value}` arrays (cleanest).
- **(b)** Position + FACING restore depth — capture both; restore `Cell` via the `Actor` ctor; **facing-restore
  seam vs defer** (designer's call after reading `Entity`/`Actor` — facing-independent continuation sidesteps it).
- **(c)** The replay-equivalence test depth — state-behaviour-restore (a saved door switch re-opens the door in a
  fresh sim) vs a full continued-inputs match.
- **(d)** The `SaveState` DTO field layout + the STJ source-gen context.

## Linked Artifacts
- Design docs: `docs/roadmap.md` (P1 state spine; "RNG state already capturable"), `docs/decisions.md` (D-0016 determinism), `docs/product-phasing.md` (#15, save/load row)
- Ticket doc: docs/planning/tickets/open/TICKET-0015-p0-saveload.md
- Forge ticket: c0095793-531e-4828-8097-93be968ffe53 (#15, project monorpgmaker `a9ee8162-3cfd-4c99-be5c-bbdb4be32b10`)
- Builds on: #12 `MapSerializer`/`MapLoadResult` (the pattern); `GameState`, `IRandom`/`SplitMix64Random`, `Actor`/`WorldSim`/`TracerRoom`
- Reuse PRs: PR-claude-total-parser-substring-overlap-001, PR-claude-long-product-dimension-guard-001, PR-claude-readonly-collection-for-true-immutability-001, PR-claude-analyzer-mutation-isolated-asserts-001
- Ground-truth anchors: `src/MonoRpgMaker.Engine/Sim/GameState.cs`, `src/MonoRpgMaker.Abstractions/{IRandom,SplitMix64Random}.cs`, `src/MonoRpgMaker.Engine/World/MapSerializer.cs` (the pattern), `src/MonoRpgMaker.Engine/Sim/Tracer/TracerRoom.cs`
- AAR: b124ed64-9643-48a3-8f33-2ca72393f686

## Phase Plan
| Phase | Status |
|---|---|
| 1 — Plan | PASS |
| 2 — Design | PASS |
| 3 — Implement | PASS |
| 3.5 — Inspect | PASS |
| 4 — Validate | PASS |
| 5 — Complete | PASS |
