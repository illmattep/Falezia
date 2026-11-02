"""Costruisce la mappa di Calanera come scena Blender modificabile.

Un oggetto per tipo di volume (edifici, strade, rocce...), un oggetto per le cornici di
gronda, gli empty nominati dei 16 punti notevoli, camera e sole. Geometria creata con
bmesh in blocco: la scena si apre in pochi secondi.

La collisione di Unity resta quella del JSON (`game/maps/calanera.json`): questa scena è
la PELLE della mappa, e si può rimodellare a mano senza rompere gioco, rotte o test.

  blender -b -noaudio --factory-startup --python build_town_blend.py
"""
import bpy, json, bmesh, math

ROOT = "/home/matteo/Progetti/Falezia"
MAP = json.load(open(f"{ROOT}/game/maps/calanera.json"))
KINDS = MAP["kinds"]
OUT = f"{ROOT}/art/map-assets/calanera.blend"
BUILDING = {"plaster", "plaster_ochre", "stone", "concrete"}

COLORS = {
    "plaster": (0.78, 0.70, 0.56, 1), "plaster_ochre": (0.80, 0.55, 0.28, 1),
    "stone": (0.68, 0.65, 0.58, 1), "concrete": (0.55, 0.55, 0.55, 1),
    "rock": (0.30, 0.28, 0.25, 1), "wood": (0.40, 0.26, 0.14, 1),
    "steel": (0.35, 0.38, 0.42, 1), "roof": (0.55, 0.22, 0.12, 1),
    "cover": (0.45, 0.45, 0.45, 1), "street": (0.42, 0.40, 0.37, 1),
    "stairs": (0.60, 0.58, 0.52, 1), "water": (0.10, 0.22, 0.30, 1),
    "objective": (0.90, 0.75, 0.20, 1),
    "team_north": (0.30, 0.60, 0.90, 1), "team_south": (0.90, 0.35, 0.30, 1),
}
FACES = [(0, 1, 3, 2), (4, 6, 7, 5), (0, 4, 5, 1), (2, 3, 7, 6), (0, 2, 6, 4), (1, 5, 7, 3)]


def log(m):
    print("STEP:", m, flush=True)


def material(kind):
    m = bpy.data.materials.get(kind)
    if m:
        return m
    m = bpy.data.materials.new(kind)
    m.use_nodes = True
    b = m.node_tree.nodes["Principled BSDF"]
    b.inputs["Base Color"].default_value = COLORS.get(kind, (0.6, 0.6, 0.6, 1))
    b.inputs["Roughness"].default_value = 0.18 if kind == "water" else 0.85
    return m


def add_box(bm, x0, y0, z0, sx, sy, sz):
    """Blender: Y = -Z del mondo (la mappa ha Y su, Z avanti)."""
    if sx <= 0 or sy <= 0 or sz <= 0:
        return
    cx, cy, cz = x0 + sx / 2, -(z0 + sz / 2), y0 + sy / 2
    vs = [bm.verts.new((cx + dx * sx / 2, cy + dy * sz / 2, cz + dz * sy / 2))
          for dx in (-1, 1) for dy in (-1, 1) for dz in (-1, 1)]
    for f in FACES:
        try:
            bm.faces.new([vs[i] for i in f])
        except ValueError:
            pass


def make_object(name, kind, boxes):
    bm = bmesh.new()
    for b in boxes:
        add_box(bm, *b)
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    ob = bpy.data.objects.new(name, me)
    bpy.context.collection.objects.link(ob)
    ob.data.materials.append(material(kind))
    return ob


def main():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    log("factory")

    per_kind = {}
    for b in MAP["boxes"]:
        per_kind.setdefault(KINDS[b[6]], []).append((b[0], b[1], b[2], b[3], b[4], b[5]))
    for b in MAP["decor"]:
        per_kind.setdefault("decor_" + KINDS[b[6]], []).append((b[0], b[1], b[2], b[3], b[4], b[5]))
    log(f"volumi raccolti: {sum(len(v) for v in per_kind.values())}")

    for kind, boxes in per_kind.items():
        make_object(f"{kind} · {len(boxes)}", kind.replace("decor_", ""), boxes)
    log("oggetti creati")

    cornici = []
    for b in MAP["boxes"]:
        kind = KINDS[b[6]]
        if kind not in BUILDING or b[4] < 5.0:
            continue
        x0, y0, z0, sx, sy, sz = b[0], b[1], b[2], b[3], b[4], b[5]
        cornici.append((x0 - 0.12, y0 + sy - 0.30, z0 - 0.12, sx + 0.24, 0.30, sz + 0.24))
    make_object(f"cornici · {len(cornici)}", "stone", cornici)
    log("cornici create")

    for p in MAP["points"]:
        e = bpy.data.objects.new(f"PT {p['name']}", None)
        e.empty_display_type = 'SINGLE_ARROW'
        e.empty_display_size = 2.5
        e.location = (p["pos"][0], -p["pos"][2], p["pos"][1] + 0.3)
        bpy.context.collection.objects.link(e)
    log("punti creati")

    bpy.ops.object.camera_add(location=(0, -130, 100))
    cam = bpy.context.object
    cam.rotation_euler = (math.radians(40), 0, 0)
    cam.data.lens = 24
    bpy.context.scene.camera = cam
    bpy.ops.object.light_add(type='SUN', location=(40, -40, 80))
    bpy.context.object.data.energy = 3.0
    bpy.context.object.rotation_euler = (math.radians(38), math.radians(20), math.radians(35))

    bpy.ops.wm.save_as_mainfile(filepath=OUT)
    log(f"salvato {OUT}")


main()
