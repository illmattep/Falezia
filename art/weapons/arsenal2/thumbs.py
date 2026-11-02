"""
FALEZIA · art/weapons/arsenal2/thumbs.py — le miniature per lo shop.

    blender -b --factory-startup -P art/weapons/arsenal2/thumbs.py

Un'arma per volta, profilo laterale, sfondo TRASPARENTE, 512×256. Finiscono in
game/assets/weapons/ui/<id>.png e sono quelle che il pannello d'acquisto mostra
nelle schede (come i ritratti delle armi nel negozio di Valorant).
"""
import bpy
import os
import math
import sys
from mathutils import Vector

ROOT = "/home/matteo/Progetti/Falezia"
GAME = os.path.join(ROOT, "game", "assets", "weapons")
OUT = os.path.join(GAME, "ui")
os.makedirs(OUT, exist_ok=True)

ORDER = ["standard", "hush", "chatter", "snub", "hammer",
         "cadence", "scrap", "blitz",
         "choke", "sweeper",
         "anvil", "service", "vantage", "spotter", "longshot",
         "heavy", "melee"]


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
    return [o for o in new if o.parent is None][0], new


def render(wid):
    clear()
    root, new = import_weapon(wid)
    pts = [o.matrix_world @ Vector(c) for o in new if o.type == "MESH" for c in o.bound_box]
    mn = Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts)))
    mx = Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))
    c = (mn + mx) / 2

    piv = bpy.data.objects.new("piv", None)
    bpy.context.scene.collection.objects.link(piv)
    root.parent = piv
    root.location = -c
    piv.rotation_euler[2] = math.radians(-90)

    sc = bpy.context.scene
    sc.render.engine = "BLENDER_EEVEE"
    sc.eevee.taa_render_samples = 48
    try:
        sc.eevee.use_raytracing = True
        sc.eevee.ray_tracing_options.use_denoise = True
    except Exception:
        pass
    sc.render.resolution_x, sc.render.resolution_y = 512, 256
    sc.render.film_transparent = True
    sc.view_settings.view_transform = "AgX"
    sc.view_settings.exposure = 0.35

    # luci direzionali: uniformi, niente cadute ai bordi
    for n, rot, e, col in (("k", (52, 0, -38), 3.4, (1, 0.97, 0.92)),
                           ("f", (66, 0, 118), 1.4, (0.80, 0.88, 1.0)),
                           ("r", (-46, 0, 14), 2.0, (1.0, 0.93, 0.85))):
        d = bpy.data.lights.new(n, "SUN"); d.energy = e; d.color = col; d.angle = math.radians(18)
        o = bpy.data.objects.new(n, d); o.rotation_euler = tuple(math.radians(a) for a in rot)
        o.visible_camera = False
        sc.collection.objects.link(o)

    w = bpy.data.worlds.new("w"); sc.world = w; w.use_nodes = True
    bg = w.node_tree.nodes["Background"]
    bg.inputs["Color"].default_value = (0.55, 0.60, 0.66, 1)
    bg.inputs["Strength"].default_value = 0.55

    cd = bpy.data.cameras.new("cam")
    cd.type = "ORTHO"
    # inquadratura: la lunghezza dell'arma più un margine; l'altezza segue il formato
    cd.ortho_scale = max((mx - mn).y, (mx - mn).z * 2.0) * 1.12
    cd.clip_start = 0.01
    cam = bpy.data.objects.new("cam", cd)
    cam.location = (0.0, -2.0, 0.0)
    sc.collection.objects.link(cam)
    sc.camera = cam
    t = bpy.data.objects.new("t", None); t.location = (0, 0, 0)
    sc.collection.objects.link(t)
    con = cam.constraints.new("TRACK_TO"); con.target = t
    con.track_axis = "TRACK_NEGATIVE_Z"; con.up_axis = "UP_Y"

    sc.render.filepath = os.path.join(OUT, wid + ".png")
    bpy.ops.render.render(write_still=True)
    print("THUMB", wid)


for wid in (sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else ORDER):
    render(wid)
