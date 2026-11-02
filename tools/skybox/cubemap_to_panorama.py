#!/usr/bin/env python3
"""
Cubemap → panorama equirettangolare, per il cielo di Godot.

Sketchfab consegna spesso gli skybox come SEI immagini (una per faccia). Godot, per
`PanoramaSkyMaterial`, vuole una sola immagine equirettangolare: questo script fa la
conversione, senza aprire Blender.

Uso:
    python3 tools/skybox/cubemap_to_panorama.py cartella/ --out game/assets/sky/sky_panorama.jpg
    python3 tools/skybox/cubemap_to_panorama.py cartella/ --width 8192 --yaw 90
    python3 tools/skybox/cubemap_to_panorama.py cartella/ --flip-v     # se il cielo esce capovolto
    python3 tools/skybox/cubemap_to_panorama.py --test                 # prova con un cubo finto

Le facce si riconoscono dal nome del file (maiuscole indifferenti):
    px nx py ny pz nz ·  posx negx posy negy posz negz ·  right left up down front back
e possono avere un prefisso qualsiasi (es. `basic_sky_px.jpg`, `sky_px.png`).

Nota sulle convenzioni: qui si usa quella OpenGL (quella di Sketchfab e della maggior
parte dei tool). Se il risultato è ruotato o specchiato, si aggiusta con `--yaw` e
`--flip-v`: sono solo due numeri, non serve rifare niente d'altro.
"""
from __future__ import annotations

import argparse
import sys
from pathlib import Path

import numpy as np
from PIL import Image

FACES = ["px", "nx", "py", "ny", "pz", "nz"]
ALIASES = {
    "right": "px", "left": "nx", "up": "py", "down": "ny", "front": "pz", "back": "nz",
    "posx": "px", "negx": "nx", "posy": "py", "negy": "ny", "posz": "pz", "negz": "nz",
    "east": "px", "west": "nx", "north": "nz", "south": "pz",
}
EXTS = (".png", ".jpg", ".jpeg", ".webp", ".tif", ".tiff", ".bmp")


def find_faces(folder: Path) -> dict[str, Path]:
    """Trova le sei facce nella cartella, accettando prefissi e nomi alternativi."""
    found: dict[str, Path] = {}
    for f in sorted(folder.iterdir()):
        if f.suffix.lower() not in EXTS:
            continue
        stem = f.stem.lower().replace("-", "_")
        for token in stem.split("_"):
            name = ALIASES.get(token, token)
            if name in FACES and name not in found:
                found[name] = f
    missing = [n for n in FACES if n not in found]
    if missing:
        raise SystemExit(
            f"mancano le facce: {', '.join(missing)}\n"
            f"trovate: {', '.join(sorted(found)) or 'nessuna'}\n"
            f"in: {folder}"
        )
    return found


def load_faces(folder: Path, size: int | None = None) -> tuple[list[np.ndarray], int]:
    files = find_faces(folder)
    images = [Image.open(files[n]).convert("RGB") for n in FACES]
    if size is None:
        size = max(im.size[0] for im in images)
    out = []
    for im in images:
        if im.size != (size, size):
            im = im.resize((size, size), Image.LANCZOS)
        out.append(np.asarray(im, dtype=np.float32) / 255.0)
    return out, size


def bilinear(img: np.ndarray, fx: np.ndarray, fy: np.ndarray) -> np.ndarray:
    s = img.shape[0]
    x0 = np.clip(np.floor(fx).astype(np.int32), 0, s - 1)
    y0 = np.clip(np.floor(fy).astype(np.int32), 0, s - 1)
    x1 = np.clip(x0 + 1, 0, s - 1)
    y1 = np.clip(y0 + 1, 0, s - 1)
    wx = (fx - x0)[:, None]
    wy = (fy - y0)[:, None]
    c00 = img[y0, x0]
    c10 = img[y0, x1]
    c01 = img[y1, x0]
    c11 = img[y1, x1]
    return (c00 * (1 - wx) + c10 * wx) * (1 - wy) + (c01 * (1 - wx) + c11 * wx) * wy


def panorama(faces: list[np.ndarray], size: int, width: int,
             yaw_deg: float = 0.0, flip_v: bool = False) -> Image.Image:
    """
    Per ogni pixel della panorama si calcola la direzione e si campiona la faccia
    del cubo che sta in quella direzione. U = longitudine, V = latitudine.
    """
    height = max(2, width // 2)

    u = (np.arange(width, dtype=np.float32) + 0.5) / width
    v = (np.arange(height, dtype=np.float32) + 0.5) / height
    phi = (u - 0.5) * (2.0 * np.pi) + np.radians(yaw_deg)   # 0,5 = avanti (-Z)
    theta = (0.5 - v) * np.pi                              # 0 = orizzonte
    p, t = np.meshgrid(phi, theta)

    ct = np.cos(t)
    x = np.sin(p) * ct
    y = np.sin(t)
    z = -np.cos(p) * ct

    ax, ay, az = np.abs(x), np.abs(y), np.abs(z)
    axis = np.where((ax >= ay) & (ax >= az), 0, np.where(ay >= az, 1, 2))

    sc = np.zeros_like(x)
    tc = np.zeros_like(x)

    m = axis == 0                                  # ±X
    sx = np.sign(x[m])
    sc[m] = -z[m] / np.maximum(ax[m], 1e-6) * sx
    tc[m] = -y[m] / np.maximum(ax[m], 1e-6)

    m = axis == 1                                  # ±Y
    sy = np.sign(y[m])
    sc[m] = x[m] / np.maximum(ay[m], 1e-6)
    tc[m] = z[m] / np.maximum(ay[m], 1e-6) * sy

    m = axis == 2                                  # ±Z
    sz = np.sign(z[m])
    sc[m] = x[m] / np.maximum(az[m], 1e-6) * sz
    tc[m] = -y[m] / np.maximum(az[m], 1e-6)

    # quale delle sei facce: l'asse da solo non basta, serve anche il verso
    face_id = np.zeros(x.shape, dtype=np.int32)
    face_id[axis == 0] = np.where(x[axis == 0] > 0, 0, 1)   # px / nx
    face_id[axis == 1] = np.where(y[axis == 1] > 0, 2, 3)   # py / ny
    face_id[axis == 2] = np.where(z[axis == 2] > 0, 4, 5)   # pz / nz

    out = np.zeros((height, width, 3), dtype=np.float32)
    for f in range(6):
        mask = face_id == f
        if not mask.any():
            continue
        fw = faces[f]
        fx = (sc[mask] + 1.0) * 0.5 * (size - 1)
        ty = tc[mask] if not flip_v else -tc[mask]
        fy = (ty + 1.0) * 0.5 * (size - 1)
        out[mask] = bilinear(fw, fx, fy)

    return Image.fromarray((np.clip(out, 0.0, 1.0) * 255.0 + 0.5).astype(np.uint8))


def self_test() -> int:
    """Cubo a facce piene: si controlla che ogni direzione finisca sulla faccia giusta."""
    colors = {
        "px": (220, 40, 40), "nx": (40, 200, 90), "py": (60, 90, 220),
        "ny": (230, 200, 40), "pz": (200, 60, 200), "nz": (40, 200, 200),
    }
    size = 8
    faces = [np.ones((size, size, 3), np.float32) * (np.array(colors[n]) / 255.0) for n in FACES]
    img = np.asarray(panorama(faces, size, 64), dtype=np.int32)

    probes = {  # direzione -> (u, v) normalizzati
        "px": (0.75, 0.50), "nx": (0.25, 0.50), "pz": (0.999, 0.50),
        "nz": (0.50, 0.50), "py": (0.50, 0.12), "ny": (0.50, 0.88),
    }
    ok = True
    for face, (pu, pv) in probes.items():
        px = min(63, int(pu * 64))
        py = min(31, int(pv * 32))
        got = tuple(int(c) for c in img[py, px])
        want = colors[face]
        good = all(abs(a - b) <= 6 for a, b in zip(got, want))
        ok &= good
        print(f"  {'OK  ' if good else 'FAIL'} {face}: {got} atteso {want}")
    print("ESITO:", "OK" if ok else "FALLITO")
    return 0 if ok else 1


def main(argv=None) -> int:
    ap = argparse.ArgumentParser(description="Cubemap -> panorama equirettangolare per Godot")
    ap.add_argument("folder", nargs="?", type=Path, help="cartella con le sei facce")
    ap.add_argument("--out", type=Path, default=Path("sky_panorama.jpg"))
    ap.add_argument("--width", type=int, default=4096, help="larghezza della panorama (default 4096)")
    ap.add_argument("--size", type=int, default=None, help="lato delle facce usato per il campionamento")
    ap.add_argument("--yaw", type=float, default=0.0, help="rotazione in gradi (default 0)")
    ap.add_argument("--flip-v", action="store_true", help="se il cielo esce capovolto")
    ap.add_argument("--test", action="store_true", help="prova interna, nessun file richiesto")
    args = ap.parse_args(argv)

    if args.test:
        return self_test()

    if args.folder is None or not args.folder.is_dir():
        ap.error("serve la cartella con le sei facce (oppure --test)")

    faces, size = load_faces(args.folder, args.size)
    img = panorama(faces, size, args.width, args.yaw, args.flip_v)
    args.out.parent.mkdir(parents=True, exist_ok=True)
    if args.out.suffix.lower() in (".jpg", ".jpeg"):
        img.save(args.out, quality=95, subsampling=0)
    else:
        img.save(args.out)
    print(f"scritto {args.out} · {img.size[0]}x{img.size[1]} da facce {size}x{size}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
