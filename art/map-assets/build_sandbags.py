"""Sacchi di sabbia: muretto di 1,4 × 0,9 × 0,6 m, tre file sfalsate di sacchi gonfi.

Dettaglio militare per gli spawn: al Forte chiude i lati della porta, al Porto protegge
la banchina. Geometria semplice ma con sacchi arrotondati, non cubi.

  blender -b -noaudio --factory-startup --python build_sandbags.py
"""
import bpy, bmesh, math, os
ROOT = "/home/matteo/Progetti/Falezia"
OUT = f"{ROOT}/Assets/StreamingAssets/map/military"
os.makedirs(OUT, exist_ok=True)

def burlap():
    m = bpy.data.materials.new("iuta"); m.use_nodes = True
    b = m.node_tree.nodes["Principled BSDF"]
    b.inputs["Base Color"].default_value = (0.56, 0.50, 0.36, 1)
    b.inputs["Roughness"].default_value = 0.95
    b.inputs["Metallic"].default_value = 0.0
    return m

def bag(bm, x, y, z, sx, sy, sz):
    # i vertici nuovi si prendono dall'indice di partenza: prima sbagliavo la fetta e
    # deformavo anche i sacchi già creati (da lì i triangoli impazziti)
    start = len(bm.verts)
    bmesh.ops.create_uvsphere(bm, u_segments=10, v_segments=6, radius=0.5)
    bm.verts.ensure_lookup_table()
    for v in bm.verts[start:]:
        v.co.x = v.co.x * sx + x
        v.co.y = v.co.y * sy + y
        v.co.z = max(v.co.z * sz + z, 0.02)     # schiacciato in basso: il sacco pesa

def main():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bm = bmesh.new()
    W, D, ROWS = 1.4, 0.6, 3
    for r in range(ROWS):
        n = 4 if r % 2 == 0 else 3
        for i in range(n):
            x = -W/2 + (i + 0.5) * W / n + (0.08 if r % 2 else 0)
            z = 0.12 + r * 0.26
            bag(bm, x, 0.0, z, 0.36, D, 0.30)
    me = bpy.data.meshes.new("sacchi"); bm.to_mesh(me); bm.free()
    ob = bpy.data.objects.new("sacchi", me)
    bpy.context.collection.objects.link(ob)
    ob.data.materials.append(burlap())
    # pivot al centro dell'ingombro, come i moduli
    ob.location = (0, 0, 0)
    bpy.ops.object.select_all(action='SELECT')
    bpy.context.view_layer.objects.active = ob
    bpy.ops.export_scene.gltf(filepath=f"{OUT}/sandbags.glb", export_format='GLB',
                              use_selection=True, export_apply=True, export_yup=True,
                              export_image_format='AUTO')
    print("RESULT: sandbags", flush=True)

main()
