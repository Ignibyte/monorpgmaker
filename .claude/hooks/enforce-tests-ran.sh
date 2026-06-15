#!/usr/bin/env bash
# enforce-tests-ran.sh — tests must actually RUN at validate (Stop hook).
# CONSTITUTION §7/§15: writing a test file is not testing. At /pipeline:validate
# the transcript must show a real `dotnet test` invocation. Exit 0 = allow, 2 = block.
set -euo pipefail
INPUT=$(cat)
command -v jq &>/dev/null || exit 0
source "$(dirname "$0")/lib-hook-helpers.sh"
TRANSCRIPT_PATH=$(echo "$INPUT" | jq -r '.transcript_path // empty')
[ -n "$TRANSCRIPT_PATH" ] && [ -f "$TRANSCRIPT_PATH" ] || exit 0
is_pipeline_session "$TRANSCRIPT_PATH" || exit 0

# Only enforce when the latest pipeline command is validate.
[ "$(latest_pipeline_command "$TRANSCRIPT_PATH")" = "pipeline:validate" ] || \
[ "$(detect_active_command "$TRANSCRIPT_PATH")" = "validate" ] || exit 0

CMDS=$(extract_bash_commands "$TRANSCRIPT_PATH")
# Anchor the runner to a command position (line start or after a shell
# separator) and drop --help/--version, so a bare `echo "dotnet test"` doesn't
# satisfy the gate. This is a NUDGE against omission — it can't prove the run
# passed (that's the /commit gate's job, enforced by enforce-commit-gate.sh).
RUNNER_AT='(^|[;&|(])[[:space:]]*'
if ! echo "$CMDS" | grep -E "${RUNNER_AT}(dotnet test|bin/gate\.sh)" | grep -vqE -- '--help|--version'; then
    { echo ""; echo "STOP BLOCKED — /pipeline:validate but tests did not execute:"; echo ""
      echo "  VIOLATION: tests never ran. Run: dotnet test MonoRpgMaker.slnx  (or bin/gate.sh)."
      echo ""; echo "CONSTITUTION §15: if it didn't happen in the transcript, it didn't happen."; } >&2
    exit 2
fi
exit 0
