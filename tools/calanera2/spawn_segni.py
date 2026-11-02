"""Segni, macchie e acqua del Porto Vecchio (spawn Nord).

Genera le texture piccole che non stanno nella libreria PBR: insegne dipinte, macchie
d'olio, pozzanghere, segnaletica consumata, ruggine, schiuma e la normal del mare.

Uscita: Assets/Art/Calanera/Textures/segni/<nome>.png
Uso:    python3 tools/calanera2/spawn_segni.py
"""
import os
import numpy as np
from PIL import Image, ImageDraw, ImageFont, ImageFilter

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
OUT = os.path.join(ROOT, "Assets", "Art", "Calanera", "Textures", "segni")
FONT = os.path.join(ROOT, "Assets", "Resources", "Fonts", "BarlowCondensed-SemiBold.ttf")
rng = np.random.default_rng(7)


def save(img, name):
    os.makedirs(OUT, exist_ok=True)
    img.save(os.path.join(OUT, name))
    print("scritto", name, img.size)


def fbm(size, seed, scales=(64, 24, 9), weights=(1.0, 0.55, 0.3)):
    """Rumore di valore periodico: serve per le macchie, non per la trama."""
    r = np.random.default_rng(seed)
    h, w = size
    out = np.zeros((h, w), np.float32)
    for sc, wt in zip(scales, weights):
        gy, gx = max(2, sc), max(2, sc)
        grid = r.random((gy, gx)).astype(np.float32)
        y = np.linspace(0, gy, h, endpoint=False)
        x = np.linspace(0, gx, w, endpoint=False)
        y0 = np.floor(y).astype(int); x0 = np.floor(x).astype(int)
        y1 = (y0 + 1) % gy; x1 = (x0 + 1) % gx
        fy = (y - y0)[:, None]; fx = (x - x0)[None, :]
        fy = fy * fy * (3 - 2 * fy); fx = fx * fx * (3 - 2 * fx)
        v = (grid[np.ix_(y0, x0)] * (1 - fx) + grid[np.ix_(y0, x1)] * fx) * (1 - fy) + \
            (grid[np.ix_(y1, x0)] * (1 - fx) + grid[np.ix_(y1, x1)] * fx) * fy
        out += v * wt
    out -= out.min(); out /= max(out.max(), 1e-6)
    return out


def smooth(a, b, x):
    t = np.clip((x - a) / max(b - a, 1e-6), 0, 1)
    return t * t * (3 - 2 * t)


def blotch(size=512, seed=1, color=(46, 36, 28), spread=0.62, alpha=0.62):
    h = fbm((size, size), seed, scales=(9, 4, 2), weights=(1, .5, .25))
    yy, xx = np.mgrid[0:size, 0:size] / size
    r = np.sqrt((xx - .5) ** 2 + (yy - .5) ** 2) * 2
    m = smooth(1.0, 0.15, r + (h - .5) * 0.45) * smooth(spread, spread + .2, h)
    a = np.clip(m * alpha, 0, 1)
    img = np.zeros((size, size, 4), np.uint8)
    img[..., 0], img[..., 1], img[..., 2] = color
    img[..., 3] = (a * 255).astype(np.uint8)
    return Image.fromarray(img, "RGBA").filter(ImageFilter.GaussianBlur(1.2))


def puddle(size=512, seed=4):
    h = fbm((size, size), seed, scales=(7, 3, 2), weights=(1, .45, .2))
    yy, xx = np.mgrid[0:size, 0:size] / size
    r = np.sqrt((xx - .5) ** 2 + (yy - .5) ** 2) * 2
    edge = 1.0 - smooth(0.35, 0.95, r + (h - .5) * .55)
    img = np.zeros((size, size, 4), np.uint8)
    img[..., 0] = 26; img[..., 1] = 31; img[..., 2] = 33
    img[..., 3] = (edge * 0.8 * 255).astype(np.uint8)
    im = Image.fromarray(img, "RGBA").filter(ImageFilter.GaussianBlur(1.5))
    return im


def worn_line(w=1024, h=64, color=(186, 146, 74)):
    a = np.ones((h, w), np.float32)
    grit = fbm((h, w), 12, scales=(40, 14, 6), weights=(1, .5, .3))
    a *= smooth(.25, .55, grit)
    a[:, :4] = 0; a[:, -4:] = 0
    img = np.zeros((h, w, 4), np.uint8)
    img[..., 0], img[..., 1], img[..., 2] = color
    img[..., 3] = (a * .85 * 255).astype(np.uint8)
    return Image.fromarray(img, "RGBA")


def grate(size=256):
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.rounded_rectangle([6, 6, size - 6, size - 6], 10, fill=(38, 39, 40, 255))
    for i in range(7):
        x = 22 + i * 34
        d.rounded_rectangle([x, 16, x + 13, size - 16], 3, fill=(14, 15, 16, 255))
    d.rounded_rectangle([6, 6, size - 6, size - 6], 10, outline=(64, 60, 55, 255), width=3)
    return img


def rust(w=256, h=512):
    n = fbm((h, w), 21, scales=(16, 7, 3), weights=(1, .55, .3))
    y = np.linspace(0, 1, h)[:, None]
    a = smooth(0.55, 0.85, n) * (1.0 - smooth(0.05, 0.78, y))
    a = np.clip(a * 2.2, 0, 1)
    img = np.zeros((h, w, 4), np.uint8)
    img[..., 0] = 96; img[..., 1] = 58; img[..., 2] = 36
    img[..., 3] = (a * 255).astype(np.uint8)
    return Image.fromarray(img, "RGBA")


def foam(w=1024, h=96):
    n = fbm((h, w), 31, scales=(48, 18, 7), weights=(1, .5, .25))
    a = smooth(0.42, 0.78, n) * smooth(0.0, 0.35, np.linspace(0, 1, h)[:, None])
    a *= (1 - smooth(0.6, 1.0, np.linspace(0, 1, h)[:, None]))
    img = np.zeros((h, w, 4), np.uint8)
    img[..., 0] = 236; img[..., 1] = 240; img[..., 2] = 238
    img[..., 3] = (a * 255).astype(np.uint8)
    return Image.fromarray(img, "RGBA")


def water_normal(size=512):
    yy, xx = np.mgrid[0:size, 0:size] / size * 2 * np.pi
    h = np.zeros((size, size), np.float32)
    r = np.random.default_rng(9)
    for k in range(6):
        fx, fy = r.integers(1, 4), r.integers(1, 4)
        ph = r.random() * 6.28
        h += np.sin(xx * fx + yy * fy + ph) / (fx + fy)
    gx = (np.roll(h, -1, 1) - np.roll(h, 1, 1)) * 0.5
    gy = (np.roll(h, -1, 0) - np.roll(h, 1, 0)) * 0.5
    n = np.dstack([-gx, -gy, np.ones_like(h)])
    n /= np.linalg.norm(n, axis=2, keepdims=True)
    img = (n * 0.5 + 0.5) * 255
    return Image.fromarray(img.astype(np.uint8), "RGB")


def text_sign(text, name, px=1040, color=(226, 210, 178), drawn=0.88, wear=0.5, border=False):
    """Lettere dipinte con consumo: alpha ritagliata, colore caldo, ombra di pittura."""
    fs = 220
    font = ImageFont.truetype(FONT, fs)
    # misura e centra
    tmp = Image.new("L", (8, 8))
    d = ImageDraw.Draw(tmp)
    l, t, r, b = d.textbbox((0, 0), text, font=font)
    tw, th = r - l, b - t
    scale = min((px - 80) / tw, 240 / th)
    font = ImageFont.truetype(FONT, max(24, int(fs * scale)))
    l, t, r, b = d.textbbox((0, 0), text, font=font)
    W = max(px, r - l + 96)
    H = 256
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    x = (W - (r - l)) // 2 - l
    y = (H - (b - t)) // 2 - t
    d.text((x, y), text, font=font, fill=color + (255,), stroke_width=3, stroke_fill=(46, 40, 33, 150))
    a = np.array(img)
    # consumo: buchi di pittura caduta e graffi verticali
    n = fbm((H, W), hash(name) % 1000, scales=(30, 11, 4), weights=(1, .5, .3))
    keep = smooth(1.0 - drawn - 0.10, 1.0 - drawn + 0.18, n)
    a[..., 3] = (a[..., 3] * keep).astype(np.uint8)
    img = Image.fromarray(a, "RGBA")
    if border:
        d = ImageDraw.Draw(img)
        d.rectangle([10, 10, W - 11, H - 11], outline=color + (220,), width=4)
    save(img, name)


if __name__ == "__main__":
    save(blotch(512, 1, (38, 30, 24), .6, .62), "olio_1.png")
    save(blotch(512, 8, (48, 40, 30), .68, .5), "olio_2.png")
    save(puddle(512, 4), "pozzanghera_1.png")
    save(puddle(512, 14), "pozzanghera_2.png")
    save(worn_line(), "linea_corsia.png")
    save(grate(), "griglia.png")
    save(rust(), "ruggine.png")
    save(foam(), "schiuma.png")
    save(water_normal(), "acqua_n.png")
    text_sign("BAR DEL PORTO", "insegna_bar.png", 900)
    text_sign("GELO", "insegna_gelo.png", 420)
    text_sign("PESCHERIA", "insegna_pescheria.png", 760)
    text_sign("CAPITANERIA DI PORTO", "insegna_capitania.png", 980)
    text_sign("OFFICINA NAUTICA", "insegna_officina.png", 940)
    text_sign("DOGANA", "insegna_dogana.png", 420)
    text_sign("TRATTORIA", "insegna_trattoria.png", 620)
    text_sign("PORTO VECCHIO", "targa_porto.png", 640, color=(232, 226, 208), border=True)
    text_sign("CALANERA   PV 047", "stencil_calanera.png", 900, color=(226, 224, 214), drawn=0.91)
    text_sign("FALEZIA", "insegna_falezia.png", 1600, color=(150, 92, 58), drawn=0.93, wear=0.4)
