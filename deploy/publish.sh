#!/usr/bin/env bash
# FALEZIA — impacchetta quello che va sulla VPS.
#
#   ./deploy/publish.sh                 → self-contained linux-x64 in dist/
#   ./deploy/publish.sh --portable      → dipende dal runtime .NET installato
#   RID=linux-arm64 ./deploy/publish.sh → altro target
#
# Produce:
#   dist/control/   il control plane (pannello + matchmaking + istanze)
#   dist/server/    il server di gioco, con la mappa accanto
#   dist/VERSION    commit e data, per sapere cosa gira
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
OUT="${OUT:-$ROOT/dist}"
RID="${RID:-linux-x64}"
MODE="${1:-self}"

export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1

if [ "$MODE" = "--portable" ]; then
  SELF=(--self-contained false)
  echo "→ publish portable ($RID): serve il runtime .NET sul server"
else
  SELF=(--self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true)
  echo "→ publish self-contained ($RID): nessun runtime richiesto"
fi

mkdir -p "$OUT"

echo "→ control plane"
dotnet publish "$ROOT/server/Falezia.Control/Falezia.Control.csproj" \
  -c Release -r "$RID" "${SELF[@]}" -o "$OUT/control" --nologo

echo "→ server di gioco"
dotnet publish "$ROOT/server/Falezia.Server/Falezia.Server.csproj" \
  -c Release -r "$RID" "${SELF[@]}" -o "$OUT/server" --nologo

# la mappa viaggia con il server: senza, parte con il mondo di test
mkdir -p "$OUT/server/maps"
cp "$ROOT/game/maps/calanera.json" "$OUT/server/maps/calanera.json"

{
  echo "commit: $(git -C "$ROOT" rev-parse --short HEAD 2>/dev/null || echo sconosciuto)"
  echo "data:   $(date -Iseconds)"
  echo "target: $RID"
  echo "modo:   $MODE"
  echo "utente: questa build non contiene il token: lo genera il primo avvio"
} > "$OUT/VERSION"

echo
echo "fatto:"
du -sh "$OUT/control" "$OUT/server" 2>/dev/null || true
echo "carica dist/ sulla VPS e lancia deploy/install-vps.sh"
