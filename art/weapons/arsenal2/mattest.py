"""Banco di prova dei materiali: 8 cubi smussati, uno per famiglia."""
import bpy, os, math
TEX = "/home/matteo/Progetti/Falezia/game/assets/weapons/_shared/tex"
OUT = "/home/matteo/Progetti/Falezia/art/weapons/arsenal2/review/mattest.png"
os.makedirs(os.path.dirname(OUT), exist_ok=True)

for o in list(bpy.data.objects):
    bpy.data.objects.remove(o, do_unlink=True)
sc = bpy.context.scene

def mat(fam):
    m = bpy.data.materials.new(fam); m.use_nodes = True
    nt = m.node_tree; nodes = nt.nodes
    bsdf = nodes["Principled BSDF"]
    def img(name, non_color=False):
        n = nodes.new("ShaderNodeTexImage")
        n.image = bpy.data.images.load(os.path.join(TEX, name))
        if non_color:
            n.image.colorspace_settings.name = "Non-Color"
        return n
    bc = img(fam + "_basecolor.png")
    nt.links.new(bc.outputs["Color"], bsdf.inputs["Base Color"])
    orm = img(fam + "_orm.png", True)
    sep = nodes.new("ShaderNodeSeparateColor")
    nt.links.new(orm.outputs["Color"], sep.inputs["Color"])
    nt.links.new(sep.outputs["Green"], bsdf.inputs["Roughness"])
    nt.links.new(sep.outputs["Blue"], bsdf.inputs["Metallic"])
    nm = img(fam + "_normal.png", True)
    nmap = nodes.new("ShaderNodeNormalMap")
    nt.links.new(nm.outputs["Color"], nmap.inputs["Color"])
    nt.links.new(nmap.outputs["Normal"], bsdf.inputs["Normal"])
    return m

fams = ["polymer", "stipple", "steel", "stainless", "wood", "sheet", "green", "brass"]
for i, f in enumerate(fams):
    bpy.ops.mesh.primitive_cube_add(size=0.09, location=((i % 4 - 1.5) * 0.115, 0, 0.02 if i < 4 else -0.10))
    ob = bpy.context.object
    ob.data.materials.append(mat(f))
    md = ob.modifiers.new("b", "BEVEL"); md.width = 0.008; md.segments = 4
    bpy.ops.object.modifier_apply(modifier=md.name)
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.uv.cube_project(cube_size=0.12, correct_aspect=True, scale_to_bounds=False)
    bpy.ops.object.mode_set(mode='OBJECT')
    bpy.ops.object.shade_smooth()

def area(n, loc, rot, size, energy, col=(1, 1, 1)):
    d = bpy.data.lights.new(n, "AREA"); d.size = size; d.energy = energy; d.color = col
    o = bpy.data.objects.new(n, d); o.location = loc; o.rotation_euler = rot
    sc.collection.objects.link(o); o.visible_camera = False

area("k", (-0.5, -0.5, 0.6), (math.radians(35), 0, math.radians(-35)), 0.5, 5.0, (1, 0.96, 0.9))
area("f", (0.6, -0.4, 0.2), (math.radians(65), 0, math.radians(55)), 0.6, 1.6, (0.8, 0.87, 1))
area("r", (0.2, 0.5, 0.3), (math.radians(-50), 0, math.radians(10)), 0.5, 3.0, (1, 0.93, 0.85))
w = bpy.data.worlds.new("w"); sc.world = w; w.use_nodes = True
w.node_tree.nodes["Background"].inputs["Color"].default_value = (0.16, 0.18, 0.21, 1)
cd = bpy.data.cameras.new("c"); cd.lens = 70
cam = bpy.data.objects.new("c", cd); cam.location = (0, -0.62, 0.10)
cam.rotation_euler = (math.radians(80), 0, 0); sc.collection.objects.link(cam); sc.camera = cam
sc.render.engine = "BLENDER_EEVEE"; sc.eevee.taa_render_samples = 64
sc.view_settings.view_transform = "AgX"; sc.view_settings.exposure = 0.25
sc.render.resolution_x = 1200; sc.render.resolution_y = 600
sc.render.filepath = OUT
bpy.ops.render.render(write_still=True)
print("OK")
