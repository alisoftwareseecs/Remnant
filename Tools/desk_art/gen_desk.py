"""Aiko's desk in four states, drawn procedurally in the project's pixel style (hard pixels, the school wood palette).

World sprites (44x48, a copy of obj_student_desk with the marks on its top, 48 px per unit; a SpriteByFlag swaps them).
Close-ups (480x270, shown 4x with point filtering): the readable surface of the desk for Day 2, the Future (2-B, with the
bloodstain and page 8) and the epilogue. A Day 1 close-up is made too, for previews only (the desk is not interactable on Day 1).
Marker writing uses GloriaHallelujah (thresholded to hard pixels). Run:  python gen_desk.py
Outputs: Assets/_Remnant/Art/Desk/*.png and D:/Unity/RemnantArt/_previews/desk_*_4x.png"""
import math
import random
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont

ROOT = Path(r"D:/Unity/Remnant")
OUT = ROOT / "Assets/_Remnant/Art/Desk"
PREV = Path(r"D:/Unity/RemnantArt/_previews")
FONT = str(ROOT / "Assets/_Remnant/Fonts/GloriaHallelujah.ttf")
OUT.mkdir(parents=True, exist_ok=True)
PREV.mkdir(parents=True, exist_ok=True)

WOOD = (105, 67, 67)
WOOD_DARK = (84, 54, 54)
WOOD_DEEP = (71, 46, 46)
WOOD_LIGHT = (122, 80, 76)
EDGE = (97, 80, 77)
MARKER = (22, 20, 24)
CARVE_LIGHT = (168, 122, 108)
CARVE_DARK = (52, 32, 34)
SPLINTER = (198, 156, 128)
GUM = (222, 122, 164)
GUM_HI = (246, 176, 204)
TAPE = (214, 198, 150)
STAIN = (78, 50, 56)
BLOOD = (96, 36, 32)
BLOOD_DARK = (64, 22, 22)


# ------------------------------------------------------------------ helpers
def rng(seed):
    return random.Random(seed)


def noise(w, h, cell, seed):
    r = np.random.default_rng(seed)
    gw, gh = w // cell + 3, h // cell + 3
    g = r.random((gh, gw))
    im = Image.fromarray((g * 255).astype(np.uint8)).resize((gw * cell, gh * cell), Image.BICUBIC)
    return np.asarray(im, dtype=float)[:h, :w] / 255.0


def text_mask(txt, size, w, h, pos, angle=0, stroke=0, font=FONT):
    """Hard-pixel mask (L, 0/255) of marker handwriting."""
    pad = 400   # draw on a larger canvas so rotated writing is not cut off at the edge
    big = Image.new("L", (w + 2 * pad, h + 2 * pad), 0)
    d = ImageDraw.Draw(big)
    f = ImageFont.truetype(font, size)
    d.text((pos[0] + pad, pos[1] + pad), txt, font=f, fill=255, stroke_width=stroke, stroke_fill=255)
    if angle:
        big = big.rotate(angle, resample=Image.BICUBIC, center=(pos[0] + pad, pos[1] + pad))
    big = big.crop((pad, pad, pad + w, pad + h))
    return big.point(lambda v: 255 if v > 120 else 0)


def paste_color(img, mask, color, alpha=1.0):
    a = (np.asarray(mask, dtype=float) / 255.0) * alpha
    arr = np.asarray(img, dtype=float)
    for i in range(3):
        arr[..., i] = arr[..., i] * (1 - a) + color[i] * a
    img.paste(Image.fromarray(np.clip(arr, 0, 255).astype(np.uint8), "RGB"))


def smear(mask, length, drop=0):
    """Smudge a mask sideways (a hand scrubbing across marker)."""
    a = np.asarray(mask, dtype=float) / 255.0
    out = np.zeros_like(a)
    for k in range(length):
        out += np.roll(np.roll(a, k, axis=1), int(k * drop / max(1, length)), axis=0) * (1 - k / length)
    out = np.clip(out / (length * 0.45), 0, 1)
    return Image.fromarray((out * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(0.8))


def wood_surface(w, h, seed, base=WOOD):
    """Plank-less classroom desk top: flat reddish wood with long grain, scuffs and a darker lip."""
    r = rng(seed)
    n1 = noise(w, h, 6, seed)
    n2 = noise(w, h, 2, seed + 1)
    grain = noise(w, h, 1, seed + 2)
    # long grain: stretch noise horizontally
    g = np.asarray(Image.fromarray((noise(w // 8 + 4, h, 1, seed + 3) * 255).astype(np.uint8)).resize((w, h), Image.BICUBIC), dtype=float) / 255.0
    arr = np.zeros((h, w, 3))
    arr[:] = base
    arr += ((n1 - 0.5) * 14 + (n2 - 0.5) * 8 + (g - 0.5) * 12)[..., None]
    img = Image.fromarray(np.clip(arr, 0, 255).astype(np.uint8), "RGB")
    d = ImageDraw.Draw(img)
    # a few scuffs and dents
    for _ in range(max(6, w * h // 3500)):
        x, y = r.randint(0, w - 4), r.randint(0, h - 4)
        d.line([(x, y), (x + r.randint(2, 9), y + r.randint(-1, 1))], fill=WOOD_DARK if r.random() < 0.6 else WOOD_LIGHT)
    return img


def lip(img, width):
    """Darker rim around the desk top."""
    d = ImageDraw.Draw(img)
    w, h = img.size
    for i in range(width):
        c = tuple(int(WOOD_DEEP[k] + (WOOD_DARK[k] - WOOD_DEEP[k]) * i / max(1, width)) for k in range(3))
        d.rectangle([i, i, w - 1 - i, h - 1 - i], outline=c)


def carved(img, draw_fn, offset=1):
    """Carving: a dark groove with a light lower edge (light drawn first, offset by one pixel)."""
    light = Image.new("L", img.size, 0)
    dark = Image.new("L", img.size, 0)
    draw_fn(ImageDraw.Draw(light), 0)
    draw_fn(ImageDraw.Draw(dark), 0)
    shifted = Image.new("L", img.size, 0)
    shifted.paste(light, (offset, offset))
    paste_color(img, shifted.point(lambda v: 255 if v > 0 else 0), CARVE_LIGHT)
    paste_color(img, dark.point(lambda v: 255 if v > 0 else 0), CARVE_DARK)


def stick_figure_on_stairs(d, ox, oy, s):
    """A crude carved stick figure tumbling down a staircase (steps descend to the right)."""
    step = int(9 * s)
    for i in range(4):   # stairs
        x0, y0 = ox + i * step, oy + i * step
        d.line([(x0, y0), (x0 + step, y0)], fill=255, width=max(1, int(s)))
        d.line([(x0 + step, y0), (x0 + step, y0 + step)], fill=255, width=max(1, int(s)))
    # the figure, mid-fall on the second step
    hx, hy = ox + int(1.6 * step), oy + int(0.2 * step)
    r = max(2, int(2.4 * s))
    d.ellipse([hx - r, hy - r, hx + r, hy + r], outline=255, width=max(1, int(s)))
    bx, by = hx + int(1.5 * s), hy + int(2.8 * s)
    d.line([(hx, hy + r), (bx + int(3 * s), by + int(5 * s))], fill=255, width=max(1, int(s)))             # body
    d.line([(hx + int(1 * s), hy + r + int(2 * s)), (hx - int(5 * s), hy + int(0 * s))], fill=255, width=max(1, int(s)))   # arm up
    d.line([(hx + int(1 * s), hy + r + int(2 * s)), (hx + int(6 * s), hy + int(1 * s))], fill=255, width=max(1, int(s)))   # arm out
    d.line([(bx + int(3 * s), by + int(5 * s)), (bx - int(2 * s), by + int(9 * s))], fill=255, width=max(1, int(s)))      # leg
    d.line([(bx + int(3 * s), by + int(5 * s)), (bx + int(8 * s), by + int(8 * s))], fill=255, width=max(1, int(s)))      # leg
    # motion ticks
    d.line([(hx - int(7 * s), hy - int(3 * s)), (hx - int(10 * s), hy - int(5 * s))], fill=255, width=max(1, int(s)))
    d.line([(hx - int(6 * s), hy + int(2 * s)), (hx - int(10 * s), hy + int(3 * s))], fill=255, width=max(1, int(s)))


def scratched_name(img, box, seed, s, clean=False):
    """Her name carved, then gouged out so hard the wood splintered."""
    x0, y0, x1, y1 = box
    r = rng(seed)
    name = Image.new("L", img.size, 0)
    ImageDraw.Draw(name).text((x0 + int(4 * s), y0 - int(5 * s)), "Aiko", font=ImageFont.truetype(FONT, int(20 * s)), fill=255)
    paste_color(img, name.point(lambda v: 255 if v > 120 else 0), CARVE_DARK, 0.9)
    gouge = Image.new("L", img.size, 0)
    gd = ImageDraw.Draw(gouge)
    # dense crossing gouges right over the name so it cannot be read
    nx0, ny0, nx1, ny1 = x0 + int(4 * s), y0 + int(2 * s), x0 + int(60 * s), y1
    for _ in range(int(40 * s)):
        a = (r.randint(nx0, nx1), r.randint(ny0, ny1))
        b = (a[0] + r.randint(int(-22 * s), int(22 * s)), a[1] + r.randint(int(-14 * s), int(14 * s)))
        gd.line([a, b], fill=255, width=max(2, int(1.8 * s)))
    for _ in range(int(26 * s)):
        a = (r.randint(x0, x1), r.randint(y0, y1))
        b = (a[0] + r.randint(int(-16 * s), int(16 * s)), a[1] + r.randint(int(-7 * s), int(7 * s)))
        gd.line([a, b], fill=255, width=max(1, int(1.4 * s)))
    # clean: the gouges are still there, but as dark cuts, with no pale splinters lying around
    paste_color(img, gouge.point(lambda v: 255 if v > 0 else 0), CARVE_DARK if clean else SPLINTER, 0.7 if clean else 0.95)
    deep = Image.new("L", img.size, 0)
    dd = ImageDraw.Draw(deep)
    for _ in range(int(12 * s)):
        a = (r.randint(x0, x1), r.randint(y0, y1))
        dd.line([a, (a[0] + r.randint(int(-10 * s), int(10 * s)), a[1] + r.randint(int(-4 * s), int(4 * s)))], fill=255, width=max(1, int(s)))
    paste_color(img, deep.point(lambda v: 255 if v > 0 else 0), CARVE_DARK, 0.85)
    if clean:
        return
    # splinters sticking out of the gouged area
    sd = ImageDraw.Draw(img)
    for _ in range(int(18 * s)):
        x = r.randint(x0 - int(3 * s), x1 + int(3 * s))
        y = r.choice([y0 - int(1 * s), y1 + int(1 * s), r.randint(y0, y1)])
        sd.line([(x, y), (x + r.randint(-3, 3), y + r.randint(int(-3 * s), int(3 * s)))], fill=SPLINTER)
        sd.point((x, y), fill=(236, 204, 176))


def gum(img, cx, cy, s):
    d = ImageDraw.Draw(img)
    r = int(5 * s)
    d.ellipse([cx - r, cy - int(r * 0.8), cx + r, cy + int(r * 0.8)], fill=GUM)
    d.ellipse([cx - int(r * 0.5), cy - int(r * 0.6), cx - int(r * 0.1), cy - int(r * 0.25)], fill=GUM_HI)
    d.line([(cx + r, cy), (cx + r + int(5 * s), cy + int(3 * s))], fill=GUM, width=max(1, int(s)))   # a string of gum


def torn_tape(img, x, y, length, height, s, tilt=0.35, yellowed=False):
    col = (214, 190, 120) if yellowed else TAPE
    poly = [(x, y), (x + length, y + int(length * tilt)), (x + length - int(3 * s), y + int(length * tilt) + height // 2),
            (x + length, y + int(length * tilt) + height), (x, y + height)]
    d = ImageDraw.Draw(img)
    d.polygon(poly, fill=col)
    d.line([(x, y), (x + length, y + int(length * tilt))], fill=tuple(min(255, c + 24) for c in col))
    for k in range(3):   # the torn end: ragged teeth
        d.point((x + length - int(1 * s), y + int(length * tilt) + k * height // 3), fill=WOOD)


def water_stains(img, spots, seed, s):
    r = rng(seed)
    for cx, cy, rad in spots:
        layer = Image.new("L", img.size, 0)
        ld = ImageDraw.Draw(layer)
        for _ in range(3):
            ox, oy = r.randint(int(-2 * s), int(2 * s)), r.randint(int(-2 * s), int(2 * s))
            ld.ellipse([cx - rad + ox, cy - int(rad * 0.7) + oy, cx + rad + ox, cy + int(rad * 0.7) + oy], outline=255, width=max(1, int(s)))
        paste_color(img, layer, STAIN, 0.7)
        inner = Image.new("L", img.size, 0)
        ImageDraw.Draw(inner).ellipse([cx - rad + int(2 * s), cy - int(rad * 0.7) + int(s), cx + rad - int(2 * s), cy + int(rad * 0.7) - int(s)], fill=255)
        paste_color(img, inner.point(lambda v: 255 if v > 0 else 0), STAIN, 0.22)


def pencil_shavings(img, r, count, area):
    d = ImageDraw.Draw(img)
    x0, y0, x1, y1 = area
    for _ in range(count):
        x, y = r.randint(x0, x1), r.randint(y0, y1)
        c = r.choice([(232, 214, 170), (244, 232, 196), (214, 150, 120)])
        d.line([(x, y), (x + r.randint(1, 2), y + r.randint(0, 1))], fill=c + (255,))


def bloodstain(img, seed, box, s):
    """Dried, old, matte blood: a dark brownish-red irregular pool with a cracked skin, a lighter dried rim and spatter."""
    x0, y0, x1, y1 = box
    w, h = img.size
    n = noise(w, h, max(4, int(18 * s)), seed)
    n2 = noise(w, h, max(2, int(5 * s)), seed + 7)
    ys, xs = np.mgrid[0:h, 0:w].astype(float)
    cx, cy = (x0 + x1) / 2, (y0 + y1) / 2
    d = np.sqrt(((xs - cx) / ((x1 - x0) / 2)) ** 2 + ((ys - cy) / ((y1 - y0) / 2)) ** 2)
    field = 1.0 - d + (n - 0.5) * 0.62 + (n2 - 0.5) * 0.10
    mask = field > 0.12
    rim = (field > 0.12) & (field < 0.24)
    core = field > 0.55
    arr = np.asarray(img, dtype=float)
    for m, col, a in ((mask, BLOOD, 0.93), (core, BLOOD_DARK, 0.7), (rim, (116, 58, 44), 0.8)):
        for i in range(3):
            arr[..., i] = np.where(m, arr[..., i] * (1 - a) + col[i] * a, arr[..., i])
    img.paste(Image.fromarray(np.clip(arr, 0, 255).astype(np.uint8), "RGB"))
    d2 = ImageDraw.Draw(img)
    r = rng(seed)
    for _ in range(int(40 * s)):   # dried cracks
        x, y = r.randint(x0, x1), r.randint(y0, y1)
        if field[min(h - 1, y), min(w - 1, x)] > 0.3:
            ex, ey = x + r.randint(int(-7 * s), int(7 * s)), y + r.randint(int(-5 * s), int(5 * s))
            d2.line([(x, y), (ex, ey)], fill=(48, 16, 16))
    for _ in range(int(26 * s)):   # spatter around the stain
        x, y = int(cx + r.gauss(0, (x1 - x0) * 0.42)), int(cy + r.gauss(0, (y1 - y0) * 0.5))
        if 0 <= x < w and 0 <= y < h:
            d2.rectangle([x, y, x + int(s) - 1, y + int(s) - 1], fill=(92, 34, 30))


def dust(img, seed, strength):
    w, h = img.size
    n = noise(w, h, 3, seed) * 0.6 + noise(w, h, 1, seed + 1) * 0.4
    a = np.clip((n - 0.5) * 2.0, 0, 1) * strength
    arr = np.asarray(img, dtype=float)
    for i, c in enumerate((158, 156, 160)):
        arr[..., i] = arr[..., i] * (1 - a) + c * a
    img.paste(Image.fromarray(np.clip(arr, 0, 255).astype(np.uint8), "RGB"))


# ------------------------------------------------------------------ close-up composition (480x270)
W, H = 480, 270


def layout_marks(state, seed):
    """Everything that sits on the desk, per state. Returns the finished 480x270 surface."""
    r = rng(seed)
    img = wood_surface(W, H, seed)
    lip(img, 7)
    s = 1.0
    # marker layer
    marker = text_mask("KILLER", 70, W, H, (40, 4), angle=2, stroke=3)
    mur = text_mask("MURDERER", 50, W, H, (34, 76), angle=-2, stroke=2)
    marker = Image.fromarray(np.maximum(np.asarray(marker), np.asarray(mur)))
    # smaller scrawls around the edges (kept above the dialogue box, which covers the bottom quarter)
    scr = [("go home", 26, (326, 62), 7), ("nobody wants you here", 21, (96, 2), 0), ("ask her about the stairs", 14, (460, 40), -88)]
    scraw = Image.new("L", (W, H), 0)
    for txt, sz, pos, ang in scr:
        scraw = Image.fromarray(np.maximum(np.asarray(scraw), np.asarray(text_mask(txt, sz, W, H, pos, angle=ang, stroke=1))))
    marks = Image.fromarray(np.maximum(np.asarray(marker), np.asarray(scraw)))

    # carvings (not scrubbable): stick figure on stairs and her scratched-out name
    carved(img, lambda d, o: stick_figure_on_stairs(d, 322, 100, 2.5), offset=2)
    scratched_name(img, (30, 160, 200, 192), seed + 5, 1.3, clean=(state == "epilogue"))

    if state == "epilogue":
        # nearly clean: the faintest grey ghost of KILLER, carvings still there, nothing else
        paste_color(img, marks.filter(ImageFilter.GaussianBlur(1.0)), (176, 152, 152), 0.26)
        # cared for: a soft polish and a warm patch of light
        ys, xs = np.mgrid[0:H, 0:W].astype(float)
        glow = np.clip(1 - np.abs((xs - ys * 0.7) - 150) / 150, 0, 1) * 0.16
        arr = np.asarray(img, dtype=float)
        arr[..., 0] += glow * 60; arr[..., 1] += glow * 44; arr[..., 2] += glow * 16
        img = Image.fromarray(np.clip(arr, 0, 255).astype(np.uint8), "RGB")
        paste_color(img, Image.new("L", (W, H), 255), (255, 240, 220), 0.05)
        pd = ImageDraw.Draw(img)   # a pencil laid down neatly along the top edge
        pd.rectangle([250, 22, 330, 27], fill=(226, 176, 62))
        pd.rectangle([250, 22, 330, 23], fill=(244, 204, 100))
        pd.polygon([(330, 22), (342, 25), (330, 27)], fill=(224, 190, 150))
        pd.polygon([(338, 24), (342, 25), (338, 26)], fill=(52, 46, 50))
        pd.rectangle([244, 22, 250, 27], fill=(214, 110, 120))
        return img

    water_stains(img, [(250, 130, 20), (410, 60, 16), (60, 112, 14)], seed + 9, 1.4)
    gum(img, 446, 30, 1.7)
    torn_tape(img, 10, 10, 56, 16, 1.0)

    if state == "day1":
        paste_color(img, marks, MARKER, 1.0)
    elif state == "day2":
        # half scrubbed: smeared grey, still readable
        sm = smear(marks, 9, drop=2)
        paste_color(img, sm, (96, 92, 98), 0.78)
        paste_color(img, marks.filter(ImageFilter.GaussianBlur(0.6)), (60, 56, 62), 0.35)
        # scrub marks: lighter arcs where a cloth went over
        d = ImageDraw.Draw(img)
        for _ in range(14):
            x, y = r.randint(30, 420), r.randint(20, 190)
            d.arc([x, y, x + r.randint(30, 70), y + r.randint(14, 30)], 190, 330, fill=WOOD_LIGHT)
    elif state == "future":
        paste_color(img, marks.filter(ImageFilter.GaussianBlur(0.7)), (58, 42, 44), 0.62)
        bloodstain(img, seed + 13, (110, 40, 380, 186), 1.0)
        dust(img, seed + 21, 0.22)
    return img


def page8_sheet(img, seed):
    """Page 8 lying on the desk: a cream sheet with ink lines, one corner dark with old blood."""
    sheet = Image.new("RGBA", (150, 205), (0, 0, 0, 0))   # drawn at 150x205, then scaled down below
    d = ImageDraw.Draw(sheet)
    d.rectangle([0, 0, 149, 204], fill=(228, 216, 188, 255), outline=(190, 176, 146, 255))
    f = ImageFont.truetype(FONT, 17)
    d.text((10, 8), "June 23rd", font=f, fill=(54, 36, 30, 255))
    r = rng(seed)
    for row in range(8):
        y = 40 + row * 19
        x = 10
        while x < 132:
            w = r.randint(10, 34)
            d.line([(x, y), (min(138, x + w), y + r.randint(-1, 1))], fill=(70, 52, 44, 255), width=2)
            x += w + r.randint(6, 10)
    for k in range(60):   # a dark dried smear on the lower corner (it lay in the stain)
        x, y = 150 - int(abs(r.gauss(0, 22))), 205 - int(abs(r.gauss(0, 26)))
        d.point((max(0, x), max(0, y)), fill=(104, 44, 36, 255))
    sheet = sheet.resize((124, 170), Image.NEAREST).rotate(-7, expand=True, resample=Image.BICUBIC)
    shadow = Image.new("RGBA", sheet.size, (0, 0, 0, 0))
    shadow.paste((20, 10, 12, 120), mask=sheet.split()[3])
    shadow = shadow.filter(ImageFilter.GaussianBlur(3))
    img.paste(shadow.convert("RGB"), (310, 14), shadow.split()[3])
    img.paste(sheet.convert("RGB"), (304, 8), sheet.split()[3])


# ------------------------------------------------------------------ world overlays (44x48)
def world_overlay(state, seed):
    s = 1
    w, h = 44, 48
    base = Image.new("RGB", (w, h), (0, 0, 0))     # work on black, alpha from what we draw
    alpha = Image.new("L", (w, h), 0)
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    r = rng(seed)

    def put(x, y, col, a=255):
        if 1 <= x <= 42 and 1 <= y <= 20:
            img.putpixel((x, y), tuple(col) + (a,))

    def blit_mask(mask, col, a=255):
        m = np.asarray(mask)
        for yy in range(m.shape[0]):
            for xx in range(m.shape[1]):
                if m[yy, xx]:
                    put(xx, yy, col, a)

    def small(txt, size, pos, angle=0, stroke=0):
        return text_mask(txt, size, w, h, pos, angle=angle, stroke=stroke)

    killer = small("KILLER", 14, (4, -1), stroke=1)
    murderer = small("MURDERER", 10, (3, 8), stroke=1)
    marks = np.maximum(np.asarray(killer), np.asarray(murderer))
    scrawls = np.maximum(np.maximum(np.asarray(small("go home", 5, (2, 14))), np.asarray(small("nobody wants you here", 4, (14, 16)))),
                         np.asarray(small("ask her about the stairs", 4, (3, 19))))

    if state == "epilogue":
        blit_mask(marks > 0, (118, 86, 84), 26)
        # carvings stay
    else:
        if state == "day1":
            blit_mask(marks > 0, MARKER, 255)
            blit_mask(scrawls > 0, MARKER, 190)
        elif state == "day2":
            core = marks > 0
            fringe = (np.asarray(smear(Image.fromarray(marks.astype(np.uint8)), 4)) > 40) & ~core
            blit_mask(fringe, (126, 118, 124), 80)
            blit_mask(core, (112, 108, 114), 190)
            blit_mask(scrawls > 0, (116, 110, 118), 120)
        elif state == "future":
            blit_mask(marks > 0, (60, 44, 46), 150)
            blit_mask(scrawls > 0, (64, 48, 50), 90)

    # carvings: a stick figure on stairs (lower right) and the scratched-out name (lower left)
    for (x, y) in [(31, 12), (32, 12), (32, 13), (33, 13), (33, 14), (34, 14), (34, 15), (35, 15), (35, 16), (36, 16)]:
        put(x, y, CARVE_LIGHT if state != "future" else (128, 96, 88), 235)
    for (x, y) in [(30, 11), (29, 12), (29, 13), (30, 13), (31, 11), (32, 10), (33, 10), (33, 11)]:
        put(x, y, CARVE_DARK, 235)
    for x in range(4, 16):
        for y in range(16, 20):
            if r.random() < 0.55:
                put(x, y, SPLINTER if r.random() < 0.7 else CARVE_DARK, 235 if state != "future" else 150)

    if state in ("day1", "day2"):
        for (x, y) in [(40, 2), (41, 2), (40, 3), (41, 3), (39, 3), (40, 4)]:
            put(x, y, GUM if (x + y) % 3 else GUM_HI)
        for (x, y) in [(2, 2), (3, 2), (4, 2), (5, 3), (6, 3), (7, 4), (3, 3), (4, 3), (5, 4), (2, 3)]:
            put(x, y, TAPE, 215)
        for (cx, cy) in [(22, 18), (36, 6)]:
            for ang in range(0, 360, 40):
                put(int(cx + 3 * math.cos(math.radians(ang))), int(cy + 2 * math.sin(math.radians(ang))), STAIN, 120)
    if state == "future":
        for (x, y) in [(40, 2), (41, 2), (40, 3), (41, 3)]:
            put(x, y, (140, 120, 120), 200)
        n = noise(w, h, 3, seed + 3)
        for yy in range(1, 21):
            for xx in range(1, 43):
                if n[yy, xx] > 0.64:
                    img.putpixel((xx, yy), (150, 148, 152, 64))
        bl = Image.new("RGBA", (w, h), (0, 0, 0, 0))
        bd = ImageDraw.Draw(bl)
        nb = noise(w, h, 5, seed + 11)
        ys, xs = np.mgrid[0:h, 0:w].astype(float)
        dd = np.sqrt(((xs - 22) / 13.0) ** 2 + ((ys - 11) / 7.5) ** 2)
        fld = 1.0 - dd + (nb - 0.5) * 0.7
        for yy in range(1, 21):
            for xx in range(1, 43):
                if fld[yy, xx] > 0.12:
                    col = BLOOD_DARK if fld[yy, xx] > 0.5 else (BLOOD if fld[yy, xx] > 0.25 else (118, 60, 46))
                    bl.putpixel((xx, yy), col + (235,))
        bd.line([(21, 18), (22, 24)], fill=BLOOD + (230,), width=1)    # a dried drip over the front edge
        for k in range(10):
            bd.point((r.randint(6, 38), r.randint(2, 19)), fill=(92, 34, 30, 255))
        img.alpha_composite(bl)
    if state == "epilogue":
        for x in range(2, 42):
            img.putpixel((x, 1), (255, 236, 214, 30))
    # stay on the desk top: clip to the desk sprite's own opaque pixels
    return img


def floor_shavings(seed):
    img = Image.new("RGBA", (96, 96), (0, 0, 0, 0))
    pencil_shavings(img, rng(seed), 26, (6, 56, 90, 92))
    # a pencil stub on the floor
    d = ImageDraw.Draw(img)
    d.line([(64, 70), (73, 72)], fill=(214, 168, 74, 255), width=1)
    d.point((74, 72), fill=(40, 34, 36, 255))
    return img


def save_scaled_preview(img, name, k=4):
    img.resize((img.width * k, img.height * k), Image.NEAREST).save(PREV / name)


if __name__ == "__main__":
    # close-ups
    for state in ("day1", "day2", "future", "epilogue"):
        surf = layout_marks(state, 11)
        if state == "future":
            page8_sheet(surf, 5)
        if state != "day1":
            surf.save(OUT / f"desk_closeup_{state}.png")
        save_scaled_preview(surf, f"desk_closeup_{state}_4x.png")
    # world overlays, clipped to the desk sprite's opaque top
    desk = Image.open(ROOT / "Assets/_Remnant/Art/Environment/School/Inside_C.png").convert("RGBA")
    dh = desk.size[1]
    base = desk.crop((145, dh - 144, 145 + 44, dh - 96))
    mask = base.split()[3]
    for state in ("day1", "day2", "future", "epilogue"):
        ov = world_overlay(state, 3)
        a = np.minimum(np.asarray(ov.split()[3]), np.asarray(mask))
        ov.putalpha(Image.fromarray(a))
        comp = base.copy()
        comp.alpha_composite(ov)
        comp.save(OUT / f"desk_aiko_{state}.png")   # the whole desk sprite with the marks on it (swapped in by story flag)
        save_scaled_preview(comp, f"desk_world_{state}_8x.png", 8)
    floor_shavings(7).save(OUT / "desk_floor_shavings.png")
    print("desk art done")
