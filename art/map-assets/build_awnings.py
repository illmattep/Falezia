"""Tenda da sole da bottega: telo a strisce inclinato, balza pendente, due bracci in ferro.

2,8 m di larghezza, 1,2 m di sporgenza, 18° di pendenza. Pivot al centro, +Z avanti.
Stessa grammatica dei moduli Meshy: si aggancia sopra la vetrina.

  blender -b -noaudio --factory-startup --python build_awnings.py
"""
import bpy, bmesh, math, os
from mathutils import Matrix

ROOT = "/home/matteo/Progetti/Falezia"
TEX = f"{ROOT}/art/map-assets/tex"
OUT = f"{ROOT}/Assets/StreamingAssets/map/awnings"
os.makedirs(OUT, exist_ok=True)

W, REACH, ANG, TH = 2.8, 1.2, math.radians(18), 0.05

def fabric(name, tex, tint):
    m = bpy.data.materials.new(name); m.use_nodes = True
    nt = m.node_tree; b = nt.nodes["Principled BSDF"]
    img = nt.nodes.new("ShaderNodeTexImage"); img.image = bpy.data.images.load(f"{TEX}/{tex}.png")
    img.image.colorspace_settings.name = 'sRGB'
    mul = nt.nodes.new("ShaderNodeMixRGB"); mul.blend_type = 'MULTIPLY'
    mul.inputs["Fac"].default_value = 1.0
    mul.inputs["Color2"].default_value = (*tint, 1)
    nt.links.new(img.outputs["Color"], mul.inputs["Color1"])
    nt.links.new(mul.outputs["Color"], b.inputs["Base Color"])
    b.inputs["Roughness"].default_value = 0.92
    return m

def iron():
    m = bpy.data.materials.new("ferro"); m.use_nodes = True
    b = m.node_tree.nodes["Principled BSDF"]
    b.inputs["Base Color"].default_value = (0.05, 0.05, 0.06, 1)
    b.inputs["Metallic"].default_value = 0.9
    b.inputs["Roughness"].default_value = 0.5
    return m

def box(sx, sy, sz, loc, rot_x=0.0):
    bpy.ops.mesh.primitive_cube_add(size=1)
    o = bpy.context.object
    o.scale = (sx / 2, sy / 2, sz / 2)
    if rot_x: o.rotation_euler[0] = rot_x
    o.location = loc
    bpy.ops.object.transform_apply(scale=True, rotation=True)
    return o

def make(name, tex, tint):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    # telo inclinato: parte dal muro in alto e scende verso fuori
    t = box(W, REACH / math.cos(ANG), TH, (0, REACH / 2, -math.tan(ANG) * REACH / 2), rot_x=-ANG)
    t.data.materials.append(fabric("telo", tex, tint))
    # balza che pende dal bordo esterno
    v = box(W, 0.02, 0.22, (0, REACH, -math.tan(ANG) * REACH - 0.11))
    v.data.materials.append(fabric("balza", tex, tint))
    # bracci
    for x in (-W / 2 + 0.08, W / 2 - 0.08):
        a = box(0.04, REACH * 0.95, 0.04, (x, REACH / 2, -math.tan(ANG) * REACH / 2 - 0.10), rot_x=-ANG)
        a.data.materials.append(iron())
    bpy.ops.object.select_all(action='SELECT')
    bpy.context.view_layer.objects.active = bpy.context.selected_objects[0]
    bpy.ops.export_scene.gltf(filepath=f"{OUT}/{name}.glb", export_format='GLB',
                              use_selection=True, export_apply=True, export_yup=True,
                              export_image_format='AUTO')
    print("RESULT:", name, flush=True)

make("awning_01", "canvas_stripes", (1.00, 1.00, 1.00))   # bianco/blu
make("awning_02", "canvas_stripes", (1.00, 0.62, 0.55))   # bianco/rosso
make("awning_03", "canvas_stripes", (0.75, 0.92, 0.75))   # bianco/verde
