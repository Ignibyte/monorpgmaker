# Agentic Authoring — Vision & Architecture

> Status: adopted 2026-06-17; **hardened to v2** after an adversarial review — see
> [decisions.md](decisions.md) **D-0011…D-0020**, [agentic-substrate.md](agentic-substrate.md),
> [agentic-skills.md](agentic-skills.md), and [agentic-strategy.md](agentic-strategy.md). This is the
> canonical product thesis; it reframes [game-overview.md](game-overview.md). The engine,
> data-format, and map foundations **stand**. What v2 changed: the target user is the **technical
> hobbyist who reads C#** (not a no-coder — D-0020); dual-truth → **code-is-truth after first emit**
> (D-0013 amended); scaffolding is **gate-clean by construction** + an executable `.expect` oracle
> (D-0017); determinism is **fixed-point**, locked before any sim module (D-0016); the generator is
> **split from a pre-compile validator** (D-0018); and the build order opens with a **tracer bullet**
> (D-0015). Where the text below still says "non-coder" or "regenerate from spec," read it through
> those decisions; the hardened detail is in the linked docs.

## 1. The inversion

RPG Maker's value proposition is **"make a game without programming."** Everything
— maps, data, *and logic* — is authored through a heavy visual UI: dialogs,
dropdowns, and a ~100-command event palette. That UI is not an accident; it is the
*consequence* of "no programming." Its cost is RPG Maker's well-known downfall: the
moment you want something off the chosen path, you fight the tool.

monorpgmaker inverts the proposition: **an AI agent does the programming; the human
directs.** Once you assume a capable coding agent, the justification for the
dialog-and-dropdown maze evaporates for everything except the parts that are
genuinely spatial or tabular. We keep the UI where direct manipulation wins, and
replace it with agentic authoring where programming wins.

This is RPG Maker for the agentic era: the human curates assets, designs the spatial
world, defines the data, and *describes intent* for systems; the agent turns intent
into working, deterministic, console-shippable C# against a clean engine.

## 2. The division of labor

The dividing line is not "data vs. behavior" — it is **which representation has the
lowest authoring friction** for each task.

| Domain | Representation | Authored by |
|---|---|---|
| Spatial — tile maps, tilesets, collision, entity placement | direct manipulation | **Visual UI** (human) |
| Tabular/relational — the database (actors, skills, items, enemies, states…) | forms / grids | **Visual UI + agent** (data is AI-fillable) |
| Behavioral/systemic — events & world-logic, battle, menus, custom mechanics | code | **Agent-authored C# modules** |

Behavioral UI does not disappear — it **moves up the stack.** Because the simulation
is deterministic and replayable, the UI's job for behavior becomes *inspection and
steering*, not authoring: visualize a battle's state machine, scrub through a
simulated battle, live-tweak a parameter and watch it ripple. RPG Maker's UI is
forms-over-data; ours becomes a **debugger/inspector for AI-authored systems**. (v2: the
review found that *inspecting* a deterministic sim is not the same as *controlling* it, and
steering AI-written C# needs code literacy — so the committed target user is the **technical
hobbyist who reads C#**, not a no-coder; see D-0020 and [agentic-strategy.md](agentic-strategy.md).)

The single biggest change is the **event system.** RPG Maker's signature feature —
the event command list — is imperative programming wearing a UI trenchcoat. It is the
thing most painful to do "off the rails," and the prime candidate for agentic
authoring. (See D-0011.)

## 3. The layering — "don't hack core"

```
MonoGame                  host / platform (window, GL, input, audio, game loop)
  └ MonoRpgMaker.Engine     the RPG framework — "core". Game authors never edit it.
      └ Abstractions          the published seam surface the agent programs against
          └ Project             UI-authored DATA + agent-authored MODULES that hook in
```

The load-bearing rule, borrowed from Drupal: **don't hack core.** A project extends
the engine only through published seams; it never forks the engine. This is not a
style preference — it is the **safety property that makes AI authorship viable**:

- **Bounded blast radius.** The agent's output lives in the project layer; it
  *cannot* corrupt the engine. That guarantee comes from architecture, not from
  prompting carefully — exactly what you want when a machine writes the code.
- **Non-breaking upgrades.** Core evolves; every project keeps working, because
  projects depend only on the semver'd seam surface. (Drupal's worst historical pain
  was cross-major API churn — semver the seams religiously.)
- **Tractable verification.** Test the engine once; per project, only verify that the
  modules satisfy their contracts.

`MonoRpgMaker.Abstractions` (the seam surface) is the engine's *published promise* and
the real product surface the agent targets. NetArchTest already enforces layer
boundaries in the gate; "don't hack core" becomes a new ring: **Project →
Abstractions only, never engine internals.**

## 4. The Drupal model — and its landmines

| Drupal | monorpgmaker |
|---|---|
| core (don't hack) | the Engine |
| hook / plugin / event system | the engine's seams + registry |
| contrib/custom modules | game modules (battle, quests, menus, mechanics) |
| themes | rendering / UI skins (separately pluggable) |
| content + config | maps (content) + the database (config) — UI-authored |
| "the Drupal way" tribal knowledge | **the shipped AI skills** ← the moat |

Two failure modes the analogy drags in, and our stance on each:

1. **Hook hell / load-order.** When many modules hook the same point, behavior
   becomes emergent and order-dependent (RPG Maker MV/MZ have the identical
   plugin-order disease). → Prefer **explicit, declared composition** — stated
   ordering + dependencies — over implicit "everyone gets called." An agent manages
   explicit ordering well; emergent ordering, no one does.
2. **Over-abstraction vs. budget.** Drupal's reflection-heavy, runtime-discovered
   generality is too slow and too dynamic for a **console perf budget + a determinism
   requirement.** → Take Drupal's *concept* but wire seams at **compile time via C#
   source generators**, not runtime reflection. That is AOT-safe (hence
   console-safe), fast, deterministic, and inspectable. (See D-0014.)

## 5. Source of truth — dual-truth, and the shipped pipeline

What does the agent emit, and what is canonical?

- **Core** is human-engineered C#; code is truth. It is not regenerated per project.
- **Project data** (maps, database) is UI-authored; the data files are truth.
- **Project modules** use **dual-truth**: an **intent-spec** describes what the module
  should do (what the human edits), and the **generated C# is a committed, gated
  artifact** (the verified behavior). Both live in version control. Changing a system
  means re-describing intent and **re-running a reviewed pipeline** — not trusting a
  one-shot generation.

This is the project's *own* development methodology, scoped and handed to the game
author: **spec → generate → inspect → gate.** monorpgmaker is built by an agentic
pipeline, and it **ships** a scoped one. The thesis closes on itself.

**Determinism is what makes any of this verifiable.** With injected RNG and a strict
simulation/rendering split, an AI-authored system can be asserted against invariants
("HP never negative," "turn order stable," "no skill targets a dead actor") and
**replayed**. You can verify determinism and invariants; you cannot verify *fun* — so
authoring is a **collaboration loop** with the human as judge, not a one-shot
generator. Designing for that loop up front is the difference between a tool and a
tech demo.

## 6. AI-agnostic by construction

The agentic layer must work with **any** capable coding agent — Claude Code, Cursor,
Aider, a GPT- or Gemini-based agent, a local model — and never depend on a single
vendor. We achieve that by expressing agent capability as **portable, open
substrate**, not vendor features:

- **A CLI** (`monorpg …`) — scaffold a module, list/explain available hooks, validate,
  test, run/replay. The universal substrate: every agent can run a shell command.
- **An MCP server** — the engine's contracts, the codegraph, and scaffold/validate/
  test tools, over an open protocol multiple vendors already speak.
- **Portable markdown skills/playbooks** — the procedural "how to build X" knowledge,
  as files any agent can read (the same shape as `AGENTS.md` / `CLAUDE.md`).
- **A machine-readable hook/contract manifest** — so any agent can *discover* the
  seams without vendor-specific magic.

Any specific agent (including Claude Code, which we use to build the engine) is one
**client** of this substrate, not a dependency of it. (See D-0013.)

## 7. The moat — a co-designed framework and skill set

The differentiator is neither the framework nor the AI alone — it is that they are
**co-designed.** RPG Maker ships docs and hopes you read them; "the Drupal way"
gatekeeps behind years of tribal knowledge. monorpgmaker ships that expertise as an
**active agent capability**, the way this repo ships its own pipeline skills.

This flips the framework's quality metric. It is no longer "is this API elegant to a
human" — it is **"can an agent reliably author a *correct* module against it?"** That
changes design decisions:

- **One obvious way** to do each thing (not Drupal's five).
- **Fail loud at compile time** — an agent loop treats a red build as a great signal.
- **Self-describing, enumerable seams** — the agent discovers "what can I hook here?"
  via the codegraph + the manifest.
- **Default implementations double as canonical examples** — the reference battle
  module is not just a default; it is the worked example the agent learns the pattern
  from.

## 8. The console constraint is an ally

"Ship to console from one C# codebase" usually kills dynamic scripting (consoles
forbid JIT / runtime codegen). That constraint **pushes us toward** the clean answer
anyway: generate C#, compile it ahead-of-time into the shipped binary, wire hooks with
source generators. A hard requirement that eliminates the worst failure mode of a
dynamic-plugin approach for free. (Reinforces D-0001.)

## 9. What this changes vs. the prior plan

This reframes the **authoring model**, not the engine. It is concentrated on one axis:

- **Supersedes D-0007** — behavior is **agent-authored modules hooking into engine
  seams**, not a visual command-palette interpreter built 1:1 with a ~100-command
  editor. (A runtime command/effect dispatch may still exist as *one* target the agent
  can emit; it is no longer the authoring surface.)
- **Inverts D-0008** — **agentic C# authoring is primary** for behavior; the visual
  editor is **maps + the database + an inspector** for AI-authored systems. (D-0008
  made visual primary and C# the optional escape hatch. The *swappable-battle-behind-
  an-interface* half of D-0008 stands.)
- **Reframes** the `game-overview.md` audience: makers still don't hand-write systems —
  they *direct an agent that does.*

What **stands** (most of the prior research): MonoGame→console (D-0001), net10
(D-0002), the forge + C# codegraph (D-0003/4, now the agent's map of the engine), the
**$data/$game split** (D-0005, the "UI for data" half), the scene-graph layer (D-0006),
the **TMX/Wang map model** (D-0009), the gamepadMapper input indirection (D-0010), and
the **Database + Map visual editors.** The pivot keeps the foundations; it changes how
systems are built on them.

## 10. Open questions (the feature research resolves these)

- The exact **seam taxonomy** per system (hooks vs. interfaces vs. components vs.
  registries) and the unified Abstractions surface.
- The **module registration + ordering + dependency** model (the anti-plugin-hell
  design) and how source generators wire it.
- How **module state** declares itself into the `$game` save, and **save migration**
  when a regenerated module changes its state shape.
- The shipped **AI skills catalog** and the **`monorpg` CLI / MCP / manifest** surface
  in detail.
- The **inspector/steering UI** scope — how much of "UI moves up the stack" is in the
  first cut.
- Where exactly **data ends and behavior begins** for skills/states (the parametric
  middle).
