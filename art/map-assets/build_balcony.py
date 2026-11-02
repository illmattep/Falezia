"""Balcone italiano: lastra in pietra, ringhiera in ferro a barre, mensole.

1,5 × 0,55 m, parapetto 0,95 m. Pivot al centro dell'ingombro, +Z avanti (verso la strada).
È il dettaglio che trasforma una facciata piatta in una casa di paese.

  blender -b -noaudio --factory-startup --python build_balcony.py
"""
import bpy, bmesh, os
from mathutils import Matrix

ROOT = "/home/matteo/Progetti/Falezia"
TEX = f"{ROOT}/art/map-assets/tex"
OUT = f"{ROOT}/Assets/StreamingAssets/map/balconies"
os.makedirs(OUT, exist_ok=True)

W, D, H = 1.5, 0.55, 0.95

def stone():
    m = bpy.data.materials.new("pietra_balcone"); m.use_nodes = True
    nt = m.node_tree; b = nt.nodes["Principled BSDF"]
    img = nt.nodes.new("ShaderNodeTexImage"); img.image = bpy.data.images.load(f"{TEX}/stone_blocks.png")
    nt.links.new(img.outputs["Color"], b.inputs["Base Color"])
    b.inputs["Roughness"].default_value = 0.85
    return m

def iron():
    m = bpy.data.materials.new("ferro_ringhiera"); m.use_nodes = True
    b = m.node_tree.nodes["Principled BSDF"]
    b.inputs["Base Color"].default_value = (0.055, 0.055, 0.06, 1)
    b.inputs["Metallic"].default_value = 0.9
    b.inputs["Roughness"].default_value = 0.45
    return m

def box(bm, sx, sy, sz, loc, bevel=0.008):
    tmp = bmesh.new(); bmesh.ops.create_cube(tmp, size=1.0)
    if bevel > 0:
        bmesh.ops.bevel(tmp, geom=list(tmp.edges) + list(tmp.verts), offset=bevel,
                        segments=2, affect='EDGES', clamp_overlap=True)
    bmesh.ops.scale(tmp, vec=(sx, sy, sz), verts=tmp.verts)
    bmesh.ops.translate(tmp, vec=loc, verts=tmp.verts)
    me = bpy.data.meshes.new("t"); tmp.to_mesh(me); tmp.free()
    bm.from_mesh(me); bpy.data.meshes.remove(me)

def export(name, bm, material):
    me = bpy.data.meshes.new(name); bm.to_mesh(me); bm.free()
    ob = bpy.data.objects.new(name, me)
    bpy.context.collection.objects.link(ob)
    ob.data.materials.append(material)
    bpy.ops.object.select_all(action='DESELECT')
    ob.select_set(True); bpy.context.view_layer.objects.active = ob
    bpy.ops.export_scene.gltf(filepath=f"{OUT}/{name}.glb", export_format='GLB',
                              use_selection=True, export_apply=True, export_yup=True,
                              export_image_format='AUTO')
    print("RESULT:", name, flush=True)

bpy.ops.wm.read_factory_settings(use_empty=True)

# --- lastra (pietra)
bm = bmesh.new()
box(bm, W, D, 0.10, (0, D / 2, 0), 0.012)                    # soletta
box(bm, W, 0.06, 0.16, (0, D - 0.02, 0.06), 0.008)           # fascia frontale
for x in (-W / 2 + 0.18, 0, W / 2 - 0.18):                    # mensole sotto
    box(bm, 0.10, D * 0.75, 0.14, (x, D * 0.42, -0.12), 0.008)
export("balcony_slab", bm, stone())

# --- ringhiera (ferro): due corrimano + barre verticali
bm = bmesh.new()
box(bm, W, 0.04, 0.04, (0, D - 0.04, H), 0.004)              # corrimano
box(bm, W, 0.03, 0.03, (0, D - 0.04, 0.18), 0.003)           # traversa bassa
n = 11
for i in range(n):
    x = -W / 2 + 0.06 + i * (W - 0.12) / (n - 1)
    box(bm, 0.018, 0.018, H - 0.18, (x, D - 0.04, (H + 0.18) / 2), 0.002)
# montanti laterali più spessi
for x in (-W / 2 + 0.03, W / 2 - 0.03):
    box(bm, 0.03, 0.03, H, (x, D - 0.04, H / 2), 0.003)
export("balcony_rail", bm, iron())
