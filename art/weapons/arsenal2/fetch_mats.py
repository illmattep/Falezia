"""
FALEZIA · art/weapons/arsenal2/fetch_mats.py — materiali fotorealistici (CC0).

Scarica da ambientCG una volta sola, estrae le mappe, le riduce e le converte nel
formato che usa l'arsenale:

    <fam>_basecolor.jpg   1024², sRGB
    <fam>_normal.jpg       512², lineare (OpenGL, verde su)
    <fam>_orm.png          256², lineare (R = occlusione, G = roughness, B = metallo)

Tutto è CC0 (pubblico dominio): nessuna attribuzione obbligatoria, nessun rischio
di licenza. I file grezzi finiscono in /tmp e vengono cancellati.

    python3 art/weapons/arsenal2/fetch_mats.py            (solo le famiglie mancanti)
    python3 art/weapons/arsenal2/fetch_mats.py --force    (riscarica tutto)
"""
import io
import os
import sys
import json
import urllib.request
import zipfile

from PIL import Image, ImageFilter
import numpy as np

OUT = "/home/matteo/Progetti/Falezia/game/assets/weapons/_shared/tex"
os.makedirs(OUT, exist_ok=True)

# famiglia → asset ambientCG. Le famiglie sono i "materiali" che le armi si passano:
# ognuna è una terna di mappe, e più armi possono usare la stessa.
# quanto si scurisce il colore di ogni famiglia: le armi non sono fatte di plastica
# chiara, e una basecolor fotografica va portata nella scala giusta.
TINT = {
    "polymer":   (0.55, 0.55, 0.56),
    "stipple":   (0.52, 0.52, 0.54),
    "steel":     (0.88, 0.89, 0.92),
    "stainless": (1.00, 1.00, 1.00),
    "wood":      (0.78, 0.62, 0.52),
    "sheet":     (0.56, 0.58, 0.56),
    "green":     (0.60, 0.70, 0.56),
    "brass":     (1.10, 0.92, 0.52),
    "cloth":     (0.85, 0.85, 0.88),
    "rubber":    (0.70, 0.70, 0.74),
    "leather":   (1.00, 1.00, 1.00),
    "concrete":  (1.00, 1.00, 1.00),
}

# Colore vero del materiale (sRGB). Dalla foto si tiene SOLO il dettaglio fine:
# le macchie grandi, una volta messe in piastrella, si leggono come camouflage.
BASE = {
    # Valori alzati rispetto alla prima passata: con l'albedo quasi nero il metallo
    # in ombra diventava una silhouette e non si leggeva la forma dell'arma.
    "polymer":   (0.088, 0.089, 0.095),
    "stipple":   (0.072, 0.073, 0.078),
    "steel":     (0.375, 0.385, 0.405),
    "stainless": (0.630, 0.640, 0.665),
    "wood":      (0.265, 0.150, 0.088),
    "sheet":     (0.285, 0.292, 0.278),
    "green":     (0.190, 0.225, 0.165),
    "brass":     (0.735, 0.555, 0.205),
    "cloth":     (0.125, 0.125, 0.132),
    "rubber":    (0.078, 0.078, 0.084),
    "leather":   (0.098, 0.080, 0.068),
    "concrete":  (0.455, 0.450, 0.432),
}

# quanto dettaglio tenere (1 = tutto il contrasto della foto)
DETAIL = {
    "wood": 1.45, "cloth": 1.20, "green": 0.85, "sheet": 0.75, "steel": 0.80,
    "stainless": 0.70, "brass": 0.75, "polymer": 0.55, "stipple": 0.75,
    "rubber": 0.60, "leather": 0.90, "concrete": 1.00,
}

SETS = {
    "polymer":   "Plastic012A",     # plastica nera opaca: fusti, paramani
    "stipple":   "Plastic012B",     # plastica nera graffiata: impugnature, caricatori
    "steel":     "Metal038",        # acciaio graffiato: carrelli, canne, castelli
    "stainless": "Metal009",        # acciaio spazzolato: rivoltelle, ottiche
    "wood":      "Wood062",         # legno di noce: calci e paramani
    "sheet":     "Metal019",        # lamiera scura sporca: armi economiche
    "green":     "PaintedMetal006",  # vernice verde militare
    "brass":     "Metal057A",       # rame (tingiuto d'ottone): bossoli, dettagli
    "cloth":     "Fabric004",       # tessuto tecnico: nastro, cinghie
    "rubber":    "Rubber004",       # gomma: calcioli, guancette
    "leather":   "Leather026",      # pelle nera: cinghie, guanti
    "concrete":  "Concrete034",     # intonaco: fori dei proiettili sui muri
}

# famiglie che non hanno senso fotografate: due pixel piatti bastano
SYNTH = {
    "void":  dict(rgb=(0.010, 0.010, 0.011), rough=0.95, metal=0.0),
    "glass": dict(rgb=(0.020, 0.028, 0.042), rough=0.045, metal=0.0),
}

SIZE_COLOR, SIZE_NORMAL, SIZE_ORM = 1024, 512, 256


UA = ("Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) "
      "Chrome/124.0 Safari/537.36")


def download(asset, size="2K"):
    """Scarica lo zip ambientCG. Il sito rifiuta urllib con 403: serve uno UA da browser."""
    url = f"https://ambientcg.com/get?file={asset}_{size}-JPG.zip"
    req = urllib.request.Request(url, headers={"User-Agent": UA, "Accept": "*/*"})
    with urllib.request.urlopen(req, timeout=180) as r:
        data = r.read()
    return zipfile.ZipFile(io.BytesIO(data))


def pick(zf, *keys):
    for n in zf.namelist():
        low = n.lower()
        if all(k.lower() in low for k in keys) and low.endswith((".jpg", ".png")):
            return n
    return None


def load(zf, name, mode="RGB"):
    if name is None:
        return None
    img = Image.open(io.BytesIO(zf.read(name))).convert(mode)
    return img


def to_arr(img, size):
    return np.asarray(img.resize((size, size), Image.LANCZOS), dtype=np.float32) / 255.0


def build(fam, asset, force=False):
    color_p = os.path.join(OUT, fam + "_basecolor.jpg")
    if not force and os.path.exists(color_p) and os.path.exists(os.path.join(OUT, fam + "_orm.png")):
        print(f"SKIP {fam:10s} già presente")
        return

    print(f"FETCH {fam:10s} ← {asset}", flush=True)
    zf = download(asset)

    color = load(zf, pick(zf, "color"))
    normal = load(zf, pick(zf, "normalgl", "normal"))
    rough = load(zf, pick(zf, "roughness"), "L")
    metal = load(zf, pick(zf, "metalness"), "L")
    ao = load(zf, pick(zf, "ambientocclusion", "ao"), "L")

    if color is None:
        print("   !! niente colore, salto")
        return

    # --- colore: base piatta + dettaglio fine (passa-alto). Così la piastrella
    # non ha macchie grandi, non si vede la ripetizione, e resta la foto.
    # passa-alto alla risoluzione finale: a 512 il grano fine si perdeva nel resize
    src = color.resize((SIZE_COLOR, SIZE_COLOR), Image.LANCZOS)
    col_full = np.asarray(src, dtype=np.float32) / 255.0
    low = np.asarray(src.filter(ImageFilter.GaussianBlur(SIZE_COLOR / 28.0)),
                     dtype=np.float32) / 255.0
    detail = (col_full - low) * DETAIL.get(fam, 0.8)
    base = np.array(BASE.get(fam, (0.3, 0.3, 0.3)), dtype=np.float32)[None, None, :]
    col = np.clip(base + detail, 0.0, 1.0)
    Image.fromarray((np.clip(col, 0, 1) * 255).astype(np.uint8), "RGB").save(
        color_p, quality=92, optimize=True)

    if normal is not None:
        nrm = to_arr(normal, SIZE_NORMAL)
        Image.fromarray((np.clip(nrm, 0, 1) * 255).astype(np.uint8), "RGB").save(
            os.path.join(OUT, fam + "_normal.jpg"), quality=94, optimize=True)
    else:
        flat = np.full((SIZE_NORMAL, SIZE_NORMAL, 3), 0.5, dtype=np.float32)
        flat[..., 1] = 1.0
        Image.fromarray((flat * 255).astype(np.uint8), "RGB").save(
            os.path.join(OUT, fam + "_normal.jpg"), quality=94)

    if rough is not None:
        r = to_arr(rough, SIZE_ORM)
        m0 = float(r.mean())
        r = np.clip(m0 + (r - m0) * 0.55, 0.04, 0.98)   # solo la variazione fine
    else:
        r = np.full((SIZE_ORM, SIZE_ORM), 0.6, np.float32)
    m = to_arr(metal, SIZE_ORM) if metal is not None else np.zeros((SIZE_ORM, SIZE_ORM), np.float32)
    a = to_arr(ao, SIZE_ORM) if ao is not None else np.ones((SIZE_ORM, SIZE_ORM), np.float32)
    orm = np.stack([a, r, m], axis=-1)
    Image.fromarray((np.clip(orm, 0, 1) * 255).astype(np.uint8), "RGB").save(
        os.path.join(OUT, fam + "_orm.png"), optimize=True)

    kb = (os.path.getsize(color_p) + os.path.getsize(os.path.join(OUT, fam + "_normal.jpg"))
          + os.path.getsize(os.path.join(OUT, fam + "_orm.png"))) // 1024
    print(f"   OK  {fam:10s} {kb:4d} KB   colore medio {col.reshape(-1,3).mean(0).round(3)}")


def synth(fam, rgb, rough, metal):
    color_p = os.path.join(OUT, fam + "_basecolor.jpg")
    if os.path.exists(color_p) and not ("--force" in sys.argv):
        print(f"SKIP {fam:10s} sintetico già presente")
        return
    c = np.full((64, 64, 3), rgb, dtype=np.float32)
    Image.fromarray((np.clip(c, 0, 1) * 255).astype(np.uint8), "RGB").save(color_p, quality=95)
    n = np.full((32, 32, 3), 0.5, dtype=np.float32); n[..., 1] = 1.0
    Image.fromarray((n * 255).astype(np.uint8), "RGB").save(
        os.path.join(OUT, fam + "_normal.jpg"), quality=95)
    o = np.zeros((32, 32, 3), dtype=np.float32)
    o[..., 0] = 1.0; o[..., 1] = rough; o[..., 2] = metal
    Image.fromarray((o * 255).astype(np.uint8), "RGB").save(os.path.join(OUT, fam + "_orm.png"))
    print(f"SYNTH {fam:10s} colore {rgb} rough {rough}")


def main():
    force = "--force" in sys.argv
    for fam, cfg in SYNTH.items():
        synth(fam, cfg["rgb"], cfg["rough"], cfg["metal"])
    only = [a for a in sys.argv[1:] if not a.startswith("--")]
    for fam, asset in SETS.items():
        if only and fam not in only:
            continue
        try:
            build(fam, asset, force)
        except Exception as e:
            print(f"   !! {fam} ({asset}): {e}")


if __name__ == "__main__":
    main()
