"""
FALEZIA · art/weapons/hush · 99_validate.py
Controllo automatico. Blocca l'export se una regola non e' rispettata.
Uso standalone:  blender -b --factory-startup -P 99_validate.py
Uso da 06_export.py: importare il modulo e chiamare run_checks().
"""
import bpy, os, json, math

ROOT = "/home/matteo/Progetti/Falezia/art/weapons/hush"
TARGET_MM = {"X": 47.0, "Y": 250.0, "Z": 145.0}   # larghezza, lunghezza, altezza
TOL = 0.02
TRI_BUDGET_VM = 8000
TRI_BUDGET_TP = 1500
NODES = ["hush", "body", "slide", "trigger", "hammer", "mag", "suppressor", "barrel"]
SOCKETS = ["socket_muzzle", "socket_eject", "socket_hand_r", "socket_hand_l",
           "socket_aim", "socket_supp"]
ANIMS = ["idle", "fire", "fire_last", "reload", "reload_empty", "draw", "holster"]

def _dims_mm(objs):
    from mathutils import Vector
    lo = Vector((1e9, 1e9, 1e9)); hi = Vector((-1e9, -1e9, -1e9))
    dg = bpy.context.evaluated_depsgraph_get()
    for o in objs:
        oe = o.evaluated_get(dg); me = oe.to_mesh()
        for v in me.vertices:
            w = oe.matrix_world @ v.co
            for i in range(3):
                lo[i] = min(lo[i], w[i]); hi[i] = max(hi[i], w[i])
        oe.to_mesh_clear()
    return [(hi[i] - lo[i]) * 1000 for i in range(3)]

def _tris(objs):
    t = 0
    dg = bpy.context.evaluated_depsgraph_get()
    for o in objs:
        oe = o.evaluated_get(dg); me = oe.to_mesh()
        me.calc_loop_triangles(); t += len(me.loop_triangles); oe.to_mesh_clear()
    return t

PIVOT = {"hammer": (0, -0.0185, 0.036), "trigger": (0, 0.0265, 0.0085)}
REST_LOC = {"body": (0, 0, 0), "slide": (0, 0, 0), "mag": (0, 0, 0)}

def to_rest_pose():
    """Mette i pezzi nella posa di riposo (misure e trasformate si controllano cosi')."""
    for o in bpy.data.objects:
        ad = o.animation_data
        if ad:
            for tr in ad.nla_tracks:
                tr.mute = True
            ad.action = None
        if o.name in ("body", "slide", "mag", "hammer", "trigger"):
            o.scale = (1, 1, 1)
            o.rotation_euler = (0, 0, 0)
            o.location = PIVOT.get(o.name, REST_LOC.get(o.name, (0, 0, 0)))
    bpy.context.view_layer.update()

def run_checks(scene_objs=None):
    errs = []
    to_rest_pose()
    objs = list(bpy.data.objects)
    by_name = {o.name: o for o in objs}

    # 1. gerarchia
    for n in NODES + SOCKETS:
        if n not in by_name:
            errs.append("nodo mancante: %s" % n)
    if "hush" in by_name:
        root = by_name["hush"]
        for n in NODES[1:] + SOCKETS:
            if n in by_name and by_name[n].parent is not root:
                errs.append("%s non e' figlio di hush" % n)
    # body/slide/trigger/hammer/mag devono essere mesh
    for n in NODES[1:]:
        if n in by_name and by_name[n].type != 'MESH':
            errs.append("%s non e' una mesh" % n)

    # 2. misure
    meshes = [by_name[n] for n in NODES[1:] if n in by_name and by_name[n].type == 'MESH']
    if meshes:
        sx, sy, sz = _dims_mm(meshes)
        got = {"X": sx, "Y": sy, "Z": sz}
        for k, target in TARGET_MM.items():
            if abs(got[k] - target) / target > TOL:
                errs.append("misura %s = %.2f mm, attesa %.2f +/-2%%" % (k, got[k], target))

    # 3. budget
    if meshes:
        t = _tris(meshes)
        if t > TRI_BUDGET_VM:
            errs.append("triangoli viewmodel %d > %d" % (t, TRI_BUDGET_VM))

    # 4. un solo materiale
    mats = set()
    for m in meshes:
        for s in m.material_slots:
            if s.material:
                mats.add(s.material.name)
    if len(mats) != 1:
        errs.append("materiali sulle mesh: %s (atteso 1)" % sorted(mats))
    elif "hush_mat" not in mats:
        errs.append("il materiale non si chiama standard_mat: %s" % sorted(mats))

    # 5. animazioni (NLA track con nome esatto)
    for n in ANIMS:
        found = False
        for o in meshes:
            ad = o.animation_data
            if not ad:
                continue
            for tr in ad.nla_tracks:
                if tr.name == n:
                    found = True
        if not found:
            errs.append("animazione mancante: %s" % n)

    # 6. trasformate applicate
    for o in objs:
        if o.type not in ('MESH', 'EMPTY'):
            continue
        if any(abs(v - 1.0) > 1e-6 for v in o.scale):
            errs.append("scala non applicata su %s: %s" % (o.name, tuple(round(v, 4) for v in o.scale)))
        if any(abs(v) > 1e-6 for v in o.rotation_euler):
            errs.append("rotazione non applicata su %s: %s" % (o.name, tuple(round(v, 4) for v in o.rotation_euler)))

    return errs

if __name__ == "__main__":
    bpy.ops.wm.open_mainfile(filepath=os.path.join(ROOT, "hush.blend"))
    errs = run_checks()
    if errs:
        print("VALIDATE FAIL")
        for e in errs:
            print("  -", e)
        raise SystemExit(2)
    print("VALIDATE OK")
