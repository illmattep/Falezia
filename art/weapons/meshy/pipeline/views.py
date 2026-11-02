import sys, io, numpy as np
from PIL import Image
sys.path.insert(0,'/home/claude/weapons')
from glbinfo import load, acc
def mesh(p):
    js,b=load(p); pr=js['meshes'][0]['primitives'][0]
    P=acc(js,b,pr['attributes']['POSITION']).astype(np.float64)
    UV=acc(js,b,pr['attributes']['TEXCOORD_0']).astype(np.float64)
    I=acc(js,b,pr['indices']).reshape(-1,3).astype(np.int64)
    mat=js['materials'][0]; ti=mat['pbrMetallicRoughness']['baseColorTexture']['index']
    im=js['images'][js['textures'][ti]['source']]; bv=js['bufferViews'][im['bufferView']]
    tex=Image.open(io.BytesIO(b[bv.get('byteOffset',0):bv.get('byteOffset',0)+bv['byteLength']])).convert('RGB')
    return js,b,P,UV,I,tex
def splat(P,C,ax,W=900):
    # orthographic depth-sorted splat; ax=(u,v,depth sign axis)
    u,v,d,sd=ax
    U=P[:,u]; V=P[:,v]; D=P[:,d]*sd
    s=W/ (max(np.ptp(U),np.ptp(V))*1.05)
    x=((U-U.mean())*s+W/2).astype(int); y=(W/2-(V-V.mean())*s).astype(int)
    o=np.argsort(D); img=np.full((W,W,3),40,np.uint8)
    ok=(x>=0)&(x<W)&(y>=0)&(y<W); o=o[ok[o]]
    for dx in (0,1):
        for dy in (0,1):
            xx=np.clip(x[o]+dx,0,W-1); yy=np.clip(y[o]+dy,0,W-1); img[yy,xx]=C[o]
    return Image.fromarray(img)
if __name__=='__main__':
    for name in sys.argv[1:]:
        js,b,P,UV,I,tex=mesh(f'src/{name}.glb')
        t=np.asarray(tex); h,w,_=t.shape
        C=t[np.clip((UV[:,1]%1*h).astype(int),0,h-1),np.clip((UV[:,0]%1*w).astype(int),0,w-1)]
        print(name,'tex',tex.size)
        a=splat(P,C,(0,1,2,1),700); bimg=splat(P,C,(0,2,1,1),700); c=splat(P,C,(2,1,0,1),350)
        sheet=Image.new('RGB',(1400+350,700),(20,20,20)); sheet.paste(a,(0,0)); sheet.paste(bimg,(700,0)); sheet.paste(c,(1400,0))
        sheet.save(f'views_{name}.png')
