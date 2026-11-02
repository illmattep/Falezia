"""Texture PBR stilizzate per la mappa (stile Valorant, non fotorealismo).

Per ogni materiale produce albedo, normal e roughness a 512². Il linguaggio:
  · colore piatto e pulito, variazione lenta, nessuna macchia casuale;
  · dettaglio fine leggero (rumore a bassa ampiezza), così la superficie legge "dipinta";
  · normal map da height: le fughe, le doghe, le bugne si vedono in luce radente;
  · niente AO cotta, niente sporco fotografico: il contrasto lo fa la luce.

  python3 tools/map-assets/make_textures.py
"""
import os, math
import numpy as np
from PIL import Image

OUT = os.path.join(os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))),
                   "art/map-assets/tex")
os.makedirs(OUT, exist_ok=True)
S = 512
rng = np.random.default_rng(7)

def value_noise(size, freq, octaves=3, seed=0):
    r = np.random.default_rng(seed)
    total = np.zeros((size, size), np.float32)
    amp = 1.0; f = freq
    for o in range(octaves):
        g = r.random((f + 1, f + 1)).astype(np.float32)
        # interpolazione bilineare
        xs = np.linspace(0, f, size, endpoint=False)
        ys = np.linspace(0, f, size, endpoint=False)
        x0 = xs.astype(int); y0 = ys.astype(int)
        fx = (xs - x0)[None, :]; fy = (ys - y0)[:, None]
        a = g[y0][:, x0]; b = g[y0][:, x0 + 1]
        c = g[y0 + 1][:, x0]; d = g[y0 + 1][:, x0 + 1]
        layer = (a * (1 - fx) + b * fx) * (1 - fy) + (c * (1 - fx) + d * fx) * fy
        total += layer * amp
        amp *= 0.5; f *= 2
    total /= total.max()
    return total

def normal_from_height(h, strength=2.0):
    dx = np.gradient(h, axis=1) * strength
    dy = np.gradient(h, axis=0) * strength
    n = np.dstack((-dx, -dy, np.ones_like(h)))
    n /= np.linalg.norm(n, axis=2, keepdims=True)
    return (n * 0.5 + 0.5)

def save(name, albedo, height, rough, strength=2.0):
    Image.fromarray((np.clip(albedo, 0, 1) * 255).astype(np.uint8)).save(f"{OUT}/{name}.png")
    Image.fromarray((normal_from_height(height, strength) * 255).astype(np.uint8)).save(f"{OUT}/{name}_n.png")
    Image.fromarray((np.clip(rough, 0, 1) * 255).astype(np.uint8)).save(f"{OUT}/{name}_r.png")

def tint(color, h, amount=0.10):
    c = np.array(color, np.float32)
    return np.clip(c[None, None, :] * (1 - amount + amount * 2 * h[:, :, None]), 0, 1)

# ---------------------------------------------------------------- intonaco
h = value_noise(S, 6, 3, 1) * 0.6 + value_noise(S, 30, 2, 2) * 0.4
save("stucco_warm", tint((0.72, 0.60, 0.44), h, 0.16), h * 0.35, np.full((S, S), 0.88), 0.6)

# ---------------------------------------------------------------- pietra a conci
rows, cols = 8, 4
hb = np.zeros((S, S), np.float32)
alb = np.zeros((S, S, 3), np.float32)
block = np.array([0.66, 0.62, 0.55])
jh = S // rows; jw = S // cols
for r in range(rows):
    off = (jw // 2) * (r % 2)
    for c in range(cols + 1):
        x0 = (c * jw + off) % S
        jitter = rng.uniform(-0.05, 0.05)
        col = block + jitter
        xs = (np.arange(x0, x0 + jw)) % S
        ys = np.arange(r * jh, (r + 1) * jh)
        alb[np.ix_(ys, xs)] = col
        hb[np.ix_(ys, xs)] = 0.75
# fughe
for r in range(rows + 1):
    hb[(r * jh) % S, :] = 0.0
    alb[(r * jh) % S, :] = [0.42, 0.40, 0.36]
for c in range(cols * 2):
    hb[:, (c * jw // 1) % S] = 0.0
grain = value_noise(S, 40, 2, 5) * 0.25
save("stone_blocks", np.clip(alb * (0.9 + grain[:, :, None] * 0.2), 0, 1),
     hb * 0.6 + grain * 0.15, np.full((S, S), 0.82), 3.0)

# ---------------------------------------------------------------- lamiera grecata
x = np.arange(S)[None, :]
rib = (np.sin(x / S * 2 * math.pi * 18) * 0.5 + 0.5)
h = np.repeat(rib, S, axis=0)
alb = tint((0.30, 0.42, 0.48), h, 0.22)
scratch = value_noise(S, 60, 2, 9) * 0.10
save("metal_ribbed", np.clip(alb + scratch[:, :, None], 0, 1), h * 0.8, 0.45 + scratch, 4.0)

# ---------------------------------------------------------------- metallo verniciato
h = value_noise(S, 5, 2, 3) * 0.5
alb = tint((0.55, 0.42, 0.30), h, 0.18)
# qualche graffio verticale, pulito
for i in range(18):
    xx = rng.integers(0, S); yy = rng.integers(0, S); ln = rng.integers(20, 120)
    alb[yy:yy + ln, xx:xx + 1] = np.clip(alb[yy:yy + ln, xx:xx + 1] * 1.35, 0, 1)
save("metal_painted", alb, h * 0.25, 0.5 + value_noise(S, 50, 1, 11) * 0.15, 1.2)

# ---------------------------------------------------------------- legno
planks = 6
ph = S // planks
h = np.tile(np.linspace(0, 1, ph)[:, None], (1, S))
h = np.vstack([h] * planks)
if h.shape[0] < S:                       # arrotonda all'altezza esatta della texture
    h = np.vstack([h, np.repeat(h[-1:], S - h.shape[0], axis=0)])
grain = value_noise(S, 8, 3, 13)
alb = tint((0.42, 0.30, 0.20), grain, 0.22)
for p in range(planks + 1):
    y = (p * ph) % h.shape[0]
    alb[y, :] = [0.18, 0.13, 0.09]
    h[y, :] = 0.0
save("wood_planks", alb, h * 0.4 + grain * 0.3, np.full((S, S), 0.75), 2.0)

# ---------------------------------------------------------------- tela a strisce (tende)
stripes = 8
sw = S // stripes
alb = np.zeros((S, S, 3), np.float32)
for i in range(stripes):
    col = np.array([0.86, 0.86, 0.82]) if i % 2 == 0 else np.array([0.24, 0.36, 0.48])
    alb[:, i * sw:(i + 1) * sw] = col
weave = value_noise(S, 80, 2, 17) * 0.10
save("canvas_stripes", np.clip(alb * (0.94 + weave[:, :, None] * 0.2), 0, 1),
     value_noise(S, 50, 2, 19) * 0.2, np.full((S, S), 0.9), 0.8)

# ---------------------------------------------------------------- coppi
rows = 10; cols = 12
h = np.zeros((S, S), np.float32); alb = np.zeros((S, S, 3), np.float32)
rh = S // rows; cw = S // cols
for r in range(rows):
    for c in range(cols):
        cx = (c * cw + (cw // 2) * (r % 2)) % S
        ys = np.arange(r * rh, (r + 1) * rh)
        xs = (np.arange(cx, cx + cw)) % S
        t = np.linspace(0, math.pi, cw)[None, :]
        prof = np.sin(t)
        h[np.ix_(ys, xs)] = 0.4 + prof * 0.6
        shade = 0.85 + 0.25 * prof
        alb[np.ix_(ys, xs)] = np.array([0.60, 0.26, 0.16]) * shade.T
save("roof_tiles", np.clip(alb, 0, 1), h * 0.9, np.full((S, S), 0.8), 3.5)

print("RESULT:", len(os.listdir(OUT)), "file in", OUT)
