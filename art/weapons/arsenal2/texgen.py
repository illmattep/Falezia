"""
FALEZIA · art/weapons/arsenal2/texgen.py — texture PBR procedurali per l'arsenale.

Niente download: le texture si generano qui, in numpy + Pillow, e sono le stesse per
tutte le armi (una famiglia di materiale = una terna di mappe). Così 17 armi hanno
lo stesso linguaggio materico e il repository resta leggero.

Per ogni famiglia:
    <fam>_basecolor.png   2048², sRGB   (colore + usura + graffi)
    <fam>_normal.png      1024², lineare (microrilievo: ruvidità, grano, graffi)
    <fam>_orm.png          512², lineare (R = occlusione, G = roughness, B = metallo)

    python3 art/weapons/arsenal2/texgen.py
"""
import os
import numpy as np
from PIL import Image

OUT = "/home/matteo/Progetti/Falezia/game/assets/weapons/_shared/tex"
os.makedirs(OUT, exist_ok=True)


# ------------------------------------------------------------------ rumore

def value_noise(size, cells, seed):
    """Rumore a celle grandi, interpolato: la base di tutto."""
    rng = np.random.default_rng(seed)
    small = (rng.random((max(1, cells), max(1, cells))) * 255).astype(np.uint8)
    img = Image.fromarray(small).resize((size, size), Image.BICUBIC)
    return np.asarray(img, dtype=np.float32) / 255.0


def fbm(size, base_cells=4, octaves=5, seed=0, gain=0.5):
    out = np.zeros((size, size), dtype=np.float32)
    amp, norm, cells = 1.0, 0.0, base_cells
    for o in range(octaves):
        out += amp * value_noise(size, cells, seed + 37 * o)
        norm += amp
        amp *= gain
        cells *= 2
    return out / norm


def stretched(size, cx, cy, seed):
    """Rumore stirato: serve per le righe di lavorazione e i graffi."""
    rng = np.random.default_rng(seed)
    small = (rng.random((max(1, cy), max(1, cx))) * 255).astype(np.uint8)
    img = Image.fromarray(small).resize((size, size), Image.BICUBIC)
    return np.asarray(img, dtype=np.float32) / 255.0


def smoothstep(a, b, x):
    t = np.clip((x - a) / max(1e-6, (b - a)), 0.0, 1.0)
    return t * t * (3.0 - 2.0 * t)


def normal_from_height(height, strength=0.35):
    """Normali da una mappa di altezza (Sobel), nel formato OpenGL (verde su)."""
    dx = np.roll(height, -1, axis=1) - np.roll(height, 1, axis=1)
    dy = np.roll(height, -1, axis=0) - np.roll(height, 1, axis=0)
    nx = -dx * strength * 8.0
    ny = -dy * strength * 8.0
    nz = np.ones_like(height)
    ln = np.sqrt(nx * nx + ny * ny + nz * nz)
    normal = np.stack([nx / ln, ny / ln, nz / ln], axis=-1)
    return ((normal * 0.5 + 0.5) * 255).clip(0, 255).astype(np.uint8)


def save(name, rgb, size=None, resample=Image.LANCZOS):
    """Salva un array HxWx3 in 0..1 (o 0..255) come PNG della dimensione voluta."""
    arr = rgb
    if arr.dtype != np.uint8:
        arr = (np.clip(arr, 0.0, 1.0) * 255).astype(np.uint8)
    img = Image.fromarray(arr, "RGB")
    if size and img.size[0] != size:
        img = img.resize((size, size), resample)
    path = os.path.join(OUT, name)
    img.save(path, optimize=True)
    print("TEX", name, img.size, os.path.getsize(path) // 1024, "KB")


# ------------------------------------------------------------------ famiglie

def build(name, base, metal, rough_lo, rough_hi, kind, seed, size=1024, nrm=512, orm=256):
    """
    kind:
      'polymer'  → puntinatura fine, nessun graffio
      'stipple'  → puntinatura grossa (guancette, impugnature)
      'steel'    → righe di lavorazione + graffi radi
      'brushed'  → spazzolato fitto (acciaio inox)
      'wood'     → venature lunghe
      'paint'    → vernice che si sfoglia sul metallo
      'cloth'    → trama tessuta
      'glass'    → quasi liscia
    """
    base = np.array(base, dtype=np.float32)
    grain = fbm(size, 3, 5, seed)
    fine = fbm(size, 64, 3, seed + 11)

    height = np.zeros((size, size), dtype=np.float32)
    rough = np.full((size, size), (rough_lo + rough_hi) * 0.5, dtype=np.float32)
    metal_map = np.full((size, size), metal, dtype=np.float32)
    color = np.tile(base, (size, size, 1))
    ao = np.ones((size, size), dtype=np.float32)

    if kind in ("polymer", "stipple"):
        cells = 380 if kind == "polymer" else 250
        dots = value_noise(size, cells, seed + 3)
        dots = smoothstep(0.62, 0.90, dots)
        height += dots * (0.05 if kind == "polymer" else 0.16)
        color *= (0.92 + 0.16 * grain)[..., None]
        rough += (grain - 0.5) * 0.16 + dots * 0.10
        ao -= dots * 0.10

    elif kind in ("steel", "brushed"):
        lines = stretched(size, 2 if kind == "brushed" else 3, 1400 if kind == "brushed" else 700, seed + 5)
        height += lines * (0.06 if kind == "brushed" else 0.04)
        # graffi radi: rumore stirato molto contrastato
        scratches = stretched(size, 2, 1600, seed + 9)
        scratches = smoothstep(0.80, 0.95, scratches)
        height -= scratches * 0.06
        color *= (1.0 - scratches[..., None] * 0.35)
        color *= (0.90 + 0.20 * grain)[..., None]
        rough += (grain - 0.5) * 0.12 + scratches * 0.15
        ao -= scratches * 0.25

    elif kind == "wood":
        # venature: onde lunghe lungo X, con anelli più scuri
        xs = np.linspace(0.0, 1.0, size, dtype=np.float32)[:, None]
        warp = fbm(size, 6, 4, seed + 13) * 0.02
        rings = np.sin((xs * 6.0 + warp) * np.pi * 11.0) * 0.5 + 0.5
        rings = rings ** 1.6
        grain2 = fbm(size, 12, 4, seed + 21)
        color = (base[None, None, :] * (0.62 + 0.62 * rings)[..., None]
                 * (0.85 + 0.30 * grain2)[..., None]).reshape(size, size, 3)
        height += rings * 0.12 + grain2 * 0.06
        rough += (rings - 0.5) * 0.18
        ao -= (1.0 - rings) * 0.12

    elif kind == "paint":
        # vernice con scrostature: il metallo sotto è più chiaro e più metallico
        chip = fbm(size, 90, 4, seed + 7)
        chip = smoothstep(0.80, 0.90, chip) * 0.75
        wear = fbm(size, 60, 4, seed + 31)
        wear = smoothstep(0.62, 0.88, wear) * 0.5
        metal_color = np.array([0.14, 0.145, 0.15], dtype=np.float32)
        color = (color * (1.0 - chip[..., None]) + metal_color * chip[..., None])
        color *= (0.94 + 0.10 * grain)[..., None]
        metal_map = metal * (1.0 - chip) + 1.0 * chip
        rough += (chip * 0.12) - 0.08 + (grain - 0.5) * 0.10 + wear * 0.08
        height += (1.0 - chip) * 0.06 + grain * 0.02
        ao -= chip * 0.25

    elif kind == "cloth":
        weave = (np.sin(np.linspace(0, 1, size) * np.pi * 420)[None, :]
                 * np.sin(np.linspace(0, 1, size) * np.pi * 420)[:, None])
        height += (weave * 0.5 + 0.5) * 0.12 + fine * 0.06
        color *= (0.92 + 0.14 * fine)[..., None]
        rough += (fine - 0.5) * 0.2

    elif kind == "glass":
        color *= (0.96 + 0.06 * fine)[..., None]
        height += fine * 0.05

    # sporco e polvere nelle cavità: abbassa appena la luce e alza la ruvidità
    dirt = fbm(size, 5, 4, seed + 41)
    dirt = smoothstep(0.55, 0.95, dirt)
    color *= (1.0 - dirt[..., None] * 0.18)
    rough += dirt * 0.08
    ao -= dirt * 0.12

    rough = np.clip(rough, 0.04, 0.95)
    metal_map = np.clip(metal_map, 0.0, 1.0)
    ao = np.clip(ao, 0.35, 1.0)

    save(f"{name}_basecolor.png", color, size)
    save(f"{name}_normal.png", normal_from_height(height, 0.50), nrm)
    orm_arr = np.stack([ao, rough, metal_map], axis=-1)
    save(f"{name}_orm.png", orm_arr, orm, Image.BOX)


def main():
    # colore, metallo, rough min/max, tipo, seme
    build("polymer", [0.075, 0.078, 0.085], 0.0, 0.46, 0.66, "polymer", 11)
    build("stipple", [0.062, 0.065, 0.070], 0.0, 0.55, 0.80, "stipple", 23)
    build("steel", [0.125, 0.130, 0.140], 0.45, 0.36, 0.56, "steel", 37)
    build("stainless", [0.44, 0.45, 0.47], 1.0, 0.22, 0.40, "brushed", 41)
    build("wood", [0.340, 0.205, 0.130], 0.0, 0.34, 0.58, "wood", 53)
    build("sheet", [0.185, 0.196, 0.180], 0.30, 0.46, 0.68, "paint", 67)
    build("green", [0.185, 0.215, 0.170], 0.0, 0.44, 0.68, "polymer", 71)
    build("brass", [0.62, 0.46, 0.17], 1.0, 0.22, 0.40, "steel", 83, 512, 256, 128)
    build("glass", [0.055, 0.075, 0.095], 0.75, 0.03, 0.10, "glass", 89, 256, 128, 128)
    build("cloth", [0.10, 0.10, 0.09], 0.0, 0.80, 0.95, "cloth", 97, 512, 256, 128)
    # "void": il nero delle asole, delle feritoie, delle anime. Non è un materiale,
    # è l'ombra dove la geometria è stata tagliata via.
    build("void", [0.010, 0.010, 0.011], 0.0, 0.90, 0.96, "glass", 101, 128, 64, 64)


if __name__ == "__main__":
    main()
