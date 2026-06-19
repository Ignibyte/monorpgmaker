# WORK-p0-expect-oracle — Notes

Per-phase working notes for the paired `.spec.md`. The spec is the contract;
this is the record of what happened.

## Phase 1 — Plan
- **Request:** P0 next slice — the **`.expect` oracle** (D-0017 / agentic-substrate §7): an authored
  `<module>.expect` table (input state → expected `Outcome` rows) run against the real handler at a new
  **gate #13**; a mismatch is **MRM0006**. The anti-self-grading guarantee. User chose this at the P0 fork
  (over the scaffolder / the premature generator-validator). **NOT auto-approved** — review plan + design.
- **Intake source:** none. Slice from the LOCAL roadmap ("Remaining P0").
- **Classification / tier:** work pipeline, **type = feature**. A **new subsystem** (format + parser +
  runner + gate) — larger than the analyzer slices, but scoped to one shippable deliverable (the 2 tracer
  handlers + the runner + gate #13). **Split risk flagged:** if the format/runner/gate balloons, design may
  split (format+parser+runner as A; more handlers + the GUI grid later). Plan as one slice for now.
- **Forge recall (design/lessons surfaced):**
  - **§7** (governing): the 12 gates verify determinism/purity/layering/invariants — **none is correctness**.
    Each module ships a `<module>.expect` table of human-confirmable input→outcome rows in the shared outcome
    vocabulary, **authored in a step distinct from the implementing agent**; **gate #13 (MRM0006)** requires
    the module to deterministically reproduce every row, rejecting modules whose only tests are the agent's
    own output-mutations. (§10 lists the self-grading loop as a top risk this closes.)
  - **§2:** MRM0006 (expectation-mismatch) sits in the `MRM0xxx` validator band (with MRM0001-0005 — the
    premature DAG validator, explicitly OUT). "Each ships an analyzer code-fix" — does **not** apply to a
    semantic mismatch (no code-fix in v1).
  - **Builds on the chassis:** the pure handlers (`Run(IEventContext) → IReadOnlyList<Outcome>`) are runnable
    as functions; the **value-equal `Outcome` DU** (#8, `AD-claude-outcome-vocabulary-shape-001`) makes
    comparison trivial; analyzers #7/#9 already guarantee the handler is deterministic + pure.
  - **`bin/gate.sh` structure:** `run_gate "label" cmd...`; gates 1-10 always, 11-12 (coverage+mutation)
    FULL-only, then the receipt. A `gate:13` is a clean `run_gate` addition (band — always vs FULL — is a
    design call).
  - **Handler contracts (for the tables):** `LeverEvent` (cell, `DoorSwitch="door_open"`): `{}` →
    `[ShowMessage("You pull the lever. The door grinds open."), SetSwitch("door_open", true)]`; `{door_open}`
    → `[]`. `ChestEvent` (cell, `OpenedSwitch="chest_opened"`, `PotionCount="potions"`): `{}` →
    `[AddCounter("potions",1), SetSwitch("chest_opened",true), ShowMessage("You open the chest and take a potion.")]`;
    `{chest_opened}` → `[ShowMessage("The chest is empty.")]`. The `GridPoint` cell is irrelevant to `Run`.
  - **Forge hygiene:** the oathstar `WORK-entity-contracts-v1` (Rust/Datastar) bled into recall — discarded
    per CLAUDE.md (it confirms the "by-value contract assertions" intent, nothing more).
- **Ticket:** **d1725d2b-edc1-4c8f-bd1d-044bb8b4a7c8 (#10)** — created with explicit `project_id a9ee8162`;
  returned row confirms **monorpgmaker** (#1-#9 done, #10 open). Local doc:
  `docs/planning/tickets/open/TICKET-0010-p0-expect-oracle.md`.
- **AAR id:** 2f99c72b-8b2e-4ea6-bb50-c4c8de89c906
- **Plan review:** APPROVED. User chose **host the oracle in `MonoRpgMaker.Editor`** (gate #13 invokes it) —
  now a Locked Decision; OPEN-(c) narrows to the gate-call mechanism + band.
- **EARS requirements reviewed:** REQ-001..008 (format expresses input→outcomes; runner seeds+runs+
  value-equal compares; match→pass / mismatch→MRM0006 with module+row+expected/actual; Lever+Chest tables
  cover give-once; gate #13 fails on mismatch; MRM0xxx band; deterministic; FULL gate).

## Phase 2 — Design

### Resolved OPEN-Phase-2 decisions
- **(a) The `.expect` format** — a minimal line grammar (not JSON), one row per line:
  `<input-state> => <expected-outcomes>`
  - **input-state:** space-separated `key=value`; `value` of `true`/`false` seeds a **switch** (bool),
    an integer seeds a **counter** (int). Empty (nothing before `=>`) = no seeded state.
  - **expected-outcomes:** semicolon-separated, or the literal `(none)` for an empty list. Each is
    `SetSwitch(key, true|false)` · `AddCounter(key, <int>)` · `ShowMessage("text")` (double-quoted,
    spaces/punctuation allowed).
  - `#` line-comments and blank lines are ignored.
  - Worked **`LeverEvent.expect`:**
    ```
    # LeverEvent — pull the lever once to open the door (give-once)
    => ShowMessage("You pull the lever. The door grinds open."); SetSwitch(door_open, true)
    door_open=true => (none)
    ```
  - Worked **`ChestEvent.expect`:**
    ```
    # ChestEvent — first open grants a potion, then empty (give-once)
    => AddCounter(potions, 1); SetSwitch(chest_opened, true); ShowMessage("You open the chest and take a potion.")
    chest_opened=true => ShowMessage("The chest is empty.")
    ```
- **(b) Handler mapping + construction** — an explicit **name→factory registry** in the Editor
  (`{ "LeverEvent" => () => new LeverEvent(new GridPoint(0,0)), "ChestEvent" => () => new ChestEvent(...) }`);
  the `.expect` filename (sans extension) is the module name → registry lookup. The `GridPoint` cell is a
  dummy (`Run` ignores it). Unknown module → a typed error. No broad reflection.
- **(c) The gate #13 mechanism** — the **Editor becomes an `Exe`** (the `monorpg`-style maker CLI host): a
  thin `[ExcludeFromCodeCoverage]` `Program.Main` delegates to a **testable `OracleCli.Run(args, TextWriter)`**.
  `bin/gate.sh` adds **gate:13** = `dotnet run --no-build --project src/MonoRpgMaker.Editor -- check-expectations
  src/MonoRpgMaker.Engine/Sim/Tracer/Expectations` — placed **after** the FULL-only block so it runs in **both**
  modes (cheap correctness, always) and reads last in the summary. **`MonoRpgMaker.Editor.csproj` is added to
  the gate:12 mutation loop** so the oracle logic is MSI-gated (Program excluded; `MapEditor` gains tests).
- **(d) The `MRM0006` shape** — human-readable, no code-fix:
  `MRM0006: LeverEvent.expect:3 — expectation mismatch` then `given:` / `expected:` / `actual:` lines (the
  outcomes formatted as in the grammar). A malformed file is a distinct parse error (also fails the gate).
- **(e) `.expect` file location** — `src/MonoRpgMaker.Engine/Sim/Tracer/Expectations/{LeverEvent,ChestEvent}.expect`
  (co-located with the handlers they describe — "the module ships a `<module>.expect`"); plain data files (not
  compiled); the gate passes that directory to the CLI.

### Approach / architecture
- **A new `Expectations` area in `MonoRpgMaker.Editor`** (the maker tool — content validation is its job):
  - **`ExpectationParser`** — `string text → ParseResult` (rows, or a typed `ParseError` with the line# +
    reason). **Pure** (no IO); the CLI does the file read.
  - **`ExpectationRunner`** — `(rows, Func<IMapEvent>) → IReadOnlyList<Mismatch>`: per row, seed a `GameState`
    (`Set` each switch, `Add` each counter), build an `EventContext`, call `handler.Run`, and **value-equal**
    compare the returned `IReadOnlyList<Outcome>` to the parsed expected (`SequenceEqual` over the
    record-equal `Outcome` DU). **Pure.**
  - **`HandlerRegistry`** — name→factory; **`OracleCli`** — the `check-expectations <dir>` verb: enumerate
    `*.expect`, parse + run each, write matches/`MRM0006`s to the `TextWriter`, return `0`/non-zero. Testable
    (inject a `StringWriter`).
  - **`Program.Main`** — `[ExcludeFromCodeCoverage]` one-liner → `OracleCli.Run(args, Console.Out)`.
- The Editor is a **tooling/host layer** (consumes Engine; not a sim namespace) → the determinism analyzer
  doesn't gate it; the eval is deterministic anyway (pure handlers + value-equal compare). Typed results over
  exceptions on the parse path (§14).

### File manifest
| # | File | Change |
|---|---|---|
| 1 | `src/MonoRpgMaker.Editor/MonoRpgMaker.Editor.csproj` | **MODIFY** — add `<OutputType>Exe</OutputType>`. |
| 2 | `src/MonoRpgMaker.Editor/Program.cs` | **NEW** — `[ExcludeFromCodeCoverage]` `Main` → `OracleCli.Run`. |
| 3 | `src/MonoRpgMaker.Editor/Expectations/OracleCli.cs` | **NEW** — the `check-expectations <dir>` verb; enumerate/parse/run/report; exit code. |
| 4 | `src/MonoRpgMaker.Editor/Expectations/ExpectationParser.cs` | **NEW** — text → rows / typed `ParseError`. Pure. |
| 5 | `src/MonoRpgMaker.Editor/Expectations/ExpectationRunner.cs` | **NEW** — seed→Run→value-equal compare → mismatches. Pure. |
| 6 | `src/MonoRpgMaker.Editor/Expectations/ExpectationModels.cs` | **NEW** — `ExpectationRow`, `ParseError`, `Mismatch`/`Mrm0006`, the outcome formatter. |
| 7 | `src/MonoRpgMaker.Editor/Expectations/HandlerRegistry.cs` | **NEW** — name → `Func<IMapEvent>` (Lever/Chest). |
| 8 | `src/MonoRpgMaker.Engine/Sim/Tracer/Expectations/LeverEvent.expect` | **NEW** — the authored table. |
| 9 | `src/MonoRpgMaker.Engine/Sim/Tracer/Expectations/ChestEvent.expect` | **NEW** — the authored table. |
| 10 | `bin/gate.sh` | **MODIFY** — add gate:13 (always); add `MonoRpgMaker.Editor.csproj` to the gate:12 mutation loop. |
| 11 | `tests/MonoRpgMaker.Engine.Tests/MonoRpgMaker.Engine.Tests.csproj` | **MODIFY** — `ProjectReference` → the Editor. |
| 12 | `tests/MonoRpgMaker.Engine.Tests/ExpectationParserTests.cs` | **NEW** — parser units. |
| 13 | `tests/MonoRpgMaker.Engine.Tests/ExpectationRunnerTests.cs` | **NEW** — match + each mismatch kind + MRM0006 shape. |
| 14 | `tests/MonoRpgMaker.Engine.Tests/OracleCliTests.cs` | **NEW** — verb + exit code (temp dirs) + registry + the real tables reproduce. |
| 15 | `tests/MonoRpgMaker.Engine.Tests/MapEditorTests.cs` | **NEW** — Paint/Clear (Editor now mutation-gated). |

### Regression Test Plan
| # | Test | Proves |
|---|---|---|
| T1 | Parser: a 2-row file → 2 rows with exact input (switch/counter) + expected outcomes. | REQ-001 |
| T2 | Parser: `#` comments + blank lines ignored. | REQ-001 |
| T3 | Parser: `key=true/false` → switch, `key=<int>` → counter (disambiguation). | REQ-001 |
| T4 | Parser: `(none)` → empty expected list. | REQ-001 |
| T5 | Parser: each outcome kind round-trips its payload (incl. quoted text with spaces/`.`). | REQ-001 |
| T6 | Parser: a malformed line → a typed `ParseError` naming the line (no exception). | REQ-001 (§14) |
| T7 | Runner: handler output equals expected → no mismatch. | REQ-002 |
| T8 | Runner: COUNT mismatch → MRM0006. | REQ-002/003 |
| T9 | Runner: KIND mismatch (ShowMessage vs SetSwitch) → MRM0006. | REQ-003 |
| T10 | Runner: PAYLOAD mismatch (wrong text/key/value) → MRM0006. | REQ-003 |
| T11 | Runner: ORDER mismatch (right set, wrong order) → MRM0006. | REQ-003 |
| T12 | Runner: the MRM0006 message names module + row + expected-vs-actual. | REQ-003 |
| T13 | Registry: Lever/Chest resolve; unknown name → typed error. | REQ-002 |
| T14 | `OracleCli`: valid temp dir → exit 0, no MRM0006; seeded-mismatch file → non-zero + MRM0006 printed. | REQ-005 |
| T15 | The **real** `LeverEvent.expect` + `ChestEvent.expect` reproduce against the real handlers. | REQ-004 |
| T16 | `MapEditor`: Paint in-bounds (true, tile set) / off-map (false, unchanged); Clear → all `Empty`. | REQ-008 (Editor MSI) |
| T17 (gate) | `bin/gate.sh` gate:13 green on match; a deliberately-seeded mismatch turns the gate red. | REQ-005 (end-to-end) |

**Uncoverable:** `Program.Main` — the `[ExcludeFromCodeCoverage]` one-liner delegating to the tested
`OracleCli.Run`; it's the composition root. Everything else is pure + unit-tested.

### Risks / decisions
- **R1 (scope) — the Editor becomes an `Exe` + joins the mutation gate.** `MapEditor` (untested today) gains
  tests; `Program` is `[ExcludeFromCodeCoverage]` and the CLI logic lives in the tested `OracleCli`. The Editor
  must hit MSI ≥ 80 like the others. This is the honest cost of "host the oracle in the Editor" — **flagged for
  the review** (the slice is medium-large; it could split into oracle-library + CLI/gate, but they're tightly
  coupled).
- **R2 — gate:13 invocation:** `dotnet run --no-build` reuses gate:2's Debug build; fallback `dotnet exec
  bin/Debug/.../MonoRpgMaker.Editor.dll …` if the runtimeconfig path bites. The `.expect` directory is a
  gate-passed argument.
- **R3 — the parser returns typed results** (`ParseError`), never throws on malformed authored input (§14); a
  parse error fails the gate with a clear message (distinct from MRM0006).
- **R4 — the real `.expect` files live in Engine's tree but are read by the Editor CLI at a gate-passed path**;
  T15 loads them via a repo-relative path (or `<Content CopyToOutputDirectory>` in the test csproj).
- **R5 — value-equal compare** leans entirely on the `Outcome` DU's record equality (#8) — solid; `SequenceEqual`
  gives count + kind + payload + order in one.
- **Reversible-but-load-bearing:** the line-grammar shape; the `.expect` location; CLI-vs-filtered-test gate
  mechanism (chose CLI for a faithful, standalone gate #13).

## Phase 3 — Implement
- **Built (11 production/data/wiring items, to the manifest):**
  - **Editor → CLI host:** `MonoRpgMaker.Editor.csproj` +`<OutputType>Exe</OutputType>`; `Program.cs`
    (`[ExcludeFromCodeCoverage]` `Main` → `OracleCli.Run(args, Console.Out)`).
  - **`Expectations/` (the oracle, pure + testable):** `ExpectationModels.cs` (`ExpectationRow`,
    `ParseResult`/`ParseError`, `Mismatch`, `OutcomeFormat` — format outcomes + seed-state),
    `ExpectationParser.cs` (text→rows / typed `ParseError`, the line grammar, quote-aware `;`-split),
    `ExpectationRunner.cs` (seed `GameState` → `EventContext` → `Run` → `SequenceEqual` over the value-equal
    `Outcome` DU → mismatches), `HandlerRegistry.cs` (name→factory: Lever/Chest at `GridPoint.Zero`),
    `OracleCli.cs` (the `check-expectations <dir>` verb → MRM0006/OK + exit code).
  - **Data:** `src/MonoRpgMaker.Engine/Sim/Tracer/Expectations/{LeverEvent,ChestEvent}.expect`.
  - **Gate:** `bin/gate.sh` — `gate:13 .expect oracle (MRM0006)` (`oracle_g`, placed after the FULL-only block
    so it runs in **both** modes); `MonoRpgMaker.Editor.csproj` added to the gate:12 mutation loop.
  - **Tests wiring:** the test csproj now `ProjectReference`s the Editor (Phase 4 unit-tests the oracle + `MapEditor`).
- **Build:** solution `-warnaserror` → **0/0**. Fixes during implement (4, all CA/compile, not design): a missing
  `using MonoRpgMaker.Abstractions;` in `OracleCli`; **CA1859** (`SplitOutcomes` → concrete `List<string>`);
  **CA1865** (`EndsWith(')')` char overload); removed an unused `HandlerRegistry.Modules` (YAGNI + would be an
  untested mutation survivor). Nullable/analyzers are inherited from `Directory.Build.props` (only `OutputType` added).
- **Smoke test (early REQ-004 signal):** `dotnet run --project src/MonoRpgMaker.Editor -- check-expectations
  src/MonoRpgMaker.Engine/Sim/Tracer/Expectations` → **".expect oracle: 4 rows across 2 modules reproduced — OK"**,
  exit 0. The real Lever/Chest handlers reproduce their authored tables end-to-end (parser + runner + CLI work).
  `shellcheck -S info bin/gate.sh` clean (gate:7).
- **Deviations from design:** none material. (`--verbosity quiet` added to the gate:13 `dotnet run` to keep its
  output clean.)
- **For Phase 4:** author the test files (parser/runner/CLI/`MapEditor`) + run the FULL gate — the **Editor is now
  in coverage (gate:11) + mutation (gate:12)**, so the oracle logic + `MapEditor` must hit MSI ≥ 80 (isolated
  exact-count tests); `Program` is `[ExcludeFromCodeCoverage]`; the `OutcomeFormat` `_ =>` default arm is an
  unreachable equivalent mutant (closed `Outcome` DU).

## Inspect (Phase 3.5)
- **Lenses run:** 2 critics — (1) **oracle correctness** (general-purpose; **fuzzed the CLI** on throwaway
  temp `.expect` files for false-pass/false-fail), (2) design / purity / gate / mutation-readiness (read-only).
- **Headline: NO FALSE-PASS.** Critic 1 verified by execution that the oracle catches **every** mismatch
  kind (count / kind / payload / order → MRM0006, exit 1) and fails on malformed / unknown-module / missing-dir;
  `SequenceEqual` over the value-equal `Outcome` DU is correct; seeding (`Set`/`Add`) is order-independent. The
  oracle cannot be fooled into a green by a wrong table — the anti-self-grading guarantee holds. Findings:

  | # | Sev | Finding | Verdict | Resolution |
  |---|---|---|---|---|
  | F1 | MEDIUM | **Parser not total** — `ShowMessage(")` (a lone quote satisfies both prefix `ShowMessage("` and suffix `")`) → `Substring(start, -1)` threw `ArgumentOutOfRangeException` (exit 134), violating the parser's own §14 "total — never throws" XML-doc. Not a false-pass (exits non-zero), but breaks the typed-result contract + crashes when the parser is unit-tested as a library. | **REAL — FIXED** | `ExpectationParser.cs`: guard `length >= 0` before the `Substring` → falls through to the existing typed "malformed outcome" `ParseError`. Verified: `ShowMessage(")` now → clean parse error, exit 1. Captured `F-claude-total-parser-threw-on-affix-overlap-001` + `PR-claude-total-parser-substring-overlap-001`. |
  | F2 | LOW | **Empty-rows silent pass** — a registered module whose `.expect` is comments-only/blank parsed to 0 rows and reported "0 rows … OK" (exit 0), a coverage blind spot vs "every handler is pinned by a table". | **REAL — FIXED** | `OracleCli.cs`: a registered module with 0 rows now emits `MRM0006: <module>.expect — no expectation rows …` + non-zero. Verified. |
  | — | — | **Design/purity/gate** — Editor→Engine layering legal (ArchitectureTests don't forbid it; not a sim namespace so the determinism analyzer doesn't gate it); §14 typed-results + nullable + immutable + XML-doc clean; gate:13 wiring correct (`--no-build` reuses gate:2's build; runs both `--fast`+FULL; path repo-root-relative; exit code propagates); right-sized, no duplication. | **VERIFIED CLEAN** (Critic 2) | none |

- **Phase-4 mutation kill-list (Critic 2 — the Editor is now in gate:12; target MSI ≥ 80):**
  - **Parser:** the `line.Length==0` / `line[0]=='#'` skips (comment/blank tests); `inputPart.Length>0` + `eq<=0` (empty-state + `=value` malformed); `outputPart != "(none)"` (T4); each outcome kind + payload round-trip (T5); the quote-aware `;` split; **+ the two fix regressions: `ShowMessage(")` → parse error, and a 0-row table → MRM0006**.
  - **Runner:** `!actual.SequenceEqual(...)` — each mismatch kind (count/kind/payload/order) must be caught; an exact-match row → no mismatch.
  - **CLI:** the `failures==0` branch + `? 0 : 1` exit code (mismatch→1, all-match→0, missing-dir/usage→2); `Count(n, noun)` pluralization (`n==1` → 0/1/2 cases); unknown-module + parse-error paths increment failures.
  - **Models:** `OutcomeFormat.Format` empty→`(none)` + the `i>0` separator; `FormatInput` empty→`(initial)`. The `_ =>` default arm is an **unreachable equivalent** (closed `Outcome` DU) — like the `OutcomeApplier` default.
  - **MapEditor** (pulled into mutation by the Editor join): `Paint` in-bounds→true+tile-set / off-map→false+unchanged; `Clear`→all `Empty`. `Program.Main` is `[ExcludeFromCodeCoverage]` (Stryker skips it).
  - **REQ-004 end-to-end:** the real `LeverEvent.expect` + `ChestEvent.expect` reproduce (gate:13 + a unit test).
- **Forge capture:** `failure-record F-claude-total-parser-threw-on-affix-overlap-001`; `prevention-rule-record
  PR-claude-total-parser-substring-overlap-001`. The empty-rows fix is a hardening, not a bug (no failure-record).

## Phase 4 — Validate
- **Tests added (37; 209 total, was 172) — to the inspect kill-list, isolated exact-count:**
  - `ExpectationParserTests` (6): 2-row exact parse; comments/blanks ignored + line#; switch/counter
    disambiguation; `(none)`; each outcome kind + payload round-trip (comma-in-text); an 8-case `[Theory]` of
    malformed inputs → typed `ParseError` (never throws) — **incl. the `ShowMessage(")` regression**.
  - `ExpectationRunnerTests` + `HandlerRegistryTests` + `OutcomeFormatTests`: exact-match → no mismatch; the
    four mismatch kinds (count/kind/payload/order) → `Mismatch`; **switch seeding** (give-once) + **counter
    seeding** (a test-local `CounterEcho` handler reading `GetCounter` kills the Add-seeding mutant); registry
    known/unknown; `OutcomeFormat` `(none)`/`(initial)`/each kind/the `;`-separator.
  - `OracleCliTests` (9): all-reproduce → exit 0 + "2 rows across 1 module" (kills pluralization); mismatch →
    exit 1 + `MRM0006 module:line` + expected/actual; parse-error/unknown-module/**empty-table (inspect fix)**
    → exit 1; missing-dir/no-verb/no-dir → exit 2; **REQ-004** — the **real** `LeverEvent.expect` +
    `ChestEvent.expect` reproduce (located via the `MonoRpgMaker.slnx` repo-root marker).
  - `MapEditorTests` (3): Paint in-bounds (true + tile set) / off-map (false + unchanged); Clear → all `Empty`.
- **`dotnet test MonoRpgMaker.slnx`: Passed — 209 passed, 0 failed** (+37).
- **`bin/gate.sh`: GREEN [full]** — **13 gates** (the new **gate:13 .expect oracle (MRM0006)** PASS). Coverage
  **98.0%**; mutation MSI **Engine 90.67% / Abstractions 82.22% / Analyzers 91.18% / Editor 81.08%** (all ≥ 80).
  Receipt written.
- **Editor MSI 81.08% (new project, tight but green):** every KILLABLE oracle + `MapEditor` mutant is dead via
  the kill-list. The margin reflects the **OutcomeFormat `_ =>` default arm** — an unreachable equivalent
  (closed `Outcome` DU, like `OutcomeApplier`'s default) — plus a couple of boundary equivalents; `Program.cs`
  is `[ExcludeFromCodeCoverage]`. Above the floor; no floor lowered, nothing skipped (§0).
- **Pre-existing exclusions:** none. `packages.lock.json` updated (the test→Editor `ProjectReference`); no new
  packages.
- **Process note:** the FULL gate was launched with `nohup … &` (detached), so the harness "completed"
  notification fired for the wrapper, not `gate.sh`; completion was confirmed via `pgrep` + the `GATE GREEN
  [full]` line + all four mutation scores. (Lesson: `run_in_background` the gate directly, don't `nohup &` it.)

## Phase 5 — Complete
- Docs updated:
- Forge capture (aar/failures/rules/decisions):
- Ticket closed:
- Archived:
