---
title: INTAKE-event-trigger-system
status: promoted
created: 2026-06-20
ticket: 4afa20c7-7974-45e1-afca-87a0c2579bb5
pipeline_spec: docs/planning/pipeline/active/WORK-event-foundation.spec.md
---

# INTAKE-event-trigger-system

## Problem / Opportunity

The runtime is playable (#17) but **inert** — you walk an empty map. The vision (D-0011 agentic authoring, D-0024):
**every interactive thing in a game — NPCs, shopkeepers, room warps, chests, doors, signs — is the SAME
construct: a *trigger placed on a tile* + a *behaviour bound to it.*** One unified system, not five special cases
(the big simplification over RPG Maker, where each is its own special-cased feature).

The behaviour comes from one of **two interchangeable sources, on the same placement model**:

- **Built-in behaviours (no-code, data-configured)** — the engine ships a small library: `ShowText` (dialogue),
  `Warp` (room transition), `Shop`, `GiveItem` (chest), `Door`. You place a trigger and fill in params (the
  lines, the destination, the shop list). This is the **base-state / "no-code baseline you don't fight"** — the
  everyday 80%, **including dialogue** (you never summon an agent just to say "Hello").
- **Agent-authored modules (full code, full flexibility)** — when a trigger needs real *logic* (branching
  dialogue on a switch, a chest gated by a quest, a conditional warp), the agent writes a custom C# module
  against the seam with its `.expect` contract. The flexibility escape hatch.

Both are **chosen per trigger** — default to built-in, drop to the agent only for custom logic. The built-ins are
RPG Maker's ~100 commands *done right*; the agent replaces the plugin / event-command spaghetti for custom
behaviour.

### The non-negotiable engineering principle (owner, 2026-06-20)

This is built **contract-first, as ONE repeatable pattern — never willy-nilly one-off triggers.**
- Every behaviour implements the **already-published `IMapEvent` seam** (#13): `Cell` · `Trigger` · `Order` ·
  `Run(IEventContext) → IReadOnlyList<Outcome>` (behaviours return *declarative* outcomes, never mutate state
  directly — D-0017).
- Placement is a **uniform `$data` schema** (the same shape for every trigger kind).
- Binding is a **single registry** (`kind` → an `IMapEvent` factory).
- **Built-in and agent behaviours are interchangeable at the interface** — the runtime dispatches them identically.
- The **`.expect` oracle (gate:13)** validates *every* behaviour's contract.
- Adding a behaviour = ONE recipe: *implement `IMapEvent`, register the kind, declare its `.expect`.*

## Proposed Outcome

A single, contract-first **trigger/event system**:
- A placed trigger is **data** in the map's `$data`: `{ id (stable), cell, trigger (StepOn / ActionButton / …),
  behaviour: { kind, params } }`.
- A **behaviour registry** maps `kind` → an `IMapEvent` factory — built-in kinds (ShowText / Warp / Shop /
  GiveItem / Door) **and** agent-authored custom kinds, all implementing the same `IMapEvent` seam and returning
  declarative `Outcome`s.
- The **runtime loads** the placements, materialises each into an `IMapEvent` via the registry, and dispatches
  them through the existing `WorldSim` (the `HookSchedule` / `PressAction` / step-on machinery from #13).
- The **id is the glue** between placement (data) and behaviour (built-in or agent module).

## Candidate EARS Requirements (high-level; refined per slice)

| ID | EARS Requirement | Verification |
|---|---|---|
| REQ-001 | A map's `$data` shall carry placed triggers (`id`, `cell`, `trigger`, behaviour `kind`, `params`); the loader shall be total (a malformed placement → a typed error, never a throw). | unit |
| REQ-002 | A behaviour **registry** shall materialise each placed trigger into an `IMapEvent` by `kind`; an unknown kind → a typed error, not a crash. | unit |
| REQ-003 | The runtime shall dispatch placed triggers via the existing `WorldSim` (StepOn / ActionButton / `PressAction`) and apply their `Outcome`s. | unit + smoke |
| REQ-004 | A built-in `ShowText` behaviour shall show its configured text on `ActionButton` with **no agent / no custom code**. | unit + smoke |
| REQ-005 | Every behaviour (built-in or agent) shall implement `IMapEvent` and carry an `.expect` contract validated by gate:13. | gate:13 |

## Scope Notes — the slice sequence (bottom-up, vision-aligned)

- **Slice 1 — ✅ DONE (#18, ticket 4afa20c7):** triggers as `$data` + the `BehaviourRegistry` (single binding
  point) + runtime load/bind/dispatch, proven with the **first built-in `ShowText`** (a no-code dialogue you walk
  up to and press Space in the running game). Contract-first, gate-green (Engine MSI 86.26 / oracle 5×3).
  **Slice 2 is next.**
- **Slice 2 — the built-in library:** `GiveItem`/chest (stateful, open-once), `Door`, `Shop`, and `Warp`
  (needs multi-map). Each is one registry entry + an `IMapEvent` + an `.expect`.
- **Slice 3 — UI placement (Studio):** drop + name trigger markers on tiles, pick the behaviour kind, fill
  params → writes the `$data` placements (the "light event-placement UI; GUID identity" from the roadmap).
- **Slice 4 — the agent authoring flow:** describe a custom behaviour → the agent scaffolds (#11) + writes the
  `IMapEvent` module keyed to the trigger id + its `.expect` → the gate validates → it runs.

### Out (for now)
- Autorun / Parallel triggers (the `EventTrigger` enum is StepOn / ActionButton today — additive later).
- Multi-map (needed before `Warp` is meaningful).
- A richer dialogue *system* (portraits, choices, scrolling) beyond plain `ShowText`.
- The shop / menu *UIs* (a built-in `Shop` behaviour can land before its full UI).

### Foundation already in place (we EXTEND, not reinvent)
`IMapEvent` + `IEventContext` + `Outcome` + `EventTrigger` (the published seam, #13) · `WorldSim` dispatch
(`HookSchedule`, `PressAction`, step-on, the ambiguity check) · the event scaffolder (#11) · the `.expect`
oracle (#10, gate:13) · `MapSerializer`/`$data` (#12). The new work is the **placement schema + the registry +
the built-in library + the agent flow** — all conforming to the existing `IMapEvent` contract.

## Promotion Checklist
- [ ] Forge ticket created (per slice).
- [ ] Pipeline spec/notes pair created.
- [ ] `ticket:` frontmatter updated.
- [ ] `pipeline_spec:` frontmatter updated.
- [ ] `status:` changed to `promoted`.
