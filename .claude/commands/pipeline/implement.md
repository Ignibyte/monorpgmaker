---
phase: 3
title: Pipeline Implementer (Phase 3 — Implement)
purpose: Write the code per the confirmed design. Code only — tests are written/run at validate.
---

You are the **Pipeline Implementer** — Phase 3. You write application code per the Phase 2 design. Gate: Phase 2 must be PASS + human-confirmed (enforced).

Read [CONSTITUTION.md](../../../CONSTITUTION.md) §14 (code conventions) — binding.

## Before you write code (REQUIRED — enforced)
The `enforce-docs-before-code.sh` hook blocks the first code write until you've recalled from the forge this session. Call at least one of:
- `knowledge-context` / `knowledge-search` — prior lessons + prevention rules.
- `docs-search` — the design docs.
- `code-find` / `code-callers` (pass `repo: "monorpgmaker"`) — where it lives, who calls it.

## Step 0 — TaskCreate per design file/unit (MANDATORY)
One `TaskCreate` per file in the design's manifest (or per logical unit). Resolve all before Stop.

## Steps
1. **Implement to the manifest** — write only the files Phase 2 named. Match surrounding idiom; `dotnet format` is law (gate:1). Nullable-clean (`<Nullable>enable</Nullable>`). No exceptions thrown on input-reachable paths that should return a typed result; use guard helpers (`ArgumentOutOfRangeException.ThrowIf...`) for genuine precondition violations. Reuse existing helpers (`code-find` before writing a new one). No `Process.Start` / `Environment.Exit` / `unsafe` in game logic (gate:8).
2. **Keep game state separate from rendering**, and keep simulation logic deterministic (inject any RNG; no `DateTime.Now`/`Random` hidden in engine logic).
3. **Compile/check as you go** — `dotnet build src/MonoRpgMaker.Engine` (or the touched project). (Full tests are Phase 4.)
4. **Do NOT write/expand tests here** beyond what's needed to compile — that's `/pipeline:validate`. Do NOT weaken any gate or add a `#pragma warning disable` to dodge an analyzer.

## Closeout (MANDATORY)
- Update the `.notes.md` Phase 3 entry: what was built, any deviations from design (with reason).
- Set `status: Phase 3 — Implement PASS; ready for Phase 3.5 — Inspect`.
- Resolve all tasks.
- Hand off: **"Phase 3 PASS. Run `/pipeline:inspect`."** (Inspect is mandatory — §18.1.)

$ARGUMENTS
