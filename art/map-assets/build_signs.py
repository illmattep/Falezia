"""Insegne dipinte: pannello di legno + staffa in ferro, con la texture dell'insegna.

Sei varianti (trattoria, pescheria, falegnameria, osteria, cartoleria, forno), 2,0 × 0,55 m.
Pivot al centro dell'ingombro, come i moduli Meshy; +Z avanti.

  blender -b -noaudio --factory-startup --python build_signs.py
"""
import bpy, bmesh, os
from mathutils import Matrix

ROOT = "/home/matteo/Progetti/Falezia"
TEX = f"{ROOT}/art/map-assets/tex/signs"
OUT = f"{ROOT}/Assets/StreamingAssets/map/signs"
os.makedirs(OUT, exist_ok=True)

W, H, D = 2.0, 0.55, 0.07

def board(name, tex):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.mesh.primitive_cube_add(size=1)
    b = bpy.context.object
    b.scale = (W / 2, D / 2, H / 2)
    bpy.ops.object.transform_apply(scale=True)
    b.name = "pannello"

    # materiale con la texture dell'insegna sulla faccia +Z
    m = bpy.data.materials.new("insegna"); m.use_nodes = True
    nt = m.node_tree; bsdf = nt.nodes["Principled BSDF"]
    img = nt.nodes.new("ShaderNodeTexImage")
    img.image = bpy.data.images.load(f"{TEX}/{tex}.png")
    nt.links.new(img.outputs["Color"], bsdf.inputs["Base Color"])
    bsdf.inputs["Roughness"].default_value = 0.75
    b.data.materials.append(m)

    # staffa in ferro sotto l'insegna
    bpy.ops.mesh.primitive_cube_add(size=1)
    s = bpy.context.object
    s.scale = (0.06, 0.30, 0.06)
    s.location = (0, -0.18, -H / 2 + 0.16)
    bpy.ops.object.transform_apply(scale=True)
    fm = bpy.data.materials.new("ferro"); fm.use_nodes = True
    fb = fm.node_tree.nodes["Principled BSDF"]
    fb.inputs["Base Color"].default_value = (0.06, 0.06, 0.07, 1)
    fb.inputs["Metallic"].default_value = 0.85
    fb.inputs["Roughness"].default_value = 0.45
    s.data.materials.append(fm)

    bpy.ops.object.select_all(action='SELECT')
    bpy.context.view_layer.objects.active = b
    bpy.ops.export_scene.gltf(filepath=f"{OUT}/{name}.glb", export_format='GLB',
                              use_selection=True, export_apply=True, export_yup=True,
                              export_image_format='AUTO')
    print("RESULT:", name, flush=True)

for i in range(1, 7):
    board(f"sign_{i:02d}", f"sign_{i:02d}")
