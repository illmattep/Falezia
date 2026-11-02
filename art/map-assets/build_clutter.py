"""Casse, barili e cassette del pesce: arredi minori che riempiono i bordi strada.

Geometria semplice e sicura (scatole smussate e cilindri), texture di legno stilizzata.
Pivot al centro dell'ingombro, come gli altri moduli.

  blender -b -noaudio --factory-startup --python build_clutter.py
"""
import bpy, bmesh, os, math
from mathutils import Matrix

ROOT = "/home/matteo/Progetti/Falezia"
TEX = f"{ROOT}/art/map-assets/tex"
OUT = f"{ROOT}/Assets/StreamingAssets/map/clutter"
os.makedirs(OUT, exist_ok=True)

def wood_mat():
    m = bpy.data.materials.new("legno"); m.use_nodes = True
    nt = m.node_tree; b = nt.nodes["Principled BSDF"]
    img = nt.nodes.new("ShaderNodeTexImage"); img.image = bpy.data.images.load(f"{TEX}/wood_planks.png")
    nt.links.new(img.outputs["Color"], b.inputs["Base Color"])
    b.inputs["Roughness"].default_value = 0.85
    return m

def bm_new():
    return bmesh.new()

def add_box(bm, sx, sy, sz, loc=(0,0,0), bevel=0.02):
    start = len(bm.verts)
    bmesh.ops.create_cube(bm, size=1.0)
    bm.verts.ensure_lookup_table()
    verts = bm.verts[start:]
    edges = set()
    for v in verts:
        for e in v.link_edges: edges.add(e)
    if bevel > 0:
        bmesh.ops.bevel(bm, geom=list(edges)+list(verts), offset=bevel, segments=2,
                        affect='EDGES', clamp_overlap=True)
    bm.verts.ensure_lookup_table()
    for v in bm.verts[start:]:
        v.co.x = v.co.x * sx + loc[0]
        v.co.y = v.co.y * sy + loc[1]
        v.co.z = v.co.z * sz + loc[2]

def add_cyl(bm, r, h, loc=(0,0,0), seg=14):
    bmesh.ops.create_cone(bm, cap_ends=True, segments=seg, radius1=r, radius2=r, depth=h,
                          matrix=Matrix.Translation(loc))

def export(name, bm):
    me = bpy.data.meshes.new(name); bm.to_mesh(me); bm.free()
    ob = bpy.data.objects.new(name, me)
    bpy.context.collection.objects.link(ob)
    ob.data.materials.append(wood_mat())
    bpy.ops.object.select_all(action='DESELECT')
    ob.select_set(True)
    bpy.context.view_layer.objects.active = ob
    bpy.ops.export_scene.gltf(filepath=f"{OUT}/{name}.glb", export_format='GLB',
                              use_selection=True, export_apply=True, export_yup=True,
                              export_image_format='AUTO')
    print("RESULT:", name, flush=True)

bpy.ops.wm.read_factory_settings(use_empty=True)

# cassa 0,85 · doghe + cantonali
bm = bm_new()
add_box(bm, 0.85, 0.85, 0.75, (0, 0, 0.375))
for sx in (-0.40, 0.40):
    for sy in (-0.40, 0.40):
        add_box(bm, 0.06, 0.06, 0.78, (sx, sy, 0.39), 0.008)
export("crate", bm)

# barile 0,55 × 0,9 · doghe + tre cerchi
bm = bm_new()
add_cyl(bm, 0.275, 0.9, (0, 0, 0.45))
for z in (0.15, 0.45, 0.75):
    add_cyl(bm, 0.29, 0.05, (0, 0, z))
export("barrel", bm)

# cassetta del pesce 0,8 × 0,5 × 0,28
bm = bm_new()
add_box(bm, 0.80, 0.50, 0.28, (0, 0, 0.14))
add_box(bm, 0.84, 0.54, 0.04, (0, 0, 0.26), 0.006)
export("fishbox", bm)
