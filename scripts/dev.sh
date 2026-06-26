#!/usr/bin/env bash
# RalphForge dev task dispatcher. Run on the codespace runtime.
#   ./scripts/dev.sh {build|test|verify [safe|unsafe]|loop|engine-check [prompt]|fmt|clean}
set -euo pipefail
cd "$(dirname "$0")/.."
export PATH="$HOME/.local/bin:$PATH"   # claude / z3 / gh in non-login shells

SLN=RalphForge.slnx
CLI=src/RalphForge.Cli

cmd="${1:-help}"
shift || true

case "$cmd" in
  build)        dotnet build "$SLN" ;;
  test)         dotnet test "$SLN" ;;
  verify)       dotnet run --project "$CLI" -- verify "${1:-safe}" ;;
  loop)         dotnet run --project "$CLI" -c Release -- loop ;;
  engine-check) dotnet run --project "$CLI" -- engine-check "$@" ;;
  fmt)          dotnet format "$SLN" || true ;;
  clean)        dotnet clean "$SLN"; find . -type d \( -name bin -o -name obj \) -prune -exec rm -rf {} + ;;
  *)            echo "usage: ./scripts/dev.sh {build|test|verify [safe|unsafe]|loop|engine-check [prompt]|fmt|clean}" ;;
esac
