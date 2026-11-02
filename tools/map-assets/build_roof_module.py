"""Modulo tetto per il capannone: due falde in lamiera grecata, colmo, capriate.

Costruzione robusta: ogni falda è una scatola sottile ruotata attorno a X e appoggiata fra
gronda e colmo. Niente facce costruite a mano con l'ordine dei vertici (era lì il bug che
faceva leggere il tetto come una lastra piatta).

Impronta 6,4 × 7,0 m (+ 0,5 m di sporto), colmo a 1,6 m. Pivot al centro dell'ingombro.

  blender -b -noaudio --factory-startup --python build_roof_module.py
"""
import bpy, bmesh, math, os
from mathutils import Matrix

ROOT = "/home/matteo/Progetti/Falezia"
TEX = f"{ROOT}/art/map-assets/tex"
OUT = f"{ROOT}/Assets/StreamingAssets/map/modules/mod_09_roof.glb"

W, D, RISE, EAVE, THICK = 6.4, 7.0, 1.5, 0.6, 0.22
HALF = D / 2 + EAVE
PITCH = math.atan2(RISE, HALF)
SLOPE = math.hypot(HALF, RISE)

def material(name, rough, metal):
    m = bpy.data.materials.new(name); m.use_nodes = True
    nt = m.node_tree; b = nt.nodes["Principled BSDF"]
    b.inputs["Roughness"].default_value = rough
    b.inputs["Metallic"].default_value = metal
    alb = nt.nodes.new("ShaderNodeTexImage"); alb.image = bpy.data.images.load(f"{TEX}/{name}.png")
    nt.links.new(alb.outputs["Color"], b.inputs["Base Color"])
    n = nt.nodes.new("ShaderNodeTexImage"); n.image = bpy.data.images.load(f"{TEX}/{name}_n.png")
    n.image.colorspace_settings.name = 'Non-Color'
    nm = nt.nodes.new("ShaderNodeNormalMap")
    nt.links.new(n.outputs["Color"], nm.inputs["Color"])
    nt.links.new(nm.outputs["Normal"], b.inputs["Normal"])
    return m

def piece(bm, sx, sy, sz, loc, rot_x=0.0):
    """Scatola sx×sy×sz, ruotata di rot_x attorno a X e traslata: geometria sempre coerente."""
    tmp = bmesh.new()
    bmesh.ops.create_cube(tmp, size=1.0)
    bmesh.ops.scale(tmp, vec=(sx, sy, sz), verts=tmp.verts)
    if rot_x:
        bmesh.ops.rotate(tmp, cent=(0, 0, 0), matrix=Matrix.Rotation(rot_x, 3, 'X'), verts=tmp.verts)
    bmesh.ops.translate(tmp, vec=loc, verts=tmp.verts)
    me = bpy.data.meshes.new("t"); tmp.to_mesh(me); tmp.free()
    bm.from_mesh(me); bpy.data.meshes.remove(me)

def main():
    bpy.ops.wm.read_factory_settings(use_empty=True)

    bm = bmesh.new()
    # due falde: dal colmo (z=0, y=RISE) alla gronda (z=±HALF, y=0)
    # Blender è Z-up: la lunghezza della falda sta su Y, lo spessore su Z, la quota su Z.
    for side in (1, -1):
        piece(bm, W, SLOPE, THICK,
              (0.0, side * HALF / 2, RISE / 2),
              rot_x=-side * PITCH)
    # colmo: tegola di colmo che copre la giunzione
    piece(bm, W + 0.1, 0.34, 0.18, (0.0, 0.0, RISE + 0.09))
    me = bpy.data.meshes.new("tetto"); bm.to_mesh(me); bm.free()

    uv = me.uv_layers.new(name="UVMap")
    for poly in me.polygons:
        n = poly.normal
        ax = max(range(3), key=lambda i: abs(n[i]))
        for li in poly.loop_indices:
            co = me.vertices[me.loops[li].vertex_index].co
            u, v = (co.y, co.z) if ax == 0 else ((co.x, co.z) if ax == 1 else (co.x, co.y))
            uv.data[li].uv = (u / 2.5, v / 2.5)

    ob = bpy.data.objects.new("tetto", me)
    bpy.context.collection.objects.link(ob)
    ob.data.materials.append(material("metal_ribbed", 0.55, 0.6))

    # capriate + arcarecci a vista sotto lo sporto
    bm2 = bmesh.new()
    for i in range(4):
        x = -W / 2 + 0.7 + i * (W - 1.4) / 3
        piece(bm2, 0.18, D - 0.4, 0.18, (x, 0.0, RISE - 0.32))
    for side in (1, -1):
        piece(bm2, W + 0.8, 0.14, 0.14, (0.0, side * (HALF - 0.25), 0.18))
    me2 = bpy.data.meshes.new("capriate"); bm2.to_mesh(me2); bm2.free()
    uv2 = me2.uv_layers.new(name="UVMap")
    for poly in me2.polygons:
        for li in poly.loop_indices:
            co = me2.vertices[me2.loops[li].vertex_index].co
            uv2.data[li].uv = (co.x / 2.5, co.y / 2.5)
    ob2 = bpy.data.objects.new("capriate", me2)
    bpy.context.collection.objects.link(ob2)
    ob2.data.materials.append(material("wood_planks", 0.8, 0.0))

    bpy.ops.object.select_all(action='DESELECT')
    ob.select_set(True); ob2.select_set(True)
    bpy.context.view_layer.objects.active = ob
    bpy.ops.export_scene.gltf(filepath=OUT, export_format='GLB', use_selection=True,
                              export_apply=True, export_yup=True, export_image_format='AUTO')
    print("RESULT:", OUT)

main()
