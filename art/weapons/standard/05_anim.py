"""
FALEZIA · art/weapons/standard · 05_anim.py
Anima solo i pezzi meccanici (niente mani, niente rinculo): 30 fps.
Azioni: idle, fire, fire_last, reload, reload_empty, draw, holster.
Ogni azione viene messa su una NLA track con lo stesso nome su ogni pezzo:
l'export glTF (NLA_TRACKS) le unisce in una animazione per nome.
"""
import bpy, math, os, json
from mathutils import Vector, Matrix

ROOT = "/home/matteo/Progetti/Falezia/art/weapons/standard"
bpy.ops.wm.open_mainfile(filepath=os.path.join(ROOT, "standard.blend"))
sc = bpy.context.scene
sc.render.fps = 30

OBJS = {n: bpy.data.objects[n] for n in ("body", "slide", "trigger", "hammer", "mag")}

def mm(v):
    return v / 1000.0

def set_origin(ob, pivot_mm):
    p = Vector([mm(v) for v in pivot_mm])
    ob.data.transform(Matrix.Translation(-p))
    ob.location = p

# perni reali
set_origin(OBJS["hammer"], (0, -18.5, 36.0))
set_origin(OBJS["trigger"], (0, 26.5, 8.5))

SLIDE_BACK = mm(28)
HAMMER_COCKED = math.radians(26)
TRIGGER_PULL = math.radians(-13)
MAG_OUT = mm(130)

# ---------------------------------------------------------------- keyframe model
# ogni clip: {oggetto: {prop: [(frame, valore), ...]}}
def clip(name, length, tracks):
    return {"name": name, "length": length, "tracks": tracks}

REST = {
    "slide": {"location": [(1, (0, 0, 0))]},
    "hammer": {"rotation_euler": [(1, (0, 0, 0))]},
    "trigger": {"rotation_euler": [(1, (0, 0, 0))]},
    "mag": {"location": [(1, (0, 0, 0))]},
}

def slide_path(frames):
    return [(f, (0, y, 0)) for f, y in frames]

CLIPS = []

# --- idle: statica
CLIPS.append(clip("idle", 2, {
    "slide": {"location": [(1, (0, 0, 0)), (2, (0, 0, 0))]},
    "hammer": {"rotation_euler": [(1, (HAMMER_COCKED, 0, 0)), (2, (HAMMER_COCKED, 0, 0))]},
    "trigger": {"rotation_euler": [(1, (0, 0, 0)), (2, (0, 0, 0))]},
    "mag": {"location": [(1, (0, 0, 0)), (2, (0, 0, 0))]},
}))

# --- fire: ciclo carrello + cane riarmato (6 frame = 0.167 s)
CLIPS.append(clip("fire", 6, {
    "slide": {"location": slide_path([(1, 0), (2, -SLIDE_BACK * 0.35), (3, -SLIDE_BACK),
                                      (4, -SLIDE_BACK), (5, -SLIDE_BACK * 0.3), (6, 0)])},
    "hammer": {"rotation_euler": [(1, (HAMMER_COCKED, 0, 0)), (2, (0, 0, 0)),
                                  (3, (0, 0, 0)), (4, (HAMMER_COCKED * 0.5, 0, 0)),
                                  (6, (HAMMER_COCKED, 0, 0))]},
    "trigger": {"rotation_euler": [(1, (0, 0, 0)), (2, (TRIGGER_PULL, 0, 0)),
                                   (4, (TRIGGER_PULL, 0, 0)), (6, (0, 0, 0))]},
    "mag": {"location": [(1, (0, 0, 0)), (6, (0, 0, 0))]},
}))

# --- fire_last: carrello che resta aperto (6 frame)
CLIPS.append(clip("fire_last", 6, {
    "slide": {"location": slide_path([(1, 0), (2, -SLIDE_BACK * 0.35), (3, -SLIDE_BACK),
                                      (6, -SLIDE_BACK)])},
    "hammer": {"rotation_euler": [(1, (HAMMER_COCKED, 0, 0)), (2, (0, 0, 0)),
                                  (3, (0, 0, 0)), (4, (HAMMER_COCKED * 0.5, 0, 0)),
                                  (6, (HAMMER_COCKED, 0, 0))]},
    "trigger": {"rotation_euler": [(1, (0, 0, 0)), (2, (TRIGGER_PULL, 0, 0)),
                                   (4, (TRIGGER_PULL, 0, 0)), (6, (0, 0, 0))]},
    "mag": {"location": [(1, (0, 0, 0)), (6, (0, 0, 0))]},
}))

# --- reload: 60 frame = 2.0 s ; mag_out a 0.47 s, mag_in a 1.47 s
reload_keys = [(1, (0, 0, 0)), (10, (0, 0, 0)), (14, (0, 0, -MAG_OUT * 0.35)),
               (18, (0, 0, -MAG_OUT)), (20, (0, 0, -MAG_OUT * 1.08)),
               (21, (0, 0, -MAG_OUT * 1.08)), (22, (0, 0, -MAG_OUT * 1.15)),
               (34, (0, 0, -MAG_OUT * 0.55)), (42, (0, 0, -mm(6))),
               (45, (0, 0, 0)), (48, (0, 0, 0)), (60, (0, 0, 0))]
CLIPS.append(clip("reload", 60, {
    "slide": {"location": [(1, (0, 0, 0)), (60, (0, 0, 0))]},
    "hammer": {"rotation_euler": [(1, (HAMMER_COCKED, 0, 0)), (60, (HAMMER_COCKED, 0, 0))]},
    "trigger": {"rotation_euler": [(1, (0, 0, 0)), (60, (0, 0, 0))]},
    "mag": {"location": reload_keys},
}))

# --- reload_empty: 72 frame = 2.37 s ; + slide release a 1.97 s
CLIPS.append(clip("reload_empty", 72, {
    "slide": {"location": slide_path([(1, -SLIDE_BACK), (56, -SLIDE_BACK), (60, -SLIDE_BACK),
                                      (62, -SLIDE_BACK * 0.45), (66, 0), (72, 0)])},
    "hammer": {"rotation_euler": [(1, (HAMMER_COCKED, 0, 0)), (72, (HAMMER_COCKED, 0, 0))]},
    "trigger": {"rotation_euler": [(1, (0, 0, 0)), (72, (0, 0, 0))]},
    "mag": {"location": reload_keys[:11] + [(60, (0, 0, 0)), (72, (0, 0, 0))]},
}))

# --- draw / holster: statiche, solo finestra temporale (il movimento lo fa il gioco)
for nm, ln in (("draw", 15), ("holster", 15)):
    CLIPS.append(clip(nm, ln, {
        "slide": {"location": [(1, (0, 0, 0)), (ln, (0, 0, 0))]},
        "hammer": {"rotation_euler": [(1, (HAMMER_COCKED, 0, 0)), (ln, (HAMMER_COCKED, 0, 0))]},
        "trigger": {"rotation_euler": [(1, (0, 0, 0)), (ln, (0, 0, 0))]},
        "mag": {"location": [(1, (0, 0, 0)), (ln, (0, 0, 0))]},
    }))

# ---------------------------------------------------------------- applica
for ob in OBJS.values():
    if ob.animation_data:
        ob.animation_data_clear()

for c in CLIPS:
    for obname in ("body", "slide", "trigger", "hammer", "mag"):
        ob = OBJS[obname]
        ob.animation_data_create()
        act = bpy.data.actions.new("%s@%s" % (c["name"], obname))
        act.use_fake_user = True
        ob.animation_data.action = act
        tracks = c["tracks"].get(obname, {})
        if not tracks:
            # pezzo fermo: una chiave per tenere la posa
            base = {"body": "location"}.get(obname)
            if base:
                act.keyframe_insert  # no-op
            ob.keyframe_insert("location", frame=1)
            ob.keyframe_insert("location", frame=c["length"])
        for prop, keys in tracks.items():
            for f, val in keys:
                setattr(ob, prop, val)
                ob.keyframe_insert(data_path=prop, frame=f)
        ob.animation_data.action = None
        tr = ob.animation_data.nla_tracks.new()
        tr.name = c["name"]
        st = tr.strips.new(c["name"], 1, act)
        st.frame_start = 1
        st.frame_end = c["length"]
        st.name = c["name"]
        st.blend_type = 'REPLACE'

sc.frame_start = 1
sc.frame_end = 72
sc.frame_set(1)

# eventi (secondi dall'inizio clip)
EVENTS = {
    "idle": {},
    "fire": {"eject": 2 / 30.0},
    "fire_last": {"eject": 2 / 30.0},
    "reload": {"mag_out": 13 / 30.0, "mag_in": 44 / 30.0},
    "reload_empty": {"mag_out": 13 / 30.0, "mag_in": 44 / 30.0, "slide_release": 59 / 30.0},
    "draw": {},
    "holster": {},
}
LENGTHS = {c["name"]: (c["length"] - 1) / 30.0 for c in CLIPS}

out = {"lengths": LENGTHS, "events": EVENTS}
json.dump(out, open(os.path.join(ROOT, "review", "05_anim_report.json"), "w"), indent=2)
print("ANIM " + json.dumps(out))
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(ROOT, "standard.blend"))
