"""
FALEZIA · art/weapons/arsenal2/gunlib.py — il motore dei modelli (seconda generazione).

Regole di costruzione:
  · ogni pezzo nasce da un profilo vero (estruso in X o in Y), non da scatole accostate
  · i dettagli che si vedono in prima persona sono a 1:1: tacche, feritoie, viti, sganci
  · UV cubiche a scala fissa (12 cm = una mattonella), materiali PBR da texgen.py
  · i pezzi mobili hanno l'origine sul loro perno (cane, grilletto, tamburo, otturatore)

Convenzioni (quelle che il gioco si aspetta):
    +Y = canna · +Z = alto · origine vicino all'incavo della mano · metri
    socket_muzzle, socket_eject, socket_aim, socket_hand_r, socket_hand_l
"""
import bpy
import bmesh
import math
import os
from mathutils import Vector, Euler

ROOT = "/home/matteo/Progetti/Falezia"
TEX = os.path.join(ROOT, "game", "assets", "weapons", "_shared", "tex")
GAME = os.path.join(ROOT, "game", "assets", "weapons")

BEVEL = 0.0009
UV_TILE = 0.085

_MATS = {}


# ------------------------------------------------------------------ materiali

def material(fam):
    if fam in _MATS:
        return _MATS[fam]
    m = bpy.data.materials.new("M_" + fam)
    m.use_nodes = True
    nt = m.node_tree
    nodes, links = nt.nodes, nt.links
    bsdf = nodes["Principled BSDF"]
    bsdf.inputs["IOR"].default_value = 1.5

    def tex(name, non_color=False):
        # basecolor e normal arrivano da ambientCG in JPEG, le ORM le scriviamo noi in PNG
        path = None
        for ext in (".jpg", ".png"):
            cand = os.path.join(TEX, name + ext)
            if os.path.exists(cand):
                path = cand
                break
        n = nodes.new("ShaderNodeTexImage")
        n.image = bpy.data.images.load(path, check_existing=True)
        if non_color:
            n.image.colorspace_settings.name = "Non-Color"
        n.interpolation = "Linear"
        return n

    base = tex(fam + "_basecolor")
    links.new(base.outputs["Color"], bsdf.inputs["Base Color"])

    orm = tex(fam + "_orm", True)
    sep = nodes.new("ShaderNodeSeparateColor")
    links.new(orm.outputs["Color"], sep.inputs["Color"])
    links.new(sep.outputs["Green"], bsdf.inputs["Roughness"])
    links.new(sep.outputs["Blue"], bsdf.inputs["Metallic"])

    nrm = tex(fam + "_normal", True)
    nm = nodes.new("ShaderNodeNormalMap")
    nm.inputs["Strength"].default_value = 1.05
    links.new(nrm.outputs["Color"], nm.inputs["Color"])
    links.new(nm.outputs["Normal"], bsdf.inputs["Normal"])

    if fam == "glass":
        # il vetro dell'ottica deve LASCIAR VEDERE. Prima era opaco: in mira si
        # guardava una biglia blu. Alfa bassa + trasmissione, e il blend per il glTF.
        bsdf.inputs["Alpha"].default_value = 0.16
        bsdf.inputs["Roughness"].default_value = 0.03
        try:
            bsdf.inputs["Transmission Weight"].default_value = 0.55
        except Exception:
            pass
        try:
            m.surface_render_method = "BLENDED"     # Blender 5 / EEVEE Next
        except Exception:
            pass
        try:
            m.blend_method = "BLEND"                # compatibilità con l'esportatore
        except Exception:
            pass
        m.use_backface_culling = False

    _MATS[fam] = m
    return m


def palette(kind, over=None):
    """Materiali di base per carattere, più quelli che la singola arma impone."""
    if kind == "wood":
        p = dict(body="wood", metal="steel", dark="leather", accent="brass", recv="steel")
    elif kind == "steel":
        p = dict(body="stainless", metal="steel", dark="rubber", accent="brass", recv="stainless")
    elif kind == "sheet":
        p = dict(body="sheet", metal="steel", dark="rubber", accent="steel", recv="sheet")
    elif kind == "green":
        p = dict(body="green", metal="steel", dark="cloth", accent="steel", recv="green")
    else:
        p = dict(body="polymer", metal="steel", dark="stipple", accent="steel", recv="polymer")
    if over:
        p.update(over)
    return p


# ------------------------------------------------------------------ profili

def strip_profile(y0, z0, length, depth, ang0, ang1=None, steps=4, taper=1.0, flat_top=False):
    """
    Contorno (y,z) di un pezzo allungato che scende da (y0,z0): impugnatura,
    caricatore curvo, manico. `ang` è l'inclinazione rispetto alla verticale (gradi,
    positivo = il fondo va indietro). Torna un poligono chiuso, pronto per prism().
    """
    ang1 = ang0 if ang1 is None else ang1
    front, back = [], []
    y, z = float(y0), float(z0)
    seg = length / steps
    for i in range(steps + 1):
        t = i / steps
        a = math.radians(ang0 + (ang1 - ang0) * t)
        d = depth * (1.0 - (1.0 - taper) * t)
        ny, nz = math.cos(a), -math.sin(a)
        front.append((y + ny * d / 2, z + nz * d / 2))
        back.append((y - ny * d / 2, z - nz * d / 2))
        y += -math.sin(a) * seg
        z += -math.cos(a) * seg
    if flat_top:
        front[0] = (front[0][0], z0)
        back[0] = (back[0][0], z0)
    return front + list(reversed(back))


def _bez3(p0, p1, p2, n=5):
    out = []
    for i in range(1, n + 1):
        t = i / n
        out.append(((1 - t) ** 2 * p0[0] + 2 * (1 - t) * t * p1[0] + t * t * p2[0],
                    (1 - t) ** 2 * p0[1] + 2 * (1 - t) * t * p1[1] + t * t * p2[1]))
    return out


def stock_profile(sy0, sy1, base, butt_top=0.060, butt_bot=-0.032,
                  wrist_top=0.030, wrist_bot=-0.006):
    """Profilo (y,z) di un calcio: pettine e pancia sono curve, non rette."""
    L = max(0.05, sy1 - sy0)
    pts = [(sy0 + 0.017, base + butt_bot - 0.004),
           (sy0 + 0.003, base + butt_bot),
           (sy0, base + butt_top),
           (sy0 + 0.026, base + butt_top + 0.004)]
    pts += _bez3((sy0 + 0.026, base + butt_top + 0.004),
                 (sy0 + L * 0.78, base + butt_top + 0.002),
                 (sy1 - 0.016, base + wrist_top + 0.003), 5)
    pts += [(sy1, base + wrist_top - 0.002), (sy1, base + wrist_bot)]
    pts += _bez3((sy1, base + wrist_bot),
                 (sy0 + L * 0.60, base + wrist_bot - 0.006),
                 (sy0 + L * 0.28, base + butt_bot + 0.002), 5)
    pts += [(sy0 + 0.022, base + butt_bot - 0.002)]
    return pts



def guard_profile(y_back, y_front, z_top, depth, thick=0.005):
    """Contorno (y,z) di una guardia del grilletto: fondo piatto, bracci dritti."""
    d = depth
    return [
        (y_back, z_top),
        (y_back - 0.001, z_top - d * 0.55),
        (y_back + 0.005, z_top - d),
        (y_front - 0.005, z_top - d),
        (y_front + 0.001, z_top - d * 0.55),
        (y_front, z_top),
        (y_front - 0.004, z_top),
        (y_front - 0.005, z_top - d * 0.52),
        (y_front - 0.010, z_top - d + thick),
        (y_back + 0.010, z_top - d + thick),
        (y_back + 0.003, z_top - d * 0.52),
        (y_back + 0.003, z_top),
    ]


# ------------------------------------------------------------------ geometria

class Part:
    def __init__(self, name):
        self.name = name
        self.bm = bmesh.new()
        self.mats = []
        self.pivot = None
        self.uv_tile = UV_TILE

    def _mi(self, m):
        if m not in self.mats:
            self.mats.append(m)
        return self.mats.index(m)

    def _tag(self, verts, idx):
        for v in verts:
            for f in v.link_faces:
                f.material_index = idx

    def box(self, center, size, mat, rot=(0, 0, 0), bevel=None):
        idx = self._mi(mat)
        vs = bmesh.ops.create_cube(self.bm, size=1.0)["verts"]
        bmesh.ops.scale(self.bm, vec=Vector(size), verts=vs)
        if any(abs(a) > 1e-6 for a in rot):
            bmesh.ops.transform(self.bm, matrix=Euler(rot).to_matrix().to_4x4(), verts=vs)
        bmesh.ops.translate(self.bm, vec=Vector(center), verts=vs)
        self._tag(vs, idx)
        if bevel:
            geom = list({e for v in vs for e in v.link_edges})
            bmesh.ops.bevel(self.bm, geom=geom, offset=bevel, segments=2, affect="EDGES", profile=0.6)
        return self

    def cyl(self, center, radius, length, mat, axis="Y", seg=16, radius2=None, rot=(0, 0, 0)):
        idx = self._mi(mat)
        r2 = radius if radius2 is None else radius2
        vs = bmesh.ops.create_cone(self.bm, cap_ends=True, cap_tris=False, segments=seg,
                                   radius1=radius, radius2=r2, depth=length)["verts"]
        if axis == "Y":
            bmesh.ops.transform(self.bm, matrix=Euler((math.radians(90), 0, 0)).to_matrix().to_4x4(), verts=vs)
        elif axis == "X":
            bmesh.ops.transform(self.bm, matrix=Euler((0, math.radians(90), 0)).to_matrix().to_4x4(), verts=vs)
        if any(abs(a) > 1e-6 for a in rot):
            bmesh.ops.transform(self.bm, matrix=Euler(rot).to_matrix().to_4x4(), verts=vs)
        bmesh.ops.translate(self.bm, vec=Vector(center), verts=vs)
        self._tag(vs, idx)
        return self

    def sphere(self, center, radius, mat, seg=14, rings=8, scale=(1, 1, 1)):
        idx = self._mi(mat)
        vs = bmesh.ops.create_uvsphere(self.bm, u_segments=seg, v_segments=rings, radius=radius)["verts"]
        bmesh.ops.scale(self.bm, vec=Vector(scale), verts=vs)
        bmesh.ops.translate(self.bm, vec=Vector(center), verts=vs)
        self._tag(vs, idx)
        return self

    def sweep(self, profile, y0, y1, mat):
        """Profilo (x,z) estruso lungo la canna: carrelli, castelli, calcioli."""
        idx = self._mi(mat)
        a = [self.bm.verts.new((x, y0, z)) for (x, z) in profile]
        b = [self.bm.verts.new((x, y1, z)) for (x, z) in profile]
        self.bm.faces.new(list(reversed(a))).material_index = idx
        self.bm.faces.new(b).material_index = idx
        n = len(profile)
        for i in range(n):
            f = self.bm.faces.new((a[i], a[(i + 1) % n], b[(i + 1) % n], b[i]))
            f.material_index = idx
        return self

    def prism(self, profile, width, mat, x=0.0, taper=None):
        """Profilo (y,z) estruso in X: impugnature, caricatori, calci, lame."""
        idx = self._mi(mat)
        h = width / 2
        left = [self.bm.verts.new((x - h, y, z)) for (y, z) in profile]
        right = [self.bm.verts.new((x + h, y, z)) for (y, z) in profile]
        self.bm.faces.new(left).material_index = idx
        self.bm.faces.new(list(reversed(right))).material_index = idx
        n = len(profile)
        for i in range(n):
            a, b = left[i], left[(i + 1) % n]
            c, d = right[(i + 1) % n], right[i]
            self.bm.faces.new((a, b, c, d)).material_index = idx
        return self

    def prism_var(self, profile, halfwidth_fn, mat, x=0.0):
        """Profilo (y,z) estruso con spessore variabile: la lama si assottiglia sul filo."""
        idx = self._mi(mat)
        n = len(profile)
        left = [self.bm.verts.new((x - halfwidth_fn(y, z), y, z)) for (y, z) in profile]
        right = [self.bm.verts.new((x + halfwidth_fn(y, z), y, z)) for (y, z) in profile]
        self.bm.faces.new(left).material_index = idx
        self.bm.faces.new(list(reversed(right))).material_index = idx
        for i in range(n):
            a, b = left[i], left[(i + 1) % n]
            c, d = right[(i + 1) % n], right[i]
            self.bm.faces.new((a, b, c, d)).material_index = idx
        return self

    def tube(self, center, r_out, r_in, length, mat, axis="Y", seg=20, mat_in=None):
        """Tubo cavo vero (silenziatori, campane, calcioli): si vede il foro."""
        idx = self._mi(mat)
        idx_in = self._mi(mat_in or "stipple")
        ax = {"Y": 1, "X": 0, "Z": 2}[axis]
        u = {0: 1, 1: 2, 2: 0}[ax]
        v = {0: 2, 1: 0, 2: 1}[ax]

        def ring(r, off):
            out = []
            for i in range(seg):
                a = 2 * math.pi * i / seg
                p = [0.0, 0.0, 0.0]
                p[ax] = center[ax] + off
                p[u] = center[u] + math.cos(a) * r
                p[v] = center[v] + math.sin(a) * r
                out.append(self.bm.verts.new(p))
            return out

        h = length / 2
        o0, o1 = ring(r_out, -h), ring(r_out, h)
        i0, i1 = ring(r_in, -h), ring(r_in, h)
        for i in range(seg):
            j = (i + 1) % seg
            self.bm.faces.new((o0[i], o0[j], o1[j], o1[i])).material_index = idx
            self.bm.faces.new((i1[i], i1[j], i0[j], i0[i])).material_index = idx_in
            self.bm.faces.new((i0[i], i0[j], o0[j], o0[i])).material_index = idx
            self.bm.faces.new((o1[i], o1[j], i1[j], i1[i])).material_index = idx
        return self

    def arc(self, center, radius, thick, mat, a0, a1, steps=14, plane="YZ", width=None):
        """Tubo curvo solido: guardia del grilletto, anelli di cinghia, leve."""
        idx = self._mi(mat)
        width = thick if width is None else width
        rows = []
        for i in range(steps + 1):
            a = math.radians(a0 + (a1 - a0) * i / steps)
            c, s = math.cos(a), math.sin(a)
            if plane == "YZ":
                pts = [(center[0] - width / 2, center[1] + c * (radius - thick), center[2] + s * (radius - thick)),
                       (center[0] + width / 2, center[1] + c * (radius - thick), center[2] + s * (radius - thick)),
                       (center[0] + width / 2, center[1] + c * radius, center[2] + s * radius),
                       (center[0] - width / 2, center[1] + c * radius, center[2] + s * radius)]
            else:
                pts = [(center[0] + c * (radius - thick), center[1] - width / 2, center[2] + s * (radius - thick)),
                       (center[0] + c * (radius - thick), center[1] + width / 2, center[2] + s * (radius - thick)),
                       (center[0] + c * radius, center[1] + width / 2, center[2] + s * radius),
                       (center[0] + c * radius, center[1] - width / 2, center[2] + s * radius)]
            rows.append([self.bm.verts.new(p) for p in pts])
        for i in range(steps):
            a, b = rows[i], rows[i + 1]
            for k in range(4):
                f = self.bm.faces.new((a[k], a[(k + 1) % 4], b[(k + 1) % 4], b[k]))
                f.material_index = idx
        self.bm.faces.new(rows[0]).material_index = idx
        self.bm.faces.new(list(reversed(rows[-1]))).material_index = idx
        return self

    # ---------------------------------------------------------- dettagli

    def rail(self, y0, y1, z, width, mat, teeth=None, rise=0.0):
        """Rotella Picatinny: passo vero 10 mm, denti bassi con lo smusso."""
        self.box((0, (y0 + y1) / 2, z + 0.0020 + rise), (width, y1 - y0, 0.0040), mat, bevel=0.0006)
        n = max(3, int(round((y1 - y0) / 0.0105)))
        pitch = (y1 - y0) / n
        for i in range(n):
            yy = y0 + (i + 0.5) * pitch
            self.box((0, yy, z + 0.0048 + rise), (width * 0.985, pitch * 0.56, 0.0022), mat, bevel=0.0005)
        return self

    def slots(self, y0, y1, z, x, w, h, mat, n=5, side=1):
        for i in range(n):
            yy = y0 + (i + 0.5) * (y1 - y0) / n
            self.box((side * x, yy, z), (0.0022, w, h), mat)
        return self

    def screw(self, center, r, mat, axis="X", length=0.004, slot=True):
        self.cyl(center, r, length, mat, axis=axis, seg=10)
        if slot:
            d = length / 2 + 0.0003
            if axis == "X":
                self.box((center[0] + d * 0.5, center[1], center[2]), (0.0006, r * 1.7, r * 0.42), "stipple")
            elif axis == "Y":
                self.box((center[0], center[1] + d * 0.5, center[2]), (r * 1.7, 0.0006, r * 0.42), "stipple")
        return self

    def serrations(self, y0, y1, zc, h, x, n, mat, depth=0.0007):
        """Incisioni di presa: sottili, fitte, leggermente incassate."""
        for i in range(n):
            yy = y0 + (i + 0.5) * (y1 - y0) / n
            w = (y1 - y0) / n * 0.52
            self.box((x, yy, zc), (0.0012, w, h), "void")

    def finish(self, parent, bevel=BEVEL):
        bmesh.ops.recalc_face_normals(self.bm, faces=self.bm.faces)
        if self.pivot:
            p = Vector(self.pivot)
            for v in self.bm.verts:
                v.co -= p
        me = bpy.data.meshes.new(self.name)
        self.bm.to_mesh(me)
        self.bm.free()
        for m in self.mats:
            me.materials.append(material(m))
        ob = bpy.data.objects.new(self.name, me)
        bpy.context.scene.collection.objects.link(ob)
        ob.parent = parent
        if self.pivot:
            ob.location = self.pivot

        bpy.ops.object.select_all(action="DESELECT")
        ob.select_set(True)
        bpy.context.view_layer.objects.active = ob

        if bevel > 0:
            md = ob.modifiers.new("bev", "BEVEL")
            md.width = bevel
            md.segments = 2
            md.limit_method = "ANGLE"
            md.angle_limit = math.radians(38)
            md.harden_normals = True
            md.use_clamp_overlap = True
            bpy.ops.object.modifier_apply(modifier=md.name)

        bpy.ops.object.mode_set(mode="EDIT")
        bpy.ops.mesh.select_all(action="SELECT")
        bpy.ops.uv.cube_project(cube_size=self.uv_tile, correct_aspect=True, scale_to_bounds=False)
        bpy.ops.object.mode_set(mode="OBJECT")
        bpy.ops.object.shade_auto_smooth(angle=math.radians(34))
        return ob


def empty(parent, name, loc=(0, 0, 0)):
    e = bpy.data.objects.new(name, None)
    e.empty_display_size = 0.02
    e.parent = parent
    e.location = loc
    bpy.context.scene.collection.objects.link(e)
    return e


def clear_scene():
    for ob in list(bpy.data.objects):
        bpy.data.objects.remove(ob, do_unlink=True)
    for me in list(bpy.data.meshes):
        if me.users == 0:
            bpy.data.meshes.remove(me)
    _MATS.clear()


# ===================================================================== PISTOLE

def pistol(P, s):
    """
    Pistola moderna: carrello a sezione ottagonale, fusto con dust cover e rotaia,
    impugnatura a profilo, guardia integrata. Tutto quello che si vede in prima persona.
    """
    pal = palette(s.get("look", "polymer"), s.get("mats"))
    body, metal, dark, accent = pal["body"], pal["metal"], pal["dark"], pal["accent"]
    w = s.get("width", 0.030)
    L = s.get("slide_len", 0.186)
    zf0, zf1 = 0.008, 0.030                 # fusto
    zs0, zs1 = 0.030, 0.058                 # carrello
    y0 = -0.080
    y1 = y0 + L
    zm = (zs0 + zs1) / 2

    # ---------------------------------------------------------------- carrello
    hw = w / 2
    prof = [(-hw, zs0), (hw, zs0), (hw, zs1 - 0.0035), (hw * 0.74, zs1),
            (-hw * 0.74, zs1), (-hw, zs1 - 0.0035)]
    P["slide"].sweep(prof, y0, y1, metal)
    # muso: corona smussata, non una piastra appoggiata
    P["slide"].cyl((0, y1 - 0.002, zm), w * 0.47, 0.006, metal, seg=24, radius2=w * 0.44)
    # presa posteriore: 8 incisioni fitte, oblique
    P["slide"].serrations(y0 + 0.008, y0 + 0.042, zm - 0.001, 0.020, hw * 1.005, 8, dark)
    P["slide"].serrations(y0 + 0.008, y0 + 0.042, zm - 0.001, 0.020, -hw * 1.005, 8, dark)
    # presa anteriore: 3 incisioni
    P["slide"].serrations(y1 - 0.052, y1 - 0.020, zm - 0.002, 0.016, hw * 1.005, 3, dark)
    P["slide"].serrations(y1 - 0.052, y1 - 0.020, zm - 0.002, 0.016, -hw * 1.005, 3, dark)

    # finestrino d'espulsione, incassato (fianco destro)
    P["slide"].box((hw - 0.0016, y1 - 0.062, zs1 - 0.0055), (0.0022, 0.036, 0.0135), "void")
    P["slide"].box((hw - 0.0012, y1 - 0.044, zs1 - 0.0055), (0.0020, 0.008, 0.0140), metal)  # estrattore
    # fessura della leva di scatto (lato sinistro)
    P["slide"].box((-hw + 0.0008, y1 - 0.060, zs0 + 0.010), (0.0018, 0.030, 0.0045), "void")

    # mire: TACCA e MIRINO alla STESSA quota H. La linea di mira passa sul filo delle
    # orecchie della tacca e sulla punta del mirino; se una delle due è più bassa, in
    # puntamento si vede il mirino galleggiare sopra o sotto l'intaglio.
    H = zs1 + 0.0125
    ry = y0 + 0.010
    P["slide"].box((0, ry, H - 0.012), (0.0175, 0.009, 0.010), metal, bevel=0.0012)
    for sx in (-1, 1):
        P["slide"].box((sx * 0.0056, ry, H - 0.005), (0.0054, 0.0085, 0.010), metal, bevel=0.0010)
    P["slide"].box((0, ry, H - 0.008), (0.0026, 0.0075, 0.004), "void")
    fy = y1 - 0.008
    P["slide"].box((0, fy, H - 0.0065), (0.0042, 0.008, 0.013), metal, bevel=0.0012)
    P["slide"].box((0, fy - 0.0032, H - 0.004), (0.0028, 0.0016, 0.0034), accent)

    # volata: corona + anima scura
    P["slide"].cyl((0, y1 - 0.006, zm - 0.001), 0.0088, 0.010, metal, seg=22)
    P["slide"].cyl((0, y1 - 0.0012, zm - 0.001), 0.0056, 0.004, dark, seg=18)
    P["slide"].cyl((0, y1 - 0.0032, zm - 0.001), 0.0072, 0.003, metal, seg=22)

    # ---------------------------------------------------------------- fusto
    # dust cover con rotaia
    P["body"].box((0, y1 - 0.062, (zf0 + zf1) / 2), (w * 0.90, 0.124, zf1 - zf0), body, bevel=0.0012)
    P["body"].box((0, y1 - 0.062, zf0 - 0.0022), (w * 0.62, 0.104, 0.006), body, bevel=0.0012)
    for i in range(3):
        P["body"].box((0, y1 - 0.098 + i * 0.021, zf0 - 0.0055), (w * 0.66, 0.010, 0.0045), dark)
    # corpo del fusto (davanti alla guardia)
    P["body"].box((0, y1 - 0.020, (zf0 + zf1) / 2 + 0.001), (w * 0.94, 0.040, zf1 - zf0 - 0.002), body)
    # spalle laterali / letto del carrello
    P["body"].box((0, -0.012, zf1 - 0.0015), (w * 0.98, 0.152, 0.004), body, bevel=0.0008)

    # ---------------------------------------------------------------- impugnatura
    ang = s.get("grip_angle", 17)
    ztop = 0.016                      # il collo entra nel fusto: nessuna fessura
    gh = s.get("grip_h", 0.086)
    prof = strip_profile(-0.006, ztop, gh, 0.052, ang - 3, ang + 6, steps=6, taper=0.84, flat_top=True)
    P["body"].prism(prof, w * 0.94, dark)
    # guancette: appena rilevate, con il bordo arrotondato
    for sx in (-1, 1):
        P["body"].box((sx * w * 0.452, -0.006 - math.sin(math.radians(ang)) * 0.040,
                       ztop - math.cos(math.radians(ang)) * 0.040),
                      (0.0022, 0.038, 0.046), body, rot=(math.radians(-ang), 0, 0), bevel=0.006)
    # raccordo col fusto (chiude ogni cuneo)
    P["body"].box((0, -0.014, 0.006), (w * 0.97, 0.062, 0.026), body, bevel=0.002)
    # beavertail: si allunga sopra la mano
    P["body"].box((0, -0.042, 0.018), (w * 0.86, 0.036, 0.014), body,
                  rot=(math.radians(-9), 0, 0), bevel=0.0035)
    # front strap con le gole per le dita
    ca, sa = math.cos(math.radians(ang)), math.sin(math.radians(ang))
    for i in range(4):
        t = 0.020 + i * 0.0145
        P["body"].box((0, -0.010 + 0.028 * ca - sa * t, ztop - ca * t + 0.028 * sa),
                      (w * 0.86, 0.005, 0.0055), body, rot=(math.radians(-ang), 0, 0), bevel=0.0018)
    # backstrap morbido
    P["body"].box((0, -0.010 - 0.028 * ca - sa * 0.040, ztop - ca * 0.040 + 0.028 * sa),
                  (w * 0.93, 0.006, 0.062), body, rot=(math.radians(-ang), 0, 0), bevel=0.0022)
    # base dell'impugnatura
    P["body"].box((0, -0.010 - sa * gh, ztop - ca * gh), (w * 1.00, 0.058, 0.008), body,
                  rot=(math.radians(-ang), 0, 0), bevel=0.0022)

    # ---------------------------------------------------------------- guardia e comandi
    P["body"].prism(guard_profile(-0.002, 0.060, 0.010, 0.030), 0.0125, body)
    P["body"].box((0, 0.058, 0.006), (0.0125, 0.012, 0.010), body, bevel=0.002)   # attacco davanti
    P["body"].box((0, 0.001, 0.004), (0.0125, 0.014, 0.014), body, bevel=0.002)   # attacco dietro
    # sgancio caricatore
    P["body"].cyl((-w * 0.50, 0.008, -0.002), 0.0062, 0.005, metal, axis="X", seg=12)
    P["body"].box((-w * 0.505, 0.008, 0.004), (0.004, 0.012, 0.007), metal, bevel=0.001)
    # leva di scatto / hold open
    P["body"].box((-w * 0.505, 0.036, zf1 - 0.004), (0.0045, 0.030, 0.006), metal, bevel=0.0012)
    P["body"].box((-w * 0.505, 0.050, zf1 - 0.010), (0.0050, 0.012, 0.008), metal, bevel=0.0012)
    # leva di smontaggio
    P["body"].cyl((-w * 0.50, 0.030, zf0 + 0.006), 0.0055, 0.005, metal, axis="X", seg=10)
    P["body"].box((-w * 0.505, 0.030, zf0 + 0.010), (0.0035, 0.014, 0.005), metal, bevel=0.0008)
    # perno del cane / fusto
    P["body"].cyl((-w * 0.50, y0 + 0.012, zf1 - 0.006), 0.0042, 0.005, metal, axis="X", seg=10)

    # ---------------------------------------------------------------- caricatore
    gh2 = s.get("grip_h", 0.086)
    ml = min(s.get("mag_len", gh2), gh2 - 0.004)
    P["mag"].box((0, -0.010, ztop - 0.010 - ml / 2), (w * 0.80, 0.046, ml),
                 metal, rot=(math.radians(-ang), 0, 0), bevel=0.002)
    P["mag"].box((0, -0.010 - sa * (gh2 - 0.002), ztop - ca * (gh2 - 0.002)),
                 (w * 0.95, 0.058, 0.008), dark, rot=(math.radians(-ang), 0, 0), bevel=0.002)
    for i in range(2):
        P["mag"].box((0.0, -0.010 - sa * (0.030 + i * 0.026), ztop - ca * (0.030 + i * 0.026)),
                     (w * 0.82, 0.040, 0.004), metal, rot=(math.radians(-ang), 0, 0))

    # ---------------------------------------------------------------- cane e grilletto
    if s.get("hammer", True):
        hy, hz = y0 + 0.016, zs1 - 0.002
        P["hammer"].pivot = (-hy * 0 - 0.0, hy, hz - 0.010)
        P["hammer"].box((0, hy + 0.006, hz + 0.002), (0.0060, 0.014, 0.009), metal,
                        rot=(math.radians(-26), 0, 0), bevel=0.0012)
        P["hammer"].box((0, hy + 0.011, hz + 0.007), (0.0074, 0.009, 0.006), metal,
                        rot=(math.radians(-42), 0, 0), bevel=0.0014)
    ty, tz = 0.032, 0.004
    P["trigger"].pivot = (0.0, ty, tz + 0.012)
    P["trigger"].box((0, ty - 0.001, tz - 0.008), (0.0055, 0.0075, 0.024), metal,
                     rot=(math.radians(-8), 0, 0), bevel=0.0016)
    P["trigger"].box((0, ty - 0.004, tz + 0.003), (0.0055, 0.010, 0.005), metal,
                     rot=(math.radians(-8), 0, 0), bevel=0.0010)

    # ---------------------------------------------------------------- accessori
    if s.get("comp"):
        P["body"].box((0, y1 + 0.006, zm), (w * 1.06, 0.030, (zs1 - zs0) * 0.86), metal, bevel=0.0025)
        for i in range(3):
            P["body"].box((0, y1 - 0.002 + i * 0.009, zs1 + 0.001), (w * 0.72, 0.0045, 0.007), dark)
        P["body"].cyl((0, y1 + 0.022, zm), 0.0060, 0.006, dark, seg=16)
    if s.get("suppressor"):
        sl = s.get("supp_len", 0.150)
        r = s.get("supp_r", 0.0185)
        P["body"].cyl((0, y1 + 0.008, zm - 0.001), r * 0.80, 0.014, metal, seg=20)
        P["body"].tube((0, y1 + 0.008 + sl / 2, zm - 0.001), r, 0.0058, sl, body, seg=22, mat_in="stipple")
        for i in range(3):
            P["body"].cyl((0, y1 + 0.030 + i * 0.048, zm - 0.001), r * 1.02, 0.005, metal, seg=22)
        P["body"].cyl((0, y1 + 0.008 + sl - 0.010, zm - 0.001), r * 0.98, 0.012, metal, seg=22)
    if s.get("optic") == "dot":
        P["slide"].box((0, y0 + 0.030, zs1 + 0.006), (0.024, 0.046, 0.010), metal, bevel=0.0015)
        P["slide"].box((0, y0 + 0.030, zs1 + 0.016), (0.026, 0.038, 0.014), dark, bevel=0.0025)
        P["slide"].cyl((0, y0 + 0.048, zs1 + 0.018), 0.011, 0.004, "glass", axis="Y", seg=14)

    if s.get("suppressor"):
        muzzle = (0.0, y1 + 0.008 + s.get("supp_len", 0.150) + 0.004, zm - 0.001)
    elif s.get("comp"):
        muzzle = (0.0, y1 + 0.024, zm)
    else:
        muzzle = (0.0, y1 + 0.002, zm - 0.001)

    return dict(
        muzzle=muzzle,
        eject=(hw + 0.004, y1 - 0.058, zs1 - 0.004),
        aim=(0.0, y0 + 0.012, zs1 + (0.030 + s.get("dot_rise", 0.0) if s.get("optic") == "dot" else 0.0125)),  # = H delle mire
        # occhio del tiratore: sulla linea di mira, 15,5 cm dietro la tacca. È il punto che
        # il viewmodel porta sull'asse della camera in ADS (solo la mira non basta: per un
        # fucile l'aim sta quasi alla volata e l'arma finirebbe dietro l'obiettivo).
        eye=(0.0, y0 - 0.145, zs1 + (0.030 + s.get("dot_rise", 0.0) if s.get("optic") == "dot" else 0.0125)),
        hand_r=(0.0, -0.014, -0.030),
        hand_l=(0.0, 0.020, -0.048),
    )


# ===================================================================== RIVOLTELLE

def revolver(P, s):
    pal = palette(s.get("look", "steel"))
    body, metal, dark, accent = pal["body"], pal["metal"], pal["dark"], pal["accent"]
    w = s.get("width", 0.036)
    dz = s.get("axis_z", 0.034)          # asse canna/tamburo
    drum_r = s.get("drum_r", 0.021)
    drum_y = s.get("drum_y", 0.002)
    bl = s.get("barrel", 0.10)
    top = dz + drum_r + 0.002        # superficie della fascia superiore, subito sopra il tamburo

    # telaio: fascia inferiore, spalle, ponte superiore
    P["body"].box((0, -0.030, dz - drum_r - 0.004), (w * 0.92, 0.100, 0.020), metal, bevel=0.003)
    # culatta: avvolge il tamburo ma NON arriva alla linea di mira (copriva la tacca)
    P["body"].box((0, -0.052, dz - 0.007), (w * 0.94, 0.056, drum_r * 2 + 0.014), metal, bevel=0.004)
    P["body"].box((0, -0.004, top), (w * 0.72, 0.086, 0.008), metal, bevel=0.0022)   # topstrap
    P["body"].box((0, 0.030, dz - 0.006), (w * 0.78, 0.038, drum_r + 0.010), metal, bevel=0.003)  # forcella
    # finestra del tamburo sul fianco destro
    P["body"].box((w * 0.40, -0.004, dz), (0.004, 0.052, drum_r * 1.6), dark, bevel=0.001)

    # canna con asta estrattrice sotto e bindella sopra
    P["body"].cyl((0, 0.030 + bl / 2, dz), s.get("barrel_r", 0.0105), bl, metal, seg=22)
    P["body"].box((0, 0.030 + bl / 2, dz - 0.014), (w * 0.42, bl * 0.94, 0.011), metal, bevel=0.0022)
    P["body"].cyl((0, 0.030 + bl / 2, dz - 0.019), 0.0055, bl * 0.92, metal, seg=12)
    if s.get("rib"):
        P["body"].box((0, 0.030 + bl / 2, dz + 0.011), (0.011, bl, 0.007), metal, bevel=0.0016)
        for i in range(4):
            P["body"].box((0, 0.030 + 0.02 + i * (bl - 0.04) / 4, dz + 0.012), (0.008, 0.008, 0.0035), dark)
    # volata: corona, anima, mirino
    P["body"].cyl((0, 0.030 + bl - 0.002, dz), s.get("barrel_r", 0.0105) * 1.06, 0.008, metal, seg=22)
    P["body"].cyl((0, 0.030 + bl + 0.001, dz), s.get("barrel_r", 0.0105) * 0.55, 0.004, dark, seg=18)
    # mire: anche qui una quota sola. Il mirino sale dalla bindella fino a H, la tacca
    # è un intaglio nella fascia superiore alla stessa quota (prima il mirino era 15 mm
    # sotto la tacca: in mira si vedeva il vuoto sopra il mirino).
    H = top + 0.0085
    fy = 0.030 + bl * 0.86
    fbase = dz + s.get("barrel_r", 0.0105)
    P["body"].box((0, fy, (fbase + H) / 2), (0.0034, 0.012, max(0.008, H - fbase)), metal, bevel=0.0010)
    P["body"].box((0, fy - 0.005, H - 0.0022), (0.0032, 0.003, 0.0042), accent)
    # tacca: due orecchie sulla fascia con l'intaglio in mezzo (la linea passa sul filo)
    P["body"].box((0, -0.046, H - 0.012), (0.017, 0.010, 0.010), metal, bevel=0.0012)
    for sx in (-1, 1):
        P["body"].box((sx * 0.0056, -0.046, H - 0.005), (0.0054, 0.009, 0.010), metal, bevel=0.0010)
    P["body"].box((0, -0.046, H - 0.004), (0.0026, 0.008, 0.004), "void")

    # tamburo (parte mobile: ruota di 60° a ogni colpo)
    P["slide"].pivot = (0.0, drum_y, dz)
    P["slide"].cyl((0, drum_y, dz), drum_r, 0.048, metal, axis="Y", seg=22)
    P["slide"].cyl((0, drum_y + 0.025, dz), drum_r * 0.98, 0.003, dark, axis="Y", seg=22)
    for i in range(6):
        a = i * math.pi / 3
        P["slide"].cyl((math.cos(a) * drum_r * 0.60, drum_y - 0.025, dz + math.sin(a) * drum_r * 0.60),
                       0.0048, 0.007, dark, axis="Y", seg=8)
        P["slide"].box((math.cos(a) * drum_r * 0.97, drum_y, dz + math.sin(a) * drum_r * 0.97),
                       (0.0035, 0.042, 0.006), "void", rot=(0, -a, 0))
    P["slide"].cyl((0, drum_y, dz), 0.0062, 0.056, metal, axis="Y", seg=12)
    P["slide"].box((0, drum_y - 0.026, dz), (0.012, 0.005, 0.012), metal, rot=(0, 0, math.radians(45)))

    # impugnatura a profilo, con il codolo che entra nel telaio
    ang = s.get("grip_angle", 19)
    ztop = dz - drum_r - 0.006
    gh = s.get("grip_h", 0.100)
    prof = strip_profile(-0.040, ztop + 0.004, gh, 0.050, ang, ang + 4, steps=5, taper=0.86)
    P["body"].prism(prof, w * 0.86, body)
    for sx in (-1, 1):
        P["body"].box((sx * w * 0.44, -0.052, ztop - 0.030), (0.006, 0.040, 0.058), dark,
                      rot=(math.radians(-ang), 0, 0), bevel=0.003)
    P["body"].box((0, -0.040 - math.sin(math.radians(ang)) * gh,
                   ztop - math.cos(math.radians(ang)) * gh), (w * 0.90, 0.052, 0.010), metal,
                  rot=(math.radians(-ang), 0, 0), bevel=0.0025)
    P["body"].cyl((-w * 0.45, -0.048, ztop - 0.016), 0.0045, 0.004, metal, axis="X", seg=10)

    # guardia e grilletto
    P["body"].arc((0, 0.004, dz - drum_r - 0.004), 0.027, 0.005, metal, 198, 342, steps=14,
                  plane="YZ", width=0.010)
    P["body"].box((0, -0.022, dz - drum_r - 0.004), (0.010, 0.012, 0.010), metal, bevel=0.002)
    P["trigger"].pivot = (0.0, -0.002, dz - drum_r + 0.006)
    P["trigger"].box((0, -0.002, dz - drum_r - 0.010), (0.0050, 0.007, 0.022), metal,
                     rot=(math.radians(-12), 0, 0), bevel=0.0014)
    # cane
    # cane: la cresta resta SOTTO la linea di mira, altrimenti sporge sopra la tacca
    P["hammer"].pivot = (0.0, -0.056, top - 0.010)
    P["hammer"].box((0, -0.058, top - 0.006), (0.0070, 0.014, 0.020), metal,
                    rot=(math.radians(-16), 0, 0), bevel=0.0014)
    P["hammer"].box((0, -0.050, top + 0.001), (0.0085, 0.018, 0.007), metal,
                    rot=(math.radians(-34), 0, 0), bevel=0.0018)
    # leva d'apertura del tamburo
    P["body"].box((-w * 0.40, -0.030, dz + 0.004), (0.004, 0.030, 0.006), metal, bevel=0.0012)

    return dict(
        muzzle=(0.0, 0.030 + bl + 0.006, dz),
        eject=(w * 0.42, drum_y, dz + 0.004),
        aim=(0.0, 0.030 + bl * 0.86, top + 0.0085),   # = H delle mire
        eye=(0.0, -0.201, top + 0.0085),
        hand_r=(0.0, -0.046, ztop - 0.030),
        hand_l=(0.0, -0.010, ztop - 0.060),
    )


# ===================================================================== ARMI LUNGHE

def long_gun(P, s):
    """
    Una grammatica sola per SMG, fucili, pompe, cecchini e mitragliatrici.
    Il castello è diviso in upper/lower con la cucitura, il calcio ha un profilo
    sportivo vero, l'impugnatura si innesta nel castello senza fessure.
    """
    pal = palette(s.get("look", "polymer"), s.get("mats"))
    body, metal, dark, accent = pal["body"], pal["metal"], pal["dark"], pal["accent"]
    rv = pal.get("recv", body)
    w = s.get("width", 0.048)
    ry0, ry1 = s.get("recv", (0.0, 0.26))
    rz0, rz1 = s.get("recv_z", (0.0, 0.078))
    bz = s.get("barrel_z", rz1 - 0.012)
    rb = s.get("barrel_r", 0.0105)
    barrel = s.get("barrel", 0.34)
    yb0 = ry1 - 0.014
    yb1 = yb0 + barrel
    hw = w / 2
    zsplit = rz0 + 0.016

    # ---------------------------------------------------------------- castello
    # Sezione con gli spigoli SMUSSATI, estrusa: non è una scatola. Lo smusso
    # raccoglie la luce lungo tutto il fianco, che era il difetto "scatoletta piatta".
    ch = 0.0030
    prof_up = [
        (-hw, zsplit + ch), (-hw + ch, zsplit), (hw - ch, zsplit), (hw, zsplit + ch),
        (hw, rz1 - 0.0036), (hw - 0.0036, rz1), (-hw + 0.0036, rz1), (-hw, rz1 - 0.0036),
    ]
    P["body"].sweep(prof_up, ry0 + 0.002, ry1 - 0.002, rv)
    # lower (più stretto: si vede lo scalino), anche lui smussato
    ch2 = 0.0024
    lw = hw * 0.94
    prof_low = [
        (-lw, rz0 + ch2), (-lw + ch2, rz0), (lw - ch2, rz0), (lw, rz0 + ch2),
        (lw, zsplit - 0.0015), (-lw, zsplit - 0.0015),
    ]
    P["body"].sweep(prof_low, ry0 + 0.006, ry1 - 0.006, rv)
    # cucitura upper/lower: linea d'ombra continua
    P["body"].box((0, (ry0 + ry1) / 2, zsplit), (w * 1.006, ry1 - ry0 - 0.010, 0.0014), "void")
    # pannello stampato IN INCASSO sul fianco: è quello che fa leggere il castello
    # come un pezzo lavorato e non come una scatola con una texture sopra
    for sx in (-1, 1):
        py0, py1 = ry0 + 0.030, ry1 - 0.055
        pz0, pz1 = zsplit + 0.012, rz1 - 0.012
        P["body"].box((sx * (hw - 0.0016), (py0 + py1) / 2, (pz0 + pz1) / 2),
                      (0.0030, py1 - py0, pz1 - pz0), "void")
        # bordo del pannello (labbro appena rilevato)
        for dz in (pz0, pz1):
            P["body"].box((sx * (hw - 0.0004), (py0 + py1) / 2, dz),
                          (0.0016, py1 - py0 + 0.004, 0.0022), metal, bevel=0.0006)
        for dy in (py0, py1):
            P["body"].box((sx * (hw - 0.0004), dy, (pz0 + pz1) / 2),
                          (0.0016, 0.0022, pz1 - pz0 + 0.004), metal, bevel=0.0006)
        # rivetti del pannello
        for dy in (py0 + 0.010, py1 - 0.010):
            for dz in (pz0 + 0.006, pz1 - 0.006):
                P["body"].cyl((sx * (hw + 0.0002), dy, dz), 0.0022, 0.0018, metal, axis="X", seg=8)
    # coperchio superiore a trapezio
    P["body"].sweep([(-hw * 0.98, rz1 - 0.005), (hw * 0.98, rz1 - 0.005),
                     (hw * 0.60, rz1 + 0.004), (-hw * 0.60, rz1 + 0.004)],
                    ry0 + 0.003, ry1 - 0.003, metal)
    # culatta
    P["body"].box((0, ry0 - 0.004, (rz0 + rz1) / 2), (w * 0.96, 0.010, (rz1 - rz0) * 0.90),
                  metal, bevel=0.0018)
    # portello d'espulsione (sportello chiuso) sul fianco destro
    P["body"].box((hw + 0.0012, ry1 - 0.062, rz1 - 0.022), (0.0030, 0.058, 0.022),
                  metal, bevel=0.0012)
    P["body"].box((hw + 0.0020, ry1 - 0.036, rz1 - 0.034), (0.0032, 0.010, 0.006), metal, bevel=0.0010)
    P["body"].box((hw - 0.0006, ry1 - 0.062, rz1 - 0.033), (0.0020, 0.052, 0.0022), "void")
    # apertura d'espulsione incassata (dove esce il bossolo)
    P["body"].box((hw - 0.0012, ry1 - 0.048, rz1 - 0.016), (0.0022, 0.034, 0.012), "void")
    # deflettore
    P["body"].box((hw, ry1 - 0.082, rz1 - 0.020), (0.011, 0.010, 0.024), metal,
                  rot=(0, math.radians(18), 0), bevel=0.0014)
    # perni del gruppo scatto: due cilindri che passano da parte a parte
    for dy in (0.010, 0.036):
        P["body"].cyl((0, s.get("trig_y", ry1 - 0.170) + dy, rz0 + 0.006), 0.0032, w * 1.01,
                      metal, axis="X", seg=10)
    # manetta d'armamento
    ch = s.get("charging", "right")
    if ch == "right":
        P["body"].box((hw - 0.0008, ry1 - 0.130, rz1 - 0.008), (0.0022, 0.075, 0.006), "void")
        P["body"].box((hw + 0.005, ry1 - 0.120, rz1 - 0.010), (0.014, 0.024, 0.010), metal, bevel=0.0016)
        P["body"].box((hw + 0.010, ry1 - 0.108, rz1 - 0.010), (0.006, 0.014, 0.014), metal, bevel=0.0014)
    elif ch == "top":
        P["body"].box((0, ry1 - 0.070, rz1 + 0.010), (0.014, 0.034, 0.007), metal, bevel=0.0015)
        P["body"].box((0, ry1 - 0.130, rz1 + 0.002), (0.008, 0.090, 0.004), dark)
    # selettore di fuoco, sgancio caricatore, bolt catch, presa QD
    P["body"].cyl((-hw - 0.003, ry1 - 0.150, rz0 + 0.024), 0.0078, 0.007, metal, axis="X", seg=12)
    P["body"].box((-hw - 0.004, ry1 - 0.150, rz0 + 0.034), (0.0045, 0.016, 0.009), metal, bevel=0.0012)
    P["body"].box((-hw - 0.010, ry1 - 0.150, rz0 + 0.024), (0.006, 0.010, 0.010), dark, bevel=0.0014)
    P["body"].box((hw - 0.003, ry1 - 0.176, rz0 + 0.010), (0.006, 0.016, 0.012), dark, bevel=0.0016)
    P["body"].box((-hw + 0.002, ry0 + 0.070, rz0 + 0.014), (0.004, 0.030, 0.010), metal, bevel=0.0012)
    P["body"].cyl((-hw - 0.002, ry0 + 0.030, rz0 + 0.008), 0.0055, 0.006, metal, axis="X", seg=10)

    if s.get("rail", "top") == "top":
        P["body"].rail(ry0 + 0.014, ry1 - 0.014, rz1 + 0.004, 0.021, metal, teeth=s.get("rail_teeth", 8))

    # ---------------------------------------------------------------- canna e blocco
    P["body"].cyl((0, (yb0 + yb1) / 2, bz), rb * 1.06, barrel, metal, seg=s.get("barrel_seg", 22),
                  radius2=rb * 0.88)
    P["body"].box((0, yb0 + 0.020, bz), (w * 0.58, 0.044, (rz1 - rz0) * 0.42), metal, bevel=0.0022)
    P["body"].box((0, ry1 - 0.020, rz1 - 0.004), (w * 0.72, 0.040, 0.026), metal, bevel=0.0022)
    P["body"].cyl((0, yb1 - 0.004, bz), rb * 1.06, 0.008, metal, seg=22)
    P["body"].cyl((0, yb1, bz), rb * 0.52, 0.004, dark, seg=16)
    if s.get("gas_block"):
        gy = yb0 + barrel * s.get("gas_frac", 0.62)
        # il blocco gas sta SOTTO la linea di mira: il tubo sporgeva di 18 mm e in
        # puntamento si vedeva un cilindro al posto del mirino
        P["body"].box((0, gy, bz + rb + 0.006), (w * 0.44, 0.028, 0.020), metal, bevel=0.0024)
        P["body"].cyl((0, gy, bz + rb + 0.012), 0.006, 0.026, metal, axis="Y", seg=10)
        P["body"].cyl((0, gy - 0.026, bz + rb + 0.006), 0.0032, 0.130, metal, axis="Y", seg=8)
        P["body"].box((0, gy, bz - rb - 0.008), (0.016, 0.026, 0.012), metal, bevel=0.0016)

    # ---------------------------------------------------------------- paramano
    hg0 = ry1 - 0.020
    hg1 = yb0 + barrel * s.get("hg_frac", 0.70)
    if s.get("handguard", True):
        hr = s.get("hg_r", 0.027)
        eight = (0, math.radians(22.5), 0)
        P["body"].cyl((0, (hg0 + hg1) / 2, bz), hr, hg1 - hg0, body, seg=8, rot=eight)
        P["body"].cyl((0, hg0 + 0.012, bz), hr * 1.03, 0.026, metal, seg=8, rot=eight)
        P["body"].cyl((0, hg1 - 0.008, bz), hr * 0.97, 0.014, metal, seg=8, rot=eight)
        n = s.get("vents", 5)
        for i in range(n):
            yy = hg0 + 0.034 + (i + 0.5) * (hg1 - hg0 - 0.056) / n
            for sx in (-1, 1):
                # asola M-LOK: solo l'ombra, nessun rilievo
                P["body"].box((sx * hr * 0.912, yy, bz), (0.0026, 0.030, 0.009), "void")
            P["body"].box((0, yy, bz + hr * 0.88), (0.016, 0.032, 0.004), "void")
            P["body"].box((0, yy, bz - hr * 0.90), (0.018, 0.030, 0.004), "void")
        if s.get("hg_rail", True):
            P["body"].rail(hg0 + 0.026, hg1 - 0.020, bz - hr * 0.92, 0.022, metal, teeth=6)
        for yy in (hg0 + 0.010, hg1 - 0.014):
            P["body"].cyl((0, yy, bz), hr * 1.01, 0.008, metal, seg=8, rot=eight)
        if s.get("hg_tape"):
            for yy in (hg0 + 0.070, hg0 + 0.150):
                P["body"].cyl((0, yy, bz), hr * 1.03, 0.020, "cloth", seg=8, rot=eight)

    # scudo termico (mitragliatrici)
    if s.get("heat_shield"):
        sh0 = yb0 + 0.050
        sh1 = sh0 + s.get("shield_len", 0.24)
        P["body"].cyl((0, (sh0 + sh1) / 2, bz), 0.030, sh1 - sh0, metal, seg=18)
        for i in range(6):
            yy = sh0 + (i + 0.5) * (sh1 - sh0) / 6
            P["body"].box((0, yy, bz + 0.028), (0.040, 0.020, 0.006), dark)
            P["body"].box((0, yy, bz - 0.028), (0.040, 0.020, 0.006), dark)
        P["body"].cyl((0, sh0, bz), 0.033, 0.014, metal, seg=18)
        P["body"].cyl((0, sh1, bz), 0.032, 0.012, metal, seg=18)

    # astina della pompa: sta sotto la canna, attorno al tubo del caricatore
    if s.get("pump"):
        py = yb0 + s.get("pump_y", 0.12)
        pl = s.get("pump_len", 0.20)
        pz = bz - 0.024
        # tubo del caricatore, dalla culatta fin quasi alla volata
        P["body"].cyl((0, (yb0 + yb1 - 0.03) / 2, pz), 0.0105, (yb1 - 0.03) - yb0, metal, seg=16)
        P["body"].cyl((0, yb1 - 0.030, pz), 0.012, 0.012, metal, seg=16)
        # fascetta che unisce tubo e canna davanti all'astina
        P["body"].box((0, yb1 - 0.045, (bz + pz) / 2), (0.020, 0.012, 0.040), metal, bevel=0.0022)
        P["slide"].box((0, py, pz - 0.008), (w * 0.94, pl, 0.042), s.get("pump_mat", body), bevel=0.007)
        for i in range(5):
            yy = py - pl * 0.36 + i * pl * 0.18
            P["slide"].box((0, yy, pz - 0.008), (w * 0.96, 0.006, 0.038), dark, bevel=0.0022)
        P["slide"].cyl((0, py, pz), 0.0165, pl * 0.94, s.get("pump_mat", body), seg=16)

    # ---------------------------------------------------------------- dispositivo di volata
    md = s.get("muzzle_device")
    if md == "brake":
        P["body"].cyl((0, yb1 - 0.024, bz), 0.017, 0.058, metal, seg=16)
        for i in range(3):
            P["body"].box((0, yb1 - 0.040 + i * 0.015, bz + 0.009), (0.030, 0.008, 0.016), dark)
        P["body"].box((0, yb1 - 0.010, bz - 0.012), (0.026, 0.024, 0.010), metal, bevel=0.0015)
    elif md == "flash":
        P["body"].cyl((0, yb1 - 0.016, bz), 0.014, 0.042, metal, seg=14)
        for i in range(4):
            a = i * math.pi / 2
            P["body"].box((math.cos(a) * 0.011, yb1 - 0.016, bz + math.sin(a) * 0.011),
                          (0.005, 0.034, 0.005), dark, rot=(0, -a, 0))
    elif md == "suppressor":
        sl = s.get("supp_len", 0.16)
        P["body"].cyl((0, yb1 + 0.006, bz), 0.020, 0.014, metal, seg=20)
        P["body"].tube((0, yb1 + 0.012 + sl / 2, bz), 0.0205, 0.0062, sl, metal, seg=22, mat_in="stipple")
        for i in range(3):
            P["body"].cyl((0, yb1 + 0.040 + i * 0.050, bz), 0.0212, 0.006, dark, seg=22)

    # ---------------------------------------------------------------- calcio
    st = s.get("stock", "fixed")
    sm = s.get("stock_mat", body)
    slen = s.get("stock_len", 0.26)
    base = rz0 + 0.004
    if st == "fixed":
        sy0 = ry0 - slen
        bt = s.get("butt_top", 0.060)
        bb = s.get("butt_bot", -0.032)
        prof = stock_profile(sy0, ry0 + 0.004, base, butt_top=bt, butt_bot=bb,
                             wrist_top=bt * 0.52, wrist_bot=-0.004)
        P["body"].prism(prof, w * 0.72, sm)
        # calciolo in gomma
        P["body"].box((0, sy0 + 0.006, base + (bt + bb) / 2), (w * 0.72, 0.014, bt - bb - 0.002),
                      dark, bevel=0.004)
        for i in range(3):
            P["body"].box((0, sy0 + 0.014, base + (bt + bb) / 2 + (i - 1) * (bt - bb) * 0.30),
                          (w * 0.48, 0.006, 0.004), dark)
        if s.get("cheek"):
            P["body"].box((0, sy0 + slen * 0.42, base + bt + 0.006), (w * 0.54, slen * 0.50, 0.018),
                          s.get("cheek_mat", sm), bevel=0.007)
        P["body"].cyl((hw * 0.78, sy0 + 0.055, base + 0.010), 0.0035, 0.005, metal, axis="X", seg=8)
        P["body"].box((0, sy0 + 0.006, base + bb - 0.004), (w * 0.56, 0.012, 0.008), metal, bevel=0.002)
        # anello di cinghia sul lato
        P["body"].arc((0, sy0 + 0.075, base + 0.002), 0.014, 0.0035, metal, 190, 350, steps=8,
                      plane="YZ", width=0.004)
    elif st == "folding":
        sy0 = ry0 - slen
        P["body"].box((0, ry0 - 0.006, rz0 + 0.026), (0.028, 0.026, 0.048), metal, bevel=0.002)
        for dz in (-0.011, 0.013):
            P["body"].box((0, (ry0 + sy0) / 2 - 0.008, rz0 + 0.026 + dz),
                          (0.010, ry0 - sy0 - 0.014, 0.010), metal, bevel=0.0012)
        P["body"].box((0, sy0 + 0.012, rz0 + 0.026), (0.022, 0.030, 0.072), dark, bevel=0.005)
        P["body"].box((0, sy0 + 0.004, rz0 + 0.026), (0.024, 0.008, 0.064), dark, bevel=0.003)
        P["body"].box((0, sy0 + 0.046, rz0 + 0.026), (0.012, 0.062, 0.008), metal, bevel=0.0012)
    elif st == "wire":
        sy0 = ry0 - slen
        for dz in (0.004, 0.046):
            P["body"].box((0, (ry0 + sy0) / 2, rz0 + dz), (0.007, ry0 - sy0, 0.007), metal)
        P["body"].box((0, sy0 + 0.012, rz0 + 0.025), (0.020, 0.032, 0.076), dark, bevel=0.004)
        for yy in (sy0 + 0.030, ry0 - 0.014):
            P["body"].sphere((0, yy, rz0 + 0.025), 0.009, metal, seg=8, rings=6, scale=(0.55, 1, 1))
        P["body"].box((0, sy0 + 0.046, rz0 + 0.025), (0.012, 0.070, 0.007), metal, bevel=0.0012)
        P["body"].box((0, (ry0 + sy0) / 2, rz0 + 0.025), (0.008, 0.020, 0.040), metal, bevel=0.0012)
    elif st == "skeleton":
        sy0 = ry0 - slen
        P["body"].box((0, (ry0 + sy0) / 2, rz0 + 0.048), (0.011, ry0 - sy0, 0.012), metal, bevel=0.0012)
        P["body"].box((0, (ry0 + sy0) / 2, rz0 - 0.004), (0.011, ry0 - sy0, 0.012), metal, bevel=0.0012)
        P["body"].box((0, ry0 - 0.020, rz0 + 0.022), (0.014, 0.020, 0.056), metal, bevel=0.0014)
        P["body"].box((0, sy0 + 0.014, rz0 + 0.023), (0.016, 0.030, 0.078), dark, bevel=0.004)
        P["body"].box((0, sy0 + 0.040, rz0 + 0.058), (0.014, 0.090, 0.010), sm, bevel=0.003)
        P["body"].box((0, sy0 + 0.006, rz0 - 0.012), (0.016, 0.020, 0.026), metal, bevel=0.002)
    elif st == "bullpup":
        P["body"].box((0, ry0 - 0.018, rz0 + 0.010), (w * 0.86, 0.060, 0.062), dark, bevel=0.006)
        P["body"].box((0, ry0 - 0.004, rz0 + 0.038), (w * 0.90, 0.030, 0.020), body, bevel=0.003)
        P["body"].box((0, ry0 - 0.030, rz0 - 0.004), (w * 0.70, 0.020, 0.010), dark, bevel=0.003)

    # ---------------------------------------------------------------- impugnatura
    gh = s.get("grip_h", 0.104)
    trig_y = s.get("trig_y", ry1 - 0.170)
    ang = s.get("grip_angle", 15)
    gy = s.get("grip_y", trig_y - 0.052)
    gtop = rz0 + 0.010
    ga, gs = math.cos(math.radians(ang)), math.sin(math.radians(ang))
    prof = strip_profile(gy, gtop, gh, 0.046, ang - 2, ang + 5, steps=5, taper=0.94, flat_top=True)
    P["body"].prism(prof, 0.031, dark)
    # raccordo col castello + beavertail
    P["body"].box((0, gy - 0.004, rz0 + 0.004), (0.035, 0.056, 0.022), body, bevel=0.0024)
    P["body"].box((0, gy - 0.026, rz0 + 0.002), (0.030, 0.030, 0.016), body,
                  rot=(math.radians(-12), 0, 0), bevel=0.004)
    # guancette laterali
    for sx in (-1, 1):
        P["body"].box((sx * 0.0162, gy - gs * 0.040, gtop - ga * 0.040),
                      (0.0030, 0.040, 0.052), dark, rot=(math.radians(-ang), 0, 0), bevel=0.0045)
    # gole per le dita
    for i in range(3):
        t = 0.022 + i * 0.016
        P["body"].box((0, gy + 0.025 * ga - gs * t, gtop - ga * t + 0.025 * gs),
                      (0.031, 0.005, 0.005), body, rot=(math.radians(-ang), 0, 0), bevel=0.0016)
    # tappo di base
    P["body"].box((0, gy - gs * (gh - 0.004), gtop - ga * (gh - 0.004)),
                  (0.034, 0.052, 0.008), dark, rot=(math.radians(-ang), 0, 0), bevel=0.0022)
    if s.get("grip_tape"):
        P["body"].box((0, gy - 0.002, gtop - 0.044), (0.034, 0.052, 0.032), "cloth",
                      rot=(math.radians(-ang), 0, 0), bevel=0.002)

    # ---------------------------------------------------------------- grilletto e guardia
    P["body"].prism(guard_profile(trig_y - 0.026, trig_y + 0.030, rz0 + 0.010, 0.028, 0.0055),
                    0.013, metal)
    P["body"].box((0, trig_y + 0.034, rz0 + 0.002), (0.011, 0.018, 0.018), metal, bevel=0.0022)
    P["body"].box((0, trig_y - 0.024, rz0 + 0.002), (0.011, 0.022, 0.016), metal, bevel=0.0022)
    P["trigger"].pivot = (0.0, trig_y, rz0 + 0.002)
    P["trigger"].box((0, trig_y, rz0 - 0.013), (0.0060, 0.009, 0.024), metal,
                     rot=(math.radians(-10), 0, 0), bevel=0.0016)
    P["trigger"].box((0, trig_y - 0.003, rz0 - 0.001), (0.0055, 0.011, 0.005), metal,
                     rot=(math.radians(-10), 0, 0), bevel=0.0010)
    if s.get("hammer_gun"):
        P["hammer"].pivot = (0.0, ry0 + 0.012, rz1 - 0.010)
        P["hammer"].box((0, ry0 + 0.014, rz1 + 0.002), (0.0065, 0.018, 0.014), metal,
                        rot=(math.radians(-24), 0, 0), bevel=0.0012)

    # ---------------------------------------------------------------- caricatore
    mt = s.get("mag_type", "box")
    mag_y = s.get("mag_y", trig_y + 0.062)
    depth = s.get("mag_depth", 0.058)
    if mt in ("box", "banana"):
        ml = s.get("mag_len", 0.19) * 0.88
        a0 = s.get("mag_angle", 4)
        a1 = a0 + s.get("mag_curve", 0 if mt == "box" else 18)
        prof = strip_profile(mag_y, rz0 + 0.014, ml, depth, a0, a1,
                             steps=4 if mt == "box" else 6, taper=s.get("mag_taper", 0.96),
                             flat_top=True)
        P["mag"].prism(prof, 0.028, metal)
        # piastrina: un tappo ALLINEATO al corpo del caricatore, spesso 13 mm.
        # Prima era una aletta messa di traverso, a 5 cm dal caricatore: il "pezzo
        # senza senso" che si vedeva sotto l'arma.
        ca, sa = math.cos(math.radians(a1)), math.sin(math.radians(a1))
        ty = mag_y - sa * (ml - 0.006)
        tz = rz0 + 0.014 - ca * (ml - 0.006)
        cap = strip_profile(ty + sa * 0.007, tz + ca * 0.007, 0.014, depth * 1.10,
                            a1, a1, steps=2)
        P["mag"].prism(cap, 0.032, dark)
        # nervature: sottili, lungo il corpo
        if mt == "box":
            for sx in (-1, 1):
                P["mag"].box((sx * 0.0146, mag_y, rz0 + 0.014 - ml * 0.45),
                             (0.0020, depth * 0.55, ml * 0.62), metal, bevel=0.0010)
                P["mag"].box((sx * 0.0152, mag_y, rz0 + 0.014 - ml * 0.45),
                             (0.0012, 0.006, ml * 0.58), dark)
        # bocchettone svasato: si allarga verso il basso, il caricatore ci entra dentro
        wprof = [(mag_y - depth * 0.46, rz0 + 0.020), (mag_y + depth * 0.46, rz0 + 0.020),
                 (mag_y + depth * 0.60, rz0 - 0.006), (mag_y - depth * 0.60, rz0 - 0.006)]
        P["body"].prism(wprof, 0.036, rv)
    elif mt == "drum":
        dr = s.get("drum_r", 0.066)
        dy = s.get("mag_y", trig_y + 0.078)
        cz = rz0 - dr * 0.86
        P["mag"].cyl((0, dy, cz), dr, 0.076, metal, axis="X", seg=24)
        P["mag"].cyl((0, dy, cz), dr * 0.99, 0.080, dark, axis="X", seg=24)
        P["mag"].cyl((0, dy, cz), 0.020, 0.078, dark, axis="X", seg=16)
        P["mag"].cyl((0, dy, cz), 0.010, 0.086, metal, axis="X", seg=12)
        for i in range(7):
            a = i * math.pi * 1.7 / 7
            rr = dr * (0.30 + 0.62 * i / 7)
            P["mag"].box((0.040, dy + math.cos(a) * rr, cz + math.sin(a) * rr),
                         (0.004, 0.014, 0.014), "void", rot=(0, -a, 0))
        P["mag"].box((0, dy, cz + dr * 0.70), (0.040, 0.052, 0.060), metal, bevel=0.003)
        for sx in (-1, 1):
            P["mag"].cyl((sx * 0.040, dy, cz), dr * 0.70, 0.008, dark, axis="X", seg=20)
        for i in range(10):
            a = i * math.pi / 5
            P["mag"].box((0, dy + math.cos(a) * dr * 0.84, cz + math.sin(a) * dr * 0.84),
                         (0.078, 0.008, 0.008), dark, rot=(0, -a, 0))
        P["body"].box((0, dy, rz0 - 0.004), (0.036, 0.072, 0.020), body, bevel=0.002)
    elif mt == "horizontal":
        # appoggiato al castello, non sopra la linea di mira (la copriva)
        P["mag"].box((0, ry0 + 0.080, rz1 + 0.009), (0.030, 0.180, 0.026), metal, bevel=0.004)
        P["mag"].cyl((0, ry0 + 0.170, rz1 + 0.009), 0.013, 0.026, metal, axis="X", seg=14)
        for i in range(5):
            for sx in (-1, 1):
                P["mag"].box((sx * 0.0152, ry0 + 0.020 + i * 0.032, rz1 + 0.009),
                             (0.0012, 0.010, 0.019), "void")
        P["mag"].box((0, ry0 + 0.010, rz1 + 0.009), (0.032, 0.016, 0.028), dark, bevel=0.002)

    # ---------------------------------------------------------------- ottica e mire
    op = s.get("optic", "irons")
    aim_z = rz1 + 0.020
    if op == "scope":
        sc_len = s.get("scope_len", 0.26)
        cy_ = s.get("scope_y", (ry0 + ry1) / 2)
        zz = rz1 + s.get("scope_rise", 0.040)
        tube_r = s.get("scope_r", 0.0165)
        P["body"].tube((0, cy_, zz), tube_r, tube_r * 0.74, sc_len, dark, seg=22, mat_in="stipple")
        P["body"].cyl((0, cy_ + sc_len / 2 - 0.014, zz), tube_r * 1.55, 0.030, dark, seg=22)
        P["body"].cyl((0, cy_ - sc_len / 2 + 0.014, zz), tube_r * 1.38, 0.028, dark, seg=22)
        P["body"].cyl((0, cy_ + sc_len / 2 + 0.001, zz), tube_r * 1.50, 0.004, "glass", seg=22)
        P["body"].cyl((0, cy_ - sc_len / 2 - 0.012, zz), tube_r * 1.30, 0.006, "glass", seg=22)
        P["body"].cyl((0, cy_ + 0.020, zz + tube_r * 1.30), 0.014, 0.020, dark, axis="Z", seg=14)
        P["body"].cyl((0, cy_ + 0.020, zz + tube_r * 1.30 + 0.011), 0.0145, 0.004, metal, axis="Z", seg=14)
        P["body"].cyl((0, cy_ + 0.020, zz - tube_r * 1.30), 0.014, 0.018, dark, axis="Z", seg=14)
        P["body"].cyl((tube_r * 1.30, cy_ + 0.020, zz), 0.013, 0.018, dark, axis="X", seg=14)
        P["body"].cyl((0, cy_ - 0.008, zz + tube_r * 1.30), 0.017, 0.008, metal, axis="Z", seg=14)
        for dy in (-sc_len * 0.28, sc_len * 0.28):
            P["body"].box((0, cy_ + dy, rz1 + 0.020), (0.026, 0.018, 0.040), metal, bevel=0.0022)
            P["body"].cyl((0, cy_ + dy, zz), tube_r * 1.16, 0.014, metal, seg=16)
        aim_z = zz
    elif op == "dot":
        # Punto rosso APERTO e sottile: due spalle da 3 mm e un ponte basso, vetro
        # da 19 mm trasparente. Il telaio della prima versione era spesso 6,2 mm con
        # un ponte da 8: in mira copriva mezzo schermo e il vetro era opaco.
        dy = s.get("dot_y", ry1 - 0.150)
        zb = rz1 + 0.004 + s.get("dot_rise", 0.0)
        zh = zb + 0.034
        zc = zh - 0.011
        P["body"].box((0, dy, zb + 0.004), (0.026, 0.044, 0.008), metal, bevel=0.0018)
        for sx in (-1, 1):
            P["body"].box((sx * 0.0124, dy, (zb + zh) / 2), (0.0028, 0.040, zh - zb), dark, bevel=0.0010)
        P["body"].box((0, dy, zh), (0.0276, 0.040, 0.0042), dark, bevel=0.0012)
        # vetro: 19 mm, dentro l'apertura (22 mm), leggermente incassato
        P["body"].cyl((0, dy + 0.0015, zc), 0.0096, 0.0020, "glass", axis="Y", seg=22)
        # torretta della batteria e dado di sinistra: bassi, non sulla linea
        P["body"].cyl((0.0145, dy, zc), 0.0052, 0.009, metal, axis="X", seg=10)
        P["body"].cyl((-0.0145, dy, zc), 0.0052, 0.009, metal, axis="X", seg=10)
        P["body"].box((0, dy - 0.026, zb + 0.010), (0.014, 0.010, 0.010), metal, bevel=0.0018)
        aim_z = zc
    else:
        # mire: quota unica H. Ci arrivano il filo delle orecchie della tacca e la punta
        # del mirino, quindi la linea di mira è una retta vera e non "a occhio".
        H = rz1 + 0.022
        sy = ry0 + 0.030
        P["body"].box((0, sy, H - 0.011), (0.021, 0.010, 0.012), metal, bevel=0.0014)
        for sx in (-1, 1):
            P["body"].box((sx * 0.0064, sy, H - 0.0055), (0.0062, 0.009, 0.011), metal, bevel=0.0012)
        P["body"].box((0, sy, H - 0.0075), (0.0030, 0.008, 0.004), "void")

        # mirino: la base sta sulla canna, l'asta arriva fino a H
        fbase = bz + rb + 0.008
        fh = max(0.008, H - fbase)
        P["body"].box((0, yb1 - 0.034, bz + rb + 0.002), (0.016, 0.020, 0.012), metal, bevel=0.0018)
        P["body"].box((0, yb1 - 0.034, fbase + fh / 2), (0.0032, 0.010, fh), metal, bevel=0.0010)
        for sx in (-1, 1):
            P["body"].box((sx * 0.0075, yb1 - 0.034, fbase + (fh + 0.003) / 2),
                          (0.0030, 0.012, fh + 0.003), metal, rot=(0, math.radians(sx * 9), 0), bevel=0.0010)
        P["body"].box((0, yb1 - 0.038, H - 0.0022), (0.0026, 0.004, 0.0042), accent)
        aim_z = H

    # ---------------------------------------------------------------- extra
    if s.get("carry_handle"):
        # maniglione di trasporto SULLA CANNA, basso e avanti. Sopra il castello copriva
        # la linea di mira e in puntamento si vedeva solo il maniglione.
        hy = yb0 + 0.110
        hz = bz + rb + 0.006
        P["body"].box((0, hy, hz), (0.026, 0.096, 0.010), body, bevel=0.0030)
        for dy in (-0.036, 0.036):
            P["body"].box((0, hy + dy, hz - 0.016), (0.024, 0.014, 0.030), body, bevel=0.0022)
        for i in range(3):
            P["body"].box((0, hy - 0.030 + i * 0.030, hz + 0.004), (0.020, 0.014, 0.003), "void")
    if s.get("bipod"):
        by = yb1 - 0.230
        P["body"].box((0, by, bz - 0.030), (0.026, 0.036, 0.024), metal, bevel=0.0022)
        P["body"].cyl((0, by, bz - 0.030), 0.008, 0.030, dark, axis="X", seg=12)
        for dx in (-1, 1):
            P["body"].box((dx * 0.014, by + 0.040, bz - 0.082), (0.012, 0.012, 0.100), metal,
                          rot=(math.radians(24), 0, math.radians(dx * 7)), bevel=0.0012)
            P["body"].box((dx * 0.017, by + 0.082, bz - 0.146), (0.010, 0.010, 0.070), metal,
                          rot=(math.radians(24), 0, math.radians(dx * 7)), bevel=0.0012)
            P["body"].box((dx * 0.019, by + 0.096, bz - 0.180), (0.014, 0.030, 0.010), dark,
                          rot=(math.radians(24), 0, 0), bevel=0.0016)
    if s.get("belt"):
        by = s.get("mag_y", trig_y + 0.078)
        for i in range(8):
            P["mag"].box((-0.014, by - 0.050 + i * 0.012, rz0 - 0.126 - i * 0.016),
                         (0.032, 0.014, 0.016), "brass",
                         rot=(math.radians(28 + i * 2), 0, math.radians(8)), bevel=0.0015)
        P["body"].box((0, by - 0.062, rz0 - 0.030), (0.036, 0.030, 0.024), metal, bevel=0.002)
    if s.get("sheet_fold"):
        P["body"].box((0, (ry0 + ry1) / 2, rz1 + 0.0035), (w * 1.02, ry1 - ry0 - 0.018, 0.005), body)
        for dy in (0.040, 0.150, 0.225):
            for dx in (-1, 1):
                P["body"].cyl((dx * hw * 1.01, ry0 + dy, rz0 + 0.030), 0.0042, 0.006, metal,
                              axis="X", seg=6)
        # nervature stampate sul fianco
        for dy in (0.055, 0.175):
            for dx in (-1, 1):
                P["body"].box((dx * hw * 0.995, ry0 + dy, rz0 + 0.030), (0.0022, 0.085, 0.005), dark)
    if s.get("sling"):
        for yy, zz2, r in ((ry0 - 0.030, rz0 + 0.002, 0.016), (yb1 - 0.120, bz - 0.036, 0.014)):
            P["body"].arc((0, yy, zz2), r, 0.0035, metal, 190, 350, steps=8, plane="YZ", width=0.004)
    if s.get("rail_side"):
        P["body"].rail(ry0 + 0.030, ry0 + 0.140, rz0 + 0.012, 0.018, metal, teeth=5)

    return dict(
        muzzle=(0.0, yb1 + 0.004, bz),
        eject=(hw + 0.004, ry1 - 0.052, rz1 - 0.014),
        aim=(0.0, s.get("scope_y", (ry0 + ry1) / 2) if op == "scope" else yb1 - 0.030, aim_z),
        # l'occhio: dietro l'oculare per il cannocchiale (7,5 cm di rilievo oculare),
        # dietro il castello per dot e mire.
        eye=(0.0,
             (s.get("scope_y", (ry0 + ry1) / 2) - s.get("scope_len", 0.26) / 2 - 0.075) if op == "scope"
             else ry0 - 0.155,
             aim_z),
        hand_r=(0.0, gy, gtop - 0.030),
        hand_l=(0.0, (hg0 + hg1) / 2 if s.get("handguard", True) else yb0 + 0.10, bz - 0.020),
    )


# ===================================================================== COLTELLO

def knife(P, s):
    """Coltello da combattimento: lama a goccia con filo assottigliato, manico pieno."""
    pal = palette(s.get("look", "wood"), s.get("mats"))
    body, metal, dark, accent = pal["body"], pal["metal"], pal["dark"], pal["accent"]
    blade = s.get("blade", 0.170)
    zc = 0.030
    hw = s.get("width", 0.0034)
    z_edge, z_spine = zc - 0.017, zc + 0.015

    prof = [
        (0.016, zc + 0.015),
        (blade * 0.60, zc + 0.015),
        (blade * 0.84, zc + 0.012),
        (blade * 0.95, zc + 0.004),
        (blade, zc - 0.005),
        (blade * 0.90, zc - 0.012),
        (blade * 0.56, zc - 0.017),
        (0.018, zc - 0.017),
    ]

    def thick(y, z):
        t = max(0.0, min(1.0, (z - z_edge) / (z_spine - z_edge)))
        return hw * (0.06 + 0.94 * (t ** 0.75))

    P["body"].prism_var(prof, thick, "steel")
    # scanalatura della lama
    P["body"].box((0, blade * 0.40, zc + 0.008), (hw * 0.22, blade * 0.26, 0.0016), "void")
    # guardia e codolo (un blocco solo, con la base del manico)
    P["body"].prism([(0.020, zc + 0.017), (0.020, zc - 0.019), (-0.006, zc - 0.021),
                     (-0.006, zc + 0.020)], 0.018, metal)
    P["body"].box((0, -0.002, zc + 0.002), (0.020, 0.012, 0.038), metal, bevel=0.0035)
    # manico: un pezzo pieno, con la pancia per la mano
    hp = strip_profile(-0.012, zc + 0.010, 0.108, 0.025, 90, 94, steps=4, taper=0.93, flat_top=True)
    P["body"].prism(hp, 0.020, dark)
    # ricasso: lo scalino fra lama e guardia, e la gola per l'indice appena accennata
    P["body"].box((0, 0.012, zc), (0.0175, 0.008, 0.030), metal, bevel=0.002)
    P["body"].box((0, -0.032, zc + 0.002), (0.0202, 0.013, 0.024), dark, bevel=0.0035)
    # pomello con il codolo a vista
    P["body"].box((0, -0.122, zc + 0.003), (0.021, 0.014, 0.030), metal, bevel=0.0035)
    P["body"].box((0, -0.130, zc + 0.003), (0.012, 0.008, 0.012), metal, bevel=0.0018)

    return dict(
        muzzle=(0.0, blade * 0.96, zc - 0.004),
        eject=(0.0, 0.0, 0.0),
        aim=(0.0, blade * 0.55, zc + 0.018),
        eye=(0.0, -0.160, zc + 0.018),
        hand_r=(0.0, -0.068, zc + 0.004),
        hand_l=(0.0, 0.030, zc + 0.004),
    )
