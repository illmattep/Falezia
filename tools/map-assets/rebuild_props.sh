#!/usr/bin/env bash
# Rigenera props.json da zero nell'ordine giusto: piazzamento → moduli e lore → manifesti → targhe.
set -e
cd "$(dirname "$0")/../.."
python3 tools/map-assets/make_placement.py
python3 tools/map-assets/add_lore_and_modules.py
python3 tools/map-assets/add_posters.py
python3 tools/map-assets/add_streets.py
python3 tools/map-assets/add_faction_marks.py
