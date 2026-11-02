"""
FALEZIA · art/weapons/standard · 02_detail.py
Standard · Serafini Armi — modello completo (blockout + dettagli 1-7 della scheda).

Silhouette a linea orizzontale continua: carrello piatto, nessun elemento sporgente
tranne il cane. Leva caricatore solo sul lato sinistro, nessuna sicura manuale.

Assi: +Y canna (volata +170), +Z alto, +X destra del tiratore.
Origine: centro impugnatura all'altezza del grilletto. 1 unita' = 1 m.
Quote in MILLIMETRI.
"""
import bpy, bmesh, math, os, json
from mathutils import Vector, Matrix

ROOT = "/home/matteo/Progetti/Falezia/art/weapons/standard"
REVIEW = os.path.join(ROOT, "review")
os.makedirs(REVIEW, exist_ok=True)

def clear_scene():
    for ob in list(bpy.data.objects):
        bpy.data.objects.remove(ob, do_unlink=True)
    for coll in (bpy.data.meshes, bpy.data.materials, bpy.data.cameras,
                 bpy.data.lights, bpy.data.curves, bpy.data.armatures,
                 bpy.data.worlds):
        for b in list(coll):
            if b.users == 0:
                try:
                    coll.remove(b)
                except Exception:
                    pass

clear_scene()
sc = bpy.context.scene
sc.unit_settings.system = 'METRIC'
sc.unit_settings.scale_length = 1.0
sc.unit_settings.length_unit = 'MILLIMETERS'
sc.render.fps = 30

def mm(v):
    return v / 1000.0

# ------------------------------------------------------------------ materials
def hex_to_linear(h):
    h = h.lstrip('#')
    out = []
    for i in (0, 2, 4):
        c = int(h[i:i + 2], 16) / 255.0
        out.append(c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4)
    return (out[0], out[1], out[2], 1.0)

def new_mat(name, hexcol, rough, metallic, spec=0.30):
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    m.use_nodes = True
    nt = m.node_tree
    b = nt.nodes.get("Principled BSDF")
    b.inputs['Base Color'].default_value = hex_to_linear(hexcol)
    b.inputs['Roughness'].default_value = rough
    b.inputs['Metallic'].default_value = metallic
    if 'Specular IOR Level' in b.inputs:
        b.inputs['Specular IOR Level'].default_value = spec
    m.diffuse_color = hex_to_linear(hexcol)
    return m

# Fosfatazione: e' una conversione chimica, non metallo nudo -> metallic basso,
# altrimenti in un ambiente senza riflessioni il carrello diventa nero piatto.
MAT = {
    "steel_phosphate": new_mat("steel_phosphate", "3A3D40", 0.68, 0.03),
    "steel_plain":     new_mat("steel_plain",     "3A3D40", 0.68, 0.03),
    "polymer_black":   new_mat("polymer_black",   "1B1B1D", 0.78, 0.0),
    "metal_bare":      new_mat("metal_bare",      "8A8D8F", 0.65, 0.80, 0.5),
    "sight_white":     new_mat("sight_white",     "F2F2EE", 0.82, 0.0),
    "barrel":          new_mat("barrel",          "34373A", 0.55, 0.55),
    "marking_light":   new_mat("marking_light",   "B4B7B9", 0.55, 0.0),
    "trigger_steel":   new_mat("trigger_steel",   "4A4F54", 0.50, 0.25, 0.45),
}

# rigature a piramidi sull'impugnatura: reticolo a rombi, non linee (scheda punto 4)
def add_checkering(mat, scale=110.0, strength=0.45, dist=0.0005):
    nt = mat.node_tree
    b = nt.nodes.get("Principled BSDF")
    tc = nt.nodes.new('ShaderNodeTexCoord')
    mp = nt.nodes.new('ShaderNodeMapping')
    mp.inputs['Rotation'].default_value = (math.radians(45.0), 0.0, 0.0)
    w1 = nt.nodes.new('ShaderNodeTexWave')
    w1.wave_type = 'BANDS'; w1.bands_direction = 'Y'; w1.wave_profile = 'SIN'
    w1.inputs['Scale'].default_value = scale
    w1.inputs['Distortion'].default_value = 0.0
    w2 = nt.nodes.new('ShaderNodeTexWave')
    w2.wave_type = 'BANDS'; w2.bands_direction = 'Z'; w2.wave_profile = 'SIN'
    w2.inputs['Scale'].default_value = scale
    w2.inputs['Distortion'].default_value = 0.0
    mn = nt.nodes.new('ShaderNodeMath'); mn.operation = 'MINIMUM'
    # maschera: solo i fianchi del grip
    sep = nt.nodes.new('ShaderNodeSeparateXYZ')
    ax = nt.nodes.new('ShaderNodeMath'); ax.operation = 'ABSOLUTE'
    gt = nt.nodes.new('ShaderNodeMath'); gt.operation = 'GREATER_THAN'
    gt.inputs[1].default_value = 0.0130
    zl = nt.nodes.new('ShaderNodeMath'); zl.operation = 'GREATER_THAN'
    zl.inputs[1].default_value = -0.056
    zh = nt.nodes.new('ShaderNodeMath'); zh.operation = 'LESS_THAN'
    zh.inputs[1].default_value = 0.012
    m1 = nt.nodes.new('ShaderNodeMath'); m1.operation = 'MULTIPLY'
    m2 = nt.nodes.new('ShaderNodeMath'); m2.operation = 'MULTIPLY'
    m3 = nt.nodes.new('ShaderNodeMath'); m3.operation = 'MULTIPLY'
    bump = nt.nodes.new('ShaderNodeBump')
    bump.inputs['Strength'].default_value = strength
    bump.inputs['Distance'].default_value = dist
    nt.links.new(tc.outputs['Object'], mp.inputs['Vector'])
    nt.links.new(mp.outputs['Vector'], w1.inputs['Vector'])
    nt.links.new(mp.outputs['Vector'], w2.inputs['Vector'])
    nt.links.new(w1.outputs['Fac'], mn.inputs[0])
    nt.links.new(w2.outputs['Fac'], mn.inputs[1])
    nt.links.new(tc.outputs['Object'], sep.inputs['Vector'])
    nt.links.new(sep.outputs['X'], ax.inputs[0])
    nt.links.new(ax.outputs[0], gt.inputs[0])
    nt.links.new(sep.outputs['Z'], zl.inputs[0])
    nt.links.new(sep.outputs['Z'], zh.inputs[0])
    nt.links.new(gt.outputs[0], m1.inputs[0]); nt.links.new(zl.outputs[0], m1.inputs[1])
    nt.links.new(m1.outputs[0], m2.inputs[0]); nt.links.new(zh.outputs[0], m2.inputs[1])
    nt.links.new(mn.outputs[0], m3.inputs[0]); nt.links.new(m2.outputs[0], m3.inputs[1])
    nt.links.new(m3.outputs[0], bump.inputs['Height'])
    nt.links.new(bump.outputs['Normal'], b.inputs['Normal'])
    return bump

# scanalature di presa del carrello (scheda punto 5): 5 per lato, geometriche.
# Qui solo la maschera per il bake: la geometria le fa comunque, questo aggiunge
# il micro-solco interno alla scanalatura.
def add_slide_serration_grain(mat):
    nt = mat.node_tree
    b = nt.nodes.get("Principled BSDF")
    tc = nt.nodes.new('ShaderNodeTexCoord')
    sep = nt.nodes.new('ShaderNodeSeparateXYZ')
    w = nt.nodes.new('ShaderNodeTexWave')
    w.wave_type = 'BANDS'; w.bands_direction = 'Y'; w.wave_profile = 'SAW'
    w.inputs['Scale'].default_value = 900.0
    def gt(sock, v):
        n = nt.nodes.new('ShaderNodeMath'); n.operation = 'GREATER_THAN'
        n.inputs[1].default_value = v
        nt.links.new(sock, n.inputs[0]); return n
    def lt(sock, v):
        n = nt.nodes.new('ShaderNodeMath'); n.operation = 'LESS_THAN'
        n.inputs[1].default_value = v
        nt.links.new(sock, n.inputs[0]); return n
    def mul(a, bb):
        n = nt.nodes.new('ShaderNodeMath'); n.operation = 'MULTIPLY'
        nt.links.new(a, n.inputs[0]); nt.links.new(bb, n.inputs[1]); return n
    ax = nt.nodes.new('ShaderNodeMath'); ax.operation = 'ABSOLUTE'
    nt.links.new(sep.outputs['X'], ax.inputs[0])
    m = gt(ax.outputs[0], 0.0112)
    m = mul(m.outputs[0], gt(sep.outputs['Y'], -0.0160).outputs[0])
    m = mul(m.outputs[0], lt(sep.outputs['Y'], 0.0090).outputs[0])
    m = mul(m.outputs[0], gt(sep.outputs['Z'], 0.0368).outputs[0])
    m = mul(m.outputs[0], lt(sep.outputs['Z'], 0.0575).outputs[0])
    m = mul(m.outputs[0], w.outputs['Fac'])
    bump = nt.nodes.new('ShaderNodeBump')
    bump.name = "serration_bump"
    bump.inputs['Strength'].default_value = 0.25
    bump.inputs['Distance'].default_value = 0.0004
    nt.links.new(tc.outputs['Object'], sep.inputs['Vector'])
    nt.links.new(tc.outputs['Object'], w.inputs['Vector'])
    nt.links.new(m.outputs[0], bump.inputs['Height'])
    nt.links.new(bump.outputs['Normal'], b.inputs['Normal'])
    return bump

grip_mat = MAT["polymer_black"].copy()
grip_mat.name = "polymer_grip"
add_checkering(grip_mat)
MAT["polymer_grip"] = grip_mat
add_slide_serration_grain(MAT["steel_phosphate"])

# ------------------------------------------------------------------ geometry core
def apply_mods(ob):
    if not ob.modifiers:
        return
    bpy.context.view_layer.update()
    dg = bpy.context.evaluated_depsgraph_get()
    me = bpy.data.meshes.new_from_object(ob.evaluated_get(dg))
    old = ob.data
    ob.modifiers.clear()
    ob.data = me
    bpy.data.meshes.remove(old)

def finish(bm, name, mat, bevel=0.0, segments=2, smooth=False):
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    ob = bpy.data.objects.new(name, me)
    bpy.context.scene.collection.objects.link(ob)
    me.materials.append(MAT[mat] if isinstance(mat, str) else mat)
    if bevel > 0:
        md = ob.modifiers.new("bevel", 'BEVEL')
        md.width = mm(bevel)
        md.segments = segments
        md.limit_method = 'ANGLE'
        md.angle_limit = math.radians(35)
        apply_mods(ob)
    if smooth:
        for p in ob.data.polygons:
            p.use_smooth = True
    return ob

def box(name, x0, x1, y0, y1, z0, z1, mat, bevel=0.0, segments=2):
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1.0)
    bmesh.ops.scale(bm, vec=(mm(x1 - x0), mm(y1 - y0), mm(z1 - z0)), verts=bm.verts)
    bmesh.ops.translate(bm, vec=(mm((x0 + x1) / 2), mm((y0 + y1) / 2), mm((z0 + z1) / 2)), verts=bm.verts)
    return finish(bm, name, mat, bevel, segments)

def prism(name, pts_yz, x0, x1, mat, bevel=0.0, smooth=False):
    bm = bmesh.new()
    vs = [bm.verts.new((mm(x0), mm(y), mm(z))) for (y, z) in pts_yz]
    f = bm.faces.new(vs)
    r = bmesh.ops.extrude_face_region(bm, geom=[f])
    ext = [v for v in r['geom'] if isinstance(v, bmesh.types.BMVert)]
    bmesh.ops.translate(bm, verts=ext, vec=(mm(x1 - x0), 0, 0))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    return finish(bm, name, mat, bevel, 2, smooth)

def prism_y(name, pts_xz, y0, y1, mat, bevel=0.0, smooth=False):
    """Profilo nel piano XZ estruso lungo Y (mire, cane visto di lato)."""
    bm = bmesh.new()
    vs = [bm.verts.new((mm(x), mm(y0), mm(z))) for (x, z) in pts_xz]
    f = bm.faces.new(vs)
    r = bmesh.ops.extrude_face_region(bm, geom=[f])
    ext = [v for v in r['geom'] if isinstance(v, bmesh.types.BMVert)]
    bmesh.ops.translate(bm, verts=ext, vec=(0, mm(y1 - y0), 0))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    return finish(bm, name, mat, bevel, 2, smooth)

def cylinder(name, r, y0, y1, x, z, mat, segs=20, axis='Y', bevel=0.0):
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=True, cap_tris=False, segments=segs,
                          radius1=mm(r), radius2=mm(r), depth=mm(y1 - y0))
    if axis == 'Y':
        R = Matrix.Rotation(math.radians(90), 4, 'X')
        bmesh.ops.transform(bm, matrix=R, verts=bm.verts)
    else:
        R = Matrix.Rotation(math.radians(90), 4, 'Y')
        bmesh.ops.transform(bm, matrix=R, verts=bm.verts)
    bmesh.ops.translate(bm, vec=(mm(x), mm((y0 + y1) / 2), mm(z)), verts=bm.verts)
    return finish(bm, name, mat, bevel)

def rounded(points, radii, nc=3):
    """Contorno chiuso con angoli arrotondati (Bezier quadratica sui vertici)."""
    out = []
    n = len(points)
    for i in range(n):
        p = Vector(points[i]); r = radii[i]
        if r <= 0:
            out.append((p.x, p.y)); continue
        a = Vector(points[(i - 1) % n]); b = Vector(points[(i + 1) % n])
        da = (a - p); db = (b - p)
        la = da.length; lb = db.length
        da.normalize(); db.normalize()
        r = min(r, la * 0.45, lb * 0.45)
        p0 = p + da * r
        p2 = p + db * r
        for k in range(nc + 1):
            t = k / nc
            q = p0 * (1 - t) ** 2 + p * 2 * (1 - t) * t + p2 * t ** 2
            out.append((q.x, q.y))
    return out

def loft_xz(name, prof, sections, mat, smooth=True):
    """Loft lungo Y di un contorno XZ. sections: (y, scala_x, scala_z, dz)."""
    cx = sum(p[0] for p in prof) / len(prof)
    cz = sum(p[1] for p in prof) / len(prof)
    bm = bmesh.new()
    rings = []
    for (y, sx, sz, dz) in sections:
        ring = [bm.verts.new((mm(cx + (p[0] - cx) * sx),
                              mm(y),
                              mm(cz + (p[1] - cz) * sz + dz))) for p in prof]
        rings.append(ring)
    n = len(prof)
    for k in range(len(rings) - 1):
        A, B = rings[k], rings[k + 1]
        for i in range(n):
            j = (i + 1) % n
            bm.faces.new((A[i], A[j], B[j], B[i]))
    bm.faces.new(list(reversed(rings[0])))
    bm.faces.new(rings[-1])
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    ob = finish(bm, name, mat, 0.0, 2, smooth)
    return ob

def loft_yz(name, sections, mat, nseg=28, bevel=0.0, smooth=True):
    """Loft di sezioni orizzontali (z, y_front, y_back, half_w, esponente)."""
    bm = bmesh.new()
    rings = []
    for (z, yf, yb, hw, ex) in sections:
        cy = (yf + yb) / 2.0
        hd = max((yf - yb) / 2.0, 0.5)
        ring = []
        for i in range(nseg):
            a = 2 * math.pi * i / nseg
            ca, sa = math.cos(a), math.sin(a)
            x = hw * math.copysign(abs(ca) ** (2.0 / ex), ca)
            y = cy + hd * math.copysign(abs(sa) ** (2.0 / ex), sa)
            ring.append(bm.verts.new((mm(x), mm(y), mm(z))))
        rings.append(ring)
    for k in range(len(rings) - 1):
        A, B = rings[k], rings[k + 1]
        for i in range(nseg):
            j = (i + 1) % nseg
            bm.faces.new((A[i], A[j], B[j], B[i]))
    bm.faces.new(list(reversed(rings[0])))
    bm.faces.new(rings[-1])
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    return finish(bm, name, mat, bevel, 2, smooth)

def sweep_yz(name, path, rx, rz, mat, nseg=12, bevel=0.0, smooth=True, exp=2.0):
    """Tubo ellittico lungo un percorso nel piano YZ (guardia del grilletto)."""
    bm = bmesh.new()
    rings = []
    n = len(path)
    for i, (y, z) in enumerate(path):
        if i == 0:
            ty, tz = path[1][0] - y, path[1][1] - z
        elif i == n - 1:
            ty, tz = y - path[-2][0], z - path[-2][1]
        else:
            ty, tz = path[i + 1][0] - path[i - 1][0], path[i + 1][1] - path[i - 1][1]
        L = math.hypot(ty, tz) or 1.0
        ny, nz = -tz / L, ty / L
        rs = 0.88 if i in (0, n - 1) else 1.0
        ring = []
        for k in range(nseg):
            a = 2 * math.pi * k / nseg
            ca, sa = math.cos(a), math.sin(a)
            po = rz * rs * math.copysign(abs(sa) ** (2.0 / exp), sa)
            px = rx * rs * math.copysign(abs(ca) ** (2.0 / exp), ca)
            ring.append(bm.verts.new((mm(px), mm(y + ny * po), mm(z + nz * po))))
        rings.append(ring)
    for k in range(len(rings) - 1):
        A, B = rings[k], rings[k + 1]
        for i in range(nseg):
            j = (i + 1) % nseg
            bm.faces.new((A[i], A[j], B[j], B[i]))
    bm.faces.new(list(reversed(rings[0])))
    bm.faces.new(rings[-1])
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    return finish(bm, name, mat, bevel, 2, smooth)

def auto_smooth(ob, angle=38.0):
    """Facce smooth ma edge sharp oltre l'angolo: niente effetto gomma."""
    me = ob.data
    for p in me.polygons:
        p.use_smooth = True
    bm = bmesh.new()
    bm.from_mesh(me)
    lim = math.radians(angle)
    for e in bm.edges:
        if len(e.link_faces) == 2:
            try:
                if e.calc_face_angle(0.0) > lim:
                    e.smooth = False
            except ValueError:
                pass
        else:
            e.smooth = False
    bm.to_mesh(me)
    bm.free()

def cut(target, cutter):
    bpy.context.view_layer.update()
    md = target.modifiers.new("bool", 'BOOLEAN')
    md.operation = 'DIFFERENCE'
    md.object = cutter
    md.solver = 'MANIFOLD'
    apply_mods(target)
    me = cutter.data
    bpy.data.objects.remove(cutter, do_unlink=True)
    bpy.data.meshes.remove(me)
    if len(target.data.polygons) < 8:
        raise RuntimeError("boolean svuotata la mesh: " + target.name)

def join(name, objs):
    mats = []
    bm = bmesh.new()
    for o in objs:
        remap = []
        for slot in o.material_slots:
            m = slot.material or MAT["polymer_black"]
            if m not in mats:
                mats.append(m)
            remap.append(mats.index(m))
        if not remap:
            remap = [0]
            if not mats:
                mats.append(MAT["polymer_black"])
        for p in o.data.polygons:
            p.material_index = remap[p.material_index] if p.material_index < len(remap) else remap[0]
        bm.from_mesh(o.data)
    me = bpy.data.meshes.new(name + "_mesh")
    bm.to_mesh(me)
    bm.free()
    for m in mats:
        me.materials.append(m)
    ob = bpy.data.objects.new(name, me)
    bpy.context.scene.collection.objects.link(ob)
    for o in objs:
        d = o.data
        bpy.data.objects.remove(o, do_unlink=True)
        bpy.data.meshes.remove(d)
    return ob

def text_mesh(name, body, size, side, x_face, y0, z0, depth=0.35, mat="marking_light"):
    """Testo estruso sul lato sinistro (-1) o destro (+1) del pezzo."""
    cu = bpy.data.curves.new(name, type='FONT')
    cu.body = body
    cu.size = mm(size)
    cu.resolution_u = 3
    cu.extrude = mm(depth) / 2.0
    cu.align_x = 'LEFT'
    cu.align_y = 'CENTER'
    ob = bpy.data.objects.new(name, cu)
    bpy.context.scene.collection.objects.link(ob)
    bpy.context.view_layer.update()
    dg = bpy.context.evaluated_depsgraph_get()
    me = bpy.data.meshes.new_from_object(ob.evaluated_get(dg))
    bpy.data.objects.remove(ob, do_unlink=True)
    bpy.data.curves.remove(cu)
    me.transform(Matrix(((0, 0, side, 0), (side, 0, 0, 0), (0, 1, 0, 0), (0, 0, 0, 1))))
    me.transform(Matrix.Translation((mm(x_face) + side * mm(depth) * 0.25, mm(y0), mm(z0))))
    o = bpy.data.objects.new(name, me)
    bpy.context.scene.collection.objects.link(o)
    me.materials.append(MAT[mat])
    return o

# ================================================================== BODY
bp = []
# rail del fusto: coperto dal carrello, largo quanto il carrello meno 0.4
bp.append(box("frame_rail", -12.2, 12.2, -23, 138, 26.0, 33.5, "polymer_black", 0.8))
# corpo del fusto: profilo unico, niente gradini
frame_prof = rounded([
    (-23.0, 33.5), (138.0, 33.5), (141.5, 30.5), (141.5, 26.0),
    (143.0, 22.0), (143.0, 16.0), (140.0, 13.0), (62.0, 13.0),
    (55.0, 15.5), (42.0, 17.5), (30.0, 17.0), (22.0, 12.0), (4.0, 6.0), (-23.0, 8.0),
], [1.2, 2.2, 2.5, 1.0, 2.0, 1.5, 2.0, 1.0, 2.5, 2.5, 2.0, 2.0, 2.5, 2.5], nc=2)
bp.append(prism("frame_body", frame_prof, -12.4, 12.4, "polymer_black", 1.0))
# guardia del grilletto: tubo ellittico con front strap quasi verticale
guard_path = [(57, 24), (60, 17), (61, 7), (60, -2), (56, -9), (49, -13),
              (40, -14), (31, -11), (26, -4), (23, 4), (22, 11), (23, 16)]
bp.append(sweep_yz("trigger_guard", guard_path, 3.9, 2.8, "polymer_black", 14, 0.0, exp=2.2))
# impugnatura: loft con 17 anelli (nessuna banda visibile)
def catmull(p0, p1, p2, p3, t):
    return 0.5 * ((2 * p1) + (-p0 + p2) * t +
                  (2 * p0 - 5 * p1 + 4 * p2 - p3) * t * t +
                  (-p0 + 3 * p1 - 3 * p2 + p3) * t * t * t)

grip_keys = [
    (26.0, 21.0, -15.0, 13.2, 2.7),
    (18.0, 21.0, -18.5, 14.6, 2.55),
    (6.0, 19.5, -21.0, 15.9, 2.45),
    (-8.0, 17.0, -23.5, 16.2, 2.4),
    (-22.0, 13.5, -25.5, 16.0, 2.4),
    (-36.0, 10.0, -26.8, 15.4, 2.5),
    (-50.0, 6.0, -27.0, 14.6, 2.6),
    (-59.0, 3.0, -27.0, 14.0, 2.7),
    (-62.0, 2.0, -27.0, 13.4, 2.8),
]
grip_sections = []
for k in range(len(grip_keys) - 1):
    p0 = grip_keys[max(k - 1, 0)]
    p1 = grip_keys[k]; p2 = grip_keys[k + 1]
    p3 = grip_keys[min(k + 2, len(grip_keys) - 1)]
    steps = 1 if k == len(grip_keys) - 2 else 2
    for s in range(steps):
        t = s / steps
        grip_sections.append(tuple(catmull(p0[i], p1[i], p2[i], p3[i], t) for i in range(5)))
grip_sections.append(grip_keys[-1])
bp.append(loft_yz("grip", grip_sections, "polymer_grip", 22, 0.0))
# coda di castoro: si fonde col dorso dell'impugnatura
beaver_sections = [
    (26.0, -12.5, -25.0, 12.8, 3.0),
    (27.5, -12.8, -26.2, 12.7, 3.0),
    (30.0, -13.2, -26.4, 12.4, 3.0),
    (32.0, -13.6, -25.4, 12.2, 3.0),
    (33.5, -14.2, -23.0, 12.0, 3.0),
]
bp.append(loft_yz("beavertail", beaver_sections, "polymer_black", 18, 0.0))
# levamolle quadrato sopra l'impugnatura (scheda punto 3): linguetta sul dorso
bp.append(prism("leaf_spring", [(-21.6, 24.0), (-20.4, 32.5), (-19.0, 32.5),
                                (-20.2, 24.0)], -6.0, 6.0, "steel_phosphate", 0.5))
# vite della guancetta sinistra
bp.append(cylinder("grip_screw", 2.0, -0.30, 0.30, -15.85, -14.0, "steel_plain",
                   16, axis='X', bevel=0.25))
# sgancio caricatore, lato sinistro (unico comando dell'arma)
bp.append(box("mag_release", -16.8, -15.3, 8, 22, 0, 12, "polymer_black", 3.0, 3))
# perno di smontaggio sul fusto
bp.append(cylinder("takedown_pin", 2.5, -0.5, 1.2, -13.7, 21.0, "steel_plain",
                   16, axis='X', bevel=0.3))
body = join("body", bp)

# numero di serie inciso a laser sul fusto, davanti al grilletto
ser = text_mesh("serial", "S-9 41782", 2.6, -1, -13.4, 38, 25.0, 0.4, "marking_light")
body = join("body", [body, ser])

# ================================================================== SLIDE
# carrello: contorno arrotondato, rastremato al muso, linea orizzontale continua
car_prof = rounded([
    (-10.4, 60.5), (10.4, 60.5), (12.7, 57.2), (12.7, 38.0), (11.2, 33.5), (-11.2, 33.5),
    (-12.7, 38.0), (-12.7, 57.2),
], [1.2, 1.2, 2.0, 1.6, 1.6, 1.6, 1.6, 2.0], nc=2)
car_sections = [
    (-19.0, 0.965, 0.99, 0.2),
    (-16.0, 1.0, 1.0, 0.0),
    (20.0, 1.0, 1.0, 0.0),
    (150.0, 1.0, 1.0, 0.0),
    (163.0, 0.985, 0.99, -0.1),
    (167.0, 0.95, 0.965, -0.42),
    (168.0, 0.87, 0.90, -0.72),
]
sl = loft_xz("slide_body", car_prof, car_sections, "steel_phosphate", smooth=True)
# foro della volata: la canna ci passa dentro a filo (nessuna sporgenza)
cut(sl, cylinder("bore_hole", 6.70, 150, 175, 0, 46.5, "steel_phosphate", 28))
# feritoia di espulsione a destra, con bordo (non sfonda il tetto)
cut(sl, box("port", 5.8, 14.0, 100, 133, 44.0, 59.2, "steel_phosphate", 1.0, 1))
# 5 scanalature di presa per lato, smussate, dietro
for i in range(5):
    y = -13.5 + i * 4.6
    for side in (-1, 1):
        xa, xb = sorted((side * 13.2, side * 11.5))
        cut(sl, box("serr", xa, xb, y, y + 2.0, 37.5, 57.0, "steel_phosphate", 0.6, 1))
# canna: 112 mm (58 -> 170), corona a filo del muso
barrel = cylinder("barrel", 6.2, 58, 170, 0, 46.5, "barrel", 28)
cut(barrel, cylinder("bore", 4.5, 120, 172, 0, 46.5, "barrel", 24))
crown = cylinder("crown", 6.65, 164, 170, 0, 46.5, "barrel", 28)
barrel = join("barrel", [barrel, crown])
# estrattore
sl = join("slide", [sl, box("extractor", 11.9, 13.0, 88, 101, 47, 56, "steel_plain", 0.5)])
# mire fisse a tre punti (scheda punto 1)
fs_prof = [(-3.2, 59.8), (-2.2, 65.9), (2.2, 65.9), (3.2, 59.8)]
front_sight = prism_y("front_sight", fs_prof, 149, 156, "steel_plain", 0.5)
rear = box("rear_sight", -7.2, 7.2, -19, -11.8, 60.5, 68.0, "steel_plain", 0.6)
cut(rear, box("notch", -1.5, 1.5, -20, -10.5, 63.5, 69, "steel_plain", 0.5))
sl = join("slide", [sl, front_sight, rear])
# tre punti bianchi, sulle facce posteriori delle mire
dots = [cylinder("dot_f", 0.7, 148.4, 149.0, 0, 64.0, "sight_white", 12)]
for x in (-5.6, 5.6):
    dots.append(cylinder("dot_r", 0.55, -19.4, -18.8, x, 64.4, "sight_white", 10))
sl = join("slide", [sl] + dots)
# marchio in rilievo lato sinistro
mark = text_mesh("mark", "SERAFINI ARMI \u2014 ITALIA", 3.4, -1, -12.7, 50, 52.0, 0.35,
                 "marking_light")
sl = join("slide", [sl, mark])
cal = text_mesh("caliber", "9x19", 2.4, 1, 12.7, 22, 52.0, 0.4, "marking_light")
sl = join("slide", [sl, cal, barrel])
slide = sl

# ================================================================== HAMMER
# cane esterno, perno a (0, -18.5, 36): sperone orizzontale + corpo arcuato
ham_prof = rounded([
    (-16.6, 33.0), (-16.5, 41.0), (-17.2, 45.0), (-18.2, 48.0), (-20.0, 49.8),
    (-22.6, 50.2), (-23.6, 48.8), (-23.4, 47.2), (-20.6, 46.6), (-19.4, 44.0),
    (-18.9, 40.0), (-18.8, 33.0),
], [0.6, 1.0, 1.0, 0.8, 0.8, 0.8, 0.8, 0.8, 0.8, 1.0, 1.0, 0.6], nc=2)
hammer = prism("hammer", ham_prof, -4.0, 4.0, "steel_plain", 0.0, smooth=True)

# ================================================================== TRIGGER
# lama prismatica appesa al perno (0, 26.5, 8.5): spessore 4 mm, faccia anteriore convessa
trg_profile = [
    (31.6, 16.0), (30.6, 16.0), (30.2, 10.0), (29.8, 3.0), (28.8, -3.8),
    (27.2, -8.4), (25.2, -10.6), (23.6, -9.2), (24.2, -5.8), (25.4, -0.6),
    (26.4, 5.2), (26.9, 11.0), (27.1, 16.0),
]
trigger = prism("trigger", trg_profile, -5.0, 5.0, "trigger_steel", 1.0, smooth=False)

# ================================================================== MAG
mag_keys = [
    (18, 20, 17, 12.0, 3.0),
    (10, 19, -20, 12.6, 2.8),
    (-6, 15, -23, 13.2, 2.6),
    (-20, 11, -25, 13.0, 2.6),
    (-34, 7, -26, 12.4, 2.6),
    (-46, 4, -26.5, 11.8, 2.7),
    (-56, 2, -26.5, 11.2, 2.8),
]
mag_sections = []
for k in range(len(mag_keys) - 1):
    p0 = mag_keys[max(k - 1, 0)]; p1 = mag_keys[k]
    p2 = mag_keys[k + 1]; p3 = mag_keys[min(k + 2, len(mag_keys) - 1)]
    for s in range(2):
        t = s / 2
        mag_sections.append(tuple(catmull(p0[i], p1[i], p2[i], p3[i], t) for i in range(5)))
mag_sections.append(mag_keys[-1])
mag_parts = [loft_yz("mag_body", mag_sections, "polymer_black", 20, 0.0)]
# base del caricatore a filo dell'impugnatura, appena sporgente davanti
mag_parts.append(prism("mag_floor", [(-23.5, -61.0), (2.0, -61.0), (3.5, -62.5),
                                     (3.5, -68.8), (2.0, -70.0), (-22.0, -70.0),
                                     (-23.5, -68.8)],
                       -13.4, 13.4, "polymer_black", 1.6))
mag = join("mag", mag_parts)

for ob, ang in ((body, 36.0), (slide, 40.0), (trigger, 40.0), (hammer, 40.0), (mag, 34.0)):
    auto_smooth(ob, ang)

body.name = "body"; body.data.name = "body"
slide.name = "slide"; slide.data.name = "slide"
trigger.name = "trigger"; trigger.data.name = "trigger"
hammer.name = "hammer"; hammer.data.name = "hammer"
mag.name = "mag"; mag.data.name = "mag"

# ================================================================== GERARCHIA
def empty(name, loc=(0, 0, 0), parent=None):
    e = bpy.data.objects.new(name, None)
    e.empty_display_size = 0.012
    e.empty_display_type = 'PLAIN_AXES'
    bpy.context.scene.collection.objects.link(e)
    e.location = loc
    if parent:
        e.parent = parent
        e.matrix_parent_inverse = Matrix.Identity(4)
    return e

root = empty("standard")
for ob in (body, slide, trigger, hammer, mag):
    ob.parent = root
    ob.matrix_parent_inverse = Matrix.Identity(4)

SOCKETS = {
    "socket_muzzle": (0, 170, 46.5),
    "socket_eject":  (12.7, 118, 55),
    "socket_hand_r": (-4, -4, -28),
    "socket_hand_l": (-17, 6, -30),
    "socket_aim":    (0, -13, 64.5),
}
for n, p in SOCKETS.items():
    empty(n, (mm(p[0]), mm(p[1]), mm(p[2])), root)

# ================================================================== REPORT
def bbox_mm(objs):
    lo = Vector((1e9, 1e9, 1e9)); hi = Vector((-1e9, -1e9, -1e9))
    dg = bpy.context.evaluated_depsgraph_get()
    for o in objs:
        oe = o.evaluated_get(dg); me = oe.to_mesh()
        for v in me.vertices:
            w = oe.matrix_world @ v.co
            for i in range(3):
                lo[i] = min(lo[i], w[i]); hi[i] = max(hi[i], w[i])
        oe.to_mesh_clear()
    return lo, hi

def tri_count(objs):
    t = 0
    dg = bpy.context.evaluated_depsgraph_get()
    for o in objs:
        oe = o.evaluated_get(dg); me = oe.to_mesh()
        me.calc_loop_triangles(); t += len(me.loop_triangles); oe.to_mesh_clear()
    return t

meshes = [body, slide, trigger, hammer, mag]
lo, hi = bbox_mm(meshes)
size = [(hi[i] - lo[i]) * 1000 for i in range(3)]
rep = {
    "bbox_mm": {"min": [round(lo[i] * 1000, 2) for i in range(3)],
                "max": [round(hi[i] * 1000, 2) for i in range(3)],
                "size": [round(s, 2) for s in size]},
    "delta_mm": [round(size[0] - 33, 2), round(size[1] - 197, 2), round(size[2] - 138, 2)],
    "tris_total": tri_count(meshes),
    "objects": [],
    "sockets": {k: [round(c, 2) for c in v] for k, v in SOCKETS.items()},
}
for o in meshes:
    olo, ohi = bbox_mm([o])
    rep["objects"].append({"name": o.name, "tris": tri_count([o]),
                           "bbox_mm": [round(olo[i] * 1000, 1) for i in range(3)] +
                                      [round(ohi[i] * 1000, 1) for i in range(3)],
                           "materials": [s.material.name for s in o.material_slots if s.material]})

bpy.context.scene.render.fps = 30
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(ROOT, "standard.blend"))
json.dump(rep, open(os.path.join(REVIEW, "02_detail_report.json"), "w"), indent=2)
print("REPORT " + json.dumps(rep))
