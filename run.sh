#!/usr/bin/env bash
# FALEZIA — launcher di sviluppo.
#   ./run.sh test | build | server | game | debug
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
export PATH="$HOME/.dotnet:$PATH"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1

GODOT="${GODOT_BIN:-$HOME/opt/godot-mono/Godot_v4.7.2-stable_mono_linux_x86_64/Godot_v4.7.2-stable_mono_linux.x86_64}"

if [ ! -x "$GODOT" ]; then
  echo "Godot .NET non trovato in: $GODOT"
  echo "Imposta GODOT_BIN=/percorso/di/godot"
  exit 1
fi

cmd="${1:-help}"; shift || true

case "$cmd" in
  test)
    cd "$ROOT/tests"
    dotnet run --project Falezia.Tests.csproj
    ;;
  build)
    cd "$ROOT/game"
    dotnet build Falezia.csproj
    ;;
  server)
    cd "$ROOT/game"
    exec "$GODOT" --headless --path . server/server.tscn -- "$@"
    ;;
  game)
    cd "$ROOT/game"
    exec "$GODOT" --path .
    ;;
  editor)
    cd "$ROOT/game"
    exec "$GODOT" --editor --path .
    ;;
  debug)
    cd "$ROOT/game"
    exec "$GODOT" --path . client/debug_room.tscn
    ;;
  *)
    echo "FALEZIA — comandi:"
    echo "  ./run.sh test      test della simulazione, senza Godot"
    echo "  ./run.sh build     compila il progetto Godot C#"
    echo "  ./run.sh server    server dedicato headless (argomenti: --port= --verbose --quiet)"
    echo "  ./run.sh game      menu principale"
    echo "  ./run.sh debug     stanza debug"
    echo "  ./run.sh editor    apre il progetto nell'editor Godot .NET"
    ;;
esac
