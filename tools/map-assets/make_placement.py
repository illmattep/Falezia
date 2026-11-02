"""Genera Assets/StreamingAssets/map/props.json: dove vanno i modelli nella mappa.

Regole:
  · i prop stanno ADDOSSATI alle facciate, mai in mezzo alla strada;
  · porte e negozi solo a piano terra, nelle zone dove si combatte;
  · le caverne sono gusci già nelle coordinate del mondo (identità);
  · nessun oggetto sopra le rotte verificate.

Uso: python3 tools/map-assets/make_placement.py
"""
import csv, json, math, os, random

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
CSV = os.path.join(ROOT, "art/map-assets/buildings.csv")
OUT = os.path.join(ROOT, "Assets/StreamingAssets/map/props.json")

random.seed(11)

# zone dove vale la pena spendere geometria vera
HERO = {"PIAZZA", "MERCATO", "VIA_STRETTA", "PORTO", "LUNGOMARE", "SCALINATA", "MURA"}

# yaw del modello: i GLB esportati da Blender guardano +Z
def yaw_for(side):
    return {"N": 180.0, "S": 0.0, "O": -90.0, "E": 90.0}[side]

def side_geometry(r, side):
    """Ritorna (punto di partenza, direzione lungo il muro, normale uscente, lunghezza)."""
    x0, z0 = float(r["x0"]), float(r["z0"])
    w, d = float(r["w"]), float(r["d"])
    if side == "N":
        return (x0, z0), (1, 0), (0, -1), w
    if side == "S":
        return (x0, z0 + d), (1, 0), (0, 1), w
    if side == "O":
        return (x0, z0), (0, 1), (-1, 0), d
    return (x0 + w, z0), (0, 1), (1, 0), d

items = []

# ---------------------------------------------------------------- caverne (identità)
# I gusci delle caverne sono pronti ma NON ancora allineati ai buchi della mappa:
# finché non li verifico uno per uno restano fuori (coprivano la geometria vera).
for model in ():
    items.append(dict(model=f"map/{model}", x=0, y=0, z=0, rot=0, scale=1))

# ---------------------------------------------------------------- obiettivo
items.append(dict(model="map/objective/objective_chest", x=0, y=0.0, z=-2.0, rot=0, scale=1))

# ---------------------------------------------------------------- strada e facciate
# (modello, distanza dal muro, peso)
PROPS = [("street/street_bench", 0.62, 30), ("street/street_bollard", 0.55, 22),
         ("street/street_planter", 0.60, 24), ("street/street_fountain", 0.70, 4),
         ("street/street_lamp_wall", 0.12, 20)]

# --- geometria vera della mappa: serve per capire se un lato dà su un passaggio coperto
import json as _json
_map = _json.load(open(os.path.join(ROOT, "game/maps/calanera.json")))
KIND = _map["kinds"]
SOLID = []          # (x0,x1,y0,y1,z0,z1,kind)
for b in _map["boxes"]:
    SOLID.append((b[0], b[0]+b[3], b[1], b[1]+b[4], b[2], b[2]+b[5], KIND[b[6]]))

def wall_behind(x, z, nx, nz, depth=0.45):
    """C'è davvero un muro dietro questo lato? Se no, il pannello resterebbe sospeso."""
    px, pz = x - nx * depth, z - nz * depth
    for (x0, x1, y0, y1, z0, z1, k) in SOLID:
        if k in ("street", "team_north", "team_south", "objective", "water"):
            continue
        if x0 - 0.05 <= px <= x1 + 0.05 and z0 - 0.05 <= pz <= z1 + 0.05 and y0 < 1.6 < y1:
            return True
    return False

def free(x, z, y=1.6):
    """Nessun solido in questo punto (spazio aperto davanti alla facciata)."""
    for (x0, x1, y0, y1, z0, z1, k) in SOLID:
        if k in ("street", "team_north", "team_south", "objective", "water"):
            continue
        if x0 <= x <= x1 and z0 <= z <= z1 and y0 <= y <= y1:
            return False
    return True

def covered(x, z, y_from=2.6):
    """C'è qualcosa sopra questo punto? (arco, porticato, volta)"""
    for (x0,x1,y0,y1,z0,z1,k) in SOLID:
        if k in ("street", "team_north", "team_south", "objective"):
            continue
        if x0 <= x <= x1 and z0 <= z <= z1 and y0 >= y_from and y1 > y0 + 0.4:
            return True
    return False

rows = list(csv.DictReader(open(CSV)))
for r in rows:
    if r["zone"] not in HERO or float(r["facade_m"]) <= 0:
        continue
    sides = r["sides"].replace("-", "")
    for side in sides:
        (sx, sz), (dx, dz), (nx, nz), length = side_geometry(r, side)
        if length < 2.4:
            continue

        # metà del lato: se sopra c'è un passaggio coperto, qui non ci va facciata né arredo
        mx, mz = sx + dx * length * 0.5, sz + dz * length * 0.5
        if covered(mx, mz):
            continue

        # il pannello deve avere una parete alle spalle, altrimenti vola
        if not wall_behind(mx + nx * 0.06, mz + nz * 0.06, nx, nz):
            continue

        # e deve essere A FILO: davanti aperto, dietro pieno. Se il lato è etichettato male
        # il pannello finisce di taglio e sembra una tavola che esce dal muro.
        if not (free(mx + nx * 0.55, mz + nz * 0.55) and not free(mx - nx * 0.55, mz - nz * 0.55)):
            continue
        yaw = yaw_for(side)

        # --- arredo: uno ogni ~6 m, con pesi (niente fontane a ogni angolo)
        step = 6.0
        n = max(0, int(length // step))
        total = sum(w for _, _, w in PROPS)
        for i in range(n):
            t = (i + 0.5) * step
            if t > length - 0.8:
                break
            pick = (hash((r["id"], side, i)) >> 4) % total
            acc = 0
            choice = PROPS[0]
            for cand in PROPS:
                acc += cand[2]
                if pick < acc:
                    choice = cand
                    break
            px, pz = sx + dx * t, sz + dz * t
            off = choice[1]
            y = 2.6 if "lamp" in choice[0] else 0.0
            items.append(dict(
                model=f"map/{choice[0]}",
                x=round(px + nx * off, 2), y=y, z=round(pz + nz * off, 2),
                rot=round(yaw + random.uniform(-10, 10), 1), scale=1))

        # --- cornicione e filo di coppi sul bordo alto: è ciò che cambia lo skyline
        if length >= 2.0:
            k = int(length // 3.0)
            for i in range(k):
                t = (i + 0.5) * 3.0
                px, pz = sx + dx * t, sz + dz * t
                h = float(r["h"])
                items.append(dict(model="map/facade/facade_cornice",
                                  x=round(px + nx * 0.10, 2), y=round(h - 0.42, 2),
                                  z=round(pz + nz * 0.10, 2), rot=yaw, scale=1))
                if i % 2 == 0:
                    items.append(dict(model="map/roof/roof_tile_edge",
                                      x=round(px + nx * 0.28, 2), y=round(h + 0.02, 2),
                                      z=round(pz + nz * 0.28, 2), rot=yaw, scale=1))

        # --- porte e negozi: almeno uno per lato, poi ogni 12 m
        if length >= 3.0:
            centres = [length / 2] if length < 12 else [3 + i * 12 for i in range(int((length - 3) // 12) + 1)]
            for k, t in enumerate(centres):
                if t > length - 1.4:
                    continue
                px, pz = sx + dx * t, sz + dz * t
                shop = r["zone"] in ("MERCATO", "PIAZZA", "LUNGOMARE") and (k % 2 == 0)
                model = "map/facade/facade_shop_front" if shop else "map/facade/facade_arch_door"
                items.append(dict(model=model,
                                  x=round(px + nx * 0.05, 2), y=0.0, z=round(pz + nz * 0.05, 2),
                                  rot=yaw, scale=1))

json.dump({"items": items}, open(OUT, "w"), indent=0, ensure_ascii=False)

import collections
c = collections.Counter(i["model"] for i in items)
print(f"{len(items)} oggetti in {OUT}")
for k, v in c.most_common():
    print(f"  {v:4}  {k}")
