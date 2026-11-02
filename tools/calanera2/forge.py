"""FORGIA DEI MATERIALI di Calanera (doc 29).

Tre sorgenti, un'uscita sola e coerente:
  - procedurale  : pezzi disegnati in codice (mattoni, intonaco dipinto)          -> proc.py
  - scansione    : PBR CC0 (Poly Haven) resa pittorica (Kuwahara + rampa colori)
  - meshy        : texture dipinte piatte -> PBR vere (altezza ricostruita, pulizia)

Uscita per ogni materiale, in Assets/Art/Calanera/Textures/<id>/ :
  <id>_a.png  albedo sRGB
  <id>_n.png  normal map (OpenGL, verde = +V), intensità calcolata dalla SCALA VERA
  <id>_m.png  maschera lineare: R metallo · G occlusione · B altezza · A levigatezza
  <id>.json   metri per tessera, rilievo in metri, sorgente

Regole PBR: albedo dei non metalli fra 30 e 235 sRGB, levigatezza 0,05–0,45 (pietra, intonaco),
normali dalla pendenza reale (rilievo in metri / metri per pixel) × un fattore di stile.
Tutto è periodico: le texture si ripetono senza giunte.
"""
import json, os, sys
import numpy as np, cv2
from proc import (wrap_blur, norm01, fbm, smooth, hex2, plaster, brick, ashlar, paint_pieces, bricks_layout, rng)

S = 2048
SCARICATI = os.environ.get("FZ_MESHY", "/mnt/user-data/uploads/Scaricati")
PH = os.environ.get("FZ_PH", "/tmp/claude-0/ph")
OUT = os.environ.get("FZ_OUT", "/tmp/claude-0/forge/out")

# ------------------------------------------------------------------ utilità
def to_lin(c): return np.where(c <= 0.04045, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)
def to_srgb(c): return np.where(c <= 0.0031308, c * 12.92, 1.055 * np.power(np.clip(c, 0, 1), 1 / 2.4) - 0.055)

def kuwahara(img, r=4):
    """Kuwahara a 4 quadranti, periodico: macchie di colore pulite, bordi tenuti."""
    pad = r
    I = cv2.copyMakeBorder(img, pad, pad, pad, pad, cv2.BORDER_WRAP)
    L = I.mean(axis=2)
    k = r + 1
    box = lambda a: cv2.boxFilter(a, -1, (k, k), normalize=True, borderType=cv2.BORDER_REFLECT)
    mean = box(I); mL = box(L); var = box(L * L) - mL * mL
    H, W = img.shape[:2]; best = None; bestv = None
    for oy in (pad - r // 2, pad + r // 2):
        for ox in (pad - r // 2, pad + r // 2):
            m = mean[oy:oy + H, ox:ox + W]; v = var[oy:oy + H, ox:ox + W]
            if best is None: best, bestv = m.copy(), v.copy()
            else:
                sel = v < bestv; best[sel] = m[sel]; bestv[sel] = v[sel]
    return best

def resize(x, s=S):
    if x.shape[0] == s: return x
    return cv2.resize(x, (s, s), interpolation=cv2.INTER_LANCZOS4 if x.shape[0] < s else cv2.INTER_AREA)

def grade(alb, target_hex, amount=0.7):
    """Porta il colore medio sulla palette (doc 07) tenendo la variazione interna."""
    t = to_lin(hex2(target_hex))
    lin = to_lin(np.clip(alb, 0, 1))
    mean = lin.reshape(-1, 3).mean(0)
    graded = lin * (t / np.maximum(mean, 1e-4))
    return to_srgb(lin * (1 - amount) + graded * amount)

def load_rgb(path, s=S):
    im = cv2.imread(path, cv2.IMREAD_COLOR)
    if im is None: raise FileNotFoundError(path)
    return resize(cv2.cvtColor(im, cv2.COLOR_BGR2RGB).astype(np.float32) / 255.0, s)

def load_gray(path, s=S):
    im = cv2.imread(path, cv2.IMREAD_UNCHANGED)
    if im is None: raise FileNotFoundError(path)
    if im.ndim == 3: im = cv2.cvtColor(im[..., :3], cv2.COLOR_BGR2GRAY)
    im = im.astype(np.float32) / (65535.0 if im.dtype == np.uint16 else 255.0)
    return resize(im, s)

# ------------------------------------------------------------------ uscita PBR
def finalize(mid, alb, h, rough, tile_m, relief_m, style=1.6, metal=None, ao_src=None, source="", palette=None, cav_ao=0.55):
    s = alb.shape[0]
    h = norm01(h, 0.2, 99.8)
    if palette: alb = grade(alb, palette)
    alb = np.clip(alb, 30 / 255, 235 / 255)                      # albedo plausibile
    # normale dalla pendenza VERA: rilievo in metri su metri per pixel
    px = tile_m / s
    hm = wrap_blur(h, 0.8) * relief_m
    gx = (np.roll(hm, -1, 1) - np.roll(hm, 1, 1)) / (2 * px)
    gy = (np.roll(hm, -1, 0) - np.roll(hm, 1, 0)) / (2 * px)
    n = np.dstack([-gx * style, gy * style, np.ones_like(hm)]).astype(np.float32)
    n /= np.linalg.norm(n, axis=2, keepdims=True)
    # occlusione dalle cavità (+ quella della sorgente se c'è)
    cav = np.clip((wrap_blur(h, s * 0.006) - h) * 4.0, 0, 1)
    ao = 1 - cav_ao * cav
    if ao_src is not None: ao = ao * (0.5 + 0.5 * ao_src)
    ao = np.clip(ao, 0.35, 1.0)
    smooth_ = np.clip(1 - rough, 0.04, 0.6)
    met = np.zeros_like(h) if metal is None else metal
    d = os.path.join(OUT, mid); os.makedirs(d, exist_ok=True)
    u8 = lambda x: (np.clip(x, 0, 1) * 255 + 0.5).astype(np.uint8)
    cv2.imwrite(f"{d}/{mid}_a.png", cv2.cvtColor(u8(alb), cv2.COLOR_RGB2BGR))
    cv2.imwrite(f"{d}/{mid}_n.png", cv2.cvtColor(u8(n * 0.5 + 0.5), cv2.COLOR_RGB2BGR))
    mask = np.dstack([met, ao, h, smooth_])                        # R G B A
    cv2.imwrite(f"{d}/{mid}_m.png", cv2.cvtColor(u8(mask), cv2.COLOR_RGBA2BGRA))
    meta = dict(id=mid, tile_m=tile_m, relief_m=relief_m, style=style, source=source, size=s)
    json.dump(meta, open(f"{d}/{mid}.json", "w"), indent=1)
    return dict(alb=alb, n=n, h=h, ao=ao, meta=meta)

# ------------------------------------------------------------------ sorgente: scansione -> pittorica
def from_scan(name, pal, kr=4, hblur=1.5, edge=0.6, cavity=0.7, keep_hue=0.2):
    alb = load_rgb(f"{PH}/{name}_diff.jpg"); h = load_gray(f"{PH}/{name}_disp.jpg")
    k = max(2, int(kr * S / 1024)) // 2 * 2
    h = wrap_blur(h, hblur * S / 1024); hn = norm01(h)
    paint = kuwahara(kuwahara(alb, k), max(2, k // 2))
    lum = 0.55 * norm01(paint.mean(2)) + 0.45 * hn
    c0, c1, c2 = (np.array(c, np.float32) / 255 for c in pal)
    t = lum[..., None]
    base = np.where(t < 0.5, c0 + (c1 - c0) * np.clip(t * 2, 0, 1), c1 + (c2 - c1) * np.clip(t * 2 - 1, 0, 1))
    orig = paint / np.maximum(paint.mean(2, keepdims=True), 1e-3) * base.mean(2, keepdims=True)
    base = base * (1 - keep_hue) + np.clip(orig, 0, 1) * keep_hue
    conv = norm01(h - wrap_blur(h, 6.0 * S / 1024), 50, 99.5)
    cav = norm01(wrap_blur(h, 10.0 * S / 1024) - h, 50, 99.5)
    base = base * (1 + edge * 0.3 * conv[..., None]) * (1 - cavity * 0.4 * cav[..., None])
    rough = 0.80 + 0.10 * cav - 0.06 * conv
    return np.clip(base, 0, 1), hn, rough

# ------------------------------------------------------------------ sorgente: meshy piatta -> PBR
def meshy_soft(name, smooth_px=3.0, grain=0.25, seed=1):
    """Intonaci: pulizia (bilaterale + Kuwahara leggero), altezza da luminanza morbida + rumore."""
    alb = load_rgb(f"{SCARICATI}/Meshy_AI_{name}.png")
    clean = cv2.bilateralFilter(alb, 9, 0.08, 6)
    clean = kuwahara(clean, 4)
    lum = norm01(wrap_blur(clean.mean(2), smooth_px * S / 1024))
    h = 0.6 * lum + 0.4 * fbm(S, seed, scales=(60, 25, 10))
    # pennellate leggere per non lasciarla piatta
    clean *= (1 + 0.04 * (fbm(S, seed + 1, scales=(14, 6)) - 0.5))[..., None]
    rough = 0.86 + 0.06 * (1 - lum)
    return np.clip(clean, 0, 1), h, rough

def meshy_pieces(name, joints="light", bevel=10, gap=5, dome=0.3, chip=0.25, grout=None, seed=3):
    """Pietre e lastre dipinte: si trovano le fughe (colore), si ricostruisce il rilievo dei pezzi."""
    alb = load_rgb(f"{SCARICATI}/Meshy_AI_{name}.png")
    lab = cv2.cvtColor(alb, cv2.COLOR_RGB2Lab)
    L = wrap_blur(lab[..., 0], 1.5 * S / 1024)
    # soglia di Otsu sulla luminanza: due popolazioni (pezzi e fughe)
    l8 = (norm01(L) * 255).astype(np.uint8)
    thr, _ = cv2.threshold(l8, 0, 255, cv2.THRESH_BINARY + cv2.THRESH_OTSU)
    joint = (l8 > thr) if joints == "light" else (l8 < thr)
    # le fughe sono la popolazione SOTTILE: se occupa più del 45% abbiamo scelto la sbagliata
    if joint.mean() > 0.45: joint = ~joint
    piece = (~joint).astype(np.uint8)
    piece = cv2.morphologyEx(piece, cv2.MORPH_OPEN, np.ones((5, 5), np.uint8))
    n, lbl = cv2.connectedComponents(np.tile(piece, (3, 3)))
    lbl = lbl[S:2 * S, S:2 * S]
    big = np.tile(piece, (3, 3))
    dt = cv2.distanceTransform(big, cv2.DIST_L2, 5)[S:2 * S, S:2 * S]
    # rilievo: smusso + cupola relativa alla dimensione del pezzo
    uniq, inv = np.unique(lbl.ravel(), return_inverse=True)
    mx = np.zeros(len(uniq), np.float32); np.maximum.at(mx, inv, dt.ravel())
    rel = np.clip(dt / np.maximum(mx[inv].reshape(dt.shape), 1), 0, 1)
    b = bevel * S / 1024
    prof = smooth(0, b, dt); bulge = np.sqrt(1 - (1 - rel) ** 2)
    inside = smooth(0, 1.5, dt)
    hv = rng(seed).uniform(0.85, 1.0, len(uniq)).astype(np.float32)[inv].reshape(dt.shape)
    h = inside * (0.6 * prof + dome * bulge) * hv + 0.03 * fbm(S, seed, scales=(20, 8)) * inside
    # albedo: pezzi puliti (Kuwahara), fughe uniformi, luce sugli spigoli
    paint = kuwahara(cv2.bilateralFilter(alb, 9, 0.1, 6), 6)
    g = paint[joint].mean(0) if grout is None else hex2(grout)
    rim = smooth(0.5, b * 0.8, dt) * (1 - smooth(b * 0.8, b * 2.0, dt))
    a = paint * (1 + 0.14 * rim)[..., None] * (1 - 0.10 * (1 - bulge))[..., None]
    gcol = g[None, None, :] * (0.9 + 0.2 * fbm(S, seed + 2, scales=(30, 10)))[..., None]
    a = a * inside[..., None] + gcol * (1 - inside[..., None])
    a *= (1 - 0.3 * (1 - smooth(0, 3 * S / 1024, dt)) * inside)[..., None]
    rough = 0.80 + 0.12 * (1 - inside) - 0.05 * rim
    return np.clip(a, 0, 1), h, rough

def meshy_given(name, disp, ao=None, clean=True):
    alb = load_rgb(f"{SCARICATI}/Meshy_AI_{name}.png")
    if clean: alb = kuwahara(cv2.bilateralFilter(alb, 9, 0.08, 6), 4)
    h = load_gray(f"{SCARICATI}/Meshy_AI_{disp}.png")
    a = load_gray(f"{SCARICATI}/Meshy_AI_{ao}.png") if ao else None
    rough = 0.82 + 0.08 * (1 - norm01(h))
    return alb, h, rough, a

# ------------------------------------------------------------------ macro (variazione larga)
def macro(size=1024):
    r = fbm(size, 101, scales=(220, 110, 50, 20), weights=[1, .6, .35, .2])   # danni
    g = fbm(size, 202, scales=(300, 140, 60), weights=[1, .5, .25])            # luce
    b = fbm(size, 303, scales=(80, 30, 12), weights=[1, .6, .4])               # sporco
    a = fbm(size, 404, scales=(400, 200), weights=[1, .5])                     # tinta
    m = np.dstack([r, g, b, a])
    d = os.path.join(OUT, "macro"); os.makedirs(d, exist_ok=True)
    cv2.imwrite(f"{d}/macro_m.png", cv2.cvtColor((m * 255 + .5).astype(np.uint8), cv2.COLOR_RGBA2BGRA))

# ------------------------------------------------------------------ la libreria
RECIPES = {
    # id: (funzione, metri per tessera, rilievo in metri, stile normal, palette, sorgente)
    "intonaco_liscio": (lambda: plaster(S, 1, "#D2B892", "#E5D1AE", "#F3E7D0", cracks=0.0, strokes=0.05, blotch=0.55),
                        5.0, 0.004, 1.5, None, "procedurale"),
    "intonaco_venez":  (lambda: meshy_soft("wall_venez_01"), 4.0, 0.003, 1.5, "#DCC9A6", "meshy wall_venez_01"),
    "intonaco_grezzo": (lambda: meshy_soft("wall_grezzo_01", smooth_px=2.0, seed=7), 3.0, 0.004, 1.3, "#D9C7A6", "meshy wall_grezzo_01"),
    "mattone":         (lambda: brick(S, 2), 1.2, 0.015, 1.6, None, "procedurale"),
    "pietra_conci":    (lambda: ashlar(S, 3, cols_hex=("#CFC3A8", "#C6B99E", "#D7CCB3", "#BFB295", "#CDBFA0"), rows=8, cols=4),
                        3.2, 0.025, 1.5, "#C9BFA8", "procedurale (palette da meshy wall_stone_01)"),
    "sampietrini":     (lambda: from_scan("cobblestone_floor_08", ((92, 88, 84), (148, 142, 132), (200, 194, 182)), kr=4, hblur=1.5, edge=0.7, cavity=0.8, keep_hue=0.2),
                        2.0, 0.03, 1.5, None, "polyhaven cobblestone_floor_08"),
    "lastre":          (lambda: meshy_pieces("lastrico_piazza_01", joints="light", bevel=8, gap=4, dome=0.1, grout="#A89D88"),
                        5.0, 0.012, 1.3, "#CBBEA6", "meshy lastrico_piazza_01"),
}

def build(ids=None):
    os.makedirs(OUT, exist_ok=True)
    done = []
    soft_ao = {"lastre": 0.3, "pietra_conci": 0.35, "intonaco_grezzo": 0.3, "intonaco_liscio": 0.3, "intonaco_venez": 0.3}
    for mid, (fn, tile, relief, style, pal, src) in RECIPES.items():
        if ids and mid not in ids: continue
        a, h, r = fn()
        finalize(mid, a, h, r, tile, relief, style=style, source=src, palette=pal, cav_ao=soft_ao.get(mid, 0.55))
        done.append(mid); print("forgiato", mid, flush=True)
    # coppi: Meshy con mappe proprie
    if not ids or "coppi" in ids:
        a, h, r, ao = meshy_given("roof_coppi_02", "roof_coppi_02_displacement", "roof_coppi_02_ambient")
        finalize("coppi", a, h, r, 1.6, 0.04, style=1.3, ao_src=ao, source="meshy roof_coppi_02", palette="#A8603F")
        done.append("coppi"); print("forgiato coppi", flush=True)
    if not ids or "macro" in ids:
        macro(); done.append("macro")
    lib = {}
    for mid in sorted(os.listdir(OUT)):                 # tutta la libreria, non solo quelli rifatti ora
        p = os.path.join(OUT, mid, f"{mid}.json")
        if os.path.exists(p): lib[mid] = json.load(open(p))
    json.dump(lib, open(os.path.join(OUT, "libreria.json"), "w"), indent=1)

if __name__ == "__main__":
    build(sys.argv[1:] or None)

# ------------------------------------------------------------------ estensione: porto (spawn Nord)
def neutro(alb, warm=(1.0, 0.97, 0.92), keep=0.15):
    """Toglie il colore (per le texture da tingere in Unity): luminanza chiara con un filo di tinta originale."""
    lum = alb.mean(2, keepdims=True)
    lum = 0.35 + 0.6 * (lum - lum.min()) / max(lum.max() - lum.min(), 1e-4)
    base = lum * np.array(warm, np.float32)[None, None, :]
    return np.clip(base * (1 - keep) + alb * keep, 0, 1)

def meshy_lines(name, blur=1.5, seed=11, clean=4):
    """Texture a righe (lamiera, doghe, assi): l'altezza segue la luminanza (le righe chiare sporgono)."""
    alb = load_rgb(f"{SCARICATI}/Meshy_AI_{name}.png")
    a = kuwahara(cv2.bilateralFilter(alb, 7, 0.08, 5), clean)
    h = norm01(wrap_blur(alb.mean(2), blur * S / 1024))
    h = 0.85 * h + 0.15 * fbm(S, seed, scales=(40, 12))
    rough = 0.75 + 0.1 * (1 - h)
    return a, h, rough

def lastre_cemento(seed=21):
    """Piazzale del porto: lastre di cemento 1,5 × 1 m (tessera 6 m = 4 × 6 lastre), giunti scuri, macchie."""
    pid, d, fx, fy = bricks_layout(S, 6, 4, seed, jit=0.004, stagger=0.0)
    cols = [hex2(c) for c in ("#AFAAA0", "#B6B1A7", "#A8A399", "#BBB6AC")]
    a, h, r = paint_pieces(pid, d, cols, seed, bevel=4 * S / 1024, gap=3 * S / 1024, size=S, dome=0.03,
                           chip=0.35, hl=0.08, cav=0.5, stroke=0.0, grout=hex2("#5E5A54"), grout_h=0.05)
    stains = smooth(0.62, 0.9, fbm(S, seed + 5, scales=(120, 60, 25)))
    a = a * (1 - 0.09 * stains[..., None])
    oil = smooth(0.78, 0.95, fbm(S, seed + 9, scales=(40, 18)))
    a = a * (1 - 0.12 * oil[..., None])
    r = r - 0.15 * oil
    return a, h, r

PORTO = {
    "lamiera_blu":     (lambda: meshy_lines("lamiera_01", blur=1.2), 1.2, 0.035, 1.0, "#6F7F8A", "meshy lamiera_01"),
    "lamiera_ruggine": (lambda: meshy_lines("lamiera_02", blur=1.2), 1.2, 0.035, 1.0, "#7E5E52", "meshy lamiera_02"),
    "legno_verniciato": (lambda: (lambda a, h, r: (neutro(a), h, r))(*meshy_lines("legno_persiana_01", blur=1.0)), 1.0, 0.006, 1.2, None, "meshy legno_persiana_01 (neutro, da tingere)"),
    "legno_scuro":     (lambda: meshy_lines("legno_porta_01", blur=1.0), 1.5, 0.006, 1.2, "#6E5039", "meshy legno_porta_01"),
    "legno_chiaro":    (lambda: meshy_lines("legno_cassa_01", blur=1.0), 1.2, 0.006, 1.2, "#B39069", "meshy legno_cassa_01"),
    "ferro":           (lambda: meshy_soft("ferro_battuto_01", smooth_px=1.5, seed=31), 1.0, 0.002, 1.0, "#3B3D41", "meshy ferro_battuto_01"),
    "cemento_lastre":  (lambda: lastre_cemento(), 6.0, 0.01, 1.3, None, "procedurale"),
}

def build_porto(ids=None):
    for mid, (fn, tile, relief, style, pal, src) in PORTO.items():
        if ids and mid not in ids: continue
        a, h, r = fn()
        met = None
        if mid == "ferro": met = np.full(h.shape, 0.85, np.float32); r = np.clip(r - 0.35, 0.3, 1)
        if mid.startswith("lamiera"): r = np.clip(r - 0.15, 0.4, 1)
        finalize(mid, a, h, r, tile, relief, style=style, source=src, palette=pal, metal=met, cav_ao=0.4)
        print("forgiato", mid, flush=True)
    lib = {}
    for m in sorted(os.listdir(OUT)):
        p = os.path.join(OUT, m, f"{m}.json")
        if os.path.exists(p): lib[m] = json.load(open(p))
    json.dump(lib, open(os.path.join(OUT, "libreria.json"), "w"), indent=1)
