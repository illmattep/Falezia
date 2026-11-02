#!/usr/bin/env bash
# Copia nel client Unity gli asset che vivono fuori: armi (GLB + texture) e miniature
# del negozio. Ricopiali ogni volta che rigeneri l'arsenale con art/weapons/arsenal2.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
DST="$ROOT/Assets/StreamingAssets/weapons"

mkdir -p "$DST"

# 1) armi: game/assets/weapons/<id>/<id>.glb + texture + json delle durate
for dir in "$ROOT"/game/assets/weapons/*/; do
  id="$(basename "$dir")"
  [ "$id" = "ui" ] && continue
  [ "$id" = "_shared" ] && continue
  mkdir -p "$DST/$id"
  find "$dir" -maxdepth 1 -type f \( -name "*.glb" -o -name "*.json" -o -name "*.jpg" -o -name "*.png" \) \
    -exec cp -f {} "$DST/$id/" \;
done

# 2) miniature del negozio
mkdir -p "$DST/ui"
find "$ROOT/game/assets/weapons/ui" -maxdepth 1 -type f -name "*.png" -exec cp -f {} "$DST/ui/" \;

echo "armi sincronizzate in ${DST#"$ROOT"/}"
echo "  $(find "$DST" -name '*.glb' | wc -l) GLB · $(find "$DST/ui" -name '*.png' | wc -l) miniature"
