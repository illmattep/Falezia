"""I DUE SPAWN, VERSIONE ARTIGIANALE — stile Valorant, non blockout.

Geometria: scatole smussate (bevel), modanature, lamiere grecate, conci d'arco, bugne,
ferramenta. UV proiettate dal mondo (2,5 m per ripetizione) così la densità di texel è
costante su tutto e le texture non si stirano.

Materiali: PBR con albedo, normal e roughness generati da make_textures.py.
Palette: Porto = acciaio/blu/ruggine freddo · Forte = pietra calda/ocra/rosso.

  blender -b -noaudio --factory-startup --python build_spawns_v2.py
"""
import bpy, bmesh, math, os
from mathutils import Matrix, Vector

ROOT = "/home/matteo/Progetti/Falezia"
TEX = f"{ROOT}/art/map-assets/tex"
OUT = f"{ROOT}/Assets/StreamingAssets/map/spawn"
os.makedirs(OUT, exist_ok=True)

# ---------------------------------------------------------------- materiali
MATS = {
    "stucco":  ("stucco_warm", 0.90, 0.0),
    "stone":   ("stone_blocks", 0.82, 0.0),
    "ribbed":  ("metal_ribbed", 0.45, 0.65),
    "painted": ("metal_painted", 0.50, 0.55),
    "wood":    ("wood_planks", 0.75, 0.0),
    "canvas":  ("canvas_stripes", 0.90, 0.0),
    "tiles":   ("roof_tiles", 0.80, 0.0),
}

def make_material(key):
    name, rough, metal = MATS[key]
    m = bpy.data.materials.get(name)
    if m: return m
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    nt = m.node_tree
    bsdf = nt.nodes["Principled BSDF"]
    bsdf.inputs["Roughness"].default_value = rough
    bsdf.inputs["Metallic"].default_value = metal

    alb = nt.nodes.new("ShaderNodeTexImage")
    alb.image = bpy.data.images.load(f"{TEX}/{name}.png")
    alb.interpolation = 'Smart'
    nt.links.new(alb.outputs["Color"], bsdf.inputs["Base Color"])

    nrm_img = nt.nodes.new("ShaderNodeTexImage")
    nrm_img.image = bpy.data.images.load(f"{TEX}/{name}_n.png")
    nrm_img.image.colorspace_settings.name = 'Non-Color'
    nrm = nt.nodes.new("ShaderNodeNormalMap")
    nrm.inputs["Strength"].default_value = 1.0
    nt.links.new(nrm_img.outputs["Color"], nrm.inputs["Color"])
    nt.links.new(nrm.outputs["Normal"], bsdf.inputs["Normal"])

    rough_img = nt.nodes.new("ShaderNodeTexImage")
    rough_img.image = bpy.data.images.load(f"{TEX}/{name}_r.png")
    rough_img.image.colorspace_settings.name = 'Non-Color'
    nt.links.new(rough_img.outputs["Color"], bsdf.inputs["Roughness"])
    return m

# ---------------------------------------------------------------- costruttore
class Kit:
    def __init__(self):
        self.bm = {}

    def _bm(self, mat):
        if mat not in self.bm:
            self.bm[mat] = bmesh.new()
        return self.bm[mat]

    def box(self, mat, x0, y0, z0, sx, sy, sz, bevel=0.035, seg=2):
        """Scatola smussata: lo spigolo arrotondato è ciò che fa leggere la forma in luce radente."""
        if sx <= 1e-4 or sy <= 1e-4 or sz <= 1e-4: return
        bm = self._bm(mat)
        bmesh.ops.create_cube(bm, size=1.0)
        new = [e for e in bm.edges if e.verts[0].co.length > 0][-12:] if False else None
        # prendo le facce/vertici appena creati: sono gli ultimi 8 vertici
        verts = sorted(bm.verts, key=lambda v: v.index)[-8:]
        if bevel > 0:
            edges = set()
            for v in verts:
                for e in v.link_edges: edges.add(e)
            bmesh.ops.bevel(bm, geom=list(edges) + verts, offset=bevel, segments=seg,
                            affect='EDGES', clamp_overlap=True)
        verts = sorted(bm.verts, key=lambda v: v.index)[-len(bm.verts) + 0:] if False else None
        # trasforma solo la parte nuova: uso un oggetto temporaneo
        tmp = bmesh.new()
        bmesh.ops.create_cube(tmp, size=1.0)
        bmesh.ops.delete(tmp, geom=tmp.faces[:], context='FACES')
        tmp.free()
        # più semplice: scalo/traduco tutti i vertici creati in questo giro
        # (li ritrovo confrontando il bounding box locale)
        return

    def add_box(self, mat, x0, y0, z0, sx, sy, sz, bevel=0.04, seg=2):
        bm = self._bm(mat)
        me = bmesh.new()
        bmesh.ops.create_cube(me, size=1.0)
        if bevel > 0:
            bmesh.ops.bevel(me, geom=list(me.edges) + list(me.verts), offset=bevel,
                            segments=seg, affect='EDGES', clamp_overlap=True)
        # mondo: x, -z, y
        cx, cy, cz = x0 + sx / 2, -(z0 + sz / 2), y0 + sy / 2
        bmesh.ops.scale(me, vec=(sx, sz, sy), verts=me.verts)
        bmesh.ops.translate(me, vec=(cx, cy, cz), verts=me.verts)
        me.to_mesh(bpy.data.meshes.new("tmp"))
        tmp_me = bpy.data.meshes.new("tmp")
        me.to_mesh(tmp_me)
        bm.from_mesh(tmp_me)
        bpy.data.meshes.remove(tmp_me)
        me.free()

    def cyl(self, mat, cx, y0, cz, r, h, seg=16):
        bm = self._bm(mat)
        bmesh.ops.create_cone(bm, cap_ends=True, segments=seg, radius1=r, radius2=r, depth=h,
                              matrix=Matrix.Translation((cx, -cz, y0 + h / 2)))

    def export(self, path, name):
        objs = []
        for key, bm in self.bm.items():
            me = bpy.data.meshes.new(f"{name}_{key}")
            bm.to_mesh(me); bm.free()
            # UV dal mondo: proiezione sull'asse dominante della faccia
            uv = me.uv_layers.new(name="UVMap")
            for poly in me.polygons:
                n = poly.normal
                ax = max(range(3), key=lambda i: abs(n[i]))
                for li in poly.loop_indices:
                    co = me.vertices[me.loops[li].vertex_index].co
                    if ax == 0: u, v = co.y, co.z
                    elif ax == 1: u, v = co.x, co.z
                    else: u, v = co.x, co.y
                    uv.data[li].uv = (u / 2.5, v / 2.5)
            ob = bpy.data.objects.new(f"{name}_{key}", me)
            bpy.context.collection.objects.link(ob)
            ob.data.materials.append(make_material(key))
            objs.append(ob)

        bpy.ops.object.select_all(action='DESELECT')
        for o in objs: o.select_set(True)
        bpy.context.view_layer.objects.active = objs[0]
        bpy.ops.export_scene.gltf(filepath=path, export_format='GLB', use_selection=True,
                                  export_apply=True, export_yup=True, export_image_format='AUTO')
        print(f"RESULT: {path}", flush=True)

# ---------------------------------------------------------------- PORTO
def porto():
    k = Kit()
    # ----- capannone: basamento, lamiera grecata, copertura con sporto
    k.add_box("stucco", -20, 0, -97, 14, 0.7, 7)                       # basamento in cemento
    k.add_box("ribbed", -20, 0.7, -97, 14, 4.0, 7)                     # corpo in lamiera
    k.add_box("painted", -20.4, 4.7, -97.4, 14.8, 0.35, 7.8)           # fascia di gronda
    k.add_box("painted", -20.2, 5.0, -97.2, 14.4, 0.18, 7.4, 0.02)     # copertura
    # portone a saracinesca con doghe
    k.add_box("painted", -15.6, 0.7, -97.05, 4.2, 3.4, 0.18)
    for i in range(9):
        k.add_box("painted", -15.5, 0.85 + i * 0.36, -97.02, 4.0, 0.16, 0.1, 0.015, 1)
    # finestrino di banda + lampada
    k.add_box("painted", -8.4, 3.2, -97.05, 3.0, 0.55, 0.12)
    k.add_box("painted", -11.9, 3.4, -96.9, 0.35, 0.35, 0.35)
    # ----- container: corpo grecato + angolari + barre delle porte
    def container(x, y, z, m):
        k.add_box(m, x, y, z, 6.0, 2.6, 2.44)
        for dx in (0, 5.82):
            for dz in (0, 2.26):
                k.add_box("painted", x + dx, y + 0.02, z + dz, 0.18, 2.56, 0.18, 0.02, 1)
        for i in range(4):
            k.add_box("painted", x + 0.35 + i * 1.35, y + 0.05, z + 2.3, 0.12, 2.3, 0.14, 0.015, 1)
    container(6.5, 0, -96.5, "painted")
    container(6.5, 2.6, -96.5, "painted")
    container(12.7, 0, -96.4, "ribbed")
    container(12.7, 2.6, -96.4, "painted")
    container(6.5, 0, -89.0, "ribbed")
    # ----- gru a traliccio
    for x in (17.2, 19.0):
        for z in (-94.2, -92.4):
            k.add_box("painted", x, 0, z, 0.22, 12.0, 0.22, 0.02, 1)
    for y in (2.0, 5.0, 8.0, 11.0):
        k.add_box("painted", 17.1, y, -94.3, 2.1, 0.16, 0.16, 0.01, 1)
        k.add_box("painted", 17.1, y, -92.3, 2.1, 0.16, 0.16, 0.01, 1)
        k.add_box("painted", 17.1, y, -94.3, 0.16, 0.16, 2.1, 0.01, 1)
        k.add_box("painted", 19.0, y, -94.3, 0.16, 0.16, 2.1, 0.01, 1)
    k.add_box("painted", 17.6, 12.0, -93.3, 0.5, 0.5, 9.0)              # braccio
    k.add_box("painted", 17.5, 11.6, -88.6, 0.7, 0.7, 0.9)              # carrello
    k.add_box("stucco", 16.8, 0, -94.2, 2.6, 0.9, 3.4)                  # basamento
    # ----- bitte: piastra + fusto + cappello
    for x in range(-18, 22, 4):
        k.add_box("painted", x - 0.34, 0, -99.5, 0.68, 0.09, 0.5)
        k.cyl("painted", x, 0.09, -99.3, 0.17, 0.62, 12)
        k.cyl("painted", x, 0.7, -99.3, 0.23, 0.1, 12)
    # ----- barca in secca: scafo a doghe + falchetta + banchi
    for i in range(5):
        k.add_box("wood", 1.2 + i * 0.15, 0.0 + i * 0.12, -95.2 + i * 0.28, 4.6 - i * 0.5, 0.42, 1.9 - i * 0.3,
                  0.03, 1)
    k.add_box("painted", 1.0, 0.72, -95.4, 5.0, 0.12, 2.2, 0.02, 1)
    for zz in (-95.1, -94.2):
        k.add_box("wood", 1.6, 0.5, zz, 0.3, 0.12, 1.5, 0.02, 1)
    k.add_box("wood", 4.6, 0.5, -95.1, 0.16, 3.0, 0.16, 0.02, 1)        # remo
    # ----- casse, reti, boe
    for i in range(4):
        k.add_box("wood", -8 + i * 0.95, 0, -86.4, 0.85, 0.75, 0.85, 0.03, 1)
        k.add_box("wood", -7.9 + i * 0.95, 0.75, -86.3, 0.75, 0.06, 0.75, 0.01, 1)
    for i in range(3):
        k.cyl("canvas", -12 + i * 0.8, 0, -85.2, 0.35, 0.25, 10)
    k.cyl("painted", 2.0, 0, -87.5, 0.28, 0.55, 12)                      # boa
    k.export(f"{OUT}/spawn_porto.glb", "PortoVecchio")

# ---------------------------------------------------------------- FORTE
def forte():
    k = Kit()
    # ----- muraglia: basamento a scarpa, corpo a conci, cammino di ronda, merli smussati
    k.add_box("stone", -22, 0, 94.6, 44, 1.2, 1.9, 0.05, 2)
    k.add_box("stone", -22, 1.2, 95.0, 44, 3.0, 1.2, 0.05, 2)
    k.add_box("stone", -22.2, 4.2, 94.8, 44.4, 0.35, 1.6, 0.04, 2)       # cornice del cammino
    for i in range(38):                                                   # merli
        x = -21.0 + i * 1.15
        k.add_box("stone", x, 4.55, 95.0, 0.62, 0.75, 1.2, 0.06, 2)
        k.add_box("stone", x - 0.04, 5.3, 94.96, 0.7, 0.12, 1.28, 0.04, 2)
    # feritoie
    for x in (-14, -6, 6, 14):
        k.add_box("wood", x - 0.18, 2.2, 94.9, 0.36, 1.1, 0.5, 0.02, 1)
    # ----- porta: stipiti, arco a conci, battenti in legno con bandelle
    k.add_box("stone", -2.6, 0, 94.6, 1.1, 3.6, 1.9, 0.05, 2)
    k.add_box("stone", 1.5, 0, 94.6, 1.1, 3.6, 1.9, 0.05, 2)
    for i in range(7):
        a = math.pi * i / 6
        bx = -1.5 + 3.0 * i / 6
        k.add_box("stone", bx, 3.6 + math.sin(a) * 0.55, 94.6, 0.55, 0.45, 1.9, 0.04, 2)
    k.add_box("wood", -1.5, 0, 94.8, 1.45, 3.2, 0.14, 0.02, 1)
    k.add_box("wood", 0.05, 0, 94.8, 1.45, 3.2, 0.14, 0.02, 1)
    for y in (0.6, 1.6, 2.6):
        k.add_box("painted", -1.5, y, 94.74, 3.0, 0.12, 0.06, 0.01, 1)
    for y in (0.7, 1.7, 2.7):
        for x in (-1.2, -0.5, 0.3, 1.0):
            k.add_box("painted", x, y, 94.72, 0.09, 0.09, 0.05, 0.008, 1)
    # ----- torri: corpo, cornice a mensole, merli, feritoie, stendardo
    for tx in (-17.0, 14.0):
        k.add_box("stone", tx, 0, 92.6, 5.4, 9.6, 5.4, 0.06, 2)
        k.add_box("stone", tx - 0.25, 9.6, 92.35, 5.9, 0.42, 5.9, 0.05, 2)
        for x in range(0, 5):
            k.add_box("stone", tx + x * 1.08, 10.0, 92.6, 0.55, 0.8, 0.55, 0.05, 2)
            k.add_box("stone", tx + x * 1.08, 10.0, 97.45, 0.55, 0.8, 0.55, 0.05, 2)
        for y in (3.0, 5.5, 7.5):
            k.add_box("wood", tx + 2.55, y, 94.9, 0.3, 1.0, 0.6, 0.02, 1)
        k.add_box("painted", tx + 2.6, 10.4, 93.4, 0.12, 3.4, 0.12, 0.01, 1)
        # stendardo con pieghe
        for i in range(6):
            k.add_box("canvas", tx + 2.66, 11.9 - i * 0.06, 93.4 + i * 0.02, 0.05, 1.9 - i * 0.12, 1.05, 0.01, 1)
    # ----- scalinata d'accesso (gradini con naso)
    for i in range(5):
        k.add_box("stone", -3.6, 0, 93.0 + i * 0.42, 7.2, 0.2 * (5 - i), 0.42, 0.03, 2)
        k.add_box("stone", -3.65, 0.2 * (5 - i), 93.0 + i * 0.42, 7.3, 0.06, 0.46, 0.02, 1)
    # ----- bracieri: vasca, tre piedi, bordo
    for bx in (-5.4, 5.4):
        k.cyl("painted", bx, 0.0, 91.8, 0.42, 0.12, 12)
        for a in range(3):
            ang = a * 2.09
            k.add_box("painted", bx + math.cos(ang) * 0.3, 0.0, 91.8 + math.sin(ang) * 0.3, 0.1, 0.55, 0.1, 0.01, 1)
        k.cyl("painted", bx, 0.55, 91.8, 0.55, 0.35, 14)
        k.cyl("canvas", bx, 0.9, 91.8, 0.42, 0.18, 12)
    # ----- botti e ceppi lungo il muro
    for x in (-11, -7.5, 8, 11.5):
        k.cyl("wood", x, 0, 93.6, 0.42, 0.95, 12)
        k.cyl("painted", x, 0.15, 93.6, 0.44, 0.08, 12)
        k.cyl("painted", x, 0.72, 93.6, 0.44, 0.08, 12)
    k.export(f"{OUT}/spawn_forte.glb", "ForteSanVerano")


bpy.ops.wm.read_factory_settings(use_empty=True)
porto()
bpy.ops.wm.read_factory_settings(use_empty=True)
forte()
