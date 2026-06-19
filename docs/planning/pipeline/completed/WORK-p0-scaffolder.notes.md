# WORK-p0-scaffolder — Notes

## Phase 1 — Plan
- **Request:** P0 #11 — the scaffolder (`monorpg scaffold event <Name>`), a deterministic `{{var}}` emitter
  in the Editor CLI producing a gate-clean-by-construction handler skeleton + a `.expect` stub. **Autonomous
  goal run** (#11–#15): all phases auto-approved, then `/commit` + merge.
- **Classification:** work pipeline, feature. A new CLI verb + a template + the golden/verify tests. Reuses the
  Editor CLI (#10). One slice, medium.
- **Forge recall:** D-0017 / agentic-substrate **§3** — `monorpg scaffold <kind>` = a deterministic `{{var}}`
  template emitter (no LLM, no eval) mirroring a committed gate-clean fixture per seam-kind; signatures return
  the outcome vocabulary; only `// fill:` holes; templates CI-diffed against fixtures so they can't rot. **§1**:
  "the engine's default impls double as the gate-clean scaffold fixtures" → the tracer handlers ARE the fixture.
  The Editor CLI host + the `.expect` parser/runner already exist (#10). Reuse PRs:
  `PR-claude-analyzer-verifier-resolve-types-001` (the verify-compile test must resolve types / assert no CS
  errors), `PR-claude-analyzer-mutation-isolated-asserts-001` (isolated exact-count tests for Editor MSI ≥ 80).
- **Ticket:** d9016842-2086-4492-b65e-e01363e5295a (#11) — project a9ee8162 confirmed monorpgmaker.
- **AAR id:** a70b828b-24d4-45e8-b81a-dfb044597f93

## Phase 2 — Design

### Resolved decisions
- **(a) Template form** — embedded template strings in a `EventScaffold` static class (reviewable in source +
  golden-diffable), with `{{Name}}` / `{{Namespace}}` placeholders. Mirrors the `LeverEvent` shape.
- **(b) `{{var}}` set + CLI** — `scaffold event <Name> <out-dir> [--namespace <ns>]`; `<Name>` validated as a
  C# identifier (letter/`_` start, alphanum/`_` rest, non-empty); default namespace `MonoRpgMaker.Engine.Sim.Tracer`.
- **(c) Emission** — write `<Name>.cs` + `<Name>.expect` into `<out-dir>`; **refuse to overwrite** an existing
  file (REQ-006), exit non-zero. The renderer is **pure** (no IO); the CLI verb does the IO.
- **(d) Skeleton is *immediately* gate-clean** (not "replace the hole then compile") — the emitted `Run` body is
  a `// fill:` comment **plus** `return [];`, so it compiles + is analyzer-clean as-emitted; the `.expect` stub is
  one `=> (none)` row, which **matches** the skeleton's empty output (so the pair is oracle-consistent by
  construction). The author replaces both with real logic + rows.
- **(e) Can't-rot** — a GOLDEN test pins the exact rendered `.cs` + `.expect` for a fixed `(Name, namespace)`;
  the **verify** test renders into a **sim namespace** and feeds the `.cs` to the analyzer verifier harness,
  asserting **zero CS errors** (PR-claude-analyzer-verifier-resolve-types-001) **and** zero `MRM1001–1006`; the
  `.expect` is fed to `ExpectationParser` (Ok + ≥1 row).

### Architecture / approach
- **New `Scaffolding/` area in `MonoRpgMaker.Editor`:**
  - **`EventScaffold`** — `static (string CsFileName, string CsText, string ExpectFileName, string ExpectText)
    Render(string name, string @namespace)` (pure: validates + substitutes the templates; throws
    `ArgumentException` only on a precondition like an invalid identifier — the CLI validates first to keep the
    happy path total). Holds the two template strings; `IsValidIdentifier(string)` pure helper.
  - **`ScaffoldCli`** — `static int Run(IReadOnlyList<string> args, TextWriter)`: parse `scaffold event <Name>
    <dir> [--namespace ns]`, validate, check non-existence, `Render`, write both files, report + exit code.
  - **`MonorpgCli`** — `static int Run(IReadOnlyList<string> args, TextWriter)`: route `scaffold` → `ScaffoldCli`,
    `check-expectations` → `OracleCli`, else a usage listing both verbs. (Keeps `Program` a thin
    `[ExcludeFromCodeCoverage]` shell → `MonorpgCli.Run`; the router itself is tested.)
- The emitted skeleton (immediately gate-clean):
  ```
  using System.Collections.Generic;
  using MonoRpgMaker.Abstractions;
  namespace {{Namespace}};
  /// <summary>The {{Name}} map event — fill in its behaviour.</summary>
  public sealed class {{Name}} : IMapEvent {
      public {{Name}}(GridPoint cell) => Cell = cell;
      public GridPoint Cell { get; }
      public EventTrigger Trigger => EventTrigger.StepOn;
      public IReadOnlyList<Outcome> Run(IEventContext context) {
          // fill: read context.GetSwitch/GetCounter, then return outcomes
          //       e.g. [new ShowMessage("…"), new SetSwitch("a_flag", true)]
          return [];
      }
  }
  ```
  The `.expect` stub: a header `#` comment + `=> (none)`.

### File manifest
| # | File | Change |
|---|---|---|
| 1 | `src/MonoRpgMaker.Editor/Scaffolding/EventScaffold.cs` | **NEW** — pure renderer (templates + `Render` + `IsValidIdentifier`). |
| 2 | `src/MonoRpgMaker.Editor/Scaffolding/ScaffoldCli.cs` | **NEW** — the `scaffold event <Name> <dir>` verb (args + validation + no-overwrite IO + exit code). |
| 3 | `src/MonoRpgMaker.Editor/MonorpgCli.cs` | **NEW** — the top verb router (scaffold / check-expectations / usage). |
| 4 | `src/MonoRpgMaker.Editor/Program.cs` | **MODIFY** — `Main` → `MonorpgCli.Run`. |
| 5 | `tests/MonoRpgMaker.Engine.Tests/EventScaffoldTests.cs` | **NEW** — determinism + golden + identifier validation + rendered-`.cs` compiles+MRM-clean + rendered-`.expect` parses. |
| 6 | `tests/MonoRpgMaker.Engine.Tests/ScaffoldCliTests.cs` | **NEW** — scaffold→files (temp dir); bad-name/missing-args/existing-file→non-zero; the router. |

### Regression Test Plan
| # | Test | Proves |
|---|---|---|
| T1 | `Render` twice for the same `(Name, ns)` → byte-identical | REQ-004 |
| T2 | Golden: `Render("SampleEvent", "Demo.Ns")` → the exact pinned `.cs` + `.expect` strings | REQ-005 |
| T3 | `IsValidIdentifier` accepts valid names, rejects empty / `1Bad` / `"has space"` / `with-dash` | REQ-001/006 |
| T4 | The rendered `.cs` (sim namespace) **compiles** (zero CS errors) and is **MRM-clean** (no `MRM1001–1006`) via the analyzer verifier harness | REQ-003 |
| T5 | The rendered `.expect` parses under `ExpectationParser` (`Ok`, ≥ 1 row) | REQ-002 |
| T6 | The rendered `.cs` is a `sealed class <Name> : IMapEvent`, `Run` returns `IReadOnlyList<Outcome>`, contains a `// fill:` hole | REQ-001 |
| T7 | `ScaffoldCli`: `scaffold event Foo <tmp>` → `Foo.cs` + `Foo.expect` written with the rendered content; exit 0 | REQ-001/002 |
| T8 | `ScaffoldCli`: missing `<Name>` / bad identifier / existing target file → non-zero + a clear message, **no partial write** | REQ-006 |
| T9 | `MonorpgCli` routes `scaffold` → ScaffoldCli, `check-expectations` → OracleCli, unknown → usage(exit 2) | REQ-001 |
| T10 (gate) | FULL `bin/gate.sh` green — Editor coverage ≥ 80 + MSI ≥ 80 | REQ-007 |

**Uncoverable:** `Program.Main` — `[ExcludeFromCodeCoverage]` one-liner → `MonorpgCli.Run`.

### Risks / decisions
- **R1 — the skeleton must stay gate-clean as the analyzers evolve.** T4 runs the *real* MRM analyzers over the
  rendered `.cs` in a sim namespace, so a future analyzer rule that the template would violate fails T4 (the
  template is forced to stay clean). Strong by-construction guard.
- **R2 — `.expect` stub vs the empty-table rule.** The oracle (inspect fix) fails a registered module with 0
  rows; the stub ships **one** `=> (none)` row (matches the `return []` skeleton), so a scaffolded pair is
  oracle-consistent once registered. Registering the new handler in `HandlerRegistry` is a later authoring step
  (out of v1).
- **R3 — mutation (Editor MSI ≥ 80):** keep `Render` / `IsValidIdentifier` pure + isolated-exact-count tested;
  the CLI IO is temp-dir tested; `Program` excluded. The golden (T2) pins the template literally.
- **Reversible:** the template content/shape; the CLI arg grammar; the router split (vs folding into OracleCli).

## Phase 3 — Implement
- **Built (4 files, to the manifest):**
  - `Scaffolding/EventScaffold.cs` — the pure renderer: raw-string `.cs` + `.expect` templates with
    `{{Name}}`/`{{Namespace}}`, `Render(name, ns) → ScaffoldFiles` (guarded; pure string-replace + trailing
    `\n`), `IsValidIdentifier`. The emitted `.cs` is **immediately gate-clean** (sealed `IMapEvent`, `Run →
    IReadOnlyList<Outcome>` with a `// fill:` hole + `return []`); the `.expect` = a `#` header + `=> (none)`.
  - `Scaffolding/ScaffoldCli.cs` — the `scaffold event <Name> <out-dir> [--namespace <ns>]` verb: validate,
    refuse-to-overwrite, write both files, exit code (0/1/2). IO boundary (rendering stays pure).
  - `MonorpgCli.cs` — the verb router (`scaffold` → ScaffoldCli, `check-expectations` → OracleCli, else usage).
  - `Program.cs` — `Main → MonorpgCli.Run` (was `OracleCli.Run`); still `[ExcludeFromCodeCoverage]`.
- **Build:** solution `-warnaserror` → **0/0**. **Smoke test:** `dotnet run -- scaffold event SampleEvent
  /tmp/...` → "scaffolded SampleEvent.cs + SampleEvent.expect", exit 0; the emitted `.cs` is the exact
  gate-clean skeleton (raw-string indentation correct: 4-space members, 8-space body) and the `.expect` is the
  stub — verified by `cat`.
- **Deviation (in-scope, justified):** narrowed the existing `HandlerRegistry.Factories` (#10) from
  `IReadOnlyDictionary` to `Dictionary` — **CA1859** surfaced as a *flaky incremental-build error* once the
  Editor was re-analyzed (the field is used only via `TryGetValue`); the narrowing is correct per the rule and
  **de-flakes the gate**. Same Editor project.
- **Phase 4:** the test files (golden `.cs`+`.expect`; the rendered-`.cs`-compiles-MRM-clean verify via the
  analyzer harness; the `.expect`-parses check; the CLI scaffold/bad-input/router tests) + the FULL gate
  (Editor MSI ≥ 80).

## Inspect (Phase 3.5)
- **Lenses run:** 2 critics — (1) correctness (general-purpose; **built + scaffolded + compiled the emitted
  `.cs` in a Roslyn harness**), (2) design/purity/mutation-readiness (read-only).
- **Core promise verified:** Critic 1 proved the emitted happy-path skeleton **compiles (0 CS errors) + is
  MRM-clean (no MRM1001–1006) in the sim namespace + its `.expect` parses (Ok, 1 row)**. Findings:

  | # | Sev | Finding | Verdict | Resolution |
  |---|---|---|---|---|
  | F1 | HIGH | **Keyword names break the promise** — `IsValidIdentifier` checked only char-rules, so `scaffold event class` emitted `class class : IMapEvent` → 21 CS errors (same for `return`/`int`). | **REAL — FIXED** | `IsValidIdentifier` now rejects a reserved-keyword set. Verified: `scaffold event class` → exit 2, no file. `F-claude-codegen-validator-accepted-keywords-001` + `PR-claude-codegen-reject-keywords-001`. |
  | F2 | MEDIUM | **`--namespace` unvalidated** — `--namespace "not valid!"` emitted `namespace not valid!;` (won't compile). | **REAL — FIXED** | Added `IsValidNamespace` (each dot-segment a valid identifier); the CLI validates it + `Render` guards it. Verified: bad ns → exit 2. |
  | F3 | CRIT(critic) | "Raw-string **indentation bomb** — 8 leading spaces preserved → invalid C#." | **REJECTED — false positive** | Critic 2 *Python-simulated* without C# raw-string **dedent** semantics; Critic 1 **compiled the real emitted output → 0 CS errors**, and the smoke test showed correct indentation (0/4/8-space). |
  | F4 | (critic) | "`+ "\n"` → **double** trailing newline." | **REJECTED — false positive** | A raw string excludes the newline before the closing `"""`, so the template ends with `}` and `+ "\n"` gives exactly one; Critic 1 confirmed both files end in a single `0x0a`. |
  | — | — | HandlerRegistry `IReadOnlyDictionary`→`Dictionary` deviation; `Render` purity/determinism (byte-identical, Ordinal, no clock/RNG); no-partial-write + refuse-overwrite (exit 1); exit codes (0/1/2); the `MonorpgCli` router | **VERIFIED CLEAN** (both critics; Critic 1 by execution) | none |

- **Phase-4 Editor-MSI ≥ 80 kill-list (from Critic 2, + the fixes):** `Render`'s two `.Replace` + the `+ "\n"`
  (golden pins them); `IsValidIdentifier` branches (empty, `IsLetter||'_'`, the loop, `IsLetterOrDigit||'_'`,
  **the keyword reject**); `IsValidNamespace` (empty, the `.Split('.')` loop, segment-invalid); `ScaffoldCli` exit
  paths (`args.Count<4`, the `||` in the file-exists check — **needs BOTH single-file-exists tests**, the bad-name
  exit 2, the **bad-namespace exit 2**, refuse-overwrite exit 1, success exit 0), `ParseNamespace` loop bounds
  (`i < args.Count-1` — needs a `--namespace`-at-end test); `MonorpgCli` (scaffold/check-expectations/unknown→2).
  Plus regression fixtures: a keyword name + a bad namespace → exit 2.
- **Forge:** `failure-record F-claude-codegen-validator-accepted-keywords-001`; `prevention-rule-record
  PR-claude-codegen-reject-keywords-001`.

## Phase 4 — Validate
- **Tests added (14):** `EventScaffoldTests` (9): determinism, the structural golden (kills the `{{var}}`
  substitutions + the trailing newline), `IsValidIdentifier`/`IsValidNamespace` (incl. keywords), **the rendered
  `.cs` compiles + is MRM-clean** (T4 — the by-construction proof, via the analyzer verifier harness; zero CS
  errors asserted first), the `.expect` parses, **every reserved keyword rejected**, two `Render`-throws-with-message.
  `ScaffoldCliTests` (5): scaffold→pair + message; the exit-code matrix (2/1/0) with **message content** asserted;
  refuse-overwrite on either file (kills the `||`); `--namespace` honored + dangling; the router.
- **`dotnet test`: 223 passed, 0 failed** (+14).
- **`bin/gate.sh`: GREEN [full]** — 13 gates. Coverage **98.2%**; MSI **Engine 90.67% / Abstractions 82.22% /
  Analyzers 91.18% / Editor 83.97%** (all ≥ 80). Receipt written.
- **Mutation iteration (§0, fix-at-source, no floor lowered):** the first FULL gate hit **Editor MSI 61.58% <
  80** — root cause: the scaffolder's string-heavy code was under-killed (the 77-entry reserved-keyword set, only
  4 tested → ~73 string survivors; the CLI messages, only exit-codes asserted). Fixed at **TEST source** (no
  production / dependency change): a `[Fact]` iterating **all 77 keywords**, two `Render`-throws-with-message, and
  **message-content assertions** on the CLI tests → Editor **83.97%**. The remaining ~63 survivors are
  **pre-existing #10 oracle-code** mutants (ExpectationParser/Runner statement+string), already within the floor.
- **Pre-existing exclusions:** none. No new packages.

## Phase 5 — Complete
- Docs / forge / ticket / archive:
