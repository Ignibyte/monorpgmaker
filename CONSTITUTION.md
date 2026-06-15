# monorpgmaker Constitution

The binding rules for building monorpgmaker. The pipeline commands and the
`.claude/hooks/` enforcement layer treat this document as law. When a hook
blocks you, it cites a section here. Sections are stable anchors — hooks grep
for `§N`.

monorpgmaker is a remake of the RPG Maker authoring tool + runtime on
[MonoGame](https://monogame.net): a C# engine (`src/MonoRpgMaker.Engine`), a
desktop player (`src/MonoRpgMaker.Player`), the maker/editor
(`src/MonoRpgMaker.Editor`), and xUnit tests (`tests/`). It is paired with the
**oathstar-forge** knowledge sidecar (MCP server `forge` in `.mcp.json`) — the
pipeline records what it learns there and recalls it on the next run. The whole
point of the MonoGame rebuild is **console reach** (§ docs/technical-architecture.md).

---

## §0 — Quality Gates (binding)

The canonical gate is **`bin/gate.sh`** — the .NET analogue of a strict quality
stack. The FULL gate (11 gates) must pass green before `/commit`; `--fast` runs
the 9 static gates for a quick local loop but prints `GATE GREEN [fast]` — only
a FULL green writes the receipt the commit hook requires (§15), so `--fast`
can't satisfy `/commit`.

```
gate:1  format         dotnet format --verify-no-changes
gate:2  build          dotnet build -warnaserror      (Roslyn analyzers, warnings = errors)
gate:3  test           dotnet test
gate:4  vuln deps      dotnet list package --vulnerable
gate:5  secrets        gitleaks (history + working tree)
gate:6  shell lint     shellcheck -S info (hooks + bin)
gate:7  no-suppress    grep meta-gate (#pragma warning disable / [SuppressMessage] + justify)
gate:8  source-bans    grep meta-gate (Process.Start / Environment.Exit / unsafe)
gate:9  doc-todos      grep meta-gate
gate:10 coverage       dotnet test --collect "XPlat Code Coverage"  (line floor)   [FULL]
gate:11 mutation       dotnet stryker  (MSI floor)                                  [FULL]
```

Static analysis is **on by default** (`EnableNETAnalyzers`, `AnalysisLevel=latest-recommended`
in `Directory.Build.props`); gate:2 makes it blocking with `-warnaserror`, so the
inner dev loop stays usable while `/commit` is strict. Rule severities tune in
`.editorconfig` (visible + reviewable) — never with an inline suppression.

**No baselines. No suppressions. Source-fix only.** Any `#pragma warning disable`
/ `[SuppressMessage]` in game source (`src/`) must carry a real `//` justification
(gate:7); a blanket disable (no warning code) is banned outright. Banned source
primitives — process spawning, `Environment.Exit`/`FailFast`, `unsafe` without a
`// SAFETY:` — fail gate:8. A skipped/`Skip=`'d test is a violation unless the
spec's test plan records why and a follow-up exists.

**Floors ratchet up, never down — and never below the §0 minimum.** The minimums
(`NET_COV_MIN=80`, `MUT_MSI_MIN=80`) are baked into the gate; env may *raise* a
floor but a lower value is clamped back up. These are deliberately set at a young
project's reach (actuals currently ~94% line coverage, ~84% mutation MSI) and
ratchet toward parity with a mature codebase (94% / 100%) as the engine grows.
Lowering a floor to pass is a charter violation — write the test.

**Known scope (honest).** Coverage gate:10 is a *line* floor over the assemblies
the tests load (the Engine). The MonoGame host (`RpgGame`) is the composition
root — it owns the live loop with no unit-testable contract and is
`[ExcludeFromCodeCoverage]` (the C# analogue of an excluded `main`); the testable
world/entity/data logic it drives is covered in its own classes. The Player and
Editor are thin hosts over the Engine. Mutation gate:11 runs Stryker over the
Engine. Not yet gated: license/supply-chain policy beyond the vulnerable-package
check, architecture-layering enforcement, and per-file coverage — the ratchet
roadmap, recorded so the gap is explicit.

The gate runs ALL steps and reports each; a single red step fails the gate.
Every verdict is the tool's exit code, never a grep of its output. On a FULL
green it writes a worktree-bound receipt (`.git/monorpgmaker-gate-receipt`) that
`enforce-commit-gate.sh` validates at commit (§15).

---

## §3 — Phase Gates (binding)

Work flows through an ordered pipeline. **Every phase has an entry gate: the
previous phase must be `PASS` (and, for plan/design, human-confirmed) before
the next begins.** The `enforce-phase-gate.sh` PreToolUse hook blocks
`Write`/`Edit` to application code until the gate is satisfied.

```
/work        pre-flight (env, forge, bulletins, context) → hands off to plan
  → /pipeline:plan       (Phase 1)  forge ticket/doc + active spec/notes
  → /pipeline:design     (Phase 2)  design + regression test plan
  → /pipeline:implement  (Phase 3)  code
  → /pipeline:inspect    (Phase 3.5) adversarial review checkpoint (§18)
  → /pipeline:validate   (Phase 4)  write + RUN tests; gate green
  → /pipeline:complete   (Phase 5)  docs + AAR to forge + archive
  → /commit              delivery gate: full bin/gate.sh + commit/PR
```

- **NEVER have two pipeline documents active** in `docs/planning/pipeline/active/`.
- A pipeline doc is a `<title>.spec.md` + `<title>.notes.md` pair. The `.spec.md`
  carries `pipeline_id:` (a real UUID) and `status:` frontmatter. Phase advance =
  setting `status: Phase N — <Title> PASS; ready for Phase M — <Title>`.
- Work that is not ready for a forge ticket lives in `docs/planning/intake/`.
- Every forge ticket created for monorpgmaker work must have a local ticket
  document in `docs/planning/tickets/open/` or `closed/`; the `ticket:`
  frontmatter is the canonical link.
- Pipeline acceptance criteria use EARS: `shall`, one observable behavior per
  requirement, and a verification method for each.
- Application code = `src/**/*.cs`, `tests/**/*.cs`. Docs, `.claude/`, project
  files (`*.csproj`/`*.props`/`*.slnx`/`*.editorconfig`), config, and
  `docs/planning/` are not gated.

---

## §7 — Testing Standards (binding)

**Full testing is expected.** Every pipeline produces meaningful xUnit tests for
the code it writes. Tests are not optional and not skippable because a change
"looks simple."

- **NEVER mark a phase PASS if tests did not actually RUN.** Writing a test file
  is not testing. The `enforce-tests-ran.sh` Stop hook checks the transcript for
  a real `dotnet test` invocation at `/pipeline:validate`.
- **Keep engine logic deterministic + testable.** Inject any RNG; keep
  simulation state separate from rendering so it can be exercised headlessly.
  Host/loop code that genuinely can't be unit-tested is `[ExcludeFromCodeCoverage]`
  with a one-line reason — not left to silently drag the floor.
- **Pre-existing failures are not your problem — but document them** in the notes
  as "pre-existing"; don't fix unrelated breakage unless asked.

---

## §14 — Code Conventions (binding)

**C#** (`src/`, `tests/`):
- Matches the surrounding code's idiom; `dotnet format` is law (gate:1).
  Nullable reference types on (`<Nullable>enable</Nullable>`); explicit usings.
- Don't throw on input-reachable paths that should return a typed result; use
  guard helpers (`ArgumentOutOfRangeException.ThrowIf…`) for genuine precondition
  violations only. Public members carry XML doc comments.
- Analyzer-clean under `-warnaserror`, `latest-recommended` (gate:2). A
  suppression needs a trailing-comment justification (gate:7); prefer fixing the
  cause or tuning severity in `.editorconfig`.
- Keep game logic in `MonoRpgMaker.Engine` (framework-thin) so console ports stay
  tractable. Separate simulation state from rendering. No hidden statics; no
  `DateTime.Now`/`Random` buried in engine logic (determinism).

**Both:** no secrets in source; reuse existing helpers before adding new ones
(`code-find` with `repo: "monorpgmaker"` first); comments explain *why*, not *what*.

---

## §15 — Anti-Circumvention (binding)

**The transcript is the source of truth. If it didn't happen in the
transcript, it didn't happen.** Claiming "tests pass" without a visible test
run is a violation. Claiming a gate is green without running `bin/gate.sh` is a
violation. Hooks evaluate evidence (tool calls, Bash commands, file state), not
prose.

Do not weaken a gate, delete a test, lower a floor, or add a blanket suppression
to get past a blocked Stop. Fix the cause.

**What the enforcement is — and isn't.** The hooks are a *discipline scaffold*,
not a security boundary. They reliably catch **omissions** — writing code before
a phase is PASS, stopping a phase with unresolved tasks, reaching
`/pipeline:validate` without running tests, completing without capturing
knowledge, committing code without a green gate. They do **not** try to defeat
deliberate fabrication: the `status:` line and the AAR/test/capture calls are
self-reported. The one hard, evidence-based gate is **`bin/gate.sh` at commit** —
`enforce-commit-gate.sh` blocks `git commit` of C# source unless a FULL gate run
left a *receipt* (`.git/monorpgmaker-gate-receipt`, a content fingerprint of the
gated source) that still matches the worktree being committed. The receipt is
written only by a real FULL green, so the verdict cannot be forged by printing or
quoting `GATE GREEN` in prose, by `echo`-ing it, or by reading a file that
contains it; and any edit after the green — by Write, Edit, or a Bash heredoc —
changes the fingerprint and re-blocks. The gate trusts only tool exit codes and
clamps floors to the §0 minimums. So: the pipeline keeps us honest; the commit
gate is what's load-bearing.

---

## §18 — Inspect & Explore (binding)

**§18.1 — Mandatory inspect checkpoint.** After implementation (Phase 3),
`/pipeline:inspect` runs an adversarial review before validation. It spawns
independent critics (correctness, security/secrets, data-integrity,
simplification) against the diff, then the lead reviews the findings and fixes
the real ones. The phase-gate requires Phase 3.5 PASS before
`/pipeline:validate`; populating the inspect ledger in the notes is required by
the command.

**§18.2 — Delegate broad file-discovery to the Explore subagent.** Any lookup
with more than ~3 candidate paths, or where the location isn't known a priori,
goes through `Agent(subagent_type=Explore)`. Inline grep walks don't substitute.

**§18.3 — Forge first.** Before planning and before implementing, recall prior
knowledge: `knowledge-context` / `knowledge-search` (lessons, failures,
prevention rules) and `docs-search` (the design docs); `code-find` /
`code-callers` over the codegraph (always pass `repo: "monorpgmaker"`). At phase
close, record what you learned: `aar-submit` (lessons), `failure-record` (what
bit you). The sidecar only gets smarter if you feed it.

---

## §19 — Forge Process

monorpgmaker owns its pipeline locally. The forge sidecar is a **shared**
instance (paired with the Oathstar project); monorpgmaker is its own *project*
+ *repo* tenant there, so its tickets, knowledge, and codegraph are isolated by
the bearer in `.mcp.json`. Capture to it, recall from it. `.mcp.json` is
owner-managed (gitignored — it holds the bearer); the `forge` server points at
the local sidecar (`../oathstar-forge/scripts/start-all.sh`).

---

## Amending this Constitution

These rules change deliberately, not mid-pipeline to dodge a gate. To amend:
state the section, the change, and the reason in a commit that touches only
this file (and any hook that enforces the changed rule). Raising a floor or
tightening a convention needs no ceremony; loosening one needs a recorded reason.
