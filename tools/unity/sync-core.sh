#!/usr/bin/env bash
# SYNC DEL CORE — la simulazione vive in game/sim + game/net, ma Unity non può compilare
# file che stanno fuori da Assets/: il client ne tiene una copia in Assets/Scripts/Sim|Net.
#
# Due copie che si allontanano sono un bug silenzioso: il client continua a girare con
# regole vecchie (è già successo: cadenze e armi a terra non arrivavano in build). Per questo
# la copia si fa SEMPRE prima di aprire l'editor o compilare il player: `./run.sh unity` e
# `./run.sh build` chiamano questo script da soli, e stampano solo i file cambiati.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
copiati=0

for pair in "game/sim:Assets/Scripts/Sim" "game/net:Assets/Scripts/Net"; do
  src="$ROOT/${pair%%:*}"
  dst="$ROOT/${pair##*:}"
  [ -d "$src" ] || continue
  mkdir -p "$dst"

  for f in "$src"/*.cs; do
    [ -e "$f" ] || continue
    b="$(basename "$f")"
    if ! cmp -s "$f" "$dst/$b"; then
      cp "$f" "$dst/$b"
      echo "sync core: $b"
      copiati=$((copiati + 1))
    fi
  done

  # un file rimasto solo nella copia è codice morto che continua a compilare: va detto
  for f in "$dst"/*.cs; do
    [ -e "$f" ] || continue
    b="$(basename "$f")"
    [ -e "$src/$b" ] || echo "ATTENZIONE: $b esiste solo in $dst (non più in $src)"
  done
done

[ "$copiati" -eq 0 ] && echo "sync core: già allineato"
exit 0
