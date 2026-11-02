"""Importa le texture generate (Meshy) e ne fa set PBR completi.

Cosa fa, per ogni texture:
 1. prende l'albedo, scarta i duplicati "(1)", porta tutto a 1024;
 2. se esiste già la mappa `_normal` fornita, la usa; altrimenti ricava l'altezza
    dalla luminanza (multi-scala) e genera la normale;
 3. rugosità: dalla `_specular` se c'è, altrimenti per famiglia;
 4. salva albedo .jpg + `_n.jpg` + `_r.jpg` in Assets/Resources/MapTex;
 5. scrive il catalogo `manifest.json` con famiglia, tassello e parametri.
"""
import os, glob, json, re
import numpy as np
from PIL import Image, ImageFilter

SRC = os.path.expanduser("~/Scaricati")
TEX = "/home/matteo/Progetti/Falezia/Assets/Resources/MapTex"
S = 1024

# famiglia -> (tassello in metri, roughness base, forza normale, metal)
FAM = [
    (r"^wall_venez",    3.6, 0.92, 4.5, 0.0),
    (r"^wall_grezzo",   3.2, 0.95, 6.5, 0.0),
    (r"^wall_damaged",  3.4, 0.95, 6.5, 0.0),
    (r"^wall_stone",    2.6, 0.92, 7.5, 0.0),
    (r"^roof_coppi",    1.2, 0.85, 9.0, 0.0),
    (r"^roof_edge",     1.0, 0.85, 9.0, 0.0),
    (r"^sampietrini",   2.8, 0.94, 7.5, 0.0),
    (r"^lastrico",      3.6, 0.92, 6.0, 0.0),
    (r"^marciapiede",   2.0, 0.90, 5.0, 0.0),
    (r"^scala_pietra",  1.6, 0.92, 6.0, 0.0),
    (r"^terra_battuta", 3.0, 0.95, 4.0, 0.0),
    (r"^legno_persiana",1.4, 0.85, 7.0, 0.0),
    (r"^legno_porta",   1.4, 0.85, 7.0, 0.0),
    (r"^legno_cassa",   1.2, 0.90, 7.0, 0.0),
    (r"^legno_botte",   1.0, 0.88, 7.0, 0.0),
    (r"^tela_tenda",    1.2, 0.95, 3.5, 0.0),
    (r"^sacco",         1.0, 0.95, 5.0, 0.0),
    (r"^ferro_ruggine", 1.0, 0.85, 6.0, 0.30),
    (r"^ferro_battuto", 1.0, 0.55, 5.0, 0.85),
    (r"^lamiera",       1.0, 0.65, 5.0, 0.45),
    (r"^acqua",         2.0, 0.12, 2.0, 0.10),
    (r"^vetro",         1.5, 0.08, 2.0, 0.30),
    (r"^decal_",        0.0, 0.90, 2.0, 0.0),
]
def family(aid):
    for pat, tile, rough, nstr, metal in FAM:
        if re.match(pat, aid):
            return tile, rough, nstr, metal
    return 2.0, 0.90, 5.0, 0.0

def find(aid, suffix=""):
    for f in glob.glob(os.path.join(SRC, f"Meshy_AI_{aid}{suffix}.png*")):
        return f
    return None

def load(path):
    im = Image.open(path).convert("RGB")
    if im.size != (S, S):
        im = im.resize((S, S), Image.LANCZOS)
    return im

def height_from(alb, freq=(6, 40), blur=(2.0, 0.8)):
    """Altezza multi-scala: struttura larga + dettaglio fine, senza dipendere dal colore."""
    h = np.asarray(alb, np.float32) / 255.0
    h = h[..., 0] * 0.299 + h[..., 1] * 0.587 + h[..., 2] * 0.114
    out = np.zeros_like(h)
    for f, w in zip(freq, (0.65, 0.35)):
        im = Image.fromarray((h * 255).astype(np.uint8))
        im = im.filter(ImageFilter.GaussianBlur(max(0.5, 1024 / max(1, f) / 30)))
        a = np.asarray(im, np.float32) / 255.0
        out += (a - a.min()) / max(1e-5, a.max() - a.min()) * w
    out = (out - out.min()) / max(1e-5, out.max() - out.min())
    return out

def normal_from(h, strength):
    dx = (np.roll(h, -1, 1) - np.roll(h, 1, 1)) * 0.5
    dy = (np.roll(h, -1, 0) - np.roll(h, 1, 0)) * 0.5
    nx, ny = -dx * strength, -dy * strength
    nz = np.ones_like(h)
    ln = np.sqrt(nx * nx + ny * ny + nz * nz)
    n = np.stack([nx / ln, ny / ln, nz / ln], -1) * 0.5 + 0.5
    return n

def roughness(alb, base, spec_path=None):
    if spec_path and os.path.exists(spec_path):
        sp = np.asarray(Image.open(spec_path).convert("L"), np.float32) / 255.0
        return np.clip(1.0 - sp * 0.85, 0.05, 1.0)
    a = np.asarray(alb, np.float32) / 255.0
    lum = a[..., 0] * .299 + a[..., 1] * .587 + a[..., 2] * .114
    return np.clip(base + (0.5 - lum) * 0.12, 0.05, 1.0)

def main():
    ids = set()
    for f in glob.glob(os.path.join(SRC, "Meshy_AI_*.png*")):
        aid = re.sub(r"^Meshy_AI_|\(1\)|\.(png|jpg)$", "", os.path.basename(f))
        aid = re.sub(r"_(normal|ambient|displacement|specular)$", "", aid)
        ids.add(aid)
    ids = sorted(i for i in ids if i)

    manifest = {}
    done = 0
    for aid in ids:
        alb_p = find(aid)
        if not alb_p: continue
        tile, base_r, nstr, metal = family(aid)
        alb = load(alb_p)
        alb.save(f"{TEX}/{aid}.jpg", quality=90)

        nrm_p = find(aid, "_normal")
        if nrm_p:
            nrm = Image.open(nrm_p).convert("RGB").resize((S, S), Image.LANCZOS)
            nrm.save(f"{TEX}/{aid}_n.jpg")
        else:
            h = height_from(alb)
            n = normal_from(h, nstr)
            Image.fromarray((np.clip(n, 0, 1) * 255).astype(np.uint8)).save(f"{TEX}/{aid}_n.jpg")

        r = roughness(alb, base_r, find(aid, "_specular"))
        Image.fromarray((r * 255).astype(np.uint8)).save(f"{TEX}/{aid}_r.jpg")

        manifest[aid] = {"family": aid.split("_")[0], "tile_m": tile,
                         "rough": base_r, "metal": metal,
                         "normal": "provided" if nrm_p else "generated",
                         "roughness": "from_specular" if find(aid, "_specular") else "generated"}
        done += 1

    json.dump(manifest, open(f"{TEX}/manifest.json", "w"), indent=1)
    print(f"importate {done} texture · catalogo in manifest.json")
    for aid in sorted(manifest):
        m = manifest[aid]
        print(f"  {aid:26} tassello {m['tile_m']:.1f} m · rough {m['rough']:.2f} · normal {m['normal']}")

if __name__ == "__main__":
    main()
