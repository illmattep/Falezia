"""
FALEZIA · semplificazione + nuovo atlante UV + bake delle texture dall'alta definizione.

Perché serve: le UV di Meshy sono spezzettate in migliaia di isole; semplificare la mesh con
le UV originali le rovina (texture "a macchie"). Quindi:
  1. la geometria si semplifica da sola (solo posizioni) con gltfpack
  2. xatlas crea un atlante UV nuovo e pulito per la mesh leggera
  3. ogni texel dell'atlante prende colore, metallo/rugosità e normale dal punto più vicino
     della mesh originale (normale ricalcolata nello spazio tangente della mesh leggera)
"""
import math, os, struct, subprocess, tempfile
import numpy as np
from scipy.spatial import cKDTree
from scipy import ndimage

GLTFPACK = "/home/claude/tools/meshoptimizer/gltfpack"
ATLAS = "/home/claude/tools/atlas/atlas"


# ------------------------------------------------------------------ semplificazione
def simplify(parts, target, tmpdir):
    """parts: lista di (pos float32 (n,3), idx int (m,3)). Ritorna la lista semplificata."""
    import json
    from glbinfo import load, acc
    total = sum(len(i) for _, i in parts)
    ratio = min(1.0, target / total)
    # glb di sole posizioni, un nodo per pezzo
    bin_ = bytearray(); views = []; accs = []; nodes = []; meshes = []

    def put(arr, kind, target_):
        while len(bin_) % 4: bin_.extend(b"\0")
        views.append({"buffer": 0, "byteOffset": len(bin_), "byteLength": arr.nbytes, "target": target_})
        bin_.extend(arr.tobytes())
        a = {"bufferView": len(views) - 1, "componentType": 5126 if arr.dtype == np.float32 else 5125,
             "count": int(arr.shape[0]), "type": kind}
        if kind == "VEC3":
            a["min"] = arr.min(0).tolist(); a["max"] = arr.max(0).tolist()
        accs.append(a); return len(accs) - 1

    for k, (p, i) in enumerate(parts):
        pa = put(np.ascontiguousarray(p, np.float32), "VEC3", 34962)
        ia = put(np.ascontiguousarray(i.ravel(), np.uint32), "SCALAR", 34963)
        meshes.append({"primitives": [{"attributes": {"POSITION": pa}, "indices": ia}]})
        nodes.append({"name": f"p{k}", "mesh": k})
    js = {"asset": {"version": "2.0"}, "scene": 0, "scenes": [{"nodes": list(range(len(nodes)))}],
          "nodes": nodes, "meshes": meshes, "accessors": accs, "bufferViews": views,
          "buffers": [{"byteLength": len(bin_)}]}
    src = os.path.join(tmpdir, "simp_in.glb"); dst = os.path.join(tmpdir, "simp_out.glb")
    write_glb(src, js, bytes(bin_))
    out = None
    for extra in ([], ["-sa"]):   # prima rispettando la forma, poi forzando se non basta
        r = subprocess.run([GLTFPACK, "-i", src, "-o", dst, "-si", f"{ratio:.6f}", "-se", "0.5",
                            "-kn", "-noq"] + extra, capture_output=True, text=True)
        if r.returncode != 0:
            raise RuntimeError(r.stderr)
        js2, b2 = load(dst)
        res = {}
        for n in js2["nodes"]:
            if "mesh" in n and n.get("name", "").startswith("p"):
                pr = js2["meshes"][n["mesh"]]["primitives"][0]
                P = acc(js2, b2, pr["attributes"]["POSITION"]).astype(np.float32)
                if "translation" in n or "scale" in n or "rotation" in n:
                    raise RuntimeError("gltfpack ha messo una trasformata sul nodo")
                res[int(n["name"][1:])] = (P, acc(js2, b2, pr["indices"]).reshape(-1, 3).astype(np.int64))
        out = [res[k] for k in range(len(parts))]
        if sum(len(i) for _, i in out) <= target * 1.05:
            break
    return out


def write_glb(path, js, bin_):
    import json
    j = json.dumps(js, separators=(",", ":")).encode()
    while len(j) % 4: j += b" "
    bin_ = bytes(bin_)
    while len(bin_) % 4: bin_ += b"\0"
    with open(path, "wb") as f:
        f.write(struct.pack("<III", 0x46546C67, 2, 28 + len(j) + len(bin_)))
        f.write(struct.pack("<II", len(j), 0x4E4F534A)); f.write(j)
        f.write(struct.pack("<II", len(bin_), 0x004E4942)); f.write(bin_)


# ------------------------------------------------------------------ normali con spigoli vivi
def crease_normals(P, I, angle=40.0):
    """Normali per angolo: liscia sotto `angle`, spigolo vivo sopra. Duplica i vertici dove serve."""
    fn = np.cross(P[I[:, 1]] - P[I[:, 0]], P[I[:, 2]] - P[I[:, 0]])
    area = np.linalg.norm(fn, axis=1, keepdims=True)
    fnu = fn / np.maximum(area, 1e-20)
    cos_t = np.cos(np.radians(angle))
    # facce attorno a ogni vertice
    corner_v = I.ravel(); corner_f = np.repeat(np.arange(len(I)), 3)
    order = np.argsort(corner_v, kind="stable")
    cv = corner_v[order]; cf = corner_f[order]
    starts = np.searchsorted(cv, np.arange(len(P))); ends = np.searchsorted(cv, np.arange(len(P)), side="right")
    corner_n = np.zeros((len(I) * 3, 3))
    for v in range(len(P)):
        fs = cf[starts[v]:ends[v]]
        if len(fs) == 0: continue
        nn = fnu[fs]; w = fn[fs]
        sim = nn @ nn.T >= cos_t
        acc_ = sim.astype(np.float64) @ w
        acc_ /= np.maximum(np.linalg.norm(acc_, axis=1, keepdims=True), 1e-20)
        # scrivi sugli angoli (faccia fs[k], posizione del vertice v in quella faccia)
        for k, f in enumerate(fs):
            slot = int(np.nonzero(I[f] == v)[0][0])
            corner_n[f * 3 + slot] = acc_[k]
    # vertici unici per (posizione, normale quantizzata)
    key = np.concatenate([I.ravel()[:, None], np.round(corner_n * 1000).astype(np.int64)], 1)
    uniq, inv = np.unique(key, axis=0, return_inverse=True)
    inv = inv.ravel()
    newP = P[uniq[:, 0]]
    newN = np.zeros((len(uniq), 3)); np.add.at(newN, inv, corner_n)
    newN /= np.maximum(np.linalg.norm(newN, axis=1, keepdims=True), 1e-20)
    return newP.astype(np.float32), newN.astype(np.float32), inv.reshape(-1, 3)


# ------------------------------------------------------------------ atlante
def make_atlas(meshes, resolution, padding, tmpdir):
    """meshes: lista di (P, N, I). Ritorna lista di (P, N, UV, I) con vertici rifatti dall'atlante.
    La densità dei texel si cerca per tentativi, finché l'atlante riempie quasi tutta la texture."""
    src = os.path.join(tmpdir, "atlas_in.bin"); dst = os.path.join(tmpdir, "atlas_out.bin")
    with open(src, "wb") as f:
        f.write(struct.pack("<I", len(meshes)))
        for P, N, I in meshes:
            f.write(struct.pack("<II", len(P), I.size))
            f.write(np.ascontiguousarray(P, np.float32).tobytes())
            f.write(np.ascontiguousarray(N, np.float32).tobytes())
            f.write(np.ascontiguousarray(I.ravel(), np.uint32).tobytes())
    area = sum(0.5 * np.linalg.norm(np.cross(P[I[:, 1]] - P[I[:, 0]], P[I[:, 2]] - P[I[:, 0]]), axis=1).sum()
               for P, _, I in meshes)
    tpu = resolution * 0.55 / math.sqrt(area)
    best = None
    for _ in range(8):
        r = subprocess.run([ATLAS, src, dst, "0", str(padding), "1", f"{tpu:.4f}"], capture_output=True, text=True)
        if r.returncode != 0:
            raise RuntimeError(r.stderr)
        info = r.stderr.strip()
        w_, h_ = [int(x) for x in info.split()[1].split("x")]
        count = int(info.split("count")[1].split()[0])
        side = max(w_, h_)
        if count == 1 and side <= resolution:
            best = (tpu, info)
            if side > resolution * 0.94:
                break
            tpu *= min(1.25, (resolution * 0.97) / side)
        else:
            tpu *= 0.93 * resolution / max(side, 1)
    if best is None:
        raise RuntimeError("atlante impossibile: " + info)
    tpu, info = best
    r = subprocess.run([ATLAS, src, dst, "0", str(padding), "1", f"{tpu:.4f}"], capture_output=True, text=True)
    out = []
    with open(dst, "rb") as f:
        mc, aw, ah = struct.unpack("<III", f.read(12))
        for k in range(mc):
            nv, ni = struct.unpack("<II", f.read(8))
            xref = np.frombuffer(f.read(4 * nv), np.uint32).astype(np.int64)
            uv = np.frombuffer(f.read(8 * nv), np.float32).reshape(nv, 2).copy()
            uv *= np.array([aw / resolution, ah / resolution], np.float32)   # pixel dell'atlante → texture intera
            idx = np.frombuffer(f.read(4 * ni), np.uint32).astype(np.int64).reshape(-1, 3)
            P, N, _ = meshes[k]
            out.append((P[xref], N[xref], uv, idx))
    return out, info


# ------------------------------------------------------------------ tangenti (convenzione glTF)
def tangents(P, N, UV, I):
    p0, p1, p2 = P[I[:, 0]], P[I[:, 1]], P[I[:, 2]]
    # glTF: v cresce verso il basso; la bitangente segue la v "alla Blender" (1 - v)
    u0, u1, u2 = UV[I[:, 0]], UV[I[:, 1]], UV[I[:, 2]]
    du1 = u1[:, 0] - u0[:, 0]; du2 = u2[:, 0] - u0[:, 0]
    dv1 = -(u1[:, 1] - u0[:, 1]); dv2 = -(u2[:, 1] - u0[:, 1])
    e1 = p1 - p0; e2 = p2 - p0
    r = du1 * dv2 - du2 * dv1
    r = np.where(np.abs(r) < 1e-20, 1e-20, r)
    t = (e1 * dv2[:, None] - e2 * dv1[:, None]) / r[:, None]
    bt = (e2 * du1[:, None] - e1 * du2[:, None]) / r[:, None]
    T = np.zeros_like(P, dtype=np.float64); B = np.zeros_like(P, dtype=np.float64)
    for k in range(3):
        np.add.at(T, I[:, k], t); np.add.at(B, I[:, k], bt)
    N = N.astype(np.float64)
    T = T - N * np.sum(T * N, 1, keepdims=True)
    bad = np.linalg.norm(T, axis=1) < 1e-12
    if bad.any():   # tangente qualunque perpendicolare alla normale
        alt = np.where(np.abs(N[bad, 0:1]) < 0.9, [[1.0, 0, 0]], [[0, 1.0, 0]])
        T[bad] = alt - N[bad] * np.sum(alt * N[bad], 1, keepdims=True)
    T /= np.linalg.norm(T, axis=1, keepdims=True)
    w = np.where(np.sum(np.cross(N, T) * B, 1) < 0.0, -1.0, 1.0)
    return np.concatenate([T, w[:, None]], 1).astype(np.float32)


# ------------------------------------------------------------------ bake
def bilinear(img, uv):
    """img (H,W,C) uint8, uv (n,2) glTF (wrap). Ritorna float32 (n,C)."""
    H, W = img.shape[:2]
    x = (uv[:, 0] % 1.0) * W - 0.5; y = (uv[:, 1] % 1.0) * H - 0.5
    x0 = np.floor(x).astype(np.int64); y0 = np.floor(y).astype(np.int64)
    fx = (x - x0)[:, None]; fy = (y - y0)[:, None]
    x0 %= W; y0 %= H; x1 = (x0 + 1) % W; y1 = (y0 + 1) % H
    a = img[y0, x0].astype(np.float32); b = img[y0, x1].astype(np.float32)
    c = img[y1, x0].astype(np.float32); d = img[y1, x1].astype(np.float32)
    return (a * (1 - fx) + b * fx) * (1 - fy) + (c * (1 - fx) + d * fx) * fy


def closest_on_tris(p, a, b, c):
    """Punto più vicino a p su ogni triangolo (a,b,c), vettoriale (Ericson). Ritorna baricentriche (u,v,w)."""
    ab = b - a; ac = c - a; ap = p - a
    d1 = np.sum(ab * ap, -1); d2 = np.sum(ac * ap, -1)
    bp = p - b; d3 = np.sum(ab * bp, -1); d4 = np.sum(ac * bp, -1)
    cp = p - c; d5 = np.sum(ab * cp, -1); d6 = np.sum(ac * cp, -1)
    va = d3 * d6 - d5 * d4; vb = d5 * d2 - d1 * d6; vc = d1 * d4 - d3 * d2
    den = va + vb + vc
    den = np.where(np.abs(den) < 1e-30, 1e-30, den)
    v = vb / den; w = vc / den; u = 1 - v - w
    bary = np.stack([u, v, w], -1)
    # regioni esterne
    def setb(mask, uu, vv, ww):
        bary[mask] = np.stack([uu[mask], vv[mask], ww[mask]], -1)
    one = np.ones_like(d1); zero = np.zeros_like(d1)
    m = (d1 <= 0) & (d2 <= 0); setb(m, one, zero, zero)
    m2 = (d3 >= 0) & (d4 <= d3) & ~m; setb(m2, zero, one, zero)
    m3 = (d6 >= 0) & (d5 <= d6) & ~m & ~m2; setb(m3, zero, zero, one)
    done = m | m2 | m3
    t = d1 / np.where(np.abs(d1 - d3) < 1e-30, 1e-30, d1 - d3)
    m4 = (vc <= 0) & (d1 >= 0) & (d3 <= 0) & ~done; setb(m4, 1 - t, t, zero); done |= m4
    t = d2 / np.where(np.abs(d2 - d6) < 1e-30, 1e-30, d2 - d6)
    m5 = (vb <= 0) & (d2 >= 0) & (d6 <= 0) & ~done; setb(m5, 1 - t, zero, t); done |= m5
    t = (d4 - d3) / np.where(np.abs((d4 - d3) + (d5 - d6)) < 1e-30, 1e-30, (d4 - d3) + (d5 - d6))
    m6 = (va <= 0) & ((d4 - d3) >= 0) & ((d5 - d6) >= 0) & ~done; setb(m6, zero, 1 - t, t)
    return bary


class HighSource:
    """La mesh ad alta definizione (o un suo pezzo) con le sue texture, pronta per le ricerche."""
    def __init__(self, P, N, UV, I, tex_bc, tex_mr, tex_n):
        self.P = P.astype(np.float64); self.N = N.astype(np.float64); self.UV = UV; self.I = I
        self.cen = self.P[I].mean(1)
        self.tree = cKDTree(self.cen)
        self.fn = np.cross(self.P[I[:, 1]] - self.P[I[:, 0]], self.P[I[:, 2]] - self.P[I[:, 0]])
        self.fn /= np.maximum(np.linalg.norm(self.fn, axis=1, keepdims=True), 1e-20)
        self.TW = tangents(self.P, self.N, UV, I).astype(np.float64)
        self.bc, self.mr, self.nm = tex_bc, tex_mr, tex_n

    def sample(self, p, n, k=12):
        """p, n (m,3) punti e normali della mesh leggera → (colore, mr, normale nel mondo)."""
        _, cand = self.tree.query(p, k=k)
        tri = self.I[cand]                                     # (m,k,3)
        a = self.P[tri[..., 0]]; b = self.P[tri[..., 1]]; c = self.P[tri[..., 2]]
        bary = closest_on_tris(p[:, None, :], a, b, c)         # (m,k,3)
        q = a * bary[..., :1] + b * bary[..., 1:2] + c * bary[..., 2:3]
        d = np.linalg.norm(q - p[:, None, :], axis=-1)
        facing = np.sum(self.fn[cand] * n[:, None, :], -1)
        d = d + np.where(facing < -0.3, 1.0, 0.0)               # niente lato opposto delle lamiere sottili
        best = np.argmin(d, 1); r = np.arange(len(p))
        t = tri[r, best]; w = bary[r, best]
        uv = (self.UV[t[:, 0]] * w[:, :1] + self.UV[t[:, 1]] * w[:, 1:2] + self.UV[t[:, 2]] * w[:, 2:3])
        col = bilinear(self.bc, uv); mr = bilinear(self.mr, uv)
        nt = bilinear(self.nm, uv) / 127.5 - 1.0
        Nh = self.N[t[:, 0]] * w[:, :1] + self.N[t[:, 1]] * w[:, 1:2] + self.N[t[:, 2]] * w[:, 2:3]
        Th = self.TW[t[:, 0], :3] * w[:, :1] + self.TW[t[:, 1], :3] * w[:, 1:2] + self.TW[t[:, 2], :3] * w[:, 2:3]
        sgn = self.TW[t[:, 0], 3:4]
        Nh /= np.maximum(np.linalg.norm(Nh, axis=1, keepdims=True), 1e-20)
        Th = Th - Nh * np.sum(Th * Nh, 1, keepdims=True); Th /= np.maximum(np.linalg.norm(Th, axis=1, keepdims=True), 1e-20)
        Bh = np.cross(Nh, Th) * sgn
        wn = Th * nt[:, :1] + Bh * nt[:, 1:2] + Nh * nt[:, 2:3]
        wn /= np.maximum(np.linalg.norm(wn, axis=1, keepdims=True), 1e-20)
        return col, mr, wn, np.min(d, 1)


def bake(lows, sources, size, chunk=200000):
    """lows: lista di (P, N, UV, I, TAN) con la sorgente omonima in `sources`.
    Ritorna immagini (colore, mr, normale) size×size uint8 e la percentuale di texel coperti."""
    # rasterizza tutti i triangoli nell'atlante
    px_p, px_n, px_t, px_xy, px_src = [], [], [], [], []
    for li, (P, N, UV, I, TAN) in enumerate(lows):
        uvp = UV * size - 0.5
        for f in range(len(I)):
            tri = I[f]; q = uvp[tri]
            x0 = int(np.floor(q[:, 0].min())); x1 = int(np.ceil(q[:, 0].max()))
            y0 = int(np.floor(q[:, 1].min())); y1 = int(np.ceil(q[:, 1].max()))
            xs, ys = np.meshgrid(np.arange(max(x0, 0), min(x1, size - 1) + 1), np.arange(max(y0, 0), min(y1, size - 1) + 1))
            xs = xs.ravel(); ys = ys.ravel()
            if len(xs) == 0: continue
            v0 = q[1] - q[0]; v1 = q[2] - q[0]
            den = v0[0] * v1[1] - v1[0] * v0[1]
            if abs(den) < 1e-12: continue
            dx = xs - q[0, 0]; dy = ys - q[0, 1]
            b1 = (dx * v1[1] - v1[0] * dy) / den; b2 = (v0[0] * dy - dx * v0[1]) / den; b0 = 1 - b1 - b2
            eps = -0.02   # un filo di margine: i bordi si riempiono comunque con la dilatazione
            m = (b0 >= eps) & (b1 >= eps) & (b2 >= eps)
            if not m.any(): continue
            bb = np.stack([b0[m], b1[m], b2[m]], 1)
            px_p.append(bb @ P[tri]); px_n.append(bb @ N[tri]); px_t.append(bb @ TAN[tri])
            px_xy.append(np.stack([ys[m], xs[m]], 1)); px_src.append(np.full(m.sum(), li))
    p = np.concatenate(px_p); n = np.concatenate(px_n); tan = np.concatenate(px_t)
    xy = np.concatenate(px_xy); src = np.concatenate(px_src)
    n /= np.maximum(np.linalg.norm(n, axis=1, keepdims=True), 1e-20)

    col = np.zeros((size, size, 3), np.float32); mr = np.zeros((size, size, 3), np.float32)
    nrm = np.zeros((size, size, 3), np.float32); filled = np.zeros((size, size), bool)
    dist = []
    for li in range(len(lows)):
        sel = np.nonzero(src == li)[0]
        for s0 in range(0, len(sel), chunk):
            ix = sel[s0:s0 + chunk]
            c, m_, wn, d = sources[li].sample(p[ix], n[ix])
            dist.append(d)
            T = tan[ix, :3].astype(np.float64); sg = np.where(tan[ix, 3:4] < 0, -1.0, 1.0)
            N_ = n[ix]
            T = T - N_ * np.sum(T * N_, 1, keepdims=True); T /= np.maximum(np.linalg.norm(T, axis=1, keepdims=True), 1e-20)
            B = np.cross(N_, T) * sg
            ts = np.stack([np.sum(wn * T, 1), np.sum(wn * B, 1), np.sum(wn * N_, 1)], 1)
            ts[:, 2] = np.maximum(ts[:, 2], 0.05)
            ts /= np.linalg.norm(ts, axis=1, keepdims=True)
            yy, xx = xy[ix, 0], xy[ix, 1]
            col[yy, xx] = c; mr[yy, xx] = m_; nrm[yy, xx] = (ts + 1.0) * 127.5; filled[yy, xx] = True
    coverage = filled.mean()
    # dilatazione: ogni texel vuoto prende il texel pieno più vicino (niente aloni nei mipmap)
    _, (iy, ix2) = ndimage.distance_transform_edt(~filled, return_indices=True)
    out = []
    for img in (col, mr, nrm):
        out.append(np.clip(img[iy, ix2] + 0.5, 0, 255).astype(np.uint8))
    d = np.concatenate(dist)
    return out, float(coverage), (float(np.median(d)), float(np.percentile(d, 99)))
