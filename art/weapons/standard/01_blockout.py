"""
FALEZIA · art/weapons/standard · 01_blockout.py
Standard · Serafini Armi — blockout volumetrico a misura esatta.

Assi:  +Y = canna (volata a +170 mm), +Z = alto, +X = destra del tiratore.
Origine: centro dell'impugnatura, all'altezza del grilletto.
Unita': 1 unita' Blender = 1 m. Tutte le quote qui sotto sono in MILLIMETRI.

Questo script e' la fonte: rilancia da zero la scena e la salva.
"""
import bpy, bmesh, math, os, json
from mathutils import Vector

ROOT = "/home/matteo/Progetti/Falezia/art/weapons/standard"
REVIEW = os.path.join(ROOT, "review")
GAME = "/home/matteo/Progetti/Falezia/game/assets/weapons/standard"
for d in (ROOT, REVIEW, GAME):
    os.makedirs(d, exist_ok=True)

# ---------------------------------------------------------------- scene
# NB: mai bpy.ops.wm.read_factory_settings() da MCP: ricarica il file Blender e
# azzera il bridge dell'addon (bridge = None -> tutti i tool smettono di rispondere).
# La scena si svuota a mano, restando dentro la sessione.
def clear_scene():
    for ob in list(bpy.data.objects):
        bpy.data.objects.remove(ob, do_unlink=True)
    for coll in (bpy.data.meshes, bpy.data.materials, bpy.data.cameras,
                 bpy.data.lights, bpy.data.curves, bpy.data.armatures):
        for b in list(coll):
            if b.users == 0:
                try:
                    coll.remove(b)
                except Exception:
                    pass
    for c in list(bpy.data.collections):
        if not c.objects and not c.children:
            try:
                bpy.data.collections.remove(c)
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

# ---------------------------------------------------------------- materials
def hex_to_linear(h):
    """sRGB hex -> linear RGBA (Blender lavora in lineare)."""
    h = h.lstrip('#')
    out = []
    for i in (0, 2, 4):
        c = int(h[i:i + 2], 16) / 255.0
        out.append(c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4)
    return (out[0], out[1], out[2], 1.0)

def make_mat(name, hexcol, rough, metallic):
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    m.use_nodes = True
    b = m.node_tree.nodes["Principled BSDF"]
    b.inputs['Base Color'].default_value = hex_to_linear(hexcol)
    b.inputs['Roughness'].default_value = rough
    b.inputs['Metallic'].default_value = metallic
    m.diffuse_color = hex_to_linear(hexcol)
    return m

MAT = {
    "steel_phosphate": make_mat("steel_phosphate", "3A3D40", 0.65, 1.0),
    "polymer_black":   make_mat("polymer_black",   "1B1B1D", 0.80, 0.0),
    "metal_bare":      make_mat("metal_bare",      "8A8D8F", 0.45, 1.0),
    "sight_white":     make_mat("sight_white",     "FFFFFF", 0.80, 0.0),
    "barrel":          make_mat("barrel",          "3A3D40", 0.40, 1.0),
}

# ---------------------------------------------------------------- geometry helpers
_parts = []

def _apply_mods(ob):
    """Applica i modificatori senza operatori (indipendente dal contesto)."""
    if not ob.modifiers:
        return
    dg = bpy.context.evaluated_depsgraph_get()
    me = bpy.data.meshes.new_from_object(ob.evaluated_get(dg))
    old = ob.data
    ob.modifiers.clear()
    ob.data = me
    bpy.data.meshes.remove(old)

def _finish(bm, name, mat, bevel):
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    ob = bpy.data.objects.new(name, me)
    bpy.context.scene.collection.objects.link(ob)
    me.materials.append(MAT[mat])
    if bevel > 0:
        md = ob.modifiers.new("bevel", 'BEVEL')
        md.width = mm(bevel)
        md.segments = 2
        md.limit_method = 'ANGLE'
        md.angle_limit = math.radians(35)
        _apply_mods(ob)
    _parts.append(ob)
    return ob

def box(name, x0, x1, y0, y1, z0, z1, mat, bevel=0.0):
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1.0)
    bmesh.ops.scale(bm, vec=(mm(x1 - x0), mm(y1 - y0), mm(z1 - z0)), verts=bm.verts)
    bmesh.ops.translate(bm, vec=(mm((x0 + x1) / 2), mm((y0 + y1) / 2), mm((z0 + z1) / 2)), verts=bm.verts)
    return _finish(bm, name, mat, bevel)

def prism(name, pts_yz, x0, x1, mat, bevel=0.0):
    """Poligono nel piano YZ estruso lungo X."""
    bm = bmesh.new()
    vs = [bm.verts.new((mm(x0), mm(y), mm(z))) for (y, z) in pts_yz]
    f = bm.faces.new(vs)
    r = bmesh.ops.extrude_face_region(bm, geom=[f])
    ext = [v for v in r['geom'] if isinstance(v, bmesh.types.BMVert)]
    bmesh.ops.translate(bm, verts=ext, vec=(mm(x1 - x0), 0, 0))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    return _finish(bm, name, mat, bevel)

def join(name, objs):
    """Unisce le mesh senza operatori; tutti i pezzi condividono un materiale."""
    bm = bmesh.new()
    for o in objs:
        bm.from_mesh(o.data)
    me = bpy.data.meshes.new(name + "_mesh")
    bm.to_mesh(me)
    bm.free()
    me.materials.append(objs[0].data.materials[0])
    ob = bpy.data.objects.new(name, me)
    bpy.context.scene.collection.objects.link(ob)
    for o in objs:
        d = o.data
        bpy.data.objects.remove(o, do_unlink=True)
        bpy.data.meshes.remove(d)
    return ob

# ---------------------------------------------------------------- BODY
body_parts = []
# fusto: rotaia superiore sotto il carrello
body_parts.append(box("frame_rail", -14, 14, -23, 137, 24, 33, "polymer_black", 0.8))
# blocco posteriore (culatta, ospita cane e fusto)
body_parts.append(box("frame_rear", -14, 14, -23, 57, 8, 33, "polymer_black", 0.8))
# paracolpi / dust cover sotto la canna
body_parts.append(box("dust_cover", -13, 13, 57, 141, 14, 26, "polymer_black", 1.2))
# ponte di guardia (anello squadrato in 4 barre)
gy0, gy1 = 15, 63          # ingombro Y della guardia
gz0, gz1 = -18, 26         # ingombro Z
body_parts.append(box("guard_rear", -7, 7, gy0, 23, -14, 20, "polymer_black", 0.8))
body_parts.append(box("guard_top", -7, 7, 15, 61, 18, 26, "polymer_black", 0.8))
body_parts.append(box("guard_front", -7, 7, 53, 63, -18, 26, "polymer_black", 1.2))
body_parts.append(box("guard_bottom", -7, 7, 15, 63, -18, -10, "polymer_black", 1.2))
# impugnatura: profilo curvo, centro a Y=0 all'altezza del grilletto (Z=0)
grip_profile = [
    (37, 26), (-19, 26), (-23, 10), (-27, -36),
    (-23, -52), (-19, -62), (13, -62),
    (17, -46), (19, -40), (24, -14), (31, 6),
]
body_parts.append(prism("grip", grip_profile, -16.5, 16.5, "polymer_black", 1.5))
# beavertail
body_parts.append(box("beavertail", -10, 10, -27, -13, 26, 34, "polymer_black", 1.2))
# levamolle quadrato sopra l'impugnatura (lamina in acciaio nudo, incassata)
body_parts.append(box("leaf_spring", -8, 8, -27.5, -23.5, 4, 20, "metal_bare", 0.4))
body = join("body", body_parts)

# ---------------------------------------------------------------- SLIDE
slide_parts = []
slide_parts.append(box("slide_body", -12.7, 12.7, -19, 170, 33, 61, "steel_phosphate", 1.5))
# mire fisse a tre punti (blocchi; i punti bianchi arrivano nel dettaglio)
slide_parts.append(box("front_sight", -2.6, 2.6, 161, 167, 61, 68, "steel_phosphate", 0.5))
slide_parts.append(box("rear_sight", -8.5, 8.5, -17, -9, 61, 68, "steel_phosphate", 0.5))
slide = join("slide", slide_parts)

# ---------------------------------------------------------------- HAMMER
hammer = box("hammer", -4.5, 4.5, -27, -19, 34, 50, "steel_phosphate", 0.6)

# ---------------------------------------------------------------- TRIGGER
trigger = box("trigger", -3.5, 3.5, 23, 29, -9, 9, "steel_phosphate", 0.5)

# ---------------------------------------------------------------- MAG
mag_parts = []
mag_profile = [
    (28, 18), (-12, 18), (-16, -34), (-12, -56),
    (5, -56), (9, -40), (13, -14),
]
mag_parts.append(prism("mag_body", mag_profile, -12, 12, "polymer_black", 0.8))
mag_parts.append(box("mag_floor", -15.5, 15.5, -23, 15, -70, -62, "polymer_black", 1.2))
mag = join("mag", mag_parts)

# ---------------------------------------------------------------- GERARCHIA
def empty(name, loc=(0, 0, 0), parent=None):
    e = bpy.data.objects.new(name, None)
    e.empty_display_size = 0.012
    e.empty_display_type = 'PLAIN_AXES'
    bpy.context.scene.collection.objects.link(e)
    e.location = loc
    if parent:
        e.parent = parent
        e.matrix_parent_inverse = parent.matrix_world.inverted()
    return e

root = empty("standard")
for ob in (body, slide, trigger, hammer, mag):
    ob.parent = root
    ob.matrix_parent_inverse = root.matrix_world.inverted()

SOCKETS = {
    "socket_muzzle": (0, 170, 47),
    "socket_eject":  (12.7, 118, 52),
    "socket_hand_r": (-4, -2, -26),
    "socket_hand_l": (-17, 8, -30),
    "socket_aim":    (0, -13, 68),
}
for n, p in SOCKETS.items():
    empty(n, (mm(p[0]), mm(p[1]), mm(p[2])), root)

# ---------------------------------------------------------------- REPORT
def bbox_mm(objs):
    lo = Vector((1e9, 1e9, 1e9))
    hi = Vector((-1e9, -1e9, -1e9))
    dg = bpy.context.evaluated_depsgraph_get()
    for o in objs:
        oe = o.evaluated_get(dg)
        me = oe.to_mesh()
        for v in me.vertices:
            w = oe.matrix_world @ v.co
            for i in range(3):
                lo[i] = min(lo[i], w[i])
                hi[i] = max(hi[i], w[i])
        oe.to_mesh_clear()
    return lo, hi

def tris(objs):
    total = 0
    dg = bpy.context.evaluated_depsgraph_get()
    for o in objs:
        oe = o.evaluated_get(dg)
        me = oe.to_mesh()
        me.calc_loop_triangles()
        total += len(me.loop_triangles)
        oe.to_mesh_clear()
    return total

meshes = [body, slide, trigger, hammer, mag]
lo, hi = bbox_mm(meshes)
size = [(hi[i] - lo[i]) * 1000 for i in range(3)]
report = {
    "bbox_mm": {
        "x": [round(lo.x * 1000, 2), round(hi.x * 1000, 2)],
        "y": [round(lo.y * 1000, 2), round(hi.y * 1000, 2)],
        "z": [round(lo.z * 1000, 2), round(hi.z * 1000, 2)],
        "size": [round(s, 2) for s in size],
    },
    "target_mm": [197, 138, 33],
    "delta_mm": [round(size[0] - 33, 2), round(size[1] - 197, 2), round(size[2] - 138, 2)],
    "tris_total": tris(meshes),
    "objects": [],
    "sockets": {k: [round(c * 1000, 2) for c in v] for k, v in SOCKETS.items()},
}
for o in meshes:
    olo, ohi = bbox_mm([o])
    report["objects"].append({
        "name": o.name,
        "tris": tris([o]),
        "bbox_mm": [round(olo[i] * 1000, 1) for i in range(3)] + [round(ohi[i] * 1000, 1) for i in range(3)],
    })

# ---------------------------------------------------------------- SILHOUETTE TEST
# arma in nero pieno su bianco, ortografico laterale sinistro, 85% del frame
sil = bpy.data.materials.new("silhouette_black")
sil.use_nodes = True
nt = sil.node_tree
nt.nodes.clear()
out = nt.nodes.new('ShaderNodeOutputMaterial')
em = nt.nodes.new('ShaderNodeEmission')
em.inputs['Color'].default_value = (0, 0, 0, 1)
em.inputs['Strength'].default_value = 1.0
nt.links.new(em.outputs[0], out.inputs['Surface'])

saved = {}
for o in meshes:
    saved[o.name] = [s.material for s in o.material_slots]
    o.data.materials.clear()
    o.data.materials.append(sil)

world = bpy.data.worlds.new("SIL")
sc.world = world
world.use_nodes = True
bg = world.node_tree.nodes["Background"]
bg.inputs['Color'].default_value = (1, 1, 1, 1)
bg.inputs['Strength'].default_value = 1.0

cam_data = bpy.data.cameras.new("SilCam")
cam_data.type = 'ORTHO'
cam = bpy.data.objects.new("SilCam", cam_data)
bpy.context.scene.collection.objects.link(cam)
sc.camera = cam

# vista da -X verso +X: si vede il lato sinistro, volata a sinistra nell'immagine
cy = (lo.y + hi.y) / 2
cz = (lo.z + hi.z) / 2
cam.location = (lo.x - 1.0, cy, cz)
cam.rotation_euler = (Vector((1, 0, 0))).to_track_quat('-Z', 'Y').to_euler()

RES_X, RES_Y = 1600, 1000
sc.render.resolution_x = RES_X
sc.render.resolution_y = RES_Y
sc.render.resolution_percentage = 100
sc.render.film_transparent = False
sc.render.engine = 'BLENDER_EEVEE'
sc.eevee.taa_render_samples = 16
sc.view_settings.view_transform = 'Standard'
sc.view_settings.look = 'None'
sc.view_settings.exposure = 0.0

size_y = (hi.y - lo.y)     # orizzontale in vista
size_z = (hi.z - lo.z)     # verticale
fill = 0.85
scale_v = size_z / (RES_Y * fill)          # metri per pixel
view_w = RES_X * scale_v
cam_data.ortho_scale = view_w
sc.render.filepath = os.path.join(REVIEW, "silhouette_left.png")
bpy.ops.render.render(write_still=True)

# ripristino materiali
for o in meshes:
    o.data.materials.clear()
    for m in saved[o.name]:
        o.data.materials.append(m)
bpy.data.objects.remove(cam, do_unlink=True)
bpy.data.materials.remove(sil)

# ---------------------------------------------------------------- SAVE
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(ROOT, "standard.blend"))
with open(os.path.join(REVIEW, "01_blockout_report.json"), "w") as f:
    json.dump(report, f, indent=2)
print(json.dumps(report, indent=2))
