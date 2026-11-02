"""
FALEZIA · allinea il viewmodel Meshy all'arma di arsenal2 che sostituisce: stessa posizione della
mano destra (socket_hand_r) rispetto all'origine, così la posa da fianco di WeaponView (Unity),
tarata su arsenal2, resta giusta. Sposta vertici e socket; i pezzi mobili restano con trasformata zero.
Uso: python3 -I align.py <dir_arsenal2_streaming> <id> [...]
"""
import json, os, struct, sys
import numpy as np
HERE = os.path.dirname(os.path.abspath(__file__)); sys.path.insert(0, HERE)
from glbinfo import load, acc  # noqa
from post import write_glb  # noqa

ref_dir = sys.argv[1]
for wid in sys.argv[2:]:
    rj, _ = load(os.path.join(ref_dir, wid, wid + ".glb"))
    ref = {n.get("name"): n for n in rj["nodes"]}
    off = np.array(ref["socket_hand_r"].get("translation", [0, 0, 0]), np.float32)
    path = os.path.join(HERE, "out", wid, wid + ".glb")
    js, b = load(path); b = bytearray(b)
    by = {n.get("name"): n for n in js["nodes"]}
    cur = np.array(by["socket_hand_r"]["translation"], np.float32)
    d = off - cur
    if np.abs(d).max() < 1e-6:
        print(wid, "già allineata"); continue
    done = set()
    for m in js["meshes"]:
        for p in m["primitives"]:
            ai = p["attributes"]["POSITION"]
            if ai in done: continue
            done.add(ai)
            a = js["accessors"][ai]; bv = js["bufferViews"][a["bufferView"]]
            o = bv.get("byteOffset", 0) + a.get("byteOffset", 0)
            P = np.frombuffer(bytes(b[o:o + a["count"] * 12]), np.float32).reshape(-1, 3) + d
            b[o:o + a["count"] * 12] = P.astype(np.float32).tobytes()
            a["min"] = P.min(0).tolist(); a["max"] = P.max(0).tolist()
    for n in js["nodes"]:
        if n.get("name", "").startswith("socket_"):
            n["translation"] = (np.array(n["translation"]) + d).tolist()
    write_glb(path, js, b)
    jp = os.path.join(HERE, "out", wid, wid + ".json")
    meta = json.load(open(jp))
    meta["sockets"] = {n["name"]: [round(v, 4) for v in n["translation"]] for n in js["nodes"] if n.get("name", "").startswith("socket_")}
    meta["origin"] = "allineata ad arsenal2: socket_hand_r nello stesso punto"
    json.dump(meta, open(jp, "w"), indent=2, ensure_ascii=False)
    print(wid, "spostata di", np.round(d, 4))
