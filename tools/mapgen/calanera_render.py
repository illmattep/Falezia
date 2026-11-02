"""Anteprima di Calanera in Blender — un proxy del look del gioco.

Replica in nodi quello che fa lo shader di MapView.cs:
  * facciate con finestre procedurali (3.4 × 3.2 m, dal primo piano in su),
    vetro con riflesso del cielo, persiane su una finestra su quattro, cornice,
  * zoccolo scuro alla base,
  * tetti in coppi con file sfalsate.
Serve solo a giudicare l'aspetto: la verità resta MapView.cs.
"""
import bpy, bmesh, json, math, os
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
OUT = Path(__file__).resolve().parent / "review"
OUT.mkdir(exist_ok=True)
D = json.load(open(ROOT / "game" / "maps" / "calanera.json"))
K = D['kinds']

LOOK = {
 'street': ((0.60,0.58,0.54), 0.95), 'plaster': ((0.85,0.66,0.48), 0.92),
 'plaster_ochre': ((0.79,0.54,0.29), 0.92), 'stone': ((0.79,0.75,0.66), 0.88),
 'rock': ((0.49,0.47,0.43), 0.95), 'concrete': ((0.55,0.53,0.50), 0.90),
 'steel': ((0.33,0.35,0.36), 0.55), 'wood': ((0.54,0.42,0.29), 0.85),
 'cover': ((0.71,0.67,0.60), 0.90), 'stairs': ((0.74,0.70,0.61), 0.88),
 'roof': ((0.61,0.36,0.25), 0.85), 'water': ((0.31,0.40,0.45), 0.20),
 'objective': ((0.95,0.76,0.19), 0.6), 'team_north': ((0.18,0.61,0.90), 0.6),
 'team_south': ((0.91,0.39,0.18), 0.6),
}
FACADES = ('plaster', 'plaster_ochre', 'stone', 'concrete')

for ob in list(bpy.data.objects):
    bpy.data.objects.remove(ob, do_unlink=True)


def s2l(c):
    """sRGB -> lineare: il gioco converte l'esadecimale nello shader, Blender no."""
    return tuple(v / 12.92 if v <= 0.04045 else ((v + 0.055) / 1.055) ** 2.4 for v in c)


def mathn(nt, op, a, b=None):
    n = nt.nodes.new('ShaderNodeMath'); n.operation = op
    for i, v in enumerate((a, b)):
        if v is None:
            continue
        if isinstance(v, (int, float)):
            n.inputs[i].default_value = float(v)
        else:
            nt.links.new(v, n.inputs[i])
    return n.outputs[0]


def mix_rgb(nt, fac, c1, c2):
    n = nt.nodes.new('ShaderNodeMixRGB')
    if isinstance(fac, (int, float)):
        n.inputs['Factor'].default_value = fac
    else:
        nt.links.new(fac, n.inputs['Factor'])
    n.inputs['Color1'].default_value = (*c1, 1) if len(c1) == 3 else c1
    n.inputs['Color2'].default_value = (*c2, 1) if len(c2) == 3 else c2
    return n.outputs['Color']


def facade_material(name, col, rough):
    """Parete con finestre, come il ramo windows_mode dello shader."""
    m = bpy.data.materials.new(name); m.use_nodes = True
    nt = m.node_tree
    for n in list(nt.nodes):
        if n.type != 'OUTPUT_MATERIAL':
            nt.nodes.remove(n)
    out = [n for n in nt.nodes if n.type == 'OUTPUT_MATERIAL'][0]

    geo = nt.nodes.new('ShaderNodeNewGeometry')
    sp = nt.nodes.new('ShaderNodeSeparateXYZ'); nt.links.new(geo.outputs['Position'], sp.inputs[0])
    sn = nt.nodes.new('ShaderNodeSeparateXYZ'); nt.links.new(geo.outputs['Normal'], sn.inputs[0])

    absnx = mathn(nt, 'ABSOLUTE', sn.outputs['X'])
    absnz = mathn(nt, 'ABSOLUTE', sn.outputs['Z'])
    isx = mathn(nt, 'GREATER_THAN', absnx, 0.5)          # parete che guarda est/ovest
    vert = mathn(nt, 'LESS_THAN', absnz, 0.5)            # parete verticale

    # parete est/ovest -> la coordinata lungo il muro è Y; nord/sud -> X
    u = mathn(nt, 'ADD',
              mathn(nt, 'MULTIPLY', sp.outputs['Y'], isx),
              mathn(nt, 'MULTIPLY', sp.outputs['X'], mathn(nt, 'SUBTRACT', 1.0, isx)))

    at = nt.nodes.new('ShaderNodeAttribute'); at.attribute_name = 'topY'
    sc_ = nt.nodes.new('ShaderNodeSeparateColor'); nt.links.new(at.outputs['Color'], sc_.inputs[0])
    topy = mathn(nt, 'MULTIPLY', sc_.outputs['Red'], 64.0)
    usable = mathn(nt, 'MAXIMUM', mathn(nt, 'SUBTRACT', topy, 3.35), 2.40)
    rowsN = mathn(nt, 'MAXIMUM', mathn(nt, 'FLOOR', mathn(nt, 'ADD', mathn(nt, 'DIVIDE', usable, 3.2), 0.5)), 1.0)
    pitch = mathn(nt, 'MAXIMUM', mathn(nt, 'DIVIDE', usable, rowsN), 2.2)

    cu = mathn(nt, 'DIVIDE', u, 3.4)
    cv = mathn(nt, 'DIVIDE', mathn(nt, 'SUBTRACT', sp.outputs['Z'], 2.9), pitch)
    fu = mathn(nt, 'FRACT', cu)
    fv = mathn(nt, 'FRACT', cv)

    wu = mathn(nt, 'MULTIPLY', mathn(nt, 'GREATER_THAN', fu, 0.34), mathn(nt, 'LESS_THAN', fu, 0.66))
    wv = mathn(nt, 'MULTIPLY', mathn(nt, 'GREATER_THAN', fv, 0.30), mathn(nt, 'LESS_THAN', fv, 0.80))
    rows = mathn(nt, 'MULTIPLY', mathn(nt, 'GREATER_THAN', cv, 0.0), mathn(nt, 'LESS_THAN', cv, mathn(nt, 'SUBTRACT', rowsN, 0.5)))
    win = mathn(nt, 'MULTIPLY', mathn(nt, 'MULTIPLY', wu, wv), mathn(nt, 'MULTIPLY', rows, vert))

    # cornice della finestra: fascia appena più scura
    frx = mathn(nt, 'MULTIPLY', mathn(nt, 'GREATER_THAN', fu, 0.29), mathn(nt, 'LESS_THAN', fu, 0.71))
    fry = mathn(nt, 'MULTIPLY', mathn(nt, 'GREATER_THAN', fv, 0.24), mathn(nt, 'LESS_THAN', fv, 0.86))
    frame = mathn(nt, 'MULTIPLY', mathn(nt, 'MULTIPLY', frx, fry),
                  mathn(nt, 'MULTIPLY', mathn(nt, 'SUBTRACT', 1.0, win), mathn(nt, 'MULTIPLY', rows, vert)))

    # zoccolo: la parete si scurisce sotto i 30-85 cm
    mr = nt.nodes.new('ShaderNodeMapRange')
    mr.interpolation_type = 'SMOOTHSTEP'; mr.clamp = True
    nt.links.new(sp.outputs['Z'], mr.inputs['Value'])
    mr.inputs['From Min'].default_value = 0.30
    mr.inputs['From Max'].default_value = 0.85
    mr.inputs['To Min'].default_value = 1.0
    mr.inputs['To Max'].default_value = 0.0
    plinth = mathn(nt, 'MULTIPLY', vert, mr.outputs['Result'])
    shade = mathn(nt, 'SUBTRACT', 1.0, mathn(nt, 'MULTIPLY', plinth, 0.38))
    shade = mathn(nt, 'MULTIPLY', shade, mathn(nt, 'SUBTRACT', 1.0, mathn(nt, 'MULTIPLY', frame, 0.10)))

    # persiana: una finestra su quattro, doghe verticali
    comb = nt.nodes.new('ShaderNodeCombineXYZ')
    nt.links.new(mathn(nt, 'FLOOR', cu), comb.inputs['X'])
    nt.links.new(mathn(nt, 'FLOOR', cv), comb.inputs['Y'])
    wn = nt.nodes.new('ShaderNodeTexWhiteNoise'); wn.noise_dimensions = '3D'
    nt.links.new(comb.outputs[0], wn.inputs['Vector'])
    shutter = mathn(nt, 'GREATER_THAN', wn.outputs['Value'], 0.72)

    wall = nt.nodes.new('ShaderNodeBsdfPrincipled')
    wall.inputs['Base Color'].default_value = (*col, 1)
    wall.inputs['Roughness'].default_value = rough
    wall.inputs['Specular IOR Level'].default_value = 0.35
    vm = nt.nodes.new('ShaderNodeVectorMath'); vm.operation = 'MULTIPLY'
    vm.inputs[0].default_value = col
    nt.links.new(shade, vm.inputs[1])

    # terrazze e porticati: lastroni da 80 cm sulle superfici orizzontali alte
    tmap = nt.nodes.new('ShaderNodeMapRange'); tmap.interpolation_type = 'SMOOTHSTEP'; tmap.clamp = True
    nt.links.new(sp.outputs['Z'], tmap.inputs['Value'])
    tmap.inputs['From Min'].default_value = 2.20
    tmap.inputs['From Max'].default_value = 2.60
    tmap.inputs['To Min'].default_value = 0.0
    tmap.inputs['To Max'].default_value = 1.0
    terrace = mathn(nt, 'MULTIPLY', mathn(nt, 'SUBTRACT', 1.0, vert), tmap.outputs['Result'])

    tmapx = nt.nodes.new('ShaderNodeMapping')
    tmapx.inputs['Scale'].default_value = (1/0.8, 1/0.8, 1/0.8)
    nt.links.new(geo.outputs['Position'], tmapx.inputs['Vector'])
    slab = nt.nodes.new('ShaderNodeTexBrick')
    slab.offset = 0.5
    slab.inputs['Scale'].default_value = 1.0
    slab.inputs['Mortar Size'].default_value = 0.025
    slab.inputs['Brick Width'].default_value = 1.0
    slab.inputs['Row Height'].default_value = 1.0
    slab.inputs['Color1'].default_value = (1.0, 1.0, 1.0, 1)
    slab.inputs['Color2'].default_value = (0.93, 0.93, 0.93, 1)
    slab.inputs['Mortar'].default_value = (0.62, 0.62, 0.62, 1)
    nt.links.new(tmapx.outputs['Vector'], slab.inputs['Vector'])
    tcol = nt.nodes.new('ShaderNodeMixRGB'); tcol.blend_type = 'MULTIPLY'
    nt.links.new(terrace, tcol.inputs['Factor'])
    nt.links.new(vm.outputs[0], tcol.inputs['Color1'])
    nt.links.new(slab.outputs['Color'], tcol.inputs['Color2'])
    nt.links.new(tcol.outputs['Color'], wall.inputs['Base Color'])

    # vetro: riflesso del cielo in alto, ombra in basso
    grad = nt.nodes.new('ShaderNodeMapRange')
    grad.interpolation_type = 'SMOOTHSTEP'; grad.clamp = True
    nt.links.new(fv, grad.inputs['Value'])
    grad.inputs['From Min'].default_value = 0.34
    grad.inputs['From Max'].default_value = 0.80
    grad.inputs['To Min'].default_value = 0.0
    grad.inputs['To Max'].default_value = 0.85
    gloss = nt.nodes.new('ShaderNodeBsdfPrincipled')
    nt.links.new(mix_rgb(nt, grad.outputs['Result'], (0.018, 0.024, 0.032), (0.105, 0.145, 0.185)),
                 gloss.inputs['Base Color'])
    gloss.inputs['Roughness'].default_value = 0.16
    gloss.inputs['Specular IOR Level'].default_value = 0.55

    wood = nt.nodes.new('ShaderNodeBsdfPrincipled')
    nt.links.new(mix_rgb(nt, mathn(nt, 'GREATER_THAN', mathn(nt, 'FRACT', mathn(nt, 'MULTIPLY', fu, 7.0)), 0.5),
                         (0.150, 0.092, 0.055), (0.205, 0.125, 0.070)),
                 wood.inputs['Base Color'])
    wood.inputs['Roughness'].default_value = 0.74

    m1 = nt.nodes.new('ShaderNodeMixShader')
    nt.links.new(mathn(nt, 'MULTIPLY', win, mathn(nt, 'SUBTRACT', 1.0, shutter)), m1.inputs[0])
    nt.links.new(wall.outputs[0], m1.inputs[1])
    nt.links.new(gloss.outputs[0], m1.inputs[2])

    m2 = nt.nodes.new('ShaderNodeMixShader')
    nt.links.new(mathn(nt, 'MULTIPLY', win, shutter), m2.inputs[0])
    nt.links.new(m1.outputs[0], m2.inputs[1])
    nt.links.new(wood.outputs[0], m2.inputs[2])
    nt.links.new(m2.outputs[0], out.inputs['Surface'])
    return m


def roof_material(name):
    """Coppi: file da 34 × 42 cm sfalsate di mezza fila."""
    m = bpy.data.materials.new(name); m.use_nodes = True
    nt = m.node_tree
    b = nt.nodes['Principled BSDF']
    b.inputs['Roughness'].default_value = 0.93
    tc = nt.nodes.new('ShaderNodeTexCoord')
    mp = nt.nodes.new('ShaderNodeMapping')
    mp.inputs['Scale'].default_value = (1/0.34, 1/0.42, 1/0.42)
    br = nt.nodes.new('ShaderNodeTexBrick')
    br.offset = 0.5
    br.inputs['Scale'].default_value = 1.0
    br.inputs['Mortar Size'].default_value = 0.05
    br.inputs['Brick Width'].default_value = 1.0
    br.inputs['Row Height'].default_value = 1.0
    br.inputs['Color1'].default_value = (*s2l((0.60, 0.29, 0.17)), 1)
    br.inputs['Color2'].default_value = (*s2l((0.44, 0.20, 0.12)), 1)
    br.inputs['Mortar'].default_value = (*s2l((0.22, 0.11, 0.07)), 1)
    nt.links.new(tc.outputs['Object'], mp.inputs['Vector'])
    nt.links.new(mp.outputs['Vector'], br.inputs['Vector'])
    nt.links.new(br.outputs['Color'], b.inputs['Base Color'])
    return m


mats = {}
for name, (col, rough) in LOOK.items():
    col = s2l(col)
    if name == 'roof':
        mats[name] = roof_material(name)
    elif name in FACADES:
        mats[name] = facade_material(name, col, rough)
    else:
        m = bpy.data.materials.new(name); m.use_nodes = True
        b = m.node_tree.nodes['Principled BSDF']
        b.inputs['Base Color'].default_value = (*col, 1)
        b.inputs['Roughness'].default_value = rough
        if name == 'water':
            b.inputs['Specular IOR Level'].default_value = 0.6
        mats[name] = m

items = D['boxes'] + D.get('decor', [])
bm = bmesh.new()
for x, y, z, w, h, dd, kind in items:
    bmesh.ops.create_cube(bm, size=1.0)
    vs = bm.verts[-8:]
    # Godot (X est, Y su, Z sud) -> Blender (X, -Z, Y): la Y di Blender è -Z del gioco
    bmesh.ops.scale(bm, vec=(w, dd, h), verts=vs)
    bmesh.ops.translate(bm, vec=(x + w/2, -(z + dd/2), y + h/2), verts=vs)

me = bpy.data.meshes.new('map'); bm.to_mesh(me); bm.free()
ob = bpy.data.objects.new('map', me); bpy.context.scene.collection.objects.link(ob)
for name in LOOK:
    me.materials.append(mats[name])

ca = me.color_attributes.new(name='topY', type='FLOAT_COLOR', domain='FACE')
for i, bx in enumerate(items):
    hc = (min((bx[1] + bx[4]) / 64.0, 1.0), 0.0, 0.0, 1.0)
    for f in range(i*6, min((i+1)*6, len(ca.data))):
        ca.data[f].color = hc

for i, bx in enumerate(items):
    kind = K[bx[6]] if bx[6] < len(K) else 'concrete'
    idx = list(LOOK.keys()).index(kind)
    for f in range(i*6, min((i+1)*6, len(me.polygons))):
        me.polygons[f].material_index = idx

sc = bpy.context.scene
sc.render.engine = 'BLENDER_EEVEE'
sc.eevee.taa_render_samples = 48
sc.render.resolution_x = 1400; sc.render.resolution_y = 900
sc.view_settings.view_transform = 'AgX'
try:
    sc.view_settings.look = 'AgX - Medium High Contrast'
except Exception as e:
    print('look non disponibile:', e)
sc.view_settings.exposure = 0.0

w = bpy.data.worlds.new('w'); sc.world = w; w.use_nodes = True
bg = w.node_tree.nodes['Background']
bg.inputs['Strength'].default_value = 0.55

# Lo stesso cielo del gioco, se c'è: la panorama in game/assets/sky.
sky_file = ROOT / "game" / "assets" / "sky" / "sky_panorama.jpg"
if sky_file.exists():
    env = w.node_tree.nodes.new('ShaderNodeTexEnvironment')
    env.image = bpy.data.images.load(str(sky_file))
    w.node_tree.links.new(env.outputs['Color'], bg.inputs['Color'])
    print(f"cielo: {sky_file.name}")
else:
    bg.inputs['Color'].default_value = (0.34, 0.46, 0.62, 1)

sun = bpy.data.lights.new('sun', 'SUN'); sun.energy = 2.8; sun.angle = math.radians(2.0)
sun.color = (1.0, 0.93, 0.80)
so = bpy.data.objects.new('sun', sun); sc.collection.objects.link(so)
so.rotation_euler = (math.radians(54), 0, math.radians(-135))


def cam(name, loc, look, lens=35):
    cd = bpy.data.cameras.new(name); cd.lens = lens
    c = bpy.data.objects.new(name, cd); sc.collection.objects.link(c)
    c.location = loc
    c.rotation_euler = (Vector(look)-Vector(loc)).to_track_quat('-Z', 'Y').to_euler()
    return c


# posizioni in metri del gioco (X est, Y su, Z sud) convertite in Blender: (x, -z, y)
views = [('aerial', (60, 95, 70), (0, 0, 6), 40),
         ('street', (0, 70, 2.2), (-2, 53, 3.5), 26),
         ('piazza', (14, 8, 3.2), (-6, -2, 2.5), 24),
         ('harbor', (30, 93, 2.4), (46, 72, 6), 32),
         ('roofs', (4, 30, 26), (10, 12, 6), 45)]

only = os.environ.get('ONLY')
for n, loc, look, lens in views:
    if only and n != only:
        continue
    sc.camera = cam(n, loc, look, lens)
    sc.render.filepath = str(OUT / f"{n}.png")
    bpy.ops.render.render(write_still=True)
print('done')
