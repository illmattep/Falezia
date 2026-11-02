"""Aggiunge a props.json ciò che non nasce dal piazzamento automatico:

  · i moduli dei due spawn (capannone, container, muraglia, porta, torri, bracieri, stendardi);
  · le insegne dipinte sopra le botteghe;
  · le tende da sole.

Va eseguito DOPO make_placement.py, che rigenera il file da zero.

  python3 tools/map-assets/add_lore_and_modules.py
"""
import json, math, os

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
P = os.path.join(ROOT, "Assets/StreamingAssets/map/props.json")
d = json.load(open(P))

BASE = {"mod_01_warehouse": 2.26, "mod_02_container": 1.55, "mod_03_rampart": 2.47,
        "mod_04_gate": 2.76, "mod_05_tower": 4.96, "mod_06_brazier": 0.55,
        "mod_07_lantern": 0.32, "mod_08_banner": 1.80, "mod_09_roof": 0.05}

def add(model, x, y, z, rot=0.0, scale=1.0, base=0.0):
    d["items"].append({"model": model, "x": x, "y": y + base * scale, "z": z,
                       "rot": rot, "scale": scale})

# ---------------------------------------------------------------- Porto (Nord)
for cx in (-19.0, -13.0, -7.0):
    add("map/modules/mod_01_warehouse", cx, 0, -98.3, base=BASE["mod_01_warehouse"])
    add("map/modules/mod_09_roof", cx, 4.52, -98.3, base=BASE["mod_09_roof"])
for cx in (-15.0, -9.0):
    add("map/modules/mod_07_lantern", cx, 3.15, -97.95, base=BASE["mod_07_lantern"])
for cx in (11.5, 17.6):
    add("map/modules/mod_02_container", cx, 0, -97.2, base=BASE["mod_02_container"])
    add("map/modules/mod_02_container", cx, 3.10, -97.2, base=BASE["mod_02_container"])

# ---------------------------------------------------------------- Forte (Sud)
for cx in (-21.0, -15.0, -9.0, 3.0, 9.0, 15.0):
    add("map/modules/mod_03_rampart", cx, 0, 96.6, rot=180, base=BASE["mod_03_rampart"])
add("map/modules/mod_04_gate", -2.5, 0, 96.2, rot=180, base=BASE["mod_04_gate"])
for cx in (-18.0, 12.0):
    add("map/modules/mod_05_tower", cx, 0, 98.6, rot=180, base=BASE["mod_05_tower"])
add("map/modules/mod_06_brazier", -6.4, 0, 93.4, scale=0.58, base=BASE["mod_06_brazier"])
add("map/modules/mod_06_brazier", 1.4, 0, 93.4, scale=0.58, base=BASE["mod_06_brazier"])
add("map/modules/mod_08_banner", -17.2, 9.91, 98.6, rot=180, base=BASE["mod_08_banner"])
add("map/modules/mod_08_banner", 12.8, 9.91, 98.6, rot=180, base=BASE["mod_08_banner"])

# ---------------------------------------------------------------- insegne e tende
def normal(rot):
    r = math.radians(rot)
    return math.sin(r), math.cos(r)

shops = [i for i in d["items"] if i["model"].endswith("facade_shop_front")]
signs = awnings = 0
for k, s in enumerate(shops):
    nx, nz = normal(s["rot"])
    add(f"map/signs/sign_{k % 6 + 1:02d}", round(s["x"] + nx * 0.22, 2),
        round(s["y"] + 2.55, 2), round(s["z"] + nz * 0.22, 2), rot=s["rot"])
    signs += 1
    if k % 2 == 0:
        add(f"map/awnings/awning_{k % 3 + 1:02d}", round(s["x"] + nx * 0.55, 2),
            round(s["y"] + 2.35, 2), round(s["z"] + nz * 0.55, 2), rot=s["rot"])
        awnings += 1

json.dump(d, open(P, "w"), indent=0, ensure_ascii=False)
print(f"RESULT: {len(d['items'])} oggetti · {signs} insegne · {awnings} tende")
