"""Balconi sopra le botteghe e le porte: soletta + ringhiera, al primo piano.

La soletta sporge di 0,55 m dalla facciata, la ringhiera poggia sul bordo esterno.
Si appoggiano alla stessa posizione delle vetrine, quindi sono sempre su un muro vero.
"""
import json, math, os
ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
P = os.path.join(ROOT, "Assets/StreamingAssets/map/props.json")
d = json.load(open(P))

shops = [i for i in d["items"] if i["model"].endswith(("facade_shop_front", "facade_arch_door"))]
n = 0
for k, s in enumerate(shops):
    if k % 2:                     # un balcone ogni due aperture
        continue
    r = math.radians(s["rot"])
    nx, nz = math.sin(r), math.cos(r)
    y = 3.35                      # primo piano
    x, z = s["x"] + nx * 0.05, s["z"] + nz * 0.05
    d["items"].append({"model": "map/balconies/balcony_slab", "x": round(x,3), "y": y,
                       "z": round(z,3), "rot": s["rot"], "scale": 1.0})
    d["items"].append({"model": "map/balconies/balcony_rail", "x": round(x,3), "y": y,
                       "z": round(z,3), "rot": s["rot"], "scale": 1.0})
    n += 2
json.dump(d, open(P, "w"), indent=0, ensure_ascii=False)
print(f"RESULT: {n//2} balconi · {len(d['items'])} oggetti")
