#!/usr/bin/env python3
"""
Cielo di Calanera: genera una panorama equirettangolare 2:1 senza scaricare niente.

Serve come cielo di default del gioco (Sketchfab richiede il login per il download, e il
repository deve restare riproducibile). Se poi metti una panorama tua in
`game/assets/sky/`, la tua vince: questo file è l'ultimo della lista.

Il sole è nella stessa direzione della luce direzionale del gioco (Ovest, 42° di altezza):
così ombre e cielo raccontano la stessa ora.

    python3 tools/skybox/make_sky.py                     # 4096x2048 in game/assets/sky/
    python3 tools/skybox/make_sky.py --width 2048 --out /tmp/prova.jpg
"""
from __future__ import annotations

import argparse
import math
import sys
from pathlib import Path

import numpy as np
from PIL import Image

# --- il sole: stessa direzione di DirectionalLight3D (-42°, -90°) in MapView.cs
SUN = np.array([-0.743, 0.669, 0.0])
SUN = SUN / np.linalg.norm(SUN)

ZENITH = np.array([0.24, 0.45, 0.74])     # blu profondo allo zenit
HORIZON = np.array([0.76, 0.83, 0.88])    # foschia chiara all'orizzonte
SEA = np.array([0.35, 0.45, 0.50])        # sotto l'orizzonte: mare/lontananza
SUN_COLOR = np.array([1.00, 0.93, 0.78])


def directions(width: int) -> np.ndarray:
    """Direzioni (x, y, z) per ogni pixel: y = su, -z = avanti."""
    h = width // 2
    u = (np.arange(width, dtype=np.float32) + 0.5) / width
    v = (np.arange(h, dtype=np.float32) + 0.5) / h
    phi = (u - 0.5) * (2 * np.pi)
    theta = (0.5 - v) * np.pi
    p, t = np.meshgrid(phi, theta)
    ct = np.cos(t)
    return np.stack([np.sin(p) * ct, np.sin(t), -np.cos(p) * ct], axis=-1)


def _hash3(ix, iy, iz, seed):
    h = (ix * 374761393 + iy * 668265263 + iz * 1442695041 + seed * 2654435761) & 0xFFFFFFFF
    h = ((h ^ (h >> 13)) * 1274126177) & 0xFFFFFFFF
    h = h ^ (h >> 16)
    return (h & 0xFFFFFF).astype(np.float32) / 16777216.0


def value_noise(p: np.ndarray, seed: int) -> np.ndarray:
    """Rumore 3D con interpolazione morbida: niente cuciture ai poli o a 360°."""
    i = np.floor(p)
    f = p - i
    f = f * f * (3.0 - 2.0 * f)
    ix, iy, iz = i[..., 0].astype(np.int64), i[..., 1].astype(np.int64), i[..., 2].astype(np.int64)
    c = {}
    for dx in (0, 1):
        for dy in (0, 1):
            for dz in (0, 1):
                c[(dx, dy, dz)] = _hash3(ix + dx, iy + dy, iz + dz, seed)
    fx, fy, fz = f[..., 0], f[..., 1], f[..., 2]
    x00 = c[(0, 0, 0)] * (1 - fx) + c[(1, 0, 0)] * fx
    x10 = c[(0, 1, 0)] * (1 - fx) + c[(1, 1, 0)] * fx
    x01 = c[(0, 0, 1)] * (1 - fx) + c[(1, 0, 1)] * fx
    x11 = c[(0, 1, 1)] * (1 - fx) + c[(1, 1, 1)] * fx
    y0 = x00 * (1 - fy) + x10 * fy
    y1 = x01 * (1 - fy) + x11 * fy
    return y0 * (1 - fz) + y1 * fz


def fbm(p: np.ndarray, octaves: int = 5, seed: int = 7) -> np.ndarray:
    total = np.zeros(p.shape[:-1], np.float32)
    amp, norm, freq = 1.0, 0.0, 1.0
    for o in range(octaves):
        total += value_noise(p * freq, seed + o * 17) * amp
        norm += amp
        amp *= 0.5
        freq *= 2.0
    return total / norm


def smoothstep(a, b, x):
    t = np.clip((x - a) / (b - a), 0.0, 1.0)
    return t * t * (3.0 - 2.0 * t)


def make(width: int) -> Image.Image:
    d = directions(width)
    h = width // 2
    y = d[..., 1]
    up = np.clip(y, 0.0, 1.0)

    # --- gradiente del cielo: più chiaro all'orizzonte, blu in alto
    t = smoothstep(0.0, 0.65, up) ** 0.85
    sky = HORIZON[None, None, :] * (1 - t[..., None]) + ZENITH[None, None, :] * t[..., None]

    # --- sotto l'orizzonte: mare, con la foschia che sale
    below = y < 0
    fade = np.clip(-y * 6.0, 0.0, 1.0)[..., None]
    sea = SEA[None, None, :] * (1 - fade) + (SEA * 0.75)[None, None, :] * fade
    sky = np.where(below[..., None], sea, sky)

    # --- foschia all'orizzonte: una fascia chiara che unisce cielo e mare
    haze = np.exp(-((y / 0.065) ** 2)) * 0.42
    sky = sky * (1 - haze[..., None]) + np.array([0.86, 0.89, 0.90])[None, None, :] * haze[..., None]

    # --- sole: disco e alone
    cosang = np.clip((d * SUN).sum(-1), -1.0, 1.0)
    ang = np.arccos(cosang)
    disk = smoothstep(0.0062, 0.0045, ang)                 # ~0,5° di diametro
    glow = np.exp(-(ang / 0.13) ** 2) * 0.34 + np.exp(-(ang / 0.45) ** 2) * 0.11
    sky = sky + SUN_COLOR[None, None, :] * (disk * 2.0 + glow)[..., None]

    # --- nuvole: fBm a bassa risoluzione, poi ingrandite (morbide per costruzione)
    cw = max(256, width // 4)
    cd = directions(cw)
    p = cd * 2.6
    p[..., 1] *= 1.9                                        # schiacciate: strati, non palline
    n = fbm(p, octaves=5, seed=11)
    cover = smoothstep(0.55, 0.78, n) * smoothstep(-0.02, 0.14, cd[..., 1])
    cover *= 0.62
    cloud_img = Image.fromarray((np.clip(cover, 0, 1) * 255).astype(np.uint8), mode="L")
    cover = np.asarray(cloud_img.resize((width, h), Image.BICUBIC), dtype=np.float32) / 255.0

    # le nuvole si illuminano dal lato del sole
    lit = 0.55 + 0.45 * np.clip(cosang, 0, 1) ** 0.5
    cloud_col = np.array([0.95, 0.96, 0.97])[None, None, :] * lit[..., None]
    sky = sky * (1 - cover[..., None]) + cloud_col * cover[..., None]

    return Image.fromarray((np.clip(sky, 0.0, 1.0) * 255.0 + 0.5).astype(np.uint8))


def main(argv=None) -> int:
    root = Path(__file__).resolve().parents[2]
    ap = argparse.ArgumentParser(description="Genera la panorama del cielo di Calanera")
    ap.add_argument("--width", type=int, default=4096)
    ap.add_argument("--out", type=Path, default=root / "game/assets/sky/sky_panorama.jpg")
    args = ap.parse_args(argv)

    img = make(args.width)
    args.out.parent.mkdir(parents=True, exist_ok=True)
    img.save(args.out, quality=92, subsampling=0)
    print(f"scritto {args.out} · {img.size[0]}x{img.size[1]}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
