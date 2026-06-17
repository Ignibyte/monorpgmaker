# Strategy & Positioning

> Status: adopted 2026-06-17, from the adversarial review + competitive research. Answers the
> survival questions the architecture docs don't: who this is for, why they adopt it over a paid
> rival, how cold-start is solved, and how the project sustains itself. Companion to
> [agentic-overview.md](agentic-overview.md).

## Target user — the technical hobbyist

monorpgmaker is for the **developer-adjacent creator**: someone who reads C# (and can sanity-check a
diff), wants to ship a 2D RPG to **desktop and consoles** from one codebase, and treats an AI agent +
the inspector as a *force multiplier* — not someone who refuses to see code. We explicitly **do not**
chase RPG Maker's no-code base: the review found that promise is a trap (it reintroduces the code +
a metered per-token loop they fled, and loses on the long tail of tiny event logic where the
click-path wins), and the architecture genuinely serves the code-literate user. The honesty is a
feature — it sets the right expectations and aims the moat where it's real.

## The wedge vs. RPG Architect (paid) and RPG Maker

The defensible wedge is **trust + console-safety + a verifiable extension store** — *not*
out-configuring the rival.

- **Trust + cost-certainty.** A permissive (MIT), royalty-free, foundation-governed tool with a
  no-rug-pull pledge beats a single-dev paid tool on total cost of ownership and durability. The
  Unity→Godot exodus is the proof; RPG Architect's structural gaps (single-dev bus factor, EA
  slipping, no plugin/extension model yet) are exactly what an open, extension-first posture fills.
- **Console-safe off-the-rails authoring.** RPG Architect's end-state is "make everything a config
  form" — an ever-deeper maze of dropdowns + Code Blocks + raw JSON; even near 1.0 its primary
  surface is still the visual command palette. Our compile-time-wired, deterministic, replayable,
  AOT/console-safe C# seam surface is a structurally better answer to *anything off the rails* — and
  consoles are a market RPG Maker / RPGA reach awkwardly or not at all.
- **A verifiable module/asset store.** The unit of exchange is the dual-truth bundle (gate-clean
  `.cs` + `.expect` + test), indexed by `contract-manifest.json` — a *verifiable-by-construction*
  trust property no other store has. "This module passes the gate and reproduces these expectations"
  is checkable, not promised.

## Cold-start — on the critical path

The moat-skills are "derived from gate-clean code," so **that code must exist first** (this is also
why the build order now opens with a tracer bullet — see [roadmap.md](roadmap.md)).

- **One complete, gate-clean reference game** — small but real (a few maps, a battle, a quest). It is
  the worked example every shipped skill + scaffold fixture is *derived from*. Without it, the skills
  literally cannot exist.
- **An explicit asset story** — bundle/curate CC0 / OpenGameArt tiles, sprites, and audio so a new
  user has something to point the maps + database at on day one (RPG Maker's RTP is a real moat).
- **A zero-AI default path** — the 90% case (a standard battle, stock effects/states/dialogue) is
  *configured*, not generated: directly-instantiable reference modules + data, no agent and no API
  budget required. The agent is for going *off* the rails, not for the common case.

## Sustainability & governance

- **License: MIT**, marketed with an explicit **no-rug-pull guarantee**. The "don't hack core / semver
  the seams" rule is the *technical* backbone of that legal promise.
- **BYO-API-key by default** — the tool is free; the user funds their own tokens; no metered
  middleman. The zero-AI path means the tool is fully usable with no key at all.
- **Funding** — a Tiled/Blender-style engine: GitHub Sponsors / Open Collective, published
  use-of-funds.
- **Governance** — foundation-shaped early (bus factor is the #1 OSS failure mode); track
  **MCP-as-an-open-standard**, not one vendor's agent features.

## The AI-agnostic claim, made honest

"AI-agnostic" means **interface-neutral with a published capability floor**, not "every model is
equal." We ship:

- An **agent capability matrix** — a standing benchmark of representative authoring tasks run
  end-to-end-to-gate-green across a frontier / mid-tier / named-local model matrix, reporting
  pass-rate + loop-count. Converts "any capable agent" from marketing into a regression-tested support
  claim.
- **Analyzer code-fixes** — every fail-loud diagnostic ships the exact source edit (the missing
  `[DependsOn]` line), so the iterate-to-green loop collapses for weaker/cheaper agents.
- The **zero-AI path** above — the floor for the common case is *no model at all*.

## What RPG Architect teaches us (borrow, don't copy)

Match these 2D strengths, built our way: **author-defined stats + a formula engine** (compiled to
gate-clean C#, not runtime-string-eval'd), a **Data Sources** binding model for UI, **one configurable
battle core** spanning turn-based / ATB / on-map (our seam-coverage test matrix), and **per-entity
local state with a persist toggle**. Beat its walls: no runtime string eval, no config-form maze, real
extensibility, console safety, open governance.
