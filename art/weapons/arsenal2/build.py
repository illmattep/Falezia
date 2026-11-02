"""
FALEZIA · art/weapons/arsenal2/build.py — costruisce, anima ed esporta l'arsenale.

    blender -b --factory-startup -P art/weapons/arsenal2/build.py -- standard chatter ...
    blender -b --factory-startup -P art/weapons/arsenal2/build.py            (tutte)

Ogni arma esce in game/assets/weapons/<id>/<id>.glb (materiali e texture dentro) più un
JSON con classe, triangoli e durate delle clip.
"""
import bpy
import math
import os
import sys
import json

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import gunlib as G
from gunlib import Part, empty, clear_scene, GAME

# ------------------------------------------------------------------ specifiche
# Le misure sono quelle vere delle armi che rappresentano; i tempi vengono dal doc 11.
SPECS = {
    # ---------------------------------------------------------- pistole
    "standard": dict(kind="pistol", look="polymer", slide_len=0.186, grip_h=0.086,
                     mats=dict(body="polymer", metal="steel", dark="stipple", accent="steel"),
                     grip_angle=17, width=0.030, mag_len=0.094,
                     class_label="Pistola", cycle=0.25, reload_t=1.50, reload_empty_t=2.00),
    "hush": dict(kind="pistol", look="polymer", slide_len=0.186, grip_h=0.086,
                 mats=dict(body="polymer", metal="steel", dark="stipple", accent="stainless"),
                 grip_angle=17, width=0.032, mag_len=0.094, suppressor=True, supp_len=0.150,
                 class_label="Pistola silenziata", cycle=0.273, reload_t=1.70, reload_empty_t=2.20),
    "chatter": dict(kind="pistol", look="polymer", slide_len=0.172, grip_h=0.082,
                    mats=dict(body="polymer", metal="steel", dark="stipple", accent="brass"),
                    grip_angle=15, width=0.033, mag_len=0.128, comp=True,
                    class_label="Pistola automatica", cycle=0.097, reload_t=1.60, reload_empty_t=2.10),
    "snub": dict(kind="revolver", look="steel", barrel=0.048, drum_r=0.019, width=0.034,
                 mats=dict(body="stainless", metal="steel", dark="rubber", accent="brass"),
                 grip_h=0.084, grip_angle=19, class_label="Rivoltella",
                 cycle=0.333, travel=0.0, mag_out=0.02, reload_t=1.95, reload_empty_t=2.35),
    "hammer": dict(kind="revolver", look="steel", barrel=0.170, drum_r=0.0235, width=0.040,
                   mats=dict(body="stainless", metal="steel", dark="leather", accent="brass"),
                   rib=True, grip_h=0.098, grip_angle=17, class_label="Magnum",
                   cycle=0.667, travel=0.0, mag_out=0.02, reload_t=2.35, reload_empty_t=2.80),

    # ---------------------------------------------------------- SMG / PDW
    "cadence": dict(kind="long", look="polymer", recv=(0.0, 0.250), recv_z=(-0.006, 0.074),
                    mats=dict(body="polymer", metal="steel", dark="rubber", accent="steel"),
                    barrel=0.185, barrel_r=0.0095, width=0.046, stock="folding", stock_len=0.205,
                    mag_type="banana", mag_len=0.230, mag_depth=0.058, mag_y=0.150, mag_curve=22,
                    optic="dot", dot_y=0.120, charging="right", trig_y=0.082,
                    class_label="SMG", cycle=0.115, reload_t=1.85, reload_empty_t=2.25),
    "scrap": dict(kind="long", look="sheet", recv=(-0.008, 0.235), recv_z=(-0.014, 0.066),
                  mats=dict(body="sheet", metal="steel", dark="rubber", accent="cloth", recv="sheet"),
                  barrel=0.150, barrel_r=0.0105, width=0.050, stock="wire", stock_len=0.200,
                  mag_type="box", mag_len=0.175, mag_angle=8, mag_y=0.140,
                  handguard=False, sheet_fold=True, grip_tape=True, grip_angle=19,
                  optic="irons", charging="right", trig_y=0.086,
                  class_label="SMG economica", cycle=0.100, reload_t=1.75, reload_empty_t=2.10),
    "blitz": dict(kind="long", look="green", recv=(0.0, 0.280), recv_z=(-0.008, 0.076),
                  mats=dict(body="green", metal="steel", dark="stipple", accent="cloth", recv="green"),
                  barrel=0.190, barrel_r=0.0095, width=0.050, stock="bullpup", stock_len=0.03,
                  mag_type="horizontal", handguard=True, hg_r=0.024, hg_frac=0.55,
                  # il caricatore orizzontale sta SOPRA il castello: il dot sale su un
                  # rialzo, altrimenti la linea di mira passa dentro il caricatore
                  optic="dot", dot_y=0.230, dot_rise=0.016, rail="top", charging="top", trig_y=0.060,
                  grip_y=0.030, class_label="PDW", cycle=0.086, reload_t=1.80, reload_empty_t=2.20),

    # ---------------------------------------------------------- pompe
    "choke": dict(kind="long", look="wood", recv=(0.0, 0.235), recv_z=(-0.004, 0.072),
                  mats=dict(body="wood", metal="steel", dark="leather", accent="brass", recv="steel"),
                  barrel=0.600, barrel_r=0.0140, width=0.046, stock="fixed", stock_len=0.320,
                  mag_type=None, handguard=False, pump=True, pump_len=0.200, pump_y=0.150,
                  gas_block=False, hg_rail=False, optic="irons", trig_y=0.090,
                  class_label="Pompa", cycle=1.09, reload_t=2.60, reload_empty_t=3.10),
    "sweeper": dict(kind="long", look="polymer", recv=(0.0, 0.280), recv_z=(-0.008, 0.082),
                    mats=dict(body="polymer", metal="steel", dark="rubber", accent="steel"),
                    barrel=0.440, barrel_r=0.0140, width=0.052, stock="fixed", stock_len=0.250,
                    mag_type="box", mag_len=0.200, mag_y=0.175, carry_handle=False,
                    gas_block=True, gas_frac=0.55, hg_r=0.030,
                    optic="irons", trig_y=0.086,
                    class_label="Pompa semiautomatico", cycle=0.545, reload_t=2.40, reload_empty_t=2.85),

    # ---------------------------------------------------------- fucili d'assalto
    "anvil": dict(kind="long", look="wood", recv=(0.0, 0.265), recv_z=(-0.008, 0.078),
                  mats=dict(body="wood", metal="steel", dark="leather", accent="brass", recv="steel"),
                  barrel=0.410, barrel_r=0.0115, width=0.048, stock="fixed", stock_len=0.275,
                  mag_type="banana", mag_len=0.225, mag_angle=5, mag_curve=16, mag_y=0.160,
                  optic="irons", charging="right", trig_y=0.098, gas_block=True,
                  class_label="Fucile d'assalto 7,62", cycle=0.111, reload_t=2.05, reload_empty_t=2.45),
    "service": dict(kind="long", look="polymer", recv=(0.0, 0.258), recv_z=(-0.008, 0.076),
                    mats=dict(body="polymer", metal="steel", dark="stipple", accent="steel"),
                    barrel=0.365, barrel_r=0.0105, width=0.048, stock="fixed", stock_len=0.250,
                    mag_type="box", mag_len=0.190, mag_y=0.158, carry_handle=False,
                    optic="irons", charging="right", trig_y=0.096, gas_block=True,
                    class_label="Fucile d'assalto", cycle=0.098, reload_t=1.95, reload_empty_t=2.35),
    "vantage": dict(kind="long", look="polymer", recv=(0.0, 0.290), recv_z=(-0.010, 0.080),
                    mats=dict(body="polymer", metal="steel", dark="stipple", accent="steel"),
                    barrel=0.490, barrel_r=0.0120, width=0.050, stock="fixed", stock_len=0.265,
                    cheek=True, mag_type="box", mag_len=0.190, mag_y=0.175, optic="scope",
                    scope_len=0.265, scope_y=0.150, charging="right", trig_y=0.100, gas_block=True,
                    class_label="Fucile da battaglia", cycle=0.545, reload_t=2.15, reload_empty_t=2.55),

    # ---------------------------------------------------------- cecchini
    "spotter": dict(kind="long", look="wood", recv=(0.0, 0.300), recv_z=(-0.010, 0.082),
                    mats=dict(body="wood", metal="steel", dark="leather", accent="brass", recv="steel"),
                    barrel=0.530, barrel_r=0.0110, width=0.048, stock="skeleton", stock_len=0.270,
                    mag_type="box", mag_len=0.165, mag_y=0.180, optic="scope", scope_len=0.310,
                    scope_y=0.155, charging="right", trig_y=0.104, gas_block=True,
                    class_label="Cecchino semiautomatico", cycle=0.500, reload_t=2.25, reload_empty_t=2.65),
    "longshot": dict(kind="long", look="polymer", recv=(0.0, 0.310), recv_z=(-0.010, 0.084),
                     mats=dict(body="polymer", metal="steel", dark="rubber", accent="steel"),
                     barrel=0.590, barrel_r=0.0125, width=0.050, stock="fixed", stock_len=0.285,
                     cheek=True, mag_type="box", mag_len=0.150, mag_y=0.190, optic="scope",
                     scope_len=0.335, scope_y=0.160, muzzle_device="brake", charging="right",
                     sling=True, trig_y=0.108, gas_block=False,
                     class_label="Fucile a otturatore", cycle=1.50, reload_t=2.55, reload_empty_t=3.00),

    # ---------------------------------------------------------- mitragliatrice
    "heavy": dict(kind="long", look="sheet", recv=(-0.010, 0.340), recv_z=(-0.016, 0.096),
                  mats=dict(body="sheet", metal="steel", dark="rubber", accent="brass", recv="sheet"),
                  barrel=0.550, barrel_r=0.0155, width=0.062, stock="fixed", stock_len=0.285,
                  cheek=True, mag_type="drum", drum_r=0.066, mag_y=0.230, heat_shield=True,
                  bipod=True, carry_handle=True, belt=True, sheet_fold=True, rail="top",
                  charging="right", trig_y=0.112, gas_block=True, hg_frac=0.42, hg_r=0.032,
                  class_label="Mitragliatrice leggera", cycle=0.091, reload_t=3.35, reload_empty_t=3.90),

    # ---------------------------------------------------------- coltello
    "melee": dict(kind="knife", look="wood", blade=0.170,
                  mats=dict(body="leather", dark="leather", metal="steel", accent="stainless"),
                  class_label="Coltello", cycle=0.60),
}


def travel_of(spec):
    return spec.get("travel", 0.028)


# ------------------------------------------------------------------ animazioni

def add_clips(root, objs, s):
    """Sette clip per arma: riposo, colpo, ultimo colpo, due ricariche, estrazione, riposizione."""
    sc = bpy.context.scene
    sc.render.fps = 30
    kind = s.get("kind")
    moving = ["body"] if kind == "knife" else [n for n in ("slide", "mag", "trigger", "hammer") if n in objs]
    objects = {n: objs[n] for n in moving}
    if kind != "knife" and "slide" not in objects:
        objects["root"] = root
    else:
        objects["root"] = root

    travel = travel_of(s)
    mag_out = s.get("mag_out", 0.145 if kind not in ("revolver",) else 0.02)
    cycle = s.get("cycle", 0.2)
    fire_frames = max(6, min(48, round(cycle * 30) + 2))
    rf = max(24, round(s.get("reload_t", 1.97) * 30) + 1)
    ef = max(rf + 8, round(s.get("reload_empty_t", 2.37) * 30) + 1)

    def at(f, total):
        return max(2, min(total - 1, round(f * (total - 1)) + 1))

    def fcurves_of(act):
        """Blender 5: le curve stanno dentro i channelbag delle action a livelli."""
        if hasattr(act, "fcurves"):
            return list(act.fcurves)
        out = []
        for layer in act.layers:
            for strip in layer.strips:
                for bag in getattr(strip, "channelbags", []):
                    out.extend(bag.fcurves)
        return out

    def key(obj, prop, pairs, interp="BEZIER"):
        for frame, val in pairs:
            setattr(obj, prop, val)
            obj.keyframe_insert(data_path=prop, frame=frame)
        if obj.animation_data and obj.animation_data.action:
            for fc in fcurves_of(obj.animation_data.action):
                for kp in fc.keyframe_points:
                    kp.interpolation = interp
                    kp.handle_left_type = kp.handle_right_type = "AUTO_CLAMPED"

    def slide_cycle(hold):
        if kind == "revolver":
            return {"rotation_euler": [(1, (0, 0, 0)), (2, (0, math.radians(60), 0)),
                                       (fire_frames, (0, math.radians(60), 0))]}
        if hold:
            return {"location": [(1, (0, 0, 0)), (2, (0, -travel, 0)),
                                 (fire_frames, (0, -travel, 0))]}
        return {"location": [(1, (0, 0, 0)), (2, (0, -travel * 0.85, 0)), (3, (0, -travel, 0)),
                             (4, (0, -travel * 0.25, 0)), (5, (0, 0, 0)),
                             (fire_frames, (0, 0, 0))]}

    clips = []

    # ------------------------------------------------------------ riposo (respiro)
    clips.append(dict(name="idle", length=36, tracks={
        "root": {"location": [(1, (0, 0, 0)), (18, (0.0002, 0, -0.0007)), (36, (0, 0, 0))],
                 "rotation_euler": [(1, (0, 0, 0)), (18, (math.radians(0.30), math.radians(0.18), 0)),
                                    (36, (0, 0, 0))]},
    }))

    # ------------------------------------------------------------ colpo
    for nm, hold in (("fire", False), ("fire_last", True)):
        tracks = {}
        if "slide" in objects:
            tracks["slide"] = slide_cycle(hold)
        if "hammer" in objects:
            # cane armato -> percussione al frame 2 -> riarmo lento
            tracks["hammer"] = {"rotation_euler": [(1, (0.40, 0, 0)), (2, (0, 0, 0)),
                                                   (4, (0, 0, 0)), (fire_frames, (0.40, 0, 0))]}
        if "trigger" in objects:
            tracks["trigger"] = {"rotation_euler": [(1, (0, 0, 0)), (2, (-0.22, 0, 0)),
                                                    (4, (-0.22, 0, 0)), (5, (0, 0, 0))]}
        if "mag" in objects and kind != "knife":
            tracks["mag"] = {"location": [(1, (0, 0, 0)), (fire_frames, (0, 0, 0))]}
        if kind == "knife":
            tracks["body"] = {"rotation_euler": [
                (1, (0, 0, 0)),
                (2, (math.radians(-8), math.radians(-26), math.radians(10))),
                (3, (math.radians(26), math.radians(18), math.radians(-14))),
                (4, (math.radians(12), math.radians(6), math.radians(-4))),
                (fire_frames, (0, 0, 0))]}
        else:
            # rinculo della radice: 5 mm indietro e 1,4° di becco
            tracks["root"] = {"location": [(1, (0, 0, 0)), (2, (0, 0.005, 0.0012)),
                                           (4, (0, 0.0022, 0.0004)), (fire_frames, (0, 0, 0))],
                              "rotation_euler": [(1, (0, 0, 0)),
                                                 (2, (math.radians(-1.4), math.radians(0.35), 0)),
                                                 (4, (math.radians(-0.55), 0, 0)),
                                                 (fire_frames, (0, 0, 0))]}
        clips.append(dict(name=nm, length=fire_frames, tracks=tracks))

    # ------------------------------------------------------------ ricarica
    def reload_tracks(total, empty):
        t = {}
        if "mag" in objects and kind != "revolver":
            t["mag"] = {"location": [(1, (0, 0, 0)),
                                     (at(0.10, total), (0, 0, 0)),
                                     (at(0.18, total), (0, -0.004, -mag_out * 0.45)),
                                     (at(0.28, total), (0, -0.010, -mag_out)),
                                     (at(0.50, total), (0, -0.010, -mag_out)),
                                     (at(0.60, total), (0, -0.004, -mag_out * 0.80)),
                                     (at(0.70, total), (0, 0, -0.006)),
                                     (at(0.75, total), (0, 0, 0.0015)),
                                     (total, (0, 0, 0))],
                      "rotation_euler": [(1, (0, 0, 0)),
                                         (at(0.18, total), (math.radians(-4), 0, math.radians(-3))),
                                         (at(0.50, total), (math.radians(-6), 0, math.radians(-5))),
                                         (at(0.72, total), (0, 0, 0))]}
        if kind == "revolver":
            t["slide"] = {"rotation_euler": [(1, (0, 0, 0)), (at(0.30, total), (0, math.radians(200), 0)),
                                             (at(0.80, total), (0, math.radians(200), 0)),
                                             (total, (0, 0, 0))]}
        if "slide" in objects and kind not in ("revolver", "knife"):
            if empty:
                # otturatore aperto fino alla fine: si chiude con lo sgancio
                t["slide"] = {"location": [(1, (0, -travel, 0)), (at(0.82, total), (0, -travel, 0)),
                                           (at(0.86, total), (0, -travel * 0.30, 0)),
                                           (at(0.90, total), (0, 0.0012, 0)),
                                           (total, (0, 0, 0))]}
            else:
                t["slide"] = {"location": [(1, (0, 0, 0)), (total, (0, 0, 0))]}
        if "hammer" in objects:
            t["hammer"] = {"rotation_euler": [(1, (0.40, 0, 0)), (at(0.72, total), (0.40, 0, 0)),
                                              (at(0.80, total), (0, 0, 0)), (total, (0.40, 0, 0))]}
        if "trigger" in objects:
            t["trigger"] = {"rotation_euler": [(1, (0, 0, 0)), (total, (0, 0, 0))]}
        # l'arma si abbassa e ruota mentre ricarichi: è quello che rende viva l'animazione
        t["root"] = {"location": [(1, (0, 0, 0)), (at(0.14, total), (0.004, 0.006, -0.014)),
                                  (at(0.64, total), (0.004, 0.006, -0.014)), (at(0.94, total), (0, 0, 0))],
                     "rotation_euler": [(1, (0, 0, 0)),
                                        (at(0.14, total), (math.radians(-5), math.radians(-2), math.radians(8))),
                                        (at(0.64, total), (math.radians(-5), math.radians(-2), math.radians(8))),
                                        (at(0.94, total), (0, 0, 0))]}
        return t

    clips.append(dict(name="reload", length=rf, tracks=reload_tracks(rf, False)))
    clips.append(dict(name="reload_empty", length=ef, tracks=reload_tracks(ef, True)))

    # ------------------------------------------------------------ estrazione / riposizione
    for nm, upward in (("draw", True), ("holster", False)):
        ln = 15
        a, b = (1, ln) if upward else (ln, 1)
        low = (0, 0.055, -0.035) if kind != "knife" else (0, 0.02, -0.10)
        rot = (math.radians(17), 0, math.radians(-10))
        t = {"root": {"location": [(a, low), (b, (0, 0, 0))] if upward
                      else [(1, (0, 0, 0)), (ln, low)],
                      "rotation_euler": [(a, rot), (b, (0, 0, 0))] if upward
                      else [(1, (0, 0, 0)), (ln, rot)]}}
        for n in ("slide", "mag", "trigger", "hammer"):
            if n in objects:
                t[n] = {"location": [(1, (0, 0, 0)), (ln, (0, 0, 0))]}
        clips.append(dict(name=nm, length=ln, tracks=t))

    # ------------------------------------------------------------ applica
    for ob in objects.values():
        if ob.animation_data:
            ob.animation_data_clear()

    for c in clips:
        for name, ob in objects.items():
            ob.animation_data_create()
            act = bpy.data.actions.new("%s@%s" % (c["name"], name))
            act.use_fake_user = True
            ob.animation_data.action = act
            tracks = c["tracks"].get(name, {})
            if not tracks:
                ob.keyframe_insert("location", frame=1)
                ob.keyframe_insert("location", frame=c["length"])
            else:
                for prop, pairs in tracks.items():
                    key(ob, prop, pairs)
            ob.animation_data.action = None
            tr = ob.animation_data.nla_tracks.new()
            tr.name = c["name"]
            st = tr.strips.new(c["name"], 1, act)
            st.frame_start = 1
            st.frame_end = c["length"]
            st.name = c["name"]
            st.blend_type = "REPLACE"

    # le tracce NLA restano mute: l'esportatore le accende una alla volta per le clip,
    # intanto la posa di riposo resta pulita (senza questo il GLB nasce in ricarica)
    for ob in objects.values():
        if ob.animation_data:
            for tr in ob.animation_data.nla_tracks:
                tr.mute = True
    sc.frame_start = 1
    sc.frame_end = 120
    sc.frame_set(1)
    return {c["name"]: round((c["length"] - 1) / 30.0, 3) for c in clips}


# ------------------------------------------------------------------ costruzione

def build(wid):
    sp = SPECS[wid]
    clear_scene()

    root = bpy.data.objects.new(wid, None)
    root.empty_display_size = 0.05
    bpy.context.scene.collection.objects.link(root)

    parts = {n: Part(n) for n in ("body", "slide", "mag", "trigger", "hammer")}
    kind = sp["kind"]
    if kind == "pistol":
        info = G.pistol(parts, sp)
    elif kind == "revolver":
        info = G.revolver(parts, sp)
    elif kind == "knife":
        info = G.knife(parts, sp)
    else:
        info = G.long_gun(parts, sp)

    objs = {}
    for n, p in parts.items():
        if len(p.bm.verts) == 0:
            p.bm.free()
            continue
        objs[n] = p.finish(root)

    sockets = {
        "socket_muzzle": info["muzzle"],
        "socket_eject": info["eject"],
        "socket_aim": info["aim"],
        "socket_eye": info.get("eye", (0.0, -0.16, info["aim"][2])),
        "socket_hand_r": info.get("hand_r", (0.0, -0.045, -0.075)),
        "socket_hand_l": info.get("hand_l", (0.0, 0.16, -0.01)),
    }
    for name, loc in sockets.items():
        empty(root, name, loc)

    lengths = add_clips(root, objs, sp)

    tris = 0
    dg = bpy.context.evaluated_depsgraph_get()
    for ob in objs.values():
        me = ob.evaluated_get(dg).to_mesh()
        me.calc_loop_triangles()
        tris += len(me.loop_triangles)
        ob.evaluated_get(dg).to_mesh_clear()

    out = os.path.join(GAME, wid)
    os.makedirs(out, exist_ok=True)
    bpy.ops.export_scene.gltf(
        filepath=os.path.join(out, wid + ".glb"),
        export_format="GLB",
        use_selection=False,
        export_apply=True,
        export_yup=True,
        export_animations=True,
        export_animation_mode="NLA_TRACKS",
        export_nla_strips=True,
        export_bake_animation=False,
        export_optimize_animation_size=False,
        export_optimize_animation_keep_anim_object=True,
        export_anim_slide_to_zero=True,
        export_materials="EXPORT",
        export_image_format="JPEG",
        export_image_quality=92,
        export_texcoords=True,
        export_normals=True,
        export_extras=True,
        export_cameras=False,
        export_lights=False,
    )

    data = {
        "id": wid,
        "class": sp.get("class_label", ""),
        "tris": tris,
        "animations": {k: {"length_s": v} for k, v in lengths.items()},
    }
    json.dump(data, open(os.path.join(out, wid + ".json"), "w"), indent=2)
    size = os.path.getsize(os.path.join(out, wid + ".glb")) // 1024
    print("BUILT %-9s tris=%-6d %4d KB" % (wid, tris, size))
    return data


# ------------------------------------------------------------------ main

argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
targets = argv or list(SPECS.keys())
for wid in targets:
    if wid not in SPECS:
        print("SKIP (arma sconosciuta):", wid)
        continue
    build(wid)
print("DONE", len(targets))
