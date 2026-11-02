import bpy, math, os
from mathutils import Vector

SRC = "/home/matteo/Scaricati/Meshy_AI_tactical_mechanic_her_1026212040_image-to-3d-texture.glb"
OUT = "/tmp/op"

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=SRC)
o = [x for x in bpy.context.scene.objects if x.type == 'MESH'][0]
o.name = "Operatore"

# scala: piedi a terra, corpo 1,70 m (l'antenna della radio sporge oltre)
bb = [o.matrix_world @ Vector(c) for c in o.bound_box]
zmin = min(v.z for v in bb); zmax = max(v.z for v in bb)
s = 1.80 / (zmax - zmin)
o.scale = (s, s, s); bpy.context.view_layer.update()
bb = [o.matrix_world @ Vector(c) for c in o.bound_box]
o.location.z = -min(v.z for v in bb)
bpy.context.view_layer.update()
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)

# decimazione PRIMA dei pesi: si pesano i vertici che finiscono in gioco.
# Le MANI vanno protette: su un corpo da 20 000 triangoli a una mano toccano ~300,
# e le dita si sbriciolano appena si chiudono. Un gruppo di peso dice al decimatore
# di lasciare i triangoli lì.
target_tris = 24000
tris = len(o.data.polygons)

keep = o.vertex_groups.new(name="keephands")
wristL = Vector((-0.60, 0.02, 0.88))
wristR = Vector((0.60, 0.02, 0.88))
hand_verts = [v.index for v in o.data.vertices
              if (v.co - wristL).length < 0.19 or (v.co - wristR).length < 0.19]
keep.add(hand_verts, 1.0, 'REPLACE')
print("vertici delle mani protetti:", len(hand_verts))

mod = o.modifiers.new("dec", 'DECIMATE')
mod.decimate_type = 'COLLAPSE'
mod.vertex_group = "keephands"
mod.invert_vertex_group = True       # il peso 1 sulla mano DISATTIVA la decimazione lì dentro
mod.vertex_group_factor = 1.0
mod.ratio = min(1.0, target_tris / max(1, tris))
bpy.context.view_layer.objects.active = o
bpy.ops.object.modifier_apply(modifier="dec")
print(f"DECIMATO: {tris} → {len(o.data.polygons)} triangoli, {len(o.data.vertices)} vertici")

# ---------------------------------------------------------------- scheletro
# giunti misurati sul mesh (piedi a z=0): anche, ginocchia, caviglie, spalle, gomiti, polsi
J = {
    "pelvis":   ((0.00,  0.05, 0.88), (0.00,  0.03, 1.02)),
    "spine":    ((0.00,  0.03, 1.02), (0.00,  0.00, 1.20)),
    "chest":    ((0.00,  0.00, 1.20), (0.00, -0.01, 1.38)),
    "neck":     ((0.00, -0.01, 1.38), (0.00, -0.02, 1.45)),
    "head":     ((0.00, -0.02, 1.45), (0.00, -0.02, 1.66)),
    "clav.L":   ((-0.05, 0.02, 1.35), (-0.20, 0.02, 1.40)),
    "arm.L":    ((-0.20, 0.02, 1.40), (-0.42, 0.02, 1.17)),
    "fore.L":   ((-0.42, 0.02, 1.17), (-0.60, 0.02, 0.88)),
    "hand.L":   ((-0.60, 0.02, 0.88), (-0.66, 0.02, 0.78)),
    "clav.R":   (( 0.05, 0.02, 1.35), ( 0.20, 0.02, 1.40)),
    "arm.R":    (( 0.20, 0.02, 1.40), ( 0.42, 0.02, 1.17)),
    "fore.R":   (( 0.42, 0.02, 1.17), ( 0.60, 0.02, 0.88)),
    "hand.R":   (( 0.60, 0.02, 0.88), ( 0.66, 0.02, 0.78)),
    "thigh.L":  ((-0.12, 0.03, 0.86), (-0.13, 0.02, 0.47)),
    "shin.L":   ((-0.13, 0.02, 0.47), (-0.14, 0.02, 0.09)),
    "foot.L":   ((-0.14, 0.02, 0.09), (-0.14, -0.18, 0.05)),
    "thigh.R":  (( 0.12, 0.03, 0.86), ( 0.13, 0.02, 0.47)),
    "shin.R":   (( 0.13, 0.02, 0.47), ( 0.14, 0.02, 0.09)),
    "foot.R":   (( 0.14, 0.02, 0.09), ( 0.14, -0.18, 0.05)),
}
PARENT = {
    "spine": "pelvis", "chest": "spine", "neck": "chest", "head": "neck",
    "clav.L": "chest", "arm.L": "clav.L", "fore.L": "arm.L", "hand.L": "fore.L",
    "clav.R": "chest", "arm.R": "clav.R", "fore.R": "arm.R", "hand.R": "fore.R",
    "thigh.L": "pelvis", "shin.L": "thigh.L", "foot.L": "shin.L",
    "thigh.R": "pelvis", "shin.R": "thigh.R", "foot.R": "shin.R",
}

# --- DITA: quattro dita a due falangi più il pollice, per mano.
# La scansione ha le mani aperte e le dita fuse nella geometria: le ossa si mettono lo stesso,
# e con un peso corto (σ 2 cm) la mano si chiude a pugno in modo credibile.
FINGERS = [("index", -0.030), ("middle", -0.010), ("ring", 0.010), ("pinky", 0.030)]
for side in ("L", "R"):
    wrist = Vector(J[f"hand.{side}"][0])
    tip = Vector(J[f"hand.{side}"][1])
    d = (tip - wrist).normalized()
    knuckle = wrist + d * 0.085

    for fname, off in FINGERS:
        p0 = knuckle + Vector((0.0, off, 0.0))
        p1 = p0 + d * 0.042
        p2 = p1 + d * 0.032
        J[f"{fname}.01.{side}"] = (tuple(p0), tuple(p1))
        J[f"{fname}.02.{side}"] = (tuple(p1), tuple(p2))
        PARENT[f"{fname}.01.{side}"] = f"hand.{side}"
        PARENT[f"{fname}.02.{side}"] = f"{fname}.01.{side}"

    t0 = wrist + d * 0.030 + Vector((0.0, -0.036, 0.0))
    t1 = t0 + (d * 0.55 + Vector((0.0, -0.72, 0.0))).normalized() * 0.042
    t2 = t1 + (d * 0.45 + Vector((0.0, -0.80, 0.0))).normalized() * 0.030
    J[f"thumb.01.{side}"] = (tuple(t0), tuple(t1))
    J[f"thumb.02.{side}"] = (tuple(t1), tuple(t2))
    PARENT[f"thumb.01.{side}"] = f"hand.{side}"
    PARENT[f"thumb.02.{side}"] = f"thumb.01.{side}"


arm_data = bpy.data.armatures.new("RigOperatore")
rig = bpy.data.objects.new("RigOperatore", arm_data)
bpy.context.scene.collection.objects.link(rig)
bpy.context.view_layer.objects.active = rig
bpy.ops.object.mode_set(mode='EDIT')
eb = arm_data.edit_bones
order = ["pelvis", "spine", "chest", "neck", "head",
         "clav.L", "arm.L", "fore.L", "hand.L",
         "clav.R", "arm.R", "fore.R", "hand.R",
         "thigh.L", "shin.L", "foot.L", "thigh.R", "shin.R", "foot.R"]
order += [n for n in J if n.split(".")[0] in ("thumb", "index", "middle", "ring", "pinky")]
FINGER_BONES = {n for n in order if n.split(".")[0] in ("thumb", "index", "middle", "ring", "pinky")}
for name in order:
    h, t = J[name]
    b = eb.new(name)
    b.head = Vector(h); b.tail = Vector(t)
    if name in PARENT:
        b.parent = eb[PARENT[name]]
        b.use_connect = (Vector(J[PARENT[name]][1]) - Vector(h)).length < 1e-4
bpy.ops.object.mode_set(mode='OBJECT')

# ---------------------------------------------------------------- pesi a mano
# distanza punto-segmento, decadimento gaussiano, i 3 ossi più vicini, lato rispettato.
BONES = [(n, Vector(J[n][0]), Vector(J[n][1])) for n in order]

def seg_dist(p, a, b):
    ab = b - a; t = (p - a).dot(ab) / max(1e-9, ab.length_squared)
    t = max(0.0, min(1.0, t))
    return (p - (a + ab * t)).length

groups = {n: o.vertex_groups.new(name=n) for n in order}
SIGMA = 0.075
SIGMA_FINGER = 0.020          # le dita sono a 2 cm l'una dall'altra: un peso largo le incolla
for v in o.data.vertices:
    p = v.co
    # l'antenna della radio sporge sopra la testa ma è cucita allo ZAINO: va al petto
    if p.z > 1.655 and abs(p.x) > 0.15:
        groups["chest"].add([v.index], 1.0, 'REPLACE')
        continue
    # --- MANI: pesi RIGIDI. Le dita della scansione sono fuse e a 2 cm l'una dall'altra:
    # con pesi sfumati i vertici si dividono fra due dita e la mano si strappa. Un vertice
    # della mano va al 100% all'osso più vicino: si creano pieghe nette, ma la mano resta intera.
    wristL = Vector(J["hand.L"][0]); wristR = Vector(J["hand.R"][0])
    if (p - wristL).length < 0.19 or (p - wristR).length < 0.19:
        side = ".L" if (p - wristL).length < (p - wristR).length else ".R"
        hand_bones = [n for n in order if n in FINGER_BONES or n == ("hand" + side)]
        best = min(((seg_dist(p, Vector(J[n][0]), Vector(J[n][1])), n) for n in hand_bones))
        groups[best[1]].add([v.index], 1.0, 'REPLACE')
        continue

    cand = []
    for name, a, b in BONES:
        if name.endswith(".L") and p.x > 0.06:  continue     # niente sangue fra i due lati
        if name.endswith(".R") and p.x < -0.06: continue
        if name in FINGER_BONES: continue                    # le dita sono già state assegnate

        d = seg_dist(p, a, b)
        w = math.exp(-(d / SIGMA) ** 2)
        if w > 0.02: cand.append((w, name))
    if not cand:
        # fuori da tutto: si attacca all'osso più vicino, senza sfumatura
        best = min(((seg_dist(p, a, b), n) for n, a, b in BONES))
        groups[best[1]].add([v.index], 1.0, 'REPLACE')
        continue
    cand.sort(reverse=True)
    cand = cand[:3]
    tot = sum(w for w, _ in cand)
    for w, name in cand:
        groups[name].add([v.index], w / tot, 'REPLACE')

o.parent = rig
o.matrix_parent_inverse = rig.matrix_world.inverted()
amod = o.modifiers.new("armatura", 'ARMATURE')
amod.object = rig
print("PESI: fatto", len(groups), "gruppi")

# ---------------------------------------------------------------- posa di prova
bpy.context.view_layer.objects.active = rig
bpy.ops.object.mode_set(mode='POSE')
pb = rig.pose.bones
from mathutils import Matrix
def rot_world(name, axis, deg):
    """Rotazione attorno a un asse del MONDO, convertita nello spazio locale dell'osso."""
    b = pb[name]
    M = b.bone.matrix_local.to_3x3()
    Rw = Matrix.Rotation(math.radians(deg), 3, axis)
    b.rotation_mode = 'QUATERNION'
    b.rotation_quaternion = (M.inverted() @ Rw @ M).to_quaternion()

# posa da gioco: braccia giù (dalla A-pose), gomiti piegati, ginocchia flesse
rot_world("clav.L", 'X', 5); rot_world("clav.R", 'X', 5)
rot_world("arm.L", 'Y', -22); rot_world("arm.R", 'Y', 22)
rot_world("fore.L", 'Y', -14); rot_world("fore.R", 'Y', 14)
rot_world("thigh.L", 'X', -22); rot_world("thigh.R", 'X', 16)
rot_world("shin.L", 'X', 36); rot_world("shin.R", 'X', -8)
rot_world("head", 'Z', 16)

# pugno: le dita si chiudono verso il palmo (per vedere se i pesi tengono)
import mathutils
def curl(side, amount=68):
    for fname in ("index", "middle", "ring", "pinky"):
        for seg in ("01", "02"):
            n = f"{fname}.{seg}.{side}"
            b = pb[n]
            M = b.bone.matrix_local.to_3x3()
            # asse di flessione: perpendicolare alla mano, nel piano del palmo
            axis = (M.inverted() @ mathutils.Vector((0, 1, 0)))
            b.rotation_mode = 'QUATERNION'
            b.rotation_quaternion = mathutils.Quaternion(axis.normalized(), math.radians(amount))
    for seg, ang in (("01", 45), ("02", 35)):
        n = f"thumb.{seg}.{side}"
        b = pb[n]
        M = b.bone.matrix_local.to_3x3()
        axis = (M.inverted() @ mathutils.Vector((1, 0, 0)))
        b.rotation_mode = 'QUATERNION'
        b.rotation_quaternion = mathutils.Quaternion(axis.normalized(), math.radians(ang))

curl("R", 68)
curl("L", 68)
bpy.context.view_layer.update()

bpy.ops.object.mode_set(mode='OBJECT')
sc = bpy.context.scene
sc.render.engine = 'CYCLES'; sc.cycles.samples = 20; sc.cycles.device = 'CPU'
sc.render.resolution_x = 480; sc.render.resolution_y = 640
w = bpy.data.worlds.new("w"); sc.world = w; w.use_nodes = True
w.node_tree.nodes["Background"].inputs[0].default_value = (0.05, 0.055, 0.06, 1)
for (loc, en) in [((2.5, -3.0, 3.2), 900), ((-3.0, -2.0, 2.0), 350), ((0.0, 3.0, 2.5), 300)]:
    l = bpy.data.lights.new("L", 'AREA'); l.energy = en; l.size = 3.0
    ob = bpy.data.objects.new("L", l); sc.collection.objects.link(ob); ob.location = loc
    ob.rotation_euler = (Vector((0, 0, 1.0)) - Vector(loc)).to_track_quat('-Z', 'Y').to_euler()
cd = bpy.data.cameras.new("cam"); cd.lens = 60
cam = bpy.data.objects.new("cam", cd); sc.collection.objects.link(cam); sc.camera = cam
for name, ang, dist in (("pose_front", 0, 3.4), ("pose_34", 40, 3.4), ("pose_side", 90, 3.4)):
    a = math.radians(ang)
    cam.location = (math.sin(a) * dist, -math.cos(a) * dist, 1.05)
    cam.rotation_euler = (Vector((0, 0, 0.95)) - cam.location).to_track_quat('-Z', 'Y').to_euler()
    sc.render.filepath = os.path.join(OUT, name + ".png")
    bpy.ops.render.render(write_still=True)
    print("render", name)

# posa accovacciata (per controllare le ginocchia)
for n in ("thigh.L","thigh.R","shin.L","shin.R","arm.L","arm.R","fore.L","fore.R","head","clav.L","clav.R"):
    pb[n].rotation_mode = 'QUATERNION'; pb[n].rotation_quaternion = (1,0,0,0)
rot_world("thigh.L", 'X', -62); rot_world("thigh.R", 'X', -62)
rot_world("shin.L", 'X', 92); rot_world("shin.R", 'X', 92)
rot_world("foot.L", 'X', -28); rot_world("foot.R", 'X', -28)
rot_world("pelvis", 'X', 12)
rot_world("arm.L", 'Y', -18); rot_world("arm.R", 'Y', 18)
rot_world("fore.L", 'Y', -45); rot_world("fore.R", 'Y', 45)
bpy.context.view_layer.update()
bpy.ops.object.mode_set(mode='OBJECT')
cam.location = (2.2, -2.6, 1.0)
cam.rotation_euler = (Vector((0, 0, 0.7)) - cam.location).to_track_quat('-Z', 'Y').to_euler()
sc.render.filepath = os.path.join(OUT, "pose_crouch.png")
bpy.ops.render.render(write_still=True)
print("render pose_crouch")
bpy.ops.object.mode_set(mode='POSE')

# ---------------------------------------------------------------- export (posa di riposo)
bpy.ops.object.mode_set(mode='POSE')
bpy.ops.pose.select_all(action='SELECT')
bpy.ops.pose.transforms_clear()
bpy.ops.object.mode_set(mode='OBJECT')
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT, "operatore.blend"))

# texture: da 4096 a 1024 (il base color era 36 MB da solo). Per un personaggio visto
# a 5–40 m è più che sufficiente, e il file scende sotto i 4 MB.
for img in bpy.data.images:
    w, h = img.size
    if max(w, h) > 1024:
        f = 1024.0 / max(w, h)
        img.scale(max(1, int(w * f)), max(1, int(h * f)))
        print("texture ridotta:", img.name, "→", img.size[:])

bpy.ops.export_scene.gltf(filepath=os.path.join(OUT, "operator.glb"),
                          export_format='GLB', use_selection=False,
                          export_apply=True, export_animations=False,
                          export_image_format='JPEG', export_jpeg_quality=85)
print("EXPORT", os.path.join(OUT, "operator.glb"))
