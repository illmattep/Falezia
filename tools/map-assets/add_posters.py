"""Manifesti murali accanto alle botteghe (una su tre), alla quota di lettura."""
import json, math, os
ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
P = os.path.join(ROOT, "Assets/StreamingAssets/map/props.json")
d = json.load(open(P))
shops = [i for i in d["items"] if i["model"].endswith("facade_shop_front")]
n = 0
for k, s in enumerate(shops):
    if k % 3:
        continue
    r = math.radians(s["rot"])
    nx, nz = math.sin(r), math.cos(r)
    sx, sz = math.cos(r), -math.sin(r)
    off = 1.6 if k % 2 else -1.6
    d["items"].append({"model": f"map/posters/poster_{k%3+1:02d}",
                       "x": round(s["x"] + sx*off + nx*0.10, 2), "y": 1.75,
                       "z": round(s["z"] + sz*off + nz*0.10, 2),
                       "rot": s["rot"], "scale": 1.0})
    n += 1
json.dump(d, open(P, "w"), indent=0, ensure_ascii=False)
print(f"RESULT: {n} manifesti · {len(d['items'])} oggetti")
