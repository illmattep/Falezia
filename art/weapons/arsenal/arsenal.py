"""
FALEZIA · art/weapons/arsenal/arsenal.py

Genera TUTTI i modelli dell'arsenale mancanti con la stessa grammatica visiva della
Standard e della Hush: masse semplici, spigoli vivi, materiali opachi, niente loghi.

    blender -b --factory-startup -P art/weapons/arsenal/arsenal.py -- chatter cadence ...

Convenzioni (le stesse della Standard): +Y = canna, +Z = alto, origine al centro
dell'impugnatura, metri. Ogni arma esce con:
    body, slide (otturatore/pompa/otturatore rotante), mag, trigger, hammer
    socket_muzzle, socket_eject, socket_aim, socket_hand_r, socket_hand_l
    7 animazioni a 30 fps: idle, fire, fire_last, reload, reload_empty, draw, holster

Le dimensioni vengono da docs/11-armi.md; il colore/il carattere dalle schede
"Estetica" e dalla foto del lineup (polimero nero, legno, acciaio brunito, lamiera).
"""
import bpy, bmesh, math, os, sys, json
from mathutils import Vector, Euler

ROOT = "/home/matteo/Progetti/Falezia"
ART = os.path.join(ROOT, "art", "weapons", "arsenal")
GAME = os.path.join(ROOT, "game", "assets", "weapons")

# ------------------------------------------------------------------ materiali
MATS = {}


def mat(name, color, metallic=0.0, rough=0.7):
    if name in MATS:
        return MATS[name]
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    b = m.node_tree.nodes["Principled BSDF"]
    b.inputs["Base Color"].default_value = (*color, 1.0)
    b.inputs["Metallic"].default_value = metallic
    b.inputs["Roughness"].default_value = rough
    MATS[name] = m
    return m


def palette(kind):
    """I quattro materiali di base di un'arma."""
    if kind == "wood":
        body = mat("wood_walnut", (0.105, 0.052, 0.026), 0.0, 0.55)
        metal = mat("steel_blued", (0.045, 0.05, 0.058), 0.85, 0.42)
        dark = mat("rubber", (0.018, 0.018, 0.02), 0.0, 0.9)
        accent = mat("brass", (0.42, 0.32, 0.12), 1.0, 0.35)
    elif kind == "polymer":
        body = mat("polymer_black", (0.032, 0.033, 0.036), 0.0, 0.62)
        metal = mat("steel_phosphate", (0.075, 0.08, 0.085), 0.9, 0.45)
        dark = mat("rubber", (0.018, 0.018, 0.02), 0.0, 0.9)
        accent = mat("sight_white", (0.80, 0.80, 0.76), 0.0, 0.6)
    elif kind == "steel":
        body = mat("steel_plain", (0.20, 0.21, 0.22), 1.0, 0.38)
        metal = mat("steel_blued", (0.05, 0.055, 0.062), 0.85, 0.42)
        dark = mat("rubber", (0.018, 0.018, 0.02), 0.0, 0.9)
        accent = mat("sight_white", (0.80, 0.80, 0.76), 0.0, 0.6)
    elif kind == "sheet":
        body = mat("sheet_grey", (0.16, 0.165, 0.17), 0.6, 0.62)
        metal = mat("steel_scraped", (0.13, 0.135, 0.14), 0.8, 0.5)
        dark = mat("rubber", (0.018, 0.018, 0.02), 0.0, 0.9)
        accent = mat("tape", (0.22, 0.21, 0.18), 0.0, 0.85)
    elif kind == "green":
        body = mat("polymer_greengrey", (0.075, 0.085, 0.075), 0.0, 0.62)
        metal = mat("steel_phosphate", (0.075, 0.08, 0.085), 0.9, 0.45)
        dark = mat("rubber", (0.018, 0.018, 0.02), 0.0, 0.9)
        accent = mat("sight_white", (0.80, 0.80, 0.76), 0.0, 0.6)
    else:  # nero opaco, il caso base
        body = mat("polymer_black", (0.032, 0.033, 0.036), 0.0, 0.62)
        metal = mat("steel_phosphate", (0.075, 0.08, 0.085), 0.9, 0.45)
        dark = mat("rubber", (0.018, 0.018, 0.02), 0.0, 0.9)
        accent = mat("sight_white", (0.80, 0.80, 0.76), 0.0, 0.6)
    return body, metal, dark, accent


# ------------------------------------------------------------------ geometria
class Part:
    """Un pezzo mobile (o il corpo): accumula geometria in un bmesh."""

    def __init__(self, name):
        self.name = name
        self.bm = bmesh.new()
        self.mats = []

    def _idx(self, m):
        if m not in self.mats:
            self.mats.append(m)
        return self.mats.index(m)

    def box(self, center, size, m, rot=(0.0, 0.0, 0.0)):
        idx = self._idx(m)
        ret = bmesh.ops.create_cube(self.bm, size=1.0)
        vs = ret["verts"]
        bmesh.ops.scale(self.bm, vec=size, verts=vs)
        if abs(rot[0]) + abs(rot[1]) + abs(rot[2]) > 1e-6:
            R = Euler(rot, "XYZ").to_matrix().to_4x4()
            bmesh.ops.transform(self.bm, matrix=R, verts=vs)
        bmesh.ops.translate(self.bm, vec=center, verts=vs)
        faces = {f for v in vs for f in v.link_faces}
        for f in faces:
            f.material_index = idx
        return self

    def cyl(self, center, radius, length, m, axis="Y", seg=12, radius2=None, rot=(0, 0, 0)):
        idx = self._idx(m)
        r2 = radius if radius2 is None else radius2
        ret = bmesh.ops.create_cone(
            self.bm, cap_ends=True, cap_tris=False, segments=seg,
            radius1=radius, radius2=r2, depth=length)
        vs = ret["verts"]
        if axis == "Y":
            R = Euler((math.radians(90), 0, 0), "XYZ").to_matrix().to_4x4()
            bmesh.ops.transform(self.bm, matrix=R, verts=vs)
        elif axis == "X":
            R = Euler((0, math.radians(90), 0), "XYZ").to_matrix().to_4x4()
            bmesh.ops.transform(self.bm, matrix=R, verts=vs)
        if abs(rot[0]) + abs(rot[1]) + abs(rot[2]) > 1e-6:
            R = Euler(rot, "XYZ").to_matrix().to_4x4()
            bmesh.ops.transform(self.bm, matrix=R, verts=vs)
        bmesh.ops.translate(self.bm, vec=center, verts=vs)
        faces = {f for v in vs for f in v.link_faces}
        for f in faces:
            f.material_index = idx
        return self

    def finish(self, parent, bevel=0.0012):
        me = bpy.data.meshes.new(self.name)
        self.bm.to_mesh(me)
        self.bm.free()
        for m in self.mats:
            me.materials.append(m)
        ob = bpy.data.objects.new(self.name, me)
        bpy.context.scene.collection.objects.link(ob)
        ob.parent = parent
        if bevel > 0:
            md = ob.modifiers.new("bev", "BEVEL")
            md.width = bevel
            md.segments = 2
            md.limit_method = "ANGLE"
            md.angle_limit = math.radians(40)
            bpy.context.view_layer.update()
            dg = bpy.context.evaluated_depsgraph_get()
            new_me = bpy.data.meshes.new_from_object(ob.evaluated_get(dg))
            ob.modifiers.clear()
            old = ob.data
            ob.data = new_me
            bpy.data.meshes.remove(old)
        return ob


def empty(parent, name, loc=(0, 0, 0)):
    e = bpy.data.objects.new(name, None)
    e.empty_display_size = 0.02
    e.parent = parent
    e.location = loc
    bpy.context.scene.collection.objects.link(e)
    return e


# ------------------------------------------------------------------ armi lunghe
def long_gun(p, sp):
    """
    Fucili, SMG, pompe, cecchini, mitragliatrice: una sola grammatica.
    p = dict di Part già create; sp = specifica.
    """
    body, metal, dark, accent = palette(sp.get("look", "polymer"))
    w = sp.get("width", 0.045)
    recv_y0, recv_y1 = sp.get("recv", (0.00, 0.24))
    recv_z0, recv_z1 = sp.get("recv_z", (0.00, 0.075))
    barrel = sp.get("barrel", 0.30)
    r_b = sp.get("barrel_r", 0.0095)

    # --- receiver
    p["body"].box(((0, (recv_y0 + recv_y1) / 2, (recv_z0 + recv_z1) / 2)),
                  (w, recv_y1 - recv_y0, recv_z1 - recv_z0), body)
    # culatta/backplate
    p["body"].box((0, recv_y0 - 0.006, (recv_z0 + recv_z1) / 2),
                  (w * 0.95, 0.012, (recv_z1 - recv_z0) * 0.9), metal)

    # --- canna + paramano
    y_b0 = recv_y1 - 0.01
    y_b1 = y_b0 + barrel
    bz = (recv_z0 + recv_z1) / 2 + 0.006          # quota dell'asse canna
    p["body"].cyl((0, (y_b0 + y_b1) / 2, bz), r_b, y_b1 - y_b0, metal)
    if sp.get("handguard", True):
        hg0 = recv_y1 - 0.02
        hg1 = sp.get("hg_end", y_b0 + barrel * sp.get("hg_frac", 0.75))
        p["body"].box((0, (hg0 + hg1) / 2, recv_z1 - 0.008),
                      (w * 0.86, hg1 - hg0, 0.042), body)
        # feritoie di raffreddamento
        for i in range(sp.get("vents", 4)):
            yy = hg0 + (hg1 - hg0) * (i + 1) / (sp.get("vents", 4) + 1)
            p["body"].box((0, yy, recv_z1 + 0.012), (w * 0.9, 0.010, 0.004), metal)

    # --- calcio
    st = sp.get("stock", "fixed")
    if st == "fixed":
        sy0, sy1 = recv_y0 - sp.get("stock_len", 0.26), recv_y0 + 0.01
        p["body"].box((0, (sy0 + sy1) / 2, recv_z0 + 0.018),
                      (w * 0.8, sy1 - sy0, 0.055), sp.get("stock_mat", body))
        p["body"].box((0, sy0 + 0.018, recv_z0 + 0.006), (w * 0.85, 0.05, 0.072), dark)
        if sp.get("cheek"):
            p["body"].box((0, sy0 + 0.09, recv_z0 + 0.052), (w * 0.7, 0.14, 0.028), sp.get("cheek_mat", body))
    elif st == "folding":
        sy0 = recv_y0 - sp.get("stock_len", 0.22)
        p["body"].box((0, (recv_y0 + sy0) / 2, recv_z0 + 0.030), (0.010, recv_y0 - sy0, 0.010), metal)
        p["body"].box((0, sy0 + 0.01, recv_z0 + 0.026), (0.022, 0.05, 0.055), dark)
    elif st == "wire":
        sy0 = recv_y0 - sp.get("stock_len", 0.22)
        for dz in (0.010, 0.042):
            p["body"].box((0, (recv_y0 + sy0) / 2, recv_z0 + dz), (0.006, recv_y0 - sy0, 0.006), metal)
        p["body"].box((0, sy0 + 0.01, recv_z0 + 0.026), (0.018, 0.04, 0.060), metal)
    elif st == "skeleton":
        sy0 = recv_y0 - sp.get("stock_len", 0.26)
        p["body"].box((0, (recv_y0 + sy0) / 2, recv_z0 + 0.058), (0.010, recv_y0 - sy0, 0.012), metal)
        p["body"].box((0, (recv_y0 + sy0) / 2, recv_z0 + 0.006), (0.010, recv_y0 - sy0, 0.012), metal)
        p["body"].box((0, sy0 + 0.02, recv_z0 + 0.030), (0.016, 0.05, 0.075), dark)

    # --- impugnatura: il collo si innesta SOTTO il castello. Prima era un pezzo
    # appeso nel vuoto: 6 cm di aria fra castello e impugnatura, e in prima persona
    # si vedeva subito. Ora il collo tocca recv_z0 e il grilletto sta nella sua guardia.
    garg = math.radians(sp.get("grip_angle", 12))
    gh = sp.get("grip_h", 0.115)
    # la vecchia impugnatura stava a y=-0,055: DIETRO il castello (che parte da y=0),
    # quindi appesa nel vuoto sotto il calcio. Ora sta sotto il castello, dietro il grilletto.
    gy = sp.get("grip_y", sp.get("trig_y", 0.065) - 0.030)
    gz = recv_z0 - gh * 0.5 + 0.014
    p["body"].box((0, gy, gz), (0.030, 0.052, gh), dark, rot=(garg, 0, 0))
    p["body"].box((0, gy + 0.016, recv_z0 - 0.008), (0.032, 0.068, 0.030), body, rot=(garg * 0.4, 0, 0))
    if sp.get("grip_tape"):
        p["body"].box((0, gy + 0.002, gz + 0.014), (0.033, 0.044, 0.046),
                      palette("sheet")[3], rot=(garg, 0, 0))

    # --- guardia del grilletto: traversa + due montanti, così il grilletto è "dentro"
    trig_y = sp.get("trig_y", 0.065)
    p["body"].box((0, trig_y + 0.016, recv_z0 - 0.046), (0.008, 0.086, 0.006), metal)
    p["body"].box((0, trig_y + 0.057, recv_z0 - 0.026), (0.008, 0.007, 0.044), metal)
    p["body"].box((0, trig_y - 0.025, recv_z0 - 0.026), (0.008, 0.007, 0.044), metal)

    # --- caricatore
    mt = sp.get("mag_type", "box")
    if mt == "box":
        ml = sp.get("mag_len", 0.19)
        cy = sp.get("mag_y", sp.get("trig_y", 0.065) + 0.075)
        ang = math.radians(sp.get("mag_angle", 0))
        p["mag"].box((0, cy, recv_z0 - ml / 2 + 0.01), (0.030, 0.075, ml), metal, rot=(ang, 0, 0))
        p["mag"].box((0, cy, recv_z0 - ml + 0.02), (0.033, 0.080, 0.012), dark, rot=(ang, 0, 0))
    elif mt == "banana":
        ml = sp.get("mag_len", 0.22)
        cy = sp.get("mag_y", sp.get("trig_y", 0.065) + 0.075)
        for i in range(3):
            t = i / 2.0
            p["mag"].box((0, cy + 0.012 * t, recv_z0 - ml * (0.18 + 0.32 * t)),
                         (0.030, 0.078, ml * 0.42), metal,
                         rot=(math.radians(-8 * i), 0, 0))
    elif mt == "drum":
        dr = sp.get("drum_r", 0.070)
        dy = sp.get("mag_y", sp.get("trig_y", 0.065) + 0.090)
        p["mag"].cyl((0, dy, recv_z0 - dr * 0.92), dr, 0.078, metal, axis="X", seg=16)
        p["mag"].cyl((0, dy, recv_z0 - dr * 0.92), 0.030, 0.084, dark, axis="X", seg=12)
    elif mt == "horizontal":
        p["mag"].box((0, 0.02, recv_z1 + 0.028), (0.028, 0.20, 0.035), metal)
        p["mag"].box((0, 0.12, recv_z1 + 0.028), (0.030, 0.02, 0.038), dark)

    # --- grilletto e cane
    p["trigger"].box((0, trig_y, recv_z0 - 0.019), (0.008, 0.010, 0.030), metal)
    if sp.get("hammer", True):
        p["hammer"].box((0, recv_y0 + 0.012, recv_z1 + 0.004), (0.008, 0.030, 0.016), metal,
                        rot=(math.radians(-20), 0, 0))

    # --- ottica
    op = sp.get("optic")
    if op == "scope":
        zy = recv_z1 + 0.045
        p["body"].cyl((0, sp.get("scope_y", recv_y0 + 0.16), zy), 0.019, sp.get("scope_len", 0.26),
                      dark, axis="Y", seg=14)
        p["body"].cyl((0, sp.get("scope_y", recv_y0 + 0.16) + sp.get("scope_len", 0.26) / 2, zy),
                      0.024, 0.03, dark, axis="Y", seg=14)
        for dy in (-0.07, 0.07):
            p["body"].box((0, sp.get("scope_y", recv_y0 + 0.16) + dy, recv_z1 + 0.022),
                          (0.024, 0.016, 0.040), metal)
    elif op == "dot":
        p["body"].box((0, recv_y0 + 0.13, recv_z1 + 0.026), (0.026, 0.05, 0.030), dark)
        p["body"].box((0, recv_y0 + 0.13, recv_z1 + 0.044), (0.020, 0.020, 0.012), metal)
    elif op == "irons" or op is None:
        p["body"].box((0, recv_y1 - 0.03, recv_z1 + 0.014), (0.010, 0.010, 0.014), metal)
        p["body"].box((0, recv_y0 + 0.02, recv_z1 + 0.014), (0.014, 0.010, 0.016), metal)

    # --- dettagli per carattere
    if sp.get("carry_handle"):
        p["body"].box((0, recv_y0 + 0.10, recv_z1 + 0.040), (0.026, 0.16, 0.012), body)
        p["body"].box((0, recv_y0 + 0.03, recv_z1 + 0.022), (0.022, 0.012, 0.040), body)
        p["body"].box((0, recv_y0 + 0.17, recv_z1 + 0.022), (0.022, 0.012, 0.040), body)
    if sp.get("bipod"):
        # cerniera + due gambe inclinate IN AVANTI (rotazione su X): di profilo si leggono,
        # mentre prima ruotavano su Y e restavano una linea sottile dietro l'altra
        by = y_b1 - 0.24
        p["body"].box((0, by, bz - 0.026), (0.024, 0.034, 0.024), metal)
        for dx in (-1, 1):
            p["body"].box((dx * 0.013, by + 0.044, bz - 0.082), (0.009, 0.011, 0.110), metal,
                          rot=(math.radians(26), 0, math.radians(dx * 5)))
            p["body"].box((dx * 0.013, by + 0.070, bz - 0.132), (0.012, 0.026, 0.012), dark,
                          rot=(math.radians(26), 0, 0))
    if sp.get("pump"):
        py0 = y_b0 + 0.11
        p["slide"].box((0, py0, bz - 0.014), (w * 0.95, 0.19, 0.046), sp.get("pump_mat", body))
        for i in range(4):
            p["slide"].box((0, py0 - 0.062 + i * 0.041, bz - 0.038), (w * 0.98, 0.012, 0.013), metal)

    if sp.get("heat_shield"):
        # manicotto traforato: il cilindro + le asole scure sotto, non un tubo liscio
        p["body"].cyl((0, y_b0 + 0.15, bz), 0.030, 0.24, metal, axis="Y", seg=12)
        for i in range(3):
            p["body"].box((0, y_b0 + 0.07 + i * 0.06, bz - 0.030), (0.030, 0.028, 0.006), dark)
    if sp.get("shroud"):
        # paramano grezzo: un tubo con due asole larghe, avvitato al castello
        p["body"].cyl((0, y_b0 + 0.10, bz), 0.018, 0.19, metal, axis="Y", seg=10)
        for i in range(2):
            p["body"].box((0, y_b0 + 0.06 + i * 0.075, bz + 0.018), (0.028, 0.036, 0.004), dark)
        for dy in (0.03, 0.17):
            p["body"].cyl((w * 0.5, y_b0 + dy, bz), 0.004, 0.006, metal, axis="X", seg=6)
    if sp.get("sheet_fold"):
        # lamiera piegata: coperchio con risvolto sul bordo, saldatura sul fianco,
        # bulloni a vista, finestrino d'espulsione incassato, anello di cinghia
        p["body"].box((0, (recv_y0 + recv_y1) / 2, recv_z1 + 0.003),
                      (w * 1.06, recv_y1 - recv_y0 - 0.012, 0.008), body)
        p["body"].box((0, recv_y1 - 0.005, recv_z1 + 0.001), (w * 1.08, 0.010, 0.016), body)
        p["body"].box((0, (recv_y0 + recv_y1) / 2, recv_z1 - 0.006),
                      (w * 1.03, recv_y1 - recv_y0 - 0.020, 0.004), metal)
        for dy in (0.035, 0.135, 0.205):
            for dx in (-1, 1):
                p["body"].cyl((dx * w * 0.52, recv_y0 + dy, recv_z0 + 0.030), 0.004, 0.007,
                              metal, axis="X", seg=6)
        p["body"].box((w * 0.52, recv_y1 - 0.055, recv_z1 - 0.022), (0.005, 0.060, 0.022), dark)
        p["body"].box((0, recv_y1 - 0.02, recv_z0 - 0.030), (0.014, 0.006, 0.020), metal)
        # manetta d'armamento grezza, sul fianco destro
        p["body"].box((w * 0.62, recv_y1 - 0.10, recv_z1 - 0.012), (0.020, 0.030, 0.009), metal)
    if sp.get("belt"):
        # nastro di cartucce che scende dal tamburo: il peso si vede
        by = sp.get("mag_y", sp.get("trig_y", 0.065) + 0.090)
        for i in range(7):
            p["mag"].box((-0.016, by - 0.048 + i * 0.013, recv_z0 - 0.132 - i * 0.020),
                         (0.034, 0.017, 0.018), palette("wood")[3],
                         rot=(math.radians(24 + i * 3), 0, math.radians(6)))
    if sp.get("muzzle_device") == "brake":
        p["body"].cyl((0, y_b1 - 0.03, recv_z1 - 0.010), 0.016, 0.06, metal, axis="Y", seg=10)
    if sp.get("rib"):
        p["body"].box((0, (y_b0 + y_b1) / 2, recv_z1 + 0.004), (0.008, y_b1 - y_b0, 0.008), metal)

    return dict(muzzle=(0.0, y_b1 + 0.005, recv_z1 - 0.010),
                eject=(w * 0.5, recv_y1 - 0.07, recv_z1 - 0.004),
                aim=(0.0, recv_y0 + 0.10, recv_z1 + (0.05 if op == "scope" else 0.02)))


# ------------------------------------------------------------------ pistole
def pistol(p, sp):
    body, metal, dark, accent = palette(sp.get("look", "polymer"))
    w = sp.get("width", 0.033)
    slide_len = sp.get("slide_len", 0.185)
    slide_z0, slide_z1 = 0.030, 0.062
    y0, y1 = -slide_len * 0.45, slide_len * 0.55

    # carrello
    p["slide"].box((0, (y0 + y1) / 2, (slide_z0 + slide_z1) / 2),
                   (w, y1 - y0, slide_z1 - slide_z0), metal)
    # muso rastremato
    p["slide"].box((0, y1 - 0.006, (slide_z0 + slide_z1) / 2 + 0.002),
                   (w * 0.92, 0.014, (slide_z1 - slide_z0) * 0.8), metal)
    # mire
    p["slide"].box((0, y0 + 0.016, slide_z1 + 0.005), (w * 0.35, 0.012, 0.010), metal)
    p["slide"].box((0, y1 - 0.022, slide_z1 + 0.004), (w * 0.18, 0.008, 0.008), accent)

    # fusto
    p["body"].box((0, 0.005, 0.014), (w * 0.95, 0.150, 0.030), body)
    # guardia
    p["body"].box((0, 0.075, -0.012), (w * 0.55, 0.075, 0.010), body)
    p["body"].box((0, 0.110, 0.004), (w * 0.55, 0.010, 0.030), body)
    # impugnatura
    ang = math.radians(sp.get("grip_angle", 16))
    p["body"].box((0, -0.040, -0.075), (w * 0.92, 0.048, 0.135), body, rot=(ang, 0, 0))
    # guancette
    p["body"].box((0, -0.048, -0.075), (w * 0.98, 0.040, 0.120), dark, rot=(ang, 0, 0))

    # caricatore (dentro l'impugnatura, fuori sotto se esteso)
    ml = sp.get("mag_len", 0.14)
    p["mag"].box((0, -0.040, -0.070 - (ml - 0.13) / 2), (w * 0.75, 0.036, ml), metal, rot=(ang, 0, 0))
    p["mag"].box((0, -0.062, -0.145 - (ml - 0.13)), (w * 0.80, 0.040, 0.010), dark)

    # grilletto e cane
    p["trigger"].box((0, 0.060, -0.020), (0.007, 0.009, 0.026), metal)
    if sp.get("hammer", True):
        p["hammer"].box((0, y0 + 0.012, 0.052), (0.007, 0.024, 0.014), metal, rot=(math.radians(-25), 0, 0))

    # canna/compensatore
    if sp.get("comp"):
        p["body"].cyl((0, y1 + 0.012, 0.046), 0.011, 0.030, metal, axis="Y", seg=10)
        for i in range(3):
            p["body"].box((0, y1 + 0.008 + i * 0.008, 0.058), (w * 0.9, 0.003, 0.008), metal)
    if sp.get("suppressor"):
        sl = sp.get("supp_len", 0.12)
        p["body"].cyl((0, y1 + sl / 2, 0.046), 0.019, sl, metal, axis="Y", seg=14)

    return dict(muzzle=(0.0, y1 + sp.get("supp_len", 0.12) + 0.01 if sp.get("suppressor") else y1 + 0.02,
                        0.046),
                eject=(w * 0.5, y0 + 0.05, 0.050),
                aim=(0.0, y0 + 0.02, 0.070))


# ------------------------------------------------------------------ rivoltelle
def revolver(p, sp):
    body, metal, dark, accent = palette(sp.get("look", "steel"))
    w = sp.get("width", 0.036)
    drum_r = sp.get("drum_r", 0.020)
    drum_y = sp.get("drum_y", 0.010)
    z = 0.030

    # castello
    p["body"].box((0, -0.020, z), (w * 0.8, 0.090, 0.050), metal)
    # canna con astina
    bl = sp.get("barrel", 0.10)
    p["body"].cyl((0, 0.045 + bl / 2, z + 0.006), sp.get("barrel_r", 0.010), bl, metal, axis="Y", seg=12)
    p["body"].box((0, 0.045 + bl / 2, z - 0.012), (w * 0.5, bl * 0.9, 0.014), metal)
    if sp.get("rib"):
        p["body"].box((0, 0.045 + bl / 2, z + 0.020), (0.008, bl, 0.006), metal)
    # tamburo (è lo "slide": ruota)
    p["slide"].cyl((0, drum_y, z + 0.004), drum_r, 0.042, metal, axis="Y", seg=8)
    for i in range(6):
        a = i * math.pi / 3
        p["slide"].cyl((math.cos(a) * drum_r * 0.55, drum_y, z + 0.004 + math.sin(a) * drum_r * 0.55),
                       0.005, 0.046, dark, axis="Y", seg=6)
    # impugnatura in legno
    ang = math.radians(sp.get("grip_angle", 18))
    p["body"].box((0, -0.075, -0.055), (w * 0.85, 0.048, 0.115), body, rot=(ang, 0, 0))
    # cane
    p["hammer"].box((0, -0.058, z + 0.020), (0.008, 0.020, 0.018), metal, rot=(math.radians(-30), 0, 0))
    p["trigger"].box((0, -0.010, z - 0.024), (0.007, 0.009, 0.026), metal)

    return dict(muzzle=(0.0, 0.045 + bl + 0.01, z + 0.006),
                eject=(w * 0.5, drum_y, z + 0.010),
                aim=(0.0, 0.045, z + 0.024))


# ------------------------------------------------------------------ coltello
def knife(p, sp):
    body, metal, dark, accent = palette("wood")
    blade = sp.get("blade", 0.17)
    w = 0.004
    # lama annerita, punta a coltello
    p["body"].box((0, blade * 0.5, 0.012), (w, blade * 0.75, 0.032), mat("blade_black", (0.05, 0.05, 0.055), 0.9, 0.35))
    p["body"].box((0, blade * 0.86, 0.010), (w * 0.9, blade * 0.18, 0.024),
                  mat("blade_black", (0.05, 0.05, 0.055), 0.9, 0.35), rot=(math.radians(8), 0, 0))
    p["body"].box((0, blade * 0.5, -0.004), (w * 1.1, blade * 0.8, 0.006), dark)
    # guardia
    p["body"].box((0, -0.004, 0.008), (0.016, 0.010, 0.034), metal)
    # manico in gomma
    p["body"].box((0, -0.060, 0.008), (0.020, 0.105, 0.028), body)
    for i in range(4):
        p["body"].box((0, -0.085 + i * 0.022, 0.008), (0.022, 0.008, 0.030), dark)
    return dict(muzzle=(0, blade, 0.012), eject=(0, 0, 0), aim=(0, blade * 0.5, 0.03))


# ------------------------------------------------------------------ specifiche
SPECS = {
    # --- pistole
    "chatter": dict(builder=pistol, look="polymer", slide_len=0.195, mag_len=0.185, comp=True,
                    grip_angle=14, class_label="Pistola automatica", cycle=0.060, reload_t=1.80, reload_empty_t=2.40),
    "service": dict(builder=long_gun, look="polymer", recv=(0.0, 0.26), recv_z=(0.0, 0.075),
                    barrel=0.34, stock="fixed", stock_len=0.24, mag_type="box", mag_len=0.19,
                    optic=None, carry_handle=True, class_label="Fucile d'assalto", cycle=0.086, reload_t=2.30, reload_empty_t=3.00),
    "blitz": dict(builder=long_gun, look="green", recv=(0.0, 0.30), recv_z=(0.0, 0.080),
                  barrel=0.24, stock="folding", stock_len=0.16, mag_type="horizontal",
                  handguard=False, optic="dot", class_label="PDW", cycle=0.052, reload_t=2.40, reload_empty_t=3.10),
    "sweeper": dict(builder=long_gun, look="polymer", recv=(0.0, 0.28), recv_z=(0.0, 0.085),
                    barrel=0.44, barrel_r=0.014, stock="fixed", stock_len=0.26,
                    mag_type="box", mag_len=0.20, carry_handle=True,
                    class_label="Pompa semiautomatico", cycle=0.35, reload_t=2.60, reload_empty_t=3.40, travel=0.035),
    "snub": dict(builder=revolver, look="steel", barrel=0.05, drum_r=0.019, grip_angle=20, cylinder=True,
                 class_label="Rivoltella", cycle=0.25, reload_t=3.00, reload_empty_t=3.00, mag_out=0.02),
    "hammer": dict(builder=revolver, look="steel", barrel=0.20, drum_r=0.022, rib=True, cylinder=True,
                   grip_angle=16, class_label="Magnum", cycle=0.55, reload_t=2.10, reload_empty_t=2.80, travel=0.030),
    # --- SMG
    "cadence": dict(builder=long_gun, look="polymer", recv=(0.0, 0.24), recv_z=(0.0, 0.070),
                    barrel=0.20, stock="folding", stock_len=0.18, mag_type="banana", mag_len=0.24,
                    optic="dot", class_label="SMG", cycle=0.075, reload_t=2.20, reload_empty_t=2.90),
    "scrap": dict(builder=long_gun, look="sheet", recv=(-0.008, 0.245), recv_z=(-0.014, 0.068),
                  barrel=0.155, barrel_r=0.0105, stock="wire", stock_len=0.215,
                  mag_type="box", mag_len=0.170, mag_angle=9, mag_y=0.118,
                  grip_tape=True, grip_angle=19, grip_h=0.108,
                  handguard=False, shroud=True, sheet_fold=True, optic=None, width=0.050,
                  class_label="SMG economica", cycle=0.067, reload_t=2.00, reload_empty_t=2.60),
    # --- fucili
    "anvil": dict(builder=long_gun, look="wood", recv=(0.0, 0.27), recv_z=(0.0, 0.080),
                  barrel=0.40, barrel_r=0.011, stock="fixed", stock_len=0.27, mag_type="banana",
                  mag_len=0.22, optic=None, class_label="Fucile d'assalto 7,62", cycle=0.10, reload_t=2.60, reload_empty_t=3.30),
    "choke": dict(builder=long_gun, look="wood", recv=(0.0, 0.26), recv_z=(0.0, 0.080),
                  barrel=0.55, barrel_r=0.015, stock="fixed", stock_len=0.30, mag_type=None,
                  handguard=False, pump=True, optic=None, class_label="Pompa", cycle=0.90, reload_t=3.60, reload_empty_t=3.60, travel=0.055, mag_out=0.02),
    "vantage": dict(builder=long_gun, look="polymer", recv=(0.0, 0.28), recv_z=(0.0, 0.080),
                    barrel=0.46, barrel_r=0.011, stock="fixed", stock_len=0.26, cheek=True,
                    mag_type="box", mag_len=0.20, optic="scope", scope_len=0.26,
                    class_label="Fucile da battaglia", cycle=0.40, reload_t=2.80, reload_empty_t=3.50, travel=0.030),
    "spotter": dict(builder=long_gun, look="wood", recv=(0.0, 0.29), recv_z=(0.0, 0.082),
                    barrel=0.50, barrel_r=0.010, stock="skeleton", stock_len=0.26,
                    mag_type="box", mag_len=0.16, optic="scope", scope_len=0.30,
                    class_label="Cecchino semiautomatico", cycle=0.35, reload_t=3.00, reload_empty_t=3.80, travel=0.032),
    "longshot": dict(builder=long_gun, look="polymer", recv=(0.0, 0.30), recv_z=(0.0, 0.085),
                     barrel=0.60, barrel_r=0.012, stock="fixed", stock_len=0.28, cheek=True,
                     mag_type="box", mag_len=0.14, optic="scope", scope_len=0.34,
                     muzzle_device="brake", class_label="Fucile a otturatore", cycle=1.40, reload_t=3.20, reload_empty_t=4.00, travel=0.070),
    "heavy": dict(builder=long_gun, look="sheet", recv=(-0.010, 0.340), recv_z=(-0.016, 0.102),
                  barrel=0.520, barrel_r=0.016, stock="fixed", stock_len=0.285, cheek=True,
                  mag_type="drum", mag_y=0.172, drum_r=0.066, heat_shield=True, bipod=True, carry_handle=True,
                  sheet_fold=True, belt=True, optic=None, width=0.062, grip_angle=9,
                  grip_h=0.128, travel=0.045,
                  class_label="Mitragliatrice leggera", cycle=0.080, reload_t=4.20, reload_empty_t=5.20),
    "melee": dict(builder=knife, look="wood", blade=0.17, class_label="Coltello", cycle=0.60, reload_t=0.60, reload_empty_t=0.60),
}


# ------------------------------------------------------------------ animazioni
def mm(v):
    return v / 1000.0


def add_clips(root, parts, sp):
    """Le stesse sette clip della Standard, adattate ai pezzi che esistono."""
    sc = bpy.context.scene
    sc.render.fps = 30
    # il coltello non ha pezzi mobili: il suo "colpo" è il fendente, quindi è il CORPO
    # a dover finire nelle tracce (prima restava fuori da `objects` e non si animava)
    moving = ["body"] if sp.get("builder") is knife else [n for n in ("slide", "mag", "trigger", "hammer") if n in parts]
    objects = {n: parts[n] for n in moving}

    travel = sp.get("travel", 0.028)      # corsa dell'otturatore/pompa
    mag_out = sp.get("mag_out", 0.13)
    kind = sp.get("builder")
    drum = sp.get("mag_type") == "drum" or sp.get("cylinder", False)

    def keys(obj, prop, pairs):
        for f, val in pairs:
            setattr(obj, prop, val)
            obj.keyframe_insert(data_path=prop, frame=f)

    def cycle(frames, hold=False):
        # la canna è +Y: l'otturatore arretra verso -Y
        if hold:
            return [(1, (0, 0, 0)), (2, (0, -travel * 0.4, 0)), (3, (0, -travel, 0)), (frames, (0, -travel, 0))]
        return [(1, (0, 0, 0)), (2, (0, -travel * 0.4, 0)), (3, (0, -travel, 0)),
                (4, (0, -travel * 0.8, 0)), (frames, (0, 0, 0))]

    def clip(name, length, tracks):
        return dict(name=name, length=length, tracks=tracks)

    clips = []
    idle_tracks = {n: {"location": [(1, (0, 0, 0)), (2, (0, 0, 0))]} for n in moving}
    clips.append(clip("idle", 2, idle_tracks))

    # la clip del colpo dura quanto il ciclo dell'arma (0,052 s per il Blitz, 1,40 s per
    # il Longshot): così l'otturatore torna a posto prima del colpo successivo
    fire_frames = max(4, min(20 if sp.get("builder") is knife else 14, round(sp.get("cycle", 0.20) * 30) + 1))
    for nm, hold in (("fire", False), ("fire_last", True)):
        tr = {}
        if "slide" in moving:
            if drum:
                # rivoltella: il tamburo ruota di 60°
                tr["slide"] = {"rotation_euler": [(1, (0, 0, 0)), (3, (0, math.radians(60), 0)),
                                                  (6, (0, math.radians(60), 0))]}
            else:
                tr["slide"] = {"location": cycle(6, hold)}
        if "hammer" in moving:
            tr["hammer"] = {"rotation_euler": [(1, (0.45, 0, 0)), (2, (0, 0, 0)), (3, (0, 0, 0)),
                                               (4, (0.2, 0, 0)), (6, (0.45, 0, 0))]}
        if "trigger" in moving:
            tr["trigger"] = {"rotation_euler": [(1, (0, 0, 0)), (2, (-0.22, 0, 0)),
                                                (4, (-0.22, 0, 0)), (6, (0, 0, 0))]}
        if "mag" in moving:
            tr["mag"] = {"location": [(1, (0, 0, 0)), (6, (0, 0, 0))]}
        if sp.get("builder") is knife:
            # il coltello non ha otturatore: il colpo È il fendente, e si vede dal corpo
            if hold:
                tr["body"] = {"rotation_euler": [(1, (0, 0, 0)),
                                                 (fire_frames, (math.radians(-30), 0, math.radians(10)))]}
            else:
                tr["body"] = {"rotation_euler": [(1, (0, 0, 0)),
                                                 (2, (math.radians(-42), 0, math.radians(16))),
                                                 (3, (math.radians(-18), 0, math.radians(6))),
                                                 (fire_frames, (0, 0, 0))]}
        clips.append(clip(nm, fire_frames, tr))

    # ricarica: la clip dura quanto la ricarica VERA dell'arma (Snub 3,0 s, Longshot 4,0 s),
    # altrimenti il viewmodel finisce l'animazione prima che il colpo sia pronto
    rf = max(20, round(sp.get("reload_t", 1.97) * 30) + 1)
    ef = max(rf + 6, round(sp.get("reload_empty_t", 2.37) * 30) + 1)

    def at(f, total):
        return max(2, min(total - 1, round(f * (total - 1)) + 1))

    # caricatore fuori e dentro (o tamburo che gira se non c'è caricatore)
    reload_keys = [(1, (0, 0, 0)), (at(0.15, rf), (0, 0, 0)), (at(0.23, rf), (0, 0, -mag_out * 0.35)),
                   (at(0.30, rf), (0, 0, -mag_out)), (at(0.52, rf), (0, 0, -mag_out)),
                   (at(0.72, rf), (0, 0, -mm(6))), (at(0.80, rf), (0, 0, 0)), (rf, (0, 0, 0))]
    tr = {}
    if "mag" in moving:
        tr["mag"] = {"location": reload_keys}
    if "slide" in moving:
        tr["slide"] = {"location": [(1, (0, 0, 0)), (rf, (0, 0, 0))],
                       "rotation_euler": [(1, (0, 0, 0)), (rf, (0, 0, 0))]}
    if "trigger" in moving:
        tr["trigger"] = {"rotation_euler": [(1, (0, 0, 0)), (rf, (0, 0, 0))]}
    if "hammer" in moving:
        tr["hammer"] = {"rotation_euler": [(1, (0.45, 0, 0)), (rf, (0.45, 0, 0))]}
    clips.append(clip("reload", rf, tr))

    # ricarica a vuoto: più lunga + otturatore che si chiude alla fine
    empty_keys = [(1, (0, 0, 0)), (at(0.12, ef), (0, 0, 0)), (at(0.20, ef), (0, 0, -mag_out * 0.35)),
                  (at(0.26, ef), (0, 0, -mag_out)), (at(0.50, ef), (0, 0, -mag_out)),
                  (at(0.70, ef), (0, 0, -mm(6))), (at(0.78, ef), (0, 0, 0)), (ef, (0, 0, 0))]
    tr = {}
    if "mag" in moving:
        tr["mag"] = {"location": empty_keys}
    if "slide" in moving:
        if drum:
            tr["slide"] = {"rotation_euler": [(1, (0, 0, 0)), (ef, (0, 0, 0))]}
        else:
            tr["slide"] = {"location": [(1, (0, -travel, 0)), (ef - 16, (0, -travel, 0)),
                                        (ef - 11, (0, -travel * 0.5, 0)), (ef - 5, (0, 0, 0)),
                                        (ef, (0, 0, 0))]}
    if "trigger" in moving:
        tr["trigger"] = {"rotation_euler": [(1, (0, 0, 0)), (ef, (0, 0, 0))]}
    if "hammer" in moving:
        tr["hammer"] = {"rotation_euler": [(1, (0.45, 0, 0)), (ef, (0.45, 0, 0))]}
    clips.append(clip("reload_empty", ef, tr))

    for nm, ln in (("draw", 15), ("holster", 15)):
        tr = {}
        for n in moving:
            if n == "hammer":
                tr[n] = {"rotation_euler": [(1, (0.45, 0, 0)), (ln, (0.45, 0, 0))]}
            elif n == "slide" and drum:
                tr[n] = {"rotation_euler": [(1, (0, 0, 0)), (ln, (0, 0, 0))]}
            else:
                tr[n] = {"location": [(1, (0, 0, 0)), (ln, (0, 0, 0))]}
        clips.append(clip(nm, ln, tr))

    # --- applica: una azione per clip per oggetto, poi NLA track con lo stesso nome
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
                    keys(ob, prop, pairs)
            ob.animation_data.action = None
            tr = ob.animation_data.nla_tracks.new()
            tr.name = c["name"]
            st = tr.strips.new(c["name"], 1, act)
            st.frame_start = 1
            st.frame_end = c["length"]
            st.name = c["name"]
            st.blend_type = "REPLACE"

    sc.frame_start = 1
    sc.frame_end = 72
    sc.frame_set(1)
    return {c["name"]: round((c["length"] - 1) / 30.0, 3) for c in clips}


# ------------------------------------------------------------------ costruzione
def build(wid):
    sp = SPECS[wid]
    for ob in list(bpy.data.objects):
        bpy.data.objects.remove(ob, do_unlink=True)
    MATS.clear()

    root = bpy.data.objects.new(wid, None)
    root.empty_display_size = 0.05
    bpy.context.scene.collection.objects.link(root)

    parts = {n: Part(n) for n in ("body", "slide", "mag", "trigger", "hammer")}
    info = sp["builder"](parts, sp)

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
        "socket_hand_r": (0.0, -0.045, -0.075),
        "socket_hand_l": (0.0, 0.16, -0.01),
    }
    for name, loc in sockets.items():
        empty(root, name, loc)

    lengths = add_clips(root, objs, sp)

    # --- statistiche e export
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
    print("BUILT %-9s tris=%-5d %s" % (wid, tris, out))
    return data


def main():
    args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    ids = args or list(SPECS.keys())
    for wid in ids:
        build(wid)
    print("ARSENALE OK")


if __name__ == "__main__":
    main()
