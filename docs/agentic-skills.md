# AI Skills & Commands Catalog

> The agent-facing authoring layer for monorpgmaker, derived from the 10-agent research
> (2026-06-17). **Status: proposed, not yet built.** These skills author against the seams
> in [agentic-substrate.md](agentic-substrate.md). Every skill is **vendor-neutral**: a
> markdown playbook + CLI/MCP affordances any capable agent can use (see
> [agentic-overview.md](agentic-overview.md) §6).

## How a skill is packaged

A skill is **not** a Claude-Code feature, and (v2 — D-0017) it does **not** ask the agent to
free-write or copy-and-adapt a reference body (that was aic's *rejected* mode). It is:

1. A **deterministic `{{var}}` scaffolder** (`monorpg scaffold <kind>`) that emits a
   **gate-clean-by-construction** skeleton mirroring a committed **fixture** — signatures already
   return the shared outcome vocabulary (so a raw state mutation won't compile), `FixedPoint`/
   `IRandom` pre-wired, a seeded test co-located. The agent fills only the `// fill:` holes.
2. A **markdown playbook *derived from* that fixture** — a Reference Pattern + a Quality Checklist
   mapping 1:1 to gate checks + the determinism rules (`FixedPoint` only; inject `IRandom`; no
   floats / `DateTime` / `Random` in sim). CI-diffed against the fixture so it can't drift.
3. A **dual-truth output, corrected**: `intent.md` is a scaffold seed + changelog; **the `.cs` is
   the truth after first emit**; plus a `<module>.expect` table (input→outcome rows, confirmed by a
   step distinct from the implementing agent), `$data`, and a seeded replay test. A skill's job
   isn't done until that bundle passes the gate (now including gate #13 / `MRM0006`).

Any agent that can read a file, run `monorpg`, and fill C# holes can author games — and a published
**capability matrix** floors what "capable" means, with a **zero-AI default path** for the 90% case
(configure stock modules, no agent). Claude Code is one client; Cursor/Aider/GPT/local call the
identical CLI/MCP.

## The `monorpg` CLI surface

| Group | Commands |
|---|---|
| Discover | `seams [--json]`, `explain <seamId>` (contract + current ordered subscribers) |
| Scaffold | `new module <id>`, `scaffold <kind> <id>` (kinds mirror the seam families) |
| Validate | `validate` (restore-locked build `-warnaserror` → generator diagnostics + schema/manifest checks) |
| Test | `test [--module <id>] [--replay <trace>]` |
| Simulate | `simulate <battle\|walk\|damage\|…> --seed N` (headless deterministic; emits event/transition log + state hashes) |
| Replay | `replay <trace>` (re-run to same state hash), `--dump-presentation-log` |
| Inspect | `save inspect`, `ui preview --png`, the editor inspector (manifest + replay logs) |

## Skill catalog

### Foundation (every author-\* skill builds on these)
| Skill | Produces |
|---|---|
| `explain-seams` | a *grounded* seam report (read the manifest + codegraph; never invent from training data) — the precondition for all authoring |
| `author-module` | the base skill: `<module>.intent.md` + `<Module>.cs` implementing `IModule` with declared deps/order + a starter replay/invariant test |
| `validate-module` / `reorder-modules` | maps a generator diagnostic (cycle/missing-dep/ambiguous-order/multi-bind/unmigrated-state) to the exact source fix |
| `inspect-replay` | a recorded `(seed, input)` trace fixture + a replay test asserting the golden state/event stream |

### Database → behavior (effects, traits, states, formulas)
| Skill | Scope | Produces |
|---|---|---|
| `author-effect-module` | **Database + Battle (one seam)** | `IEffect` module + registry attr + `$data` field binding + editor `InputSpec` + damage-probe test |
| `author-trait-module` | Database | `ITrait` module + aggregation test (composes with stock traits) |
| `author-state-behavior` | Database (states/buffs) | `IStateBehavior` + state-machine test (apply→N ticks→auto-remove, seeded) |
| `author-damage-formula` | Database + Battle | `IDamageFormula` + golden `(A,B,seed)→damage` table |

### Maps & world
| `author-map-module` / `author-region-trigger` | Maps | `IMapModule` + `[OnRegion]`/`[OnZone]`/`[OnTerrain]` handlers + manifest patch + seeded simulate-walk snapshot |
| `author-encounter-logic` | Maps | `IEncounterResolver` + replay fixtures asserting troop choice for a seeded walk |
| `author-atmosphere-driver` | Maps + A/V | parallax/weather driver returning per-frame *state* (never draws) + snapshot test |

### Movement
| `author-move-behavior` / `author-pathfinding-policy` / `author-vehicle` | Movement | sealed `[MoveBehavior]`/`IVehicle`/pathfinder + editor manifest entry + golden cell/path replay test |

### Events (the inversion keystone)
| `author-event-behavior` / `compose-common-behavior` | Events | `<event>.intent.spec.md` + `IEventBehavior` coroutine + page/placement patch + replay stub. **Semantic verbs, not 1:1 palette mirrors (lint-enforced)** |
| `define-flags` / `author-quest` | Events | flag/quest `$data` + regenerated `FlagsGen` typed accessors + quest modules + monotonicity invariant tests |

### Dialogue
| `author-dialogue-flow` / `author-text-code` / `author-message-view` | Dialogue | `IDialogueFlow` (shared coroutine) + message rows by `LocKey` (never inline literals) / `ITextCode` (presentation-only) / `IMessageView` |

### Battle (flagship swap)
| `author-battle-system` / `author-turn-scheduler` / `author-battle-ai` / `author-phase-hook` | Battle | the battle module + manifest registration + golden replay test (seed→event stream) / scheduler ordering / AI action-pin test |

### Menus & in-game UI
| `author-menu-scene` / `author-window-widget` / `bind-ui-action` / `theme-and-skin-ui` | Menus | `Scene`/`Window` module + menu-schema JSON + handler with invariant test (gold ≥ 0, inventory conserved) + golden retained-draw-list test |

### Audio / VFX
| `author-effect-trigger` / `compose-effect-sequence` | A/V | `IEffectModule`/`CompositeEffect` + intent-spec + PresentationLog golden + sim-invariance test (`NullPresenter` == real) |

### Save / load (consumed by every stateful module)
| `author-save-state-component` / `author-save-migration` / `verify-save-determinism` | Save | `[SaveState]` record + Capture/Restore + Migrate + old-save fixture + round-trip & replay-to-same-hash tests |

### Cross-cutting data-fill (the AI-fills-data path, parallel to the human visual editor)
| `author-animation-data` / `theme-skin-content` | Data | validated `$data` JSON (Animations/Messages/Menu/Theme/Database grids) — **no C#**, pure data with no flow |

## The one rule every skill enforces

**Never re-import the command palette.** The `EventContext`/`IMessageService`/
`IPresentation` surfaces expose *semantic verbs* (Dialogue/Transfer/Battle/Flags/Quest);
branching and looping use native C# control flow; flow-control text codes are banned. An
analyzer fails the build if a context method mirrors a single MV command code. This is the
discipline that keeps the inversion from collapsing back into RPG Maker.
