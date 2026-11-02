"""Stemmi di fazione vicino ai due spawn: ancora al Porto, torre al Forte.

Si appendono alla bottega più vicina allo spawn, così sono sempre su un muro vero.
"""
import json, math, os
ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
P = os.path.join(ROOT, "Assets/StreamingAssets/map/props.json")
d = json.load(open(P))
shops = [i for i in d["items"] if i["model"].endswith("facade_shop_front")]
SPAWNS = [("mark_nord", 0, -91), ("mark_sud", 0, 91)]
n = 0
for model, px, pz in SPAWNS:
    near = sorted(shops, key=lambda s: (s["x"]-px)**2 + (s["z"]-pz)**2)[:2]
    for k, s in enumerate(near):
        r = math.radians(s["rot"])
        nx, nz = math.sin(r), math.cos(r)
        sx, sz = math.cos(r), -math.sin(r)
        off = -2.2 if k == 0 else 2.2
        d["items"].append({"model": f"map/marks/{model}",
                           "x": round(s["x"] + sx*off + nx*0.12, 2), "y": 3.6,
                           "z": round(s["z"] + sz*off + nz*0.12, 2),
                           "rot": s["rot"], "scale": 1.0})
        n += 1
json.dump(d, open(P, "w"), indent=0, ensure_ascii=False)
print(f"RESULT: {n} stemmi · {len(d['items'])} oggetti")
