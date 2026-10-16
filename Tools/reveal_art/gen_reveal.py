"""Reveal art for the storage room: the hanging figure (waist down), her cast shadow, the fallen shoe, and the
full-screen close-up still. Hard pixels only. Run:  python gen_reveal.py
Outputs go to D:/Unity/RemnantArt/_work/reveal (sources) and D:/Unity/RemnantArt/_previews (4x previews).

Restraint rules: nothing above the waist (it fades into darkness), never a face, no gore."""
import math
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter

OUT = Path(r"D:/Unity/RemnantArt/_work/reveal")
PREV = Path(r"D:/Unity/RemnantArt/_previews")
OUT.mkdir(parents=True, exist_ok=True)
PREV.mkdir(parents=True, exist_ok=True)

BAYER = [[0, 8, 2, 10], [12, 4, 14, 6], [3, 11, 1, 9], [15, 7, 13, 5]]


def clamp(v, a, b):
    return max(a, min(b, v))


def lerp(a, b, t):
    return a + (b - a) * t


def mix(c1, c2, t):
    return tuple(int(round(lerp(c1[i], c2[i], t))) for i in range(3))


def smooth(t):
    t = clamp(t, 0, 1)
    return t * t * (3 - 2 * t)


# ------------------------------------------------------------------ palette (dark, close to Aiko's uniform)
SKIN, SKIN_DARK, SKIN_LIT = (186, 148, 124), (92, 66, 62), (226, 190, 160)
SOCK, SOCK_DARK, SOCK_LIT = (206, 200, 176), (104, 98, 98), (240, 234, 212)
SHOE, SHOE_DARK, SHOE_LIT = (58, 48, 52), (24, 20, 26), (128, 112, 110)
SOLE = (132, 100, 76)
SKIRT, SKIRT_DARK, SKIRT_LIT = (54, 44, 66), (24, 20, 32), (116, 96, 128)
SLEEVE, SLEEVE_DARK, SLEEVE_LIT = (68, 58, 82), (30, 26, 40), (124, 108, 140)
CUFF, CUFF_DARK, CUFF_LIT = (206, 202, 192), (96, 92, 100), (238, 234, 224)
ROPE, ROPE_DARK, ROPE_LIT = (126, 108, 84), (70, 58, 48), (178, 156, 124)


# ------------------------------------------------------------------ shading helpers
SS_DEFAULT = [1]


def poly_mask(size, pts, ss=None):
    """Polygon mask, drawn 4x and averaged down so contours come out smooth instead of stair-stepped."""
    if ss is None:
        ss = SS_DEFAULT[0]
    if ss == 1:
        m = Image.new("L", size, 0)
        ImageDraw.Draw(m).polygon([(round(x), round(y)) for x, y in pts], fill=255)
        return m
    big = Image.new("L", (size[0] * ss, size[1] * ss), 0)
    ImageDraw.Draw(big).polygon([((x + 0.5) * ss, (y + 0.5) * ss) for x, y in pts], fill=255)
    small = big.resize(size, Image.BOX)
    return small.point(lambda v: 255 if v >= 128 else 0)


def shade_mask(img, mask, base, dark, lit, light=1, steps=5, amb=0.16, pleat=None, alpha_fn=None, dither=False, pleat_weight=0.28):
    """Cylinder-like shading across each row of the mask, light coming from the right (light=1) or left (-1).
    Quantised with an ordered dither so it stays pixel art."""
    w, h = mask.size
    mp, op = mask.load(), img.load()
    for y in range(h):
        xs = [x for x in range(w) if mp[x, y] > 127]
        if not xs:
            continue
        x0, x1 = min(xs), max(xs)
        mid, half = (x0 + x1) / 2, max((x1 - x0) / 2 + 0.6, 0.9)
        for x in xs:
            u = clamp((x - mid) / half * light, -1, 1)
            nz = math.sqrt(1 - u * u)
            lam = max(0.0, 0.80 * u + 0.60 * nz)
            rim = max(0.0, (u - 0.70) / 0.30)
            t = amb + 0.70 * lam + 0.22 * rim
            if pleat is not None:
                t = t * (1 - pleat_weight) + pleat_weight * pleat(x, y)
            t = clamp(t, 0, 1) * (steps - 1)
            lo = int(t)
            if dither and t - lo > (BAYER[y % 4][x % 4] + 0.5) / 16:
                lo += 1
            elif not dither and t - lo > 0.5:
                lo += 1
            t = clamp(lo / (steps - 1), 0, 1)
            c = mix(dark, base, t * 2) if t < 0.5 else mix(base, lit, (t - 0.5) * 2)
            a = 255 if alpha_fn is None else int(255 * clamp(alpha_fn(x, y), 0, 1))
            if a > 0:
                op[x, y] = c + (a,)


def profile_polygon(keys, s, ox, oy, mirror=1):
    """keys: (y, centre_x, half_width) in figure units, origin = waist centre. Returns a closed polygon."""
    left, right = [], []
    steps = 40
    for i in range(steps + 1):
        y = keys[0][0] + (keys[-1][0] - keys[0][0]) * i / steps
        for j in range(len(keys) - 1):
            if keys[j][0] <= y <= keys[j + 1][0]:
                t = (y - keys[j][0]) / max(keys[j + 1][0] - keys[j][0], 1e-6)
                cx = lerp(keys[j][1], keys[j + 1][1], t)
                hw = lerp(keys[j][2], keys[j + 1][2], t)
                break
        left.append((ox + (cx * mirror - hw) * s, oy + y * s))
        right.append((ox + (cx * mirror + hw) * s, oy + y * s))
    return left + right[::-1]


def shape_polygon(pts, s, ox, oy, dx_per_y=0.0, mirror=1, y0=0.0):
    out = []
    for (x, y) in pts:
        out.append((ox + (x * mirror + (y - y0) * dx_per_y * mirror) * s, oy + y * s))
    return out


# ------------------------------------------------------------------ the figure
LEG_L = [(13.5, -5.0, 4.1), (21.0, -4.9, 3.5), (26.0, -4.7, 2.5), (30.0, -4.7, 2.9), (34.0, -4.8, 3.5),
         (40.0, -4.7, 2.6), (46.5, -4.6, 1.6)]
LEG_R = [(13.5, 5.3, 4.1), (21.0, 5.1, 3.5), (26.0, 4.9, 2.5), (30.0, 4.9, 2.9), (34.0, 5.0, 3.5),
         (40.0, 4.8, 2.6), (46.5, 4.6, 1.6)]
SHOE_PTS = [(-2.0, 0), (2.0, 0), (2.7, 2.0), (3.1, 4.6), (3.0, 7.0), (2.3, 9.2), (1.0, 10.8), (-0.6, 10.9), (-1.9, 9.4),
            (-2.7, 7.0), (-2.9, 4.2), (-2.5, 1.6)]
SOCK_FOOT = [(-1.9, 0), (1.9, 0), (2.5, 2.4), (2.8, 5.0), (2.5, 7.4), (1.4, 9.0), (-0.2, 9.5), (-1.7, 8.4), (-2.6, 6.0),
             (-2.6, 3.0)]


def asym_keys(keys, stretch, shift):
    """Lengthens (stretch > 1) or shortens the shin and eases the leg sideways, so the two legs do not mirror each other."""
    out = []
    for (y, cx, hw) in keys:
        y2 = y if y <= 26 else 26 + (y - 26) * stretch
        out.append((y2, cx + shift * smooth((y - 20) / 26.0), hw))
    return out


def draw_figure(img, ox, oy, s, light=1, skirt=True, arms=True, rope=True, one_shoe_off=True, asym=False):
    SS_DEFAULT[0] = 4 if s >= 2 else 1
    """ox, oy: pixel position of the waist centre. s: pixels per figure unit (1 = game sprite)."""
    size = img.size

    def mask(pts):
        return poly_mask(size, pts)

    # --- rope, coming down out of the dark
    if rope:
        top, bottom = -40, 3
        for y in range(int(oy + top * s), int(oy + bottom * s)):
            fy = (y - oy) / s
            a = smooth((fy - top) / 20) * (1 - 0.65 * smooth((fy + 8) / 11))
            for dx in (-1, 0, 1):
                x = int(round(ox + dx * max(1, s * 0.9) * 0.9))
                twist = ((y // max(1, int(2 * s))) + dx) % 2
                col = ROPE_DARK if dx == -1 else (ROPE_LIT if dx == 1 else (ROPE if twist else ROPE_DARK))
                if 0 <= x < size[0] and 0 <= y < size[1]:
                    img.putpixel((x, y), col + (int(255 * a),))

    # --- arms: only below the waist, limp; a faint sleeve fades in above the hem, hands hang beside the thighs
    if arms:
        for side in (-1, 1):
            wob = 0.0 if side == -1 else 0.8
            sx = side * (13.8 + wob)
            sleeve = [(sx - 1.7, 5.0), (sx + 1.7, 5.0), (sx + side * 0.4 + 1.4, 15.2), (sx + side * 0.4 - 1.4, 15.2)]
            shade_mask(img, poly_mask(size, shape_polygon(sleeve, s, ox, oy)), SLEEVE, SLEEVE_DARK, SLEEVE_LIT, light,
                       alpha_fn=lambda x, y: smooth(((y - oy) / s - 5.0) / 5.0))
            hx = sx + side * 0.4
            cuff = [(hx - 1.7, 15.0), (hx + 1.7, 15.0), (hx + 1.6, 16.4), (hx - 1.6, 16.4)]
            shade_mask(img, poly_mask(size, shape_polygon(cuff, s, ox, oy)), CUFF, CUFF_DARK, CUFF_LIT, light, steps=4)
            palm = [(hx - 1.9, 16.4), (hx + 1.9, 16.4), (hx + 1.8, 19.4), (hx - 1.8, 19.4)]
            shade_mask(img, poly_mask(size, shape_polygon(palm, s, ox, oy)), SKIN, SKIN_DARK, SKIN_LIT, light, steps=4)
            # four limp fingers, one pixel column each, slightly different lengths, curling a little towards the body
            lengths = [3.0, 4.2, 3.8, 2.6]
            for i, fl in enumerate(lengths):
                fx = hx - 1.5 + i
                for k in range(int(fl * s)):
                    px = int(round(ox + (fx - side * (0.9 * k / s) * 0.18) * s))
                    py = int(round(oy + 19.4 * s + k))
                    shade = SKIN_DARK if (i % 2 == 0) else SKIN
                    if k >= int(fl * s) - 1:
                        shade = SKIN_DARK
                    if 0 <= px < img.width and 0 <= py < img.height:
                        img.putpixel((px, py), shade + (255,))

    # --- legs (skin, then sock, then shoe)
    leg_sets = [(LEG_L, 0.19), (LEG_R, -0.25)]
    if asym:
        leg_sets = [(asym_keys(LEG_L, 1.025, -0.2), 0.12), (asym_keys(LEG_R, 0.925, 0.7), -0.22)]
    for idx, (keys, inward) in enumerate(leg_sets):
        dy = keys[6][0] - 46.5
        poly = profile_polygon(keys, s, ox, oy)
        leg = mask(poly)
        shade_mask(img, leg, SKIN, SKIN_DARK, SKIN_LIT, light, steps=6, amb=0.22)
        # sock (crew length, ribbed top)
        sock_keys = [(38.4 + dy, keys[5][1], 2.75), (40.0 + dy, keys[5][1], 2.7), (46.5 + dy, keys[6][1], 1.95), (48.4 + dy, keys[6][1] + (0.05 if idx else -0.05), 1.9)]
        sock = mask(profile_polygon(sock_keys, s, ox, oy))
        shade_mask(img, sock, SOCK, SOCK_DARK, SOCK_LIT, light, steps=5)
        for ry in (39.2 + dy, 40.8 + dy):
            y = int(oy + ry * s)
            for x in range(img.width):
                if 0 <= y < img.height and sock.getpixel((x, y)) > 127:
                    p = img.getpixel((x, y))
                    img.putpixel((x, y), tuple(int(v * 0.78) for v in p[:3]) + (p[3],))
        # foot, toes down and turned in
        ax = ox + keys[6][1] * s
        if idx == 1 and one_shoe_off:
            foot = shape_polygon(SOCK_FOOT, s, ax, oy + (47.4 + dy) * s, dx_per_y=inward)
            shade_mask(img, mask(foot), SOCK, SOCK_DARK, SOCK_LIT, light, steps=5)
            # toe seam and the dip of the heel
            ty = int(oy + (47.4 + dy + 7.4) * s)
            fm = mask(foot)
            for x in range(img.width):
                if 0 <= ty < img.height and fm.getpixel((x, ty)) > 127:
                    p = img.getpixel((x, ty)); img.putpixel((x, ty), tuple(int(v * 0.7) for v in p[:3]) + (p[3],))
        else:
            foot = shape_polygon(SHOE_PTS, s, ax, oy + (47.4 + dy) * s, dx_per_y=inward)
            fm = mask(foot)
            shade_mask(img, fm, SHOE, SHOE_DARK, SHOE_LIT, light, steps=4, amb=0.04)
            # vamp seam across the shoe and a small highlight on the toe cap, on the side the light comes from
            seam_y = int(oy + (47.4 + dy + 6.0) * s)
            hi_y = int(oy + (47.4 + dy + 8.2) * s)
            for x in range(img.width):
                if 0 <= seam_y < img.height and fm.getpixel((x, seam_y)) > 127:
                    img.putpixel((x, seam_y), SHOE_DARK + (255,))
            xs = [x for x in range(img.width) if 0 <= hi_y < img.height and fm.getpixel((x, hi_y)) > 127]
            if xs:
                hx = xs[-2] if light == 1 else xs[1]
                for dx in (0, -1 if light == 1 else 1):
                    for ddy in (0, 1):
                        if 0 <= hx + dx < img.width and 0 <= hi_y + ddy < img.height and fm.getpixel((hx + dx, hi_y + ddy)) > 127:
                            img.putpixel((hx + dx, hi_y + ddy), (172, 154, 148, 255))
            # sole: the lowest pixel of every column, a warm brown line
            cols = {}
            for y in range(img.height):
                for x in range(img.width):
                    if fm.getpixel((x, y)) > 127:
                        cols[x] = y
            for x, y in cols.items():
                img.putpixel((x, y), SOLE + (255,))
            # collar: the dark opening of the shoe where the sock goes in
            cy = int(oy + (47.5 + dy) * s)
            for x in range(img.width):
                if 0 <= cy < img.height and fm.getpixel((x, cy)) > 127:
                    img.putpixel((x, cy), SHOE_DARK + (255,))

    # --- pleated skirt over the tops of the legs
    if skirt:
        hem = 15.4
        pts = []
        for i in range(0, 31):
            y = hem * i / 30
            half = 6.0 + 7.4 * (y / hem) ** 1.15
            pts.append((-half, y))
        left = [(ox + x * s, oy + y * s) for x, y in pts]
        right = [(ox - x * s, oy + y * s) for x, y in pts]
        poly = left + right[::-1]
        m = poly_mask(size, poly)

        def pleat(x, y):
            p = ((x - ox) / s + 40) / 2.2
            return 0.5 + 0.5 * math.cos(p * 2 * math.pi)

        def fade(x, y):
            return smooth((y - oy) / s / 7.5)

        shade_mask(img, m, SKIRT, SKIRT_DARK, SKIRT_LIT, light, steps=6, amb=0.2, pleat=pleat, alpha_fn=fade, pleat_weight=0.5)
        # wavy hem with a darker lip and the shadow it throws on the thighs
        mp = m.load()
        for x in range(img.width):
            ys = [y for y in range(img.height) if mp[x, y] > 127]
            if not ys:
                continue
            yb = max(ys)
            for k, f in ((0, 0.45), (1, 0.7)):
                yy = yb - k
                if 0 <= yy < img.height:
                    p = img.getpixel((x, yy))
                    img.putpixel((x, yy), tuple(int(v * f) for v in p[:3]) + (p[3],))
            for k in (1, 2, 3):
                yy = yb + k
                if 0 <= yy < img.height:
                    p = img.getpixel((x, yy))
                    if p[3] > 0:
                        f = 0.45 + 0.18 * k
                        img.putpixel((x, yy), tuple(int(v * f) for v in p[:3]) + (p[3],))


def make_figure():
    img = Image.new("RGBA", (48, 108), (0, 0, 0, 0))
    draw_figure(img, 24, 40, 1.0, light=1)
    return img


if __name__ == "__main__":
    fig = make_figure()
    fig.save(OUT / "hanging_figure_v2.png")
    S = 8
    bg = Image.new("RGBA", (fig.width * S, fig.height * S), (34, 38, 52, 255))
    bg.alpha_composite(fig.resize((fig.width * S, fig.height * S), Image.NEAREST))
    bg.convert("RGB").save(OUT / "_figure_8x.png")
    print("figure ok", fig.size)
