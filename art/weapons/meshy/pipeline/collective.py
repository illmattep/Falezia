"""Foglio collettivo del doc 12: tutte le silhouette in nero pieno, alla stessa scala."""
import sys
from PIL import Image, ImageDraw, ImageFont
ids = sys.argv[1:]
font = ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf', 20)
PX_PER_M = 900   # scala comune: 0,9 px per mm
cells = []
for w in ids:
    o = float(open(f'review/{w}_ortho.txt').read().strip())
    im = Image.open(f'review/{w}_silhouette.png').convert('L')
    k = PX_PER_M / (1000 / o)
    im = im.resize((max(1, int(im.width * k)), max(1, int(im.height * k))), Image.LANCZOS)
    bbox = Image.eval(im, lambda v: 255 - v).getbbox()
    cells.append((w, im.crop(bbox)))
W = max(c.width for _, c in cells) + 60
H = sum(c.height + 50 for _, c in cells) + 40
sheet = Image.new('RGB', (W, H), 'white'); d = ImageDraw.Draw(sheet); y = 20
for w, c in cells:
    d.text((20, y), w.upper(), fill=(120, 120, 120), font=font); y += 28
    sheet.paste(Image.merge('RGB', (c, c, c)), (20, y)); y += c.height + 22
d.rectangle([W - 20 - int(0.1 * PX_PER_M), H - 30, W - 20, H - 22], fill='black')
d.text((W - 20 - int(0.1 * PX_PER_M), H - 22 - 26), '100 mm', fill='black', font=font)
sheet.save('review/foglio_collettivo_silhouette.png'); print(W, H)
