"""Targhe stradali in ceramica smaltata: 0,9 × 0,32 m, staffa minima.

  blender -b -noaudio --factory-startup --python build_streets.py
"""
import bpy, os
ROOT = "/home/matteo/Progetti/Falezia"
TEX = f"{ROOT}/art/map-assets/tex/streets"
OUT = f"{ROOT}/Assets/StreamingAssets/map/streets"
os.makedirs(OUT, exist_ok=True)
for i in range(1, 10):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.mesh.primitive_plane_add(size=1)
    p = bpy.context.object
    p.scale = (0.9, 0.32, 1)
    p.rotation_euler[0] = 1.5707963
    bpy.ops.object.transform_apply(scale=True, rotation=True)
    m = bpy.data.materials.new("targa"); m.use_nodes = True
    nt = m.node_tree; b = nt.nodes["Principled BSDF"]
    img = nt.nodes.new("ShaderNodeTexImage")
    img.image = bpy.data.images.load(f"{TEX}/street_{i:02d}.png")
    nt.links.new(img.outputs["Color"], b.inputs["Base Color"])
    b.inputs["Roughness"].default_value = 0.45
    p.data.materials.append(m)
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.export_scene.gltf(filepath=f"{OUT}/street_{i:02d}.glb", export_format='GLB',
                              use_selection=True, export_apply=True, export_yup=True,
                              export_image_format='AUTO')
    print("RESULT:", i, flush=True)
