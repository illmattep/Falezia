"""Dal layout del blockout al LAYOUT DEL KIT: quali pezzi, dove, con che rotazione e scala.

Unity non costruisce più nulla: legge questo file e mette in scena i pezzi già fatti.
"""
import json, math, os

ROOT = "/home/matteo/Progetti/Falezia"
MAP  = os.path.join(ROOT, "game/maps/calanera.json")
OUT  = os.path.join(ROOT, "Assets/StreamingAssets/map/props.json")
m = json.load(open(MAP)); K = m["kinds"]

BUILD = ("plaster", "plaster_ochre", "stone", "concrete")
items = []

def add(model, x, y, z, rot=0.0, scale=1.0):
    items.append({"model": f"map/kit/{model}", "x": round(x,2), "y": round(y,2),
                  "z": round(z,2), "rot": round(rot,1), "scale": round(scale,3)})

# ---------- 1. piano piatto (il terreno base) ----------
add("piano", 0, 0, 0, 0, 1.0)

# ---------- 2. case, orientate verso lo spazio aperto ----------
BUILD_BOXES = [b for b in m["boxes"] if K[b[6]] in BUILD and b[4] >= 3.0 and b[3] >= 2.5 and b[5] >= 2.5]

def clearance(b, side, boxes, maxd=40.0):
    """Quanto spazio libero c'è davanti a quel lato (per orientare la facciata)."""
    x0, z0, w, d = b[0], b[2], b[3], b[5]
    step = 2.0; dist = 0.0
    while dist < maxd:
        dist += step
        if side == "-z":  px, pz = x0 + w/2, z0 - dist
        elif side == "+z":px, pz = x0 + w/2, z0 + d + dist
        elif side == "-x":px, pz = x0 - dist, z0 + d/2
        else:             px, pz = x0 + w + dist, z0 + d/2
        for o in boxes:
            if o is b: continue
            if o[0] <= px <= o[0] + o[3] and o[2] <= pz <= o[2] + o[5]:
                return dist
    return maxd

ROT = {"-z": 0.0, "-x": 90.0, "+z": 180.0, "+x": 270.0}
n_casa = 0
for b in BUILD_BOXES:
    x0, y0, z0, w, h, d = b[0], b[1], b[2], b[3], b[4], b[5]
    cx, cz = x0 + w/2, z0 + d/2
    # modello scelto per altezza, con varietà data dalla posizione (deterministica)
    pick = abs(int(x0 * 7 + z0 * 13)) % 10
    if h < 6.8:
        opts = [("casa_1f_ocra", 6.4, 7.0), ("casa_1f_crema", 7.0, 7.6), ("casa_bottega", 9.4, 7.4),
                ("casa_1f_giallo", 6.8, 7.2)]
    elif h < 10.0:
        opts = [("casa_2f_ocra", 8.0, 8.0), ("casa_2f_terra", 7.4, 8.4),
                ("casa_2f_crema", 8.6, 7.8), ("casa_2f_rosa", 7.8, 8.2),
                ("casa_2f_salvia", 8.2, 7.6)]
    else:
        opts = [("casa_3f_ocra", 7.6, 9.0), ("casa_3f_terra", 8.0, 9.4), ("casa_3f_rosa", 7.4, 8.8)]
    model, mw, md = opts[pick % len(opts)]
    # allineamento alla griglia: la facciata guarda il lato corto del lotto
    side = "-z" if d <= w else "-x"
    rot = ROT[side]
    fw, fd = (mw, md) if rot in (0.0, 180.0) else (md, mw)
    scale = max(0.52, min(w / (fw * 1.16), d / (fd * 1.16), 0.98))   # margine: i tetti non si compenetrano
    add(model, cx, y0, cz, rot, scale)
    n_casa += 1

    # marciapiede davanti alla facciata
    L = (w if side in ("-z", "+z") else d) * scale
    off = (d * scale / 2 + 0.9) if side in ("-z", "+z") else (w * scale / 2 + 0.9)
    if side == "-z":  add("marciapiede", cx, y0, cz - off, 90.0, max(0.5, L/6.0))
    elif side == "+z":add("marciapiede", cx, y0, cz + off, 90.0, max(0.5, L/6.0))
    elif side == "-x":add("marciapiede", cx - off, y0, cz, 0.0, max(0.5, L/6.0))
    else:             add("marciapiede", cx + off, y0, cz, 0.0, max(0.5, L/6.0))

# ---------- 3. scale ----------
n_scala = 0
for b in m["boxes"]:
    if K[b[6]] != "stairs" or b[4] < 0.6:
        continue
    x0, y0, z0, w, h, d = b[0], b[1], b[2], b[3], b[4], b[5]
    if w < 1.2 and d < 1.2:
        continue
    rot = 0.0 if w >= d else 90.0
    scale = max(0.5, min((w if rot == 0 else d) / 2.4, (d if rot == 0 else w) / 3.2, 2.2))
    add("scala", x0 + w/2, y0, z0 + d/2, rot, scale)
    n_scala += 1
    if n_scala > 40:
        break

# ---------- 4. mura del Forte (merli) ----------
n_muro = 0
x = -24.0
while x < 24.0:                     # muraglia sud, merlata
    add("muro_forte", x + 3.75, 0, 96.0, 0.0, 1.0)
    n_muro += 1
    x += 7.5

# ---------- 5. porto ----------
for x, z in ((-34, -90), (30, -90)):
    add("gru", x, 0, z, 0, 1.0)
for i, (x, z) in enumerate([(-34,-86),(-28,-86),(34,-86),(40,-86),(-58,-86),(60,-86)]):
    add("container", x, 0, z, 0, 1.0)
for x in range(-84, 85, 12):
    if abs(x) < 12: continue
    add("bitta", x, 0, -90.5, 0, 1.0)
for i in range(12):
    x = -76 + i*13
    if abs(x) < 20: continue
    add("cassa" if i % 2 == 0 else "barile", x, 0, -88.5 + (i % 3), 0, 1.0)

# ---------- 5b. piazza: fontana, alberi, bancarelle ----------
add("piazza_lastrico", 6.0, 0, -14.0, 0, 1.0)
add("fontana", 4.0, 0, -10.0, 0, 1.0)
add("campanile", -13.0, 0, -30.0, 0, 1.0)
for i, (x, z) in enumerate([(-16,-26),(-6,-30),(16,-28),(24,-20),(-18,-12),(14,-6)]):
    add("albero", x, 0, z, 0, 0.9 + 0.1*(i % 3))
for i in range(5):
    add("bancarella", -10 + i*4.4, 0, 8.5, 0, 1.0)
for i in range(4):
    add("bancarella", -6 + i*4.4, 0, 14.0, 0, 1.0)
for i in range(8):
    add("vaso", -12 + i*3.4, 0, -30.0, 0, 1.0)
# lampioni e panche lungo il perimetro della piazza
for x in (-16, -8, 4, 12, 20):
    add("lampada", x, 0, -23.5, 0, 1.0)
for x in (-14, -2, 10, 18):
    add("lampada", x, 0, -5.0, 0, 1.0)
for x in (-12, 0, 12):
    add("panca", x, 0, -19.0, 180.0, 1.0)
    add("panca", x, 0, -9.0, 0.0, 1.0)

# ---------- 6. arredi ----------
for x, z in ((-8,-52),(12,-20),(24,-2),(-14,-40),(40,10),(-30,20)):
    add("panca", x, 0, z, 0, 1.0)
for i, b in enumerate(m["boxes"]):
    if K[b[6]] not in BUILD or b[4] < 3.5: continue
    if abs(i) % 17: continue
    add("lampada", b[0] + b[3]/2, b[1], b[2] - 0.6, 0, 1.0)
for i, b in enumerate(m["boxes"]):
    if K[b[6]] not in BUILD or b[4] < 3.5: continue
    if abs(i) % 23: continue
    add("vaso", b[0] + 0.9, b[1], b[2] - 0.8, 0, 1.0)

json.dump({"items": items}, open(OUT, "w"), indent=0, ensure_ascii=False)
print(f"layout: {len(items)} pezzi · case {n_casa} · scale {n_scala} · merli {n_muro} · arredi vari")
