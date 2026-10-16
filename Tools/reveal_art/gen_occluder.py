"""The darkness occluder: pure black, soft on its sides and bottom, solid above. Drawn unlit in front of the hanging legs so
nothing above the knees can ever be lit by the flashlight. 64 x 192 px, meant to be shown 3.4 x 4.8 world units
(about 19 px per unit across, 40 px per unit tall) with bilinear filtering. Run:  python gen_occluder.py"""
from pathlib import Path

from PIL import Image

OUT = Path(r"D:/Unity/RemnantArt/_work/reveal")
W, H = 64, 192
FEATHER_X = 14          # px of soft edge on each side (about 0.7 units)
FEATHER_Y = 13          # px of soft edge at the bottom (about 0.32 units)


def smooth(t):
    t = max(0.0, min(1.0, t))
    return t * t * (3 - 2 * t)


img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
px = img.load()
for y in range(H):
    # y = 0 is the top of the image; the soft bottom edge is at y = H - 1
    ay = smooth((H - 1 - y) / FEATHER_Y) if False else smooth(((H - 1) - y) / FEATHER_Y)
    ay = 1.0 - ay if False else ay
    for x in range(W):
        edge = min(x, W - 1 - x)
        ax = smooth(edge / FEATHER_X)
        # the bottom edge fades OUT towards the bottom, so alpha grows with the distance above it
        dist_from_bottom = (H - 1) - y
        a_bottom = smooth(dist_from_bottom / FEATHER_Y)
        px[x, y] = (0, 0, 0, int(255 * ax * a_bottom))
img.save(OUT / "occluder_dark.png")
print("occluder", img.size)
