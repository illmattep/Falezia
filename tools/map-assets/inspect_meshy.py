"""Misura i GLB di Meshy: dimensioni reali, triangoli, materiali, texture.

Uso:
  blender -b -noaudio --factory-startup --python inspect_meshy.py -- file1.glb file2.glb ...

Stampa una riga JSON per file su stdout, preceduta da "RESULT:".
"""
import bpy, sys, json, os
from mathutils import Vector

def clean():
    bpy.ops.wm.read_factory_settings(use_empty=True)

def measure(path):
    clean()
    bpy.ops.import_scene.gltf(filepath=path)

    objs = [o for o in bpy.context.scene.objects if o.type == 'MESH']
    if not objs:
        return {"file": path, "error": "nessuna mesh"}

    tris = 0
    mn = Vector((1e9, 1e9, 1e9))
    mx = Vector((-1e9, -1e9, -1e9))
    for o in objs:
        me = o.data
        me.calc_loop_triangles()
        tris += len(me.loop_triangles)
        for corner in o.bound_box:
            w = o.matrix_world @ Vector(corner)
            mn = Vector((min(mn[i], w[i]) for i in range(3)))
            mx = Vector((max(mx[i], w[i]) for i in range(3)))

    size = mx - mn
    mats = sorted({m.name for o in objs for m in o.data.materials if m})
    imgs = []
    for im in bpy.data.images:
        if im.size[0] and im.size[1]:
            imgs.append({"name": im.name, "w": im.size[0], "h": im.size[1]})

    return {
        "file": os.path.basename(path),
        "objects": len(objs),
        "tris": tris,
        "size_m": [round(size.x, 3), round(size.y, 3), round(size.z, 3)],
        "min": [round(v, 2) for v in mn],
        "max": [round(v, 2) for v in mx],
        "materials": mats,
        "images": imgs,
        "mb": round(os.path.getsize(path) / 1048576, 1),
    }

args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
for p in args:
    try:
        print("RESULT:" + json.dumps(measure(p), ensure_ascii=False))
    except Exception as e:
        print("RESULT:" + json.dumps({"file": os.path.basename(p), "error": str(e)}))
