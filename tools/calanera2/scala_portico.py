"""Scala del porticato — mesh unica, nessun bpy.ops (contesto MCP senza active_object).
Rampa 19 gradini, alzata 3,3/19, pedata 0,30, larghezza 3,0, muretto 1,0 m sul lato +Z,
pianerottolo e pilastro che si innesta nel porticato. Export GLB."""
import bpy, os

RISE, STEPS, RUN, WIDTH, PH, PT = 3.3, 19, 0.30, 3.0, 1.0, 0.26
verts, faces = [], []

def box(x0,x1,y0,y1,z0,z1):
    o=len(verts)
    for x in (x0,x1):
        for y in (y0,y1):
            for z in (z0,z1):
                verts.append((x,z,y))
    def v(a,b,c): return o + a*4 + b*2 + c
    faces.extend([
        (v(0,0,0),v(0,0,1),v(0,1,1),v(0,1,0)),
        (v(1,0,1),v(1,0,0),v(1,1,0),v(1,1,1)),
        (v(0,0,0),v(1,0,0),v(1,0,1),v(0,0,1)),
        (v(0,1,1),v(1,1,1),v(1,1,0),v(0,1,0)),
        (v(0,0,0),v(0,1,0),v(1,1,0),v(1,0,0)),
        (v(0,0,1),v(1,0,1),v(1,1,1),v(0,1,1)),
    ])
rise=RISE/STEPS
for k in range(STEPS):
    x0=k*RUN; x1=x0+RUN; top=rise*(k+1)
    box(x0,x1,0.0,top,-WIDTH/2,WIDTH/2)
    box(x0,x1,top,top+PH,WIDTH/2,WIDTH/2+PT)
    box(x0,x1,top+PH,top+PH+0.10,WIDTH/2-0.03,WIDTH/2+PT+0.03)
xt=STEPS*RUN
box(xt,xt+1.4,0.0,RISE,-WIDTH/2,WIDTH/2+PT)
box(xt+0.95,xt+1.4,0.0,RISE+2.6,WIDTH/2,WIDTH/2+0.40)

for _o in list(bpy.data.objects):
    bpy.data.objects.remove(_o, do_unlink=True)
me=bpy.data.meshes.new("ScalaPorticoMesh")
me.from_pydata(verts,[],faces); me.validate(); me.update()
ob=bpy.data.objects.new("ScalaPortico",me)
bpy.context.collection.objects.link(ob)
mat=bpy.data.materials.new("M_Pietra_Conci"); mat.use_nodes=True
bsdf=mat.node_tree.nodes.get("Principled BSDF")
if bsdf:
    bsdf.inputs["Base Color"].default_value=(0.72,0.68,0.58,1.0)
    bsdf.inputs["Roughness"].default_value=0.85
me.materials.append(mat)
bpy.context.view_layer.objects.active=ob; ob.select_set(True)
out="/home/matteo/Progetti/Falezia/Assets/Art/Calanera/Props/scala_portico.glb"
os.makedirs(os.path.dirname(out),exist_ok=True)
bpy.ops.export_scene.gltf(filepath=out, export_format="GLB", use_selection=True)
print("EXPORT",out,"verts",len(verts),"faces",len(faces))
