"""Reveal art, part 2: the lower-legs sprite, the cast shadow derived from it, the fallen shoe, and the close-up still.
Imports the figure drawing from gen_reveal.py.   Run:  python gen_reveal2.py   (then make_previews.py)"""
import math
import random

from PIL import Image, ImageFilter

from gen_reveal import BAYER, OUT, PREV, clamp, draw_figure, poly_mask, shade_mask, smooth

LEG_SCALE = 2.4          # figure units -> pixels for the in-scene sprite (the still uses 3.2)
TOP_UNITS = 19.0         # the sprite starts just above the knee (the knee is at 26 units)


# ------------------------------------------------------------------ lower legs: the in-scene sprite
def make_legs(s=LEG_SCALE, light=-1):
    """Both lower legs, from just above the knees down. One shoe on, one socked foot, toes down, slightly asymmetric.
    The top edge fades out (to black in the dark). No rope, no skirt, nothing above the knees."""
    w = 72
    h = int((60.5 - TOP_UNITS) * s) + 6
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    draw_figure(img, w // 2, -TOP_UNITS * s, s, light=light, skirt=False, arms=False, rope=False, asym=True)
    fade = (26.4 - TOP_UNITS) * s + 4          # fully visible a little below the knee
    px = img.load()
    for y in range(h):
        f = smooth(y / fade)
        for x in range(w):
            r, g, b, a = px[x, y]
            if a:
                px[x, y] = (r, g, b, int(a * f))
    return img


# ------------------------------------------------------------------ the fallen shoe, hand-pixelled (22 x 11)
SHOE_PALETTE = {
    "d": (22, 18, 24), "s": (58, 48, 52), "m": (84, 70, 70), "h": (150, 132, 128),
    "o": (10, 8, 12), "b": (136, 102, 78), "c": (96, 70, 56),
}
SHOE_ROWS = [
    "......dddd............",
    "....ddhhhmdd..........",
    "...dmmmooodmdd........",
    "..dmmsoooooosmdd......",
    "..dmsssoooosssmmddd...",
    "..dmssssssssssssmmdd..",
    "..dmsssssssssssssmmhd.",
    "..dmssssssssssssssssd.",
    "..ddssssssssssssssssd.",
    "..cbbbbbbbbbbbbbbbbbb.",
    "..cccbbbbbbbbbbbbbbc..",
]


def make_fallen_shoe():
    img = Image.new("RGBA", (22, 11), (0, 0, 0, 0))
    for y, row in enumerate(SHOE_ROWS):
        for x, ch in enumerate(row):
            if ch in SHOE_PALETTE:
                img.putpixel((x, y), SHOE_PALETTE[ch] + (255,))
    return img


# ------------------------------------------------------------------ cast shadow, derived from the legs sprite
def make_cast_shadow(legs, stretch_x=1.45, stretch_y=1.3, shear=0.05, blur=1.3, strength=0.46):
    """Black, semi-transparent, stretched copy of the legs' silhouette with slightly blurred edges, for the wall behind.
    It keeps the legs' own top fade. Light comes from the left, so it leans a little to the right."""
    alpha = legs.split()[3]
    w, h = round(legs.width * stretch_x), round(legs.height * stretch_y)
    big = alpha.resize((w, h), Image.NEAREST)
    pad = 8
    canvas = Image.new("L", (w + int(h * shear) + pad * 2, h + pad * 2), 0)
    canvas.paste(big, (pad, pad))
    canvas = canvas.transform(canvas.size, Image.AFFINE, (1, -shear, 0, 0, 1, 0), Image.NEAREST)
    soft = canvas.filter(ImageFilter.GaussianBlur(blur))
    out = Image.new("RGBA", canvas.size, (0, 0, 0, 0))
    op, sp = out.load(), soft.load()
    for y in range(canvas.height):
        for x in range(canvas.width):
            a = sp[x, y] / 255 * strength
            q = round(a * 12) / 12                    # many alpha steps: soft edges and a smooth top fade, still crisp pixels
            if q > 0.02:
                op[x, y] = (0, 0, 0, int(255 * q))
    return out


# ------------------------------------------------------------------ the close-up still (320 x 180)
def box3(layer, x0, y0, x1, y1, dx, dy, base, dark, lit, light=-1):
    """A chair part drawn as a small box: front face, a lit top face and a dark side face."""
    size = layer.size
    top = [(x0, y0), (x1, y0), (x1 + dx, y0 - dy), (x0 + dx, y0 - dy)]
    side = [(x1, y0), (x1, y1), (x1 + dx, y1 - dy), (x1 + dx, y0 - dy)]
    shade_mask(layer, poly_mask(size, side), dark, dark, base, light, steps=2, amb=0.0)
    shade_mask(layer, poly_mask(size, top), lit, base, lit, light, steps=2, amb=0.9)
    shade_mask(layer, poly_mask(size, [(x0, y0), (x1, y0), (x1, y1), (x0, y1)]), base, dark, lit, light, steps=5, amb=0.14)


def make_still(seed=4):
    rnd = random.Random(seed)
    W, H = 320, 180
    FLOOR_Y = 140
    img = Image.new("RGBA", (W, H), (0, 0, 0, 255))
    px = img.load()

    # the player's beam comes in from the left edge, aimed a little downward
    sx, sy, beam_angle, half_angle, reach = -30.0, 92.0, 0.06, 0.5, 400.0

    def light_at(x, y):
        dx, dy = x - sx, y - sy
        dist = math.hypot(dx, dy)
        ang = abs(math.atan2(dy, dx) - beam_angle)
        cone = smooth(1 - ang / half_angle)
        fall = clamp(1 - dist / reach, 0, 1) ** 1.15
        core = math.exp(-(((x - 190) / 110.0) ** 2 + ((y - 80) / 62.0) ** 2)) * 0.34
        return clamp(cone * fall * 1.25 + core, 0, 1.25)

    def wall_color(x, y):
        c = (100, 110, 138)
        seam = x % 80
        if seam < 2:
            c = tuple(int(v * 0.62) for v in c)
        elif seam < 4:
            c = tuple(int(v * 1.08) for v in c)
        if 126 <= y < FLOOR_Y:
            c = (126, 104, 98)
            if y == 126:
                c = (62, 50, 52)
        g = 0.82 + 0.18 * (y / FLOOR_Y)
        return tuple(int(v * g) for v in c)

    def floor_color(x, y):
        c = (108, 84, 66)
        row = (y - FLOOR_Y) // 6
        if (y - FLOOR_Y) % 6 == 0:
            c = tuple(int(v * 0.7) for v in c)
        if (x + row * 37) % 71 < 2:
            c = tuple(int(v * 0.74) for v in c)
        return c

    # her lower legs, knees at the top of the frame (same drawing as the in-scene sprite, just bigger)
    legs = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    s = 3.2
    ox, oy = 186, 3 - 26 * s
    draw_figure(legs, ox, oy, s, light=-1, skirt=False, arms=False, rope=False, asym=True)

    # her cast shadow on the wall: lighter and narrower than before, with soft edges, thrown a little to the right
    sil = legs.split()[3].point(lambda v: 255 if v > 90 else 0)
    kx, ky = 1.1, 1.3
    big = sil.resize((round(W * kx), round(H * ky)), Image.NEAREST)
    shadow = Image.new("L", (W, H), 0)
    shadow.paste(big.crop((int(ox * kx - ox) - 34, 0, int(ox * kx - ox) - 34 + W, H)), (0, 0))
    shadow = shadow.filter(ImageFilter.GaussianBlur(1.6))

    for y in range(H):
        for x in range(W):
            base = wall_color(x, y) if y < FLOOR_Y else floor_color(x, y)
            lum = light_at(x, y)
            kk = 0.07 + 1.0 * lum
            warm = (1.0, 0.93 - 0.05 * (1 - lum), 0.76 - 0.1 * (1 - lum))
            col = [base[i] * kk * warm[i] for i in range(3)]
            if y < FLOOR_Y:
                m = shadow.getpixel((x, y)) / 255.0
                if m > 0:
                    f = 1.0 - 0.52 * m
                    col = [c * f for c in col]
            px[x, y] = tuple(int(clamp(c, 0, 255)) for c in col) + (255,)

    # the tipped chair, a school chair lying on its side: backrest (vertical slats) on the left, the seat standing on its
    # edge in the middle, four legs pointing right with a stretcher. Backrest and seat share the same height.
    chair = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    wood, wood_dark, wood_lit = (116, 64, 62), (46, 26, 32), (186, 120, 104)
    far, far_dark, far_lit = (84, 46, 48), (34, 20, 26), (138, 88, 80)
    top, bot = FLOOR_Y - 34, FLOOR_Y - 3                     # the chair's width runs up and down now
    # far legs and stretcher (behind)
    box3(chair, 74, top + 3, 112, top + 8, 5, 4, far, far_dark, far_lit)
    box3(chair, 74, bot - 8, 108, bot - 3, 5, 4, far, far_dark, far_lit)
    # seat, standing on its edge
    box3(chair, 66, top, 74, bot, 6, 5, wood, wood_dark, wood_lit)
    # backrest board, its face towards us, with vertical slats
    box3(chair, 36, top + 1, 66, bot - 1, 4, 4, wood, wood_dark, wood_lit)
    for sx_ in (42, 49, 56):
        for yy in range(top + 4, bot - 3):
            chair.putpixel((sx_, yy), wood_dark + (255,))
            chair.putpixel((sx_ + 1, yy), wood_dark + (255,))
    # near legs and the front stretcher
    box3(chair, 74, top, 116, top + 5, 5, 4, wood, wood_dark, wood_lit)
    box3(chair, 74, bot - 5, 112, bot, 5, 4, wood, wood_dark, wood_lit)
    box3(chair, 104, top + 5, 109, bot - 5, 4, 3, wood, wood_dark, wood_lit)

    # the fallen shoe, upscaled from the hand-pixelled sprite so it matches the still's pixel size
    shoe_big = make_fallen_shoe().resize((round(22 * 2.1), round(11 * 2.1)), Image.BICUBIC)
    shoe = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    shoe.alpha_composite(shoe_big, (140, FLOOR_Y - shoe_big.height + 4))

    def blend(layer, strength_fn):
        lp = layer.load()
        for y in range(H):
            for x in range(W):
                r, g, b, a = lp[x, y]
                if a > 128:
                    f = strength_fn(x, y)
                    px[x, y] = (int(clamp(r * f, 0, 255)), int(clamp(g * f * 0.97, 0, 255)), int(clamp(b * f * 0.9, 0, 255)), 255)

    blend(chair, lambda x, y: 0.14 + 0.95 * light_at(x, y))
    blend(shoe, lambda x, y: 0.14 + 0.95 * light_at(x, y))
    blend(legs, lambda x, y: 0.14 + 0.95 * light_at(x, y))

    # dust in the beam, then a vignette so the room falls into darkness
    for _ in range(46):
        x, y = rnd.randint(30, 260), rnd.randint(30, 130)
        if light_at(x, y) > 0.35:
            r, g, b, _a = px[x, y]
            px[x, y] = (min(255, r + 70), min(255, g + 66), min(255, b + 52), 255)
    for y in range(H):
        for x in range(W):
            nx, ny = (x - W / 2) / (W / 2), (y - H / 2) / (H / 2)
            v = clamp(1.14 - 0.55 * (nx * nx * 0.9 + ny * ny * 0.7), 0.28, 1)
            r, g, b, _a = px[x, y]
            px[x, y] = (int(r * v), int(g * v), int(b * v), 255)

    # limited palette with an ordered dither so it reads as pixel art
    levels = 16
    for y in range(H):
        for x in range(W):
            r, g, b, _a = px[x, y]
            t = (BAYER[y % 4][x % 4] + 0.5) / 16
            out = []
            for v in (r, g, b):
                q = v / 255 * (levels - 1)
                lo = int(q)
                if q - lo > t:
                    lo += 1
                out.append(int(clamp(lo / (levels - 1), 0, 1) * 255))
            px[x, y] = tuple(out) + (255,)
    return img


if __name__ == "__main__":
    legs = make_legs()
    print("legs", legs.size)
