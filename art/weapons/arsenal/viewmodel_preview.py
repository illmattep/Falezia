"""
FALEZIA · art/weapons/arsenal/viewmodel_preview.py — come si vede l'arma in prima persona.

Replica in Blender quello che fa WeaponView.cs: modello sotto la camera, posa hip,
posa in mira calcolata dal socket_aim, scala per classe. Serve perché su questa
macchina Godot non è installato: senza questo, la posa del viewmodel non si vedrebbe
mai finché non si apre il gioco.

    blender -b --factory-startup -P art/weapons/arsenal/viewmodel_preview.py -- chatter cadence heavy

Le pose qui sotto sono COPIATE da game/client/WeaponView.cs: se cambiano là, si cambiano qui.
"""
import bpy, os, math, sys
import numpy as np
from mathutils import Vector, Matrix

ROOT = "/home/matteo/Progetti/Falezia"
GAME = os.path.join(ROOT, "game", "assets", "weapons")
OUT = os.path.join(ROOT, "art", "weapons", "arsenal", "review")
os.makedirs(OUT, exist_ok=True)

argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
NAMES = argv or ["chatter", "service", "cadence", "choke", "anvil", "spotter", "longshot", "heavy", "melee"]

# --- le pose di WeaponView.cs (hip, hipRot in gradi, scala, distanza del mirino)
POSES = {
    "pistol":  ((0.172, -0.146, -0.272), (2.0, -8.0, 1.5), 1.00, -0.225),
    "smg": ((0.145, -0.19, -0.285), (1.8, -7.0, 1.2), 0.93, -0.185),
    "shotgun": ((0.145, -0.205, -0.3), (1.6, -6.5, 1.0), 0.95, -0.175),
    "rifle": ((0.145, -0.205, -0.295), (1.8, -7.0, 1.2), 0.93, -0.17),
    "sniper": ((0.15, -0.21, -0.29), (1.4, -6.0, 1.0), 0.96, -0.165),
    "lmg": ((0.15, -0.215, -0.29), (1.6, -6.5, 1.0), 0.95, -0.17),
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

# Godot (X,Y,Z) -> Blender (X,-Z,Y)
M = np.array([[1, 0, 0], [0, 0, -1], [0, 1, 0]], dtype=float)


def g2b(p):
    return Vector(M @ np.array(p, dtype=float))


def euler_yxz(pitch, yaw, roll):
    """La base di Godot: YXZ (yaw, poi pitch, poi roll), in radianti."""
    cp, sp = math.cos(pitch), math.sin(pitch)
    cy, sy = math.cos(yaw), math.sin(yaw)
    cr, sr = math.cos(roll), math.sin(roll)
    Rx = np.array([[1, 0, 0], [0, cp, -sp], [0, sp, cp]])
    Ry = np.array([[cy, 0, sy], [0, 1, 0], [-sy, 0, cy]])
    Rz = np.array([[cr, -sr, 0], [sr, cr, 0], [0, 0, 1]])
    return Ry @ Rx @ Rz


def place(obj, pos_godot, rot_deg, scale):
    """Mette l'oggetto (radice del modello) nella posa del viewmodel, in coordinate Blender."""
    pitch, yaw, roll = (math.radians(a) for a in rot_deg)
    B = euler_yxz(pitch, yaw, roll)
    Bb = M @ B @ M.T                     # stessa rotazione, assi Blender
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
    roots = [o for o in new if o.parent is None]
    return roots[0], new


def setup_scene():
    sc = bpy.context.scene
    sc.render.engine = "BLENDER_EEVEE"
    sc.eevee.taa_render_samples = 32
    sc.render.resolution_x = 960
    sc.render.resolution_y = 540
    sc.render.film_transparent = False
    sc.view_settings.view_transform = "AgX"
    sc.view_settings.exposure = 0.6

    world = bpy.data.worlds.new("w")
    sc.world = world
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.10, 0.11, 0.12, 1)
    world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.55

    cd = bpy.data.cameras.new("cam")
    cd.sensor_fit = "VERTICAL"
    cd.angle_y = math.radians(85.0)       # il FOV verticale di Godot
    cd.clip_start = 0.01
    cam = bpy.data.objects.new("cam", cd)
    cam.location = (0, 0, 0)
    cam.rotation_euler = (math.radians(90), 0, 0)   # guarda +Y (avanti in Godot)
    sc.collection.objects.link(cam)
    sc.camera = cam

    def area(name, loc, rot, size, energy, color=(1, 1, 1)):
        d = bpy.data.lights.new(name, "AREA")
        d.size = size
        d.energy = energy
        d.color = color
        o = bpy.data.objects.new(name, d)
        o.location = loc
        o.rotation_euler = rot
        o.visible_camera = False       # le luci area non si vedono in camera
        sc.collection.objects.link(o)

    # luci vicine, come una giornata limpida davanti al giocatore
    area("key", (-0.7, 0.9, 0.9), (math.radians(-40), 0, math.radians(-35)), 1.4, 90, (1.0, 0.96, 0.9))
    area("fill", (0.9, 0.6, 0.2), (math.radians(80), 0, math.radians(50)), 1.6, 30, (0.8, 0.87, 1.0))
    area("rim", (0.2, -0.6, 0.8), (math.radians(120), 0, math.radians(10)), 1.2, 40, (1.0, 0.92, 0.82))

    # un po' di contesto: pavimento all'altezza dei piedi e una parete lontana, così
    # si capisce dove sta l'orizzonte e quanto è grande l'arma sullo schermo
    mat = bpy.data.materials.new("ground")
    mat.use_nodes = True
    b = mat.node_tree.nodes["Principled BSDF"]
    b.inputs["Base Color"].default_value = (0.19, 0.20, 0.21, 1)
    b.inputs["Roughness"].default_value = 0.9

    me = bpy.data.meshes.new("floor")
    me.from_pydata([(-30, -30, -1.75), (30, -30, -1.75), (30, 30, -1.75), (-30, 30, -1.75)], [], [(0, 1, 2, 3)])
    me.update()
    floor = bpy.data.objects.new("floor", me)
    floor.data.materials.append(mat)
    sc.collection.objects.link(floor)

    me2 = bpy.data.meshes.new("wall")
    me2.from_pydata([(-30, 9.0, -1.75), (30, 9.0, -1.75), (30, 9.0, 12.0), (-30, 9.0, 12.0)], [], [(0, 1, 2, 3)])
    me2.update()
    wall = bpy.data.objects.new("wall", me2)
    m2 = mat.copy()
    m2.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (0.34, 0.35, 0.36, 1)
    wall.data.materials.append(m2)
    sc.collection.objects.link(wall)
    return sc


def render_pose(wid, state, out_path):
    sc = setup_scene()
    root, parts = import_weapon(wid)
    cls = CLASSES.get(wid, "pistol")
    hip, hip_rot, scale, ads_z = POSES[cls]

    # il socket_aim in coordinate Godot: dal locale Blender (x, y, z) -> (x, z, -y)
    aim = (0.0, 0.0, -0.08)
    for ob in bpy.data.objects:
        if ob.name.endswith("socket_aim"):
            b = ob.location
            aim = (b.x, b.z, -b.y)
            break
    # come WeaponView: X e Y dal socket_aim, la distanza dalla posa (ADS_Z)
    ads = (-aim[0], -aim[1] - 0.004, ads_z)

    print("AIM", wid, aim, "ADS" if state == "ads" else "HIP", ads if state == "ads" else hip)
    if state == "ads":
        if cls == "knife":
            place(root, (hip[0] - 0.03, hip[1] + 0.035, hip[2] - 0.055), (0.0, 0.0, 0.0), scale)
        else:
            place(root, ads, (0.0, 0.0, 0.0), scale)
    else:
        place(root, hip, hip_rot, scale)

    sc.render.filepath = out_path
    bpy.ops.render.render(write_still=True)
    clear()


def main():
    tmp = "/tmp/viewmodel"
    os.makedirs(tmp, exist_ok=True)
    shots = []
    for wid in NAMES:
        if not os.path.exists(os.path.join(GAME, wid, wid + ".glb")):
            print("MANCA", wid)
            continue
        for state in ("hip", "ads"):
            path = os.path.join(tmp, f"{wid}_{state}.png")
            render_pose(wid, state, path)
            shots.append((wid, state, path))
            print("RESO", wid, state, path)

    # griglia: una riga per arma, hip a sinistra e mira a destra
    try:
        from PIL import Image, ImageDraw
    except ImportError:
        print("Pillow assente: le immagini restano in", tmp)
        return
    rows = len(NAMES)
    w, h = 960, 540
    sheet = Image.new("RGB", (w * 2, h * rows), (16, 17, 19))
    d = ImageDraw.Draw(sheet)
    for i, wid in enumerate(NAMES):
        for j, state in enumerate(("hip", "ads")):
            path = os.path.join(tmp, f"{wid}_{state}.png")
            if not os.path.exists(path):
                continue
            sheet.paste(Image.open(path).convert("RGB"), (j * w, i * h))
            d.text((j * w + 16, i * h + 12), f"{wid.upper()} · {state.upper()}", fill=(210, 220, 230))
    out = os.path.join(OUT, "viewmodel.png")
    sheet.save(out)
    print("VIEWMODEL", out, sheet.size)


main()
