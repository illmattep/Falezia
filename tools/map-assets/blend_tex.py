"""Fusione di texture in stile dipinto.

Perché serve: mettere due texture "una accanto all'altra" produce una cucitura netta e un
ritmo visibile a ogni tassello. Qui si compone UNA texture: base + varianti + danni, con
maschere di rumore **tileable** a due scale (larga per le macchie, fine per la grana).

Le maschere sono costruite con lo stesso tassello a specchio delle texture, quindi anche la
fusione si ripete senza bordi visibili.
"""
import numpy as np
from PIL import Image, ImageFilter

TEX = "/home/matteo/Progetti/Falezia/Assets/Resources/MapTex"

def _noise(size, freq, oct=4, seed=0):
    r = np.random.default_rng(seed)
    tot = np.zeros((size, size), np.float32); amp = 1.0; f = freq
    for _ in range(oct):
        g = r.random((f + 1, f + 1)).astype(np.float32)
        xs = np.linspace(0, f, size, endpoint=False); ys = np.linspace(0, f, size, endpoint=False)
        x0 = xs.astype(int); y0 = ys.astype(int)
        fx = (xs - x0)[None, :]; fy = (ys - y0)[:, None]
        x1 = (x0 + 1) % f; y1 = (y0 + 1) % f
        a = g[y0][:, x0]; b = g[y0][:, x1]
        c = g[y1][:, x0]; d = g[y1][:, x1]
        tot += ((a * (1 - fx) + b * fx) * (1 - fy) + (c * (1 - fx) + d * fx) * fy) * amp
        amp *= 0.5; f *= 2
    return tot / tot.max()

def seamless(a):
    """Tassello a specchio: elimina le cuciture ai bordi."""
    h, w = a.shape[:2]; q = a[:h // 2, :w // 2]
    return np.concatenate([np.concatenate([q, q[:, ::-1]], 1),
                           np.concatenate([q[::-1, :], q[::-1, ::-1]], 1)], 0)

def load(name, size=1024):
    im = Image.open(f"{TEX}/{name}.jpg").convert("RGB")
    if im.size[0] != size:
        im = im.resize((size, size), Image.LANCZOS)
    return np.asarray(im, np.float32) / 255.0

def save(a, name):
    Image.fromarray((np.clip(a, 0, 1) * 255).astype(np.uint8)) \
         .save(f"{TEX}/{name}.jpg", quality=90)

def load_h(name):
    import os
    fn = f"{TEX}/{name}_h.png"
    if not os.path.exists(fn):
        return None
    a = np.asarray(Image.open(fn).convert("L"), np.float32) / 255.0
    return (a - a.min()) / max(1e-5, a.max() - a.min())

def save_h(h, name):
    Image.fromarray((np.clip(h, 0, 1) * 255).astype(np.uint8)).save(f"{TEX}/{name}_h.png")

def blend(base, over, mask):
    """base e over: (S,S,3); mask: (S,S,1) in 0..1."""
    return base * (1 - mask) + over * mask

def mask_patches(size, seed, coverage=0.18, edge=1.4, freq=5):
    """Macchie irregolari, bordi morbidi: la maschera che fa sembrare il danno dipinto."""
    n = _noise(size, freq, 4, seed)
    thr = np.quantile(n, 1.0 - coverage)
    m = np.clip((n - thr) / max(1e-3, edge * (1.0 - thr)), 0, 1)
    m = np.array(Image.fromarray((m * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(0.9)), np.float32) / 255.0
    return m[:, :, None]

def mask_streaks(size, seed, strength=0.5):
    """Colature verticali, rade."""
    n = _noise(size, 7, 3, seed)
    n = np.repeat(n[:1, :], size, 0) * 0.6 + n * 0.4
    n = (n - n.min()) / (n.max() - n.min())
    return n[:, :, None] * strength

def macro(size, seed, amount=0.16):
    """Variazione larga di luminosità: rompe la ripetizione del tassello."""
    n = _noise(size, 3, 4, seed)
    return (1.0 + (n - 0.5) * 2 * amount)[:, :, None]
