"""
FALEZIA · art/weapons/hush · 04_render_sheet.py
Foglio di render per l'approvazione (5 viste), luci come da brief artistico:
principale 45 gradi in alto a sinistra 5600 K, riempimento -30 a destra 5600 K -1.5 stop,
bordo dietro 6500 K +0.5 stop, sfondo piatto #4A4A4A. Niente fumo/DOF.
"""
import bpy, bmesh, math, os
from mathutils import Vector

ROOT = "/home/matteo/Progetti/Falezia/art/weapons/hush"
OUT = os.path.join(ROOT, "review")
os.makedirs(OUT, exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=os.path.join(ROOT, "hush.blend"))
sc = bpy.context.scene
# il file puo' contenere luci/camera di anteprima salvate dalla sessione live
for ob in list(bpy.data.objects):
    if ob.name.startswith("preview_"):
        bpy.data.objects.remove(ob, do_unlink=True)
sc.frame_set(1)

def s2l(c):
    return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4

def kelvin(k):
    t = k / 100.0
    if t <= 66:
        r, g = 255, 99.4708025861 * math.log(t) - 161.1195681661
        b = 0 if t <= 19 else 138.5177312231 * math.log(t - 10) - 305.0447927307
    else:
        r = 329.698727446 * ((t - 60) ** -0.1332047592)
        g = 288.1221695283 * ((t - 60) ** -0.0755148492)
        b = 255
    return tuple(max(0.0, min(1.0, s2l(v / 255.0))) for v in (r, g, b))

bg = s2l(0x4A / 255)
w = bpy.data.worlds.new("sheet")
sc.world = w
w.use_nodes = True
w.node_tree.nodes["Background"].inputs['Color'].default_value = (bg, bg, bg, 1)
w.node_tree.nodes["Background"].inputs['Strength'].default_value = 1.0

CENTER = Vector((0, 0.098, 0.000))

def area(name, d, dist, energy, k, size, target=None):
    ld = bpy.data.lights.get(name)
    if ld is None:
        ld = bpy.data.lights.new(name, 'AREA')
        ob = bpy.data.objects.new(name, ld)
        sc.collection.objects.link(ob)
    else:
        ob = bpy.data.objects[name]
    ld.energy = energy
    ld.color = kelvin(k)
    ld.size = size
    v = Vector(d).normalized()
    base = CENTER if target is None else Vector(target)
    ob.location = base + v * dist
    ob.rotation_euler = (-v).to_track_quat('-Z', 'Y').to_euler()
    return ob

KEY = 10.5
# le luci orbitano con la camera: ogni vista ha la stessa luce relativa
# (key 45 gradi alto-sinistra, fill -30 a destra, rim dietro), come da brief.
def place_lights(loc, look):
    f = (Vector(look) - Vector(loc)).normalized()      # direzione di vista
    right = f.cross(Vector((0, 0, 1)))
    if right.length < 1e-5:
        right = Vector((1, 0, 0))
    right.normalize()
    up = Vector((0, 0, 1))
    kd = (-f * 0.78 - right * 0.52 + up * 0.66)
    fd = (-f * 0.88 + right * 0.42 + up * 0.10)
    rd = (f * 0.80 + up * 0.42)
    area("Key", kd, 1.00, KEY, 5600, 0.55, look)
    area("Fill", fd, 1.05, KEY / (2 ** 1.5), 5600, 0.70, look)
    area("Rim", rd, 1.15, KEY * (2 ** 0.5), 6500, 0.40, look)

# righello 100 mm per la vista 1
mat_r = bpy.data.materials.new("ruler"); mat_r.use_nodes = True
mat_r.node_tree.nodes["Principled BSDF"].inputs['Base Color'].default_value = (0.55, 0.55, 0.55, 1)
mat_r.node_tree.nodes["Principled BSDF"].inputs['Roughness'].default_value = 0.6
bm = bmesh.new()
def bar(x0, x1, y0, y1, z0, z1):
    bmesh.ops.create_cube(bm, size=1.0)
    vs = bm.verts[-8:]
    bmesh.ops.scale(bm, vec=(x1 - x0, y1 - y0, z1 - z0), verts=vs)
    bmesh.ops.translate(bm, vec=((x0 + x1) / 2, (y0 + y1) / 2, (z0 + z1) / 2), verts=vs)
bar(-0.006, 0.0, 0.020, 0.120, -0.089, -0.085)
for i in range(11):
    y = 0.020 + i * 0.010
    h = 0.008 if i % 5 == 0 else 0.005
    bar(-0.006, -0.002, y - 0.0006, y + 0.0006, -0.085, -0.085 + h)
me = bpy.data.meshes.new("ruler")
bm.to_mesh(me); bm.free()
me.materials.append(mat_r)
ruler = bpy.data.objects.new("ruler", me)
sc.collection.objects.link(ruler)

def camera(name, loc, look, lens=35.0, ortho=None):
    cd = bpy.data.cameras.new(name)
    if ortho:
        cd.type = 'ORTHO'; cd.ortho_scale = ortho
    else:
        cd.lens = lens
    c = bpy.data.objects.new(name, cd)
    sc.collection.objects.link(c)
    c.location = loc
    c.rotation_euler = (Vector(look) - Vector(loc)).to_track_quat('-Z', 'Y').to_euler()
    return c

RESX = int(os.environ.get("SHEET_RESX", 1400))
RESY = int(os.environ.get("SHEET_RESY", 900))
SAMPLES = int(os.environ.get("SHEET_SAMPLES", 64))
ONLY = [v for v in os.environ.get("SHEET_ONLY", "").split(",") if v]
sc.render.engine = 'BLENDER_EEVEE'
sc.eevee.taa_render_samples = SAMPLES
sc.render.resolution_x = RESX
sc.render.resolution_y = RESY
sc.view_settings.view_transform = 'AgX'
sc.view_settings.look = 'AgX - Medium High Contrast'
sc.view_settings.exposure = 0.0
sc.render.film_transparent = False

# vista 1: ortografica laterale sinistra, arma 85% del frame (con righello)
ortho = (0.145 / 0.85) * RESX / RESY
v1 = camera("v1", (-1.0, 0.098, 0.000), (0, 0.098, 0.000), ortho=ortho)
# vista 2: tre quarti anteriore sinistro, 35 mm equivalenti
v2 = camera("v2", (-0.300, 0.560, 0.185), (0.0, 0.105, 0.000), lens=50)
# vista 3: tre quarti posteriore destro
v3 = camera("v3", (0.300, -0.260, 0.170), (0.0, 0.080, 0.000), lens=50)
# vista 4: dettaglio culatta
v4 = camera("v4", (0.175, 0.150, 0.082), (0.0, 0.110, 0.046), lens=70)
# vista 5: profilo destro ortografico (controllo marchi/lato destro)
v5 = camera("v5", (1.0, 0.098, 0.000), (0, 0.098, 0.000),
            ortho=ortho)

VIEWS = (("hush_1_left", v1, False, (-1.0, 0.098, 0.000), (0, 0.098, 0.000)),
         ("hush_2_34front", v2, True, (-0.300, 0.560, 0.185), (0.0, 0.105, 0.000)),
         ("hush_3_34rear", v3, True, (0.300, -0.260, 0.170), (0.0, 0.080, 0.000)),
         ("hush_4_barrelgap", v4, True, (0.175, 0.150, 0.082), (0.0, 0.110, 0.046)),
         ("hush_5_right", v5, True, (1.0, 0.098, 0.000), (0, 0.098, 0.000)))
for name, cam, hide_ruler, cloc, clook in VIEWS:
    if ONLY and name not in ONLY:
        continue
    ruler.hide_render = hide_ruler
    place_lights(cloc, clook)
    sc.camera = cam
    sc.render.filepath = os.path.join(OUT, name + ".png")
    bpy.ops.render.render(write_still=True)
    print("RENDERED", name)
