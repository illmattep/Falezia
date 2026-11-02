#!/usr/bin/env python3
"""
CALANERA — generatore del graybox (fase A0 del doc 07).

Produce game/maps/calanera.json: la geometria di collisione della mappa, in scatole
allineate agli assi, letta IDENTICA dal server, dal client e dai test.

Perché un generatore e non scatole scritte a mano:
  * la regola D2 (simmetria architettonica) è garantita per costruzione: si disegna
    la metà NORD e la si ruota di 180° per ottenere la metà SUD;
  * la verifica del doc 02 (anello, rotte, tempi) gira sugli stessi dati del gioco.

Convenzioni: X = est, Z = sud (-Z è nord, come "avanti" in Godot), Y = su. Metri.
Uso:  python3 tools/mapgen/calanera.py            (genera + verifica)
      python3 tools/mapgen/calanera.py --preview  (anche l'anteprima PNG dall'alto)
"""
from __future__ import annotations

import heapq
import json
import math
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
OUT_JSON = ROOT / "game" / "maps" / "calanera.json"
OUT_PNG = ROOT / "tools" / "mapgen" / "calanera_preview.png"

# --------------------------------------------------------------------------- numeri
# Gli stessi di sim/Constants.cs: la verifica deve muoversi come la simulazione.
STEP_HEIGHT = 0.35        # gradino superabile camminando
PLAYER_RADIUS = 0.30
PLAYER_HEIGHT = 1.80
WALK = 4.2
SPRINT = 6.5

X_MIN, X_MAX = -90, 90    # 180 m
Z_MIN, Z_MAX = -100, 100  # 200 m (spawn a ±91 dal centro)

KINDS = [
    "street",         # 0  lastricato, pavimento
    "plaster",        # 1  intonaco caldo (nord)
    "plaster_ochre",  # 2  intonaco ocra (sud)
    "stone",          # 3  pietra calcarea
    "rock",           # 4  roccia (grotta, galleria)
    "concrete",       # 5  cemento
    "steel",          # 6  acciaio (container, gru)
    "wood",           # 7  legno (casse)
    "cover",          # 8  muretti e ripari bassi
    "stairs",         # 9  gradini
    "roof",           # 10 coppi (solo decorazione)
    "objective",      # 11 GIALLO: la cassa, i punti di estrazione
    "team_north",     # 12 CIANO: spawn Nord
    "team_south",     # 13 ARANCIO: spawn Sud
    "water",          # 14 mare e bagnasciuga (solo decorazione)
]
K = {name: i for i, name in enumerate(KINDS)}

# la metà sud è la stessa architettura con un'altra pelle (D2: asimmetria cosmetica)
SOUTH_SKIN = {
    K["plaster"]: K["plaster_ochre"],
    K["concrete"]: K["stone"],
    K["steel"]: K["wood"],
    K["team_north"]: K["team_south"],
}


# --------------------------------------------------------------------------- primitive
def rect(x0, x1, z0, z1):
    return (min(x0, x1), max(x0, x1), min(z0, z1), max(z0, z1))


def rot_rect(r):
    x0, x1, z0, z1 = r
    return (-x1, -x0, -z1, -z0)


def path(points, width):
    """Una polilinea larga `width`: i vicoli e le strade."""
    h = width / 2
    out = []
    for (ax, az), (bx, bz) in zip(points, points[1:]):
        if ax == bx:
            out.append(rect(ax - h, ax + h, min(az, bz) - h, max(az, bz) + h))
        elif az == bz:
            out.append(rect(min(ax, bx) - h, max(ax, bx) + h, az - h, az + h))
        else:
            raise ValueError("solo segmenti allineati agli assi")
    return out


class Box:
    __slots__ = ("x0", "y0", "z0", "x1", "y1", "z1", "kind")

    def __init__(self, x0, y0, z0, x1, y1, z1, kind):
        self.x0, self.y0, self.z0 = min(x0, x1), min(y0, y1), min(z0, z1)
        self.x1, self.y1, self.z1 = max(x0, x1), max(y0, y1), max(z0, z1)
        self.kind = kind

    def rotated(self):
        return Box(-self.x1, self.y0, -self.z1, -self.x0, self.y1, -self.z0,
                   SOUTH_SKIN.get(self.kind, self.kind))

    def as_list(self):
        r = lambda v: round(v, 3)
        return [r(self.x0), r(self.y0), r(self.z0),
                r(self.x1 - self.x0), r(self.y1 - self.y0), r(self.z1 - self.z0), self.kind]


def B(x0, x1, y0, y1, z0, z1, kind):
    return Box(x0, y0, z0, x1, y1, z1, kind)


def stairs(axis, start, direction, cross0, cross1, y_base, rise, steps, kind=K["stairs"]):
    """
    Una rampa di scale come pila di scatole piene (niente rampe inclinate: la
    simulazione conosce solo AABB e il gradino da STEP_HEIGHT).
    axis = 'x' o 'z', start = coordinata del primo gradino, direction = +1/-1.
    Alzata e pedata reali del doc 07: 17 cm × 30 cm.
    """
    out = []
    tread = 0.30
    h = rise / steps
    for i in range(steps):
        a = start + direction * i * tread
        b = a + direction * tread
        top = y_base + h * (i + 1)
        if axis == "x":
            out.append(B(a, b, y_base, top, cross0, cross1, kind))
        else:
            out.append(B(cross0, cross1, y_base, top, a, b, kind))
    return out


# --------------------------------------------------------------------------- disegno
#
# LO SCHEMA (preso dalle mappe di Valorant, adattato all'estrazione):
#
#   ogni spawn ha TRE CORSIE corte e spezzate, non corridoi:
#     · centrale  → Salita, LARGO (piazzetta con gradinata), Arco, Via del Mercato a gomito → PIAZZA
#     · est       → Banchina, Varco della Dogana (strozzatura), Cortile della Dogana, MOLO → GROTTA
#     · ovest     → Via Stretta, Piazzetta del Forno (con il forno attraversabile), PIAZZALE → GALLERIA
#   il Largo ha due passaggi coperti verso le corsie laterali (le rotazioni del lato "attacco");
#   la piazza ha due collegamenti verso le estrazioni (Mercato coperto + Cortile): le rotazioni
#   del "difensore", corte come in Valorant.
#
#   Ogni estrazione ha TRE ingressi: uno da ogni squadra (dal Molo / dal Piazzale) e uno dal centro.
#   Così l'anello del doc 02 esiste senza strade dedicate: Nord → corsia est → Grotta ← corsia ovest ← Sud.

def north_half():
    """
    La metà NORD. Tutto viene poi ruotato di 180° per la metà SUD (D2).
    Restituisce (aree aperte, scatole).
    """
    O = []      # aree aperte (strade, piazze, cortili)
    b = []      # volumi dentro le aree aperte (ripari, scale, archi)

    # ======================================================= PORTO VECCHIO (spawn)
    O.append(rect(-20, 20, -99, -82))
    O.append(rect(20, 24, -95, -87))                  # uscita est → Banchina
    for x0, z0, sx, sz in [(-16, -97, 6, 2.4), (10, -97, 6, 2.4)]:          # container
        b.append(B(x0, x0 + sx, 0, 2.6, z0, z0 + sz, K["steel"]))
    for x0, z0 in [(-12, -86.5), (9.5, -86.5)]:                              # casse da 1 m
        b.append(B(x0, x0 + 2.5, 0, 1.0, z0, z0 + 2.5, K["wood"]))
    for x in (-8, -4, 0, 4, 8):                                              # i 5 punti di spawn
        b.append(B(x - 0.6, x + 0.6, 0, 0.02, -91.6, -90.4, K["team_north"]))

    # ======================================================= CORSIA CENTRALE
    O.append(rect(-4, 4, -82, -70))                   # Salita del Porto, 12 m
    O.append(rect(-13, 13, -70, -52))                 # LARGO
    b.append(B(-2, 2, 0, 0.8, -63, -59, K["stone"]))  # fontana: spezza la vista spawn → arco
    b.append(B(-0.4, 0.4, 0.8, 1.9, -61.4, -60.6, K["stone"]))
    b.append(B(-11, -8.5, 0, 1.0, -57, -54.5, K["wood"]))
    # gradinata: un palco a 1,5 m nell'angolo NE del Largo (vede l'arco, non lo spawn)
    b.append(B(7, 13, 0, 1.53, -70, -65, K["stone"]))
    b += stairs("z", -62.3, -1, 7, 13, 0, 1.53, 9)
    # Arco: strozzatura coperta, 6 m
    O.append(rect(-3, 3, -52, -44))
    b.append(B(-3, 3, 3.2, 6.2, -52, -46, K["stone"]))
    # Via del Mercato a gomito: nessuna linea dritta dallo spawn alla piazza
    O.append(rect(-3, 3, -44, -34))
    O.append(rect(-3, 9, -34, -28))
    O.append(rect(3, 9, -28, -18))
    b.append(B(-2.5, -0.5, 0, 1.5, -33, -29, K["wood"]))                    # carretto all'angolo
    # passaggi coperti dal Largo alle corsie laterali
    O.append(rect(-34, -13, -64, -59))
    b.append(B(-26, -16, 3.2, 6.2, -64, -59, K["plaster"]))
    O.append(rect(13, 30, -64, -59))
    b.append(B(16, 26, 3.2, 6.2, -64, -59, K["plaster"]))

    # ======================================================= CORSIA EST → GROTTA (Scalinata Lunga)
    O.append(rect(24, 46, -99, -84))                  # Banchina
    b.append(B(30, 36, 0, 2.6, -97, -94.6, K["steel"]))
    b.append(B(40, 42.4, 0, 2.6, -94, -88, K["steel"]))
    b.append(B(27, 29.5, 0, 1.0, -88, -85.5, K["wood"]))
    O.append(rect(36, 42, -84, -74))                  # Varco della Dogana: 6 m, coperto
    b.append(B(36, 42, 3.4, 6.2, -82, -76, K["concrete"]))
    O.append(rect(28, 54, -74, -52))                  # Cortile della Dogana
    b.append(B(33, 35.4, 0, 1.1, -66, -65.4, K["cover"]))
    b.append(B(44, 46, 0, 1.5, -69, -65, K["wood"]))                         # carro
    b.append(B(48, 50.5, 0, 1.0, -58, -55.5, K["wood"]))
    b.append(B(30, 31.4, 0, 2.6, -57, -54, K["concrete"]))                   # pilastro
    O.append(rect(46, 52, -52, -40))                  # discesa al Molo
    O.append(rect(46, 82, -44, -30))                  # MOLO
    b.append(B(52, 54.5, 0, 1.0, -36, -33.5, K["wood"]))
    b.append(B(62, 64, 0, 1.4, -38, -32, K["wood"]))                         # barca in secca
    b.append(B(76, 78.4, 0, 1.1, -42, -41.4, K["cover"]))
    # Terrazza del Molo: la posizione alta che guarda l'ingresso nord della Grotta
    O.append(rect(58, 72, -56, -44))
    b.append(B(58, 72, 0, 3.4, -56, -50, K["stone"]))
    b += stairs("z", -44, -1, 66, 70, 0, 3.4, 20)
    b.append(B(58, 66, 3.4, 4.45, -50.3, -50, K["stone"]))                   # parapetto
    b.append(B(70, 72, 3.4, 4.45, -50.3, -50, K["stone"]))

    # ======================================================= CORSIA OVEST → GALLERIA (Via Stretta)
    O += path([(-22, -91), (-33, -91), (-33, -74)], 4)                       # Via Stretta, a gomito
    O.append(rect(-58, -30, -74, -56))                # Piazzetta del Forno
    # il forno: una stanza attraversabile, due porte (est/ovest)
    t, H = 0.4, 4.0
    for zs in (-70, -62 - t):
        b.append(B(-50, -40, 0, H, zs, zs + t, K["plaster"]))
    for xs in (-50, -40 - t):
        b.append(B(xs, xs + t, 0, H, -70 + t, -66.8, K["plaster"]))
        b.append(B(xs, xs + t, 0, H, -65.2, -62 - t, K["plaster"]))
        b.append(B(xs, xs + t, 2.05, H, -66.8, -65.2, K["plaster"]))
    b.append(B(-50, -40, H, H + 0.4, -70, -62, K["roof"]))
    b.append(B(-46, -44, 0, 1.1, -69.4, -67.4, K["stone"]))                  # il forno vero
    b.append(B(-56, -54.6, 0, 1.1, -61, -58.6, K["cover"]))
    b.append(B(-36, -33.5, 0, 1.0, -60, -57.5, K["wood"]))
    O.append(rect(-62, -54, -56, -44))                # passaggio verso il Piazzale
    O.append(rect(-82, -56, -44, -30))                # PIAZZALE DELLA GALLERIA
    b.append(B(-68, -62, 0, 3.0, -41, -38.6, K["steel"]))                    # camion fermo
    b.append(B(-79, -77, 0, 0.8, -36, -35.4, K["concrete"]))                 # new jersey
    b.append(B(-60, -58.6, 0, 1.1, -36, -33.6, K["cover"]))

    return O, b


def center():
    """
    PIAZZA VECCHIA (la cassa, nella banca) e i due collegamenti verso le estrazioni.
    Si disegna la parte EST e la si ruota: la piazza è simmetrica per costruzione.
    """
    O = [rect(-20, 20, -18, 18)]
    b = []

    # --- la banca: pareti da 40 cm, 4 porte, la cassa nella cassaforte
    t, H, door = 0.4, 4.6, 0.8
    for zs in (-4, 4 - t):
        b.append(B(-6, -door, 0, H, zs, zs + t, K["stone"]))
        b.append(B(door, 6, 0, H, zs, zs + t, K["stone"]))
        b.append(B(-door, door, 2.05, H, zs, zs + t, K["stone"]))
    for xs in (-6, 6 - t):
        b.append(B(xs, xs + t, 0, H, -4 + t, -door, K["stone"]))
        b.append(B(xs, xs + t, 0, H, door, 4 - t, K["stone"]))
        b.append(B(xs, xs + t, 2.05, H, -door, door, K["stone"]))
    b.append(B(-6, 6, H, H + 0.4, -4, 4, K["stone"]))
    b.append(B(-3.4, -2.8, 0, 1.1, -2, 2, K["wood"]))                        # banconi
    b.append(B(2.8, 3.4, 0, 1.1, -2, 2, K["wood"]))
    b.append(B(-0.6, 0.6, 0, 1.0, -0.4, 0.4, K["objective"]))                # LA CASSA

    east_open = []
    east = []
    # --- ripari della piazza (metà est; la rotazione fa l'altra)
    east.append(B(-1.5, 1.5, 0, 0.8, -12.5, -9.5, K["stone"]))               # fontana nord
    east.append(B(10, 12, 0, 1.0, -15, -13, K["cover"]))                     # fioriera
    east.append(B(-12, -10, 0, 1.0, -15, -13, K["cover"]))
    east.append(B(9.6, 10.4, 0, 1.1, 4, 8, K["cover"]))                      # muretto
    # --- il porticato: secondo livello sul lato est della piazza
    east.append(B(14, 20, 3.0, 3.3, -12, 12, K["stone"]))                    # solaio
    east.append(B(14, 14.3, 3.3, 4.35, -9, 12, K["stone"]))                  # parapetto, varco sulla scala
    for zc in (-10, -6, 6, 10):                                              # pilastri (non nel passaggio)
        east.append(B(14, 14.6, 0, 3.0, zc - 0.3, zc + 0.3, K["stone"]))
    east += stairs("x", 8, +1, -12, -9, 0, 3.3, 20)                          # da x=8 a x=14
    # --- Mercato coperto: una sala a pilastri, non un corridoio
    east_open.append(rect(20, 36, -7, 7))
    east.append(B(20, 36, 4.5, 6.2, -7, 7, K["roof"]))
    for xc in (24, 28, 32):
        for zc in (-3.5, 3.5):
            east.append(B(xc - 0.3, xc + 0.3, 0, 4.5, zc - 0.3, zc + 0.3, K["stone"]))
    east.append(B(26.8, 29.2, 0, 1.0, -1.2, 1.2, K["wood"]))                 # banco del pesce
    # --- Cortile Est, davanti alla Grotta
    east_open.append(rect(36, 52, -9, 9))
    east.append(B(42.5, 46.5, 0, 0.9, -2, 2, K["stone"]))                   # pozzo con statua: niente linea
    east.append(B(43.9, 45.1, 0.9, 2.6, -0.6, 0.6, K["stone"]))              # dritta dalla piazza alla Grotta
    east.append(B(38, 40.5, 0, 1.0, 5, 7.5, K["wood"]))
    east.append(B(48, 50.4, 0, 1.1, -8, -7.4, K["cover"]))

    O += east_open + [rot_rect(r) for r in east_open]
    b += east + [x.rotated() for x in east]
    return O, b


def grotta():
    """
    GROTTA DEL LEVANTE — estrazione EST. Chiusa e verticale: una camera buia, tre
    ingressi stretti (nord dal Molo, sud dal Piazzale ruotato, ovest dal Cortile).
    Simmetrica rispetto a z=0: Nord e Sud ci arrivano allo stesso modo.
    """
    region = rect(52, X_MAX, -28, 28)
    O, b = [], []
    O.append(rect(52, 66, -1.5, 1.5))                 # ingresso frontale: 3 m, soffitto basso
    b.append(B(52, 66, 3.0, 10, -1.5, 1.5, K["rock"]))
    O.append(rect(66, 86, -12, 12))                   # la camera
    b.append(B(66, 86, 6.0, 10, -12, 12, K["rock"]))
    for s in (-1, 1):
        # ingressi laterali a gomito, dal Molo (nord) e dal suo ruotato (sud)
        for r in path([(68, 30 * s), (68, 20 * s), (71, 20 * s), (71, 12 * s)], 3.4):
            O.append(r)
            b.append(B(r[0], r[1], 3.0, 10, r[2], r[3], K["rock"]))
        b.append(B(81, 85, 0, 1.4, 4 * s, 9 * s, K["wood"]))                 # motoscafo
        b.append(B(70, 72, 0, 1.3, 5 * s, 7.5 * s, K["rock"]))               # scoglio
        b.append(B(76.5, 78, 0, 1.0, 2.5 * s, 4 * s, K["wood"]))             # casse
    return region, O, b


def galleria():
    """
    GALLERIA VECCHIA — estrazione OVEST. Lunga e orizzontale: il tunnel si apre
    sul Piazzale a nord e a sud, più l'ingresso di servizio dal Cortile Ovest.
    Linee lunghe dentro, nicchie per non morire allo scoperto. Simmetrica rispetto a z=0.
    """
    region = rect(X_MIN, -62, -30, 30)
    O, b = [], []
    O.append(rect(-78, -66, -30, 30))                 # il tunnel: 12 m × 60 m
    b.append(B(-78, -66, 6.0, 10, -30, 30, K["rock"]))
    O.append(rect(-66, -52, -2, 2))                   # ingresso di servizio dal Cortile Ovest
    b.append(B(-66, -52, 3.0, 10, -2, 2, K["rock"]))
    for s in (-1, 1):
        O.append(rect(-82, -78, 10 * s, 14 * s))      # nicchie di manutenzione
        b.append(B(-82, -78, 3.0, 10, 10 * s, 14 * s, K["rock"]))
        b.append(B(-76, -73.5, 0, 3.0, 3 * s, 10 * s, K["steel"]))           # camion del convoglio
        b.append(B(-70.5, -68, 0, 3.0, 16 * s, 22 * s, K["steel"]))
        b.append(B(-75, -72, 0, 0.8, 25 * s, 25.6 * s, K["concrete"]))       # new jersey
    return region, O, b


# --------------------------------------------------------------------------- isolati
def fill_blocks(open_rects, regions):
    """
    Tutto ciò che non è aperto è edificio pieno (i "gusci chiusi" del doc 07).
    Griglia da 1 m → rettangoli massimi → lotti da ≤ 10 m con altezze di 2-4 piani.
    """
    W, D = X_MAX - X_MIN, Z_MAX - Z_MIN
    solid = [[True] * W for _ in range(D)]
    for (x0, x1, z0, z1) in open_rects:
        for zi in range(max(0, math.floor(z0 - Z_MIN)), min(D, math.ceil(z1 - Z_MIN))):
            row = solid[zi]
            for xi in range(max(0, math.floor(x0 - X_MIN)), min(W, math.ceil(x1 - X_MIN))):
                row[xi] = False

    def region_of(xi, zi):
        x, z = xi + X_MIN + 0.5, zi + Z_MIN + 0.5
        for name, (x0, x1, z0, z1) in regions:
            if x0 <= x < x1 and z0 <= z < z1:
                return name
        return "town"

    label = [[region_of(xi, zi) if solid[zi][xi] else None for xi in range(W)] for zi in range(D)]
    used = [[False] * W for _ in range(D)]
    rects = []
    for zi in range(D):
        for xi in range(W):
            if label[zi][xi] is None or used[zi][xi]:
                continue
            lab = label[zi][xi]
            x2 = xi
            while x2 + 1 < W and label[zi][x2 + 1] == lab and not used[zi][x2 + 1]:
                x2 += 1
            z2 = zi
            while z2 + 1 < D and all(label[z2 + 1][k] == lab and not used[z2 + 1][k]
                                     for k in range(xi, x2 + 1)):
                z2 += 1
            for zz in range(zi, z2 + 1):
                for xx in range(xi, x2 + 1):
                    used[zz][xx] = True
            rects.append((lab, xi + X_MIN, x2 + 1 + X_MIN, zi + Z_MIN, z2 + 1 + Z_MIN))

    def split(a, b, max_len=10):
        n = max(1, math.ceil((b - a) / max_len))
        step = (b - a) / n
        return [(a + i * step, a + (i + 1) * step) for i in range(n)]

    boxes = []
    for lab, x0, x1, z0, z1 in rects:
        for (lx0, lx1) in split(x0, x1):
            for (lz0, lz1) in split(z0, z1):
                cx, cz = (lx0 + lx1) / 2, (lz0 + lz1) / 2
                h = (int(abs(cx) * 7 + abs(cz) * 13)) % 3
                if lab in ("grotta", "galleria"):
                    boxes.append(B(lx0, lx1, 0, 10.0, lz0, lz1, K["rock"]))
                    continue
                height = (6.2, 9.0, 11.8)[h]
                if cz < 0:
                    kind = (K["plaster"], K["plaster"], K["concrete"])[h]
                else:
                    kind = (K["plaster_ochre"], K["stone"], K["plaster_ochre"])[h]
                boxes.append(B(lx0, lx1, 0, height, lz0, lz1, kind))
    return boxes


# --------------------------------------------------------------------------- tetti
BUILDING_KINDS = {K["plaster"], K["plaster_ochre"], K["stone"], K["concrete"]}


def hrand(cx, cz, salt):
    """Hash deterministico 0..1: la stessa città a ogni generazione."""
    h = (int(cx * 8) * 374761393 + int(cz * 8) * 668265263 + salt * 2246822519) & 0xFFFFFFFF
    h = ((h ^ (h >> 13)) * 1274126177) & 0xFFFFFFFF
    return ((h ^ (h >> 16)) & 0xFFFF) / 65535.0


def roofscape(boxes):
    """
    I tetti: coppi, comignoli, corpi scala, serbatoi, antenne. SOLO decorazione —
    non entrano nella collisione (la simulazione conosce gli AABB di `boxes`).

    Il tetto è una lastra rientrata di 35 cm: resta una cornice chiara del muro
    intorno al tetto, e due lotti vicini mostrano una cucitura invece di fondersi
    in un piano unico. È quello che manca al graybox visto dall'alto: senza tetti
    la città è una distesa di facciate chiare.
    """
    out = []
    for b in boxes:
        if b.kind not in BUILDING_KINDS:
            continue
        if b.y1 - b.y0 < 5.0:          # archi e porticati non hanno tetto
            continue
        x0, x1 = b.x0 + 0.35, b.x1 - 0.35
        z0, z1 = b.z0 + 0.35, b.z1 - 0.35
        if x1 - x0 < 1.0 or z1 - z0 < 1.0:
            continue
        cx, cz = (b.x0 + b.x1) / 2, (b.z0 + b.z1) / 2
        top = b.y1 + 0.42
        out.append(B(x0, x1, b.y1, top, z0, z1, K["roof"]))

        # comignolo: quasi tutti i tetti ne hanno uno
        if hrand(cx, cz, 1) > 0.30:
            ox = x0 + 0.55 + hrand(cx, cz, 2) * max(0.0, (x1 - x0) - 1.8)
            oz = z0 + 0.55 + hrand(cx, cz, 3) * max(0.0, (z1 - z0) - 1.8)
            out.append(B(ox, ox + 0.62, top, top + 1.25, oz, oz + 0.62, K["cover"]))
            out.append(B(ox - 0.08, ox + 0.70, top + 1.25, top + 1.37, oz - 0.08, oz + 0.70, K["cover"]))

        # corpo scala / abbaino: dà volume allo skyline
        if hrand(cx, cz, 4) > 0.52 and (x1 - x0) > 3.4 and (z1 - z0) > 3.2:
            w, d = 2.6, 2.3
            ox = x0 + 0.5 + hrand(cx, cz, 5) * ((x1 - x0) - w - 1.0)
            oz = z0 + 0.5 + hrand(cx, cz, 6) * ((z1 - z0) - d - 1.0)
            out.append(B(ox, ox + w, top, top + 2.25, oz, oz + d, K["cover"]))
            out.append(B(ox - 0.14, ox + w + 0.14, top + 2.25, top + 2.57,
                         oz - 0.14, oz + d + 0.14, K["roof"]))

        # serbatoio dell'acqua
        if hrand(cx, cz, 7) > 0.87 and (x1 - x0) > 3.0 and (z1 - z0) > 2.2:
            ox = x0 + 0.7 + hrand(cx, cz, 8) * max(0.0, (x1 - x0) - 2.2)
            oz = z0 + 0.7 + hrand(cx, cz, 9) * max(0.0, (z1 - z0) - 2.2)
            out.append(B(ox, ox + 1.5, top, top + 1.55, oz, oz + 1.5, K["steel"]))

        # antenna: un filo d'acciaio contro il cielo
        if hrand(cx, cz, 10) > 0.74:
            ox = x0 + 0.3 + hrand(cx, cz, 11) * max(0.0, (x1 - x0) - 0.4)
            oz = z0 + 0.3 + hrand(cx, cz, 12) * max(0.0, (z1 - z0) - 0.4)
            out.append(B(ox, ox + 0.08, top, top + 2.6, oz, oz + 0.08, K["steel"]))
    return out


# --------------------------------------------------------------------------- mappa
def build():
    n_open, n_boxes = north_half()
    c_open, c_boxes = center()
    g_region, g_open, g_boxes = grotta()
    v_region, v_open, v_boxes = galleria()

    open_rects = n_open + [rot_rect(r) for r in n_open] + c_open + g_open + v_open
    boxes = [B(X_MIN, X_MAX, -1, 0, Z_MIN, Z_MAX, K["street"])]   # il suolo
    boxes += n_boxes + [b.rotated() for b in n_boxes]
    boxes += c_boxes + g_boxes + v_boxes
    boxes += fill_blocks(open_rects, [("grotta", g_region), ("galleria", v_region)])

    decor = []
    decor.append(B(-400, 400, -2.2, -2.0, -400, 400, K["water"]))   # il mare
    decor.append(B(-6, 6, -2.0, 0, -112, -100, K["concrete"]))      # molo oltre lo spawn
    decor.append(B(14, 16, 0, 30, -110, -108, K["steel"]))          # gru del porto
    decor.append(B(-4, 30, 28, 29.6, -109.8, -108.2, K["steel"]))
    decor.append(B(-22, 22, -2.0, 0, 100, 114, K["stone"]))         # bastione del forte
    decor.append(B(-18, -8, 0, 24, 102, 112, K["stone"]))           # torre del forte
    decor.append(B(22, 27, 11.8, 28, -26, -21, K["stone"]))         # campanile, sopra un isolato
    decor.append(B(21.6, 27.4, 28, 29.4, -26.4, -20.6, K["roof"]))
    decor.append(B(75, 79, 0, 0.03, -2, 2, K["objective"]))         # punto di estrazione Est
    decor.append(B(-74, -70, 0, 0.03, -2, 2, K["objective"]))       # punto di estrazione Ovest
    decor += roofscape(boxes)                                        # tetti: coppi, comignoli, antenne

    spawns_n = [[x, 0.1, -91.0] for x in (-8, -4, 0, 4, 8)]
    spawns_s = [[-x, 0.1, 91.0] for x in (-8, -4, 0, 4, 8)]

    points = [
        ("Porto Vecchio", (0, 0, -91)),
        ("Forte San Verano", (0, 0, 91)),
        ("Largo", (-6, 0, -55)),
        ("Piazza Vecchia", (6, 0, -16)),
        ("Cassa", (0, 0, -2.0)),
        ("Porticato", (17, 3.3, 0)),
        ("Mercato", (22, 0, 0)),
        ("Grotta del Levante", (77, 0, 0)),
        ("Galleria Vecchia", (-72, 0, 0)),
        ("Scalinata Lunga", (41, 0, -62)),
        ("Molo", (56, 0, -38)),
        ("Terrazza del Molo", (63, 3.4, -53)),
        ("Via Stretta", (-33, 0, -82)),
        ("Piazzetta del Forno", (-36, 0, -66)),
        ("Lungomare", (33, 0, 82)),
        ("Mura", (-41, 0, 62)),
    ]
    return {
        "format": 1,
        "name": "Calanera",
        "note": "GENERATO da tools/mapgen/calanera.py — non modificare a mano.",
        "bounds": [X_MIN, Z_MIN, X_MAX, Z_MAX],
        "kinds": KINDS,
        "boxes": [b.as_list() for b in boxes],
        "decor": [b.as_list() for b in decor],
        "spawns": {"north": spawns_n, "south": spawns_s},
        "points": [{"name": n, "pos": list(p)} for n, p in points],
    }, boxes


# --------------------------------------------------------------------------- verifica
def simplify(points):
    """Toglie i punti intermedi sui tratti dritti: restano gli angoli."""
    if len(points) < 3:
        return points
    out = [points[0]]
    for a, b_, c in zip(points, points[1:], points[2:]):
        d1 = (round(b_[0] - a[0], 3), round(b_[1] - a[1], 3))
        d2 = (round(c[0] - b_[0], 3), round(c[1] - b_[1], 3))
        if d1 != d2:
            out.append(b_)
    out.append(points[-1])
    return out


def verify(data, boxes):
    import simport
    print("CALANERA — verifica del graybox (doc 02)")
    print(f"  ingombro {X_MAX - X_MIN} × {Z_MAX - Z_MIN} m · scatole {len(data['boxes'])} · decorazione {len(data['decor'])}")
    nav = Nav(boxes)
    print(f"  celle percorribili: {sum(len(v) for v in nav.nodes.values())}")

    pts = {p["name"]: p["pos"] for p in data["points"]}

    def node(x, y, z, label):
        n = nav.node_at(x, y, z)
        if n is None:
            raise SystemExit(f"  FAIL  '{label}' ({x}, {y}, {z}) non è su un pavimento libero")
        return n

    def pnode(name):
        x, y, z = pts[name]
        return node(x, y, z, name)

    ok = True

    def check(label, cond, detail=""):
        nonlocal ok
        ok &= bool(cond)
        print(f"  {'OK  ' if cond else 'FAIL'}  {label}  {detail}")

    in_center = lambda x, z: abs(x) < 20 and abs(z) < 18
    spawn = {"Nord": pnode("Porto Vecchio"), "Sud": pnode("Forte San Verano")}
    exits = {"Grotta": pnode("Grotta del Levante"), "Galleria": pnode("Galleria Vecchia")}
    crate = {"Nord": node(0, 0, -1.4, "cassa lato nord"), "Sud": node(0, 0, 1.4, "cassa lato sud")}
    boxes_near = boxes

    routes = []

    def route(name, start, goal, forbidden=None, expect_y=None, timed=False):
        d, p = nav.shortest(start, goal, forbidden, want_path=True)
        if d is None:
            check(name, False, "nessun percorso")
            return None
        wps = simplify([(nav.x_of(n), nav.z_of(n)) for n in p])
        feet = (nav.x_of(start), start[2] + 0.1, nav.z_of(start))
        arrived, t, pl, _ = simport.walk(boxes_near, feet, wps[1:], max_s=180)
        fy = pl.pos[1] - PLAYER_HEIGHT / 2
        good = arrived and (expect_y is None or abs(fy - expect_y) < 0.06)
        check(name, good, f"{d:6.1f} m · camminando {t:5.1f} s" + ("" if arrived else f" · BLOCCATO a ({pl.pos[0]:.1f}, {pl.pos[2]:.1f})"))
        r = {"name": name, "from": [round(feet[0], 2), round(feet[1], 2), round(feet[2], 2)],
             "waypoints": [[round(x, 2), round(z, 2)] for x, z in wps[1:]],
             "avoids_center": forbidden is not None, "walk_s": round(t, 2)}
        if expect_y is not None:
            r["end_feet_y"] = expect_y
        if timed:
            r["timed"] = True
        routes.append(r)
        return d, t

    res = {}
    for team in ("Nord", "Sud"):
        res[(team, "cassa")] = route(f"{team} → cassa", spawn[team], crate[team], timed=True)
        for e in exits:
            res[(team, e)] = route(f"{team} → {e} per la sua corsia, senza la piazza",
                                   spawn[team], exits[e], forbidden=in_center)
    for e in exits:
        route(f"cassa → {e}", crate["Nord"], exits[e])
    route("Grotta ↔ Galleria senza la piazza", exits["Grotta"], exits["Galleria"], forbidden=in_center)
    route("scala del porticato", node(8, 0, -14, "piede della scala"), pnode("Porticato"), expect_y=3.3)
    route("scala della Terrazza del Molo", pnode("Molo"), pnode("Terrazza del Molo"), expect_y=3.4)
    route("gradinata del Largo", pnode("Largo"), node(10, 1.53, -68, "palco"), expect_y=1.53)

    # equità
    a, b_ = res[("Nord", "cassa")][0], res[("Sud", "cassa")][0]
    check("equità Nord/Sud verso la cassa", abs(a - b_) < 0.02 * max(a, b_), f"{a:.1f} vs {b_:.1f}")
    for e1, e2 in (("Grotta", "Galleria"), ("Galleria", "Grotta")):
        a, b_ = res[("Nord", e1)][0], res[("Sud", e2)][0]
        check(f"rotazione: Nord→{e1} = Sud→{e2}", abs(a - b_) < 0.04 * max(a, b_), f"{a:.1f} vs {b_:.1f}")
    for team in ("Nord", "Sud"):
        a, b_ = res[(team, "Grotta")][0], res[(team, "Galleria")][0]
        check(f"squilibrio Grotta/Galleria per {team} ≤ 8%", abs(a - b_) < 0.08 * max(a, b_), f"{a:.1f} vs {b_:.1f}")
    for p in data["points"]:
        check(f"'{p['name']}' raggiungibile da Nord", nav.shortest(spawn["Nord"], pnode(p["name"]))[0] is not None)

    data["routes"] = routes
    return ok, nav


CELL = 0.5


class Nav:
    """
    Grafo di percorribilità con le STESSE regole della simulazione: AABB del
    giocatore 0,6 × 1,8 × 0,6, gradino massimo STEP_HEIGHT. Niente salti: una rotta
    che richiede un salto non è una rotta. Le celle a ridosso dei muri costano di più,
    così i percorsi stanno in mezzo alla strada come un giocatore vero.
    """

    def __init__(self, boxes):
        self.boxes = boxes
        self.bucket = {}
        for i, b in enumerate(boxes):
            for bx in range(math.floor(b.x0 / 4), math.floor(b.x1 / 4) + 1):
                for bz in range(math.floor(b.z0 / 4), math.floor(b.z1 / 4) + 1):
                    self.bucket.setdefault((bx, bz), []).append(i)
        self.nx = int((X_MAX - X_MIN) / CELL)
        self.nz = int((Z_MAX - Z_MIN) / CELL)
        self.nodes = {}   # (ix, iz) -> [y...]
        self._build()

    def near(self, x0, x1, z0, z1):
        seen = set()
        for bx in range(math.floor(x0 / 4), math.floor(x1 / 4) + 1):
            for bz in range(math.floor(z0 / 4), math.floor(z1 / 4) + 1):
                for i in self.bucket.get((bx, bz), ()):
                    if i not in seen:
                        seen.add(i)
                        yield self.boxes[i]

    def _build(self):
        r = PLAYER_RADIUS
        for iz in range(self.nz):
            cz = Z_MIN + (iz + 0.5) * CELL
            for ix in range(self.nx):
                cx = X_MIN + (ix + 0.5) * CELL
                near = list(self.near(cx - r, cx + r, cz - r, cz + r))
                tops = set()
                for b in near:
                    if b.x1 > cx - r and b.x0 < cx + r and b.z1 > cz - r and b.z0 < cz + r:
                        tops.add(round(b.y1, 3))
                ys = []
                for y in sorted(tops):
                    blocked = False
                    for b in near:
                        if (b.x1 > cx - r + 1e-4 and b.x0 < cx + r - 1e-4 and b.z1 > cz - r + 1e-4
                                and b.z0 < cz + r - 1e-4 and b.y1 > y + 1e-4 and b.y0 < y + PLAYER_HEIGHT):
                            blocked = True
                            break
                    if not blocked:
                        ys.append(y)
                if ys:
                    self.nodes[(ix, iz)] = ys

    def x_of(self, n):
        return X_MIN + (n[0] + 0.5) * CELL

    def z_of(self, n):
        return Z_MIN + (n[1] + 0.5) * CELL

    def cell(self, x, z):
        return (int((x - X_MIN) / CELL), int((z - Z_MIN) / CELL))

    def node_at(self, x, y, z):
        c = self.cell(x, z)
        ys = self.nodes.get(c, [])
        best = min(ys, key=lambda v: abs(v - y), default=None)
        if best is None or abs(best - y) > 0.5:
            return None
        return (c[0], c[1], best)

    def _ok(self, c, y, steps=1):
        return any(abs(v - y) <= STEP_HEIGHT * steps + 1e-3 for v in self.nodes.get(c, ()))

    def _crowded(self, n):
        """Vero se entro 1 m c'è un muro o un dislivello non percorribile (una scala no)."""
        ix, iz, y = n
        for dx in (-2, -1, 0, 1, 2):
            for dz in (-2, -1, 0, 1, 2):
                if not self._ok((ix + dx, iz + dz), y, max(1, abs(dx), abs(dz))):
                    return True
        return False

    def neighbours(self, n):
        ix, iz, y = n
        for dx, dz in ((1, 0), (-1, 0), (0, 1), (0, -1), (1, 1), (1, -1), (-1, 1), (-1, -1)):
            c = (ix + dx, iz + dz)
            for y2 in self.nodes.get(c, ()):
                if abs(y2 - y) > STEP_HEIGHT + 1e-3:
                    continue
                if dx and dz and not (self._ok((ix + dx, iz), y) and self._ok((ix, iz + dz), y)):
                    continue   # niente tagli d'angolo
                yield (c[0], c[1], y2), CELL * (1.4142 if dx and dz else 1.0)

    def shortest(self, start, goal, forbidden=None, want_path=False):
        """
        Dijkstra. Restituisce (distanza, percorso). La distanza è quella geometrica;
        la scelta del percorso evita di strisciare lungo i muri. `forbidden(x, z)` esclude zone.
        """
        goals = goal if isinstance(goal, (set, frozenset)) else {goal}
        cost = {start: 0.0}
        dist = {start: 0.0}
        prev = {}
        crowd = {}
        pq = [(0.0, start)]
        while pq:
            c, n = heapq.heappop(pq)
            if n in goals:
                if not want_path:
                    return dist[n], None
                p = [n]
                while p[-1] in prev:
                    p.append(prev[p[-1]])
                return dist[n], p[::-1]
            if c > cost.get(n, 1e18):
                continue
            for m, w in self.neighbours(n):
                if forbidden and forbidden(self.x_of(m), self.z_of(m)):
                    continue
                if m not in crowd:
                    crowd[m] = self._crowded(m)
                nc = c + w * (2.0 if crowd[m] else 1.0)
                if nc < cost.get(m, 1e18):
                    cost[m] = nc
                    dist[m] = dist[n] + w
                    prev[m] = n
                    heapq.heappush(pq, (nc, m))
        return None, None


def preview(data, nav):
    from PIL import Image, ImageDraw
    s = 4
    W, D = (X_MAX - X_MIN) * s, (Z_MAX - Z_MIN) * s
    img = Image.new("RGB", (W, D), (40, 44, 50))
    dr = ImageDraw.Draw(img)
    pal = {
        "street": (110, 106, 98), "plaster": (216, 169, 122), "plaster_ochre": (201, 138, 75),
        "stone": (201, 191, 168), "rock": (95, 92, 88), "concrete": (140, 136, 128),
        "steel": (85, 88, 92), "wood": (138, 107, 74), "cover": (175, 168, 150),
        "stairs": (160, 150, 130), "roof": (156, 91, 63), "objective": (242, 194, 48),
        "team_north": (46, 155, 230), "team_south": (232, 99, 46), "water": (70, 90, 105),
    }
    P = lambda x, z: ((x - X_MIN) * s, (z - Z_MIN) * s)
    rows = sorted(data["boxes"][1:], key=lambda b: b[1] + b[4])
    for b in rows:
        x0, y0, z0, sx, sy, sz, k = b
        c = pal[KINDS[k]]
        shade = 0.55 + 0.45 * min(1.0, (y0 + sy) / 12.0) if KINDS[k] not in ("objective",) else 1
        c = tuple(int(v * shade) for v in c)
        dr.rectangle([P(x0, z0), P(x0 + sx, z0 + sz)], fill=c, outline=(30, 30, 30))
    for r in data.get("routes", []):
        pts = [P(r["from"][0], r["from"][2])] + [P(x, z) for x, z in r["waypoints"]]
        col = (120, 255, 140) if r["avoids_center"] else (255, 255, 255)
        dr.line(pts, fill=col, width=2)
    for b in data["decor"]:
        x0, y0, z0, sx, sy, sz, k = b
        if KINDS[k] in ("objective",):
            dr.rectangle([P(x0, z0), P(x0 + sx, z0 + sz)], outline=(242, 194, 48), width=3)
    for p in data["points"]:
        x, _, z = p["pos"]
        px, pz = P(x, z)
        dr.ellipse([px - 5, pz - 5, px + 5, pz + 5], fill=(255, 255, 255))
        dr.text((px + 8, pz - 6), p["name"], fill=(255, 255, 255))
    dr.text((10, 10), "N ↑  (−Z)   CALANERA graybox — vista dall'alto, 1 px = 25 cm · linee: rotte verificate (verde = senza piazza)", fill=(255, 255, 255))
    img.save(OUT_PNG)
    print(f"  anteprima: {OUT_PNG}")


def write_json(data):
    OUT_JSON.parent.mkdir(parents=True, exist_ok=True)
    with open(OUT_JSON, "w", encoding="utf-8") as f:
        f.write("{\n")
        keys = list(data.keys())
        for i, k in enumerate(keys):
            v = data[k]
            end = ",\n" if i < len(keys) - 1 else "\n"
            if k in ("boxes", "decor", "routes"):
                f.write(f'  "{k}": [\n')
                f.write(",\n".join("    " + json.dumps(b, separators=(",", ":"), ensure_ascii=False) for b in v))
                f.write("\n  ]" + end)
            else:
                f.write(f'  "{k}": ' + json.dumps(v, ensure_ascii=False) + end)
        f.write("}\n")
    print(f"scritto {OUT_JSON.relative_to(ROOT)}")


def main():
    data, boxes = build()
    ok, nav = verify(data, boxes)     # aggiunge a data le rotte verificate, che i test C# ripercorrono
    write_json(data)
    if "--preview" in sys.argv:
        preview(data, nav)
    print("ESITO:", "OK" if ok else "FALLITO")
    return 0 if ok else 1


if __name__ == "__main__":
    sys.exit(main())
