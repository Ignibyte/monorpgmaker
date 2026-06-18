#!/usr/bin/env bash
# =============================================================================
# bin/gate.sh — monorpgmaker canonical quality gate (C#/.NET)
# =============================================================================
#
# The single source of truth for "is this change shippable?". Invoked by
# /commit (the delivery gate) and the enforce-commit-gate.sh hook. Strict by
# charter (CONSTITUTION §0): no baselines, no suppressions, source-fix only.
#
# Runs ALL gates, reports each, exits non-zero if any failed. Every gate's
# verdict is the tool's EXIT CODE — never a grep of output. Bash 3.2 compatible
# (macOS /bin/bash) — no mapfile/declare -A.
#
# The .NET analogue of the Oathstar Rust gate:
#   fmt          dotnet format --verify-no-changes
#   build        dotnet build -warnaserror  (Roslyn + BannedApiAnalyzers, warnings = errors)
#   test         dotnet test                (incl. NetArchTest layering guardrails)
#   vuln         dotnet list package --vulnerable
#   licenses     nuget-license              (SPDX allowlist — the cargo-deny licenses analogue)
#   secrets      gitleaks (history + working tree)
#   sh-lint      shellcheck (hooks + bin)
#   no-suppr     grep meta-gate (#pragma warning disable / [SuppressMessage])
#   source-bans  grep meta-gate backstop (Process.Start / Environment.Exit / unsafe)
#   doc-todos    grep meta-gate
#   [FULL] coverage  dotnet test --collect "XPlat Code Coverage"  (line floor)
#   [FULL] mutation  dotnet stryker (local tool)                   (MSI floor)
#
# Reproducibility: global.json pins the SDK, .config/dotnet-tools.json pins the
# CLI tools (Stryker, nuget-license), and packages.lock.json pins the dependency
# graph — the gate restores with --locked-mode, so drift fails here instead of
# silently resolving something new.
#
# Modes:
#   bin/gate.sh           FULL — every gate (the /commit sweep).
#   bin/gate.sh --fast    FAST — skips coverage + mutation (quick local loop).
#   GATE_FAST=1 also selects FAST. /commit always runs FULL.
#
# Floors (env may RAISE to ratchet; the §0 minimums are a HARD floor env can
# never lower):  NET_COV_MIN=80  MUT_MSI_MIN=80
# =============================================================================

set -uo pipefail
cd "$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)" || exit 2

# Pinned CLI tools (dotnet-stryker, nuget-license) come from the repo tool
# manifest (.config/dotnet-tools.json) via `dotnet tool restore` below. Keep the
# global tools dir on PATH too, as a harmless fallback.
export PATH="$HOME/.dotnet/tools:$PATH"

# Shared helpers — gate_state_hash() for the commit-gate receipt.
# shellcheck source=.claude/hooks/lib-hook-helpers.sh
. ./.claude/hooks/lib-hook-helpers.sh 2>/dev/null \
  || { echo "FATAL: cannot load .claude/hooks/lib-hook-helpers.sh" >&2; exit 2; }

# §0 minimums — baked in. env may RAISE a floor; a lower value is clamped back
# up, so a green can never be bought by lowering the bar.
NET_COV_FLOOR=80; MUT_MSI_FLOOR=80
NET_COV_MIN="${NET_COV_MIN:-$NET_COV_FLOOR}"
MUT_MSI_MIN="${MUT_MSI_MIN:-$MUT_MSI_FLOOR}"
if awk -v c="$NET_COV_MIN" -v f="$NET_COV_FLOOR" 'BEGIN{exit !(c+0 < f+0)}'; then echo "note: NET_COV_MIN below the §0 minimum $NET_COV_FLOOR — clamped." >&2; NET_COV_MIN=$NET_COV_FLOOR; fi
if awk -v c="$MUT_MSI_MIN" -v f="$MUT_MSI_FLOOR" 'BEGIN{exit !(c+0 < f+0)}'; then echo "note: MUT_MSI_MIN below the §0 minimum $MUT_MSI_FLOOR — clamped." >&2; MUT_MSI_MIN=$MUT_MSI_FLOOR; fi

SLN="MonoRpgMaker.slnx"
MODE="full"
case "${1:-}" in --fast|fast) MODE="fast" ;; esac
[ "${GATE_FAST:-0}" = "1" ] && MODE="fast"

PASS=0; FAIL=0
RESULTS=()

run_gate() {
  local label="$1"; shift
  printf '\n\033[1m▶ %s\033[0m\n    %s\n' "$label" "$*"
  if "$@"; then RESULTS+=("PASS  $label"); PASS=$((PASS + 1))
  else RESULTS+=("FAIL  $label"); FAIL=$((FAIL + 1)); fi
}

need() { command -v "$1" >/dev/null 2>&1 || { echo "MISSING TOOL: $1 — $2" >&2; return 1; }; }

# Restore once up front so the format/build/test gates can run --no-restore.
# --locked-mode verifies every packages.lock.json is current (reproducibility);
# on drift the build gate goes red and the guidance below says how to fix it.
echo "▶ restore (locked)"
if ! dotnet restore "$SLN" --locked-mode >/tmp/mrm-restore.log 2>&1; then
  echo "  locked restore FAILED — a packages.lock.json is missing or out of date."
  echo "  Fix: run 'dotnet restore' to regenerate the lock files, then commit them."
  tail -8 /tmp/mrm-restore.log 2>/dev/null || true
fi
# Pinned CLI tools used by the license + mutation gates.
echo "▶ tool restore"; dotnet tool restore >/dev/null 2>&1 \
  || echo "  (tool restore had issues — license/mutation gates will surface them)"

# ── 1. format ─────────────────────────────────────────────────────────────────
run_gate "gate:1  dotnet format" dotnet format "$SLN" --verify-no-changes --no-restore

# ── 2. build (analyzers + warnings as errors) ─────────────────────────────────
run_gate "gate:2  dotnet build (-warnaserror)" dotnet build "$SLN" -warnaserror --no-restore --nologo

# ── 3. tests ──────────────────────────────────────────────────────────────────
run_gate "gate:3  dotnet test" dotnet test "$SLN" --no-build --nologo

# ── 4. vulnerable dependencies ────────────────────────────────────────────────
vuln_g() {
  local out
  out=$(dotnet list "$SLN" package --vulnerable --include-transitive 2>&1) || { echo "$out"; return 1; }
  if echo "$out" | grep -qiE 'has the following vulnerable|>[[:space:]]*[A-Za-z].*(Critical|High|Moderate|Low)'; then
    echo "$out" | grep -iE 'vulnerable|Critical|High|Moderate|Low'
    echo "vulnerable packages found (CONSTITUTION §0) — upgrade them."
    return 1
  fi
  echo "no vulnerable packages."
}
run_gate "gate:4  vulnerable packages" vuln_g

# ── 5. dependency licenses (SPDX allowlist) ──────────────────────────────────
# Supply-chain hygiene (the cargo-deny licenses analogue). `--error-only -o Json`
# prints ONLY allowlist violations, so a clean tree yields "[]". nuget-license
# exits 0 whether clean OR violating, so a non-zero rc means the tool itself
# failed to run (e.g. tools not restored) — that fails the gate rather than
# false-greening on empty output.
license_g() {
  local out err rc tmp_err viol
  tmp_err=$(mktemp)
  out=$(dotnet nuget-license -i "$SLN" -t \
          --allowed-license-types .config/nuget-license-allowed.json \
          --licenseurl-to-license-mappings .config/nuget-license-url-mappings.json \
          --override-package-information .config/nuget-license-overrides.json \
          --error-only -o Json 2>"$tmp_err"); rc=$?
  err=$(cat "$tmp_err" 2>/dev/null); rm -f "$tmp_err"
  if [ "$rc" -ne 0 ]; then
    echo "$err" | tail -6
    echo "nuget-license failed to run — did 'dotnet tool restore' succeed?"
    return 1
  fi
  viol=$(printf '%s' "$out" | tr -d '[:space:]')
  if [ -z "$viol" ] || [ "$viol" = "[]" ]; then
    echo "all dependency licenses within the allowlist (.config/nuget-license-allowed.json)."
    return 0
  fi
  echo "dependencies whose license is NOT on the allowlist (CONSTITUTION §0 supply-chain):"
  printf '%s\n' "$out" | grep -oE '"PackageId":"[^"]*"' || printf '%s\n' "$out"
  echo "→ vet each; if acceptable, add its SPDX id to .config/nuget-license-allowed.json"
  return 1
}
run_gate "gate:5  dependency licenses" license_g

# ── 6. secrets — committed history AND the working tree ──────────────────────
secrets_g() {
  need gitleaks "brew install gitleaks" || return 1
  local cfg=".gitleaks.toml" d args=()
  [ -f "$cfg" ] && args=(-c "$cfg")
  # ${args[@]+...} keeps an empty array safe under `set -u` (bash 3.2).
  gitleaks detect --no-banner -s . ${args[@]+"${args[@]}"} || return 1
  for d in src tests bin .claude; do
    [ -d "$d" ] || continue
    gitleaks dir "$d" --no-banner ${args[@]+"${args[@]}"} || return 1
  done
  return 0
}
run_gate "gate:6  gitleaks (secrets)" secrets_g

# ── 7. shell scripts (the hooks + bin) ───────────────────────────────────────
shellcheck_g() { need shellcheck "brew install shellcheck" || return 1; shellcheck -S info -e SC1091 .claude/hooks/*.sh bin/*.sh; }
run_gate "gate:7  shellcheck" shellcheck_g

# ── 8. no inline suppressions (CONSTITUTION §0/§15) ──────────────────────────
# A `#pragma warning disable` / `[SuppressMessage]` in game source must carry a
# real `// <text>` justification on the line. A blanket disable (no warning code)
# is banned outright.
no_suppr_g() {
  local unjust blanket
  unjust=$(grep -rnE '#pragma warning disable|\[SuppressMessage' src --include='*.cs' 2>/dev/null \
           | grep -vE '//[[:space:]]*[^[:space:]]' || true)
  blanket=$(grep -rnE '#pragma warning disable[[:space:]]*(//.*)?$' src --include='*.cs' 2>/dev/null || true)
  if [ -n "$unjust$blanket" ]; then
    echo "unjustified / blanket suppressions in game source (CONSTITUTION §0/§15):"
    [ -n "$unjust" ]  && { echo "— missing a // justification:"; echo "$unjust"; }
    [ -n "$blanket" ] && { echo "— blanket disable (no warning code — banned):"; echo "$blanket"; }
    return 1
  fi
  return 0
}
run_gate "gate:8  no-suppressions" no_suppr_g

# ── 9. source bans (SAST) ─────────────────────────────────────────────────────
# Backstop for BannedApiAnalyzers (the primary, compile-time enforcement via
# BannedSymbols.txt): process spawning, hard exits, and `unsafe` without a
# // SAFETY: justification. Kept because it also catches `unsafe`, which the
# analyzer does not, and survives someone dropping the analyzer package.
source_bans_g() {
  local prims unsafes
  prims=$(grep -rnE 'Process\.Start|Environment\.(Exit|FailFast)' src --include='*.cs' 2>/dev/null || true)
  unsafes=$(grep -rnE '(^|[^_[:alnum:]])unsafe[[:space:]]' src --include='*.cs' 2>/dev/null | grep -v 'SAFETY:' || true)
  if [ -n "$prims$unsafes" ]; then
    echo "banned source primitives (CONSTITUTION §14):"
    [ -n "$prims" ]   && { echo "— process/exit primitives:"; echo "$prims"; }
    [ -n "$unsafes" ] && { echo "— unsafe without a // SAFETY: justification:"; echo "$unsafes"; }
    return 1
  fi
  return 0
}
run_gate "gate:9  source-bans (SAST)" source_bans_g

# ── 10. doc TODOs ─────────────────────────────────────────────────────────────
doc_todos_g() {
  local hits
  hits=$(grep -rlE 'TODO|FIXME|XXX' --include='*.md' docs/ 2>/dev/null | grep -v 'docs/planning/' || true)
  [ -z "$hits" ] || { echo "TODO/FIXME/XXX markers in committed docs:"; echo "$hits"; return 1; }
  return 0
}
run_gate "gate:10 doc-todos" doc_todos_g

# ── FULL-only: coverage + mutation ───────────────────────────────────────────
if [ "$MODE" = "fast" ]; then
  RESULTS+=("SKIP  gate:11-12 coverage+mutation (--fast) — run the FULL gate before /commit")
else
  # 11. line coverage floor (coverlet via the test collector)
  net_cov() {
    local covdir="coverage" f pct
    rm -rf "$covdir"
    dotnet test "$SLN" --collect:"XPlat Code Coverage" --results-directory "$covdir" --nologo >/dev/null 2>&1 \
      || { echo "coverage test run failed"; return 1; }
    f=$(find "$covdir" -name 'coverage.cobertura.xml' 2>/dev/null | head -1)
    [ -n "$f" ] || { echo "no cobertura report produced"; return 1; }
    pct=$(grep -oE 'line-rate="[0-9.]+"' "$f" | head -1 | grep -oE '[0-9.]+')
    [ -n "$pct" ] || { echo "could not parse coverage line-rate"; return 1; }
    pct=$(awk -v r="$pct" 'BEGIN{printf "%.1f", r*100}')
    awk -v p="$pct" -v min="$NET_COV_MIN" 'BEGIN{exit !(p+0 >= min+0)}' \
      || { echo "line coverage ${pct}% < floor ${NET_COV_MIN}% — write tests"; return 1; }
    echo "line coverage ${pct}% >= ${NET_COV_MIN}%"
  }
  run_gate "gate:11 coverage (>= ${NET_COV_MIN}% lines)" net_cov

  # 12. mutation testing (Stryker.NET MSI floor) — Stryker is the pinned local
  # tool from .config/dotnet-tools.json (restored above), invoked as `dotnet stryker`.
  mutation_g() {
    local out msi testdir="tests/MonoRpgMaker.Engine.Tests" proj
    # Run from the test project, mutating each referenced production project in turn
    # (Stryker's canonical invocation; avoids .slnx solution-parsing). break=0 default,
    # so Stryker never fails the run on a low score — the floor below is the gate.
    for proj in MonoRpgMaker.Engine.csproj MonoRpgMaker.Abstractions.csproj MonoRpgMaker.Analyzers.csproj; do
      out=$( (cd "$testdir" && dotnet stryker --project "$proj" --reporter cleartext) 2>&1 ) \
        || { echo "$out" | tail -20; echo "stryker did not complete for $proj"; return 1; }
      msi=$(echo "$out" | grep -oiE 'mutation score[^0-9]*[0-9]+\.?[0-9]*' | grep -oE '[0-9]+\.?[0-9]*' | tail -1)
      [ -n "$msi" ] || { echo "could not parse mutation score for $proj"; return 1; }
      echo "${proj}: mutation score ${msi}% (floor ${MUT_MSI_MIN}%)"
      awk -v m="$msi" -v min="$MUT_MSI_MIN" 'BEGIN{exit !(m+0 >= min+0)}' \
        || { echo "MSI ${msi}% < floor ${MUT_MSI_MIN}% for $proj — kill more mutants (write tests)"; return 1; }
    done
  }
  run_gate "gate:12 mutation (MSI >= ${MUT_MSI_MIN}%)" mutation_g
fi

# ── Summary ──────────────────────────────────────────────────────────────────
printf '\n\033[1m══ gate summary (%s) ══\033[0m\n' "$MODE"
for r in "${RESULTS[@]}"; do
  case "$r" in
    PASS*) printf '  \033[32m%s\033[0m\n' "$r" ;;
    FAIL*) printf '  \033[31m%s\033[0m\n' "$r" ;;
    *)     printf '  %s\n' "$r" ;;
  esac
done
printf '  %d passed, %d failed\n' "$PASS" "$FAIL"

[ "$FAIL" -eq 0 ] || { echo "GATE RED — fix at source (CONSTITUTION §0: no baselines, no suppressions)."; exit 1; }
echo "GATE GREEN [$MODE]"

# Receipt — bind this FULL green to the exact worktree it ran on.
if [ "$MODE" = "full" ]; then
  GITDIR=$(git rev-parse --git-dir 2>/dev/null || true)
  if [ -n "$GITDIR" ]; then gate_state_hash > "$GITDIR/monorpgmaker-gate-receipt" 2>/dev/null || true; fi
fi
