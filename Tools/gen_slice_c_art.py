"""Generates the small pixel-art sprites for slice C. Run from the project root:  python gen_art.py
Everything is drawn at the school art scale (48 px per world unit) with hard pixels, no anti-aliasing."""
import random
import sys
from pathlib import Path

from PIL import Image

root = Path(sys.argv[1]) if len(sys.argv) > 1 else Path(".")
school = root / "Assets/_Remnant/Art/Environment/School"
chars = root / "Assets/_Remnant/Art/Characters"
school.mkdir(parents=True, exist_ok=True)
chars.mkdir(parents=True, exist_ok=True)

CHALK = (236, 236, 226)


def put(img, x, y, color, alpha=255):
    if 0 <= x < img.width and 0 <= y < img.height:
        img.putpixel((x, y), (*color[:3], alpha))


def line(img, x0, y0, x1, y1, color, alpha=255):
    dx, dy = abs(x1 - x0), -abs(y1 - y0)
    sx, sy = (1 if x0 < x1 else -1), (1 if y0 < y1 else -1)
    err = dx + dy
    while True:
        put(img, x0, y0, color, alpha)
        if x0 == x1 and y0 == y1:
            break
        e2 = 2 * err
        if e2 >= dy:
            err += dy
            x0 += sx
        if e2 <= dx:
            err += dx
            y0 += sy


# ---------------------------------------------------------------- chalk hangman (3 stages)
GLYPHS = {
    "A": ["01110", "10001", "11111", "10001", "10001"],
    "I": ["11111", "00100", "00100", "00100", "11111"],
    "K": ["10001", "10010", "11100", "10010", "10001"],
    "O": ["01110", "10001", "10001", "10001", "01110"],
}


def hangman(stage):
    img = Image.new("RGBA", (44, 48), (0, 0, 0, 0))
    rng = random.Random(100 + stage)

    # gallows
    line(img, 3, 33, 21, 33, CHALK)          # base
    line(img, 7, 4, 7, 33, CHALK)            # post
    line(img, 7, 4, 25, 4, CHALK)            # beam
    line(img, 7, 11, 14, 4, CHALK)           # brace
    line(img, 25, 4, 25, 8, CHALK)           # rope

    # head: small ring (stage 1+)
    for (x, y) in [(24, 9), (25, 9), (26, 9), (23, 10), (27, 10), (23, 11), (27, 11), (23, 12), (27, 12),
                   (24, 13), (25, 13), (26, 13)]:
        put(img, x, y, CHALK)

    if stage >= 2:
        line(img, 25, 14, 25, 24, CHALK)     # body
        line(img, 25, 17, 20, 21, CHALK)     # arms
        line(img, 25, 17, 30, 21, CHALK)
    if stage >= 3:
        line(img, 25, 24, 21, 30, CHALK)     # legs
        line(img, 25, 24, 29, 30, CHALK)

    # four letter dashes
    for i in range(4):
        x = 6 + i * 9
        line(img, x, 45, x + 6, 45, CHALK)

    # chalk texture: drop a few pixels, soften others
    for y in range(img.height):
        for x in range(img.width):
            r, g, b, a = img.getpixel((x, y))
            if a:
                roll = rng.random()
                if roll < 0.09:
                    img.putpixel((x, y), (r, g, b, 0))
                elif roll < 0.35:
                    img.putpixel((x, y), (r, g, b, 190))
    # the word, written in only at the end (no dropout, so it stays readable)
    if stage >= 3:
        for i, ch in enumerate("AIKO"):
            for gy, row in enumerate(GLYPHS[ch]):
                for gx, bit in enumerate(row):
                    if bit == "1":
                        put(img, 6 + i * 9 + 1 + gx, 39 + gy, CHALK)
    return img


for stage in (1, 2, 3):
    hangman(stage).save(school / f"chalk_hangman_{stage}.png")

# ---------------------------------------------------------------- diary page (a small loose sheet)
page = Image.new("RGBA", (16, 20), (0, 0, 0, 0))
PAPER, EDGE, RULE, INK = (230, 222, 198), (176, 164, 136), (202, 192, 166), (92, 84, 78)
for y in range(1, 19):
    for x in range(1, 15):
        page.putpixel((x, y), (*PAPER, 255))
for x in range(1, 15):
    page.putpixel((x, 1), (*EDGE, 255))
    page.putpixel((x, 18), (*EDGE, 255))
for y in range(1, 19):
    page.putpixel((1, y), (*EDGE, 255))
    page.putpixel((14, y), (*EDGE, 255))
for y in (6, 9, 12, 15):
    for x in range(3, 13):
        page.putpixel((x, y), (*RULE, 255))
rng = random.Random(7)
for y in (5, 8, 11):
    for x in range(3, 3 + rng.randint(5, 9)):
        if rng.random() < 0.7:
            page.putpixel((x, y), (*INK, 255))
page.putpixel((14, 1), (0, 0, 0, 0))     # folded corner
page.putpixel((13, 1), (0, 0, 0, 0))
page.putpixel((14, 2), (0, 0, 0, 0))
page.putpixel((13, 2), (*EDGE, 255))
page.save(school / "diary_page.png")

# ---------------------------------------------------------------- cracked mirror
W, H = 36, 58
mirror = Image.new("RGBA", (W, H), (0, 0, 0, 0))
FRAME, FRAME_DARK = (84, 62, 54), (52, 38, 34)
for y in range(H):
    for x in range(W):
        edge = x < 3 or x >= W - 3 or y < 3 or y >= H - 3
        if edge:
            mirror.putpixel((x, y), (*(FRAME if (x + y) % 7 else FRAME_DARK), 255))
        else:
            shade = int(58 + 22 * (y / H) + (6 if (x // 4 + y // 6) % 2 else 0))
            mirror.putpixel((x, y), (shade - 6, shade, shade + 14, 255))
CRACK = (190, 200, 210)
cx, cy = 21, 20
for (x1, y1) in [(5, 6), (30, 4), (31, 26), (28, 52), (7, 44), (4, 24), (14, 54)]:
    # jagged line from the impact point towards the target
    x, y = cx, cy
    steps = max(abs(x1 - x), abs(y1 - y))
    for i in range(steps):
        t = (i + 1) / steps
        x = round(cx + (x1 - cx) * t + (1 if i % 5 == 2 else 0))
        y = round(cy + (y1 - cy) * t + (-1 if i % 7 == 3 else 0))
        if 3 <= x < W - 3 and 3 <= y < H - 3:
            mirror.putpixel((x, y), (*CRACK, 255))
for (x, y) in [(cx, cy), (cx - 1, cy), (cx + 1, cy), (cx, cy - 1), (cx, cy + 1)]:
    mirror.putpixel((x, y), (225, 232, 238, 255))
mirror.save(school / "mirror_cracked.png")

# ---------------------------------------------------------------- the hanging figure (rope, skirt hem, legs, shoes)
# Restrained on purpose: nothing above the skirt hem is drawn. The top of the skirt fades into the dark.
W, H = 20, 104
fig = Image.new("RGBA", (W, H), (0, 0, 0, 0))
ROPE, ROPE_DARK = (112, 98, 78), (78, 68, 56)
SKIRT, SKIRT_PLEAT, HEM = (38, 36, 56), (54, 52, 76), (92, 90, 112)
SKIN, SOCK, SHOE = (168, 138, 122), (196, 190, 170), (58, 50, 52)
cxm = W // 2

# rope, coming down out of the darkness
for y in range(0, 50):
    alpha = int(255 * min(1.0, 0.25 + y / 14.0))
    fig.putpixel((cxm - 1, y), (*ROPE, alpha))
    fig.putpixel((cxm, y), (*ROPE_DARK, alpha))

# skirt: only the lower part. Its top edge fades out.
for y in range(40, 60):
    half = 5 + (y - 40) // 4
    fade = min(1.0, (y - 40) / 7.0)
    for x in range(cxm - half, cxm + half):
        pleat = (x - (cxm - half)) % 3 == 0
        color = SKIRT_PLEAT if pleat else SKIRT
        fig.putpixel((x, y), (*color, int(255 * fade)))
for x in range(cxm - 8, cxm + 8):
    fig.putpixel((x, 59), (*HEM, 255))

# legs, a little apart, one a touch shorter as if swaying
for y in range(60, 84):
    for x in (cxm - 4, cxm - 3):
        fig.putpixel((x, y), (*SKIN, 255))
for y in range(60, 82):
    for x in (cxm + 2, cxm + 3):
        fig.putpixel((x, y), (*SKIN, 255))
# socks
for y in range(80, 90):
    for x in (cxm - 4, cxm - 3):
        fig.putpixel((x, y), (*SOCK, 255))
for y in range(78, 88):
    for x in (cxm + 2, cxm + 3):
        fig.putpixel((x, y), (*SOCK, 255))
# school shoes, toes pointing down
for y in range(90, 97):
    for x in (cxm - 5, cxm - 4, cxm - 3):
        fig.putpixel((x, y), (*SHOE, 255))
for y in range(88, 95):
    for x in (cxm + 1, cxm + 2, cxm + 3):
        fig.putpixel((x, y), (*SHOE, 255))
fig.save(chars / "hanging_figure.png")
print("generated")
