"""
FALEZIA · art/weapons/arsenal2/sights.py — la prova delle mire.

    blender -b --factory-startup -P art/weapons/arsenal2/sights.py -- standard anvil

Per ogni arma guarda dentro la linea di mira: camera dietro al calcio, ortogonale,
centrata ESATTAMENTE sul socket `aim`, con due bacchette rosse emissive che
segnano la linea (una orizzontale, una verticale). Se la tacca e il mirino sono
alla stessa quota, il mirino cade sul centro della croce; se una tacca è più
bassa, si vede il buco sopra o sotto la bacchetta.

Produce review/sight_<arma>.png (520×520). Il foglio si compone con make_sights_sheet.py.
"""
import bpy
import os
import math
import sys
from mathutils import Vector

ROOT = "/home/matteo/Progetti/Falezia"
GAME = os.path.join(ROOT, "game", "assets", "weapons")
OUT = os.path.join(ROOT, "art", "weapons", "arsenal2", "review")
os.makedirs(OUT, exist_ok=True)

ORDER = ["standard", "hush", "chatter", "service", "blitz", "sweeper", "snub", "hammer",
         "choke", "anvil", "cadence", "scrap", "spotter", "vantage", "longshot", "heavy", "melee"]

# finestra in metri: deve contenere le due tacche e un po' di arma attorno
WINDOW = {
    "standard": 0.080, "hush": 0.080, "chatter": 0.080, "snub": 0.080, "hammer": 0.080,
    "cadence": 0.090, "scrap": 0.090, "blitz": 0.090,
    "choke": 0.100, "sweeper": 0.100,
    "anvil": 0.100, "service": 0.100, "vantage": 0.100,
    "spotter": 0.110, "longshot": 0.110,
    "heavy": 0.110, "melee": 0.110,
}


def clear():
    for ob in list(bpy.data.objects):
        bpy.data.objects.remove(ob, do_unlink=True)


def import_weapon(wid):
    before = set(bpy.data.objects)
    bpy.ops.import_scene.gltf(filepath=os.path.join(GAME, wid, wid + ".glb"))
    new = [o for o in bpy.data.objects if o not in before]
    for o in new:
        if o.animation_data:
            o.animation_data_clear()
    roots = [o for o in new if o.parent is None]
    return roots[0], new


def rod(name, a, b, radius, mat):
    """Bacchetta cilindrica fra due punti (la croce della linea di mira)."""
    a, b = Vector(a), Vector(b)
    d = b - a
    bpy.ops.mesh.primitive_cylinder_add(vertices=8, radius=radius, depth=d.length,
                                        location=(a + b) * 0.5)
    ob = bpy.context.object
    ob.name = name
    ob.rotation_mode = "QUATERNION"
    ob.rotation_quaternion = d.to_track_quat("Z", "Y")
    ob.data.materials.append(mat)
    return ob


def lights(sc):
    def area(n, loc, rot, size, energy, col=(1, 1, 1)):
        d = bpy.data.lights.new(n, "AREA"); d.size = size; d.energy = energy; d.color = col
        o = bpy.data.objects.new(n, d); o.location = loc; o.rotation_euler = rot
        sc.collection.objects.link(o); o.visible_camera = False

    area("key", (-0.4, -0.3, 0.5), (math.radians(40), 0, math.radians(-40)), 0.4, 45, (1.0, 0.96, 0.9))
    area("fill", (0.5, -0.2, 0.15), (math.radians(70), 0, math.radians(60)), 0.5, 16, (0.82, 0.88, 1.0))
    area("rim", (0.1, 0.4, 0.35), (math.radians(-50), 0, math.radians(5)), 0.4, 30, (1.0, 0.93, 0.85))

    w = bpy.data.worlds.new("w"); sc.world = w; w.use_nodes = True
    nt = w.node_tree
    bg = nt.nodes["Background"]
    try:
        sky = nt.nodes.new("ShaderNodeTexSky")
        sky.sky_type = "NISHITA"
        sky.sun_elevation = math.radians(32)
        sky.sun_rotation = math.radians(100)
        nt.links.new(sky.outputs["Color"], bg.inputs["Color"])
        bg.inputs["Strength"].default_value = 0.55
    except Exception:
        bg.inputs["Color"].default_value = (0.25, 0.29, 0.35, 1)


def render_one(wid):
    clear()
    root, new = import_weapon(wid)

    aim = None
    for ob in new:
        if ob.name.endswith("socket_aim"):
            aim = ob.matrix_world.translation.copy()
            break
    if aim is None:
        print("NO AIM", wid)
        return None

    pts = [o.matrix_world @ Vector(c) for o in new if o.type == "MESH" for c in o.bound_box]
    ymin = min(v.y for v in pts)

    sc = bpy.context.scene
    sc.render.engine = "BLENDER_EEVEE"
    sc.eevee.taa_render_samples = 48
    try:
        sc.eevee.use_raytracing = True
        sc.eevee.ray_tracing_options.use_denoise = True
    except Exception:
        pass
    sc.render.resolution_x = sc.render.resolution_y = 520
    sc.view_settings.view_transform = "AgX"
    sc.view_settings.exposure = 0.05
    lights(sc)

    # croce della linea di mira: due bacchette emissive appena davanti alla camera,
    # così segnano il centro dell'immagine qualunque sia l'arma
    red = bpy.data.materials.new("aim_line")
    red.use_nodes = True
    nt = red.node_tree
    for n in list(nt.nodes):
        if n.type != "OUTPUT_MATERIAL":
            nt.nodes.remove(n)
    em = nt.nodes.new("ShaderNodeEmission")
    em.inputs["Color"].default_value = (1.0, 0.06, 0.04, 1)
    em.inputs["Strength"].default_value = 4.0
    nt.links.new(em.outputs["Emission"], nt.nodes["Material Output"].inputs["Surface"])

    cy = ymin - 0.02
    span = WINDOW.get(wid, 0.10) * 0.42
    rod("aim_h", (aim.x - span, cy, aim.z), (aim.x + span, cy, aim.z), 0.00035, red)
    rod("aim_v", (aim.x, cy, aim.z - span), (aim.x, cy, aim.z + span), 0.00035, red)

    cd = bpy.data.cameras.new("cam")
    cd.type = "ORTHO"
    cd.ortho_scale = WINDOW.get(wid, 0.10)
    cd.clip_start = 0.001
    cam = bpy.data.objects.new("cam", cd)
    # la canna è +Y: la camera sta dietro al calcio e guarda avanti, sulla linea di mira
    cam.location = Vector((aim.x, ymin - 0.06, aim.z))
    sc.collection.objects.link(cam)
    sc.camera = cam
    t = bpy.data.objects.new("target", None)
    t.location = Vector((aim.x, aim.y + 1.0, aim.z))
    sc.collection.objects.link(t)
    c = cam.constraints.new("TRACK_TO")
    c.target = t
    c.track_axis = "TRACK_NEGATIVE_Z"
    c.up_axis = "UP_Y"

    path = os.path.join(OUT, f"sight_{wid}.png")
    sc.render.filepath = path
    bpy.ops.render.render(write_still=True)
    print("SIGHT", wid, "aim", tuple(round(v, 4) for v in aim))
    return path


argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
names = argv if argv else ORDER
for wid in names:
    render_one(wid)
