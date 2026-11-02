#!/usr/bin/env python3
"""Texture della mappa di Calanera — generate in codice, tileable, stile Valorant.

Perché generate e non fotografiche: la mappa è un graybox con volumi enormi e
ripetuti; una texture fotografica su un muro di 30 m diventa un tappeto di piastrelle
riconoscibili. Qui ogni materiale è *più grande del dettaglio*: colore piatto, macchie
larghe, fughe sottili. Si legge da lontano, non disturba, e non ha problemi di licenza.

Uso:  python3 tools/map/make_map_textures.py
Esce in Assets/Resources/MapTex/<kind>.png (512×512, RGB, tileable).
"""
from __future__ import annotations

import os
import numpy as np
from PIL import Image, ImageFilter

SIZE = 512
OUT = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "Resources", "MapTex")
rng = np.random.default_rng(20261021)


# ---------------------------------------------------------------- rumore periodico

def fourier_noise(size: int, octaves: tuple[float, ...], seed: int = 0) -> np.ndarray:
    """Rumore tileable: somma di sinusoidi con frequenze intere (periodo esatto = size)."""
    r = np.random.default_rng(seed)
    yy, xx = np.mgrid[0:size, 0:size] / size
    out = np.zeros((size, size), np.float32)
    amp = 1.0
    total = 0.0
    for octave, freq in enumerate(octaves):
        f = int(max(1, freq))
        for _ in range(3):
            ax, ay = r.integers(1, f + 1, size=2)
            phase = r.random() * 2 * np.pi
            out += amp * np.sin(2 * np.pi * (ax * xx + ay * yy) + phase)
            total += amp
        amp *= 0.55
    out /= max(total, 1e-6)
    return (out - out.min()) / max(1e-6, out.max() - out.min())


def smoothstep(a, b, x):
    t = np.clip((x - a) / np.maximum(1e-6, b - a), 0.0, 1.0)
    return t * t * (3 - 2 * t)


def fbm(size: int, seed: int, octaves=(3, 6, 12, 24, 48)) -> np.ndarray:
    return fourier_noise(size, octaves, seed)


# ---------------------------------------------------------------- pezzi comuni

def blocks(size: int, rows: int, cols: int, mortar: float, seed: int):
    """Muratura a corsi sfalsati. Restituisce (maschera_fuga, profondità 0..1)."""
    yy, xx = np.mgrid[0:size, 0:size].astype(np.float32)
    row_h = size / rows
    col_w = size / cols
    off = np.where(((yy // row_h).astype(int) % 2) == 0, 0.0, col_w * 0.5)

    fy = (yy % row_h) / row_h
    fx = ((xx + off) % col_w) / col_w

    edge_y = np.minimum(fy, 1 - fy) * row_h
    edge_x = np.minimum(fx, 1 - fx) * col_w
    edge = np.minimum(edge_x, edge_y)

    m = 1.0 - smoothstep(mortar * 0.5, mortar, edge)
    depth = smoothstep(0.0, mortar * 3.0, edge)
    return m, depth


def planks(size: int, rows: int, seed: int):
    yy = np.mgrid[0:size, 0:size][0].astype(np.float32)
    row_h = size / rows
    fy = (yy % row_h) / row_h
    seam = 1.0 - smoothstep(0.0, 0.06, np.minimum(fy, 1 - fy))
    return seam


def tint(base, amount, noise):
    return base * (1.0 + (noise - 0.5) * amount)


def to_png(name: str, rgb: np.ndarray, blur: float = 0.0):
    img = Image.fromarray(np.clip(rgb * 255, 0, 255).astype(np.uint8), "RGB")
    if blur > 0:
        img = img.filter(ImageFilter.GaussianBlur(blur))
    os.makedirs(OUT, exist_ok=True)
    img.save(os.path.join(OUT, name + ".png"))
    print(f"  {name}.png")


# ---------------------------------------------------------------- materiali

def make_plaster(name, base, seed):
    """Intonaco: colore pieno, macchie larghe, colature verticali morbide, grana fine."""
    n_large = fbm(SIZE, seed, (2, 3, 5))
    n_mid = fbm(SIZE, seed + 1, (6, 12, 24))
    n_fine = fbm(SIZE, seed + 2, (64, 128, 256))

    col = tint(np.array(base, np.float32), 0.07, n_large[..., None].repeat(3, 2))
    col *= (0.97 + 0.05 * n_mid[..., None])

    # colature: rumore basso in X, allungato in Y (non un pettine)
    streak = fbm(SIZE, seed + 3, (4, 9, 18))
    streak = np.array(Image.fromarray((streak * 255).astype(np.uint8)).resize((SIZE, 24), Image.BILINEAR)
                      .resize((SIZE, SIZE), Image.BILINEAR), np.float32) / 255
    streak = smoothstep(0.55, 0.85, streak) ** 1.5
    col *= (1.0 - 0.16 * streak[..., None])

    # macchie di umidità sparse
    damp = fbm(SIZE, seed + 6, (2, 4, 8))
    damp = smoothstep(0.70, 0.92, damp)
    col *= (1.0 - 0.10 * damp[..., None])

    col *= (0.985 + 0.03 * n_fine[..., None])
    to_png(name, col, blur=0.8)


def make_stone(name, base, seed, rows=7, cols=4, mortar=6.0):
    m, depth = blocks(SIZE, rows, cols, mortar, seed)
    n = fbm(SIZE, seed + 1, (6, 14, 30, 70))
    col = tint(np.array(base), 0.14, n[..., None].repeat(3, 2))
    col *= (0.90 + 0.14 * depth[..., None])          # le fughe sono in ombra
    col *= (1.0 - 0.32 * m[..., None])               # fuga scura
    grime = fbm(SIZE, seed + 2, (3, 7, 18))
    col *= (0.90 + 0.18 * grime[..., None])
    to_png(name, col, blur=0.5)


def make_roof(name, base, seed):
    yy, xx = np.mgrid[0:size, 0:size] if False else np.mgrid[0:SIZE, 0:SIZE]
    yy = yy.astype(np.float32); xx = xx.astype(np.float32)
    rows, cols = 9, 7
    row_h, col_w = SIZE / rows, SIZE / cols
    off = np.where(((yy // row_h).astype(int) % 2) == 0, 0.0, col_w * 0.5)
    fy = (yy % row_h) / row_h
    fx = ((xx + off) % col_w) / col_w
    body = 1.0 - np.abs(fx - 0.5) * 2.0
    shade = 0.55 + 0.45 * smoothstep(0.0, 0.35, fy) * (0.6 + 0.4 * body)
    seam = 1.0 - smoothstep(0.0, 0.09, np.minimum(fy, 1 - fy))
    n = fbm(SIZE, seed, (8, 20, 48, 96))
    col = tint(np.array(base), 0.18, n[..., None].repeat(3, 2))
    col *= shade[..., None]
    col *= (1.0 - 0.45 * seam[..., None])
    weather = fbm(SIZE, seed + 5, (4, 10, 26))
    col = col * (0.88 + 0.24 * weather[..., None])
    to_png(name, col, blur=0.5)


def make_street(name, base, seed):
    n_big = fbm(SIZE, seed, (2, 5, 11))
    n_fine = fbm(SIZE, seed + 1, (48, 96, 192))
    col = tint(np.array(base), 0.12, n_big[..., None].repeat(3, 2))
    col *= (0.96 + 0.07 * n_fine[..., None])
    # macchie di usura e qualche sasso
    spots = fbm(SIZE, seed + 2, (14, 30, 70))
    col *= (0.94 + 0.12 * spots[..., None])
    grit = (n_fine > 0.78).astype(np.float32).astype(np.float32)
    grit = np.array(Image.fromarray((grit * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(1.2))) / 255
    col *= (1.0 - 0.12 * grit[..., None])
    to_png(name, col, blur=0.4)


def make_metal(name, base, seed):
    yy = np.mgrid[0:SIZE, 0:SIZE][0].astype(np.float32)
    panel = SIZE / 4
    seam = 1.0 - smoothstep(0.0, 0.02, np.minimum(yy % panel, panel - (yy % panel)) / panel)
    n = fbm(SIZE, seed, (10, 24, 56, 120))
    col = tint(np.array(base), 0.08, n[..., None].repeat(3, 2))
    col *= (1.0 - 0.35 * seam[..., None])
    rust = fbm(SIZE, seed + 3, (5, 12, 30))
    rust = smoothstep(0.62, 0.85, rust)
    col = col * (1 - rust[..., None]) + np.array([0.42, 0.22, 0.12]) * rust[..., None] * 0.6
    to_png(name, col, blur=0.4)


def make_wood(name, base, seed):
    seam = planks(SIZE, 9, seed)
    grain = fbm(SIZE, seed + 1, (60, 140, 280))
    yy = np.mgrid[0:SIZE, 0:SIZE][0].astype(np.float32)
    rings = 0.5 + 0.5 * np.sin((yy / SIZE) * 90 + grain * 6)
    col = tint(np.array(base), 0.16, rings[..., None].repeat(3, 2))
    col *= (0.94 + 0.10 * grain[..., None])
    col *= (1.0 - 0.55 * seam[..., None])
    to_png(name, col, blur=0.5)


def make_water(name, base, seed):
    n = fbm(SIZE, seed, (4, 9, 20, 44))
    col = tint(np.array(base), 0.22, n[..., None].repeat(3, 2))
    col = col * 0.92 + np.array([0.06, 0.10, 0.13])
    to_png(name, col, blur=1.2)


def make_flat(name, base, seed, amount=0.06):
    n = fbm(SIZE, seed, (5, 12, 30))
    col = tint(np.array(base), amount, n[..., None].repeat(3, 2))
    to_png(name, col, blur=0.5)


def make_hazard(name, base, seed):
    yy, xx = np.mgrid[0:SIZE, 0:SIZE].astype(np.float32)
    stripe = ((xx + yy) % 96) < 48
    n = fbm(SIZE, seed, (8, 20, 48))
    col = np.where(stripe[..., None], np.array(base, np.float32), np.array([0.10, 0.10, 0.11], np.float32))
    col = col * (0.94 + 0.10 * n[..., None].repeat(3, 2))
    to_png(name, col, blur=0.4)


def main():
    print(f"texture mappa → {os.path.normpath(OUT)}")
    make_plaster("plaster", (0.79, 0.63, 0.47), 11)
    make_plaster("plaster_ochre", (0.72, 0.50, 0.29), 21)
    make_stone("stone", (0.76, 0.72, 0.63), 31, rows=6, cols=4, mortar=4.0)
    make_stone("stairs", (0.72, 0.68, 0.60), 41, rows=10, cols=2, mortar=6.0)
    make_stone("cover", (0.66, 0.62, 0.55), 51, rows=5, cols=3, mortar=7.0)
    make_roof("roof", (0.52, 0.26, 0.19), 61)
    make_street("street", (0.42, 0.40, 0.36), 71)
    make_street("concrete", (0.52, 0.51, 0.48), 81)
    make_metal("steel", (0.33, 0.35, 0.38), 91)
    make_wood("wood", (0.45, 0.31, 0.19), 101)
    make_stone("rock", (0.36, 0.34, 0.31), 111, rows=3, cols=2, mortar=10.0)
    make_water("water", (0.24, 0.34, 0.38), 121)
    make_hazard("objective", (0.95, 0.76, 0.16), 131)
    make_flat("team_north", (0.18, 0.61, 0.90), 141, 0.10)
    make_flat("team_south", (0.91, 0.39, 0.18), 151, 0.10)
    print("fatto.")


if __name__ == "__main__":
    main()
