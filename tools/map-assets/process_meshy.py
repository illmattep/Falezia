"""Da GLB di Meshy a asset di gioco: scala, pivot, decimazione, texture, anteprima.

  blender -b -noaudio --factory-startup --python process_meshy.py -- jobs.json

jobs.json = lista di:
  {"src": ".../chest.glb", "out": ".../objective_chest.glb",
   "size": [1.2,1.0,0.8], "tris": 2500, "fit": "box"|"width", "tex": 1024,
   "preview": ".../preview.png", "category": "objective"}
"""
import bpy, sys, json, os, math
from mathutils import Vector

def clean():
    bpy.ops.wm.read_factory_settings(use_empty=True)

def world_bbox(objs):
    mn = Vector((1e9,)*3); mx = Vector((-1e9,)*3)
    for o in objs:
        for c in o.bound_box:
            w = o.matrix_world @ Vector(c)
            mn = Vector((min(mn[i], w[i]) for i in range(3)))
            mx = Vector((max(mx[i], w[i]) for i in range(3)))
    return mn, mx

def apply_transform(objs):
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)

def decimate(objs, target_tris):
    total = 0
    for o in objs:
        o.data.calc_loop_triangles()
        total += len(o.data.loop_triangles)
    if total <= target_tris or total == 0:
        return total, total
    ratio = max(0.001, target_tris / total)
    for o in objs:
        bpy.context.view_layer.objects.active = o
        m = o.modifiers.new("dec", 'DECIMATE')
        m.decimate_type = 'COLLAPSE'
        m.ratio = ratio
        bpy.ops.object.modifier_apply(modifier=m.name)
    after = 0
    for o in objs:
        o.data.calc_loop_triangles()
        after += len(o.data.loop_triangles)
    return total, after

def resize_textures(max_px):
    for im in bpy.data.images:
        w, h = im.size
        if w > max_px or h > max_px:
            k = max_px / max(w, h)
            im.scale(max(1, int(w * k)), max(1, int(h * k)))
            try: im.pack()
            except Exception: pass

def fit_and_pivot(objs, target_world, mode):
    # target_world è [larghezza, altezza, profondità] (convenzione del gioco, Y su).
    # Blender ha Z su, quindi qui diventa [X, Y=profondità, Z=altezza].
    target = [target_world[0], target_world[2], target_world[1]]
    mn, mx = world_bbox(objs)
    size = mx - mn
    if mode == "height":
        k = target[2] / max(size.z, 1e-6)
    elif mode == "width":
        # la larghezza (X) deve combaciare col modulo: 3 m per le facciate
        k = target[0] / max(size.x, 1e-6)
    else:
        # dentro la scatola dichiarata, senza sforare
        k = min(target[i] / max(size[i], 1e-6) for i in range(3))
    for o in objs:
        o.scale = (o.scale.x * k, o.scale.y * k, o.scale.z * k)
        bpy.context.view_layer.objects.active = o
        o.select_set(True)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)

    mn, mx = world_bbox(objs)
    cx, cz = (mn.x + mx.x) / 2, (mn.z + mx.z) / 2
    for o in objs:
        o.location.x -= cx
        o.location.z -= cz
        o.location.z -= 0            # Z è l'altezza in Blender: il pivot va alla BASE
        o.location.y -= mn.y
    bpy.ops.object.transform_apply(location=True, rotation=False, scale=True)
    mn, mx = world_bbox(objs)
    return [round(v, 3) for v in (mx - mn)], k

def setup_preview(objs, path):
    mn, mx = world_bbox(objs)
    d = max((mx - mn).length, 0.3)
    bpy.ops.object.camera_add(location=(d * 1.4, -d * 1.9, d * 1.15))
    cam = bpy.context.object
    cam.rotation_euler = (math.radians(68), 0, math.radians(37))
    bpy.context.scene.camera = cam
    bpy.ops.object.light_add(type='SUN', location=(3, -4, 6))
    bpy.context.object.data.energy = 3.0
    bpy.ops.object.light_add(type='AREA', location=(-4, -3, 3))
    bpy.context.object.data.energy = 200
    bpy.context.scene.render.engine = 'CYCLES'
    bpy.context.scene.cycles.samples = 24
    bpy.context.scene.cycles.device = 'CPU'
    bpy.context.scene.render.resolution_x = 512
    bpy.context.scene.render.resolution_y = 512
    bpy.context.scene.render.filepath = path
    bpy.context.scene.render.image_settings.file_format = 'PNG'
    bpy.ops.render.render(write_still=True)

def run(job):
    clean()
    bpy.ops.import_scene.gltf(filepath=job["src"])
    objs = [o for o in bpy.context.scene.objects if o.type == 'MESH']
    if not objs:
        return {"ok": False, "error": "nessuna mesh", "src": job["src"]}

    apply_transform(objs)

    # unisce i pezzi in una mesh sola: la decimazione lavora sul totale, non pezzo per pezzo
    if len(objs) > 1:
        bpy.ops.object.select_all(action='DESELECT')
        for o in objs: o.select_set(True)
        bpy.context.view_layer.objects.active = objs[0]
        bpy.ops.object.join()
        objs = [o for o in bpy.context.scene.objects if o.type == 'MESH']

    before, after = decimate(objs, job.get("tris", 2000))
    resize_textures(job.get("tex", 1024))
    size, k = fit_and_pivot(objs, job["size"], job.get("fit", "box"))

    os.makedirs(os.path.dirname(job["out"]), exist_ok=True)
    bpy.ops.object.select_all(action='DESELECT')
    for o in objs:
        o.select_set(True)
    bpy.ops.export_scene.gltf(
        filepath=job["out"], export_format='GLB', use_selection=True,
        export_apply=True, export_yup=True, export_image_format='AUTO',
        export_materials='EXPORT', export_cameras=False, export_lights=False)

    if job.get("preview"):
        setup_preview(objs, job["preview"])

    return {"ok": True, "src": os.path.basename(job["src"]), "out": job["out"],
            "tris_before": before, "tris_after": after, "scale": round(k, 4),
            "size_m": size, "mb_out": round(os.path.getsize(job["out"]) / 1048576, 2)}

jobs = json.load(open(sys.argv[sys.argv.index("--") + 1]))
for j in jobs:
    try:
        print("RESULT:" + json.dumps(run(j), ensure_ascii=False))
    except Exception as e:
        print("RESULT:" + json.dumps({"ok": False, "error": str(e), "src": j.get("src")}))
