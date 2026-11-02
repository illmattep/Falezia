"""
FALEZIA · art/weapons/standard · 03_uv_bake.py
UV atlas condiviso, usura procedurale e bake in un unico materiale standard_mat.

- UV: smart project per pezzo, poi rimappato in un rettangolo dedicato dell'atlante
  (rettangoli disgiunti -> nessuna sovrapposizione fra pezzi).
- Usura: edge wear (Pointiness) su carrello, graffi da fondina sul lato sinistro,
  vernice delle mire scheggiata, AO.
- Bake: basecolor, roughness, metallic, normal (OpenGL/tangent), AO -> 2048 px.
"""
import bpy, bmesh, math, os, json
from mathutils import Vector

ROOT = "/home/matteo/Progetti/Falezia/art/weapons/standard"
TEX = os.path.join(ROOT, "textures")
os.makedirs(TEX, exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=os.path.join(ROOT, "standard.blend"))
sc = bpy.context.scene
OBJS = [bpy.data.objects[n] for n in ("body", "slide", "trigger", "hammer", "mag")]

RES = 2048
# rettangoli dell'atlante: (u0, v0, larghezza, altezza)
RECTS = {
    "body":    (0.00, 0.00, 0.56, 0.98),
    "slide":   (0.57, 0.00, 0.42, 0.98),
    "mag":     (0.00, 0.00, 0.00, 0.00),   # riempito dopo (sotto lo slide)
}
# layout: body a sinistra, slide a destra, mag/trigger/hammer in basso a sinistra
RECTS = {
    "body":    (0.000, 0.150, 0.560, 0.830),
    "slide":   (0.570, 0.150, 0.420, 0.830),
    "mag":     (0.000, 0.000, 0.230, 0.135),
    "trigger": (0.240, 0.000, 0.100, 0.135),
    "hammer":  (0.350, 0.000, 0.100, 0.135),
}

def srgb_to_lin(c):
    return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4

def lin(hexcol):
    h = hexcol.lstrip('#')
    return tuple(srgb_to_lin(int(h[i:i + 2], 16) / 255.0) for i in (0, 2, 4)) + (1.0,)

# ------------------------------------------------------------------ 1. UV
for ob in OBJS:
    bpy.context.view_layer.objects.active = ob
    for o in OBJS:
        o.select_set(o == ob)
    if not ob.data.uv_layers:
        ob.data.uv_layers.new(name="UVMap")
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.uv.smart_project(angle_limit=math.radians(66), island_margin=0.02)
    bpy.ops.object.mode_set(mode='OBJECT')
    # rimappa nel rettangolo dedicato
    uvl = ob.data.uv_layers.active.data
    us = [d.uv[0] for d in uvl]; vs = [d.uv[1] for d in uvl]
    u0, u1 = min(us), max(us); v0, v1 = min(vs), max(vs)
    du = max(u1 - u0, 1e-6); dv = max(v1 - v0, 1e-6)
    ru, rv, rw, rh = RECTS[ob.name]
    for d in uvl:
        d.uv[0] = ru + (d.uv[0] - u0) / du * (rw * 0.98) + rw * 0.01
        d.uv[1] = rv + (d.uv[1] - v0) / dv * (rh * 0.98) + rh * 0.01
    print("UV", ob.name, len(uvl))

# ------------------------------------------------------------------ 2. usura
def principled(mat):
    return next(n for n in mat.node_tree.nodes if n.type == 'BSDF_PRINCIPLED')

def add_edge_wear(mat, base_hex, wear_hex, rough_base, rough_wear, lo=0.52, hi=0.60):
    """Usura sugli spigoli convessi: colore metallo nudo, rugosita' piu' bassa."""
    nt = mat.node_tree
    b = principled(mat)
    geo = nt.nodes.new('ShaderNodeNewGeometry')
    ramp = nt.nodes.new('ShaderNodeValToRGB')
    ramp.color_ramp.elements[0].position = lo
    ramp.color_ramp.elements[1].position = hi
    ramp.color_ramp.elements[0].color = (0, 0, 0, 1)
    ramp.color_ramp.elements[1].color = (1, 1, 1, 1)
    mix_c = nt.nodes.new('ShaderNodeMixRGB')
    mix_c.inputs['Color1'].default_value = lin(base_hex)
    mix_c.inputs['Color2'].default_value = lin(wear_hex)
    mix_r = nt.nodes.new('ShaderNodeMixRGB')
    mix_r.inputs['Color1'].default_value = (rough_base,) * 3 + (1,)
    mix_r.inputs['Color2'].default_value = (rough_wear,) * 3 + (1,)
    # le facce terminali del loft hanno i vertici tutti sul bordo: il pointiness le
    # legge come spigolo e le fa diventare metallo nudo. Limito l'intensita'.
    lim = nt.nodes.new('ShaderNodeMath')
    lim.operation = 'MULTIPLY'
    lim.inputs[1].default_value = 0.55
    nt.links.new(geo.outputs['Pointiness'], ramp.inputs['Fac'])
    nt.links.new(ramp.outputs['Color'], lim.inputs[0])
    nt.links.new(lim.outputs[0], mix_c.inputs['Fac'])
    nt.links.new(lim.outputs[0], mix_r.inputs['Fac'])
    nt.links.new(mix_c.outputs['Color'], b.inputs['Base Color'])
    nt.links.new(mix_r.outputs['Color'], b.inputs['Roughness'])
    return mix_c

def add_holster_scratches(mat, wear_socket=None):
    """Graffi paralleli sul lato sinistro (X negativo), direzione Y.
    Si sommano all'edge wear invece di sostituirlo."""
    nt = mat.node_tree
    b = principled(mat)
    tc = nt.nodes.new('ShaderNodeTexCoord')
    mapping = nt.nodes.new('ShaderNodeMapping')
    mapping.inputs['Scale'].default_value = (1.0, 0.06, 9.0)
    noise = nt.nodes.new('ShaderNodeTexNoise')
    noise.inputs['Scale'].default_value = 26.0
    noise.inputs['Detail'].default_value = 2.0
    sep = nt.nodes.new('ShaderNodeSeparateXYZ')
    lt = nt.nodes.new('ShaderNodeMath'); lt.operation = 'LESS_THAN'
    lt.inputs[1].default_value = -0.0122
    mul = nt.nodes.new('ShaderNodeMath'); mul.operation = 'MULTIPLY'
    ramp = nt.nodes.new('ShaderNodeValToRGB')
    ramp.color_ramp.elements[0].position = 0.62
    ramp.color_ramp.elements[1].position = 0.66
    mix = nt.nodes.new('ShaderNodeMixRGB')
    mix.inputs['Color1'].default_value = lin("3A3D40")
    mix.inputs['Color2'].default_value = lin("8A8D8F")
    nt.links.new(tc.outputs['Object'], mapping.inputs['Vector'])
    nt.links.new(mapping.outputs['Vector'], noise.inputs['Vector'])
    nt.links.new(tc.outputs['Object'], sep.inputs['Vector'])
    nt.links.new(sep.outputs['X'], lt.inputs[0])
    nt.links.new(noise.outputs['Fac'], ramp.inputs['Fac'])
    nt.links.new(ramp.outputs['Color'], mul.inputs[0])
    nt.links.new(lt.outputs[0], mul.inputs[1])
    nt.links.new(mul.outputs[0], mix.inputs['Fac'])
    if wear_socket is not None:
        nt.links.new(wear_socket, mix.inputs['Color1'])
    nt.links.new(mix.outputs['Color'], b.inputs['Base Color'])
    return mix

def add_paint_patches(mat, prev_socket=None):
    """Chiazze di vernice consumata: noise largo, colore leggermente piu' chiaro."""
    nt = mat.node_tree
    b = principled(mat)
    tc = nt.nodes.new('ShaderNodeTexCoord')
    noise = nt.nodes.new('ShaderNodeTexNoise')
    noise.inputs['Scale'].default_value = 115.0
    noise.inputs['Detail'].default_value = 5.0
    noise.inputs['Roughness'].default_value = 0.62
    ramp = nt.nodes.new('ShaderNodeValToRGB')
    ramp.color_ramp.elements[0].position = 0.60
    ramp.color_ramp.elements[1].position = 0.72
    ramp.color_ramp.elements[0].color = (0, 0, 0, 1)
    ramp.color_ramp.elements[1].color = (0.38, 0.38, 0.38, 1)
    mix = nt.nodes.new('ShaderNodeMixRGB')
    mix.inputs['Color2'].default_value = lin("5A5E62")
    nt.links.new(tc.outputs['Object'], noise.inputs['Vector'])
    nt.links.new(noise.outputs['Fac'], ramp.inputs['Fac'])
    if prev_socket is not None:
        nt.links.new(prev_socket, mix.inputs['Color1'])
    nt.links.new(ramp.outputs['Color'], mix.inputs['Fac'])
    nt.links.new(mix.outputs['Color'], b.inputs['Base Color'])
    return mix

def add_slide_side_groove(mat, prev_bump=None):
    """Linea di separazione orizzontale sul fianco del carrello (come nel riferimento)."""
    nt = mat.node_tree
    b = principled(mat)
    tc = nt.nodes.new('ShaderNodeTexCoord')
    sep = nt.nodes.new('ShaderNodeSeparateXYZ')
    ax = nt.nodes.new('ShaderNodeMath'); ax.operation = 'ABSOLUTE'
    n = nt.nodes.new('ShaderNodeMath'); n.operation = 'SUBTRACT'
    n.inputs[1].default_value = 0.0436
    ab = nt.nodes.new('ShaderNodeMath'); ab.operation = 'ABSOLUTE'
    mr = nt.nodes.new('ShaderNodeMapRange')
    mr.inputs['From Min'].default_value = 0.0
    mr.inputs['From Max'].default_value = 0.0007
    mr.inputs['To Min'].default_value = -1.0
    mr.inputs['To Max'].default_value = 0.0
    gt = nt.nodes.new('ShaderNodeMath'); gt.operation = 'GREATER_THAN'
    gt.inputs[1].default_value = 0.0114
    mul = nt.nodes.new('ShaderNodeMath'); mul.operation = 'MULTIPLY'
    bump = nt.nodes.new('ShaderNodeBump')
    bump.inputs['Strength'].default_value = 0.7
    bump.inputs['Distance'].default_value = 0.0005
    nt.links.new(tc.outputs['Object'], sep.inputs['Vector'])
    nt.links.new(sep.outputs['Z'], n.inputs[0])
    nt.links.new(n.outputs[0], ab.inputs[0])
    nt.links.new(ab.outputs[0], mr.inputs['Value'])
    nt.links.new(sep.outputs['X'], ax.inputs[0])
    nt.links.new(ax.outputs[0], gt.inputs[0])
    nt.links.new(mr.outputs['Result'], mul.inputs[0])
    nt.links.new(gt.outputs[0], mul.inputs[1])
    nt.links.new(mul.outputs[0], bump.inputs['Height'])
    if prev_bump is not None:
        nt.links.new(prev_bump.outputs['Normal'], bump.inputs['Normal'])
    nt.links.new(bump.outputs['Normal'], b.inputs['Normal'])
    return bump

steel = bpy.data.materials["steel_phosphate"]
wear = add_edge_wear(steel, "3A3D40", "8A8D8F", 0.68, 0.48, 0.528, 0.585)
scr = add_holster_scratches(steel, wear.outputs['Color'])
add_paint_patches(steel, scr.outputs['Color'])
add_slide_side_groove(steel, steel.node_tree.nodes.get("serration_bump"))

# mire: vernice bianca scheggiata (~30%)
sw = bpy.data.materials["sight_white"]
nt = sw.node_tree
b = principled(sw)
noise = nt.nodes.new('ShaderNodeTexNoise')
noise.inputs['Scale'].default_value = 900.0
noise.inputs['Detail'].default_value = 3.0
tc = nt.nodes.new('ShaderNodeTexCoord')
ramp = nt.nodes.new('ShaderNodeValToRGB')
ramp.color_ramp.elements[0].position = 0.40
ramp.color_ramp.elements[1].position = 0.44
ramp.color_ramp.elements[0].color = (1, 1, 1, 1)
ramp.color_ramp.elements[1].color = (0, 0, 0, 1)
mix = nt.nodes.new('ShaderNodeMixRGB')
mix.inputs['Color1'].default_value = lin("FFFFFF")
mix.inputs['Color2'].default_value = lin("6E7275")
nt.links.new(tc.outputs['Object'], noise.inputs['Vector'])
nt.links.new(noise.outputs['Fac'], ramp.inputs['Fac'])
nt.links.new(ramp.outputs['Color'], mix.inputs['Fac'])
nt.links.new(mix.outputs['Color'], b.inputs['Base Color'])

# ------------------------------------------------------------------ 3. bake
sc.render.engine = 'CYCLES'
sc.cycles.samples = 32
sc.cycles.use_denoising = False
sc.render.bake.use_selected_to_active = False
sc.render.bake.margin = 8
sc.render.bake.use_clear = True

MATS = [m for m in bpy.data.materials if m.name != "Dots Stroke"]
IMAGES = {}
for name in ("basecolor", "roughness", "metallic", "normal", "ao"):
    img = bpy.data.images.new("T_" + name, RES, RES, alpha=False)
    img.colorspace_settings.name = 'Non-Color' if name != "basecolor" else 'sRGB'
    IMAGES[name] = img

def attach_target(img):
    """Aggiunge (o riusa) il nodo immagine attivo in ogni materiale."""
    for mat in MATS:
        nt = mat.node_tree
        node = nt.nodes.get("BAKE_TARGET")
        if not node:
            node = nt.nodes.new('ShaderNodeTexImage')
            node.name = "BAKE_TARGET"
            node.location = (-900, 600)
        node.image = img
        for n in nt.nodes:
            n.select = False
        node.select = True
        nt.nodes.active = node

def bake(kind, img, **kw):
    attach_target(img)
    for ob in OBJS:
        ob.select_set(True)
    bpy.context.view_layer.objects.active = OBJS[0]
    bpy.ops.object.bake(type=kind, **kw)
    fname = {"DIFFUSE": "standard_basecolor", "ROUGHNESS": "standard_roughness",
             "NORMAL": "standard_normal", "AO": "standard_ao", "EMIT": "standard_metallic"}[kind]
    path = os.path.join(TEX, fname + ".png")
    img.filepath_raw = path
    img.file_format = 'PNG'
    img.save()
    print("BAKED", kind, path)

bake('DIFFUSE', IMAGES["basecolor"], pass_filter={'COLOR'})
bake('ROUGHNESS', IMAGES["roughness"])

# Il bake scrive la roughness come se fosse un colore sRGB: la ricodifico per
# riottenere i valori reali (0.62 non deve diventare 0.345, altrimenti tutto lucido).
def reencode_srgb(img):
    import numpy as np
    n = len(img.pixels)
    a = np.array(img.pixels[:], dtype=np.float32)
    v = np.clip(a, 0.0, 1.0)
    enc = np.where(v <= 0.0031308, v * 12.92, 1.055 * np.power(v, 1.0 / 2.4) - 0.055)
    img.pixels[:] = enc
    img.update()
    img.filepath_raw = os.path.join(TEX, "standard_roughness.png")
    img.file_format = 'PNG'
    img.save()
    print("ROUGHNESS reencoded")

reencode_srgb(IMAGES["roughness"])
bake('NORMAL', IMAGES["normal"])
bake('AO', IMAGES["ao"])

# metallic: bake via emission (nessun tipo dedicato)
saved = {}
for mat in MATS:
    b = principled(mat)
    saved[mat.name] = (b.inputs['Metallic'].default_value,
                       b.inputs['Base Color'].default_value[:])
for mat in MATS:
    b = principled(mat)
    m = b.inputs['Metallic'].default_value
    nt = mat.node_tree
    em = nt.nodes.new('ShaderNodeEmission')
    em.inputs['Color'].default_value = (m, m, m, 1)
    out = next(n for n in nt.nodes if n.type == 'OUTPUT_MATERIAL')
    old_link = out.inputs['Surface'].links[0]
    from_socket = old_link.from_socket
    nt.links.remove(old_link)
    nt.links.new(em.outputs[0], out.inputs['Surface'])
    mat["_em"] = em.name
bake('EMIT', IMAGES["metallic"])
for mat in MATS:
    nt = mat.node_tree
    em = nt.nodes[mat["_em"]]
    out = next(n for n in nt.nodes if n.type == 'OUTPUT_MATERIAL')
    nt.links.remove(out.inputs['Surface'].links[0])
    nt.links.new(principled(mat).outputs['BSDF'], out.inputs['Surface'])
    nt.nodes.remove(em)

# ------------------------------------------------------------------ 4. materiale unico
std = bpy.data.materials.new("standard_mat")
std.use_nodes = True
nt = std.node_tree
for n in list(nt.nodes):
    if n.type != 'OUTPUT_MATERIAL':
        nt.nodes.remove(n)
b = nt.nodes.new('ShaderNodeBsdfPrincipled')
out = nt.nodes.get("Material Output")
nt.links.new(b.outputs['BSDF'], out.inputs['Surface'])

def tex_node(key, cs):
    n = nt.nodes.new('ShaderNodeTexImage')
    n.image = IMAGES[key]
    n.image.colorspace_settings.name = cs
    return n

bc = tex_node("basecolor", 'sRGB'); bc.location = (-600, 300)
ao = tex_node("ao", 'Non-Color'); ao.location = (-600, 450)
ro = tex_node("roughness", 'Non-Color'); ro.location = (-600, 0)
me = tex_node("metallic", 'Non-Color'); me.location = (-600, -300)
nm = tex_node("normal", 'Non-Color'); nm.location = (-600, -600)
# AO al 30%: ombreggia le cavità senza sporcare il colore
aom = nt.nodes.new('ShaderNodeMixRGB')
aom.blend_type = 'MULTIPLY'
aom.inputs['Fac'].default_value = 0.30
nt.links.new(bc.outputs['Color'], aom.inputs['Color1'])
nt.links.new(ao.outputs['Color'], aom.inputs['Color2'])
nt.links.new(aom.outputs['Color'], b.inputs['Base Color'])
nt.links.new(ro.outputs['Color'], b.inputs['Roughness'])
nt.links.new(me.outputs['Color'], b.inputs['Metallic'])
nmap = nt.nodes.new('ShaderNodeNormalMap'); nmap.location = (-300, -600)
nt.links.new(nm.outputs['Color'], nmap.inputs['Color'])
nt.links.new(nmap.outputs['Normal'], b.inputs['Normal'])

for ob in OBJS:
    ob.data.materials.clear()
    ob.data.materials.append(std)

bpy.ops.wm.save_as_mainfile(filepath=os.path.join(ROOT, "standard.blend"))
print("DONE materials:", [o.name + ":" + str(len(o.data.materials)) for o in OBJS])
