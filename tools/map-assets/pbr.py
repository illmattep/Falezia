"""Da albedo a set PBR completo: normal + roughness (+ height intermedio).

Perché a livello di height: fondere due normal map pesate dà normali sbagliate (si sommano
vettori non ortogonali). Si fondono le ALTEZZE, poi si ricava la normale dal risultato:
così la normale descrive davvero la superficie finale.
"""
import numpy as np
from PIL import Image, ImageFilter

TEX = "/home/matteo/Progetti/Falezia/Assets/Resources/MapTex"

def load_rgb(name):
    return np.asarray(Image.open(f"{TEX}/{name}").convert("RGB"), np.float32) / 255.0

def save_gray(a, name):
    Image.fromarray((np.clip(a, 0, 1) * 255).astype(np.uint8)).save(f"{TEX}/{name}")

def save_rgb(a, name):
    Image.fromarray((np.clip(a, 0, 1) * 255).astype(np.uint8)).save(f"{TEX}/{name}")

def height_from_albedo(alb, blur=1.1, invert=False):
    """Altezza approssimata: luminanza levigata. Il rilievo viene dai bordi, non dal colore."""
    h = alb[..., 0] * 0.299 + alb[..., 1] * 0.587 + alb[..., 2] * 0.114
    im = Image.fromarray((np.clip(h, 0, 1) * 255).astype(np.uint8))
    im = im.filter(ImageFilter.GaussianBlur(blur))
    h = np.asarray(im, np.float32) / 255.0
    if invert:
        h = 1.0 - h
    return (h - h.min()) / max(1e-5, h.max() - h.min())

def normal_from_height(h, strength=2.2):
    """Normale tangente (OpenGL, Y+). `strength` alto per superfici a basso contrasto."""
    """Normale tangente (OpenGL, Y+): gradiente centrale."""
    dx = (np.roll(h, -1, axis=1) - np.roll(h, 1, axis=1)) * 0.5
    dy = (np.roll(h, -1, axis=0) - np.roll(h, 1, axis=0)) * 0.5
    nx = -dx * strength
    ny = -dy * strength
    nz = np.ones_like(h)
    ln = np.sqrt(nx * nx + ny * ny + nz * nz)
    n = np.stack([nx / ln, ny / ln, nz / ln], axis=-1)
    return n * 0.5 + 0.5

def roughness_map(alb, base=0.90, variation=0.06, pattern=None):
    """Rugosità: base per famiglia, con variazione dove la superficie è più chiara/scura.
    `pattern` (0..1) spinge certe zone più ruvide (giunti, mortaio)."""
    lum = alb[..., 0] * 0.299 + alb[..., 1] * 0.587 + alb[..., 2] * 0.114
    r = base + (0.5 - lum) * variation * 2.0
    if pattern is not None:
        r = r * (1.0 - pattern) + np.clip(base + 0.08, 0, 1) * pattern
    return np.clip(r, 0.05, 1.0)

def load_height(name):
    """Se il disegno ha prodotto una mappa di altezza, si usa quella: è il rilievo vero."""
    fn = f"{TEX}/{name}_h.png"
    import os
    if not os.path.exists(fn):
        return None
    a = np.asarray(Image.open(fn).convert("L"), np.float32) / 255.0
    return (a - a.min()) / max(1e-5, a.max() - a.min())

def build(name, base_rough=0.90, variation=0.06, normal_strength=8.0, invert=False, pattern=None):
    alb = load_rgb(name + ".jpg") if _has(name + ".jpg") else load_rgb(name + ".png")
    h = load_height(name)
    if h is None:
        h = height_from_albedo(alb, invert=invert)
    n = normal_from_height(h, normal_strength)
    r = roughness_map(alb, base_rough, variation, pattern)
    save_rgb(n, name + "_n.jpg")
    save_gray(r, name + "_r.jpg")
    return h, r

def _has(fn):
    import os
    return os.path.exists(f"{TEX}/{fn}")
