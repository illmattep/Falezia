"""Texture stilizzate + ALTEZZA per ogni materiale.

L'altezza non si ricava dalla luminanza (le tegole dipinte hanno poco contrasto): si costruisce
insieme al disegno — cupola per tegola, giunto incassato per concio, solco per asse. Da lì
`pbr.py` ricava la normal vera.
"""
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

S = 1024
TEX = "/home/matteo/Progetti/Falezia/Assets/Resources/MapTex"

def new(color):
    a = np.zeros((S, S, 3), np.float32); a[:] = color; return a

def noise(freq, oct=3, seed=0):
    r = np.random.default_rng(seed); tot = np.zeros((S, S), np.float32); amp = 1.; f = freq
    for _ in range(oct):
        g = r.random((f, f)).astype(np.float32)
        xs = np.linspace(0, f, S, endpoint=False); ys = np.linspace(0, f, S, endpoint=False)
        x0 = xs.astype(int); y0 = ys.astype(int)
        x1 = (x0 + 1) % f; y1 = (y0 + 1) % f
        fx = (xs - x0)[None, :]; fy = (ys - y0)[:, None]
        a_ = g[y0][:, x0]; b_ = g[y0][:, x1]; c_ = g[y1][:, x0]; d_ = g[y1][:, x1]
        tot += ((a_ * (1 - fx) + b_ * fx) * (1 - fy) + (c_ * (1 - fx) + d_ * fx) * fy) * amp
        amp *= .5; f *= 2
    return tot / tot.max()

def save(a, name):
    Image.fromarray((np.clip(a, 0, 1) * 255).astype(np.uint8)).save(f"{TEX}/{name}.jpg", quality=90)

def save_h(h, name):
    Image.fromarray((np.clip(h, 0, 1) * 255).astype(np.uint8)).save(f"{TEX}/{name}_h.png")

def paint(base, blotch=0.065, grain=0.025, seed=1):
    a = new(base)
    a *= (1.0 + (noise(3, 3, seed) - .5) * 2 * blotch)[:, :, None]
    a *= (1.0 + (noise(40, 2, seed+1) - .5) * 2 * grain)[:, :, None]
    st = noise(4, 2, seed+2)
    st = np.repeat(st[:1, :], S, 0) * 0.55 + st * 0.45
    a *= (1.0 - (st - .5) * 0.18)[:, :, None]
    yv = np.linspace(0, 1, S)[:, None, None]
    a *= (0.88 + 0.12 * np.clip(yv * 3.0, 0, 1))
    # rilievo dell'intonaco: appena percettibile
    h = 0.5 + (noise(6, 3, seed+3) - .5) * 0.25 + (noise(60, 2, seed+4) - .5) * 0.10
    return a, h

# ---------- intonaci ----------
for name, col, seed in (("kit_plaster_a", (0.88, 0.72, 0.46), 1),
                        ("kit_plaster_b", (0.86, 0.62, 0.44), 2),
                        ("kit_plaster_c", (0.90, 0.84, 0.70), 3)):
    a, h = paint(col, seed=seed)
    save(a, name); save_h(h, name)

# ---------- pietra a conci ----------
a = new((0.66, 0.64, 0.60)); h = np.zeros((S, S), np.float32)
rng = np.random.default_rng(7)
bh, bw = 74, 132
for j in range(S//bh + 1):
    off = (bw//2) if j % 2 else 0
    for i in range(S//bw + 2):
        x0 = i*bw - off; y0 = j*bh
        tone = 0.62 + rng.uniform(-0.07, 0.07)
        a[y0:y0+bh-6, x0:x0+bw-6] = (tone*1.02, tone*0.99, tone*0.94)
        a[y0:y0+4, x0:x0+bw-6] *= 1.10
        a[y0+bh-10:y0+bh-6, x0:x0+bw-6] *= 0.88
        h[y0:y0+bh-6, x0:x0+bw-6] = 1.0                      # faccia del concio
        h[y0+bh-6:y0+bh, x0:x0+bw] = 0.0                     # giunto
        h[y0:y0+bh, x0+bw-6:x0+bw] = 0.0
save(a, "kit_pietra"); save_h(h, "kit_pietra")

# ---------- mattoni (per il danno) ----------
a = new((0.78, 0.59, 0.49)); h = np.zeros((S, S), np.float32)
rng = np.random.default_rng(12)
bh2, bw2 = 42, 96
for j in range(S//bh2 + 1):
    off = (bw2//2) if j % 2 else 0
    for i in range(S//bw2 + 2):
        x0 = i*bw2 - off; y0 = j*bh2
        t = rng.uniform(-0.06, 0.06)
        a[y0:y0+bh2-5, x0:x0+bw2-5] = (0.78+t, 0.59+t, 0.47+t)
        a[y0:y0+3, x0:x0+bw2-5] *= 1.10
        h[y0:y0+bh2-5, x0:x0+bw2-5] = 0.85 + t
        h[y0+bh2-5:y0+bh2, x0:x0+bw2] = 0.15
save(a, "damage_brick"); save_h(h, "damage_brick")

# ---------- coppi tondi ----------
img = Image.new("RGB", (S, S), (196, 96, 62)); d = ImageDraw.Draw(img)
h = np.zeros((S, S), np.float32)
rng = np.random.default_rng(5)
tw, th = 84, 104
for j in range(S//th + 2):
    off = (tw//2) if j % 2 else 0
    for i in range(S//tw + 2):
        x = i*tw - off; y = j*th
        tone = rng.integers(-14, 15)
        base = (196+tone, 96+tone//2, 62+tone//3)
        d.rounded_rectangle([x+2, y+2, x+tw-4, y+th-2], radius=(tw//2)-3, fill=base)
        d.arc([x+3, y+3, x+tw-5, y+th-3], 200, 340, fill=tuple(min(255, c+26) for c in base), width=7)
        d.line([x+2, y+th-4, x+tw-4, y+th-4], fill=tuple(int(c*0.72) for c in base), width=5)
        # altezza: cupola della tegola
        yy = np.arange(max(0,y), min(S, y+th))[:, None]
        xx = np.arange(max(0,x), min(S, x+tw))[None, :]
        u = (xx - (x + tw/2)) / (tw/2); v = (yy - (y + th/2)) / (th/2)
        dome = np.clip(1.0 - (u*u + v*v) * 0.9, 0, 1)
        h[yy, xx] = np.maximum(h[yy, xx], dome * 0.9 + 0.1)
img = img.filter(ImageFilter.GaussianBlur(0.8))
save(np.asarray(img, np.float32)/255.0, "kit_coppi"); save_h(h, "kit_coppi")

# ---------- legno ----------
a = new((0.62, 0.44, 0.28)); h = np.zeros((S, S), np.float32)
rng = np.random.default_rng(13)
pw = 128
for i in range(S//pw + 1):
    tone = rng.uniform(-0.05, 0.05)
    a[:, i*pw:i*pw+pw-4] = (0.64+tone, 0.46+tone*0.8, 0.30+tone*0.6)
    a[:, i*pw:i*pw+4] *= 0.78
    a[:, i*pw+pw-8:i*pw+pw-4] *= 1.10
    h[:, i*pw:i*pw+pw-4] = 0.9
    h[:, i*pw+pw-4:i*pw+pw] = 0.1
a *= (0.96 + 0.08*noise(120, 2, 15))[:, :, None]
save(a, "kit_legno"); save_h(h, "kit_legno")

# ---------- sampietrini ----------
a = new((0.52, 0.49, 0.45)); h = np.zeros((S, S), np.float32)
rng = np.random.default_rng(17)
cw, ch = 62, 52
for j in range(S//ch + 2):
    off = (cw//2) if j % 2 else 0
    for i in range(S//cw + 2):
        cx = i*cw + off + rng.integers(-4, 5); cy = j*ch + rng.integers(-4, 5)
        w = cw - rng.integers(8, 16); hh = ch - rng.integers(8, 14)
        tone = 0.60 + rng.uniform(-0.055, 0.055)
        y0, y1 = max(0, cy-hh//2), min(S, cy+hh//2)
        x0, x1 = max(0, cx-w//2), min(S, cx+w//2)
        if x1 > x0 and y1 > y0:
            a[y0:y1, x0:x1] = (tone*1.02, tone*1.00, tone*0.95)
            a[y0:y0+4, x0:x1] *= 1.12
            a[y1-5:y1, x0:x1] *= 0.86
            yy = np.arange(y0, y1)[:, None]; xx = np.arange(x0, x1)[None, :]
            u = (xx - (x0+x1)/2) / max(1, (x1-x0)/2); v = (yy - (y0+y1)/2) / max(1, (y1-y0)/2)
            dome = np.clip(1.0 - (u*u + v*v) * 0.85, 0, 1)
            h[y0:y1, x0:x1] = np.maximum(h[y0:y1, x0:x1], dome * 0.85 + 0.15)
a *= (0.95 + 0.10*noise(4, 3, 19))[:, :, None]
save(a, "kit_lastrico"); save_h(h, "kit_lastrico")

# ---------- lastre di piazza ----------
a = new((0.58, 0.56, 0.52)); h = np.zeros((S, S), np.float32)
rng = np.random.default_rng(23)
bw3, bh3 = 256, 170
for j in range(S//bh3 + 1):
    off = (bw3//2) if j % 2 else 0
    for i in range(S//bw3 + 2):
        x0 = i*bw3 - off; y0 = j*bh3
        t = rng.uniform(-0.05, 0.05)
        a[y0:y0+bh3-6, x0:x0+bw3-6] = (0.60+t, 0.58+t, 0.54+t)
        a[y0:y0+3, x0:x0+bw3-6] *= 1.08
        h[y0:y0+bh3-6, x0:x0+bw3-6] = 0.95
        h[y0+bh3-6:y0+bh3, :] = 0.1
save(a, "kit_piazza"); save_h(h, "kit_piazza")
print("texture + altezze rigenerate")
