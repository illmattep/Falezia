#!/bin/bash
# FALEZIA · sceglie quale modello usa il gioco per un'arma: Meshy o arsenal2.
#   bash usa.sh meshy     <id ...>|tutte   → art/weapons/meshy/<id>/ in Assets/StreamingAssets/weapons/<id>/
#   bash usa.sh originale <id ...>|tutte   → rimette arsenal2 da art/weapons/arsenal2_backup/
# Copia .glb, .json e la miniatura dello shop (ui/<id>.png). I backup stanno FUORI da
# StreamingAssets, così non finiscono nella build.
set -e
QUI="$(cd "$(dirname "$0")" && pwd)"
RADICE="$(cd "$QUI/../../.." && pwd)"
DEST="$RADICE/Assets/StreamingAssets/weapons"
BAK="$RADICE/art/weapons/arsenal2_backup"
MODO="$1"; shift || true
if [ "$1" = "tutte" ]; then set -- standard hush snub hammer chatter blitz cadence scrap choke sweeper anvil service spotter longshot; fi
[ -n "$MODO" ] && [ $# -gt 0 ] || { echo "uso: bash $0 meshy|originale <id ...>|tutte"; exit 1; }
for id in "$@"; do
  if [ "$MODO" = "meshy" ]; then
    [ -f "$QUI/$id/$id.glb" ] || { echo "$id: nessun modello Meshy"; continue; }
    mkdir -p "$BAK/$id" "$BAK/ui"
    [ -f "$BAK/$id/$id.glb" ] || { cp "$DEST/$id/$id.glb" "$DEST/$id/$id.json" "$BAK/$id/"; cp "$DEST/ui/$id.png" "$BAK/ui/" 2>/dev/null || true; }
    cp "$QUI/$id/$id.glb" "$QUI/$id/$id.json" "$DEST/$id/"
    [ -f "$QUI/ui/$id.png" ] && cp "$QUI/ui/$id.png" "$DEST/ui/"
    echo "$id → Meshy"
  elif [ "$MODO" = "originale" ]; then
    [ -f "$BAK/$id/$id.glb" ] || { echo "$id: nessun backup"; continue; }
    cp "$BAK/$id/$id.glb" "$BAK/$id/$id.json" "$DEST/$id/"
    [ -f "$BAK/ui/$id.png" ] && cp "$BAK/ui/$id.png" "$DEST/ui/"
    echo "$id → arsenal2"
  else echo "modo sconosciuto: $MODO"; exit 1; fi
done
