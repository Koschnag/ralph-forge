#!/usr/bin/env bash
# Launch the CDD-v0 spec-repair loop. Meant to run inside the codespace `ralph`
# tmux session so it keeps running across client disconnects (Mac closed).
set -euo pipefail
cd "$(dirname "$0")/.."
export PATH="$HOME/.local/bin:$PATH"   # ensure claude / z3 / gh resolve in non-login shells
git pull --ff-only --quiet || true
exec dotnet run --project src/RalphForge.Cli -c Release -- "${@:-loop}"
