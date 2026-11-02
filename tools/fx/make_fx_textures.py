#!/usr/bin/env python3
"""
Texture del fumo per le armi: due immagini, una per il fumo di canna e una per la scia.

Sono **alpha** (bianco + trasparenza): il colore lo decide lo shader, così la stessa
texture serve per il fumo chiaro della Standard e per quello tenue della Hush.

    smoke_puff_1..4.png   512×512   quattro sbuffi di forma diversa
    smoke_wisp_1..3.png   256×512   tre strisce cucite in orizzontale, per la scia

    python3 tools/fx/make_fx_textures.py
"""
from __future__ import annotations

import argparse
import sys
from pathlib import Path

import numpy as np
from PIL import Image


def _hash(ix, iy, iz, seed):
    h = (ix * 374761393 + iy * 668265263 + iz * 1442695041 + seed * 2654435761) & 0xFFFFFFFF
    h = ((h ^ (h >> 13)) * 1274126177) & 0xFFFFFFFF
    h = h ^ (h >> 16)
    return (h & 0xFFFFFF).astype(np.float32) / 16777216.0


def noise3(p, seed):
    """Rumore 3D con interpolazione morbida (niente griglie visibili)."""
    i = np.floor(p)
    f = p - i
    f = f * f * (3.0 - 2.0 * f)
    ix, iy, iz = (i[..., k].astype(np.int64) for k in range(3))
    c = {}
    for dx in (0, 1):
        for dy in (0, 1):
            for dz in (0, 1):
                c[(dx, dy, dz)] = _hash(ix + dx, iy + dy, iz + dz, seed)
    fx, fy, fz = f[..., 0], f[..., 1], f[..., 2]
    x00 = c[(0, 0, 0)] * (1 - fx) + c[(1, 0, 0)] * fx
    x10 = c[(0, 1, 0)] * (1 - fx) + c[(1, 1, 0)] * fx
    x01 = c[(0, 0, 1)] * (1 - fx) + c[(1, 0, 1)] * fx
    x11 = c[(0, 1, 1)] * (1 - fx) + c[(1, 1, 1)] * fx
    y0 = x00 * (1 - fy) + x10 * fy
    y1 = x01 * (1 - fy) + x11 * fy
    return y0 * (1 - fz) + y1 * fz


def fbm(p, octaves=5, seed=3):
    total = np.zeros(p.shape[:-1], np.float32)
    amp, norm, freq = 1.0, 0.0, 1.0
    for o in range(octaves):
        total += noise3(p * freq, seed + o * 31) * amp
        norm += amp
        amp *= 0.5
        freq *= 2.0
    return total / norm


def smoothstep(a, b, x):
    t = np.clip((x - a) / (b - a), 0.0, 1.0)
    return t * t * (3.0 - 2.0 * t)


def puff(size=512, seed=17, ax=2.3, ay=2.3, lo=0.30, hi=0.72, core=0.35):
    """
    Uno sbuffo. I parametri cambiano FORMA, non solo il seme: ax/ay allungano il
    rumore in orizzontale o verticale, lo/hi decidono quanti buchi ha, core quanto
    è pieno al centro. Le varianti servono a non vedere mai lo stesso fumo due volte.
    """
    y, x = np.mgrid[0:size, 0:size].astype(np.float32) / size
    dx = (x - 0.5) * 2.0
    dy = (y - 0.5) * 2.0
    r = np.sqrt(dx * dx + dy * dy)

    p = np.stack([dx * ax, dy * ay, np.full_like(dx, 0.7)], axis=-1)
    base = fbm(p, octaves=5, seed=seed)
    fine = fbm(p * 2.6 + 11.0, octaves=4, seed=seed + 5)

    body = smoothstep(1.0, 0.10, r)
    wisps = smoothstep(lo, hi, base * 0.7 + fine * 0.3)
    alpha = body * (core + (1.0 - core) * wisps)
    alpha *= 1.0 - smoothstep(0.86, 1.0, r)      # niente spigoli

    a8 = (np.clip(alpha, 0, 1) ** 0.9 * 255).astype(np.uint8)
    rgb = np.full((size, size, 3), 255, np.uint8)
    return Image.fromarray(np.dstack([rgb, a8]), mode="RGBA")


def wisp(width=256, height=512, seed=29, radius=1.15, vscale=3.4, lo=0.44, hi=0.78):
    """Striscia cucita in orizzontale: rumore campionato su un cilindro."""
    y, x = np.mgrid[0:height, 0:width].astype(np.float32)
    u = x / width
    v = y / height
    ang = u * 2.0 * np.pi
    p = np.stack([np.cos(ang) * radius, np.sin(ang) * radius, v * vscale], axis=-1)

    base = fbm(p, octaves=5, seed=seed)
    fine = fbm(p * 2.2 + 7.0, octaves=4, seed=seed + 3)

    # vene: si assottigliano verso la punta, più dense all'attacco
    veins = smoothstep(lo, hi, base * 0.75 + fine * 0.25)
    taper = (1.0 - smoothstep(0.55, 1.0, v)) * smoothstep(0.0, 0.04, v)
    alpha = veins * taper

    a8 = (np.clip(alpha, 0, 1) * 255).astype(np.uint8)
    rgb = np.full((height, width, 3), 255, np.uint8)
    return Image.fromarray(np.dstack([rgb, a8]), mode="RGBA")


def main(argv=None) -> int:
    root = Path(__file__).resolve().parents[2]
    ap = argparse.ArgumentParser(description="Texture del fumo delle armi")
    ap.add_argument("--out", type=Path, default=root / "game/assets/textures/fx")
    args = ap.parse_args(argv)

    args.out.mkdir(parents=True, exist_ok=True)

    # Quattro sbuffi diversi per forma, non solo per seme: uno tondo e pieno, uno
    # allungato, uno molto bucherellato, uno rado. Il client ne pesca uno a caso.
    puffs = [
        dict(seed=17, ax=2.3, ay=2.3, lo=0.30, hi=0.72, core=0.35),
        dict(seed=41, ax=1.7, ay=3.1, lo=0.36, hi=0.80, core=0.22),   # alto e stretto
        dict(seed=73, ax=3.2, ay=1.6, lo=0.28, hi=0.66, core=0.40),   # largo e basso
        dict(seed=101, ax=2.6, ay=2.6, lo=0.45, hi=0.86, core=0.15),  # rado, quasi solo vene
    ]
    for i, kw in enumerate(puffs, 1):
        puff(**kw).save(args.out / f"smoke_puff_{i}.png")

    wisps = [
        dict(seed=29, radius=1.15, vscale=3.4, lo=0.44, hi=0.78),
        dict(seed=57, radius=1.40, vscale=4.6, lo=0.38, hi=0.74),
        dict(seed=83, radius=1.00, vscale=2.6, lo=0.50, hi=0.84),
    ]
    for i, kw in enumerate(wisps, 1):
        wisp(**kw).save(args.out / f"smoke_wisp_{i}.png")

    print(f"scritte {len(puffs)} varianti di sbuffo e {len(wisps)} di scia in {args.out}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
