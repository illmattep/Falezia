"""
RI-RIGGING dell'operatore, senza Blender: riscrive scheletro e pesi dentro operator.glb.

Perché: lo scheletro di rig_operator.py era stato misurato con le braccia e il busto
~15 cm PIÙ INDIETRO del mesh (la scansione ha le braccia appese davanti al corpo).
Risultato: i vertici della mano andavano tutti al pollice (l'osso più vicino), l'avambraccio
al braccio, e appena le ossa si muovevano la mesh si strappava (mani lontane dall'arma,
avambracci "a tavola" in prima persona).

Qui i giunti sono misurati sulla superficie del mesh (sezioni del braccio, della gamba e del
busto) e i pesi si rifanno con le stesse regole di prima:
  - mani RIGIDE (ogni vertice al 100 % all'osso della mano o della falange più vicina);
  - resto del corpo: decadimento gaussiano dai 3 ossi più vicini, lati separati;
  - zaino e antenna al petto.
Le ossa hanno lo stesso nome, la stessa gerarchia e l'asse +Y lungo l'osso (come Blender),
quindi OperatorRig/OperatorView non cambiano.

Uso:  python3 rerig_operator.py IN.glb OUT.glb
"""
import sys, math, struct
import numpy as np
from pygltflib import GLTF2, BufferView, Accessor

# ------------------------------------------------------------------ giunti (coordinate Blender: x destra-modello, -y avanti, z su)
SH, EL, WR, HT = (0.205, -0.05, 1.375), (0.44, -0.09, 1.11), (0.565, -0.185, 0.955), (0.615, -0.21, 0.86)

def joints():
    J = {}
    J["pelvis"] = ((0, -0.11, 0.88), (0, -0.12, 1.02))
    J["spine"] = ((0, -0.12, 1.02), (0, -0.12, 1.20))
    J["chest"] = ((0, -0.12, 1.20), (0, -0.11, 1.37))
    J["neck"] = ((0, -0.11, 1.37), (0, -0.11, 1.47))
    J["head"] = ((0, -0.11, 1.47), (0, -0.11, 1.68))
    for s, sg in (("L", -1), ("R", 1)):
        f = lambda p: (p[0] * sg, p[1], p[2])
        J[f"clav.{s}"] = (f((0.04, -0.09, 1.34)), f(SH))
        J[f"arm.{s}"] = (f(SH), f(EL))
        J[f"fore.{s}"] = (f(EL), f(WR))
        J[f"hand.{s}"] = (f(WR), f(HT))
        J[f"thigh.{s}"] = (f((0.11, -0.10, 0.86)), f((0.145, -0.115, 0.47)))
        J[f"shin.{s}"] = (f((0.145, -0.115, 0.47)), f((0.19, -0.07, 0.09)))
        J[f"foot.{s}"] = (f((0.19, -0.07, 0.09)), f((0.21, -0.25, 0.04)))
        # dita: quattro a due falangi + pollice; la mano ha il palmo verso la coscia
        wr = np.array(f(WR)); ht = np.array(f(HT)); d = ht - wr; d /= np.linalg.norm(d)
        palm = np.array([-sg, 0.0, 0.0]); palm -= palm.dot(d) * d; palm /= np.linalg.norm(palm)
        w = np.cross(d, palm); w /= np.linalg.norm(w)
        if w[1] > 0: w = -w                       # w verso l'avanti (lato del pollice)
        knuckle = wr + d * 0.090
        for name, off in (("index", 0.028), ("middle", 0.009), ("ring", -0.010), ("pinky", -0.028)):
            p0 = knuckle + w * off; p1 = p0 + d * 0.045; p2 = p1 + d * 0.040
            J[f"{name}.01.{s}"] = (tuple(p0), tuple(p1)); J[f"{name}.02.{s}"] = (tuple(p1), tuple(p2))
        t0 = wr + d * 0.025 + w * 0.030 + palm * 0.012
        t1 = t0 + (d * 0.55 + w * 0.70).astype(float) / np.linalg.norm(d * 0.55 + w * 0.70) * 0.040
        t2 = t1 + (d * 0.75 + w * 0.45) / np.linalg.norm(d * 0.75 + w * 0.45) * 0.032
        J[f"thumb.01.{s}"] = (tuple(t0), tuple(t1)); J[f"thumb.02.{s}"] = (tuple(t1), tuple(t2))
    return J

PARENT = {"spine": "pelvis", "chest": "spine", "neck": "chest", "head": "neck"}
for s in "LR":
    PARENT.update({f"clav.{s}": "chest", f"arm.{s}": f"clav.{s}", f"fore.{s}": f"arm.{s}", f"hand.{s}": f"fore.{s}",
                   f"thigh.{s}": "pelvis", f"shin.{s}": f"thigh.{s}", f"foot.{s}": f"shin.{s}"})
    for fn in ("index", "middle", "ring", "pinky", "thumb"):
        PARENT[f"{fn}.01.{s}"] = f"hand.{s}"; PARENT[f"{fn}.02.{s}"] = f"{fn}.01.{s}"

def b2g(p): return np.array([p[0], p[2], -p[1]], float)       # Blender → glTF
def g2b(P): return np.c_[P[:, 0], -P[:, 2], P[:, 1]]

# ------------------------------------------------------------------ algebra
def from_to(a, b):
    a = a / np.linalg.norm(a); b = b / np.linalg.norm(b); c = np.cross(a, b); d = a.dot(b)
    if d < -0.999999:
        ax = np.cross([1, 0, 0], a)
        if np.linalg.norm(ax) < 1e-6: ax = np.cross([0, 0, 1], a)
        ax /= np.linalg.norm(ax); return np.array([*ax, 0.0])
    q = np.array([*c, 1 + d]); return q / np.linalg.norm(q)
def q2m(q):
    x, y, z, w = q
    return np.array([[1-2*(y*y+z*z), 2*(x*y-z*w), 2*(x*z+y*w)], [2*(x*y+z*w), 1-2*(x*x+z*z), 2*(y*z-x*w)], [2*(x*z-y*w), 2*(y*z+x*w), 1-2*(x*x+y*y)]])
def m2q(m):
    t = m.trace()
    if t > 0: s = math.sqrt(t + 1) * 2; return np.array([(m[2,1]-m[1,2])/s, (m[0,2]-m[2,0])/s, (m[1,0]-m[0,1])/s, 0.25*s])
    i = int(np.argmax([m[0,0], m[1,1], m[2,2]]))
    if i == 0: s = math.sqrt(1+m[0,0]-m[1,1]-m[2,2])*2; return np.array([0.25*s, (m[0,1]+m[1,0])/s, (m[0,2]+m[2,0])/s, (m[2,1]-m[1,2])/s])
    if i == 1: s = math.sqrt(1+m[1,1]-m[0,0]-m[2,2])*2; return np.array([(m[0,1]+m[1,0])/s, 0.25*s, (m[1,2]+m[2,1])/s, (m[0,2]-m[2,0])/s])
    s = math.sqrt(1+m[2,2]-m[0,0]-m[1,1])*2; return np.array([(m[0,2]+m[2,0])/s, (m[1,2]+m[2,1])/s, 0.25*s, (m[1,0]-m[0,1])/s])

def seg_dist(P, a, b):
    ab = b - a; t = np.clip(((P - a) @ ab) / max(1e-12, ab @ ab), 0, 1)
    return np.linalg.norm(P - (a + t[:, None] * ab), axis=1)

# ------------------------------------------------------------------ glb
CT = {5126: np.float32, 5123: np.uint16, 5125: np.uint32, 5121: np.uint8}
NC = {'SCALAR': 1, 'VEC2': 2, 'VEC3': 3, 'VEC4': 4, 'MAT4': 16}
def read(g, i):
    a = g.accessors[i]; bv = g.bufferViews[a.bufferView]; dt = CT[a.componentType]; n = NC[a.type]
    off = (bv.byteOffset or 0) + (a.byteOffset or 0)
    arr = np.frombuffer(g.binary_blob(), dtype=dt, count=a.count * n, offset=off).reshape(a.count, n)
    if a.normalized and dt != np.float32: arr = arr / np.iinfo(dt).max
    return arr

def append(g, blob, data, ctype, typ, normalized=False, target=None):
    pad = (-len(blob)) % 4; blob += b"\0" * pad
    raw = data.tobytes(); off = len(blob); blob += raw
    g.bufferViews.append(BufferView(buffer=0, byteOffset=off, byteLength=len(raw), target=target))
    acc = Accessor(bufferView=len(g.bufferViews) - 1, componentType=ctype, count=len(data), type=typ, normalized=normalized or None)
    if ctype == 5126 and typ != 'MAT4':
        acc.min = data.min(0).tolist(); acc.max = data.max(0).tolist()
    g.accessors.append(acc)
    return blob, len(g.accessors) - 1

def main(src, dst):
    g = GLTF2().load(src)
    J = joints()
    names = {n.name: i for i, n in enumerate(g.nodes)}
    skin = g.skins[0]
    jnames = [g.nodes[j].name for j in skin.joints]

    # --- ossa: rotazione = +Y lungo l'osso, rollio minimo (spazio glTF)
    world = {}
    for n in J:
        h, t = b2g(J[n][0]), b2g(J[n][1])
        R = q2m(from_to(np.array([0, 1.0, 0]), t - h))
        M = np.eye(4); M[:3, :3] = R; M[:3, 3] = h; world[n] = M
    for n in J:
        node = g.nodes[names[n]]
        par = PARENT.get(n)
        L = np.linalg.inv(world[par]) @ world[n] if par else world[n]
        node.translation = L[:3, 3].tolist(); node.rotation = m2q(L[:3, :3]).tolist(); node.scale = None
    ibm = np.stack([np.linalg.inv(world[n]) for n in jnames]).transpose(0, 2, 1).reshape(-1, 16).astype(np.float32)

    # --- pesi
    prim = g.meshes[0].primitives[0]
    Pg = read(g, prim.attributes.POSITION).astype(float); P = g2b(Pg)
    bones = list(J.keys()); idx = {n: jnames.index(n) for n in bones}
    seg = {n: (np.array(J[n][0], float), np.array(J[n][1], float)) for n in bones}
    FING = [n for n in bones if n.split(".")[0] in ("thumb", "index", "middle", "ring", "pinky")]
    nv = len(P); Wt = np.zeros((nv, len(jnames)))

    done = np.zeros(nv, bool)
    # zaino e antenna: al petto, rigidi
    pack = ((P[:, 1] > 0.07) & (np.abs(P[:, 0]) < 0.27) & (P[:, 2] > 0.95)) | ((P[:, 2] > 1.655) & (np.abs(P[:, 0]) > 0.15))
    Wt[pack, idx["chest"]] = 1; done |= pack
    # mani: oltre il polso (lungo la direzione dell'avambraccio) e vicine al segmento della mano
    for s in "LR":
        el, wr = seg[f"fore.{s}"]; d = (wr - el) / np.linalg.norm(wr - el)
        ht = seg[f"hand.{s}"][1]; tip = ht + (ht - wr) * 1.1
        near = seg_dist(P, wr, tip) < 0.085
        beyond = (P - wr) @ d > -0.012
        m = near & beyond & ~done
        cand = [f"hand.{s}"] + [n for n in FING if n.endswith(s)]
        D = np.stack([seg_dist(P[m], *seg[n]) for n in cand], 1)
        best = np.argmin(D, 1)
        rows = np.where(m)[0]
        for k, n in enumerate(cand): Wt[rows[best == k], idx[n]] = 1
        # polso: sfumatura di 2,5 cm fra mano e avambraccio (niente strappo al polso)
        t = (P[rows] - wr) @ d
        blend = np.clip((t + 0.012) / 0.037, 0, 1)
        hand_rows = rows[best == 0]; tb = blend[best == 0]
        Wt[hand_rows, idx[f"hand.{s}"]] = tb; Wt[hand_rows, idx[f"fore.{s}"]] = 1 - tb
        done |= m
    # resto: gaussiano dai 3 ossi più vicini
    SIG = 0.075
    rest = np.where(~done)[0]; Pr = P[rest]
    main_b = [n for n in bones if n not in FING]
    D = np.stack([seg_dist(Pr, *seg[n]) for n in main_b], 1)
    for k, n in enumerate(main_b):
        if n.endswith(".L"): D[Pr[:, 0] > 0.06, k] = 9
        if n.endswith(".R"): D[Pr[:, 0] < -0.06, k] = 9
    G = np.exp(-(D / SIG) ** 2)
    order = np.argsort(-G, 1)[:, :3]
    for r in range(3):
        k = order[:, r]; w = G[np.arange(len(rest)), k]
        far = G.max(1) < 0.02
        w = np.where(far & (r == 0), 1.0, np.where(far, 0.0, w))
        np.add.at(Wt, (rest, [idx[main_b[j]] for j in k]), w)
    # normalizza e tieni 4 influenze
    top = np.argsort(-Wt, 1)[:, :4]
    tw = np.take_along_axis(Wt, top, 1); tw = tw / np.maximum(tw.sum(1, keepdims=True), 1e-9)
    tw[tw < 1e-4] = 0; tw = tw / tw.sum(1, keepdims=True)
    joints_arr = top.astype(np.uint16); weights_arr = tw.astype(np.float32)

    blob = bytearray(g.binary_blob())
    blob, ja = append(g, blob, joints_arr, 5123, 'VEC4', target=34962)
    blob, wa = append(g, blob, weights_arr, 5126, 'VEC4', target=34962)
    blob, ia = append(g, blob, ibm, 5126, 'MAT4')
    for p in g.meshes[0].primitives:
        p.attributes.JOINTS_0 = ja; p.attributes.WEIGHTS_0 = wa
    skin.inverseBindMatrices = ia
    g.buffers[0].byteLength = len(blob)
    g.set_binary_blob(bytes(blob))
    g.save_binary(dst)
    print("scritto", dst, "vertici", nv, "mani", int(sum(Wt[:, idx[n]].astype(bool).sum() for n in FING)))

if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2])
