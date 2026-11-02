import sys, json
from PIL import Image, ImageDraw, ImageFilter, ImageFont
wid=sys.argv[1]; L=float(sys.argv[2])
R=f'review/{wid}'
def ld(n): return Image.open(f'{R}_{n}.png').convert('RGB')
try: font=ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf',22); small=ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf',16)
except: font=small=ImageFont.load_default()
W,H=1600,1000
prof=ld('1_profilo_sx'); d=ImageDraw.Draw(prof)
# barra 100 mm: ortografica, 1600 px = L/0.85 m
ortho=float(open(f'{R}_ortho.txt').read().strip()); px_per_m=1000/ortho; bar=int(0.1*px_per_m)
d.rectangle([60,920,60+bar,934],fill=(235,235,235)); d.text((60,940),'100 mm',fill=(235,235,235),font=font)
sil=ld('silhouette'); blur=sil.filter(ImageFilter.GaussianBlur(8))
cells=[('1 · profilo laterale sinistro (ortografico)',prof),('2 · tre quarti anteriore sinistro',ld('2_tre_quarti_ant_sx')),
       ('3 · tre quarti posteriore destro',ld('3_tre_quarti_post_dx')),('4 · dettaglio culatta',ld('4_culatta')),
       ('pezzi mossi (carrello/caricatore/pompa)',ld('5_pezzi_mossi')),('prima persona (posizione da fianco)',ld('6_prima_persona')),
       ('test silhouette: nero pieno',sil),('test silhouette: sfocatura 8 px',blur)]
cw,ch=800,500
sheet=Image.new('RGB',(cw*2,ch*4+70),(30,30,30)); sd=ImageDraw.Draw(sheet)
rep=json.load(open(f'tmp/{wid}_report.json'))
sd.text((16,14),f"{wid.upper()} · versione Meshy · {rep['size_mm'][0]:.0f}×{rep['size_mm'][1]:.0f}×{rep['size_mm'][2]:.0f} mm (doc 12: {rep['doc_mm'][0]}×{rep['doc_mm'][1]}×{rep['doc_mm'][2]})",fill=(240,240,240),font=font)
for i,(t,im) in enumerate(cells):
    x=(i%2)*cw; y=70+(i//2)*ch
    sheet.paste(im.resize((cw,ch),Image.LANCZOS),(x,y))
    sd.rectangle([x,y,x+cw-1,y+26],fill=(0,0,0)); sd.text((x+8,y+4),t,fill=(230,230,230),font=small)
sheet.save(f'review/{wid}_tavola.png'); print('ok')
