"""Texture stilizzate procedurali per Calanera (stile Valorant/Ascent).
Tutto periodico (tileabile). Ogni materiale restituisce: albedo RGB, altezza [0,1], ruvidità.
"""
import numpy as np, cv2

def rng(seed): return np.random.default_rng(seed)

def wrap_blur(x, s):
    if s <= 0: return x
    pad = int(s * 3) + 1
    big = cv2.copyMakeBorder(x, pad, pad, pad, pad, cv2.BORDER_WRAP)
    return cv2.GaussianBlur(big, (0, 0), s)[pad:-pad, pad:-pad]

def norm01(x, lo=0.5, hi=99.5):
    a, b = np.percentile(x, lo), np.percentile(x, hi)
    return np.clip((x - a) / max(b - a, 1e-6), 0, 1).astype(np.float32)

def _periodic_up(small, size):
    """Ingrandisce una griglia periodica senza giunte (bordo avvolto, poi ritaglio)."""
    n = small.shape[0]; p = 3
    big = cv2.copyMakeBorder(small, p, p, p, p, cv2.BORDER_WRAP)
    f = size / n
    up = cv2.resize(big, (int(round((n + 2 * p) * f)),) * 2, interpolation=cv2.INTER_CUBIC)
    o = int(round(p * f))
    return up[o:o + size, o:o + size]

def blurred_noise(size, r, sigma):
    """Rumore bianco sfocato periodico; per sfocature larghe si lavora in piccolo e si ingrandisce."""
    if sigma <= 6:
        return wrap_blur(r.random((size, size)).astype(np.float32), sigma)
    n = max(16, int(size * 3.0 / sigma))
    n = min(n, size)
    small = r.random((n, n)).astype(np.float32)
    small = wrap_blur(small, sigma * n / size)
    return _periodic_up(small, size)

def fbm(size, seed, scales=(64, 32, 16, 8), weights=None):
    """Rumore periodico: somma di rumori bianchi sfocati in modo periodico."""
    r = rng(seed); out = np.zeros((size, size), np.float32)
    weights = weights or [1.0 / (i + 1) for i in range(len(scales))]
    for s, w in zip(scales, weights):
        out += w * norm01(blurred_noise(size, r, s * size / 1024))
    return norm01(out)

def aniso(size, seed, sx, sy):
    """Rumore allungato (pennellate): sfocatura diversa sui due assi, periodica."""
    r = rng(seed)
    k = max(1, int(min(sx, sy) // 4))
    if k > 1:
        small = aniso(size // k, seed, sx / k, sy / k)
        return norm01(_periodic_up(small, size))
    x = r.random((size, size)).astype(np.float32)
    pad = int(max(sx, sy) * 3) + 1
    big = cv2.copyMakeBorder(x, pad, pad, pad, pad, cv2.BORDER_WRAP)
    b = cv2.GaussianBlur(big, (0, 0), sigmaX=sx, sigmaY=sy)[pad:-pad, pad:-pad]
    return norm01(b)

def smooth(e0, e1, x):
    t = np.clip((x - e0) / (e1 - e0), 0, 1); return t * t * (3 - 2 * t)

def hex2(c): c = c.lstrip('#'); return np.array([int(c[i:i+2], 16) for i in (0, 2, 4)], np.float32) / 255

def jitter_color(base, r, hv=0.008, sv=0.06, vv=0.07):
    """Variazione di colore per pezzo (HSV)."""
    hsv = cv2.cvtColor(base.reshape(1, 1, 3).astype(np.float32), cv2.COLOR_RGB2HSV)[0, 0]
    hsv[0] = (hsv[0] + r.normal(0, hv * 360)) % 360
    hsv[1] = np.clip(hsv[1] * (1 + r.normal(0, sv)), 0, 1)
    hsv[2] = np.clip(hsv[2] * (1 + r.normal(0, vv)), 0, 1)
    return cv2.cvtColor(hsv.reshape(1, 1, 3), cv2.COLOR_HSV2RGB)[0, 0]

# ------------------------------------------------------------------ disposizioni
def bricks_layout(size, rows, cols, seed, mortar=0.06, jit=0.03, stagger=0.5, wvar=0.0):
    """Mattoni a corsi sfalsati. Ritorna id pezzo, distanza dal bordo (in frazione di altezza corso), coord locali."""
    r = rng(seed)
    y = (np.arange(size) + 0.5) / size * rows
    x = (np.arange(size) + 0.5) / size * cols
    Y, X = np.meshgrid(y, x, indexing='ij')
    row = np.floor(Y).astype(int)
    off = (row % 2) * stagger + r.random(rows)[row % rows] * 0.0
    Xs = X + off
    col = np.floor(Xs).astype(int) % cols
    fy = Y - np.floor(Y); fx = Xs - np.floor(Xs)
    pid = row * 1000 + col
    # bordi irregolari: ogni lato del mattone rientra di una quantità casuale
    rr = rng(seed + 7)
    ins = rr.random((rows, cols, 4)) * jit
    l, rgt, t, b = (ins[row % rows, col, k] for k in range(4))
    aspect = cols / rows  # larghezza di un mattone in unità di altezza corso
    dx = np.minimum(fx - l, (1 - rgt) - fx) / aspect * (rows / rows) * (1.0 / 1.0)
    dxu = np.minimum(fx - l, (1 - rgt) - fx) * (size / cols)
    dyu = np.minimum(fy - t, (1 - b) - fy) * (size / rows)
    d = np.minimum(dxu, dyu)  # distanza in pixel dal bordo del mattone
    return pid, d, fx, fy

def voronoi(size, n, seed, relax=1):
    """Voronoi periodico. Ritorna id cella, distanza dal bordo (px, approssimata F2-F1), distanza dal centro."""
    r = rng(seed); pts = r.random((n, 2))
    for _ in range(relax):   # rilassamento di Lloyd grossolano per celle regolari
        small = 128
        yy, xx = np.meshgrid((np.arange(small) + .5) / small, (np.arange(small) + .5) / small, indexing='ij')
        best = np.full((small, small), 9.0); idx = np.zeros((small, small), int)
        for i, (py, px) in enumerate(pts):
            dy = np.abs(yy - py); dy = np.minimum(dy, 1 - dy); dx = np.abs(xx - px); dx = np.minimum(dx, 1 - dx)
            d = dy * dy + dx * dx; m = d < best; best[m] = d[m]; idx[m] = i
        for i in range(n):
            m = idx == i
            if m.any():
                # media circolare per rispettare la periodicità
                ay = np.angle(np.exp(2j * np.pi * yy[m]).mean()) / (2 * np.pi) % 1
                ax = np.angle(np.exp(2j * np.pi * xx[m]).mean()) / (2 * np.pi) % 1
                pts[i] = (ay, ax)
    yy, xx = np.meshgrid((np.arange(size) + .5) / size, (np.arange(size) + .5) / size, indexing='ij')
    d1 = np.full((size, size), 9.0, np.float32); d2 = d1.copy(); idx = np.zeros((size, size), np.int32)
    for i, (py, px) in enumerate(pts):
        dy = np.abs(yy - py); dy = np.minimum(dy, 1 - dy); dx = np.abs(xx - px); dx = np.minimum(dx, 1 - dx)
        d = np.sqrt(dy * dy + dx * dx).astype(np.float32)
        m1 = d < d1
        d2 = np.where(m1, d1, np.minimum(d2, d)); idx = np.where(m1, i, idx); d1 = np.where(m1, d, d1)
    edge = (d2 - d1) * 0.5 * size
    return idx, edge, d1 * size

# ------------------------------------------------------------------ pittura comune
def paint_pieces(pid, edge_px, base_cols, seed, bevel=6.0, gap=2.0, size=1024,
                 dome=0.35, chip=0.25, hl=0.22, cav=0.45, stroke=0.025, grout=None, grout_h=0.15,
                 grad_top=0.06, fy=None):
    """Pezzi (mattoni, pietre) con smusso, luce dipinta sugli spigoli, fughe scure, scheggiature."""
    r = rng(seed)
    ids = np.unique(pid)
    lut_col = {}; lut_h = {}
    for i in ids:
        c = base_cols[r.integers(len(base_cols))]
        lut_col[i] = jitter_color(c, r); lut_h[i] = r.uniform(0.85, 1.0)
    # mappa id -> colore/altezza
    flat = pid.ravel(); uniq, inv = np.unique(flat, return_inverse=True)
    cols = np.array([lut_col[u] for u in uniq], np.float32); hs = np.array([lut_h[u] for u in uniq], np.float32)
    col = cols[inv].reshape(pid.shape + (3,)); hp = hs[inv].reshape(pid.shape)
    # scheggiature: il bordo rientra dove il rumore è alto
    n_chip = fbm(size, seed + 3, scales=(20, 8, 4))
    e = edge_px - chip * 10 * smooth(0.55, 0.85, n_chip)
    inside_b = (e > gap * 0.5).astype(np.uint8)
    # distanza euclidea dal bordo (periodica: si calcola su 3x3 copie) -> spigoli arrotondati
    big = np.tile(inside_b, (3, 3))
    dt = cv2.distanceTransform(big, cv2.DIST_L2, 5)[size:2 * size, size:2 * size]
    dt = wrap_blur(dt, 1.0)
    inside = smooth(0.0, 1.2, dt)
    prof = smooth(0.0, bevel, dt)                                     # smusso
    # cupola: profilo circolare normalizzato sulla dimensione del pezzo
    flatid = pid.ravel(); uniq2, inv2 = np.unique(flatid, return_inverse=True)
    mx = np.zeros(len(uniq2), np.float32); np.maximum.at(mx, inv2, dt.ravel())
    rel = np.clip(dt / np.maximum(mx[inv2].reshape(dt.shape), 1.0), 0, 1)
    bulge = np.sqrt(1 - (1 - rel) ** 2)
    h = inside * (0.55 * prof + dome * bulge) * hp
    h += 0.03 * fbm(size, seed + 5, scales=(24, 10)) * inside        # superficie morbida
    e = dt + gap * 0.5                                                # da qui in poi: distanza vera
    if grout is None: grout = np.array([0.55, 0.52, 0.48], np.float32)
    g = grout * (0.9 + 0.2 * fbm(size, seed + 9, scales=(30, 12)))[..., None]
    h = np.maximum(h, grout_h * (1 - inside) * fbm(size, seed + 11, scales=(16, 6)))
    # pittura: gradiente interno, luce sugli spigoli alti, fughe scure, pennellate
    lightrim = smooth(gap * 0.5 + 0.5, gap * 0.5 + bevel * 0.9, e) * (1 - smooth(gap * 0.5 + bevel * 0.9, gap * 0.5 + bevel * 2.2, e))
    alb = col * (1 + hl * lightrim[..., None])
    alb *= (1 - 0.12 * (1 - bulge))[..., None]
    if fy is not None: alb *= (1 + grad_top * (0.5 - fy))[..., None]
    st = aniso(size, seed + 13, 10 * size / 1024, 5 * size / 1024)
    alb *= (1 + stroke * (st - 0.5) * 2)[..., None]
    alb *= (0.94 + 0.12 * fbm(size, seed + 17, scales=(200, 90)))[..., None]
    alb = alb * inside[..., None] + g * (1 - inside[..., None])
    cavity = 1 - smooth(gap * 0.5 - 0.5, gap * 0.5 + 2.5, e)
    alb *= (1 - cav * 0.5 * cavity * inside)[..., None]
    rough = 0.78 + 0.14 * (1 - inside) - 0.05 * lightrim
    return np.clip(alb, 0, 1), np.clip(h, 0, 1), rough

# ------------------------------------------------------------------ materiali
def plaster(size, seed, c_shadow, c_mid, c_light, cracks=0.6, strokes=0.07, blotch=0.5):
    """Intonaco dipinto: macchie larghe a due toni, pennellate di cazzuola, poche crepe sottili."""
    big = fbm(size, seed, scales=(260, 140, 70), weights=[1, .6, .3])
    mid = fbm(size, seed + 1, scales=(48, 22, 10), weights=[1, .5, .25])
    t = 0.55 + blotch * (big - 0.5) * 0.9 + 0.18 * (mid - 0.5)
    t = np.clip(t, 0, 1)
    c0, c1, c2 = hex2(c_shadow), hex2(c_mid), hex2(c_light)
    lo = c0 + (c1 - c0) * smooth(0.0, 0.5, t)[..., None]
    hi = c1 + (c2 - c1) * smooth(0.5, 1.0, t)[..., None]
    alb = np.where((t < 0.5)[..., None], lo, hi)
    tr = aniso(size, seed + 2, 60 * size / 1024, 14 * size / 1024)    # cazzuola orizzontale
    tr2 = aniso(size, seed + 3, 16 * size / 1024, 40 * size / 1024)
    alb *= (1 + strokes * ((tr - 0.5) * 1.6 + (tr2 - 0.5) * 0.8))[..., None]
    h = 0.5 + 0.12 * (mid - 0.5) + 0.06 * (tr - 0.5) + 0.05 * (big - 0.5)   # solo ondulazioni larghe: niente puntinato
    # crepe: creste di un rumore (|n-0.5| piccolo), solo dove una maschera larga lo consente
    if cracks > 0:
        n = fbm(size, seed + 5, scales=(90, 40, 16), weights=[1, .5, .25])
        ridge = 1 - smooth(0.0, 0.012, np.abs(n - 0.5))
        mask = smooth(0.62, 0.8, fbm(size, seed + 6, scales=(220, 120)))
        c = ridge * mask * cracks
        h -= 0.25 * c
        alb *= (1 - 0.45 * c)[..., None]
        # bordo chiaro della crepa (luce dipinta)
        rim = (1 - smooth(0.012, 0.03, np.abs(n - 0.5))) * mask * cracks - c
        alb *= (1 + 0.10 * np.clip(rim, 0, 1))[..., None]
    rough = 0.86 + 0.05 * (1 - tr)
    return np.clip(alb, 0, 1), np.clip(h, 0, 1), rough

def brick(size, seed, cols_hex=("#A65A3E", "#B8694A", "#94503A", "#C27A55", "#8C4A36"), rows=16, cols=4):
    pid, d, fx, fy = bricks_layout(size, rows, cols, seed, jit=0.035)
    return paint_pieces(pid, d, [hex2(c) for c in cols_hex], seed, bevel=5 * size / 1024, gap=4 * size / 1024,
                        size=size, dome=0.2, chip=0.35, grout=hex2("#B3A796"), fy=fy)

def ashlar(size, seed, cols_hex=("#CFC3A8", "#C4B79B", "#D8CDB4", "#BDAF93"), rows=6, cols=3):
    pid, d, fx, fy = bricks_layout(size, rows, cols, seed, jit=0.02, stagger=0.5)
    return paint_pieces(pid, d, [hex2(c) for c in cols_hex], seed, bevel=10 * size / 1024, gap=4 * size / 1024,
                        size=size, dome=0.15, chip=0.45, hl=0.18, grout=hex2("#A89C88"), fy=fy)

def cobble(size, seed, cols_hex=("#8E8A84", "#9C978E", "#858079", "#A39D93"), n=10):
    pid, d, fx, fy = bricks_layout(size, n, n, seed, jit=0.06, stagger=0.37)
    return paint_pieces(pid, d, [hex2(c) for c in cols_hex], seed, bevel=12 * size / 1024, gap=5 * size / 1024,
                        size=size, dome=0.55, chip=0.25, hl=0.22, cav=0.6, grout=hex2("#6E665C"), grout_h=0.1, fy=fy)

def slabs(size, seed, cols_hex=("#CBC3B3", "#BFB6A4", "#D4CCBC", "#B7AE9C"), n=26):
    idx, edge, dc = voronoi(size, n, seed, relax=3)
    return paint_pieces(idx, edge, [hex2(c) for c in cols_hex], seed, bevel=7 * size / 1024, gap=5 * size / 1024,
                        size=size, dome=0.08, chip=0.3, hl=0.15, cav=0.5, grout=hex2("#8E8576"), grout_h=0.1)

def coppi(size, seed, cols_hex=("#B0623F", "#BA6C48", "#A65C3C", "#C27852"), cols=9, rows=6):
    """Coppi: file verticali di tegole a canale, sovrapposte."""
    r = rng(seed)
    y = (np.arange(size) + .5) / size; x = (np.arange(size) + .5) / size
    Y, X = np.meshgrid(y, x, indexing='ij')
    cx = X * cols; ci = np.floor(cx).astype(int) % cols; fxl = cx - np.floor(cx)
    ry = Y * rows + (ci % 2) * 0.5; ri = np.floor(ry).astype(int); fyl = ry - np.floor(ry)
    pid = ri * 100 + ci
    prof = np.sin(np.pi * fxl)                                       # sezione a mezzo cilindro
    over = smooth(0.0, 0.12, fyl) * (0.75 + 0.25 * fyl)              # la tegola sopra copre l'inizio
    h = prof * 0.8 * over + 0.2 * fyl
    ids = np.unique(pid); lut = {i: jitter_color(hex2(cols_hex[r.integers(len(cols_hex))]), r, vv=0.08) for i in ids}
    uniq, inv = np.unique(pid.ravel(), return_inverse=True)
    col = np.array([lut[u] for u in uniq], np.float32)[inv].reshape(pid.shape + (3,))
    alb = col * (0.72 + 0.38 * prof)[..., None]                      # luce dipinta sul dorso
    alb *= (1 - 0.35 * (1 - smooth(0.0, 0.10, fyl)))[..., None]      # ombra sotto la tegola sopra
    alb *= (1 - 0.25 * (1 - smooth(0.0, 0.18, np.minimum(fxl, 1 - fxl))))[..., None]
    alb *= (0.95 + 0.1 * fbm(size, seed + 3, scales=(120, 50)))[..., None]
    lich = smooth(0.7, 0.85, fbm(size, seed + 4, scales=(30, 12, 5))) * 0.5
    alb = alb * (1 - lich[..., None]) + hex2("#8A8670") * lich[..., None]
    rough = 0.8 + 0.08 * (1 - prof)
    return np.clip(alb, 0, 1), np.clip(h, 0, 1), rough

def normal_from_height(h, strength):
    gx = (np.roll(h, -1, 1) - np.roll(h, 1, 1)) * 0.5
    gy = (np.roll(h, -1, 0) - np.roll(h, 1, 0)) * 0.5
    n = np.dstack([-gx * strength, gy * strength, np.ones_like(h)]).astype(np.float32)
    return n / np.linalg.norm(n, axis=2, keepdims=True)

def shade(alb, n, light=(-0.45, 0.55, 0.70), amb=0.45):
    l = np.array(light, np.float32); l /= np.linalg.norm(l)
    nd = np.clip((n * l).sum(axis=2), 0, 1)[..., None]
    return np.clip(alb * (amb + (1 - amb) * nd * 1.1), 0, 1)

def to8(x): return (np.clip(x, 0, 1) * 255 + 0.5).astype(np.uint8)
