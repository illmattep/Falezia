import sys, numpy as np
from PIL import Image, ImageDraw
from views import mesh
name=sys.argv[1]
js,b,P,UV,I,tex=mesh(f'src/{name}.glb')
t=np.asarray(tex); h,w,_=t.shape
C=t[np.clip((UV[:,1]%1*h).astype(int),0,h-1),np.clip((UV[:,0]%1*w).astype(int),0,w-1)].astype(np.float32)
C=np.clip(C*1.8+25,0,255).astype(np.uint8)
W=1400; s=W/2.0; H=int((P[:,1].max()-P[:,1].min()+0.1)*s)
x0=-1.0; y1=P[:,1].max()+0.05
x=((P[:,0]-x0)*s).astype(int); y=((y1-P[:,1])*s).astype(int)
o=np.argsort(P[:,2])  # +z toward camera? view from +Z side
img=np.full((H,W,3),70,np.uint8)
for dx in (0,1):
  for dy in (0,1):
    img[np.clip(y[o]+dy,0,H-1),np.clip(x[o]+dx,0,W-1)]=C[o]
im=Image.fromarray(img); d=ImageDraw.Draw(im)
for g in np.arange(-1.0,1.01,0.05):
    X=int((g-x0)*s); d.line([(X,0),(X,H)],fill=(255,80,80) if abs(g*10-round(g*10))<1e-6 else (120,60,60),width=1)
    if abs(g*10-round(g*10))<1e-6: d.text((X+2,2),f'{g:.1f}',fill=(255,200,200))
for g in np.arange(-1.0,1.01,0.05):
    Y=int((y1-g)*s)
    if 0<=Y<H:
        d.line([(0,Y),(W,Y)],fill=(80,160,255) if abs(g*10-round(g*10))<1e-6 else (50,80,120),width=1)
        if abs(g*10-round(g*10))<1e-6: d.text((2,Y+2),f'{g:.1f}',fill=(200,220,255))
im.save(f'grid_{name}.png')
print(name, 'zrange', P[:,2].min(), P[:,2].max(), 'H',H)
