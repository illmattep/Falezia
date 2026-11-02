"""Esporta TUTTO il kit: case + arredi, con le texture importate (PBR completo).

Un solo punto d'ingresso: `python3 run.py export_kit.py` (dentro Blender via MCP).
"""
import sys, os, bpy

ROOT = "/home/matteo/Progetti/Falezia"
sys.path.insert(0, f"{ROOT}/tools/map-assets")
import importlib, kit
importlib.reload(kit)

# (nome, larghezza, profondità, altezza, piani, texture muro, texture tetto, bottega, persiane)
HOUSES = [
    ("casa_1f_ocra",   6.4, 7.0,  5.20, 1, "wall_venez_02",  "roof_coppi_01", False, (0.96,0.94,0.92)),
    ("casa_1f_crema",  7.0, 7.6,  5.20, 1, "wall_venez_01",  "roof_coppi_02", False, (0.30,0.34,0.44)),
    ("casa_1f_giallo", 6.8, 7.2,  5.20, 1, "wall_venez_02",  "roof_coppi_04", False, (0.24,0.34,0.26)),
    ("casa_bottega",   9.4, 7.4,  5.20, 1, "wall_grezzo_03", "roof_coppi_02", True,  (0.52,0.24,0.20)),
    ("casa_2f_ocra",   8.0, 8.0,  8.30, 2, "wall_venez_02",  "roof_coppi_01", False, (0.24,0.34,0.26)),
    ("casa_2f_terra",  7.4, 8.4,  8.30, 2, "wall_grezzo_03", "roof_coppi_03", False, (0.52,0.26,0.20)),
    ("casa_2f_crema",  8.6, 7.8,  8.30, 2, "wall_venez_01",  "roof_coppi_02", False, (0.30,0.34,0.44)),
    ("casa_2f_rosa",   7.8, 8.2,  8.30, 2, "wall_rose_01",   "roof_coppi_04", False, (0.32,0.28,0.24)),
    ("casa_2f_salvia", 8.2, 7.6,  8.30, 2, "wall_venez_04",  "roof_coppi_01", False, (0.22,0.30,0.22)),
    ("casa_2f_grezza", 8.4, 8.0,  8.30, 2, "wall_grezzo_01", "roof_coppi_03", False, (0.40,0.28,0.20)),
    ("casa_3f_ocra",   7.6, 9.0, 11.40, 3, "wall_venez_02",  "roof_coppi_03", False, (0.24,0.34,0.26)),
    ("casa_3f_terra",  8.0, 9.4, 11.40, 3, "wall_grezzo_01", "roof_coppi_02", False, (0.30,0.36,0.46)),
    ("casa_3f_rosa",   7.4, 8.8, 11.40, 3, "wall_rose_01",   "roof_coppi_04", False, (0.28,0.30,0.36)),
    ("casa_3f_crema",  7.8, 9.0, 11.40, 3, "wall_venez_01",  "roof_coppi_01", False, (0.34,0.30,0.24)),
]

count = 0
for name, w, d, h, fl, wall, roof, shop, shutter in HOUSES:
    kit.clear()
    objs = kit.house(w, d, h, fl, wall, (1.0, 1.0, 1.0), shop=shop,
                     shutter=shutter, roof_tex=roof, varname=name)
    p = kit.export(objs, name)
    print("H", name, round(os.path.getsize(p) / 1048576, 2), "MB")
    count += 1

for name, fn in kit.PIECES.items():
    kit.clear()
    p = kit.export(fn(), name)
    count += 1
    print("P", name, round(os.path.getsize(p) / 1048576, 2), "MB")

print("KIT", count, "pezzi")
