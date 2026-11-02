"""
FALEZIA · art/weapons/hush · 06_export.py
Valida (99_validate) e poi esporta:
  game/assets/weapons/hush/hush.glb      viewmodel, gerarchia + animazioni
  game/assets/weapons/hush/hush_tp.glb   un solo mesh, <=1500 tri, texture 512, no anim
  game/assets/weapons/hush/hush.json     dati misurati
"""
import bpy, bmesh, os, json, math, importlib.util, sys
from mathutils import Vector, Matrix

ROOT = "/home/matteo/Progetti/Falezia/art/weapons/hush"
GAME = "/home/matteo/Progetti/Falezia/game/assets/weapons/hush"
TEX = os.path.join(ROOT, "textures")
os.makedirs(GAME, exist_ok=True)
os.makedirs(os.path.join(TEX, "512"), exist_ok=True)

bpy.ops.wm.open_mainfile(filepath=os.path.join(ROOT, "hush.blend"))

# ---------------------------------------------------------------- validazione (blocca l'export)
spec = importlib.util.spec_from_file_location("validate_hush", os.path.join(ROOT, "99_validate.py"))
val = importlib.util.module_from_spec(spec)
spec.loader.exec_module(val)
errs = val.run_checks()
if errs:
    print("EXPORT BLOCCATO DAL VALIDATOR:")
    for e in errs:
        print("  -", e)
    sys.exit(2)
print("VALIDATOR OK")

MESHES = [bpy.data.objects[n] for n in ("body", "slide", "trigger", "hammer", "mag", "suppressor", "barrel")]
def tris(objs):
    t = 0
    dg = bpy.context.evaluated_depsgraph_get()
    for o in objs:
        oe = o.evaluated_get(dg); me = oe.to_mesh()
        me.calc_loop_triangles(); t += len(me.loop_triangles); oe.to_mesh_clear()
    return t

# ripristina la posa (il validator l'ha messa a riposo, va bene anche per l'export)
bpy.context.view_layer.update()
TRIS_VM = tris(MESHES)
dims = val._dims_mm(MESHES)

# pulizia dei dati rimasti dal bake e salvataggio del viewmodel "pulito"
# (va fatto ORA: dopo la costruzione del TP la scena contiene solo la mesh unita)
for _ in range(4):
    bpy.data.orphans_purge(do_local_ids=True, do_linked_ids=False, do_recursive=True)
for ob in list(bpy.data.objects):
    if ob.name.startswith("preview_"):
        bpy.data.objects.remove(ob, do_unlink=True)
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(ROOT, "hush.blend"))
print("CLEAN materials=%d meshes=%d images=%d actions=%d" % (
    len(bpy.data.materials), len(bpy.data.meshes), len(bpy.data.images), len(bpy.data.actions)))

# texture metallic: rinomina il file prodotto dal bake
img = bpy.data.images.get("T_metallic")
if img:
    src = bpy.path.abspath(img.filepath)
    dst = os.path.join(TEX, "hush_metallic.png")
    if os.path.exists(src) and src != dst:
        os.replace(src, dst)
    img.filepath_raw = dst
    img.save()

# ---------------------------------------------------------------- 1. viewmodel
sc = bpy.context.scene
sc.frame_set(1)
bpy.ops.export_scene.gltf(
    filepath=os.path.join(GAME, "hush.glb"),
    export_format='GLB',
    use_selection=False,
    export_apply=True,
    export_yup=True,
    export_animations=True,
    export_animation_mode='NLA_TRACKS',
    export_nla_strips=True,
    export_bake_animation=False,
    export_optimize_animation_size=False,
    export_optimize_animation_keep_anim_object=True,
    export_anim_slide_to_zero=True,
    export_materials='EXPORT',
    export_image_format='AUTO',
    export_texcoords=True,
    export_normals=True,
    export_tangents=True,
    export_extras=True,
    export_cameras=False,
    export_lights=False,
)
print("EXPORTED hush.glb")

# ---------------------------------------------------------------- 2. terza persona
# set 512
img512 = {}
for key in ("basecolor", "roughness", "metallic", "normal", "ao"):
    im = bpy.data.images.get("T_" + key)
    if not im:
        continue
    copy = im.copy()
    copy.name = "T512_" + key
    copy.scale(512, 512)
    p = os.path.join(TEX, "512", "hush_%s_512.png" % key)
    copy.filepath_raw = p
    copy.file_format = 'PNG'
    copy.save()
    img512[key] = copy

# mesh unica: applica le trasformate e unisci
bm = bmesh.new()
for o in MESHES:
    me = o.data.copy()
    me.transform(o.matrix_world)
    bm.from_mesh(me)
    bpy.data.meshes.remove(me)
tp_mesh = bpy.data.meshes.new("hush_tp")
bm.to_mesh(tp_mesh)
bm.free()

# materiale TP con le 512
std = bpy.data.materials.get("hush_mat")
std.name = "hush_mat_vm"
tp_mat = std.copy()
tp_mat.name = "hush_mat"
nt = tp_mat.node_tree
for n in nt.nodes:
    if n.type == 'TEX_IMAGE':
        key = n.image.name.replace("T_", "").replace("T512_", "")
        if key in img512:
            n.image = img512[key]
    if n.type == 'NORMAL_MAP':
        pass

tp_mesh.materials.append(tp_mat)
tp = bpy.data.objects.new("hush_tp", tp_mesh)
sc.collection.objects.link(tp)

# decima al budget
n_tris = len(tp_mesh.loop_triangles) if tp_mesh.loop_triangles else 0
if n_tris == 0:
    tp_mesh.calc_loop_triangles()
    n_tris = len(tp_mesh.loop_triangles)
if n_tris > 1500:
    md = tp.modifiers.new("dec", 'DECIMATE')
    md.decimate_type = 'COLLAPSE'
    md.ratio = 1500.0 / n_tris
    bpy.context.view_layer.objects.active = tp
    bpy.context.view_layer.update()
    dg = bpy.context.evaluated_depsgraph_get()
    new_me = bpy.data.meshes.new_from_object(tp.evaluated_get(dg))
    tp.modifiers.clear()
    old = tp.data
    tp.data = new_me
    bpy.data.meshes.remove(old)
tp_mesh = tp.data
tp_mesh.calc_loop_triangles()
TRIS_TP = len(tp_mesh.loop_triangles)
print("TP tris", TRIS_TP)

# scena per l'export TP: solo il mesh
for o in list(bpy.data.objects):
    if o is not tp:
        bpy.data.objects.remove(o, do_unlink=True)
sc.frame_set(1)
bpy.ops.export_scene.gltf(
    filepath=os.path.join(GAME, "hush_tp.glb"),
    export_format='GLB',
    use_selection=False,
    export_apply=True,
    export_yup=True,
    export_animations=False,
    export_materials='EXPORT',
    export_image_format='AUTO',
    export_texcoords=True,
    export_normals=True,
)
print("EXPORTED hush_tp.glb")

# ---------------------------------------------------------------- 3. json
anim = json.load(open(os.path.join(ROOT, "review", "05_anim_report.json")))
data = {
    "id": "hush",
    "class": "pistol",
    "size_mm": [round(dims[1], 1), round(dims[2], 1), round(dims[0], 1)],  # Y, Z, X
    "tris": {"viewmodel": TRIS_VM, "tp": TRIS_TP},
    "material": "hush_mat",
    "sockets": ["socket_muzzle", "socket_eject", "socket_hand_r", "socket_hand_l", "socket_aim", "socket_supp"],
    "animations": {},
}
for name in ("idle", "fire", "fire_last", "reload", "reload_empty", "draw", "holster"):
    data["animations"][name] = {
        "length_s": round(anim["lengths"][name], 3),
        "events": {k: round(v, 3) for k, v in anim["events"].get(name, {}).items()},
    }
json.dump(data, open(os.path.join(GAME, "hush.json"), "w"), indent=2)
print("JSON " + json.dumps(data))

