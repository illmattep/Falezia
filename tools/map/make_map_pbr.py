#!/usr/bin/env python3
"""Materiali PBR della mappa di Calanera — texture fotografiche CC0 di ambientCG.

Sostituisce, per i tipi "di ambiente", le texture piatte di make_map_textures.py.
Restano di quello script obiettivo e colori di squadra: sono informazione di gioco
(doc 07, regola del colore) e devono restare piatti e saturi.

Per ogni tipo escono tre file in Assets/Resources/MapTex/:
  <tipo>_c.jpg   colore. RICOLORATO: la media colpisce la palette del doc 07, la
                 variazione resta quella della foto. L'ambiente resta desaturato.
  <tipo>_n.jpg   normal map (convenzione OpenGL, quella di Unity)
  <tipo>_m.png   maschera per URP/Lit: R metallo · G occlusione · A levigatezza
Più macro.png (variazione larga contro la ripetizione) e grass.png (ciuffi d'erba,
atlante 2×2 con alfa per l'alpha-test).

Le texture 1K si scaricano da ambientCG la prima volta e restano in una cache
(tools/map/.cache, fuori dal repository). Licenza CC0: nessun obbligo, ma l'elenco
delle fonti sta in Assets/Resources/MapTex/SOURCES.md.

Uso:  python3 tools/map/make_map_pbr.py
"""
from __future__ import annotations

import io
import os
import sys
import urllib.request
import zipfile

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, "..", "..", "Assets", "Resources", "MapTex")
CACHE = os.environ.get("FALEZIA_TEX_CACHE", os.path.join(HERE, ".cache"))
SIZE = 1024
MASK_SIZE = 512

# tipo → (asset ambientCG, colore medio sRGB voluto, levigatezza max, metallo)
# Levigatezza PBR realistica: materiali asciutti e porosi stanno bassi (intonaco ~0,1,
# sampietrini ~0,2). Valori alti fanno "specchio" sotto il sole e il lampo di volata.
# I colori sono la palette del doc 07; dove il doc non dice niente, quelli di MapBuilder.
MATERIALS = {
    "street":        ("PavingStones070", (132, 126, 114), 0.22, 0.0),   # sampietrini
    "plaster":       ("Plaster001",      (216, 169, 122), 0.12, 0.0),   # intonaco caldo #D8A97A
    "plaster_ochre": ("Plaster001",      (201, 138, 75),  0.12, 0.0),   # intonaco ocra #C98A4B
    "stone":         ("Bricks084",       (201, 191, 168), 0.16, 0.0),   # pietra calcarea #C9BFA8
    "rock":          ("Rock026",         (168, 160, 146), 0.20, 0.0),   # scogliera calcarea
    "concrete":      ("Concrete034",     (140, 136, 128), 0.18, 0.0),   # cemento #8C8880
    "steel":         ("CorrugatedSteel005", (96, 102, 106), 0.40, 0.35),  # container, camion
    "wood":          ("Planks023A",      (128, 100, 72),  0.18, 0.0),   # casse, carri, barche
    "cover":         ("Bricks113A",      (181, 172, 152), 0.16, 0.0),   # muretti, fioriere, comignoli
    "stairs":        ("Tiles139",        (196, 186, 164), 0.28, 0.0),   # lastre di calcare
    "roof":          ("RoofingTiles007", (156, 91, 63),   0.22, 0.0),   # coppi #9C5B3F
    # materiali della sola decorazione (MapDressing)
    "trim":          ("Travertine009",   (214, 204, 182), 0.30, 0.0),   # cornici, davanzali, cornicioni
    "shutter":       ("PaintedWood005",  (92, 104, 86),   0.25, 0.0),   # persiane, verde spento (vedi make_shutter)
    "metal":         ("Metal032",        (70, 72, 74),    0.50, 0.70),  # pluviali, lampioni
}


# tipi che usano normal e maschera di un altro (MapBuilder lo sa: vedi MapBuilder.NormalOf)
SHARED = {"plaster_ochre": "plaster"}


# ---------------------------------------------------------------- download

def fetch(asset: str) -> dict[str, Image.Image]:
    os.makedirs(CACHE, exist_ok=True)
    path = os.path.join(CACHE, f"{asset}_1K-JPG.zip")
    if not os.path.exists(path):
        url = f"https://ambientcg.com/get?file={asset}_1K-JPG.zip"
        print(f"  scarico {asset}…")
        req = urllib.request.Request(url, headers={"User-Agent": "Mozilla/5.0 (falezia map tools)"})
        with urllib.request.urlopen(req, timeout=120) as r, open(path, "wb") as f:
            f.write(r.read())
    maps = {}
    with zipfile.ZipFile(path) as z:
        for name in z.namelist():
            for key in ("Color", "NormalGL", "Roughness", "AmbientOcclusion", "Displacement", "Metalness"):
                if name.endswith(f"_{key}.jpg"):
                    maps[key] = Image.open(io.BytesIO(z.read(name))).copy()
    if "Color" not in maps or "NormalGL" not in maps:
        raise SystemExit(f"{asset}: mancano colore o normal map nello zip")
    return maps


# ---------------------------------------------------------------- utilità

def arr(im: Image.Image, mode="RGB", size=SIZE) -> np.ndarray:
    im = im.convert(mode)
    if im.size != (size, size):
        im = im.resize((size, size), Image.LANCZOS)
    return np.asarray(im).astype(np.float32) / 255.0


def save(name: str, a: np.ndarray, mode="RGB"):
    a = np.clip(a * 255.0 + 0.5, 0, 255).astype(np.uint8)
    path = os.path.join(OUT, name)
    if name.endswith(".jpg"):
        Image.fromarray(a, mode).save(path, quality=92, subsampling=0, optimize=True)
    else:
        Image.fromarray(a, mode).save(path, optimize=True)


def periodic_blur(a: np.ndarray, radius: float) -> np.ndarray:
    """Sfocatura che rispetta la ripetizione (la texture è tileable)."""
    pad = int(radius * 3) + 2
    big = np.pad(a, ((pad, pad), (pad, pad)), mode="wrap")
    im = Image.fromarray(np.clip(big * 255, 0, 255).astype(np.uint8), "L").filter(ImageFilter.GaussianBlur(radius))
    return (np.asarray(im).astype(np.float32) / 255.0)[pad:-pad, pad:-pad]


def recolor(color: np.ndarray, target_srgb) -> np.ndarray:
    """La media colpisce la palette; le variazioni della foto restano (in proporzione)."""
    mean = color.reshape(-1, 3).mean(axis=0)
    target = np.array(target_srgb, np.float32) / 255.0
    # un filo di desaturazione delle variazioni: l'ambiente non deve "urlare" (doc 07)
    lum = color @ np.array([0.299, 0.587, 0.114], np.float32)
    color = color * 0.75 + lum[..., None] * 0.25
    mean = color.reshape(-1, 3).mean(axis=0)
    return np.clip(color * (target / np.maximum(mean, 1e-4)), 0.0, 1.0)


# ---------------------------------------------------------------- un materiale

def build(kind: str, asset: str, target, smooth_max: float, metal: float):
    m = fetch(asset)
    color = recolor(arr(m["Color"]), target)
    save(f"{kind}_c.jpg", color)

    # le due varianti di intonaco condividono normal e maschera: stessa foto, colore diverso
    if kind in SHARED:
        return asset
    # ricompressa: l'originale di ambientCG è un JPG alla massima qualità (2 MB l'uno)
    save(f"{kind}_n.jpg", arr(m["NormalGL"]))

    rough = arr(m["Roughness"], "L", MASK_SIZE) if "Roughness" in m else np.full((MASK_SIZE, MASK_SIZE), 0.8, np.float32)
    if "AmbientOcclusion" in m:
        ao = arr(m["AmbientOcclusion"], "L", MASK_SIZE)
    elif "Displacement" in m:
        # occlusione dalle cavità: dove il rilievo è sotto la media locale, meno luce
        disp = arr(m["Displacement"], "L", MASK_SIZE)
        cav = periodic_blur(disp, 6.0) - disp
        ao = np.clip(1.0 - cav * 3.5, 0.45, 1.0)
    else:
        ao = np.ones((MASK_SIZE, MASK_SIZE), np.float32)
    metal_map = arr(m["Metalness"], "L", MASK_SIZE) * metal if "Metalness" in m else np.full_like(ao, metal)

    smooth = np.clip((1.0 - rough) * smooth_max / max(1e-3, (1.0 - rough).max()), 0.0, smooth_max)
    mask = np.stack([metal_map, ao, np.zeros_like(ao), smooth], axis=-1)
    save(f"{kind}_m.png", mask, "RGBA")
    return asset


# ---------------------------------------------------------------- variazione larga

def fourier(size, freqs, seed):
    r = np.random.default_rng(seed)
    yy, xx = np.mgrid[0:size, 0:size] / size
    out = np.zeros((size, size), np.float32)
    amp, total = 1.0, 0.0
    for f in freqs:
        for _ in range(4):
            ax, ay = r.integers(-f, f + 1, size=2)
            if ax == 0 and ay == 0:
                ax = 1
            out += amp * np.sin(2 * np.pi * (ax * xx + ay * yy) + r.random() * 6.283)
            total += amp
        amp *= 0.6
    out /= total
    return (out - out.min()) / (out.max() - out.min())


def make_macro():
    """Grigio medio 0,5 = neutro nel "detail ×2" di URP/Lit: macchie larghe, chiare e scure."""
    n = fourier(512, (1, 2, 3, 5, 8), 77)
    g = 0.5 + (n - 0.5) * 0.30
    save("macro.jpg", np.stack([g, g, g], axis=-1))


# ---------------------------------------------------------------- erba

def make_grass(size=512, seed=4242):
    """Quattro ciuffi DIVERSI (atlante 2×2), erba mediterranea:
    0 verde e alta · 1 secca, dorata · 2 bassa e fitta, scura · 3 mista con le spighe.
    Ogni filo ha la sua tinta: niente ciuffi fotocopia."""
    rng = np.random.default_rng(seed)
    ss = 4
    big = size * ss
    img = Image.new("RGBA", (big, big), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    cell = big // 2
    styles = [
        # fili, altezza min/max, larghezza, curvatura, colore base, colore secco, quota secchi, spighe
        dict(n=(30, 40), h=(0.55, 0.95), w=(0.014, 0.026), lean=0.30, base=(92, 118, 58), dry=(150, 140, 86), dryp=0.15, seeds=0.0),
        dict(n=(34, 46), h=(0.45, 0.85), w=(0.010, 0.020), lean=0.38, base=(168, 150, 92), dry=(190, 170, 112), dryp=0.7, seeds=0.25),
        dict(n=(44, 60), h=(0.25, 0.50), w=(0.020, 0.036), lean=0.22, base=(70, 92, 48), dry=(112, 118, 66), dryp=0.15, seeds=0.0),
        dict(n=(26, 36), h=(0.50, 0.95), w=(0.012, 0.022), lean=0.30, base=(104, 122, 66), dry=(160, 146, 92), dryp=0.35, seeds=0.55),
    ]
    for k, st in enumerate(styles):
        cx, cy = k % 2, k // 2
        ox, oy = cx * cell, cy * cell
        blades = []
        for _ in range(rng.integers(*st["n"])):
            blades.append((rng.uniform(*st["h"]), float(np.clip(rng.normal(0, 0.12), -0.3, 0.3))))
        # prima i fili alti (dietro), poi i bassi (davanti)
        blades.sort(key=lambda b: -b[0])
        for hh, spread in blades:
            base_x = ox + cell * (0.5 + spread)
            base_y = oy + cell * 0.985
            h = cell * hh
            lean = rng.normal(0, st["lean"]) * h
            # ogni filo resta nella sua cella dell'atlante (niente pezzi del ciuffo accanto)
            lean = float(np.clip(lean, ox + cell * 0.05 - base_x, ox + cell * 0.95 - base_x))
            w = cell * rng.uniform(*st["w"])
            col = np.array(st["dry"] if rng.random() < st["dryp"] else st["base"], np.float32)
            col *= rng.uniform(0.78, 1.18)
            col += rng.normal(0, 6, 3)
            pts_l, pts_r = [], []
            for t in np.linspace(0, 1, 10):
                x = base_x + lean * t * t
                y = base_y - h * t
                ww = w * (1 - t) ** 0.75
                pts_l.append((x - ww, y))
                pts_r.append((x + ww, y))
            c = tuple(int(v) for v in np.clip(col, 0, 255)) + (255,)
            d.polygon(pts_l + pts_r[::-1], fill=c)
            if rng.random() < st["seeds"]:
                # spiga: piccola ellisse chiara in cima al filo
                tx, ty = base_x + lean, base_y - h
                r = cell * 0.022
                sc = tuple(int(v) for v in np.clip(np.array([196, 180, 128]) * rng.uniform(0.85, 1.1), 0, 255)) + (255,)
                d.ellipse([tx - r * 0.7, ty - r * 2.2, tx + r * 0.7, ty + r * 0.4], fill=sc)
    img = img.resize((size, size), Image.LANCZOS)
    a = np.asarray(img).astype(np.float32) / 255.0
    rgb, alpha = a[..., :3], a[..., 3:4]
    # colore anche sotto l'alfa zero: dilatazione del colore dei fili (niente aloni con i mipmap)
    filled = np.where(alpha > 0.02, rgb / np.maximum(alpha, 1e-3), 0.0)
    w8 = (alpha > 0.02).astype(np.float32)
    acc, wacc = filled.copy(), w8.copy()
    for _ in range(24):
        acc = (np.roll(acc, 1, 0) + np.roll(acc, -1, 0) + np.roll(acc, 1, 1) + np.roll(acc, -1, 1) + acc)
        wacc = (np.roll(wacc, 1, 0) + np.roll(wacc, -1, 0) + np.roll(wacc, 1, 1) + np.roll(wacc, -1, 1) + wacc)
        acc /= 5; wacc /= 5
    rgb = np.where(alpha > 0.02, filled, acc / np.maximum(wacc, 1e-4))
    rgb = np.clip(rgb, 0, 1)
    # base più scura: il ciuffo sembra radicato a terra
    yy = np.linspace(0, 1, size)[:, None]
    local = (yy * 2) % 1.0
    shade = 0.55 + 0.45 * (1.0 - np.clip((local - 0.5) / 0.5, 0, 1))
    rgb = rgb * shade[..., None]
    save("grass.png", np.concatenate([rgb, alpha], axis=-1), "RGBA")


def make_shutter():
    """Persiana alla genovese: stecche orizzontali da 6 cm (16 per metro, la texture copre
    1 m), vernice verde spenta. La foto dà solo la grana; stecche, ombre e rilievo sono
    disegnati, così da lontano si legge "persiana" e non "macchia"."""
    kind = "shutter"
    asset, target, smooth_max, _ = MATERIALS[kind]
    m = fetch(asset)
    grain = arr(m["Color"])
    lum = grain @ np.array([0.299, 0.587, 0.114], np.float32)
    lum = 0.85 + (lum - lum.mean()) * 0.6

    y = (np.arange(SIZE, dtype=np.float32) + 0.5) / SIZE * 16.0       # 16 stecche
    f = y - np.floor(y)                                                # 0 in basso, 1 in alto nella stecca
    # profilo della stecca inclinata: luce in alto, ombra sotto il bordo della successiva
    light = 0.78 + 0.32 * np.clip((f - 0.1) / 0.8, 0, 1)
    light = np.where(f < 0.12, 0.42 + f * 2.0, light)
    shade = light[::-1][:, None]                                       # riga 0 = in alto nell'immagine
    color = np.array(target, np.float32) / 255.0
    rgb = color[None, None, :] * (lum[..., None] * shade[..., None])
    save(f"{kind}_c.jpg", np.clip(rgb, 0, 1))

    # normal: la stecca è inclinata verso il basso (normale che guarda giù), il gradino in fondo verso l'alto
    ny = np.where(f < 0.12, 0.55, -0.32)[::-1]
    nz = np.sqrt(1 - ny ** 2)
    n = np.zeros((SIZE, SIZE, 3), np.float32)
    n[..., 0] = 0.5
    n[..., 1] = (ny[:, None] * 0.5 + 0.5)
    n[..., 2] = (nz[:, None] * 0.5 + 0.5)
    save(f"{kind}_n.jpg", n)

    ms = MASK_SIZE
    yy = (np.arange(ms, dtype=np.float32) + 0.5) / ms * 16.0
    ff = (yy - np.floor(yy))[::-1]
    ao = np.where(ff < 0.15, 0.55, 1.0)[:, None] * np.ones((1, ms), np.float32)
    mask = np.stack([np.zeros_like(ao), ao, np.zeros_like(ao), np.full_like(ao, smooth_max)], axis=-1)
    save(f"{kind}_m.png", mask, "RGBA")


def main():
    os.makedirs(OUT, exist_ok=True)
    sources = []
    for kind, (asset, target, smooth, metal) in MATERIALS.items():
        print(f"{kind:14s} ← {asset}")
        build(kind, asset, target, smooth, metal)
        sources.append((kind, asset))
    make_shutter()
    make_macro()
    make_grass()
    with open(os.path.join(OUT, "SOURCES.md"), "w") as f:
        f.write("# Fonti delle texture della mappa\n\n")
        f.write("Generate da `tools/map/make_map_pbr.py`. Foto **CC0** di [ambientCG](https://ambientcg.com), ricolorate sulla palette del doc 07.\n\n")
        f.write("| Tipo | Asset ambientCG |\n|---|---|\n")
        for kind, asset in sources:
            f.write(f"| `{kind}` | [{asset}](https://ambientcg.com/view?id={asset}) |\n")
        f.write("| `macro`, `grass` | generate in codice |\n")
        f.write("| `objective`, `team_north`, `team_south` | `tools/map/make_map_textures.py` (piatte: informazione di gioco) |\n")
    print("fatto:", os.path.abspath(OUT))


if __name__ == "__main__":
    sys.exit(main())
