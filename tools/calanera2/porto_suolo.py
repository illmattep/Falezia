"""Suolo del Porto Vecchio: lastre di cemento 1,5 × 1 m, giunti sottili, sporco appena accennato.

Sostituisce la versione della forgia per il solo piazzale dello spawn: i giunti erano troppo
scuri e, di taglio, l'orlo chiaro delle lastre faceva un effetto "tratteggio" sul terreno.

Uscita (stessa cartella, stessi nomi: i materiali non cambiano):
  Assets/Art/Calanera/Textures/cemento_lastre/cemento_lastre_a.png  albedo sRGB
  ..._n.png  normal (OpenGL)
  ..._m.png  R metallo · G occlusione · B altezza · A levigatezza
Tessera: 6 m. Uso: python3 tools/calanera2/porto_suolo.py
"""
import os
import numpy as np
from PIL import Image, ImageFilter

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
OUT = os.path.join(ROOT, "Assets", "Art", "Calanera", "Textures", "cemento_lastre")
S = 2048
TILE_M = 6.0
COLS, ROWS = 4, 6

rng = np.random.default_rng(24)


def smooth(a, b, x):
    t = np.clip((x - a) / max(b - a, 1e-9), 0, 1)
    return t * t * (3 - 2 * t)


def noise(size, seed, scales=(64, 24, 9), weights=(1.0, 0.55, 0.3)):
    r = np.random.default_rng(seed)
    h = w = size
    out = np.zeros((h, w), np.float32)
    for sc, wt in zip(scales, weights):
        gy = gx = max(2, sc)
        grid = r.random((gy, gx)).astype(np.float32)
        y = np.linspace(0, gy, h, endpoint=False); x = np.linspace(0, gx, w, endpoint=False)
        y0 = np.floor(y).astype(int); x0 = np.floor(x).astype(int)
        y1 = (y0 + 1) % gy; x1 = (x0 + 1) % gx
        fy = (y - y0)[:, None]; fx = (x - x0)[None, :]
        fy = fy * fy * (3 - 2 * fy); fx = fx * fx * (3 - 2 * fx)
        v = (grid[np.ix_(y0, x0)] * (1 - fx) + grid[np.ix_(y0, x1)] * fx) * (1 - fy) + \
            (grid[np.ix_(y1, x0)] * (1 - fx) + grid[np.ix_(y1, x1)] * fx) * fy
        out += v * wt
    out -= out.min(); out /= max(out.max(), 1e-6)
    return out


def save_rgb(path, a):
    Image.fromarray((np.clip(a, 0, 1) * 255 + .5).astype(np.uint8), "RGB").save(path)


def main():
    os.makedirs(OUT, exist_ok=True)
    px_m = S / TILE_M
    sx = S / COLS
    sy = S / ROWS
    yy, xx = np.mgrid[0:S, 0:S].astype(np.float32)
    # distanza dal giunto più vicino (in pixel)
    dx = np.minimum(xx % sx, sx - (xx % sx))
    dy = np.minimum(yy % sy, sy - (yy % sy))
    d = np.minimum(dx, dy)
    # giunto largo 8 mm, raccordo 4 mm
    edge = smooth(0.6, 3.2, d)                       # 0 nel giunto, 1 nella lastra
    joint = 1.0 - edge

    # lastra: indice di colonna/riga per variare la tinta
    cx = np.floor(xx / sx).astype(int) % COLS
    cy = np.floor(yy / sy).astype(int) % ROWS
    idx = (cx + cy * 3) % 7
    palettes = [(0.690, 0.672, 0.632), (0.660, 0.643, 0.602), (0.712, 0.694, 0.652),
                (0.638, 0.622, 0.584), (0.680, 0.665, 0.626), (0.700, 0.682, 0.638),
                (0.665, 0.650, 0.615)]
    base = np.zeros((S, S, 3), np.float32)
    for i, c in enumerate(palettes):
        m = idx == i
        base[m] = c

    # macchie larghe e umido
    mott = noise(S, 3, scales=(96, 32, 11), weights=(1, .55, .3))
    base *= (0.96 + 0.08 * mott)[..., None]
    stains = smooth(0.62, 0.9, noise(S, 8, scales=(40, 16, 6), weights=(1, .5, .25)))
    base *= (1 - 0.10 * stains)[..., None]
    # giunti chiari di umido, non neri
    joint_col = np.array((0.44, 0.425, 0.395), np.float32)
    alb = base * edge[..., None] + joint_col * (1 - edge[..., None])

    # grana finissima
    grain = rng.normal(0, 0.008, (S, S, 1)).astype(np.float32)
    alb = alb + grain

    # altezza: lastra bombata appena, giunto incassato 4 mm
    h = 0.55 + 0.45 * edge + 0.03 * noise(S, 12, scales=(80, 30, 12), weights=(1, .5, .3))
    relief = 0.004
    hh = h * relief
    gx = (np.roll(hh, -1, 1) - np.roll(hh, 1, 1)) / (2 / px_m)
    gy = (np.roll(hh, -1, 0) - np.roll(hh, 1, 0)) / (2 / px_m)
    n = np.dstack([-gx * 1.1, gy * 1.1, np.ones_like(hh)])
    n /= np.linalg.norm(n, axis=2, keepdims=True)

    rough = 0.30 + 0.06 * (1 - edge) - 0.03 * mott
    ao = 0.80 + 0.20 * smooth(0, 5, d)
    mask = np.dstack([np.zeros_like(h), ao, h, np.clip(rough, 0.05, 0.6)])

    u8 = lambda x: (np.clip(x, 0, 1) * 255 + .5).astype(np.uint8)
    save_rgb(f"{OUT}/cemento_lastre_a.png", alb)
    save_rgb(f"{OUT}/cemento_lastre_n.png", n * 0.5 + 0.5)
    Image.fromarray(u8(mask), "RGBA").save(f"{OUT}/cemento_lastre_m.png")
    print("scritto", OUT)


if __name__ == "__main__":
    main()
