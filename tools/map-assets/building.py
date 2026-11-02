"""Costruttore di edifici veri (P0): pareti con aperture, davanzali, cornice, tetto a falde.

Spazio di lavoro = coordinate MAPPA: X larghezza, Y quota, Z profondità.
Alla fine tutto viene ruotato in Blender (Z in su) e esportato in coordinate mondo.

  blender -b -noaudio --factory-startup --python building.py -- <lotto> [out.blend]
"""
import bpy, bmesh, json, math, os, sys
from mathutils import Matrix

ROOT = "/home/matteo/Progetti/Falezia"
TEX = f"{ROOT}/Assets/Resources/MapTex"
MAP = json.load(open(f"{ROOT}/game/maps/calanera.json"))
KINDS = MAP["kinds"]

WALL_T, REVEAL, SILL_OUT, CORNICE, ZOCCOLO = 0.30, 0.16, 0.09, 0.26, 0.95
TO_BLENDER = Matrix(((1,0,0,0),(0,0,-1,0),(0,1,0,0),(0,0,0,1)))   # (x,y,z)->(x,-z,y)

# ---------------------------------------------------------------- palette
# Ogni zona ha la sua famiglia di colori: si riconosce a colpo d'occhio.
ZONES = {
    "piazza":  {"c": [(0.86,0.72,0.50),(0.78,0.56,0.34),(0.90,0.78,0.60),(0.66,0.52,0.38),(0.88,0.68,0.44),(0.72,0.62,0.48)],
                "shutter": [(0.22,0.32,0.22),(0.30,0.40,0.30),(0.34,0.42,0.52),(0.55,0.50,0.42)]},
    "mercato": {"c": [(0.96,0.58,0.30),(0.88,0.44,0.24),(1.00,0.70,0.38),(0.78,0.38,0.24),(1.00,0.80,0.48)],
                "shutter": [(0.45,0.30,0.20),(0.55,0.22,0.18),(0.30,0.30,0.34),(0.62,0.44,0.26)]},
    "porto":   {"c": [(0.62,0.70,0.78),(0.50,0.58,0.68),(0.74,0.78,0.80),(0.56,0.64,0.60),(0.44,0.52,0.62)],
                "shutter": [(0.20,0.26,0.32),(0.24,0.30,0.26),(0.34,0.36,0.40)]},
    "forte":   {"c": [(0.78,0.66,0.50),(0.68,0.54,0.40),(0.84,0.72,0.54),(0.60,0.48,0.36)],
                "shutter": [(0.42,0.20,0.16),(0.36,0.24,0.18),(0.30,0.28,0.24)]},
    "vicoli":  {"c": [(0.78,0.70,0.58),(0.72,0.64,0.52),(0.84,0.76,0.62),(0.66,0.60,0.50)],
                "shutter": [(0.28,0.36,0.28),(0.30,0.38,0.46),(0.50,0.44,0.36),(0.26,0.26,0.28)]},
}

def zone_of(x, z):
    if (x-6)**2 + (z+16)**2 < 55**2: return "piazza"
    if (x-22)**2 + z**2 < 45**2:     return "mercato"
    if z < -70:                       return "porto"
    if z > 60:                        return "forte"
    return "vicoli"

def choose(seq, seed):
    return seq[abs(seed * 2654435761) % len(seq)]

# cache materiali dipinti
_PAINTED = {}

def painted(kind, tex, color, rough=0.86, metal=0.0, compress=0.0):
    """Materiale dipinto: texture × colore (multiply), opaco. La tinta è cotta nel materiale."""
    # con le foto la tinta va compressa verso il bianco, altrimenti il muro si spegne;
    # legno e persiane invece tengono il colore pieno (compress=0)
    if compress > 0.0:
        color = tuple(1.0 - (1.0 - c) * compress for c in color)
    key = (kind, tuple(round(c, 3) for c in color))
    if key in _PAINTED: return _PAINTED[key]
    m = bpy.data.materials.new(f"{kind}_{int(color[0]*255):02x}{int(color[1]*255):02x}{int(color[2]*255):02x}")
    m.use_nodes = True
    nt = m.node_tree; b = nt.nodes["Principled BSDF"]
    img_path = f"{TEX}/{tex}.png"
    if os.path.exists(img_path):
        img = nt.nodes.new("ShaderNodeTexImage"); img.image = bpy.data.images.load(img_path)
        mix = nt.nodes.new("ShaderNodeMixRGB"); mix.blend_type = 'MULTIPLY'
        mix.inputs["Fac"].default_value = 1.0
        mix.inputs["Color2"].default_value = (*color, 1)
        nt.links.new(img.outputs["Color"], mix.inputs["Color1"])
        nt.links.new(mix.outputs["Color"], b.inputs["Base Color"])
    else:
        b.inputs["Base Color"].default_value = (*color, 1)
    b.inputs["Roughness"].default_value = rough
    b.inputs["Metallic"].default_value = metal
    _PAINTED[key] = m
    return m

def mat(name, tex, rough=0.85, metal=0.0):
    m = bpy.data.materials.get(name)
    if m: return m
    m = bpy.data.materials.new(name); m.use_nodes = True
    nt = m.node_tree; b = nt.nodes["Principled BSDF"]
    if tex and os.path.exists(f"{TEX}/{tex}.png"):
        img = nt.nodes.new("ShaderNodeTexImage"); img.image = bpy.data.images.load(f"{TEX}/{tex}.png")
        nt.links.new(img.outputs["Color"], b.inputs["Base Color"])
    b.inputs["Roughness"].default_value = rough
    b.inputs["Metallic"].default_value = metal
    return m

def box(bm, x0, x1, y0, y1, z0, z1, bevel=0.0):
    """Scatola in coordinate mappa (y = quota)."""
    if x1-x0 < 1e-4 or y1-y0 < 1e-4 or z1-z0 < 1e-4: return
    tmp = bmesh.new(); bmesh.ops.create_cube(tmp, size=1.0)
    if bevel > 0:
        bmesh.ops.bevel(tmp, geom=list(tmp.edges)+list(tmp.verts), offset=bevel,
                        segments=2, affect='EDGES', clamp_overlap=True)
    bmesh.ops.scale(tmp, vec=(x1-x0, z1-z0, y1-y0), verts=tmp.verts)      # blender: y=depth, z=quota
    bmesh.ops.translate(tmp, vec=((x0+x1)/2, -(z0+z1)/2, (y0+y1)/2), verts=tmp.verts)
    me = bpy.data.meshes.new("t"); tmp.to_mesh(me); tmp.free()
    bm.from_mesh(me); bpy.data.meshes.remove(me)

def uv_world(me, scale=6.0, off=(0.0, 0.0)):
    uv = me.uv_layers.new(name="UVMap")
    for poly in me.polygons:
        n = poly.normal
        ax = max(range(3), key=lambda i: abs(n[i]))
        for li in poly.loop_indices:
            co = me.vertices[me.loops[li].vertex_index].co
            u, v = (co.y, co.z) if ax == 0 else ((co.x, co.z) if ax == 1 else (co.x, co.y))
            uv.data[li].uv = (u/scale + off[0], v/scale + off[1])

def make(name, bm, material, off=(0.0, 0.0)):
    me = bpy.data.meshes.new(name); bm.to_mesh(me); bm.free()
    uv_world(me, off=off)
    ob = bpy.data.objects.new(name, me)
    bpy.context.collection.objects.link(ob)
    ob.data.materials.append(material)
    return ob

# ---------------------------------------------------------------- parete
def wall(bm, along, fixed, u0, u1, h, ops, sign, thick=None):
    """Parete con aperture, costruita a GRIGLIA: si dividono le quote e le ascisse sugli
    spigoli delle aperture, poi si emette una scatola per ogni cella che non è dentro un
    vano. Così il muro sopra e sotto ogni finestra esiste sempre — niente più colonne vuote."""
    T = thick if thick else WALL_T
    zt0, zt1 = fixed - T * 0.5 * sign, fixed + T * 0.5 * sign
    zlo, zhi = min(zt0, zt1), max(zt0, zt1)

    def inside(u, v):
        for (a, b, c, d) in ops:
            if a < u < b and c < v < d:
                return True
        return False

    def emit(a, b, c, d):
        if b - a < 1e-4 or d - c < 1e-4:
            return
        if inside((a + b) * 0.5, (c + d) * 0.5):
            return
        if along == 'x':
            box(bm, a, b, c, d, zlo, zhi)
        else:
            box(bm, zlo, zhi, c, d, a, b)

    xs = {u0, u1}
    ys = {0.0, h}
    for (a, b, c, d) in ops:
        a2, b2 = max(u0, a), min(u1, b)
        if b2 > a2:
            xs.add(a2); xs.add(b2)
        if 0 < c < h: ys.add(c)
        if 0 < d < h: ys.add(d)
    xs = sorted(xs); ys = sorted(ys)
    for i in range(len(xs) - 1):
        for j in range(len(ys) - 1):
            emit(xs[i], xs[i + 1], ys[j], ys[j + 1])

    # vani: spalle, davanzale, architrave
    for (a, b, c, d) in ops:
        inner = zhi - REVEAL * sign if sign > 0 else zlo - REVEAL * sign
        lo_r, hi_r = min(zhi, inner), max(zhi, inner)
        for uu in ((a, a + 0.03), (b - 0.03, b)):
            if along == 'x':
                box(bm, uu[0], uu[1], c, d, lo_r, hi_r)
            else:
                box(bm, lo_r, hi_r, c, d, uu[0], uu[1])
        out = sign * SILL_OUT
        if along == 'x':
            box(bm, a - 0.14, b + 0.14, c - 0.10, c, min(zlo, zlo + out), max(zlo, zlo + out))
            box(bm, a - 0.06, b + 0.06, d, d + 0.06, zlo, zhi)
        else:
            box(bm, min(zlo, zlo + out), max(zlo, zlo + out), c - 0.10, c, a - 0.14, b + 0.14)
            box(bm, zlo, zhi, d, d + 0.06, a - 0.06, b + 0.06)

def window_fill(bm_glass, bm_wood, bm_stone, along, fixed, a, b, c, d, sign):
    """Vetro arretrato, telaio e due persiane: senza, un'apertura è un buco nero."""
    zc = fixed + sign * (WALL_T * 0.5 - 0.05)      # vetro quasi a filo: si vede
    quarter = (b - a) / 4
    # cornice in pietra attorno al vano: 12 cm, sporge 3 cm
    f0, f1 = (fixed, fixed + sign * 0.03) if sign > 0 else (fixed + sign * 0.03, fixed)
    lo, hi = min(f0, f1), max(f0, f1)
    if along == 'x':
        box(bm_stone, a - 0.12, b + 0.12, d, d + 0.12, lo, hi)          # architrave
        box(bm_stone, a - 0.12, a, c - 0.06, d, lo, hi)                  # stipite sx
        box(bm_stone, b, b + 0.12, c - 0.06, d, lo, hi)                  # stipite dx
    else:
        box(bm_stone, lo, hi, d, d + 0.12, a - 0.12, b + 0.12)
        box(bm_stone, lo, hi, c - 0.06, d, a - 0.12, a)
        box(bm_stone, lo, hi, c - 0.06, d, b, b + 0.12)
    if along == 'x':
        box(bm_glass, a + 0.02, b - 0.02, c + 0.02, d - 0.02, zc - 0.02, zc + 0.02)
        box(bm_stone, a, a + 0.07, c, d, zc, zc + 0.06)                # stipite chiaro
        box(bm_wood, b - 0.06, b, c, d, zc, zc + 0.05)
        box(bm_wood, a, b, d - 0.07, d, zc, zc + 0.05)                 # traversa alta
        box(bm_wood, a, b, c, c + 0.06, zc, zc + 0.05)                 # traversa bassa
        # persiane aperte contro il muro
        box(bm_wood, a - 0.02 - quarter, a - 0.02, c, d, fixed + sign * 0.02, fixed + sign * 0.08)
        box(bm_wood, b + 0.02, b + 0.02 + quarter, c, d, fixed + sign * 0.02, fixed + sign * 0.08)
    else:
        box(bm_glass, zc - 0.02, zc + 0.02, c + 0.02, d - 0.02, a + 0.02, b - 0.02)
        box(bm_wood, zc, zc + 0.05, c, d, a, a + 0.06)
        box(bm_wood, zc, zc + 0.05, c, d, b - 0.06, b)
        box(bm_wood, zc, zc + 0.05, d - 0.07, d, a, b)
        box(bm_wood, zc, zc + 0.05, c, c + 0.06, a, b)
        box(bm_wood, fixed + sign * 0.02, fixed + sign * 0.08, c, d, a - 0.02 - quarter, a - 0.02)
        box(bm_wood, fixed + sign * 0.02, fixed + sign * 0.08, c, d, b + 0.02, b + 0.02 + quarter)

def door_fill(bm_wood, bm_glass, along, fixed, a, b, c, d, sign):
    """Battente in legno, stipiti e gradino: l'ingresso della casa."""
    zc = fixed + sign * (WALL_T * 0.5 - REVEAL)
    if along == 'x':
        box(bm_wood, a + 0.04, b - 0.04, c + 0.02, d - 0.05, zc - 0.03, zc + 0.03)
        box(bm_wood, a, a + 0.08, c, d, zc, zc + 0.06)
        box(bm_wood, b - 0.08, b, c, d, zc, zc + 0.06)
        box(bm_wood, a - 0.10, b + 0.10, c - 0.16, c, fixed - 0.25 * sign, fixed + 0.10 * sign)
    else:
        box(bm_wood, zc - 0.03, zc + 0.03, c + 0.02, d - 0.05, a + 0.04, b - 0.04)
        box(bm_wood, zc, zc + 0.06, c, d, a, a + 0.08)
        box(bm_wood, zc, zc + 0.06, c, d, b - 0.08, b)
        box(bm_wood, fixed - 0.25 * sign, fixed + 0.10 * sign, c - 0.16, c, a - 0.10, b + 0.10)

def balcony(bm_stone, bm_wood, along, fixed, a, b, floor_y, sign):
    """Balcone: soletta in pietra, mensole, ringhiera a barre."""
    out = sign * 0.55
    depth = 0.55
    if along == 'x':
        box(bm_stone, a - 0.10, b + 0.10, floor_y, floor_y + 0.12, fixed, fixed + out)
        for x in (a + 0.10, (a + b) / 2, b - 0.10):
            box(bm_stone, x - 0.05, x + 0.05, floor_y - 0.28, floor_y, fixed, fixed + out * 0.7)
        # ringhiera
        box(bm_wood, a - 0.10, b + 0.10, floor_y + 0.92, floor_y + 0.99, fixed + out - 0.07, fixed + out + 0.01)
        n = max(4, int((b - a) / 0.14))
        for i in range(n):
            x = a + (i + 0.5) * (b - a) / n
            box(bm_wood, x - 0.013, x + 0.013, floor_y + 0.12, floor_y + 0.92, fixed + out - 0.06, fixed + out)
    else:
        box(bm_stone, fixed, fixed + out, floor_y, floor_y + 0.12, a - 0.10, b + 0.10)
        for z in (a + 0.10, (a + b) / 2, b - 0.10):
            box(bm_stone, fixed, fixed + out * 0.7, floor_y - 0.28, floor_y, z - 0.05, z + 0.05)
        box(bm_wood, fixed + out - 0.07, fixed + out + 0.01, floor_y + 0.92, floor_y + 0.99, a - 0.10, b + 0.10)
        n = max(4, int((b - a) / 0.14))
        for i in range(n):
            z = a + (i + 0.5) * (b - a) / n
            box(bm_wood, fixed + out - 0.06, fixed + out, floor_y + 0.12, floor_y + 0.92, z - 0.013, z + 0.013)

def is_party(x0, z0, sx, sz, face, me):
    """Vero solo se un ALTRO edificio copre almeno metà di quel lato (muro in aderenza)."""
    along, fixed, u0, u1, sign = face
    probe = fixed + sign * 0.35
    need = 0.5 * abs(u1 - u0)
    covered = 0.0
    for b in MAP["boxes"]:
        if b is me:                      # la casa che stiamo costruendo non è un vicino
            continue
        if KINDS[b[6]] not in ("plaster", "plaster_ochre", "stone", "concrete") or b[4] < 2.5:
            continue
        bx0, bx1 = b[0], b[0] + b[3]
        bz0, bz1 = b[2], b[2] + b[5]
        # stessa casa: si salta
        if abs(bx0 - x0) < 0.01 and abs(bz0 - z0) < 0.01 and abs(b[4] - (u1 - u0)) < 50:
            pass
        if along == 'x':
            if not (bz0 - 0.35 <= probe <= bz1 + 0.35):
                continue
            lo, hi = max(u0, bx0), min(u1, bx1)
        else:
            if not (bx0 - 0.35 <= probe <= bx1 + 0.35):
                continue
            lo, hi = max(u0, bz0), min(u1, bz1)
        if hi > lo:
            covered += (hi - lo)
    return covered > need

def build(pick):
    b = MAP["boxes"][pick]
    x0, y0, z0, sx, sy, sz = b[0], b[1], b[2], b[3], b[4], b[5]
    H = y0 + sy
    print("lotto", pick, "x", x0, "z", z0, "w", sx, "d", sz, "h", sy, "piani", max(1,int((sy-1.2)//3.1)), flush=True)

    walls, stone, roof, wood = bmesh.new(), bmesh.new(), bmesh.new(), bmesh.new()
    blind = bmesh.new()
    glass = bmesh.new()
    zone = zone_of(x0 + sx/2, z0 + sz/2)
    seed = int(x0) * 73 + int(z0) * 131
    wall_color = choose(ZONES[zone]["c"], seed)
    # 4 texture dipinte: due edifici vicini non hanno mai la stessa superficie
    # un edificio su tre ha l'intonaco rotto, gli altri pulito: la città non è uniforme
    # famiglia di texture per zona: la Piazza ha intonaco veneziano liscio,
    # i vicoli intonaco grezzo, il Forte pietra a vista. Dentro la famiglia,
    # il degrado compare su un edificio su tre.
    zn = zone_of(x0 + sx / 2, z0 + sz / 2)
    fam = {"piazza": "real_plaster_a", "mercato": "real_plaster_a",
           "vicoli": "real_plaster_a", "forte": "real_plaster_c",
           "porto": "real_plaster_b", "molo": "real_plaster_b"}.get(zn, "real_plaster_a")
    if abs(seed) % 3 == 0:
        wall_tex = "real_damaged"
    else:
        wall_tex = fam
    shutter_color = choose(ZONES[zone]["shutter"], seed + 7)

    # arrotonda al piano più vicino: la fila di finestre arriva al filo della gronda
    floors = max(1, int(round((sy - 1.2) / 3.1)))
    # schema diverso per ogni casa: 2-4 finestre per piano, a volte una sola larga
    rnd = (abs(seed) % 1000) / 1000.0
    def openings(length):
        base_n = max(2, int(length // 2.8))
        n = base_n if rnd < 0.6 else max(1, base_n - 1 + (1 if rnd > 0.85 else 0))
        wide = (rnd > 0.72)          # piano terra con finestra larga
        ops = []
        w = 0.55
        for f in range(floors):
            base = 1.15 + 3.1 * f
            for i in range(n):
                cu = (i + 0.5) * length / n
                hw = 0.575 if not (wide and f == 0) else 0.80
                ops.append((cu - hw, cu + hw, base, base + 1.55))
        return ops, n

    # lato principale = il più lungo: lì va la porta
    faces = [
        ('x', z0, x0, x0 + sx, -1),
        ('x', z0 + sz, x0, x0 + sx, +1),
        ('z', x0, z0, z0 + sz, -1),
        ('z', x0 + sx, z0, z0 + sz, +1),
    ]
    main = max(faces, key=lambda f: f[3] - f[2])

    for (along, fixed, u0, u1, sign) in (
        ('x', z0, x0, x0 + sx, -1),
        ('x', z0 + sz, x0, x0 + sx, +1),
        ('z', x0, z0, z0 + sz, -1),
        ('z', x0 + sx, z0, z0 + sz, +1),
    ):
        L = u1 - u0
        # le aperture sono calcolate da 0: vanno portate nella posizione reale del muro
        ops = [(a + u0, b + u0, c, d) for (a, b, c, d) in openings(L)[0]]
        if (along, fixed, u0, u1, sign) == main:
            # porta d'ingresso: 1,20 × 2,35 al centro del lato principale
            dc = (u0 + u1) / 2
            ops.append((dc - 0.60, dc + 0.60, 0.0, 2.35))
        party = is_party(x0, z0, sx, sz, (along, fixed, u0, u1, sign), b)
        if party and (along, fixed, u0, u1, sign) == main:
            party = False        # il lato principale non è mai cieco
        if party:
            ops = []                      # muro in aderenza: cieco, niente finestre
        target = blind if party else walls
        wall(target, along, fixed, u0, u1, H, ops, sign, thick=(0.62 if party else None))
        for op in ops:
            if op[2] < 0.01:
                door_fill(wood, glass, along, fixed, op[0], op[1], op[2], op[3], sign)   # porta
            else:
                window_fill(glass, wood, stone, along, fixed, op[0], op[1], op[2], op[3], sign)
                # un balcone ogni due finestre dei piani superiori
                if op[2] > 3.0 and (int(op[0] * 10) % 2 == 0):
                    balcony(stone, wood, along, fixed, op[0] - 0.15, op[1] + 0.15, op[2] - 0.45, sign)

    # zoccolo
    box(stone, x0 - 0.05, x0 + sx + 0.05, y0, y0 + ZOCCOLO, z0 - 0.05, z0 + sz + 0.05, 0.012)
    # cornice a due risalti
    box(stone, x0 - CORNICE, x0 + sx + CORNICE, H - 0.32, H - 0.08, z0 - CORNICE, z0 + sz + CORNICE, 0.015)
    box(stone, x0 - CORNICE*0.6, x0 + sx + CORNICE*0.6, H - 0.08, H + 0.06, z0 - CORNICE*0.6, z0 + sz + CORNICE*0.6, 0.015)

    # tetto a due falde lungo X: due quade precise + colmo + fascioni
    over, rise = 0.50, 1.35
    rx0, rx1 = x0 - over, x0 + sx + over
    rz0, rz1 = z0 - over, z0 + sz + over
    zc = (rz0 + rz1) / 2
    top = H + rise
    eave = H + 0.02

    def quad(bm, p0, p1, p2, p3):
        # coordinate mappa (x, quota, profondita) -> Blender (x, -profondita, quota)
        conv = lambda p: (p[0], -p[2], p[1])
        tmp = bmesh.new()
        vs = [tmp.verts.new(conv(p)) for p in (p0, p1, p2, p3)]
        tmp.faces.new(vs)
        me = bpy.data.meshes.new("q"); tmp.to_mesh(me); tmp.free()
        bm.from_mesh(me); bpy.data.meshes.remove(me)

    quad(roof, (rx0, eave, rz0), (rx1, eave, rz0), (rx1, top, zc), (rx0, top, zc))
    quad(roof, (rx0, top, zc), (rx1, top, zc), (rx1, eave, rz1), (rx0, eave, rz1))
    # colmo
    box(roof, rx0, rx1, top - 0.02, top + 0.09, zc - 0.16, zc + 0.16, 0.01)
    # fascioni di gronda
    box(stone, rx0, rx1, eave - 0.18, eave + 0.02, rz0 - 0.04, rz0 + 0.10, 0.01)
    box(stone, rx0, rx1, eave - 0.18, eave + 0.02, rz1 - 0.10, rz1 + 0.04, 0.01)
    # comignolo
    box(stone, (x0 + x0 + sx)/2 - 0.28, (x0 + x0 + sx)/2 + 0.28, top, top + 1.15, zc - 0.28, zc + 0.28, 0.012)

    # offset UV diverso per edificio e per materiale: le texture non si ripetono in fila
    ox, oy = (abs(seed) % 97) / 97.0, (abs(seed * 7) % 89) / 89.0
    return [make("Pareti", walls, painted("intonaco", wall_tex, wall_color, compress=0.35), (ox, oy)),
            make("MuriCiechi", blind, painted("muro_cieco", wall_tex, (0.62, 0.58, 0.52), 0.94, compress=0.35), (oy, ox)),
            make("Pietra", stone, painted("pietra", "real_plaster_c", (0.80, 0.78, 0.74), compress=0.75), (ox * 0.5, oy * 0.5)),
            make("Coppi", roof, painted("coppi", "styl_roof_c", (0.62, 0.30, 0.18), 0.8), (oy, ox)),
            make("Legno", wood, painted("legno", "wood_planks_c", shutter_color, 0.8), (ox, 0.0)),
            make("Vetro", glass, painted("vetro", None, (0.055, 0.075, 0.095), 0.08, 0.55))]

if __name__ == "__main__":
    argv = sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else []
    pick = int(argv[0]) if argv else 379
    out = argv[1] if len(argv) > 1 else "/tmp/fz/edificio.blend"
    bpy.ops.wm.read_factory_settings(use_empty=True)
    objs = build(pick)
    b = MAP["boxes"][pick]
    cx, cz = b[0] + b[3]/2, b[2] + b[5]/2

    cd = bpy.data.cameras.new("c"); cam = bpy.data.objects.new("c", cd)
    bpy.context.collection.objects.link(cam); bpy.context.scene.camera = cam
    cam.location = (cx + b[3]*2.6, -(cz - b[5]*2.6), b[4]*1.05)
    tgt = bpy.data.objects.new("t", None); bpy.context.collection.objects.link(tgt)
    tgt.location = (cx, -cz, b[4]*0.5)
    c = cam.constraints.new('TRACK_TO'); c.target = tgt; c.track_axis='TRACK_NEGATIVE_Z'; c.up_axis='UP_Y'
    cd.lens = 42
    bpy.ops.object.light_add(type='SUN'); bpy.context.object.data.energy = 3.5
    bpy.context.object.rotation_euler = (math.radians(50), 0, math.radians(35))
    sc = bpy.context.scene
    sc.render.engine='CYCLES'; sc.cycles.samples=40; sc.cycles.device='CPU'
    sc.render.resolution_x=900; sc.render.resolution_y=700
    w = bpy.data.worlds.new("w"); sc.world=w; w.use_nodes=True
    w.node_tree.nodes["Background"].inputs[0].default_value=(0.45,0.55,0.70,1)
    sc.render.filepath="/tmp/fz/edificio_p0.png"
    bpy.ops.render.render(write_still=True)
    bpy.ops.wm.save_as_mainfile(filepath=out)
    print("P0 OK")
