import json, struct, sys, numpy as np
CT={5120:np.int8,5121:np.uint8,5122:np.int16,5123:np.uint16,5125:np.uint32,5126:np.float32}
NC={'SCALAR':1,'VEC2':2,'VEC3':3,'VEC4':4,'MAT4':16}
def load(path):
    b=open(path,'rb').read()
    assert b[:4]==b'glTF'
    off=12; js=None; bin_=None
    while off<len(b):
        ln,typ=struct.unpack_from('<II',b,off); data=b[off+8:off+8+ln]
        if typ==0x4E4F534A: js=json.loads(data)
        elif typ==0x004E4942: bin_=data
        off+=8+ln
    return js,bin_
def acc(js,bin_,i):
    a=js['accessors'][i]; bv=js['bufferViews'][a['bufferView']]
    n=NC[a['type']]; dt=CT[a['componentType']]
    start=bv.get('byteOffset',0)+a.get('byteOffset',0)
    stride=bv.get('byteStride',0); cnt=a['count']
    item=np.dtype(dt).itemsize*n
    if stride and stride!=item:
        raw=np.frombuffer(bin_,dtype=np.uint8,count=stride*cnt,offset=start).reshape(cnt,stride)[:,:item].copy()
        arr=raw.view(dt).reshape(cnt,n)
    else:
        arr=np.frombuffer(bin_,dtype=dt,count=cnt*n,offset=start).reshape(cnt,n)
    return arr
if __name__=='__main__':
    for p in sys.argv[1:]:
        js,bin_=load(p)
        tris=0; verts=0; mins=[];maxs=[]
        for m in js['meshes']:
            for pr in m['primitives']:
                pos=acc(js,bin_,pr['attributes']['POSITION']); verts+=len(pos)
                tris+=(js['accessors'][pr['indices']]['count']//3) if 'indices' in pr else len(pos)//3
                mins.append(pos.min(0)); maxs.append(pos.max(0))
        mn=np.min(mins,0); mx=np.max(maxs,0)
        imgs=[(im.get('mimeType'),js['bufferViews'][im['bufferView']]['byteLength']//1024) for im in js.get('images',[])]
        mats=[(m.get('name'),list(m.get('pbrMetallicRoughness',{}).keys()), 'normal' in m.get('normalTexture',{}) or 'normalTexture' in m) for m in js.get('materials',[])]
        nodes=[(n.get('name'),n.get('rotation'),n.get('scale'),n.get('translation')) for n in js['nodes']]
        print(f"== {p.split('/')[-1]}: tris {tris:,} verts {verts:,} meshes {len(js['meshes'])} prims {sum(len(m['primitives']) for m in js['meshes'])}")
        print("   bbox size", np.round(mx-mn,4), "min", np.round(mn,3), "max", np.round(mx,3))
        print("   materials", mats)
        print("   images", imgs, "samplers", len(js.get('samplers',[])))
        print("   nodes", nodes[:4], "attrs", list(js['meshes'][0]['primitives'][0]['attributes'].keys()))
        print("   generator", js.get('asset',{}).get('generator'), "extensions", js.get('extensionsUsed'))
