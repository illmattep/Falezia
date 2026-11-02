"""
FALEZIA · controllo del formato armi sui .glb prodotti (stesse regole di 99_validate.py di arsenal,
più quelle che il client Unity dà per scontate). Uso: python3 -I validate.py <id> [...]
"""
import json, os, sys
import numpy as np
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
from glbinfo import load, acc  # noqa: E402

TRI_VM, TRI_TP = 8000, 1500
SOCKETS = ["socket_muzzle", "socket_aim", "socket_eye", "socket_eject", "socket_hand_r", "socket_hand_l"]
ANIMS = ["idle", "fire", "fire_last", "reload", "reload_empty", "draw", "holster"]
TOL_LEN = 0.02


def check(wid):
    errs, warns = [], []
    d = os.path.join(HERE, "out", wid)
    meta = json.load(open(os.path.join(d, wid + ".json")))
    js, b = load(os.path.join(d, wid + ".glb"))
    nodes = js["nodes"]; by = {n.get("name"): n for n in nodes}
    root = nodes[js["scenes"][0]["nodes"][0]]
    if root.get("name") != wid: errs.append(f"radice '{root.get('name')}' invece di '{wid}'")
    for k in ("translation", "rotation", "scale", "matrix"):
        if k in root: errs.append(f"la radice ha {k}: deve essere identità")
    kids = {nodes[i].get("name") for i in root.get("children", [])}
    for s in SOCKETS + ["body"]:
        if s not in kids: errs.append(f"manca {s} sotto la radice")
    for n in nodes:
        if "mesh" in n and any(k in n for k in ("rotation", "scale")):
            errs.append(f"{n.get('name')}: trasformata non applicata")
    tris = sum(js["accessors"][p["indices"]]["count"] // 3 for m in js["meshes"] for p in m["primitives"])
    if tris > TRI_VM: errs.append(f"viewmodel {tris} triangoli > {TRI_VM}")
    mats = {m.get("name") for m in js["materials"]}
    if mats != {wid + "_mat"}: errs.append(f"materiali {mats}")
    for m in js["meshes"]:
        for p in m["primitives"]:
            for a in ("POSITION", "NORMAL", "TANGENT", "TEXCOORD_0"):
                if a not in p["attributes"]: errs.append(f"{m['name']}: manca {a}")
    names = [a["name"] for a in js.get("animations", [])]
    for a in ANIMS:
        if a not in names: errs.append(f"manca la clip {a}")
    for a in js.get("animations", []):
        L = max(js["accessors"][s["input"]]["max"][0] for s in a["samplers"])
        want = meta["animations"][a["name"]]["length_s"]
        if abs(L - want) > 1e-3: errs.append(f"clip {a['name']} lunga {L:.3f} s, attesi {want}")
    # misure: lunghezza esatta (scala), altezza e larghezza solo segnalate
    lo, hi = [], []
    for n in nodes:
        if "mesh" in n:
            for p in js["meshes"][n["mesh"]]["primitives"]:
                P = acc(js, b, p["attributes"]["POSITION"]); lo.append(P.min(0)); hi.append(P.max(0))
    size = (np.max(hi, 0) - np.min(lo, 0)) * 1000
    L_, H_, W_ = meta["size_mm_doc12"]
    if abs(size[2] - L_) / L_ > TOL_LEN: errs.append(f"lunghezza {size[2]:.1f} mm, doc 12 {L_}")
    for nm, got, want in (("altezza", size[1], H_), ("larghezza", size[0], W_)):
        if abs(got - want) / want > 0.05:
            warns.append(f"{nm} {got:.0f} mm contro {want} del doc 12 ({(got - want) / want * 100:+.0f}%)")
    mz = by["socket_muzzle"]["translation"]
    if mz[2] > np.min(lo, 0)[2] + 0.03: warns.append("socket_muzzle lontano dalla punta")
    if abs(mz[2] - np.min(lo, 0)[2]) > 0.01: warns.append(f"socket_muzzle a {abs(mz[2] - np.min(lo, 0)[2]) * 1000:.0f} mm dalla punta")
    # terza persona
    jt, bt = load(os.path.join(d, wid + "_tp.glb"))
    tt = sum(jt["accessors"][p["indices"]]["count"] // 3 for m in jt["meshes"] for p in m["primitives"])
    if tt > TRI_TP: errs.append(f"terza persona {tt} triangoli > {TRI_TP}")
    if len(jt["meshes"]) != 1: errs.append("la terza persona deve essere una mesh sola")
    if jt.get("animations"): errs.append("la terza persona non deve avere clip")
    for im in jt["images"]:
        pass
    return errs, warns, tris, tt, size


if __name__ == "__main__":
    bad = 0
    for w in sys.argv[1:]:
        e, wa, t, tt, s = check(w)
        print(f"{w:9s} {'OK  ' if not e else 'FAIL'} vm {t} tri · tp {tt} tri · {s[2]:.0f}×{s[1]:.0f}×{s[0]:.0f} mm")
        for x in e: print("    ERRORE", x)
        for x in wa: print("    nota  ", x)
        bad += bool(e)
    sys.exit(1 if bad else 0)
