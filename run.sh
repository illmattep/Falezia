#!/usr/bin/env bash
# FALEZIA — launcher di sviluppo.
#   ./run.sh test | core | server | dev | unity | build | play | map | sky | fx | sound
#
# Client: Unity 6 (URP) nella radice del repo.
# Server: .NET 8 dedicato (server/Falezia.Server).
# Simulazione: una sola copia in game/sim + game/net, compilata da shared/Falezia.Core.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
export PATH="$HOME/.dotnet:$PATH"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1
# sulla macchina c'è solo il runtime 10: il server è compilato per net8.0
export DOTNET_ROLL_FORWARD="${DOTNET_ROLL_FORWARD:-Major}"

UNITY="${UNITY_BIN:-$HOME/Unity/Hub/Editor/6000.6.7f1/Editor/Unity}"
PLAYER="$ROOT/build/linux/Falezia.x86_64"

# la copia del core dentro Assets/ deve essere identica a game/sim + game/net
sync_core() {
  "$ROOT/tools/unity/sync-core.sh"
}

need_unity() {
  if [ ! -x "$UNITY" ]; then
    echo "Unity non trovato in: $UNITY"
    echo "Imposta UNITY_BIN=/percorso/Unity"
    exit 1
  fi
}

cmd="${1:-help}"; shift || true

case "$cmd" in
  test)
    cd "$ROOT/tests"
    exec dotnet run --project Falezia.Tests.csproj "$@"
    ;;
  core)
    exec dotnet build "$ROOT/shared/Falezia.Core/Falezia.Core.csproj" -c Release "$@"
    ;;
  server)
    exec dotnet run --project "$ROOT/server/Falezia.Server" -- "$@"
    ;;
  unity)
    need_unity
    sync_core
    exec "$UNITY" -projectPath "$ROOT" "$@"
    ;;
  build)
    # player Linux: finisce in build/linux/Falezia.x86_64
    need_unity
    sync_core
    exec "$UNITY" -batchmode -quit -nographics -projectPath "$ROOT" \
      -executeMethod Falezia.EditorTools.FaleziaBuild.BuildLinux -logFile /tmp/falezia-build.log
    ;;
  play)
    if [ ! -x "$PLAYER" ]; then
      echo "player non compilato: lancia prima ./run.sh build"
      exit 1
    fi
    exec "$PLAYER" "$@"
    ;;
  sync)
    # allinea la copia del core dentro Assets/ con game/sim + game/net
    exec "$ROOT/tools/unity/sync-core.sh"
    ;;
  dev)
    # control plane in primo piano: pannello su http://127.0.0.1:8090, coda e istanze.
    # Ricompila solo il control plane e (una volta, all'avvio) il server: mai il gioco.
    exec dotnet run --project "$ROOT/server/Falezia.Control" -- "$@"
    ;;
  deploy)
    # impacchetta control plane + server per la VPS, in dist/
    exec "$ROOT/deploy/publish.sh" "$@"
    ;;
  sound)
    # suoni delle armi: sintetizzati in Assets/StreamingAssets/audio/weapons (numpy)
    exec python3 "$ROOT/tools/audio/make_weapon_sounds.py" "$@"
    ;;
  tex)
    # texture della mappa: le genera in Assets/Resources/MapTex
    exec python3 "$ROOT/tools/map/make_map_textures.py" "$@"
    ;;
  map)
    # rigenera la mappa e verifica rotte e tempi (doc 02); serve solo Python 3 (+ Pillow per l'anteprima)
    exec python3 "$ROOT/tools/mapgen/calanera.py" --preview
    ;;
  sky)
    if [ "${1:-}" = "make" ]; then
      shift
      exec python3 "$ROOT/tools/skybox/make_sky.py" "$@"
    fi
    exec python3 "$ROOT/tools/skybox/cubemap_to_panorama.py" "$@"
    ;;
  fx)
    exec python3 "$ROOT/tools/fx/make_fx_textures.py" "$@"
    ;;
  *)
    echo "FALEZIA — comandi:"
    echo "  ./run.sh test     test della simulazione e della mappa (244 test)"
    echo "  ./run.sh core     compila la simulazione condivisa (netstandard2.1)"
    echo "  ./run.sh server   server dedicato .NET (--port= --max= --map=)"
    echo "  ./run.sh dev      pannello + matchmaking + istanze (http://127.0.0.1:8090)"
    echo "  ./run.sh deploy   impacchetta tutto per la VPS, in dist/"
    echo "  ./run.sh unity    apre il progetto nell'editor Unity"
    echo "  ./run.sh build    compila il player Linux in build/linux/"
    echo "  ./run.sh play     avvia il player (-faleziaOnline host:port per giocare online)"
    echo "  ./run.sh sync     allinea la copia del core dentro Assets/ (game/sim + game/net)"
    echo "  ./run.sh sound    rigenera i suoni delle armi (Python + numpy)"
    echo "  ./run.sh tex      rigenera le texture della mappa (Python)"
    echo "  ./run.sh map      rigenera e verifica la mappa Calanera (Python)"
    echo "  ./run.sh sky      cielo: \"make\" per rigenerarlo, oppure una cartella di sei facce"
    echo "  ./run.sh fx       rigenera le texture del fumo delle armi (Python)"
    ;;
esac
