---
pipeline_id: 6adfe963-ad88-4182-9e1f-28fe7c5ba561
title: WORK-builtin-library
ticket: c33b2570-1ba3-452b-baac-dca68a1854b6
type: work
intake: none (direct /work request under a standing /goal — slice C of 3)
notes: WORK-builtin-library.notes.md
status: Phase 5 — Complete PASS
---

# WORK-builtin-library

> Pipeline spec (always-loaded contract). Detailed per-phase work lives in the
> paired `.notes.md`. Phase advance = updating the `status:` frontmatter above.

## Work Spec
- **Title:** The built-in behaviour library — placeable **`Chest`** + **`GiveItem`** (contract-first, no-code). The
  trigger program's library expansion beyond `ShowText` + `Warp`.
- **Scope:**
  - *In* — two new placeable built-ins via the contract-first recipe (D-0024): each is a parameterized
    `IMapEvent` that RETURNS declarative outcomes (composing the EXISTING `AddCounter`/`SetSwitch`/`ShowMessage`
    — **no new `Outcome` case, no `OutcomeApplier`/parser change**), registered in `BehaviourRegistry` (one
    `Registration` descriptor each), with an `.expect` contract (gate:13) + a `HandlerRegistry` entry, placeable
    via the #22 kind-aware Studio inspector (it renders fields from `BehaviourKindInfo.ParamKeys` — single-source).
    - **`GiveItem`** — grants an item on its trigger: `AddCounter("item.<id>", amount)` + a `ShowMessage`. Items
      are **GameState counters keyed `item.<id>`** (no new inventory system).
    - **`Chest`** — a give-once container: on the first trigger (its open-switch unset) grants the item
      (`AddCounter`) + sets the open-switch (`SetSwitch`) + a message; afterwards an "empty" message. (The #13
      tracer `ChestEvent` is hardcoded; this is the parameterized, registered, placeable version.)
  - *Out / deferred* — **Door** (a placeable door that actually opens needs **`DoorRule`-from-`$data`**:
    `GameSession.LoadMap` currently passes `Array.Empty<DoorRule>()`, so doors don't sync in the multi-map runtime
    — an engine change; **the immediate NEXT slice**). **Shop** (a buy/sell SCREEN + inventory/currency UI — a
    separate feature). A real inventory/item-effects system (the `ItemRecord` in `Database` stays unwired).
- **Systems:** events (the `Chest`/`GiveItem` `IMapEvent` + the registry + `.expect`) · the outcome vocabulary
  (composed, not extended) · sim (`GameState` counters) · editor/studio (the inspector renders the new
  `ParamKeys`) · the `.expect` oracle (gate:13).

## Acceptance Criteria (EARS)

| ID | EARS Requirement | Verification |
|---|---|---|
| REQ-001 | A `GiveItem` placement shall, on its trigger, return outcomes granting the configured item count (an `item.<id>` counter) + showing its message — materialised by the `BehaviourRegistry`. | unit |
| REQ-002 | A `Chest` placement shall, on its FIRST trigger (open-switch unset), return outcomes granting the item + setting the open-switch + showing the first message; on a subsequent trigger it shall show the empty message (give-once). | unit |
| REQ-003 | Each new built-in shall be registered in the `BehaviourRegistry` (one descriptor — name + ParamKeys + factory) and carry an `.expect` contract reproduced by the gate:13 oracle + a `HandlerRegistry` entry. | unit + gate:13 |
| REQ-004 | Materialising a `Chest`/`GiveItem` placement with a missing/invalid parameter shall return a typed `BehaviourResult.Failure` (totality), never a throw. | unit |
| REQ-005 | The Studio event inspector shall render the new kinds' fields from their `BehaviourKindInfo.ParamKeys` (single-source — no Studio code change), so the kinds are placeable. | unit (ParamKeys) + manual smoke |
| REQ-006 | The FULL `bin/gate.sh` shall be green (coverage + mutation + the `.expect` oracle now covering the new behaviours). | gate |

## Locked-In Decisions
- **D-0024 (contract-first):** each built-in is the single-descriptor recipe — `IMapEvent` + one `BehaviourRegistry`
  `Registration` + an `.expect` + a `HandlerRegistry` entry; the editor's `ParamKeys` drive the inspector (no
  drift — the #20/#22 lesson). Not a one-off.
- **D-0017 (declarative outcomes):** the behaviours RETURN outcomes composing the EXISTING vocabulary
  (`AddCounter`/`SetSwitch`/`ShowMessage`). **No new `Outcome` case** → no `OutcomeApplier` arm, no
  `ExpectationParser` arm (the `.expect` outcomes already parse).
- **Items-as-counters:** an item is a `GameState` counter keyed `item.<id>` — no new inventory system; a real
  inventory + item effects + the `ItemRecord` wiring is a future slice.
- **Chest = give-once** (gated by an open-switch); **GiveItem = grant-on-trigger**.
- **Door DEFERRED to the next slice** (needs `DoorRule`-from-`$data`). **Shop DEFERRED** (a buy/sell UI). Determinism;
  MRM-clean sim.

## Linked Artifacts
- Design docs: `docs/decisions.md` (D-0017, D-0024), `docs/roadmap.md`.
- Intake doc: none (direct `/work` under a standing `/goal`).
- Ticket doc: `docs/planning/tickets/open/TICKET-0025-builtin-library.md`
- Forge ticket: c33b2570-1ba3-452b-baac-dca68a1854b6 (#25)

## Phase Plan
| Phase | Status |
|---|---|
| 1 — Plan | PASS (autonomous — standing /goal; scope = Chest + GiveItem; Door/Shop deferred) |
| 2 — Design | PASS (GiveItemEvent + ContainerEvent; 2 registrations + .expect + handlers; zero engine changes) |
| 3 — Implement | PASS (GiveItem + Chest registered + .expect + handlers; build 0/0; oracle 9/6; GATE GREEN [fast]) |
| 3.5 — Inspect | PASS (1 critic, all lenses cleared with evidence; +1 Chest totality test; no code defect) |
| 4 — Validate | PASS (500 tests; cov 93.0%; MSI Engine 85.43/Abs 82.22/Analyzers 91.18/Editor 84.45; oracle 9/6; GATE GREEN [full]) |
| 5 — Complete | PASS (docs touched; AAR closed; ticket #25 done; archived) |
