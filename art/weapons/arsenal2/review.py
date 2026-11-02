"""
FALEZIA · art/weapons/arsenal2/review.py — controllo visivo fuori dal motore.

    blender -b --factory-startup -P art/weapons/arsenal2/review.py -- closeup standard chatter
    blender -b --factory-startup -P art/weapons/arsenal2/review.py -- sheet
    blender -b --factory-startup -P art/weapons/arsenal2/review.py -- viewmodel standard service heavy

Tre viste: primo piano di profilo, foglio dell'arsenale, e "in prima persona" con la
camera a FOV 85 come in gioco (stesse pose di WeaponView.cs).
"""
import bpy
import os
import math
import sys
from mathutils import Vector, Matrix
import numpy as np

ROOT = "/home/matteo/Progetti/Falezia"
GAME = os.path.join(ROOT, "game", "assets", "weapons")
OUT = os.path.join(ROOT, "art", "weapons", "arsenal2", "review")
os.makedirs(OUT, exist_ok=True)

ORDER = ["standard", "hush", "chatter", "service", "blitz", "sweeper", "snub", "hammer",
         "choke", "anvil", "cadence", "scrap", "spotter", "vantage", "longshot", "heavy", "melee"]

POSES = {
    "pistol":  ((0.172, -0.146, -0.272), (2.0, -8.0, 1.5), 1.00, -0.225),
    "smg":     ((0.145, -0.190, -0.285), (1.8, -7.0, 1.2), 0.93, -0.185),
    "shotgun": ((0.145, -0.205, -0.300), (1.6, -6.5, 1.0), 0.95, -0.175),
    "rifle":   ((0.145, -0.205, -0.295), (1.8, -7.0, 1.2), 0.93, -0.170),
    "sniper":  ((0.150, -0.210, -0.290), (1.4, -6.0, 1.0), 0.96, -0.165),
    "lmg":     ((0.150, -0.215, -0.290), (1.6, -6.5, 1.0), 0.95, -0.170),
    "knife":   ((0.120, -0.215, -0.300), (22.0, -52.0, 14.0), 1.00, -0.355),
}
CLASSES = {
    "standard": "pistol", "hush": "pistol", "chatter": "pistol", "snub": "pistol", "hammer": "pistol",
    "cadence": "smg", "scrap": "smg", "blitz": "smg",
    "choke": "shotgun", "sweeper": "shotgun",
    "anvil": "rifle", "service": "rifle", "vantage": "rifle",
    "spotter": "sniper", "longshot": "sniper",
    "heavy": "lmg", "melee": "knife",
}

M = np.array([[1, 0, 0], [0, 0, -1], [0, 1, 0]], dtype=float)


def g2b(p):
    return Vector(M @ np.array(p, dtype=float))


def euler_yxz(pitch, yaw, roll):
    cp, sp = math.cos(pitch), math.sin(pitch)
    cy, sy = math.cos(yaw), math.sin(yaw)
    cr, sr = math.cos(roll), math.sin(roll)
    Rx = np.array([[1, 0, 0], [0, cp, -sp], [0, sp, cp]])
    Ry = np.array([[cy, 0, sy], [0, 1, 0], [-sy, 0, cy]])
    Rz = np.array([[cr, -sr, 0], [sr, cr, 0], [0, 0, 1]])
    return Ry @ Rx @ Rz


def place(obj, pos_godot, rot_deg, scale):
    pitch, yaw, roll = (math.radians(a) for a in rot_deg)
    B = euler_yxz(pitch, yaw, roll)
    Bb = M @ B @ M.T
    rot = Matrix([[Bb[0][0], Bb[0][1], Bb[0][2]],
                  [Bb[1][0], Bb[1][1], Bb[1][2]],
                  [Bb[2][0], Bb[2][1], Bb[2][2]]]).to_4x4()
    obj.matrix_world = Matrix.Translation(g2b(pos_godot)) @ rot @ Matrix.Scale(scale, 4)


def clear():
    for ob in list(bpy.data.objects):
        bpy.data.objects.remove(ob, do_unlink=True)


def import_weapon(wid):
    before = set(bpy.data.objects)
    bpy.ops.import_scene.gltf(filepath=os.path.join(GAME, wid, wid + ".glb"))
    new = [o for o in bpy.data.objects if o not in before]
    # le clip NLA vengono valutate all'import: senza questo si vede la posa della
    # ricarica (caricatore fuori, fucile inclinato) invece dell'arma a riposo
    for o in new:
        if o.animation_data:
            o.animation_data_clear()
    roots = [o for o in new if o.parent is None]
    return roots[0], new


def lights(sc, close=1.0, sun=False):
    def area(n, loc, rot, size, energy, col=(1, 1, 1)):
        d = bpy.data.lights.new(n, "AREA"); d.size = size; d.energy = energy; d.color = col
        o = bpy.data.objects.new(n, d); o.location = loc; o.rotation_euler = rot
        sc.collection.objects.link(o); o.visible_camera = False

    def sun_lamp(n, rot_deg, energy, col=(1, 1, 1)):
        d = bpy.data.lights.new(n, "SUN")
        d.energy = energy
        d.angle = math.radians(14)
        d.color = col
        o = bpy.data.objects.new(n, d)
        o.rotation_euler = tuple(math.radians(a) for a in rot_deg)
        o.visible_camera = False
        sc.collection.objects.link(o)

    if sun:
        # foglio dell'arsenale: le armi ai bordi sono lontane dalle area light e
        # diventerebbero silhouette. Tre luci direzionali le illuminano tutte uguale.
        sun_lamp("key", (52, 0, -38), 3.2, (1.0, 0.97, 0.92))
        sun_lamp("fill", (66, 0, 118), 1.1, (0.80, 0.88, 1.0))
        sun_lamp("rim", (-46, 0, 14), 1.8, (1.0, 0.93, 0.85))
    else:
        area("key", (-0.55 * close, -0.45 * close, 0.55 * close),
             (math.radians(35), 0, math.radians(-35)), 0.5 * close, 55 * close, (1.0, 0.96, 0.9))
        area("fill", (0.6 * close, -0.35 * close, 0.2 * close),
             (math.radians(65), 0, math.radians(55)), 0.6 * close, 16 * close, (0.8, 0.87, 1.0))
        area("rim", (0.2 * close, 0.5 * close, 0.45 * close),
             (math.radians(-50), 0, math.radians(10)), 0.5 * close, 34 * close, (1.0, 0.93, 0.85))

    w = bpy.data.worlds.new("w"); sc.world = w; w.use_nodes = True
    nt = w.node_tree
    bg = nt.nodes["Background"]
    # cielo in gradiente: i metalli ci riflettono dentro e smettono di sembrare plastica
    try:
        sky = nt.nodes.new("ShaderNodeTexSky")
        sky.sky_type = "NISHITA"
        sky.sun_elevation = math.radians(28)
        sky.sun_rotation = math.radians(120)
        nt.links.new(sky.outputs["Color"], bg.inputs["Color"])
        # il foglio guarda le armi di fianco: i metalli vivono di riflessi, con un
        # mondo a 0,38 diventavano neri. Un cielo più presente li fa leggere.
        bg.inputs["Strength"].default_value = 0.72 if sun else 0.38
    except Exception:
        bg.inputs["Color"].default_value = (0.24, 0.28, 0.34, 1)
        bg.inputs["Strength"].default_value = 1.0


def scene(cam_pos, target, ortho=None, res=(1600, 900), lens=70, fov=None, sun=False):
    sc = bpy.context.scene
    sc.render.engine = "BLENDER_EEVEE"
    sc.eevee.taa_render_samples = 64
    # senza raytracing EEVEE non riflette niente: i materiali metallici sembrano piatti
    try:
        sc.eevee.use_raytracing = True
        sc.eevee.ray_tracing_options.use_denoise = True
        sc.eevee.ray_tracing_options.screen_trace_quality = 0.5
    except Exception:
        pass
    sc.render.resolution_x, sc.render.resolution_y = res
    sc.view_settings.view_transform = "AgX"
    sc.view_settings.exposure = 0.05
    lights(sc, close=max(0.6, min(2.0, (Vector(cam_pos) - Vector(target)).length)), sun=sun)

    cd = bpy.data.cameras.new("cam")
    if ortho:
        cd.type = "ORTHO"; cd.ortho_scale = ortho
    elif fov:
        cd.sensor_fit = "VERTICAL"; cd.angle_y = math.radians(fov)
    else:
        cd.lens = lens
    cd.clip_start = 0.01
    cam = bpy.data.objects.new("cam", cd)
    cam.location = cam_pos
    sc.collection.objects.link(cam)
    sc.camera = cam
    t = bpy.data.objects.new("target", None); t.location = target
    sc.collection.objects.link(t)
    c = cam.constraints.new("TRACK_TO"); c.target = t
    c.track_axis = "TRACK_NEGATIVE_Z"; c.up_axis = "UP_Y"
    return sc


def bounds(objs):
    pts = [o.matrix_world @ Vector(c) for o in objs if o.type == "MESH" for c in o.bound_box]
    mn = Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts)))
    mx = Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))
    return mn, mx


def render_closeup(names, tag="closeup"):
    clear()
    rows = len(names)
    longest = 0.3
    tallest = 0.1
    for i, wid in enumerate(names):
        before = set(bpy.data.objects)
        root, new = import_weapon(wid)
        mn, mx = bounds(new)
        center = (mn + mx) / 2
        longest = max(longest, (mx - mn).y)
        tallest = max(tallest, (mx - mn).z)
        row_h = max(0.10, longest * 0.34, tallest * 1.35)
        pivot = bpy.data.objects.new("piv_" + wid, None)
        bpy.context.scene.collection.objects.link(pivot)
        root.parent = pivot
        root.location = -center
        pivot.location = (0.0, 0.0, -(i - (rows - 1) / 2) * row_h - row_h * 0.10)
        pivot.rotation_euler[2] = math.radians(-90)

        cu = bpy.data.curves.new("t_" + wid, "FONT")
        cu.body = wid.upper(); cu.size = row_h * 0.16
        t = bpy.data.objects.new("t_" + wid, cu)
        t.location = (-longest * 0.62, 0.0, -(i - (rows - 1) / 2) * row_h + row_h * 0.36)
        bpy.context.scene.collection.objects.link(t)

    sc = scene((0, -2.2, 0), (0, 0, 0), ortho=longest * 1.32,
               res=(1600, int(1600 * row_h * rows / (longest * 1.32))))
    sc.render.filepath = os.path.join(OUT, tag + ".png")
    bpy.ops.render.render(write_still=True)
    print("RENDER", sc.render.filepath)


def render_sheet(names=None):
    names = names or ORDER
    clear()
    cols = 4
    rows = (len(names) + cols - 1) // cols
    # la cella deve contenere l'arma PIU' LUNGA, altrimenti i fucili si sovrappongono
    # alle pistole della colonna accanto (era il difetto del foglio)
    cell_w, cell_h = 1.62, 0.42
    for i, wid in enumerate(names):
        root, new = import_weapon(wid)
        mn, mx = bounds(new)
        center = (mn + mx) / 2
        pivot = bpy.data.objects.new("piv_" + wid, None)
        bpy.context.scene.collection.objects.link(pivot)
        root.parent = pivot
        root.location = -center
        # la colonna alterna l'arma a sinistra e a destra: le canne non si incrociano
        side = 1 if i % 2 == 0 else -1
        pivot.location = ((i % cols - (cols - 1) / 2) * cell_w + side * 0.06, 0.0,
                          -((i // cols) - (rows - 1) / 2) * cell_h)
        pivot.rotation_euler[2] = math.radians(-90)

    width = cols * cell_w
    res_w = 2000
    res_h = int(res_w * rows * cell_h / width)
    sc = scene((0, -6.0, 0), (0, 0, 0), ortho=width, res=(res_w, res_h), sun=True)
    sc.render.filepath = os.path.join(OUT, "sheet.png")
    bpy.ops.render.render(write_still=True)
    print("RENDER", sc.render.filepath)


def render_viewmodel(names):
    import tempfile
    shots = []
    for wid in names:
        for state in ("hip", "ads"):
            clear()
            root, new = import_weapon(wid)
            # il nodo radice porta la rotazione di conversione Y-up→Z-up: se la
            # sovrascrivessi l'arma finirebbe coricata. La posa va sul pivot.
            pivot = bpy.data.objects.new("vm_" + wid, None)
            bpy.context.scene.collection.objects.link(pivot)
            root.parent = pivot
            root.matrix_parent_inverse.identity()
            cls = CLASSES.get(wid, "pistol")
            hip, hip_rot, scale, ads_z = POSES[cls]
            aim = (0.0, 0.0, -0.08)
            for ob in bpy.data.objects:
                if ob.name.endswith("socket_aim"):
                    b = ob.location
                    aim = (b.x, b.z, -b.y)
                    break
            # come WeaponView: in mira si porta sull'asse l'OCCHIO, non la mira
            eye = None
            for ob in bpy.data.objects:
                if ob.name.endswith("socket_eye"):
                    b = ob.location
                    eye = (b.x, b.z, -b.y)
                    break
            ads = ((-eye[0], -eye[1], min(-eye[2], ads_z) - 0.03)
                   if eye else (-aim[0], -aim[1], ads_z - 0.03))
            if state == "ads":
                place(pivot, ads, (0.0, 0.0, 0.0), scale)
            else:
                place(pivot, hip, hip_rot, scale)

            # pavimento e parete, per capire dove sta l'orizzonte
            me = bpy.data.meshes.new("floor")
            me.from_pydata([(-30, -30, -1.75), (30, -30, -1.75), (30, 30, -1.75), (-30, 30, -1.75)],
                           [], [(0, 1, 2, 3)])
            me.update()
            fl = bpy.data.objects.new("floor", me)
            m = bpy.data.materials.new("ground"); m.use_nodes = True
            m.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (0.16, 0.17, 0.18, 1)
            m.node_tree.nodes["Principled BSDF"].inputs["Roughness"].default_value = 0.9
            fl.data.materials.append(m)
            bpy.context.scene.collection.objects.link(fl)

            sc = scene((0, 0, 0), (0, 1, 0), fov=85, res=(960, 540))
            sc.render.filepath = os.path.join(tempfile.gettempdir(), f"vm_{wid}_{state}.png")
            bpy.ops.render.render(write_still=True)
            shots.append(sc.render.filepath)

    try:
        from PIL import Image, ImageDraw
    except ImportError:
        return
    w, h = 640, 360
    sheet = Image.new("RGB", (w * 2, h * len(names)), (16, 17, 19))
    d = ImageDraw.Draw(sheet)
    for i, wid in enumerate(names):
        for j, state in enumerate(("hip", "ads")):
            p = os.path.join(tempfile.gettempdir(), f"vm_{wid}_{state}.png")
            if not os.path.exists(p):
                continue
            sheet.paste(Image.open(p).convert("RGB").resize((w, h)), (j * w, i * h))
            d.rectangle([j * w + 5, i * h + 5, j * w + 150, i * h + 21], fill=(10, 12, 14))
            d.text((j * w + 10, i * h + 8), f"{wid.upper()} · {state.upper()}", fill=(230, 235, 240))
    path = os.path.join(OUT, "viewmodel.png")
    sheet.save(path)
    print("RENDER", path, sheet.size)


argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
mode = argv[0] if argv else "sheet"
names = argv[1:] if len(argv) > 1 else None
if mode == "closeup":
    render_closeup(names or ["standard"])
elif mode == "sheet":
    render_sheet(names)
elif mode == "viewmodel":
    render_viewmodel(names or ["standard"])
