"""Compone i due spawn con i moduli Meshy, usando le MISURE REALI dei GLB.

Misure (glTF, Y su): capannone 6.00×4.52×0.63 · container 6.06×3.10×2.61 ·
muraglia 6.00×4.93×2.25 · porta 5.00×5.51×1.58 · torre 5.00×9.91×4.98 ·
braciere 1.77×1.10×1.76 · lampada 0.33×0.65×0.17 · stendardo 1.23×3.60×0.31.

Il pivot è al centro della base: x,z sono il CENTRO del pezzo, y la quota dei piedi.
  python3 tools/map-assets/make_spawns_from_modules.py
"""
import json, os

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
P = os.path.join(ROOT, "Assets/StreamingAssets/map/props.json")

d = json.load(open(P))
d["items"] = [i for i in d["items"] if not i["model"].startswith(("map/spawn/", "map/modules/"))]

# Il pivot dei moduli Meshy è al CENTRO dell'ingombro, non alla base:
# per appoggiarli a terra serve mezza altezza. Valori misurati sui GLB.
BASE = {"mod_01_warehouse": 2.26, "mod_02_container": 1.55, "mod_03_rampart": 2.47,
        "mod_04_gate": 2.76, "mod_05_tower": 4.96, "mod_06_brazier": 0.55,
        "mod_07_lantern": 0.32, "mod_08_banner": 1.80, "mod_09_roof": 0.05}
TOP  = {"mod_01_warehouse": 4.52, "mod_02_container": 3.10, "mod_03_rampart": 4.93,
        "mod_04_gate": 5.51, "mod_05_tower": 9.91, "mod_06_brazier": 1.10,
        "mod_07_lantern": 0.65, "mod_08_banner": 3.60, "mod_09_roof": 1.83}

def add(model, x, y, z, rot=0.0, scale=1.0):
    """y è la quota da terra del PIEDE: la conversione al pivot centrale la fa add()."""
    d["items"].append({"model": f"map/modules/{model}", "x": x, "y": y + BASE[model] * scale,
                       "z": z, "rot": rot, "scale": scale})

# ---------------------------------------------------------------- PORTO (Nord, z=-91)
# capannone: tre pannelli affiancati (6 m ciascuno), faccia verso la strada (+Z)
for cx in (-19.0, -13.0, -7.0):
    add("mod_01_warehouse", cx, 0, -98.3)
    add("mod_09_roof", cx, 4.52, -98.3)          # tetto in appoggio sul filo superiore
# lampade sulla faccia del capannone (fronte a z = -97.4 + 0.32)
for cx in (-15.0, -9.0):
    add("mod_07_lantern", cx, 3.15, -97.95)
# container: due a terra, due impilati, uno di traverso
for cx in (11.5, 17.6):
    add("mod_02_container", cx, 0, -97.2)
    add("mod_02_container", cx, 3.10, -97.2)

# ---------------------------------------------------------------- FORTE (Sud, z=+91)
# muraglia continua: 6 m per tratto, con il varco della porta fra -6 e 0
for cx in (-21.0, -15.0, -9.0, 3.0, 9.0, 15.0):
    add("mod_03_rampart", cx, 0, 96.6, rot=180)
# porta nel varco (5 m, centrata a -2.5)
add("mod_04_gate", -2.5, 0, 96.2, rot=180)
# torri dietro la muraglia
for cx in (-18.0, 12.0):
    add("mod_05_tower", cx, 0, 98.6, rot=180)
# bracieri davanti alla porta (scalati a 1 m di diametro)
add("mod_06_brazier", -6.4, 0, 93.4, scale=0.58)
add("mod_06_brazier", 1.4, 0, 93.4, scale=0.58)
# stendardi in cima alle torri
add("mod_08_banner", -17.2, 9.91, 98.6, rot=180)
add("mod_08_banner", 12.8, 9.91, 98.6, rot=180)

json.dump(d, open(P, "w"), indent=0, ensure_ascii=False)
mods = sum(1 for i in d["items"] if "modules" in i["model"])
print(f"RESULT: {len(d['items'])} oggetti · {mods} moduli")
