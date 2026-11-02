#!/bin/sh
# Rigenera la HUSH da zero (02 fa scena, materiali, geometria, gerarchia, socket).
set -e
cd "$(dirname "$0")"
blender -b --factory-startup -P 02_detail.py
blender -b --factory-startup -P 03_uv_bake.py
blender -b --factory-startup -P 05_anim.py
blender -b --factory-startup -P 06_export.py
blender -b --factory-startup -P 04_render_sheet.py
