"""
FALEZIA · pipeline dei modelli Meshy → formato armi del gioco.

Per ogni arma:
  1. legge il .glb di Meshy (una mesh fusa, normalizzata a 1,9 m, volata verso -X, alto = +Y)
  2. divide la mesh in pezzi (body / slide / mag / pump) con regioni scelte guardando il profilo
  3. scala uniforme alla LUNGHEZZA del doc 12, ruota (volata → -Z), origine sull'impugnatura
  4. aggiunge i socket (muzzle, aim, eject, hand_r, hand_l) e animazioni semplici
  5. texture 8192 → 2048 (viewmodel) e 512 (terza persona), un solo materiale <id>_mat
  6. semplifica con gltfpack: ≤ 8000 triangoli il viewmodel, ≤ 1500 la terza persona
Uso: python3 -I build.py <id> [<id> ...]
"""
import io, json, math, os, struct, subprocess, sys, time
import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
from glbinfo import load, acc  # noqa: E402

SRC = os.path.join(HERE, "src")
OUT = os.path.join(HERE, "out")
TMP = os.path.join(HERE, "tmp")
GLTFPACK = "/home/claude/tools/meshoptimizer/gltfpack"
TRI_VM, TRI_TP = 8000, 1500
TEX_VM, TEX_TP = 2048, 512

# lunghezza × altezza × larghezza (mm), doc 12
DOC = {
    "standard": (197, 138, 33), "hush": (250, 145, 47), "snub": (165, 125, 38),
    "hammer": (270, 165, 45), "chatter": (210, 195, 35), "cadence": (680, 260, 60),
    "blitz": (500, 220, 58), "scrap": (550, 240, 52), "choke": (1150, 190, 55),
    "sweeper": (950, 250, 62), "anvil": (880, 250, 58), "service": (850, 245, 60),
    "spotter": (1225, 250, 62), "longshot": (1400, 260, 70),
}
CLASS = {"standard": "Pistola", "hush": "Pistola silenziata", "snub": "Rivoltella", "hammer": "Magnum",
         "chatter": "Pistola automatica", "cadence": "SMG", "blitz": "PDW", "scrap": "SMG economica",
         "choke": "Pompa", "sweeper": "Pompa semiautomatico", "anvil": "Fucile d'assalto 7,62",
         "service": "Fucile d'assalto", "spotter": "Cecchino semiautomatico", "longshot": "Fucile a otturatore"}


def inside_poly(x, y, pts):
    pts = np.asarray(pts)
    n = len(pts); res = np.zeros(len(x), bool)
    j = n - 1
    for i in range(n):
        xi, yi = pts[i]; xj, yj = pts[j]
        cond = ((yi > y) != (yj > y)) & (x < (xj - xi) * (y - yi) / (yj - yi + 1e-12) + xi)
        res ^= cond
        j = i
    return res


def tan(c):
    """Tela/cuoio chiari della cinghia (per non tagliarla insieme al caricatore)."""
    r, g, b = c[:, 0].astype(int), c[:, 1].astype(int), c[:, 2].astype(int)
    return (r > 95) & (r - b > 22)


# Punti nello spazio Meshy (x lungo l'arma, volata a -X; y in alto). Scelti sui profili con griglia.
#   G  = impugnatura all'altezza del grilletto (diventa l'origine)
#   M  = asse della volata · A = linea di mira (tacca o centro dell'ottica)
#   E  = finestra di espulsione (lato destro) · HL = mano sinistra
# parts: nome → funzione (cx, cy, colore) → maschera dei triangoli
CFG = {
    "standard": dict(G=(0.60, 0.15), M=(-0.95, 0.53), A=(0.80, 0.695), E=(-0.08, 0.58), HL=(0.45, -0.20),
                     parts={"slide": lambda x, y, c: (y > 0.44) & (x < 0.86),
                            "mag": lambda x, y, c: (y < -0.60) & (x > 0.33)}),
    "hush": dict(G=(0.65, 0.05), M=(-0.95, 0.205), A=(0.82, 0.46), E=(0.25, 0.38), HL=(0.55, -0.25),
                 parts={"slide": lambda x, y, c: (y > 0.30) & (x > -0.12),
                        "mag": lambda x, y, c: (y < -0.41) & (x > 0.55)}),
    "snub": dict(G=(0.55, -0.10), M=(-0.95, 0.43), A=(0.30, 0.62), E=(0.0, 0.35), HL=(0.45, -0.35),
                 parts={}),
    "hammer": dict(G=(0.62, 0.0), M=(-0.95, 0.32), A=(0.73, 0.47), E=(0.10, 0.38), HL=(0.55, -0.25),
                   parts={"slide": lambda x, y, c: (y > 0.27) & (x < 0.80),
                          "mag": lambda x, y, c: (y < -0.46) & (x > 0.42)}),
    "chatter": dict(G=(0.58, 0.32), M=(-0.95, 0.62), A=(0.70, 0.77), E=(0.08, 0.70), HL=(0.62, 0.0),
                    parts={"slide": lambda x, y, c: (y > 0.565) & (x > -0.585) & (x < 0.83),
                           "mag": lambda x, y, c: (y < -0.13) & (x > 0.38)}),
    "blitz": dict(G=(0.25, -0.05), M=(-0.95, 0.11), A=(0.20, 0.29), E=(-0.30, -0.12), HL=(-0.50, -0.10),
                  parts={}),
    "cadence": dict(G=(0.27, 0.15), M=(-0.95, 0.27), A=(0.21, 0.38), E=(0.0, 0.28), HL=(-0.58, 0.08),
                    parts={"mag": lambda x, y, c: inside_poly(x, y, [(-0.25, 0.20), (-0.09, 0.20), (-0.32, -0.43), (-0.49, -0.43)]) & ~tan(c)}),
    "scrap": dict(G=(0.18, 0.12), M=(-0.95, 0.30), A=(0.25, 0.46), E=(-0.10, 0.38), HL=(-0.16, 0.10),
                  parts={"mag": lambda x, y, c: (y < 0.0) & (x > -0.27) & (x < -0.05)}),
    "choke": dict(G=(0.40, 0.10), M=(-0.95, 0.19), A=(0.20, 0.21), E=(0.10, 0.15), HL=(-0.20, 0.12),
                  parts={"pump": lambda x, y, c: (x > -0.38) & (x < 0.07) & (y > 0.085) & (y < 0.172)}),
    "anvil": dict(G=(0.33, 0.03), M=(-0.95, 0.16), A=(-0.12, 0.27), E=(0.15, 0.17), HL=(-0.35, 0.12),
                  parts={"mag": lambda x, y, c: inside_poly(x, y, [(-0.11, 0.08), (0.03, 0.08), (-0.07, -0.30), (-0.19, -0.22), (-0.17, -0.15)])}),
    "sweeper": dict(G=(0.36, 0.06), M=(-0.95, 0.24), A=(0.33, 0.36), E=(0.05, 0.25), HL=(-0.40, 0.15),
                    parts={"mag": lambda x, y, c: (x > -0.11) & (x < 0.10) & (y < 0.10) & (y > -0.21) & ~tan(c)}),
    "service": dict(G=(0.35, 0.03), M=(-0.95, 0.18), A=(0.33, 0.27), E=(0.15, 0.17), HL=(-0.40, 0.15),
                    parts={"mag": lambda x, y, c: inside_poly(x, y, [(0.01, 0.02), (0.15, 0.02), (0.12, -0.25), (-0.02, -0.25)])}),
    "spotter": dict(G=(0.40, -0.02), M=(-0.95, 0.06), A=(0.42, 0.17), E=(0.22, 0.13), HL=(-0.40, -0.05),
                    parts={"mag": lambda x, y, c: inside_poly(x, y, [(0.06, -0.02), (0.18, -0.02), (0.16, -0.29), (0.04, -0.29)])}),
    "longshot": dict(G=(0.45, -0.03), M=(-0.95, 0.03), A=(0.44, 0.135), E=(0.30, 0.06), HL=(-0.30, -0.02),
                     parts={"mag": lambda x, y, c: (x > 0.13) & (x < 0.29) & (y < -0.04) & (y > -0.13)}),
}

# corsa dei pezzi nelle animazioni (metri, spazio del gioco)
SLIDE_TRAVEL = {"standard": 0.028, "hush": 0.028, "hammer": 0.034, "chatter": 0.028}
PUMP_TRAVEL = 0.09


def image_bytes(js, b, tex_index):
    im = js["images"][js["textures"][tex_index]["source"]]
    bv = js["bufferViews"][im["bufferView"]]
    o = bv.get("byteOffset", 0)
    return b[o:o + bv["byteLength"]]


def resized(data, size, normal=False):
    im = Image.open(io.BytesIO(data)).convert("RGB")
    im = im.resize((size, size), Image.LANCZOS)
    if normal:  # rinormalizza dopo il filtro
        a = np.asarray(im).astype(np.float32) / 127.5 - 1.0
        a /= np.maximum(np.linalg.norm(a, axis=2, keepdims=True), 1e-6)
        im = Image.fromarray(np.clip((a + 1.0) * 127.5 + 0.5, 0, 255).astype(np.uint8))
    out = io.BytesIO()
    im.save(out, "JPEG", quality=92 if not normal else 95, subsampling=0 if normal else 2)
    return out.getvalue()


class GlbWriter:
    def __init__(self):
        self.bin = bytearray(); self.views = []; self.accs = []

    def _view(self, data, target=None):
        while len(self.bin) % 4: self.bin += b"\0"
        v = {"buffer": 0, "byteOffset": len(self.bin), "byteLength": len(data)}
        if target: v["target"] = target
        self.bin += data; self.views.append(v)
        return len(self.views) - 1

    def acc(self, arr, kind, target=None, minmax=False):
        arr = np.ascontiguousarray(arr)
        ct = {np.dtype(np.float32): 5126, np.dtype(np.uint32): 5125}[arr.dtype]
        a = {"bufferView": self._view(arr.tobytes(), target), "componentType": ct,
             "count": int(arr.shape[0]), "type": kind}
        if minmax:
            a["min"] = arr.min(0).tolist() if arr.ndim > 1 else [float(arr.min())]
            a["max"] = arr.max(0).tolist() if arr.ndim > 1 else [float(arr.max())]
        self.accs.append(a)
        return len(self.accs) - 1

    def image(self, data):
        return self._view(data)

    def write(self, path, js):
        js["buffers"] = [{"byteLength": len(self.bin)}]
        js["bufferViews"] = self.views; js["accessors"] = self.accs
        j = json.dumps(js, separators=(",", ":")).encode()
        while len(j) % 4: j += b" "
        while len(self.bin) % 4: self.bin += b"\0"
        total = 12 + 8 + len(j) + 8 + len(self.bin)
        with open(path, "wb") as f:
            f.write(struct.pack("<III", 0x46546C67, 2, total))
            f.write(struct.pack("<II", len(j), 0x4E4F534A)); f.write(j)
            f.write(struct.pack("<II", len(self.bin), 0x004E4942)); f.write(self.bin)


def quat_x(deg):
    h = math.radians(deg) * 0.5
    return [math.sin(h), 0.0, 0.0, math.cos(h)]


def build(wid):
    cfg = CFG[wid]; L, H, W = DOC[wid]
    js, b = load(os.path.join(SRC, wid + ".glb"))
    pr = js["meshes"][0]["primitives"][0]
    P = acc(js, b, pr["attributes"]["POSITION"]).astype(np.float64)
    N = acc(js, b, pr["attributes"]["NORMAL"]).astype(np.float64)
    UV = acc(js, b, pr["attributes"]["TEXCOORD_0"]).astype(np.float32)
    I = acc(js, b, pr["indices"]).reshape(-1, 3).astype(np.int64)
    mat = js["materials"][0]; pbr = mat["pbrMetallicRoughness"]
    tex_bc = image_bytes(js, b, pbr["baseColorTexture"]["index"])
    tex_mr = image_bytes(js, b, pbr["metallicRoughnessTexture"]["index"])
    tex_n = image_bytes(js, b, mat["normalTexture"]["index"])

    # --- colore per triangolo (serve solo a escludere la cinghia dai tagli)
    small = np.asarray(Image.open(io.BytesIO(tex_bc)).convert("RGB").resize((1024, 1024)))
    cuv = UV[I].mean(1)
    col = small[np.clip(((cuv[:, 1] % 1) * 1024).astype(int), 0, 1023),
                np.clip(((cuv[:, 0] % 1) * 1024).astype(int), 0, 1023)]

    # --- divisione in pezzi
    cen = P[I].mean(1)
    owner = np.zeros(len(I), np.int64)  # 0 = body
    names = ["body"]
    for pname, rule in cfg["parts"].items():
        m = rule(cen[:, 0], cen[:, 1], col) & (owner == 0)
        if m.sum() == 0:
            continue
        names.append(pname); owner[m] = len(names) - 1

    # --- trasformazione: origine sull'impugnatura, scala sulla lunghezza, volata → -Z
    span = P[:, 0].max() - P[:, 0].min()
    s = (L / 1000.0) / span
    zc = 0.5 * (P[:, 2].max() + P[:, 2].min())
    G = np.array([cfg["G"][0], cfg["G"][1], zc])

    def T(p):  # (n,3) spazio Meshy → spazio gioco
        q = (np.atleast_2d(p) - G) * s
        return np.stack([-q[:, 2], q[:, 1], q[:, 0]], 1)

    def Tn(n):
        return np.stack([-n[:, 2], n[:, 1], n[:, 0]], 1)

    PG = T(P).astype(np.float32)
    NG = Tn(N); NG /= np.maximum(np.linalg.norm(NG, axis=1, keepdims=True), 1e-9); NG = NG.astype(np.float32)

    half_w = 0.5 * (P[:, 2].max() - P[:, 2].min())
    sock = {
        "socket_muzzle": T([cfg["M"][0], cfg["M"][1], zc])[0],
        "socket_aim": T([cfg["A"][0], cfg["A"][1], zc])[0],
        "socket_eject": T([cfg["E"][0], cfg["E"][1], zc - 0.8 * half_w])[0],   # Meshy -Z = lato destro
        "socket_hand_r": T([cfg["G"][0], cfg["G"][1], zc])[0],
        "socket_hand_l": T([cfg["HL"][0], cfg["HL"][1], zc + 0.9 * half_w])[0],
    }

    dims = (PG.max(0) - PG.min(0)) * 1000.0   # x = larghezza, y = altezza, z = lunghezza
    report = {"id": wid, "src_tris": int(len(I)), "scale": s,
              "size_mm": [round(float(dims[2]), 1), round(float(dims[1]), 1), round(float(dims[0]), 1)],
              "doc_mm": [L, H, W],
              "parts": {n: int((owner == i).sum()) for i, n in enumerate(names)}}

    os.makedirs(TMP, exist_ok=True); os.makedirs(os.path.join(OUT, wid), exist_ok=True)
    import bake as bk
    wtmp = os.path.join(TMP, wid); os.makedirs(wtmp, exist_ok=True)
    t0 = time.time()
    hi_bc = np.asarray(Image.open(io.BytesIO(tex_bc)).convert("RGB"))
    hi_mr = np.asarray(Image.open(io.BytesIO(tex_mr)).convert("RGB"))
    hi_n = np.asarray(Image.open(io.BytesIO(tex_n)).convert("RGB"))

    def sub(mask):
        tris = I[mask]
        used, inv = np.unique(tris.ravel(), return_inverse=True)
        return PG[used], NG[used], UV[used], inv.reshape(-1, 3)

    # --- viewmodel: un pezzo alla volta, stesso atlante
    hi_parts = [sub(owner == i) for i in range(len(names))]
    lows = bk.simplify([(p, i) for p, _, _, i in hi_parts], TRI_VM, wtmp)
    creased = [bk.crease_normals(p.astype(np.float64), i) for p, i in lows]
    atl, info_vm = bk.make_atlas(creased, TEX_VM, 4, wtmp)
    vm_meshes = [(p, n, uv, i, bk.tangents(p.astype(np.float64), n, uv, i)) for p, n, uv, i in atl]
    srcs = [bk.HighSource(p, n, uv, i, hi_bc, hi_mr, hi_n) for p, n, uv, i in hi_parts]
    imgs_vm, cov_vm, dist_vm = bk.bake(vm_meshes, srcs, TEX_VM)
    # --- terza persona: una mesh sola
    low_tp = bk.simplify([(PG, I)], TRI_TP, wtmp)
    cr_tp = [bk.crease_normals(p.astype(np.float64), i) for p, i in low_tp]
    atl_tp, info_tp = bk.make_atlas(cr_tp, TEX_TP, 2, wtmp)
    tp_meshes = [(p, n, uv, i, bk.tangents(p.astype(np.float64), n, uv, i)) for p, n, uv, i in atl_tp]
    src_all = bk.HighSource(PG, NG, UV, I, hi_bc, hi_mr, hi_n)
    imgs_tp, cov_tp, dist_tp = bk.bake(tp_meshes, [src_all], TEX_TP)
    del hi_bc, hi_mr, hi_n
    report["bake"] = {"vm_atlas": info_vm, "vm_coverage": round(cov_vm, 3), "vm_dist_mm_med_p99": [round(d * 1000, 2) for d in dist_vm],
                      "tp_atlas": info_tp, "tp_coverage": round(cov_tp, 3), "tp_dist_mm_med_p99": [round(d * 1000, 2) for d in dist_tp],
                      "seconds": round(time.time() - t0, 1)}
    report["tris"] = {"viewmodel": int(sum(len(m[3]) for m in vm_meshes)), "tp": int(len(tp_meshes[0][3]))}
    report["parts_tris"] = {n: int(len(m[3])) for n, m in zip(names, vm_meshes)}

    def jpg(a, q=92):
        o = io.BytesIO(); Image.fromarray(a).save(o, "JPEG", quality=q, subsampling=0); return o.getvalue()
    for k, nm in zip(range(3), ("basecolor", "orm", "normal")):
        Image.fromarray(imgs_vm[k]).save(os.path.join(TMP, f"{wid}_{nm}_{TEX_VM}.png"))
    texs = {"vm": [jpg(imgs_vm[0]), jpg(imgs_vm[1]), jpg(imgs_vm[2], 95)],
            "tp": [jpg(imgs_tp[0]), jpg(imgs_tp[1]), jpg(imgs_tp[2], 95)]}

    def material_block(w, kind):
        imgs = []
        for i, (data, nm) in enumerate(zip(texs[kind], ("basecolor", "orm", "normal"))):
            imgs.append({"bufferView": w.image(data), "mimeType": "image/jpeg", "name": f"{wid}_{nm}"})
        return {
            "images": imgs,
            "samplers": [{"magFilter": 9729, "minFilter": 9987, "wrapS": 33071, "wrapT": 33071}],
            "textures": [{"sampler": 0, "source": i} for i in range(3)],
            "materials": [{
                "name": f"{wid}_mat",
                "pbrMetallicRoughness": {"baseColorTexture": {"index": 0}, "metallicRoughnessTexture": {"index": 1},
                                         "metallicFactor": 1.0, "roughnessFactor": 1.0},
                "normalTexture": {"index": 2}, "doubleSided": False}],
        }

    def mesh_of(w, m, name):
        p, n, uv, i, tan = m
        prim = {"attributes": {
            "POSITION": w.acc(np.ascontiguousarray(p, np.float32), "VEC3", 34962, minmax=True),
            "NORMAL": w.acc(np.ascontiguousarray(n, np.float32), "VEC3", 34962),
            "TANGENT": w.acc(np.ascontiguousarray(tan, np.float32), "VEC4", 34962),
            "TEXCOORD_0": w.acc(np.ascontiguousarray(uv, np.float32), "VEC2", 34962)},
            "indices": w.acc(np.ascontiguousarray(i.ravel(), np.uint32), "SCALAR", 34963), "material": 0}
        return {"name": name, "primitives": [prim]}

    # ------------------------------------------------ viewmodel
    w = GlbWriter()
    out = {"asset": {"version": "2.0", "generator": "falezia meshy pipeline"}, "scene": 0}
    out.update(material_block(w, "vm"))
    nodes = [{"name": wid, "children": []}]
    meshes = []
    node_of = {}
    for i, n in enumerate(names):
        meshes.append(mesh_of(w, vm_meshes[i], n))
        nodes.append({"name": n, "mesh": len(meshes) - 1}); node_of[n] = len(nodes) - 1
        nodes[0]["children"].append(len(nodes) - 1)
    for sn, pos in sock.items():
        nodes.append({"name": sn, "translation": [float(v) for v in pos]}); node_of[sn] = len(nodes) - 1
        nodes[0]["children"].append(len(nodes) - 1)
    out["nodes"] = nodes; out["meshes"] = meshes; out["scenes"] = [{"name": "Scene", "nodes": [0]}]

    # --- animazioni: lunghezze identiche alle versioni DeepSeek (stessi tempi della simulazione)
    lens = json.load(open(f"/mnt/user-data/uploads/Falezia/Assets/StreamingAssets/weapons/{wid}/{wid}.json"))["animations"]
    lens = {k: v["length_s"] for k, v in lens.items()}
    anims = []

    def channel(node, path, times, values):
        t = np.asarray(times, np.float32); v = np.asarray(values, np.float32)
        kind = {"translation": "VEC3", "rotation": "VEC4", "scale": "VEC3"}[path]
        return (node, path, w.acc(t, "SCALAR", minmax=True), w.acc(v, kind))

    def add_anim(name, chans):
        a = {"name": name, "samplers": [], "channels": []}
        for node, path, ti, vi in chans:
            a["samplers"].append({"input": ti, "output": vi, "interpolation": "LINEAR"})
            a["channels"].append({"sampler": len(a["samplers"]) - 1, "target": {"node": node_of.get(node, 0) if node != wid else 0, "path": path}})
        anims.append(a)

    zero = [0.0, 0.0, 0.0]; ident = [0.0, 0.0, 0.0, 1.0]
    has = set(names)
    mover = "slide" if "slide" in has else ("pump" if "pump" in has else None)
    travel = SLIDE_TRAVEL.get(wid, 0.028) if mover == "slide" else PUMP_TRAVEL
    back = [0.0, 0.0, travel]          # indietro = +Z
    pump_back = [0.0, 0.0, travel]

    # idle: tutto fermo
    add_anim("idle", [channel(wid, "rotation", [0, lens["idle"]], [ident, ident])])
    Lf = lens["fire"]
    if mover == "slide":
        add_anim("fire", [channel("slide", "translation", [0, 0.03, min(0.09, Lf), Lf], [zero, back, zero, zero])])
        add_anim("fire_last", [channel("slide", "translation", [0, 0.03, Lf], [zero, back, back])])
    elif mover == "pump":   # colpo, poi pompa indietro e avanti
        add_anim("fire", [channel("pump", "translation", [0, 0.35 * Lf, 0.6 * Lf, 0.85 * Lf], [zero, zero, pump_back, zero])])
        add_anim("fire_last", [channel("pump", "translation", [0, Lf], [zero, zero])])
    else:
        add_anim("fire", [channel(wid, "rotation", [0, Lf], [ident, ident])])
        add_anim("fire_last", [channel(wid, "rotation", [0, Lf], [ident, ident])])
    for rn in ("reload", "reload_empty"):
        Lr = lens[rn]; ch = []
        if "mag" in has:
            down = [0.0, -0.14, 0.02]
            ch.append(channel("mag", "translation", [0, 0.15 * Lr, 0.40 * Lr, 0.55 * Lr, 0.80 * Lr, Lr],
                              [zero, zero, down, down, zero, zero]))
            ch.append(channel("mag", "scale", [0, 0.39 * Lr, 0.40 * Lr, 0.55 * Lr, 0.56 * Lr, Lr],
                              [[1, 1, 1], [1, 1, 1], [0.001] * 3, [0.001] * 3, [1, 1, 1], [1, 1, 1]]))
        if rn == "reload_empty" and mover == "slide":
            ch.append(channel("slide", "translation", [0, 0.85 * Lr, 0.90 * Lr, Lr], [back, back, zero, zero]))
        if rn == "reload_empty" and mover == "pump":
            ch.append(channel("pump", "translation", [0, 0.80 * Lr, 0.88 * Lr, 0.96 * Lr, Lr], [zero, zero, pump_back, zero, zero]))
        if not ch:
            ch.append(channel(wid, "rotation", [0, Lr], [ident, ident]))
        add_anim(rn, ch)
    add_anim("draw", [channel(wid, "rotation", [0, lens["draw"]], [quat_x(-50), ident])])
    add_anim("holster", [channel(wid, "rotation", [0, lens["holster"]], [ident, quat_x(-50)])])
    out["animations"] = anims

    vm = os.path.join(OUT, wid, wid + ".glb"); tp = os.path.join(OUT, wid, wid + "_tp.glb")
    w.write(vm, out)

    # ------------------------------------------------ terza persona: una mesh, niente pezzi, niente animazioni
    w2 = GlbWriter()
    out2 = {"asset": {"version": "2.0", "generator": "falezia meshy pipeline"}, "scene": 0}
    out2.update(material_block(w2, "tp"))
    out2["meshes"] = [mesh_of(w2, tp_meshes[0], wid + "_tp")]
    out2["nodes"] = [{"name": wid + "_tp", "mesh": 0}]
    out2["scenes"] = [{"name": "Scene", "nodes": [0]}]
    w2.write(tp, out2)

    report["anim_lengths"] = lens
    report["sockets"] = {k: [round(float(x), 4) for x in v] for k, v in sock.items()}
    report["class"] = CLASS[wid]
    json.dump(report, open(os.path.join(TMP, wid + "_report.json"), "w"), indent=1)
    print(json.dumps(report))


if __name__ == "__main__":
    for wid in sys.argv[1:]:
        build(wid)
