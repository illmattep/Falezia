"""BAKE del kit: ogni pezzo diventa mesh + 3 texture cotte (albedo, normal, roughness).

Perché: in Blender i materiali sono procedurali (texture × tinta, normali collegate a mano).
Cuocendoli in un atlante per pezzo, Unity riceve un asset semplice e identico all'originale:
un materiale, tre texture, nessuna dipendenza da nodi.

Procedura per pezzo:
 1. costruisce il pezzo (kit.py) e unisce tutte le mesh in un oggetto;
 2. Smart UV Project sull'intero oggetto (un atlante);
 3. aggiunge a OGNI materiale un nodo immagine sullo stesso atlante (destinazione del bake);
 4. cuoce DIFFUSE (solo colore), NORMAL e ROUGHNESS;
 5. sostituisce i materiali con uno solo che usa le tre immagini;
 6. esporta in map/kit_baked/<nome>.glb (JPEG per albedo/roughness, JPEG per normal).
"""
import bpy, os, sys, math

ROOT = "/home/matteo/Progetti/Falezia"
OUT = f"{ROOT}/Assets/StreamingAssets/map/kit_baked"
RES = 1024
MARGIN = 8

def clean():
    for o in list(bpy.data.objects):
        bpy.data.objects.remove(o, do_unlink=True)
    for m in list(bpy.data.materials):
        if m.users == 0:
            bpy.data.materials.remove(m)
    for i in list(bpy.data.images):
        if i.users == 0 and i.name != "Render Result":
            bpy.data.images.remove(i)

def join_all(objs):
    bpy.ops.object.select_all(action='DESELECT')
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    if len(objs) > 1:
        bpy.ops.object.join()
    return bpy.context.view_layer.objects.active

def unwrap(obj):
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.uv.smart_project(angle_limit=math.radians(66), island_margin=0.015)
    bpy.ops.object.mode_set(mode='OBJECT')

def make_image(name, color, is_data):
    img = bpy.data.images.new(name, RES, RES, alpha=False, float_buffer=False)
    img.generated_color = color
    if is_data:
        img.colorspace_settings.name = 'Non-Color'
    return img

def add_target(mat, img):
    """Nodo immagine attivo in ogni materiale: è lì che il bake scrive."""
    nt = mat.node_tree
    tex = nt.nodes.get("BAKE_TARGET") or nt.nodes.new("ShaderNodeTexImage")
    tex.name = "BAKE_TARGET"
    tex.image = img
    tex.select = True
    nt.nodes.active = tex
    return tex

def bake(obj, kind, img, samples=1):
    sc = bpy.context.scene
    sc.render.engine = 'CYCLES'
    sc.cycles.device = 'CPU'
    sc.cycles.samples = samples
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.bake(type=kind, margin=MARGIN, use_selected_to_active=False,
                        pass_filter={'COLOR'} if kind == 'DIFFUSE' else set())
    return img

def bake_piece(name, build_fn, res=RES, samples=1):
    clean()
    objs = build_fn()
    obj = join_all(objs)
    obj.name = name
    unwrap(obj)

    alb = make_image(f"{name}_albedo", (0.5, 0.5, 0.5, 1), False)
    nrm = make_image(f"{name}_normal", (0.5, 0.5, 1.0, 1), True)
    rgh = make_image(f"{name}_rough",  (0.8, 0.8, 0.8, 1), True)

    mats = [m for m in obj.data.materials if m]
    for m in mats:
        add_target(m, alb)
    bake(obj, 'DIFFUSE', alb, samples)

    for m in mats:
        add_target(m, nrm)
    bake(obj, 'NORMAL', nrm, samples)

    for m in mats:
        add_target(m, rgh)
    bake(obj, 'ROUGHNESS', rgh, samples)

    # materiale unico con le mappe cotte
    mat = bpy.data.materials.new(f"{name}_baked")
    mat.use_nodes = True
    nt = mat.node_tree
    bsdf = nt.nodes["Principled BSDF"]
    t_alb = nt.nodes.new("ShaderNodeTexImage"); t_alb.image = alb
    nt.links.new(t_alb.outputs["Color"], bsdf.inputs["Base Color"])
    t_r = nt.nodes.new("ShaderNodeTexImage"); t_r.image = rgh
    t_r.image.colorspace_settings.name = 'Non-Color'
    nt.links.new(t_r.outputs["Color"], bsdf.inputs["Roughness"])
    t_n = nt.nodes.new("ShaderNodeTexImage"); t_n.image = nrm
    t_n.image.colorspace_settings.name = 'Non-Color'
    nm = nt.nodes.new("ShaderNodeNormalMap")
    nt.links.new(t_n.outputs["Color"], nm.inputs["Color"])
    nt.links.new(nm.outputs["Normal"], bsdf.inputs["Normal"])
    bsdf.inputs["Metallic"].default_value = 0.0

    obj.data.materials.clear()
    obj.data.materials.append(mat)

    for img in (alb, nrm, rgh):
        img.filepath_raw = f"{ROOT}/Assets/Resources/MapTex/baked_{img.name}.jpg"
        img.file_format = 'JPEG'
        img.save()

    os.makedirs(OUT, exist_ok=True)
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    path = f"{OUT}/{name}.glb"
    bpy.ops.export_scene.gltf(filepath=path, export_format='GLB', use_selection=True,
                              export_apply=True, export_yup=True, export_image_format='AUTO')
    return path
