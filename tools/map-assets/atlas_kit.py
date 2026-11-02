"""Atlante per pezzo: N materiali → UNA texture (albedo+normal+roughness) e un solo materiale.

Come: si impacchettano le mappe dei materiali in una griglia, si rimappano le UV di ogni
faccia dentro il proprio riquadro (uv->frazione, così il tassello continua a ripetersi), e si
sostituiscono i materiali con uno solo che usa l'atlante. Nessun bake Cycles: è un repack.
"""
import bpy, os, math
import numpy as np

ROOT = "/home/matteo/Progetti/Falezia"
TEXOUT = f"{ROOT}/Assets/Resources/MapTex"
ATLAS = 2048
PAD = 8

def _img_of_node(node):
    return node.image if node.type == 'TEX_IMAGE' and node.image else None

def material_maps(mat):
    """(albedo_path, normal_path, roughness_path, colore) dei nodi del materiale."""
    nt = mat.node_tree
    bsdf = next((n for n in nt.nodes if n.type == 'BSDF_PRINCIPLED'), None)
    alb_img = nrm_img = rgh_img = None
    if bsdf:
        for link in nt.links:
            if link.to_node is bsdf and link.from_node.type == 'TEX_IMAGE':
                if link.to_socket.name == "Base Color": alb_img = link.from_node.image
                if link.to_socket.name == "Roughness": rgh_img = link.from_node.image
        for link in nt.links:
            if link.to_node.type == 'NORMAL_MAP':
                src = link.from_node
                if src.type == 'TEX_IMAGE': nrm_img = src.image
    color = tuple(bsdf.inputs["Base Color"].default_value)[:3] if bsdf else (0.8, 0.8, 0.8)
    return (alb_img.filepath if alb_img and alb_img.filepath else None,
            nrm_img.filepath if nrm_img and nrm_img.filepath else None,
            rgh_img.filepath if rgh_img and rgh_img.filepath else None,
            color)

def _load(path, size, gray=False):
    """Pixels da un'immagine di Blender, ridimensionati a `size` (numpy, niente PIL)."""
    if not path or not os.path.exists(path):
        return None
    img = bpy.data.images.load(path, check_existing=True)
    w, h = img.size
    px = np.array(img.pixels[:], np.float32).reshape(h, w, 4)[::-1]   # Blender è bottom-up
    if (w, h) != (size, size):
        yi = (np.linspace(0, h - 1, size)).astype(int)
        xi = (np.linspace(0, w - 1, size)).astype(int)
        px = px[yi][:, xi]
    if gray:
        g = px[..., 0] * 0.299 + px[..., 1] * 0.587 + px[..., 2] * 0.114
        return g
    return px[..., :3]

def _save(arr, path, gray=False, size=ATLAS):
    h, w = arr.shape[:2]
    img = bpy.data.images.new(os.path.basename(path), w, h, alpha=False)
    if gray:
        rgba = np.concatenate([arr[..., None]] * 3 + [np.ones((h, w, 1), np.float32)], -1)
    else:
        rgba = np.concatenate([arr, np.ones((h, w, 1), np.float32)], -1)
    img.pixels = rgba[::-1].ravel().tolist()
    img.filepath_raw = path
    img.file_format = 'JPEG'
    img.save()

def build_atlas(objs, name, tile_px=None):
    """Unisce gli oggetti, impacchetta le mappe e rimappa le UV."""
    bpy.ops.object.select_all(action='DESELECT')
    for o in objs: o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    if len(objs) > 1: bpy.ops.object.join()
    obj = bpy.context.view_layer.objects.active
    obj.name = name

    mats = [m for m in obj.data.materials if m]
    n = max(1, len(mats))
    cols = math.ceil(math.sqrt(n)); rows = math.ceil(n / cols)
    tile = (ATLAS - PAD * (cols + 1)) // cols
    tile = min(tile, (ATLAS - PAD * (rows + 1)) // rows)
    small = min(512, tile)

    alb_canvas = np.zeros((ATLAS, ATLAS, 3), np.float32); alb_canvas[:] = 0.5
    nrm_canvas = np.zeros((ATLAS, ATLAS, 3), np.float32); nrm_canvas[:] = (0.5, 0.5, 1.0)
    rgh_canvas = np.zeros((ATLAS, ATLAS), np.float32); rgh_canvas[:] = 0.85

    rects = {}
    for i, mat in enumerate(mats):
        cx, cy = i % cols, i // cols
        x0 = PAD + cx * (tile + PAD)
        y0 = PAD + cy * (tile + PAD)
        alb_p, nrm_p, rgh_p, color = material_maps(mat)
        a = _load(alb_p, small)
        if a is None:
            a = np.ones((small, small, 3), np.float32) * np.array(color, np.float32)[None, None, :]
        nr = _load(nrm_p, small)
        if nr is None:
            nr = np.ones((small, small, 3), np.float32) * np.array([0.5, 0.5, 1.0])
        rg = _load(rgh_p, small, gray=True)
        if rg is None:
            rg = np.ones((small, small), np.float32) * 0.85

        def _rs(x, size):
            h, w = x.shape[:2]
            yi = np.linspace(0, h - 1, size).astype(int); xi = np.linspace(0, w - 1, size).astype(int)
            return x[yi][:, xi]
        a = _rs(a, tile); nr = _rs(nr, tile); rg = _rs(rg, tile)

        alb_canvas[y0:y0+tile, x0:x0+tile] = a
        nrm_canvas[y0:y0+tile, x0:x0+tile] = nr
        rgh_canvas[y0:y0+tile, x0:x0+tile] = rg
        # riquadro in coordinate UV (immagine: y in alto = v alto)
        u0 = x0 / ATLAS; v0 = 1.0 - (y0 + tile) / ATLAS
        rects[i] = (u0, v0, tile / ATLAS)

    # UV rimappate: la frazione dentro il riquadro conserva la ripetizione del tassello
    for poly in obj.data.polygons:
        r = rects.get(poly.material_index, (0.0, 0.0, 1.0))
        u0, v0, ts = r
        for li in poly.loop_indices:
            uv = obj.data.uv_layers.active.data[li].uv
            uv.x = u0 + (uv.x % 1.0) * ts
            uv.y = v0 + (uv.y % 1.0) * ts

    pa = f"{TEXOUT}/atlas_{name}_albedo.jpg"
    pn = f"{TEXOUT}/atlas_{name}_normal.jpg"
    pr = f"{TEXOUT}/atlas_{name}_rough.jpg"
    _save(np.clip(alb_canvas, 0, 1), pa)
    _save(np.clip(nrm_canvas, 0, 1), pn)
    _save(np.clip(rgh_canvas, 0, 1), pr, gray=True)

    mat = bpy.data.materials.new(f"{name}_atlas"); mat.use_nodes = True
    nt = mat.node_tree; bsdf = nt.nodes["Principled BSDF"]
    ia = nt.nodes.new("ShaderNodeTexImage"); ia.image = bpy.data.images.load(pa)
    nt.links.new(ia.outputs["Color"], bsdf.inputs["Base Color"])
    ir = nt.nodes.new("ShaderNodeTexImage"); ir.image = bpy.data.images.load(pr)
    ir.image.colorspace_settings.name = 'Non-Color'
    nt.links.new(ir.outputs["Color"], bsdf.inputs["Roughness"])
    inr = nt.nodes.new("ShaderNodeTexImage"); inr.image = bpy.data.images.load(pn)
    inr.image.colorspace_settings.name = 'Non-Color'
    nm = nt.nodes.new("ShaderNodeNormalMap")
    nt.links.new(inr.outputs["Color"], nm.inputs["Color"])
    nt.links.new(nm.outputs["Normal"], bsdf.inputs["Normal"])
    bsdf.inputs["Metallic"].default_value = 0.0

    obj.data.materials.clear()
    obj.data.materials.append(mat)
    return obj, len(mats), (cols, rows, tile)
