"""Anteprima Blender del LAYOUT: importa il kit una volta e lo dispone come Unity.
Serve a giudicare la città in pochi secondi, senza build né avvio del gioco.

Uso (via MCP):  exec(open(...).read())  con VIEW/CAM definiti prima, oppure
modificare in fondo le variabili CAM_POS e CAM_TGT.
"""
import bpy, json, math, os
from mathutils import Euler, Vector

ROOT = "/home/matteo/Progetti/Falezia"
KIT  = os.path.join(ROOT, "Assets/StreamingAssets/map/kit")
LAYOUT = os.path.join(ROOT, "Assets/StreamingAssets/map/props.json")

def build_layout():
    """Importa ogni pezzo una volta, lo unisce in un solo oggetto e lo duplica."""
    data = json.load(open(LAYOUT))
    by_model = {}
    for it in data["items"]:
        by_model.setdefault(it["model"].split("/")[-1], []).append(it)

    templates = {}
    for model in by_model:
        path = os.path.join(KIT, model + ".glb")
        if not os.path.exists(path): continue
        before = set(bpy.data.objects)
        bpy.ops.import_scene.gltf(filepath=path)
        new_objs = [o for o in bpy.data.objects if o not in before and o.type == 'MESH']
        if not new_objs: continue
        bpy.ops.object.select_all(action='DESELECT')
        for o in new_objs: o.select_set(True)
        bpy.context.view_layer.objects.active = new_objs[0]
        if len(new_objs) > 1:
            bpy.ops.object.join()
        tpl = bpy.context.view_layer.objects.active
        tpl.name = "TPL_" + model
        templates[model] = tpl
        tpl.location = (0, 0, 0); tpl.rotation_euler = (0, 0, 0); tpl.scale = (1, 1, 1)

    for model, insts in by_model.items():
        tpl = templates.get(model)
        if not tpl: continue
        for it in insts:
            c = tpl.copy()                     # mesh condivisa
            bpy.context.collection.objects.link(c)
            s = it["scale"] or 1.0
            c.location = (it["x"], -it["z"], it["y"])
            c.rotation_euler = (0, math.radians(it["rot"]), 0)
            c.scale = (s, s, s)

    for tpl in templates.values():
        bpy.data.objects.remove(tpl, do_unlink=True)

def render(pos, tgt, path, lens=35, res=(1400, 800), samples=24):
    cam = bpy.data.objects.new("pcam", bpy.data.cameras.new("pcam"))
    bpy.context.collection.objects.link(cam)
    bpy.context.scene.camera = cam
    cam.location = pos
    t = bpy.data.objects.new("ptgt", None); bpy.context.collection.objects.link(t)
    t.location = tgt
    tr = cam.constraints.new('TRACK_TO'); tr.target = t
    tr.track_axis = 'TRACK_NEGATIVE_Z'; tr.up_axis = 'UP_Y'
    cam.data.lens = lens
    sc = bpy.context.scene
    sc.world = sc.world or bpy.data.worlds.new("w"); sc.world.use_nodes = True
    sc.world.node_tree.nodes["Background"].inputs[0].default_value = (0.42, 0.52, 0.66, 1)
    if not any(o.type == 'LIGHT' for o in bpy.data.objects):
        bpy.ops.object.light_add(type='SUN', location=(40, 40, 80))
        bpy.context.object.data.energy = 2.6
        bpy.context.object.rotation_euler = Euler((math.radians(50), 0, math.radians(30)))
    sc.render.engine = 'CYCLES'; sc.cycles.samples = samples; sc.cycles.device = 'CPU'
    sc.render.resolution_x, sc.render.resolution_y = res
    sc.render.filepath = path
    bpy.ops.render.render(write_still=True)
    print("PREVIEW OK", path)
