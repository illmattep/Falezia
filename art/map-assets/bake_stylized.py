"""Cuoce (Cycles) le texture stilizzate della mappa: intonaco e pietra.

Differenza rispetto alle foto attuali: colore piatto e pulito, variazione lenta, grana fine
appena percettibile, normali morbide. È il linguaggio di Valorant: la forma la danno luce e
geometria, non il rumore della fotografia.

  blender -b -noaudio --factory-startup --python bake_stylized.py
"""
import bpy, os

ROOT = "/home/matteo/Progetti/Falezia"
OUT = f"{ROOT}/Assets/Resources/MapTex"
SIZE = 1024
os.makedirs(OUT, exist_ok=True)


def plane():
    bpy.ops.mesh.primitive_plane_add(size=2)
    ob = bpy.context.object
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.uv.unwrap(method='ANGLE_BASED', margin=0.0)
    bpy.ops.object.mode_set(mode='OBJECT')
    return ob


def bake(ob, mat, name, kind):
    """kind: DIFFUSE (colore), NORMAL, ROUGHNESS"""
    bpy.context.scene.render.engine = 'CYCLES'
    # il bake deve restituire il colore vero, non quello dopo il tonemap della vista
    bpy.context.scene.view_settings.view_transform = 'Standard'
    bpy.context.scene.view_settings.look = 'None'
    bpy.context.scene.display_settings.display_device = 'sRGB'
    bpy.context.scene.cycles.samples = 8
    bpy.context.scene.cycles.device = 'CPU'
    bpy.context.scene.render.bake.use_pass_direct = False
    bpy.context.scene.render.bake.use_pass_indirect = False
    bpy.context.scene.render.bake.use_pass_color = kind == 'DIFFUSE'

    img = bpy.data.images.new(name, SIZE, SIZE, alpha=False, float_buffer=False)
    node = mat.node_tree.nodes.new("ShaderNodeTexImage")
    node.image = img
    mat.node_tree.nodes.active = node

    ob.data.materials.clear()
    ob.data.materials.append(mat)
    bpy.ops.object.select_all(action='DESELECT')
    ob.select_set(True)
    bpy.context.view_layer.objects.active = ob

    bpy.context.scene.render.bake.margin = 8
    bpy.ops.object.bake(type=kind)
    path = f"{OUT}/{name}.png"
    img.filepath_raw = path
    img.file_format = 'PNG'
    img.save()
    print("BAKED", path, flush=True)


def stylized_plaster(color, rough=0.9, grain=0.05):
    m = bpy.data.materials.new("m"); m.use_nodes = True
    nt = m.node_tree; nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    bsdf = nt.nodes.new("ShaderNodeBsdfPrincipled")
    tex = nt.nodes.new("ShaderNodeTexCoord")

    # variazione larga: macchie appena visibili
    n1 = nt.nodes.new("ShaderNodeTexNoise")
    n1.inputs["Scale"].default_value = 3.0
    n1.inputs["Detail"].default_value = 2.0
    n1.inputs["Roughness"].default_value = 0.35
    nt.links.new(tex.outputs["Object"], n1.inputs["Vector"])

    ramp = nt.nodes.new("ShaderNodeValToRGB")
    ramp.color_ramp.elements[0].position = 0.35
    ramp.color_ramp.elements[0].color = (color[0] * 0.93, color[1] * 0.93, color[2] * 0.93, 1)
    ramp.color_ramp.elements[1].position = 0.72
    ramp.color_ramp.elements[1].color = (min(color[0] * 1.05, 1), min(color[1] * 1.05, 1), min(color[2] * 1.05, 1), 1)
    nt.links.new(n1.outputs["Fac"], ramp.inputs["Fac"])

    # grana fine
    n2 = nt.nodes.new("ShaderNodeTexNoise")
    n2.inputs["Scale"].default_value = 120.0
    n2.inputs["Detail"].default_value = 1.0
    nt.links.new(tex.outputs["Object"], n2.inputs["Vector"])
    mix = nt.nodes.new("ShaderNodeMixRGB")
    mix.blend_type = 'MULTIPLY'
    mix.inputs["Fac"].default_value = grain
    nt.links.new(ramp.outputs["Color"], mix.inputs["Color1"])
    nt.links.new(n2.outputs["Color"], mix.inputs["Color2"])
    nt.links.new(mix.outputs["Color"], bsdf.inputs["Base Color"])

    # rilievo: appena accennato, come intonaco tirato a mano
    bump = nt.nodes.new("ShaderNodeBump")
    bump.inputs["Strength"].default_value = 0.08
    nt.links.new(n2.outputs["Fac"], bump.inputs["Height"])
    nt.links.new(bump.outputs["Normal"], bsdf.inputs["Normal"])

    bsdf.inputs["Roughness"].default_value = rough
    bsdf.inputs["Metallic"].default_value = 0.0
    nt.links.new(bsdf.outputs["BSDF"], out.inputs["Surface"])
    return m


def stylized_stone(color, mortar):
    m = bpy.data.materials.new("m"); m.use_nodes = True
    nt = m.node_tree; nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    bsdf = nt.nodes.new("ShaderNodeBsdfPrincipled")
    coord = nt.nodes.new("ShaderNodeTexCoord")

    brick = nt.nodes.new("ShaderNodeTexBrick")
    brick.offset = 0.5
    brick.inputs["Scale"].default_value = 2.2
    brick.inputs["Mortar Size"].default_value = 0.012
    brick.inputs["Brick Width"].default_value = 0.6
    brick.inputs["Row Height"].default_value = 0.28
    brick.inputs["Color1"].default_value = (*color, 1)
    brick.inputs["Color2"].default_value = (color[0] * 0.92, color[1] * 0.92, color[2] * 0.92, 1)
    brick.inputs["Mortar"].default_value = (*mortar, 1)
    nt.links.new(coord.outputs["Object"], brick.inputs["Vector"])

    # grana della pietra, leggera
    noise = nt.nodes.new("ShaderNodeTexNoise")
    noise.inputs["Scale"].default_value = 60.0
    noise.inputs["Detail"].default_value = 2.0
    nt.links.new(coord.outputs["Object"], noise.inputs["Vector"])

    mix = nt.nodes.new("ShaderNodeMixRGB")
    mix.blend_type = 'OVERLAY'
    mix.inputs["Fac"].default_value = 0.10
    nt.links.new(brick.outputs["Color"], mix.inputs["Color1"])
    nt.links.new(noise.outputs["Color"], mix.inputs["Color2"])
    nt.links.new(mix.outputs["Color"], bsdf.inputs["Base Color"])

    bump = nt.nodes.new("ShaderNodeBump")
    bump.inputs["Strength"].default_value = 0.35
    bump.inputs["Distance"].default_value = 0.02
    nt.links.new(brick.outputs["Fac"], bump.inputs["Height"])
    nt.links.new(bump.outputs["Normal"], bsdf.inputs["Normal"])

    bsdf.inputs["Roughness"].default_value = 0.86
    bsdf.inputs["Metallic"].default_value = 0.0
    nt.links.new(bsdf.outputs["BSDF"], out.inputs["Surface"])
    return m


def stylized_roof():
    """Coppi: file di tegole larghe, colore terracotta pieno, solco profondo fra le file.

    Come i sampietrini, la regolarità è il punto: Brick dà file dritte e ombra netta,
    che a distanza leggono come coppi. Le onde sinusoidali venivano strisce.
    """
    m = bpy.data.materials.new("m"); m.use_nodes = True
    nt = m.node_tree; nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    bsdf = nt.nodes.new("ShaderNodeBsdfPrincipled")
    coord = nt.nodes.new("ShaderNodeTexCoord")

    brick = nt.nodes.new("ShaderNodeTexBrick")
    brick.offset = 0.5
    brick.inputs["Scale"].default_value = 9.0
    brick.inputs["Mortar Size"].default_value = 0.03
    brick.inputs["Brick Width"].default_value = 0.42
    brick.inputs["Row Height"].default_value = 0.85
    brick.inputs["Color1"].default_value = (0.58, 0.25, 0.15, 1)
    brick.inputs["Color2"].default_value = (0.50, 0.20, 0.12, 1)
    brick.inputs["Mortar"].default_value = (0.24, 0.10, 0.06, 1)
    nt.links.new(coord.outputs["Object"], brick.inputs["Vector"])

    noise = nt.nodes.new("ShaderNodeTexNoise")
    noise.inputs["Scale"].default_value = 4.0
    noise.inputs["Detail"].default_value = 2.0
    nt.links.new(coord.outputs["Object"], noise.inputs["Vector"])
    mix = nt.nodes.new("ShaderNodeMixRGB")
    mix.blend_type = 'OVERLAY'
    mix.inputs["Fac"].default_value = 0.15
    nt.links.new(brick.outputs["Color"], mix.inputs["Color1"])
    nt.links.new(noise.outputs["Color"], mix.inputs["Color2"])
    nt.links.new(mix.outputs["Color"], bsdf.inputs["Base Color"])

    bump = nt.nodes.new("ShaderNodeBump")
    bump.inputs["Strength"].default_value = 0.7
    bump.inputs["Distance"].default_value = 0.02
    nt.links.new(brick.outputs["Fac"], bump.inputs["Height"])
    nt.links.new(bump.outputs["Normal"], bsdf.inputs["Normal"])

    bsdf.inputs["Roughness"].default_value = 0.85
    nt.links.new(bsdf.outputs["BSDF"], out.inputs["Surface"])
    return m


def stylized_cobble():
    """Sampietrini: ciottoli quadrati arrotondati, fuga scura, colore grigio caldo uniforme.

    Costruiti con Brick e non con Voronoi: la griglia dei sampietrini è regolare, e Brick
    dà fughe nette e pietre della stessa misura — è quella regolarità che rende la strada
    leggibile a distanza.
    """
    m = bpy.data.materials.new("m"); m.use_nodes = True
    nt = m.node_tree; nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    bsdf = nt.nodes.new("ShaderNodeBsdfPrincipled")
    coord = nt.nodes.new("ShaderNodeTexCoord")

    brick = nt.nodes.new("ShaderNodeTexBrick")
    brick.offset = 0.5
    brick.inputs["Scale"].default_value = 14.0
    brick.inputs["Mortar Size"].default_value = 0.035
    brick.inputs["Brick Width"].default_value = 0.5
    brick.inputs["Row Height"].default_value = 0.42
    brick.inputs["Color1"].default_value = (0.56, 0.54, 0.50, 1)
    brick.inputs["Color2"].default_value = (0.48, 0.46, 0.43, 1)
    brick.inputs["Mortar"].default_value = (0.28, 0.27, 0.25, 1)
    nt.links.new(coord.outputs["Object"], brick.inputs["Vector"])

    # macchie larghe di usura, appena accennate
    noise = nt.nodes.new("ShaderNodeTexNoise")
    noise.inputs["Scale"].default_value = 3.0
    noise.inputs["Detail"].default_value = 2.0
    nt.links.new(coord.outputs["Object"], noise.inputs["Vector"])
    mix = nt.nodes.new("ShaderNodeMixRGB")
    mix.blend_type = 'OVERLAY'
    mix.inputs["Fac"].default_value = 0.12
    nt.links.new(brick.outputs["Color"], mix.inputs["Color1"])
    nt.links.new(noise.outputs["Color"], mix.inputs["Color2"])
    nt.links.new(mix.outputs["Color"], bsdf.inputs["Base Color"])

    bump = nt.nodes.new("ShaderNodeBump")
    bump.inputs["Strength"].default_value = 0.55
    bump.inputs["Distance"].default_value = 0.01
    nt.links.new(brick.outputs["Fac"], bump.inputs["Height"])
    nt.links.new(bump.outputs["Normal"], bsdf.inputs["Normal"])

    bsdf.inputs["Roughness"].default_value = 0.92
    nt.links.new(bsdf.outputs["BSDF"], out.inputs["Surface"])
    return m


def stylized_shutter():
    """Persiana: doghe verticali, vernice opaca chiara (la tinta la dà il materiale)."""
    m = bpy.data.materials.new("m"); m.use_nodes = True
    nt = m.node_tree; nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    bsdf = nt.nodes.new("ShaderNodeBsdfPrincipled")
    coord = nt.nodes.new("ShaderNodeTexCoord")

    wave = nt.nodes.new("ShaderNodeTexWave")
    wave.wave_type = 'BANDS'
    wave.bands_direction = 'X'
    wave.inputs["Scale"].default_value = 9.0
    wave.inputs["Distortion"].default_value = 0.0
    nt.links.new(coord.outputs["Object"], wave.inputs["Vector"])

    ramp = nt.nodes.new("ShaderNodeValToRGB")
    ramp.color_ramp.elements[0].color = (0.52, 0.52, 0.50, 1)
    ramp.color_ramp.elements[1].color = (0.78, 0.78, 0.76, 1)
    nt.links.new(wave.outputs["Fac"], ramp.inputs["Fac"])
    nt.links.new(ramp.outputs["Color"], bsdf.inputs["Base Color"])

    bump = nt.nodes.new("ShaderNodeBump")
    bump.inputs["Strength"].default_value = 0.6
    nt.links.new(wave.outputs["Fac"], bump.inputs["Height"])
    nt.links.new(bump.outputs["Normal"], bsdf.inputs["Normal"])

    bsdf.inputs["Roughness"].default_value = 0.75
    nt.links.new(bsdf.outputs["BSDF"], out.inputs["Surface"])
    return m


def stylized_trim():
    """Cornici e modanature: bianco dipinto, liscio, leggermente sporco in basso."""
    m = bpy.data.materials.new("m"); m.use_nodes = True
    nt = m.node_tree; nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    bsdf = nt.nodes.new("ShaderNodeBsdfPrincipled")
    coord = nt.nodes.new("ShaderNodeTexCoord")
    noise = nt.nodes.new("ShaderNodeTexNoise")
    noise.inputs["Scale"].default_value = 8.0
    noise.inputs["Detail"].default_value = 2.0
    nt.links.new(coord.outputs["Object"], noise.inputs["Vector"])
    ramp = nt.nodes.new("ShaderNodeValToRGB")
    ramp.color_ramp.elements[0].color = (0.78, 0.77, 0.74, 1)
    ramp.color_ramp.elements[1].color = (0.94, 0.93, 0.90, 1)
    nt.links.new(noise.outputs["Fac"], ramp.inputs["Fac"])
    nt.links.new(ramp.outputs["Color"], bsdf.inputs["Base Color"])
    bsdf.inputs["Roughness"].default_value = 0.7
    nt.links.new(bsdf.outputs["BSDF"], out.inputs["Surface"])
    return m


def stylized_metal():
    """Ferro battuto: scuro, opaco, con macchie di ruggine larghe."""
    m = bpy.data.materials.new("m"); m.use_nodes = True
    nt = m.node_tree; nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    bsdf = nt.nodes.new("ShaderNodeBsdfPrincipled")
    coord = nt.nodes.new("ShaderNodeTexCoord")
    noise = nt.nodes.new("ShaderNodeTexNoise")
    noise.inputs["Scale"].default_value = 5.0
    noise.inputs["Detail"].default_value = 3.0
    nt.links.new(coord.outputs["Object"], noise.inputs["Vector"])
    ramp = nt.nodes.new("ShaderNodeValToRGB")
    ramp.color_ramp.elements[0].position = 0.42
    ramp.color_ramp.elements[0].color = (0.10, 0.10, 0.11, 1)
    ramp.color_ramp.elements[1].position = 0.78
    ramp.color_ramp.elements[1].color = (0.26, 0.16, 0.11, 1)
    nt.links.new(noise.outputs["Fac"], ramp.inputs["Fac"])
    nt.links.new(ramp.outputs["Color"], bsdf.inputs["Base Color"])
    bsdf.inputs["Roughness"].default_value = 0.62
    bsdf.inputs["Metallic"].default_value = 0.55
    nt.links.new(bsdf.outputs["BSDF"], out.inputs["Surface"])
    return m


def stylized_wood():
    """Legno dipinto: doghe larghe, colore chiaro che accetta la tinta."""
    m = bpy.data.materials.new("m"); m.use_nodes = True
    nt = m.node_tree; nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    bsdf = nt.nodes.new("ShaderNodeBsdfPrincipled")
    coord = nt.nodes.new("ShaderNodeTexCoord")
    brick = nt.nodes.new("ShaderNodeTexBrick")
    brick.inputs["Scale"].default_value = 2.0
    brick.inputs["Mortar Size"].default_value = 0.006
    brick.inputs["Brick Width"].default_value = 0.8
    brick.inputs["Row Height"].default_value = 0.12
    brick.inputs["Color1"].default_value = (0.72, 0.66, 0.56, 1)
    brick.inputs["Color2"].default_value = (0.66, 0.60, 0.50, 1)
    brick.inputs["Mortar"].default_value = (0.40, 0.36, 0.30, 1)
    nt.links.new(coord.outputs["Object"], brick.inputs["Vector"])
    nt.links.new(brick.outputs["Color"], bsdf.inputs["Base Color"])
    bump = nt.nodes.new("ShaderNodeBump")
    bump.inputs["Strength"].default_value = 0.3
    nt.links.new(brick.outputs["Fac"], bump.inputs["Height"])
    nt.links.new(bump.outputs["Normal"], bsdf.inputs["Normal"])
    bsdf.inputs["Roughness"].default_value = 0.78
    nt.links.new(bsdf.outputs["BSDF"], out.inputs["Surface"])
    return m


def main():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    ob = plane()

    jobs = [
        ("styl_plaster", stylized_plaster((0.74, 0.67, 0.55)), None),
        ("styl_plaster_ochre", stylized_plaster((0.72, 0.53, 0.30), 0.88), None),
        ("styl_stone", stylized_stone((0.64, 0.60, 0.53), (0.44, 0.42, 0.39)), None),
        ("styl_roof", stylized_roof(), None),
        ("styl_street", stylized_cobble(), None),
        ("styl_shutter", stylized_shutter(), None),
        ("styl_trim", stylized_trim(), None),
        ("styl_metal", stylized_metal(), None),
        ("styl_wood", stylized_wood(), None),
    ]
    for name, m, _ in jobs:
        bake(ob, m, name + "_c", 'DIFFUSE')
        bake(ob, m, name + "_n", 'NORMAL')
        bake(ob, m, name + "_r", 'ROUGHNESS')

main()
