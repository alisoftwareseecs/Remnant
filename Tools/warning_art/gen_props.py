"""Props for the content-warning screen: the felt eraser sprite used for the wipe (28x14, shown 8x with point filtering)
and a chalk grain texture (tileable speckle) that roughens the handwritten text. Run:  python gen_props.py"""
import random
from pathlib import Path
from PIL import Image, ImageDraw

OUT = Path(r"D:/Unity/Remnant/Assets/_Remnant/Art/Warning")
r = random.Random(5)

# ---- eraser: wooden back on top, felt pad below, dusted with chalk
e = Image.new("RGBA", (28, 15), (0, 0, 0, 0))
d = ImageDraw.Draw(e)
OUTL = (22, 16, 14, 255)
d.rectangle([0, 0, 27, 14], fill=OUTL)
d.rectangle([1, 1, 26, 8], fill=(150, 108, 74, 255))          # wood back (top face)
d.line([(1, 1), (26, 1)], fill=(190, 146, 104, 255))
d.line([(1, 8), (26, 8)], fill=(104, 72, 50, 255))
for y in (3, 5, 7):
    d.line([(r.randint(1, 8), y), (r.randint(14, 26), y)], fill=(126, 90, 62, 255))
d.rectangle([1, 9, 26, 13], fill=(92, 96, 90, 255))           # felt
d.line([(1, 9), (26, 9)], fill=(60, 62, 58, 255))
d.line([(1, 13), (26, 13)], fill=(168, 172, 164, 255))        # chalky lower edge
for _ in range(34):
    d.point((r.randint(1, 26), r.randint(9, 13)), fill=(212, 216, 206, 255))
d.point((0, 0), fill=(0, 0, 0, 0)); d.point((27, 0), fill=(0, 0, 0, 0))
d.point((0, 14), fill=(0, 0, 0, 0)); d.point((27, 14), fill=(0, 0, 0, 0))
e.save(OUT / "chalk_eraser.png")

# ---- chalk grain: light grey-white with scattered darker pits; multiplies the text colour
g = Image.new("RGB", (128, 128), (255, 255, 255))
px = g.load()
for y in range(128):
    for x in range(128):
        v = 255
        t = r.random()
        if t < 0.22:
            v = r.randint(120, 200)
        elif t < 0.5:
            v = r.randint(205, 245)
        px[x, y] = (v, v, v)
g.save(OUT / "chalk_grain.png")
print("ok")

# ---- eraser smear: a soft, streaky band of chalk dust left where the eraser has passed (stretched to fit a line of text)
import numpy as np
from PIL import ImageFilter
W, H = 512, 128
rs = np.random.default_rng(11)
yy = np.linspace(-1, 1, H)[:, None]
profile = np.clip(1 - np.abs(yy) ** 2.2, 0, 1)
# ragged top/bottom edges
edge = rs.random((1, W))
edge = np.asarray(Image.fromarray((edge * 255).astype(np.uint8)).resize((W, 1)).filter(ImageFilter.GaussianBlur(3)), dtype=float) / 255.0
profile = np.clip(profile + (edge - 0.5) * 0.5, 0, 1)
# long horizontal streaks
streak = rs.random((H, 1))
streak = np.repeat(streak, W, axis=1)
streak = np.asarray(Image.fromarray((streak * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(1.2)), dtype=float) / 255.0
blot = np.asarray(Image.fromarray((rs.random((14, 52)) * 255).astype(np.uint8)).resize((W, H), Image.BICUBIC), dtype=float) / 255.0
# patchy: dust only where the blot noise is high, streaky where it is present
gate = np.clip((blot - 0.3) * 1.5, 0, 1) ** 1.5
alpha = profile * (0.25 + 0.55 * streak) * gate
alpha = np.clip(alpha, 0, 1)
sm = np.zeros((H, W, 4), dtype=np.uint8)
sm[..., 0], sm[..., 1], sm[..., 2] = 214, 224, 214
sm[..., 3] = (alpha * 255).astype(np.uint8)
Image.fromarray(sm, "RGBA").save(OUT / "chalk_smear.png")
print("smear ok")
