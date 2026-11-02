"""Manifesti murali: foglio 0,9 × 1,2 m, appena staccato dal muro, con la texture stampata.

Le zone della mappa hanno ciascuna i suoi avvisi: mercato, festa del santo, annunci di lavoro.
  blender -b -noaudio --factory-startup --python build_posters.py
"""
import bpy, os
ROOT = "/home/matteo/Progetti/Falezia"
TEX = f"{ROOT}/art/map-assets/tex/posters"
OUT = f"{ROOT}/Assets/StreamingAssets/map/posters"
os.makedirs(OUT, exist_ok=True)

for i in range(1, 4):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.mesh.primitive_plane_add(size=1)
    p = bpy.context.object
    p.scale = (0.9, 1.2, 1)
    p.rotation_euler[0] = 1.5707963          # in piedi, faccia verso +Y (poi ruotata in scena)
    bpy.ops.object.transform_apply(scale=True, rotation=True)

    m = bpy.data.materials.new("manifesto"); m.use_nodes = True
    nt = m.node_tree; b = nt.nodes["Principled BSDF"]
    img = nt.nodes.new("ShaderNodeTexImage")
    img.image = bpy.data.images.load(f"{TEX}/poster_{i:02d}.png")
    nt.links.new(img.outputs["Color"], b.inputs["Base Color"])
    b.inputs["Roughness"].default_value = 0.9
    p.data.materials.append(m)

    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.export_scene.gltf(filepath=f"{OUT}/poster_{i:02d}.glb", export_format='GLB',
                              use_selection=True, export_apply=True, export_yup=True,
                              export_image_format='AUTO')
    print("RESULT:", i, flush=True)
