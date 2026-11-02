"""Stemmi delle due fazioni: targa dipinta 1,4 × 1,4 m con ancora (Porto) e torre (Forte)."""
import bpy, os
ROOT="/home/matteo/Progetti/Falezia"; TEX=f"{ROOT}/art/map-assets/tex/marks"
OUT=f"{ROOT}/Assets/StreamingAssets/map/marks"; os.makedirs(OUT, exist_ok=True)
for name in ("mark_nord","mark_sud"):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.mesh.primitive_plane_add(size=1)
    p=bpy.context.object; p.scale=(1.4,1.4,1); p.rotation_euler[0]=1.5707963
    bpy.ops.object.transform_apply(scale=True, rotation=True)
    m=bpy.data.materials.new("stemma"); m.use_nodes=True
    nt=m.node_tree; b=nt.nodes["Principled BSDF"]
    img=nt.nodes.new("ShaderNodeTexImage"); img.image=bpy.data.images.load(f"{TEX}/{name}.png")
    nt.links.new(img.outputs["Color"], b.inputs["Base Color"]); b.inputs["Roughness"].default_value=0.8
    p.data.materials.append(m)
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.export_scene.gltf(filepath=f"{OUT}/{name}.glb", export_format='GLB',
                              use_selection=True, export_apply=True, export_yup=True, export_image_format='AUTO')
    print("RESULT:", name, flush=True)
