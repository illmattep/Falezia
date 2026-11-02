"""Genera le caverne di Calanera come gusci curvi, con le misure esatte della mappa.

Regola: sotto 2,2 m la superficie coincide col volume di collisione; sopra può rientrare.
Genera:
  cave_grotta.glb      camera 20x24x6 con 3 buchi d'ingresso, volta a botte
  cave_ingresso.glb    tubo dritto 14 m, 3x3, per l'ingresso ovest
  cave_ingresso_l.glb  tubo a gomito 3,4x3, per gli ingressi laterali
  cave_galleria.glb    tunnel 12x60x6 con 2 nicchie
  galleria_servizio.glb tubo 14x4x3
  cave_rocce.glb       massi + stalattiti + detriti (per riempire gli spigoli)

Uso: blender -b -noaudio --factory-startup --python build_cave.py -- /out/dir
"""
import bpy, bmesh, math, os, sys, random
from mathutils import Vector, noise

OUT = sys.argv[sys.argv.index("--") + 1] if "--" in sys.argv else "/tmp/cave"
os.makedirs(OUT, exist_ok=True)
SPRING = 2.2          # da qui in su la parete può rientrare
INSET = 0.08          # il guscio sta 8 cm DENTRO il volume: copre le facce delle scatole senza z-fighting
random.seed(7)

def new_mesh(name):
    me = bpy.data.meshes.new(name)
    ob = bpy.data.objects.new(name, me)
    bpy.context.collection.objects.link(ob)
    return ob, me

def rock_mat():
    m = bpy.data.materials.get("Roccia")
    if m: return m
    m = bpy.data.materials.new("Roccia")
    m.use_nodes = True
    bsdf = m.node_tree.nodes["Principled BSDF"]
    bsdf.inputs["Base Color"].default_value = (0.29, 0.27, 0.24, 1)
    bsdf.inputs["Roughness"].default_value = 0.92
    n = m.node_tree.nodes.new("ShaderNodeTexNoise")
    n.inputs["Scale"].default_value = 6.0
    b = m.node_tree.nodes.new("ShaderNodeBump")
    b.inputs["Strength"].default_value = 0.35
    m.node_tree.links.new(n.outputs["Fac"], b.inputs["Height"])
    m.node_tree.links.new(b.outputs["Normal"], bsdf.inputs["Normal"])
    m.use_backface_culling = False   # il guscio si vede da dentro e da fuori
    return m

def jitter(p, amount, scale=0.35):
    """Rumore coerente: sposta un punto lungo la sua normale."""
    n = noise.noise_vector(Vector((p.x * scale, p.y * scale, p.z * scale)))
    return p + n * amount

def wall_grid(bm, a0, a1, fixed, axis, height, bulge, flip, holes, uv_scale=0.25):
    """
    Una parete come griglia. axis = 'x' (parete a x=fixed, si estende in z) o 'z'.
    La superficie rientra sopra SPRING di `bulge` metri, con rumore.
    holes = lista di (start, end, y0, y1) lungo l'asse della parete: lì non si crea la faccia.
    """
    n_len = max(4, int(abs(a1 - a0) / 0.6))
    n_h = 12
    verts = {}
    for i in range(n_len + 1):
        a = a0 + (a1 - a0) * i / n_len
        for j in range(n_h + 1):
            h = height * j / n_h
            inset = INSET if h < SPRING else INSET + bulge * ((h - SPRING) / max(height - SPRING, 0.1)) ** 1.5
            # il pavimento resta esatto; sopra rientra e ondeggia
            wob = 0.0 if h < SPRING else 0.12 * math.sin(a * 1.7 + h * 2.1) + random.uniform(-0.04, 0.04)
            off = inset + wob
            if axis == 'x':
                p = Vector((fixed + (off if flip else -off), h, a))
            else:
                p = Vector((a, h, fixed + (off if flip else -off)))
            verts[(i, j)] = bm.verts.new(jitter(p, 0.05 if h > 0.4 else 0.02))

    def in_hole(a, h):
        for (s, e, y0, y1) in holes:
            if s <= a <= e and y0 <= h <= y1: return True
        return False

    for i in range(n_len):
        a_mid = a0 + (a1 - a0) * (i + 0.5) / n_len
        for j in range(n_h):
            h_mid = height * (j + 0.5) / n_h
            if in_hole(a_mid, h_mid): continue
            bm.faces.new((verts[(i, j)], verts[(i + 1, j)], verts[(i + 1, j + 1)], verts[(i, j + 1)]))

def ceiling_grid(bm, x0, x1, z0, z1, h, dip=0.35):
    nx = max(4, int((x1 - x0) / 0.8)); nz = max(4, int((z1 - z0) / 0.8))
    v = {}
    for i in range(nx + 1):
        for k in range(nz + 1):
            x = x0 + (x1 - x0) * i / nx
            z = z0 + (z1 - z0) * k / nz
            # volta: più alta al centro, più bassa ai bordi, con rumore
            ex = 1 - abs(2 * (x - (x0 + x1) / 2) / max(x1 - x0, 0.1))
            ez = 1 - abs(2 * (z - (z0 + z1) / 2) / max(z1 - z0, 0.1))
            y = h - INSET - dip * (1 - min(ex, ez) ** 0.6) + random.uniform(-0.08, 0.08)
            v[(i, k)] = bm.verts.new(jitter(Vector((x, y, z)), 0.06))
    for i in range(nx):
        for k in range(nz):
            bm.faces.new((v[(i, k)], v[(i + 1, k)], v[(i + 1, k + 1)], v[(i, k + 1)]))

def floor_grid(bm, x0, x1, z0, z1, y=0.0):
    nx = max(4, int((x1 - x0) / 1.0)); nz = max(4, int((z1 - z0) / 1.0))
    v = {}
    for i in range(nx + 1):
        for k in range(nz + 1):
            x = x0 + (x1 - x0) * i / nx
            z = z0 + (z1 - z0) * k / nz
            v[(i, k)] = bm.verts.new(jitter(Vector((x, y - 0.02 - random.uniform(0, 0.03), z)), 0.03))
    for i in range(nx):
        for k in range(nz):
            bm.faces.new((v[(i, k)], v[(i + 1, k)], v[(i + 1, k + 1)], v[(i, k + 1)]))

def arch_profile(width, height, segments=10):
    """Sezione a tutto sesto: spalle verticali fino a SPRING, poi arco ellittico."""
    pts = []
    hw = width / 2
    for i in range(segments + 1):
        t = i / segments
        a = math.pi * t
        # da destra a sinistra passando per l'alto
        x = hw * math.cos(a)
        y = SPRING + (height - SPRING) * math.sin(a)
        pts.append((x, y))
    return pts

def tube(name, path, width, height, closed_ends=False):
    """Sweep di una sezione ad arco lungo un percorso (lista di punti XZ)."""
    ob, me = new_mesh(name)
    bm = bmesh.new()
    prof = arch_profile(width, height)
    rings = []
    for i in range(len(path)):
        p = Vector((path[i][0], 0, path[i][1]))
        # direzione
        nxt = Vector((path[min(i + 1, len(path) - 1)][0], 0, path[min(i + 1, len(path) - 1)][1]))
        prv = Vector((path[max(i - 1, 0)][0], 0, path[max(i - 1, 0)][1]))
        d = (nxt - prv); d.y = 0
        if d.length < 1e-5: d = Vector((1, 0, 0))
        d.normalize()
        side = Vector((-d.z, 0, d.x))
        ring = []
        for (px, py) in prof:
            world = p + side * px + Vector((0, py, 0))
            ring.append(bm.verts.new(jitter(world, 0.05 if py > SPRING else 0.02)))
        rings.append(ring)
    for i in range(len(rings) - 1):
        for j in range(len(prof) - 1):
            bm.faces.new((rings[i][j], rings[i + 1][j], rings[i + 1][j + 1], rings[i][j + 1]))
    if closed_ends:
        for ring in (rings[0], rings[-1]):
            bm.faces.new(ring)
    bm.normal_update()
    for f in bm.faces: f.normal_flip()   # si guarda da dentro
    bm.to_mesh(me); bm.free()
    ob.data.materials.append(rock_mat())
    return ob

def arc_path(cx, cz, r, a0, a1, steps=8):
    return [(cx + r * math.cos(a), cz + r * math.sin(a)) for a in
            [a0 + (a1 - a0) * i / steps for i in range(steps + 1)]]

def chamber(name, x0, x1, z0, z1, h, holes):
    """Camera con volta e buchi: holes = {parete: [(a0,a1,y0,y1)]}"""
    ob, me = new_mesh(name)
    bm = bmesh.new()
    wall_grid(bm, z0, z1, x0, 'x', h, 0.55, False, holes.get('ovest', []))
    wall_grid(bm, z0, z1, x1, 'x', h, 0.55, True, holes.get('est', []))
    wall_grid(bm, x0, x1, z0, 'z', h, 0.55, False, holes.get('nord', []))
    wall_grid(bm, x0, x1, z1, 'z', h, 0.55, True, holes.get('sud', []))
    ceiling_grid(bm, x0, x1, z0, z1, h)
    floor_grid(bm, x0, x1, z0, z1)
    bm.normal_update()
    bm.to_mesh(me); bm.free()
    ob.data.materials.append(rock_mat())
    return ob

def export(objs, path):
    bpy.ops.object.select_all(action='DESELECT')
    for o in objs: o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    bpy.ops.object.shade_smooth()
    bpy.ops.export_scene.gltf(filepath=path, export_format='GLB', use_selection=True,
                              export_apply=True, export_yup=True,
                              export_image_format='AUTO', export_materials='EXPORT')

def clear():
    bpy.ops.wm.read_factory_settings(use_empty=True)

# ---------------------------------------------------------------- GROTTA
clear()
ch = chamber("CaveGrotta", 66, 86, -12, 12, 6.0, {
    'ovest': [(-1.5, 1.5, 0, 3.0)],      # ingresso frontale
    'nord':  [(69.3, 72.7, 0, 3.0)],     # ingresso laterale nord
    'sud':   [(69.3, 72.7, 0, 3.0)],     # ingresso laterale sud
})
export([ch], f"{OUT}/cave_grotta.glb")

# ingresso frontale: tubo dritto 14 m lungo X, 3x3
t1 = tube("CaveIngresso", [(52, 0), (66, 0)], 3.0, 3.0)
export([t1], f"{OUT}/cave_ingresso.glb")

# ingressi laterali a gomito: 3,4 larghi, da z=±30 a z=±12 passando per x=71
path_n = [(68, -30), (68, -20.6), (69.4, -19.2), (71, -19.2), (71, -12)]
t2 = tube("CaveIngressoL", path_n, 3.4, 3.0)
export([t2], f"{OUT}/cave_ingresso_l.glb")

# ---------------------------------------------------------------- GALLERIA
clear()
gal = chamber("Galleria", -78, -66, -30, 30, 6.0, {
    'ovest': [(9.0, 13.0, 0, 3.0), (-13.0, -9.0, 0, 3.0)],   # nicchie
    'est':   [(-2.0, 2.0, 0, 3.0)],                          # servizio
})
export([gal], f"{OUT}/cave_galleria.glb")

t3 = tube("GalleriaServizio", [(-52, 0), (-66, 0)], 4.0, 3.0)
export([t3], f"{OUT}/galleria_servizio.glb")

# ---------------------------------------------------------------- ROCE E DETTAGLI
clear()
objs = []
for i in range(6):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2, radius=random.uniform(0.8, 2.0))
    o = bpy.context.object
    o.name = f"Masso{i}"
    bm = bmesh.new(); bm.from_mesh(o.data)
    for v in bm.verts:
        v.co += noise.noise_vector(v.co * 2.0) * 0.35
    bm.to_mesh(o.data); bm.free()
    o.location = (i * 3.2, 0, 0)
    o.data.materials.append(rock_mat())
    objs.append(o)

for i in range(4):
    bpy.ops.mesh.primitive_cone_add(vertices=7, radius1=random.uniform(0.25, 0.5),
                                    depth=random.uniform(1.2, 2.4))
    o = bpy.context.object
    o.name = f"Stalattite{i}"
    o.rotation_euler = (math.pi, 0, random.uniform(0, 6.28))   # punta in giù
    o.location = (i * 2.0, 6.0, 0)
    o.data.materials.append(rock_mat())
    objs.append(o)

export(objs, f"{OUT}/cave_rocce.glb")

print("RESULT:" + str({"ok": True, "files": os.listdir(OUT)}))
