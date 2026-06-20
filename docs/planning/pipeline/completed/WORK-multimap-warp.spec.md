---
pipeline_id: ae473c4a-fd94-4116-bdc3-0674a6f274a4
title: WORK-multimap-warp
ticket: 1ba1efe9-d4d7-418e-95e1-520805d30a56
type: work
intake: none (direct request via /work; the last Studio-v2 slice + trigger Slice 2)
notes: WORK-multimap-warp.notes.md
status: Phase 5 — Complete PASS
---

# WORK-multimap-warp

> Pipeline spec (always-loaded contract). Detailed per-phase work lives in the
> paired `.notes.md`. Phase advance = updating the `status:` frontmatter above.

## Work Spec
- **Title:** Multi-map + the built-in **Warp** + **Studio multi-map authoring** — the runtime holds many embedded
  maps and switches between them on a transition (step on / action a Warp tile → load the target map at the target
  cell), and the Studio authors the map set + the Warp placements. (All-in-one — the user declined the split.)
- **Scope:**
  - *In (runtime)* — (1) a **multi-map registry** of embedded maps by stable id + a start-map pointer (D-0023,
    console-safe; load-by-id is total). (2) A new declarative **`Warp` outcome** added additively to the closed
    `Abstractions` `Outcome` hierarchy. (3) A built-in **`WarpEvent : IMapEvent`** that returns the `Warp` outcome
    (no direct mutation, D-0017), materialised by the `BehaviourRegistry` from a `Warp` placement (kind + params),
    with an **`.expect`** contract (gate:13) — the D-0024 recipe, no one-off. (4) The **map-switch seam**: the
    `OutcomeApplier` signals a *pending warp* (like `CurrentMessage`); a **new gated orchestrator above
    `WorldSim`** (Engine.Sim) owns the registry + the active sim and performs the switch — load the target map,
    build a new `WorldSim`, place the player at the target cell, and **carry the `GameState`** (`WorldSim`'s
    fresh-`GameState` becomes injectable). `RpgGame` drives the orchestrator (rendering separate; deterministic).
    (5) Totality on a bad warp target.
  - *In (editor — the all-in-one half)* — a gated multi-map **project / map-set** layer in `MonoRpgMaker.Editor`
    (hold many maps + the manifest; create / select / rename; save + load the whole set — a multi-map extension
    over `MapPaintSession`, neutral surface); the **Studio multi-map UI** (a map list + new / switch; save the
    set) and the **Warp event inspector** (for a `Warp`-kind event, edit target-map [a dropdown of the set's map
    ids] + target-cell instead of the ShowText box) — a thin `[ExcludeFromCodeCoverage]` host (D-0022). The
    editor's saved set is **runtime-valid** (cross-layer proof).
  - *Out / deferred* — autorun / parallel triggers; a transition animation / fade; the rest of the built-in
    library (Shop / GiveItem / Door — trigger Slice 2 continues separately).
- **Systems:** runtime/player (the map-switch orchestrator, `RpgGame`) · events (`WarpEvent` + the registry +
  `.expect`) · the outcome vocabulary (`Abstractions`) · sim (`WorldSim`/`OutcomeApplier`/`GameState`) · save/load
  (`$game` current-map, if in scope) · map/tilemap + `$data` (the multi-map model) · **editor** (the gated map-set
  layer) · **studio** (the multi-map UI + the Warp inspector) · rendering (re-init on switch).

## Acceptance Criteria (EARS)

| ID | EARS Requirement | Verification |
|---|---|---|
| REQ-001 | The runtime shall hold a registry of embedded maps addressable by a stable id with a start-map pointer; loading a map by id shall be total (an unknown id → a typed failure, never a throw). | unit |
| REQ-002 | A built-in `WarpEvent` (`IMapEvent`) shall, on its trigger, return a declarative `Warp` outcome carrying the target map id + cell — no direct state mutation (D-0017). | unit |
| REQ-003 | When a `Warp` outcome fires, the runtime shall switch the active map to the target and place the player at the target cell, carrying the game state (switches/counters persist across the warp). | unit + smoke |
| REQ-004 | The `Warp` behaviour shall be materialised by the `BehaviourRegistry` from a `Warp` placement (kind + params) and carry an `.expect` contract validated by gate:13. | unit + gate:13 |
| REQ-005 | A `Warp` to an unknown map id or an out-of-bounds target cell shall be handled totally (a typed failure / safe no-op), never a throw. | unit |
| REQ-006 | The save **serializer** shall record the active map id (round-tripped) so a future load can resume on that map; absent → the start map (back-compat). The in-game save/load **host** wiring is deferred — no save menu exists yet (#15 is serializer-only). | unit |
| REQ-007 | The Studio shall edit a SET of maps — create / select / rename + save the whole set (manifest + per-map `$data`) — so an author builds a multi-map game; the gated map-set layer round-trips the set. | unit + manual smoke |
| REQ-008 | The Studio event inspector shall, for a `Warp`-kind event, edit its target map + target cell (not the ShowText box), writing the `$data` `Warp` placement the runtime materialises. | unit + manual smoke |
| REQ-009 | The editor's saved multi-map set shall be runtime-valid — each map + its `Warp` placements load and the registry resolves the targets (cross-layer). | unit |
| REQ-010 | The FULL `bin/gate.sh` shall be green, including the `.expect` oracle (gate:13) and the Engine + Editor coverage + mutation floors. | gate |

## Locked-In Decisions
- **D-0023 (repo-is-the-game):** the registry is over **embedded** maps (console-safe; no roamed paths, no runtime
  project loader). A new game still clones the template.
- **D-0024 (contract-first):** `Warp` is a built-in via the recipe — implement `IMapEvent`, register the kind in
  `BehaviourRegistry`, declare its `.expect`. Not a one-off; the registry's single descriptor stays the source.
- **D-0017 (declarative outcomes):** `WarpEvent` **returns** a `Warp` outcome; the **switch is applied by the
  sim-host orchestration layer**, never by the behaviour. The behaviour stays pure over the read context.
- **Additive outcome vocabulary:** the `Warp` case is added in `Abstractions/Outcome.cs`; `OutcomeApplier` gains a
  `Warp` arm that **signals a pending warp** (does not mutate `GameState`) — one registry, not two.
- **Determinism:** the switch carries no clock/RNG; sim stays separate from rendering.
- **All-in-one (the user's call at the plan gate):** this slice ships BOTH the runtime multi-map + `Warp` AND the
  Studio multi-map authoring UI — one large pipeline (the split was offered + declined). The runtime is built +
  proven first; the Studio UI layers on top (so the runtime stays the spine even if the editor UX grows).

### Open for design (Phase 2 decides)
- The multi-map **`$data` shape**: per-map embedded files + a small manifest (`{ startMap, mapIds }`) vs one
  bundled multi-map file. (Lean per-map files + a manifest — mirrors the existing single `start.json` + scales.)
- The **orchestrator** (e.g. a `GameSession`/`WorldHost`, gated Engine.Sim) — its shape, how `OutcomeApplier`
  surfaces the pending warp, and making `WorldSim`'s `GameState` **injectable** so it carries across a warp.
- **Save/load (#15)** recording the active map id (REQ-006) — Slice A for correctness vs a deferred follow-up;
  design sizes the blast radius against the existing `$game` save model.
- The `Warp` registry **params** (target map id + cell) + the `.expect` contract shape (mirror `ShowText`).
- The editor **map-set layer** shape (a `MapProject` holding the maps + the manifest; how it reuses the
  #19/#20/#21 `MapPaintSession`; neutral surface) + the Studio map-list / Warp-inspector UI (host, excluded).

## Linked Artifacts
- Design docs: `docs/decisions.md` (D-0017, D-0022, D-0023, D-0024), `docs/roadmap.md`.
- Intake doc: none (direct request).
- Ticket doc: `docs/planning/tickets/open/TICKET-0022-multimap-warp.md`
- Forge ticket: 1ba1efe9-d4d7-418e-95e1-520805d30a56 (#22)

## Phase Plan
| Phase | Status |
|---|---|
| 1 — Plan | PASS |
| 2 — Design | PASS |
| 3 — Implement | PASS (runtime + Studio; build 0/0; oracle 6/4; GATE GREEN [fast]) |
| 3.5 — Inspect | PASS (2 critics; runtime verified correct; subscription leak fixed; REQ-006 re-scoped) |
| 4 — Validate | PASS (471 tests; cov 93.0%; MSI Engine 85.43/Abs 82.22/Analyzers 91.18/Editor 85.45; GATE GREEN [full]) |
| 5 — Complete | — |
