"""
FALEZIA · art/weapons/arsenal/sheet.py — foglio di controllo dell'arsenale.

Importa i GLB esportati e li mette in fila come nel lineup, in profilo puro, per
giudicare le silhouette senza aprire Godot.

    blender -b --factory-startup -P art/weapons/arsenal/sheet.py
"""
import bpy, os, math
from mathutils import Vector

ROOT = "/home/matteo/Progetti/Falezia"
GAME = os.path.join(ROOT, "game", "assets", "weapons")
OUT = os.path.join(ROOT, "art", "weapons", "arsenal", "review")
os.makedirs(OUT, exist_ok=True)

ORDER = ["standard", "hush", "chatter", "service", "blitz", "sweeper", "snub", "hammer",
         "choke", "anvil", "cadence", "scrap", "spotter", "vantage", "longshot",
         "heavy", "melee"]

for ob in list(bpy.data.objects):
    bpy.data.objects.remove(ob, do_unlink=True)

sc = bpy.context.scene
cols = 4
col_w, row_h = 1.16, 0.30
rows = (len(ORDER) + cols - 1) // cols

for i, wid in enumerate(ORDER):
    path = os.path.join(GAME, wid, wid + ".glb")
    if not os.path.exists(path):
        print("MANCA", path)
        continue
    before = set(bpy.data.objects)
    bpy.ops.import_scene.gltf(filepath=path)
    new = [o for o in bpy.data.objects if o not in before]
    roots = [o for o in new if o.parent is None]
    if not roots:
        continue
    root = roots[0]

    # la canna è +Y: profilo puro, canna verso destra, arma centrata nello slot
    pts = []
    for o in new:
        if o.type == "MESH":
            for c in o.bound_box:
                pts.append(o.matrix_world @ Vector(c))
    if not pts:
        continue
    mn = Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts)))
    mx = Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))
    center = (mn + mx) / 2

    pivot = bpy.data.objects.new("slot_" + wid, None)
    sc.collection.objects.link(pivot)
    root.parent = pivot
    root.location = -center
    # griglia nel piano XZ: la camera guarda di profilo lungo +Y, quindi le righe
    # devono stare in altezza, non in profondità (altrimenti si sovrappongono)
    pivot.location = ((i % cols - (cols - 1) / 2) * col_w, 0.0, -((i // cols) - (rows - 1) / 2) * row_h)
    pivot.rotation_euler[2] = math.radians(-90)

# --- luci: taglio laterale, così la silhouette si legge controluce
def area(name, loc, rot, size, energy, color=(1, 1, 1)):
    d = bpy.data.lights.new(name, "AREA")
    d.size = size
    d.energy = energy
    d.color = color
    o = bpy.data.objects.new(name, d)
    o.location = loc
    o.rotation_euler = rot
    sc.collection.objects.link(o)


area("key", (-1.6, -2.2, 2.6), (math.radians(34), 0, math.radians(-34)), 2.4, 700, (1.0, 0.96, 0.9))
area("fill", (2.4, -1.8, 1.0), (math.radians(66), 0, math.radians(52)), 2.6, 220, (0.80, 0.87, 1.0))
area("rim", (0.4, 2.6, 1.8), (math.radians(-52), 0, math.radians(6)), 2.4, 620, (1.0, 0.92, 0.82))
area("top", (0.0, -0.4, 3.0), (0, 0, 0), 3.2, 180, (1.0, 1.0, 1.0))

world = bpy.data.worlds.new("w")
sc.world = world
world.use_nodes = True
world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.055, 0.058, 0.062, 1)
world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.7

# --- camera ortografica, profilo
cd = bpy.data.cameras.new("cam")
cd.type = "ORTHO"
cd.ortho_scale = 4.90
cam = bpy.data.objects.new("cam", cd)
cam.location = (0, -8.0, 0.0)
sc.collection.objects.link(cam)
sc.camera = cam

target = bpy.data.objects.new("target", None)
target.location = (0, 0, 0)
sc.collection.objects.link(target)
c = cam.constraints.new("TRACK_TO")
c.target = target
c.track_axis = "TRACK_NEGATIVE_Z"
c.up_axis = "UP_Y"

sc.render.engine = "BLENDER_EEVEE"
sc.eevee.taa_render_samples = 64
sc.render.resolution_x = 1800
sc.render.resolution_y = 620
sc.render.film_transparent = False
sc.view_settings.view_transform = "AgX"
sc.view_settings.exposure = 0.85
sc.render.filepath = os.path.join(OUT, "arsenal_sheet.png")
bpy.ops.render.render(write_still=True)
print("SHEET", sc.render.filepath)
