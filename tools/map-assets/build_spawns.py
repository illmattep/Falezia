"""I DUE SPAWN, DIFFERENZIATI. Genera due GLB in coordinate mondo.

  Nord · PORTO VECCHIO   — porto industriale: capannone, container impilati, gru a traliccio,
                           bitte, barca in secca, reti. Palette fredda: acciaio, blu, ruggine.
  Sud  · FORTE SAN VERANO— fortezza: muraglia merlata, porta ad arco, due torri, stendardi,
                           bracieri. Palette calda: pietra, ocra, rosso.

Sono decorazione: la collisione resta quella del JSON. Vanno tenuti ai bordi della zona di
spawn, dove la mappa è già chiusa, per non creare volumi attraversabili in mezzo al gioco.

  blender -b -noaudio --factory-startup --python build_spawns.py
"""
import bpy, bmesh, math, os

OUT = "/home/matteo/Progetti/Falezia/Assets/StreamingAssets/map/spawn"
os.makedirs(OUT, exist_ok=True)

FACES = [(0,1,3,2),(4,6,7,5),(0,4,5,1),(2,3,7,6),(0,2,6,4),(1,5,7,3)]

PAL = {
    # porto
    "acc_blue":   (0.16, 0.28, 0.38, 1), "acc_rust": (0.45, 0.24, 0.13, 1),
    "cont_red":   (0.48, 0.16, 0.12, 1), "cont_green": (0.20, 0.32, 0.24, 1),
    "conc_grey":  (0.46, 0.46, 0.44, 1), "rope": (0.55, 0.47, 0.33, 1),
    # forte
    "stone_warm": (0.68, 0.58, 0.42, 1), "stone_dark": (0.42, 0.36, 0.28, 1),
    "banner":     (0.55, 0.10, 0.09, 1), "wood_dark": (0.25, 0.17, 0.10, 1),
    "iron":       (0.14, 0.14, 0.15, 1),
}

def mat(name):
    m = bpy.data.materials.get(name)
    if m: return m
    m = bpy.data.materials.new(name); m.use_nodes = True
    b = m.node_tree.nodes["Principled BSDF"]
    b.inputs["Base Color"].default_value = PAL[name]
    b.inputs["Roughness"].default_value = 0.9
    return m

class Kit:
    """Accumula geometria per materiale, poi crea un oggetto per gruppo."""
    def __init__(self): self.bm = {}

    def _bm(self, name):
        if name not in self.bm: self.bm[name] = bmesh.new()
        return self.bm[name]

    def box(self, m, x0, y0, z0, sx, sy, sz):
        """x,z = pianta nel mondo; y = quota. In Blender: Y = -Z mondo."""
        if sx <= 0 or sy <= 0 or sz <= 0: return
        bm = self._bm(m)
        cx, cy, cz = x0 + sx/2, -(z0 + sz/2), y0 + sy/2
        vs = [bm.verts.new((cx+dx*sx/2, cy+dy*sz/2, cz+dz*sy/2))
              for dx in (-1,1) for dy in (-1,1) for dz in (-1,1)]
        for f in FACES:
            try: bm.faces.new([vs[i] for i in f])
            except ValueError: pass

    def cyl(self, m, cx, y0, cz, r, h, seg=10):
        bm = self._bm(m)
        bmesh.ops.create_cone(bm, cap_ends=True, segments=seg,
                              radius1=r, radius2=r, depth=h,
                              matrix=__import__("mathutils").Matrix.Translation((cx, -cz, y0+h/2)))

    def crenels(self, m, x0, x1, y, z0, z1, step=0.9, h=0.55):
        """Merli lungo un muro: la firma visiva della fortezza."""
        x = x0
        while x + 0.55 <= x1:
            self.box(m, x, y, z0, 0.55, h, z1 - z0)
            x += step

    def export(self, path, name):
        objs = []
        for m, bm in self.bm.items():
            me = bpy.data.meshes.new(f"{name}_{m}")
            bm.to_mesh(me); bm.free()
            o = bpy.data.objects.new(f"{name}_{m}", me)
            bpy.context.collection.objects.link(o)
            o.data.materials.append(mat(m))
            objs.append(o)
        bpy.ops.object.select_all(action='DESELECT')
        for o in objs: o.select_set(True)
        bpy.context.view_layer.objects.active = objs[0]
        bpy.ops.export_scene.gltf(filepath=path, export_format='GLB', use_selection=True,
                                  export_apply=True, export_yup=True, export_image_format='AUTO')
        print(f"RESULT: {path} · {len(objs)} materiali", flush=True)


def porto():
    k = Kit()
    # capannone portuale con tetto a shed
    k.box("conc_grey", -18, 0, -97, 12, 4.2, 6)
    k.box("acc_blue",  -18.3, 4.2, -97.3, 12.6, 0.25, 6.6)
    k.box("iron" if "iron" in PAL else "acc_rust", -13.5, 0, -97.2, 3.2, 3.2, 0.15)   # portone
    # container impilati (3 + 2)
    for i, (m, dz) in enumerate([("cont_red", 0), ("acc_rust", 0), ("cont_green", 2.5)]):
        k.box(m, 8, 0, -96 + dz, 6.0, 2.6, 2.4)
    k.box("acc_blue", 9.5, 2.6, -96.5, 6.0, 2.6, 2.4)
    k.box("cont_green", 8, 0, -88, 6.0, 2.6, 2.4)
    # gru a traliccio
    for x in (17.0, 18.4):
        k.box("acc_rust", x, 0, -93, 0.3, 13.0, 0.3)
    for y in (3, 6.5, 10, 12.5):
        k.box("acc_rust", 17.0, y, -93, 1.7, 0.22, 0.22)
    k.box("acc_rust", 17.6, 12.7, -93, 0.6, 0.6, 8.5)          # braccio
    k.box("conc_grey", 16.6, 0, -93.6, 2.0, 0.6, 2.0)          # basamento
    # bitte sul bordo banchina
    for x in range(-16, 20, 4):
        k.cyl("iron", x, 0, -99.2, 0.22, 0.7)
    # barca in secca
    k.box("acc_blue", 2, 0, -95, 5.2, 0.9, 1.6)
    k.box("acc_blue", 2.6, 0.9, -94.6, 4.0, 0.5, 1.0)
    k.box("wood_dark" if "wood_dark" in PAL else "acc_rust", 3.2, 0, -95.6, 0.5, 1.0, 0.5)
    # casse e reti
    for i in range(5):
        k.box("rope", -6 + i*0.9, 0, -85.5, 0.7, 0.6, 0.7)
    k.box("rope", -8, 0, -86, 2.2, 0.35, 1.4)
    # lampada di banchina
    k.box("iron" if "iron" in PAL else "acc_rust", 22, 0, -99, 0.25, 4.5, 0.25)
    k.box("acc_blue", 22, 4.5, -99, 0.9, 0.25, 0.9)
    k.export(f"{OUT}/spawn_porto.glb", "PortoVecchio")


def forte():
    k = Kit()
    # muraglia merlata sul fondo (z alto = lato sud)
    k.box("stone_warm", -22, 0, 95, 44, 4.2, 1.4)
    k.box("stone_dark", -22, 4.2, 95, 44, 0.35, 1.6)
    k.crenels("stone_warm", -22, 22, 4.55, 95, 96.4)
    # porta ad arco al centro: due stipiti + arco a conci
    k.box("stone_dark", -2.2, 0, 94.8, 1.1, 3.4, 1.6)
    k.box("stone_dark", 1.1, 0, 94.8, 1.1, 3.4, 1.6)
    for i in range(5):
        a = math.pi * i / 4
        k.box("stone_warm", -1.1 + 2.2 * i / 4, 3.4 + math.sin(a) * 0.5, 94.8, 0.55, 0.5, 1.6)
    # due torri
    for tx in (-17, 15):
        k.box("stone_warm", tx, 0, 93, 5, 9.5, 5)
        k.box("stone_dark", tx - 0.2, 9.5, 92.8, 5.4, 0.4, 5.4)
        k.crenels("stone_warm", tx - 0.2, tx + 5.2, 9.9, 92.8, 98.2, 1.0, 0.7)
        # stendardo
        k.box("wood_dark", tx + 2.4, 9.9, 93.4, 0.12, 3.2, 0.12)
        k.box("banner", tx + 2.5, 10.2, 93.4, 0.06, 2.2, 1.1)
    # bracieri davanti alla porta
    for bx in (-4.5, 4.5):
        k.cyl("iron", bx, 0, 92.6, 0.45, 1.0)
        k.cyl("banner", bx, 0.9, 92.6, 0.5, 0.25)
    # scalinata d'accesso alla porta
    for i in range(4):
        k.box("stone_warm", -3.4, 0, 93.6 + i*0.45, 6.8, 0.22 * (4-i), 0.45)
    # ceppi e catene lungo il muro
    for x in (-12, -8, 8, 12):
        k.box("wood_dark", x, 0, 94.2, 0.9, 0.8, 0.9)
    k.export(f"{OUT}/spawn_forte.glb", "ForteSanVerano")


bpy.ops.wm.read_factory_settings(use_empty=True)
porto()
bpy.ops.wm.read_factory_settings(use_empty=True)
forte()
