"""Targhe stradali in ceramica, appese alla bottega più vicina al punto della zona."""
import json, math, os
ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
P = os.path.join(ROOT, "Assets/StreamingAssets/map/props.json")
d = json.load(open(P))
ZONES = {"VIA STRETTA": (-6, -52), "LARGO DEL FORNO": (-6, -55), "PIAZZA VECCHIA": (6, -16),
         "MERCATO": (22, 0), "PORTO VECCHIO": (0, -91), "FORTE SAN VERANO": (0, 91),
         "LUNGOMARE": (41, 62), "SCALINATA": (-41, 62), "MURA": (-40, 20)}
shops = [i for i in d["items"] if i["model"].endswith("facade_shop_front")]
n = 0
for idx, name in enumerate(ZONES, 1):
    px, pz = ZONES[name]
    best = min(shops, key=lambda s: (s["x"]-px)**2 + (s["z"]-pz)**2, default=None)
    if best is None:
        continue
    r = math.radians(best["rot"])
    nx, nz = math.sin(r), math.cos(r)
    sx, sz = math.cos(r), -math.sin(r)
    off = 1.9 if idx % 2 else -1.9
    d["items"].append({"model": f"map/streets/street_{idx:02d}",
                       "x": round(best["x"] + sx*off + nx*0.12, 2), "y": 2.85,
                       "z": round(best["z"] + sz*off + nz*0.12, 2),
                       "rot": best["rot"], "scale": 1.0})
    n += 1
json.dump(d, open(P, "w"), indent=0, ensure_ascii=False)
print(f"RESULT: {n} targhe · {len(d['items'])} oggetti")
