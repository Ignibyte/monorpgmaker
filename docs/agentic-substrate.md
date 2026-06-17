# Agentic Substrate & Architecture

> The technical realization of [agentic-overview.md](agentic-overview.md). Derived from the 10-agent
> feature research and **hardened (v2, 2026-06-17) after an adversarial review** — see
> [decisions.md](decisions.md) D-0015…D-0020. **Status: proposed design, not yet built;** the build
> order ([roadmap.md](roadmap.md)) now opens with a hand-wired tracer bullet, and the chassis below is
> built as an *extraction* from it. Skills: [agentic-skills.md](agentic-skills.md). Strategy:
> [agentic-strategy.md](agentic-strategy.md).

## 1. Five rings

```
MonoGame                       host: SpriteBatch, GameTime, audio/input, content pipeline
  └ MonoRpgMaker.Engine          core runtime (never forked); its default impls double as the
  │                               gate-clean scaffold fixtures
  ├ MonoRpgMaker.Abstractions    the published seam surface — pure contracts + the FixedPoint
  │                               primitive; the ONLY assembly a Project references. Semver'd.
  ├ MonoRpgMaker.Generators      a Roslyn generator (dumb, syntax-keyed emit) + the `monorpg validate`
  │                               metadata step that emits contract-manifest.json
  └ Project = DATA + MODULES      UI-authored $data + agent-authored C# modules → Abstractions only
```

The hard invariant chain holds (bounded blast radius, non-breaking upgrades, replayable modules), and
`NetArchTest` enforces Project → Abstractions-only. **The chassis is built as an extraction from the
tracer-bullet slice (D-0015)** — so every ring is designed against a known-good emit target rather than
up-front.

## 2. The wiring tier — a split generator + a validator, with a fallback

The biggest v1 risk was loading ONE Roslyn generator with four code-gen domains + cross-assembly DAG
validation in the hot IDE loop. v2 splits it (D-0018):

- **The generator** does ONLY dumb, syntax-keyed, incremental-safe emit (per-module stubs, registration
  shims, `FlagsGen`) and **always emits a compilable, degraded composition** — even when validation fails,
  so the build error is a clean diagnostic, never a 40-error `CS0246` cascade.
- **`monorpg validate`** (a separate MSBuild/CLI step over compiled metadata) does DAG validation + the
  topological order + emits **`contract-manifest.json`**, surfacing structured errors *before* the C#
  compile. A typo'd dependency is `MRM0002` with the misspelled id, not a `CS8785` generator-crash.
- **The manifest is the source of truth;** the generator is one producer of it. A **non-generator fallback
  wiring path** (hand/CLI-materialized composition) is first-class, and the gate diffs the two — so a slow
  or buggy generator never zeroes the project.
- A measured **<1s incremental author-edit-to-rebuild budget** is itself a gate (the tight loop the thesis
  depends on).

Diagnostics: `MRM0001` cycle · `MRM0002` missing dep · `MRM0003` ambiguous order · `MRM0004` multi-bind ·
`MRM0005` unmigrated state · **`MRM0006` expectation-mismatch (§7)**. Each ships an **analyzer code-fix**
carrying the exact edit, so the iterate-to-green loop collapses for weaker/cheaper agents (D-0020).

## 3. Scaffolding — gate-clean *by construction* (the aic guarantee-A)

v1 had the strict gate but let the LLM free-write 100% of every module body. v2 adds the missing leverage
(D-0017): **`monorpg scaffold <kind> <id>` is a deterministic `{{var}}` template emitter** (no LLM, no
`eval`) that emits a skeleton **mirroring a committed gate-clean fixture** per seam-kind —

- method signatures **return the shared outcome vocabulary**, so a raw `target.Hp -= n` won't compile;
- `FixedPoint` + injected `IRandom` are pre-wired; XML-doc stubs present; a **co-located seeded replay
  test** is emitted; the result is **gate-green by construction**, leaving only `// fill: <thinking>` holes.

Each engine area **owns its templates**; emitted code mirrors a committed **fixture module** that
`bin/gate.sh` runs as a golden, and templates are CI-diffed against their fixtures so they can't rot. Every
shipped **skill is *derived from* its fixture** (a Reference Pattern + a Quality Checklist mapping 1:1 to
gate checks), not prose written ahead of code; a meta-gate fails if a skill drifts from the manifest. This
is the aic discipline: *the AI doesn't guess — the scaffolder builds the shape, the agent fills the thinking.*

## 4. Seam taxonomy — the unified hook surface

Seam **kinds** (v2 adds two additive kinds for genre reach — D-0019):

| Kind | Semantics |
|---|---|
| **hook** | many ordered subscribers at a named point |
| **service** | single-bind, **layerable** (ordered decorator chain) so extension ≠ wholesale replacement |
| **component** | behavior-emit — a pure function returns an intended action; the system commits |
| **registry** | id-resolved behavior data (effects/traits/text-codes) |
| **data-schema** | persisted state / record shapes |
| **presenter-extension** *(new)* | a Project registers a NEW visual layer (`IPresentationLayer`) driven by its own save-state, drained via an open vocabulary — the presenter stops being a closed wall |
| **persistent-spatial** *(new)* | a per-cell/region store that ticks + persists (farming soil, fog-of-war), distinct from transient triggers |

The concrete seams are otherwise as v1 — `IModule`, ordered hooks, swappable **+ layerable** services
(`IBattleSystem`/`IPresenter`/…), the shared `IEffect`/`ITrait`/`IDamageFormula`/`IStateBehavior` registry,
spatial triggers, behavior-emit components, the `EventContext` semantic-verb surface, UI seams,
`ISaveStateComponent`, the `IPresentation` stream, `ITextCode`, the input map — plus the two new kinds and a
**sub-tick offset recorded in the `InputFrame`** so rhythm-style timing stays replayable. Semver is honest:
**additively stable within a stated genre envelope; a new genre ships a new seam kind in a minor version.**

## 5. Engine primitives

| Primitive | Why | Needed by |
|---|---|---|
| **`FixedPoint` (Q16.16)** — the only sim numeric type | bit-identical math across CPU/JIT/AOT; floats banned in sim by analyzer | everything in the sim |
| `IRandom` / `IDeterministicRng` (injected, serializable) | one randomness source; seed in the save; alternatives banned | everything |
| Fixed-step sim loop + strict sim/render split | structural determinism; headless via `NullPresenter` | all sim |
| Split generator + `monorpg validate` + manifest | the wiring tier (§2) | the chassis |
| `ISaveStateComponent` + versioned state + migration | per-module persistence; built-ins are components too | save + every module |
| Declarative **outcome** vocabulary (handlers *return* outcomes) | pure-over-context, replayable; **one shared vocab**; enforced by an outcome-return analyzer | database, battle, events, maps |
| `IPassability` + spatial query layer | single spatial truth | movement, maps, events |
| Ordered hook context + `IModuleContext` | the anti-hook-hell primitive | the chassis; every hook |
| `IEnumerator`-yield coroutine (no Task/async in sim) | AOT-safe, single-thread, replayable resumable flow | events, dialogue, battle |
| Scene-graph + retained draw (D-0006) | the cascade MonoGame lacks; inspectable | menus, dialogue, A/V |

## 6. AI-agnostic substrate — interface-neutral with a published floor

Vendor-neutral, and *honestly* floored (D-0020):

- **`monorpg` CLI** — `seams`/`explain`, **`scaffold`** (the `{{var}}` emitter, §3), **`validate`** (§2),
  `test`, `simulate --seed`, `replay`. A thin reader of the manifest.
- **MCP server** — the same operations + the C# codegraph. The structured sibling of the CLI.
- **Portable markdown skills** — derived from fixtures (§3).
- **Hook manifest** — the single machine-readable contract (§2).
- **A published agent-capability matrix** — representative tasks run end-to-end-to-gate-green across a
  frontier / mid / local model matrix (pass-rate + loop-count), so "any capable agent" is a
  regression-tested claim, not marketing.
- **A zero-AI default path** — the 90% case (standard battle, stock effects/states/dialogue) is
  *configured* from directly-instantiable reference modules + data; no agent, no API budget.
- **Dual-truth, corrected (D-0013 amended):** scaffold (deterministic, once) → author (LLM fills holes) →
  **code-is-truth thereafter**; `intent.md` is a scaffold seed + changelog guarded by a drift-detector
  gate; re-steering is a localized reviewed `.cs` edit, never a stochastic from-scratch rewrite.

## 7. Determinism + correctness — the two properties that make it real

**Determinism by construction (D-0016):** `FixedPoint` for all sim math; the
float/`MathF`/`Vector2`/transcendental + `foreach`-over-`Dictionary` ban enforced by a **compile-time
analyzer**; the `IEnumerator`-yield coroutine; `NullPresenter` must yield a byte-identical trace. **The
replay-to-same-hash gate runs on NativeAOT + a second CPU arch (ARM64 + x64)** — so the moat is portable by
construction, not a localhost illusion. The no-reflection rule is **scoped to the sim path**, with enforced
carve-outs (save-migration reader, MonoGame content pipeline, STJ source generator) + a no-reentry analyzer.

**Correctness, not just mechanics (D-0017):** the 12 gates verify determinism / purity / layering /
invariants — none is *correctness*. So each module ships a **`<module>.expect`** table — human-confirmable
input→outcome rows in the shared outcome vocabulary (e.g. `poison, maxHp=4000, 1 tick ⇒ HpDelta=-200`) —
**authored in a step distinct from the implementing agent** (surfaced to the human as a fill-in grid).
**Gate #13 (`MRM0006`)** requires the module to deterministically reproduce every row, and the gate rejects
a module whose only tests are the agent's own output-mutations. The honest headline: *the AI authors
deterministic, well-formed logic the human semantically accepts, per module.*

## 8. Composition & ordering

Modules declare `[Module(Id)]`, `[DependsOn]`, `[RunsBefore]`, `[RunsAfter]`; the validator topologically
sorts; ambiguity is a build error (no last-wins, no positional order). Hooks fan out to ordered subscribers;
services are single-bind **and layerable** (ordered decorators). Behavior is composed and hooked in — never
forked.

## 9. Open decisions (most v1 questions are now locked)

**Locked in v2:** sim-math = Q16.16 fixed-point · coroutine = `IEnumerator`-yield · reflection scope =
sim-path-only + carve-outs (all D-0016) · generator/validator boundary + manifest-as-truth + fallback + <1s
edit budget (D-0018) · scaffold = `{{var}}` emitter + fixtures + `.expect`/gate #13 (D-0017) · two additive
seam kinds + layerable services + sub-tick `InputFrame` + genre-envelope semver (D-0019) · target user + MIT
(D-0020).

**Still open (lock in P2–P3):**
- The **shared outcome vocabulary** members (now also the type `.expect` rows + scaffold signatures use) —
  lock first in P2.
- **TraitDomain** catalog + fold rules (sum/product/max/or) — MZ parity reference.
- **Stable placed-event identity** — adopt the **Tiled-template GUID scheme** (a placed event = a template
  instance with a stable GUID + overrides; inter-trigger refs by GUID).
- **`ICellTransitionHook` granularity** (player-only default vs all entities) + per-entity RNG slicing.
- **Battle spatial state** — validate ATB-as-swap AND tactics-as-system over one `BattleState` before
  locking (RPG Architect's mode matrix is the test).
- **Ambience persistence** — does weather/tint persist into `$game`, and is the PresentationLog persisted?
- **Runtime command-dispatch survival** — does a numbered-command emit-target survive, or is it fully
  replaced by direct hook authoring?

## 10. Risks to design against (revised)

- **Thesis untested until the tracer ships** → P-(-1) hand-wired slice + the timing spike vs RPG Maker (D-0015).
- **Correctness gap** (gates verify mechanics, not semantics; self-grading loop) → `.expect` + gate #13 +
  independent oracle (D-0017).
- **Determinism = localhost illusion** until fixed-point + coroutine are locked → D-0016 + AOT/multi-arch gate.
- **Generator overload / cross-assembly incrementality / opaque throws** → split generator/validator +
  degraded emit + fallback (D-0018).
- **Scaffolding absent (guarantee A)** → `{{var}}` emitters + fixtures + analyzers (D-0017).
- **Dual-truth drift** → scaffold/author split + code-is-truth + drift gate (D-0013 amended).
- **Genre-bounded seams** → two new seam kinds + layerable services (D-0019).
- **Survival / go-to-market undocumented** → [agentic-strategy.md](agentic-strategy.md) (D-0020).
- **Reflection ban unverified vs the platform** → scoped to the sim path + carve-outs (D-0016).
