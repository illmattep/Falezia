"""KIT DI CALANERA — ogni pezzo è un asset a sé, con le sue texture e i suoi materiali.

Niente più "pelle" unica: si costruiscono pezzi modulari e Unity li assembla secondo
il layout (map/layout.json). Ogni pezzo è esportato come GLB separato in
Assets/StreamingAssets/map/kit/.

Convenzioni: Blender Z-up; il pezzo ha l'origine sul terreno, al centro dell'impronta.
"""
import bpy, bmesh, os, math

TEX   = "/home/matteo/Progetti/Falezia/Assets/Resources/MapTex"
OUT   = "/home/matteo/Progetti/Falezia/Assets/StreamingAssets/map/kit"
WALL  = 0.32          # spessore muri
SILL  = 0.14          # sporgenza davanzale
PLINTH = 0.95         # zoccolo
FLOOR  = 3.10         # interpiano

_MATS = {}
_MANIFEST = {}
import json as _json
_mp = f"{TEX}/manifest.json"
if os.path.exists(_mp):
    _MANIFEST = _json.load(open(_mp))

def tile_of(tex, default=3.0):
    """Tassello in metri dal catalogo: ogni texture sa quanto deve coprire."""
    m = _MANIFEST.get(tex)
    t = m.get("tile_m", default) if m else default
    return t if t and t > 0.4 else default
def mat(name, tex=None, color=(0.8,0.8,0.8), rough=0.85, metal=0.0):
    key = (name, tex, tuple(round(c,3) for c in color), rough, metal)
    if key in _MATS: return _MATS[key]
    m = bpy.data.materials.new(name); m.use_nodes = True
    b = m.node_tree.nodes["Principled BSDF"]
    src = None
    for ext in (".jpg", ".png"):
        if tex and os.path.exists(f"{TEX}/{tex}{ext}"): src = f"{TEX}/{tex}{ext}"; break
    if src:
        img = m.node_tree.nodes.new("ShaderNodeTexImage"); img.image = bpy.data.images.load(src)
        m.node_tree.links.new(img.outputs["Color"], b.inputs["Base Color"])
        if color != (1,1,1):
            mix = m.node_tree.nodes.new("ShaderNodeMixRGB"); mix.blend_type='MULTIPLY'
            mix.inputs["Fac"].default_value = 1.0
            mix.inputs["Color2"].default_value = (*color,1)
            m.node_tree.links.new(img.outputs["Color"], mix.inputs["Color1"])
            m.node_tree.links.new(mix.outputs["Color"], b.inputs["Base Color"])
    else:
        b.inputs["Base Color"].default_value = (*color,1)
    b.inputs["Metallic"].default_value = metal
    # PBR completo: normal e rugosità dalla texture, se il set esiste
    if tex:
        nrm = f"{TEX}/{tex}_n.jpg"
        if os.path.exists(nrm):
            ni = m.node_tree.nodes.new("ShaderNodeTexImage"); ni.image = bpy.data.images.load(nrm)
            ni.image.colorspace_settings.name = 'Non-Color'
            nm = m.node_tree.nodes.new("ShaderNodeNormalMap")
            nm.inputs["Strength"].default_value = 1.0
            m.node_tree.links.new(ni.outputs["Color"], nm.inputs["Color"])
            m.node_tree.links.new(nm.outputs["Normal"], b.inputs["Normal"])
        rgh = f"{TEX}/{tex}_r.jpg"
        if os.path.exists(rgh):
            ri = m.node_tree.nodes.new("ShaderNodeTexImage"); ri.image = bpy.data.images.load(rgh)
            ri.image.colorspace_settings.name = 'Non-Color'
            m.node_tree.links.new(ri.outputs["Color"], b.inputs["Roughness"])
        else:
            b.inputs["Roughness"].default_value = rough
    else:
        b.inputs["Roughness"].default_value = rough
    _MATS[key] = m
    return m

def _sphere(bm, cx, cy, cz, r, subdiv=1):
    import bmesh as _bm
    t = _bm.new()
    _bm.ops.create_icosphere(t, subdivisions=subdiv, radius=r,
                             matrix=__import__("mathutils").Matrix.Translation((cx, cy, cz)))
    me = bpy.data.meshes.new("s"); t.to_mesh(me); t.free()
    bm.from_mesh(me); bpy.data.meshes.remove(me)

def _prism(bm, quad, thick):
    """Prisma a partire da un quadrilatero (4 punti) estruso di `thick` lungo la normale."""
    import bmesh as _bm
    from mathutils import Vector
    p = [Vector(q) for q in quad]
    n = (p[1]-p[0]).cross(p[3]-p[0]).normalized() * thick
    verts = [bm.verts.new(Vector(q)) for q in quad] + [bm.verts.new(Vector(q) + n) for q in quad]
    bm.faces.new((verts[0], verts[1], verts[2], verts[3]))
    bm.faces.new((verts[7], verts[6], verts[5], verts[4]))
    for i in range(4):
        j = (i+1) % 4
        bm.faces.new((verts[i], verts[j], verts[4+j], verts[4+i]))
    bm.verts.index_update()

def _box_rot(bm, cx, cz, w, d, z0, z1, ang):
    """Scatola ruotata attorno all'asse verticale, centrata in (cx, cz)."""
    import mathutils, math
    t = bmesh.new(); bmesh.ops.create_cube(t, size=1.0)
    bmesh.ops.scale(t, vec=(w, d, z1 - z0), verts=t.verts)
    bmesh.ops.rotate(t, cent=(0,0,0), matrix=mathutils.Matrix.Rotation(ang, 3, 'Z'), verts=t.verts)
    bmesh.ops.translate(t, vec=(cx, -cz, (z0 + z1) / 2), verts=t.verts)
    me = bpy.data.meshes.new("t"); t.to_mesh(me); t.free()
    bm.from_mesh(me); bpy.data.meshes.remove(me)

class Mesh:
    """Accumula scatole in bmesh separati per materiale."""
    def __init__(self): self.bm = {}
    def box(self, k, x0,x1, y0,y1, z0,z1):
        b = self.bm.setdefault(k, bmesh.new())
        t = bmesh.new(); bmesh.ops.create_cube(t, size=1.0)
        bmesh.ops.scale(t, vec=(x1-x0, z1-z0, y1-y0), verts=t.verts)
        bmesh.ops.translate(t, vec=((x0+x1)/2, -(z0+z1)/2, (y0+y1)/2), verts=t.verts)
        me = bpy.data.meshes.new("t"); t.to_mesh(me); t.free()
        b.from_mesh(me); bpy.data.meshes.remove(me)
    def finish(self, materials, name, scale_uv=3.0, scales=None):
        """scales: scala UV per singolo materiale (i coppi vogliono un tassello piccolo)."""
        objs = []
        scales = scales or {}
        for k, b in self.bm.items():
            sc = scales.get(k, scale_uv)
            me = bpy.data.meshes.new(f"{name}_{k}"); b.to_mesh(me); b.free()
            uv = me.uv_layers.new(name="UVMap")
            for poly in me.polygons:
                ax = max(range(3), key=lambda i: abs(poly.normal[i]))
                for li in poly.loop_indices:
                    co = me.vertices[me.loops[li].vertex_index].co
                    u, v = (co.y, co.z) if ax == 0 else ((co.x, co.z) if ax == 1 else (co.x, co.y))
                    uv.data[li].uv = (u/sc, v/sc)
            ob = bpy.data.objects.new(f"{name}_{k}", me)
            bpy.context.collection.objects.link(ob)
            ob.data.materials.append(materials[k])
            objs.append(ob)
        return objs

def clear():
    for o in list(bpy.data.objects): bpy.data.objects.remove(o, do_unlink=True)

def export(obj, filename):
    bpy.ops.object.select_all(action='DESELECT')
    for o in obj: o.select_set(True)
    bpy.context.view_layer.objects.active = obj[0]
    path = os.path.join(OUT, filename + ".glb")
    os.makedirs(OUT, exist_ok=True)
    bpy.ops.export_scene.gltf(filepath=path, export_format='GLB', use_selection=True,
                              export_apply=True, export_yup=True, export_image_format='AUTO')
    return path

# ---------- pezzi ----------
def wall_run(m, k, along, fixed, u0, u1, h, ops=(), t=WALL):
    """Parete a griglia: buco solo dove c'è l'apertura."""
    def inside(u, v):
        return any(a < u < b and c < v < d for (a,b,c,d) in ops)
    xs, ys = {u0,u1}, {0.0,h}
    for (a,b,c,d) in ops:
        a2,b2 = max(u0,a), min(u1,b)
        if b2 > a2: xs.add(a2); xs.add(b2)
        if 0 < c < h: ys.add(c)
        if 0 < d < h: ys.add(d)
    xs, ys = sorted(xs), sorted(ys)
    for i in range(len(xs)-1):
        for j in range(len(ys)-1):
            a,b,c,d = xs[i], xs[i+1], ys[j], ys[j+1]
            if b-a < 1e-4 or d-c < 1e-4: continue
            if inside((a+b)/2, (c+d)/2): continue
            if along == 'x': m.box(k, a, b, c, d, fixed-t/2, fixed+t/2)
            else:            m.box(k, fixed-t/2, fixed+t/2, c, d, a, b)

def window(m, along, fixed, u0, u1, v0, v1, sign, frame, glass, stone, wall=WALL):
    """Vano a filo della facciata ESTERNA: vetro incassato 6 cm, cornice in pietra,
    davanzale sporgente. Il piano del muro è `fixed`; l'esterno è fixed + sign*wall/2."""
    a, b, c, d = u0, u1, v0, v1
    face = fixed + sign * (wall * 0.5)
    zf = face - sign * 0.06          # vetro rientrato di 6 cm
    t = 0.08
    if along == 'x':
        m.box(glass, a+0.03, b-0.03, c+0.03, d-0.03, zf-0.01, zf+0.01)
        m.box(frame, a-t, a, c-t, d+t, face, face + sign*0.04)
        m.box(frame, b, b+t, c-t, d+t, face, face + sign*0.04)
        m.box(frame, a-t, b+t, d, d+t, face, face + sign*0.04)
        m.box(stone, a-0.17, b+0.17, c-0.11, c, face, face + sign*0.16)   # davanzale
        # persiane aperte, ai lati del vano
        qw = (b - a) * 0.46
        m.box("wood", a - qw - 0.02, a - 0.02, c, d, face, face + sign*0.05)
        m.box("wood", b + 0.02, b + qw + 0.02, c, d, face, face + sign*0.05)
        # spalletta interna del vano
        m.box(frame, a, a+0.03, c, d, zf, face)
        m.box(frame, b-0.03, b, c, d, zf, face)
    else:
        m.box(glass, zf-0.01, zf+0.01, c+0.03, d-0.03, a+0.03, b-0.03)
        m.box(frame, face, face + sign*0.04, c-t, d+t, a-t, a)
        m.box(frame, face, face + sign*0.04, c-t, d+t, b, b+t)
        m.box(frame, face, face + sign*0.04, d, d+t, a-t, b+t)
        m.box(stone, face, face + sign*0.16, c-0.11, c, a-0.17, b+0.17)
        qw = (b - a) * 0.46
        m.box("wood", face, face + sign*0.05, c, d, a - qw - 0.02, a - 0.02)
        m.box("wood", face, face + sign*0.05, c, d, b + 0.02, b + qw + 0.02)
        m.box(frame, zf, face, c, d, a, a+0.03)
        m.box(frame, zf, face, c, d, b-0.03, b)

def door(m, along, fixed, uc, height, sign, wood, stone, wall=WALL, w=1.28):
    a, b = uc - w/2, uc + w/2
    face = fixed + sign * (wall * 0.5)
    zf = face - sign * 0.07
    if along == 'x':
        m.box(wood, a+0.02, b-0.02, 0.0, height-0.02, zf-0.02, zf+0.02)
        m.box(stone, a-0.14, a, 0.0, height+0.14, face, face + sign*0.10)
        m.box(stone, b, b+0.14, 0.0, height+0.14, face, face + sign*0.10)
        m.box(stone, a-0.14, b+0.14, height, height+0.16, face, face + sign*0.10)
        m.box(stone, a-0.26, b+0.26, 0.0, 0.16, face - sign*0.05, face + sign*0.62)   # gradino
        # lampada sopra il portone
        m.box("iron_rail", a-0.02, a+0.08, height+0.55, height+0.62, face, face + sign*0.28)
        m.box("lamp_glass", a-0.02, a+0.08, height+0.28, height+0.55, face + sign*0.20, face + sign*0.34)
    else:
        m.box(wood, zf-0.02, zf+0.02, 0.0, height-0.02, a+0.02, b-0.02)
        m.box(stone, face, face + sign*0.10, 0.0, height+0.14, a-0.14, a)
        m.box(stone, face, face + sign*0.10, 0.0, height+0.14, b, b+0.14)
        m.box(stone, face, face + sign*0.10, height, height+0.16, a-0.14, b+0.14)
        m.box(stone, face - sign*0.05, face + sign*0.55, 0.0, 0.14, a-0.22, b+0.22)

def roof(m, w, d, h, roofmat, cornice, overhang=0.50, pitch=0.34):
    """Tetto a due falde: fascia, falde a gradoni dal colmo allo sporto, colmo."""
    yy = d * 0.5 + overhang
    xx = w * 0.5 + overhang
    rh = yy * pitch
    eave = h + 0.02
    m.box("cornice", -xx, xx, eave - 0.14, eave, -yy, yy)          # fascia/gronda
    # due falde inclinate (prismi), non gradoni
    for sg in (-1, 1):
        quad = [(-xx, -sg*yy, eave), (xx, -sg*yy, eave),
                (xx, 0.0, eave + rh), (-xx, 0.0, eave + rh)]
        b = m.bm.setdefault(roofmat, bmesh.new())
        _prism(b, quad, 0.16)
    m.box(roofmat, -xx, xx, eave + rh - 0.04, eave + rh + 0.12, -0.20, 0.20)  # colmo
    # comignolo
    m.box("stone", w*0.28, w*0.28+0.55, eave + rh*0.55, eave + rh*0.55 + 1.05, -d*0.18, -d*0.18+0.55)

def house(w, d, h, floors, tex, tint, door_side='-z', shop=False, varname="casa",
          shutter=(0.24,0.34,0.26), roof_tex="kit_coppi"):
    m = Mesh()
    mats = {"wall":  mat("k_wall",  tex, tint, 0.88),
            "blind": mat("k_blind", "kit_plaster_b", (0.72,0.70,0.66), 0.92),
            "stone": mat("k_stone", "kit_pietra", (0.86,0.84,0.80), 0.86),
            "wood":  mat("k_wood",  "legno_persiana_01", shutter, 0.85),
            "door":  mat("k_door",  "legno_porta_01", (1.0,1.0,1.0), 0.85),
            "glass": mat("k_glass", None, (0.055,0.075,0.095), 0.08, 0.55),
            "roof":  mat("k_roof",  "kit_coppi", (0.92,0.88,0.84), 0.82),
            "plinth":mat("k_plinth","wall_stone_02", (1.0,1.0,1.0), 0.92),
            "iron_rail":mat("k_rail", None, (0.12,0.12,0.14), 0.45, 0.7),
            "cornice": mat("k_cornice", "kit_plaster_c", (0.96,0.90,0.80), 0.88),
            "lamp_glass": mat("k_lampg", None, (1.0,0.88,0.62), 0.30, 0.0)}
    # zoccolo
    m.box("plinth", -w/2, w/2, 0.0, 0.35, -d/2, d/2)
    # quattro pareti
    n = max(1, int(w // 2.9))
    ops = []
    for f in range(floors):
        base = PLINTH + 0.75 + FLOOR*f
        for i in range(n):
            cu = -w/2 + (i+0.5)*w/n
            ops.append((cu-0.575, cu+0.575, base, base+1.55))
    nd = max(1, int(d // 3.2))
    side_ops = []
    for f in range(floors):
        base = PLINTH + 0.75 + FLOOR*f
        for i in range(nd):
            cu = -d/2 + (i+0.5)*d/nd
            side_ops.append((cu-0.50, cu+0.50, base, base+1.55))
    wall_run(m, "wall", 'x', -d/2, -w/2, w/2, h, ops)
    wall_run(m, "wall", 'x', d/2, -w/2, w/2, h, ops)
    wall_run(m, "wall", 'z', -w/2, -d/2, d/2, h, side_ops)
    wall_run(m, "wall", 'z', w/2, -d/2, d/2, h, side_ops)
    # cornice marcapiano e di gronda
    for f in range(1, floors):
        z = PLINTH + 0.75 + FLOOR*f - 0.55
        m.box("cornice", -w/2-0.04, w/2+0.04, z, z+0.09, -d/2-0.04, d/2+0.04)
    # aperture sulla facciata principale (-z) e sui lati corti
    face_z = -d/2 - WALL*0.5
    for (a,b,c,d) in ops:
        window(m, 'x', -d/2, a, b, c, d, -1, "stone", "glass", "stone")
        # balcone: sui piani alti, su una finestra sì e una no
        piano = int(round((c - PLINTH - 0.75) // FLOOR))
        if floors > 1 and piano >= 1 and (int((a + 100) * 10) % 3) != 0:
            m.box("stone", a - 0.35, b + 0.35, c - 0.16, c, face_z - 1.05, face_z + 0.02)
            # ringhiera
            m.box("iron_rail", a - 0.35, b + 0.35, c, c + 0.06, face_z - 1.02, face_z - 0.96)
            for k in range(6):
                xx = a - 0.30 + k * (b - a + 0.60) / 5
                m.box("iron_rail", xx, xx + 0.04, c, c + 0.95, face_z - 1.00, face_z - 0.96)
    for (a, b, c, d2) in side_ops:
        window(m, 'z', -w/2, a, b, c, d2, -1, "stone", "glass", "stone")
        window(m, 'z',  w/2, a, b, c, d2,  1, "stone", "glass", "stone")
    for (a, b, c, d2) in ops:
        window(m, 'x', d/2, a, b, c, d2, 1, "stone", "glass", "stone")
    door(m, 'x', -d/2, 0.0, 2.35, -1, "door", "stone")
    if shop:
        face = -d/2 - WALL*0.5
        # vetrina
        m.box("glass", -w/2+0.8, w/2-0.8, 0.95, 2.10, face-0.06, face-0.04)
        m.box("wood", -w/2+0.7, -w/2+0.8, 0.95, 2.10, face-0.08, face-0.02)
        m.box("wood",  w/2-0.8,  w/2-0.7, 0.95, 2.10, face-0.08, face-0.02)
        m.box("wood", -w/2+0.7, w/2-0.7, 2.10, 2.22, face-0.08, face-0.02)
        # tenda a strisce
        mats["tela"]  = mat("k_tela",  None, (0.88,0.74,0.46), 0.95)
        mats["banda"] = mat("k_banda", None, (0.58,0.20,0.16), 0.90)
        n = 10
        for i in range(n):
            x0 = -w/2 + 0.5 + i*(w-1.0)/n
            x1 = x0 + (w-1.0)/n
            key = "tela" if i % 2 == 0 else "banda"
            m.box(key, x0, x1, 2.62, 2.70, face-0.02, face-1.05)
        # insegna dipinta sopra la tenda
        m.box("banda", -1.0, 1.0, 3.00, 3.55, face-0.05, face-0.01)
    roof(m, w, d, h, "roof", "stone")
    objs = m.finish(mats, varname, scale_uv=3.4,
                    scales={"roof": tile_of(roof_tex, 1.2),
                            "stone": tile_of("wall_stone_01", 2.6),
                            "plinth": tile_of("wall_stone_02", 2.6),
                            "wood": tile_of("legno_persiana_01", 1.4),
                            "wall": tile_of(tex, 3.4),
                            "blind": tile_of(tex, 3.4),
                            "glass": 1.2, "cornice": 2.4})
    return objs

# ---------- pezzi di città ----------
def _mats():
    return {"wall":  mat("k_wall",  "wall_venez_02", (1.0,1.0,1.0), 0.92),
            "blind": mat("k_blind", "wall_grezzo_02", (1.0,1.0,1.0), 0.95),
            "stone": mat("k_stone", "wall_stone_01_dk", (1,1,1), 0.92),
            "wood":  mat("k_wood",  "legno_cassa_01", (1.0,1.0,1.0), 0.88),
            "glass": mat("k_glass", "vetro_01", (1.0,1.0,1.0), 0.08, 0.3),
            "roof":  mat("k_roof",  "roof_coppi_01", (1.0,1.0,1.0), 0.85),
            "plinth":mat("k_plinth","wall_stone_02", (1.0,1.0,1.0), 0.92),
            "pave":  mat("k_pave",  "sampietrini_02_dk", (1,1,1), 0.94),
            "iron":  mat("k_iron",  "ferro_battuto_01", (1.0,1.0,1.0), 0.55, 0.85),
            "iron_rail": mat("k_rail", None, (0.12,0.12,0.14), 0.45, 0.7),
            "cornice": mat("k_cornice", "kit_plaster_c", (0.96,0.90,0.80), 0.88),
            "terra": mat("k_terra", None, (0.44,0.26,0.18), 0.90)}

def block(w, d, h, tex="kit_plaster_a", tint=(0.98,0.94,0.88), varname="blocco"):
    """Volume semplice con zoccolo e cornice: base per muri e corpi di fabbrica."""
    m = Mesh(); M = _mats()
    M["wall"] = mat("k_wall", tex, tint, 0.88)
    m.box("plinth", -w/2-0.04, w/2+0.04, 0.0, 0.40, -d/2-0.04, d/2+0.04)
    m.box("wall", -w/2, w/2, 0.40, h, -d/2, d/2)
    m.box("cornice", -w/2-0.05, w/2+0.05, h-0.12, h, -d/2-0.05, d/2+0.05)
    return m.finish(M, varname, 3.2)

def stairs(w=2.4, run=3.2, rise=1.2, steps=6, varname="scala"):
    """Rampa di gradini in pietra, larga w, profonda run, che sale di rise."""
    m = Mesh(); M = _mats()
    sd = run / steps; sh = rise / steps
    for i in range(steps):
        z = (i+1)*sh
        m.box("stone", -w/2, w/2, 0.0, z, -run/2 + i*sd, -run/2 + (i+1)*sd)
    m.box("plinth", -w/2-0.05, w/2+0.05, 0.0, 0.10, -run/2, run/2)
    return m.finish(M, varname, 1.4)

def sidewalk(length=6.0, width=1.6, varname="marciapiede"):
    """Marciapiede con cordolo: lastrico in quota 0,15 e bordo in pietra."""
    m = Mesh(); M = _mats()
    m.box("pave", -length/2, length/2, 0.0, 0.15, -width/2, width/2)
    m.box("stone", -length/2, length/2, 0.0, 0.20, width/2-0.12, width/2)
    m.box("stone", -length/2, length/2, 0.0, 0.20, -width/2, -width/2+0.12)
    return m.finish(M, varname, 1.6)

def fort_wall(length=8.0, height=4.6, varname="muro_forte"):
    """Muro di cinta in pietra con merli e cammino di ronda."""
    m = Mesh(); M = _mats()
    m.box("stone", -length/2, length/2, 0.0, height, -0.55, 0.55)
    m.box("pave",  -length/2, length/2, height, height+0.18, -0.75, 0.75)   # cammino
    x = -length/2 + 0.4
    while x < length/2 - 0.5:
        m.box("stone", x, x+0.62, height+0.18, height+0.95, -0.55, 0.55)
        x += 1.15
    return m.finish(M, varname, 2.0)

def arch_bay(span=3.2, depth=3.0, height=3.4, varname="portico"):
    """Campata di porticato: due pilastri, arco a tutto sesto, architrave e tettoia."""
    m = Mesh(); M = _mats()
    r = (span - 0.62) / 2
    for x in (-span/2, span/2 - 0.62):
        m.box("stone", x, x+0.62, 0.0, height, -depth/2, depth/2)
        m.box("stone", x-0.06, x+0.68, height-0.12, height, -depth/2-0.05, depth/2+0.05)
    N = 10
    cx = -span/2 + 0.62 + r
    for i in range(N):
        a0 = math.pi*i/N; a1 = math.pi*(i+1)/N
        xa0 = cx - r*math.cos(a0); xa1 = cx - r*math.cos(a1)
        ya0 = height + r*math.sin(a0); ya1 = height + r*math.sin(a1)
        m.box("stone", min(xa0,xa1)-0.02, max(xa0,xa1)+0.02,
              min(ya0,ya1), max(ya0,ya1)+0.10, -depth/2, -depth/2+0.45)
    m.box("stone", -span/2, span/2, height+r+0.10, height+r+0.30, -depth/2, depth/2)
    m.box("roof",  -span/2-0.4, span/2+0.4, height+r+0.30, height+r+0.46, -depth/2-0.5, depth/2+0.2)
    return m.finish(M, varname, 2.4)

def bench(varname="panca"):
    m = Mesh(); M = _mats()
    m.box("wood", -0.9, 0.9, 0.42, 0.50, -0.24, 0.24)
    m.box("wood", -0.9, 0.9, 0.50, 0.95, -0.28, -0.20)
    m.box("iron", -0.86, -0.74, 0.0, 0.42, -0.20, 0.20)
    m.box("iron",  0.74,  0.86, 0.0, 0.42, -0.20, 0.20)
    return m.finish(M, varname, 0.8)

def lamp(varname="lampada"):
    """Lampione da strada: fusto in ghisa, braccio ricurvo e lanterna."""
    m = Mesh(); M = _mats()
    M["glass"] = mat("k_lamp", None, (1.0,0.88,0.62), 0.30, 0.0)
    m.box("iron", -0.14, 0.14, 0.0, 0.22, -0.14, 0.14)         # basamento
    m.box("iron", -0.07, 0.07, 0.22, 3.60, -0.07, 0.07)        # fusto
    m.box("iron", -0.24, 0.24, 3.60, 3.70, -0.06, 0.06)        # braccio
    m.box("iron", 0.16, 0.26, 3.44, 3.72, -0.06, 0.06)         # ricciolo
    m.box("glass", -0.19, 0.19, 3.05, 3.44, -0.19, 0.19)       # lanterna
    m.box("iron", -0.24, 0.24, 3.44, 3.52, -0.24, 0.24)        # cappello
    return m.finish(M, varname, 0.8)

def planter(varname="vaso"):
    m = Mesh(); M = _mats()
    m.box("terra", -0.30, 0.30, 0.0, 0.55, -0.30, 0.30)
    m.box("stone", -0.34, 0.34, 0.55, 0.62, -0.34, 0.34)
    # cespuglio: due masse tonde
    M["leaf"] = mat("k_leaf", None, (0.28,0.42,0.24), 0.9)
    b = m.bm.setdefault("leaf", bmesh.new())
    _sphere(b, 0.0, 0.0, 0.92, 0.42, 1)
    _sphere(b, 0.10, -0.06, 1.18, 0.28, 1)
    return m.finish(M, varname, 0.7)

def crate(varname="cassa"):
    m = Mesh(); M = _mats()
    m.box("wood", -0.45, 0.45, 0.0, 0.80, -0.45, 0.45)
    for z in (0.18, 0.58):
        m.box("iron", -0.47, 0.47, z, z+0.06, -0.47, 0.47)
    return m.finish(M, varname, 0.6)

def barrel(varname="barile"):
    m = Mesh(); M = _mats()
    m.box("wood", -0.34, 0.34, 0.0, 0.95, -0.34, 0.34)
    m.box("iron", -0.36, 0.36, 0.18, 0.24, -0.36, 0.36)
    m.box("iron", -0.36, 0.36, 0.68, 0.74, -0.36, 0.36)
    return m.finish(M, varname, 0.6)

def bollard(varname="bitta"):
    m = Mesh(); M = _mats()
    m.box("iron", -0.17, 0.17, 0.0, 0.55, -0.17, 0.17)
    m.box("iron", -0.24, 0.24, 0.55, 0.70, -0.24, 0.24)
    return m.finish(M, varname, 0.6)

def container(color=(0.16,0.30,0.48), varname="container"):
    m = Mesh(); M = _mats()
    M["box"] = mat("k_container", "kit_legno", color, 0.7)   # righe verticali come lamiera
    m.box("box", -1.2, 1.2, 0.0, 2.55, -3.0, 3.0)
    m.box("iron", -1.25, 1.25, 0.0, 0.10, -3.05, 3.05)
    m.box("iron", -1.25, 1.25, 2.45, 2.55, -3.05, 3.05)
    # nervature della lamiera
    z = -2.8
    while z < 2.9:
        m.box("box", -1.26, -1.20, 0.15, 2.45, z, z+0.10)
        m.box("box",  1.20,  1.26, 0.15, 2.45, z, z+0.10)
        z += 0.42
    # portelline
    m.box("iron", -1.22, 1.22, 0.10, 2.45, -3.08, -3.02)
    m.box("iron", -1.22, 1.22, 0.10, 2.45,  3.02,  3.08)
    return m.finish(M, varname, 2.0)

def crane(varname="gru"):
    m = Mesh(); M = _mats()
    M["steel"] = mat("k_steel", None, (0.42,0.30,0.22), 0.6, 0.4)
    for dx in (-1.5, 1.5):
        for dz in (-1.5, 1.5):
            m.box("steel", dx-0.12, dx+0.12, 0.0, 11.0, dz-0.12, dz+0.12)
    for hy in (3.6, 7.2, 10.6):
        m.box("steel", -1.62, 1.62, hy, hy+0.16, -1.62, -1.44)
        m.box("steel", -1.62, 1.62, hy, hy+0.16,  1.44,  1.62)
    m.box("steel", -0.28, 0.28, 11.0, 11.30, -8.5, 2.0)
    m.box("iron",  -0.35, 0.35, 10.3, 11.0, -8.1, -7.5)
    m.box("iron",  -0.04, 0.04, 3.2, 10.3, -7.85, -7.75)
    return m.finish(M, varname, 1.6)

def roof_only(w, d, h=None, varname="tetto"):
    m = Mesh(); M = _mats()
    roof(m, w, d, 0.0, "roof", "stone")
    return m.finish(M, varname, 3.0)

def plaza_floor(w=26.0, d=22.0, varname="piazza_lastrico"):
    """Lastricato della piazza: lastre di pietra, bordo in conci."""
    m = Mesh(); M = _mats()
    M["slab"] = mat("k_slab", "lastrico_piazza_02_dk", (1,1,1), 0.92)
    m.box("slab", -w/2, w/2, -0.06, 0.02, -d/2, d/2)
    m.box("stone", -w/2-0.35, w/2+0.35, -0.10, 0.06, -d/2-0.35, d/2+0.35)
    return m.finish(M, varname, 3.2, scales={"slab": 3.2, "stone": 1.8})

def ground(w=240.0, d=240.0, varname="piano"):
    """Piano piatto della città: una faccia con UV a tassello regolare."""
    m = Mesh(); M = _mats()
    m.box("pave", -w/2, w/2, -0.30, 0.0, -d/2, d/2)
    return m.finish(M, varname, 2.6)

PIECES = {
    "piano":      ground,
    "scala":      lambda: stairs(),
    "scala_larga":lambda: stairs(4.0, 4.5, 1.6, 8, "scala_larga"),
    "marciapiede":lambda: sidewalk(),
    "muro_forte": lambda: fort_wall(),
    "portico":    lambda: arch_bay(),
    "panca":      bench,
    "lampada":    lamp,
    "vaso":       planter,
    "cassa":      crate,
    "barile":     barrel,
    "bitta":      bollard,
    "container":  container,
    "gru":        crane,
}

def fountain(varname="fontana"):
    """Fontana ottagonale costruita con prismi espliciti: sponde, bordo, acqua, colonna."""
    m = Mesh(); M = _mats()
    M["water"] = mat("k_water", None, (0.16, 0.30, 0.34), 0.12, 0.10)
    import bmesh as _bm3, mathutils
    from math import cos, sin, pi
    r = 1.55
    def pt(ang, rad, z):
        # coordinate del kit: x, altezza y, profondità -z
        return (rad * cos(ang), -rad * sin(ang), z)
    st = m.bm.setdefault("stone", bmesh.new())
    for i in range(8):
        a0 = 2*pi*i/8 + pi/8
        a1 = 2*pi*(i+1)/8 + pi/8
        wall = [pt(a0, r, 0.0), pt(a1, r, 0.0), pt(a1, r, 0.52), pt(a0, r, 0.52)]
        _prism(st, wall, 0.34)                                   # sponda
        rim = [pt(a0, r+0.08, 0.52), pt(a1, r+0.08, 0.52),
               pt(a1, r+0.08, 0.62), pt(a0, r+0.08, 0.62)]
        _prism(st, rim, 0.52)                                    # bordo modanato
    # fondo e acqua
    wt = m.bm.setdefault("water", bmesh.new())
    t = _bm3.new()
    _bm3.ops.create_cone(t, cap_ends=True, segments=8, radius1=r-0.34, radius2=r-0.34, depth=0.04,
                         matrix=mathutils.Matrix.Translation((0, 0, 0.40)))
    me = bpy.data.meshes.new("w"); t.to_mesh(me); t.free()
    wt.from_mesh(me); bpy.data.meshes.remove(me)
    # colonna centrale e vasca alta
    st2 = m.bm.setdefault("stone", bmesh.new())
    t = _bm3.new()
    _bm3.ops.create_cone(t, cap_ends=True, segments=8, radius1=0.34, radius2=0.34, depth=1.05,
                         matrix=mathutils.Matrix.Translation((0, 0, 0.52)))
    me = bpy.data.meshes.new("c"); t.to_mesh(me); t.free()
    st2.from_mesh(me); bpy.data.meshes.remove(me)
    t = _bm3.new()
    _bm3.ops.create_cone(t, cap_ends=True, segments=8, radius1=0.52, radius2=0.52, depth=0.10,
                         matrix=mathutils.Matrix.Translation((0, 0, 1.10)))
    me = bpy.data.meshes.new("c2"); t.to_mesh(me); t.free()
    st2.from_mesh(me); bpy.data.meshes.remove(me)
    w2 = m.bm.setdefault("water", bmesh.new())
    t = _bm3.new()
    _bm3.ops.create_cone(t, cap_ends=True, segments=8, radius1=0.44, radius2=0.44, depth=0.03,
                         matrix=mathutils.Matrix.Translation((0, 0, 1.16)))
    me = bpy.data.meshes.new("w2"); t.to_mesh(me); t.free()
    w2.from_mesh(me); bpy.data.meshes.remove(me)
    return m.finish(M, varname, 1.4, scales={"stone": 1.1, "water": 1.0})

def tree(varname="albero"):
    """Albero stilizzato: tronco e tre masse di chioma."""
    m = Mesh(); M = _mats()
    M["bark"] = mat("k_bark", "kit_legno", (0.78,0.66,0.52), 0.9)
    M["leaf"] = mat("k_leaf2", None, (0.26,0.42,0.22), 0.9)
    m.box("bark", -0.20, 0.20, 0.0, 2.0, -0.20, 0.20)
    m.box("bark", -0.55, 0.55, 1.7, 2.0, -0.12, 0.12)
    _sphere(m.bm.setdefault("leaf", bmesh.new()), 0.0, 0.0, 2.9, 1.35, 2)
    _sphere(m.bm.setdefault("leaf", bmesh.new()), 0.55, 0.35, 2.35, 0.95, 1)
    _sphere(m.bm.setdefault("leaf", bmesh.new()), -0.60, -0.30, 2.5, 0.85, 1)
    return m.finish(M, varname, 1.6)

def stall(varname="bancarella"):
    """Bancarella del mercato: tavolo, pali e tenda a strisce."""
    m = Mesh(); M = _mats()
    M["tela"] = mat("k_tela", None, (0.86,0.72,0.44), 0.95)
    M["banda"] = mat("k_banda2", None, (0.62,0.22,0.18), 0.9)
    m.box("wood", -1.3, 1.3, 0.0, 0.85, -0.6, 0.6)
    for x in (-1.25, 1.25):
        for z in (-0.55, 0.55):
            m.box("wood", x-0.06, x+0.06, 0.0, 2.05, z-0.06, z+0.06)
    for i in range(5):
        x0 = -1.40 + i*0.56
        m.box("tela", x0, x0+0.28, 2.05, 2.13, -0.75, 0.75)
        m.box("banda", x0+0.28, x0+0.56, 2.05, 2.13, -0.75, 0.75)
    return m.finish(M, varname, 1.2)

def campanile(w=4.2, h=19.0, varname="campanile"):
    """Campanile: torre in pietra, cella campanaria con archi, cornice e cuspide."""
    m = Mesh(); M = _mats()
    m.box("plinth", -w/2-0.15, w/2+0.15, 0.0, 1.2, -w/2-0.15, w/2+0.15)
    for i in range(6):
        z0 = 1.2 + i*2.6
        m.box("stone", -w/2, w/2, z0, z0+2.6, -w/2, w/2)
        m.box("stone", -w/2-0.08, w/2+0.08, z0+2.45, z0+2.6, -w/2-0.08, w/2+0.08)
    # cella campanaria: quattro archi
    top = 1.2 + 6*2.6
    for s2 in (-1, 1):
        m.box("stone", -w/2, -w/2+0.55, top, top+3.2, s2*w/2-0.55 if s2>0 else -w/2, s2*w/2 if s2>0 else -w/2+0.55)
        m.box("stone",  w/2-0.55, w/2, top, top+3.2, s2*w/2-0.55 if s2>0 else -w/2, s2*w/2 if s2>0 else -w/2+0.55)
    for s2 in (-1, 1):
        for k in range(9):
            a0 = math.pi*k/9; a1 = math.pi*(k+1)/9
            r = w/2 - 0.55
            x0 = -r*math.cos(a0); x1 = -r*math.cos(a1)
            z0 = top + 1.4 + r*math.sin(a0); z1 = top + 1.4 + r*math.sin(a1)
            m.box("stone", min(x0,x1), max(x0,x1), min(z0,z1), max(z0,z1)+0.10,
                  s2*w/2-0.10 if s2>0 else -w/2, s2*w/2 if s2>0 else -w/2+0.10)
    m.box("stone", -w/2-0.25, w/2+0.25, top+3.2, top+3.55, -w/2-0.25, w/2+0.25)
    # cuspide a gradoni
    for i in range(5):
        d = w/2 * (1 - i/5.0) + 0.1
        m.box("roof", -d, d, top+3.55+i*0.75, top+3.55+(i+1)*0.75, -d, d)
    # croce
    m.box("iron_rail", -0.05, 0.05, top+7.3, top+8.0, -0.05, 0.05)
    return m.finish(M, varname, 2.4)

PIECES["campanile"] = campanile
PIECES["fontana"] = fountain
PIECES["albero"] = tree
PIECES["bancarella"] = stall

PIECES["piazza_lastrico"] = plaza_floor
