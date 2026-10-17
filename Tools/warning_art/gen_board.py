"""Chalkboard background for the content-warning screen. Drawn at 320x180 (hard pixels, ordered dither), shown upscaled
with point filtering. A classroom chalkboard at night: dark green-black slate, worn wooden frame, chalk tray with a stub of
chalk and an eraser, chalk dust, old eraser smears, ghosts of half-erased writing, a half-erased head-only hangman in the
top-right corner, dim cold moonlight from the left and a vignette. Run:  python gen_board.py
Outputs: D:/Unity/RemnantArt/_work/warning/board.png (+ 4x preview in _previews). The text is NOT in this image
(it is written live in the scene)."""
import math
import random
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont

OUT = Path(r"D:/Unity/RemnantArt/_work/warning")
PREV = Path(r"D:/Unity/RemnantArt/_previews")
FONT = r"D:/Unity/RemnantArt/_work/fonts/GloriaHallelujah.ttf"
OUT.mkdir(parents=True, exist_ok=True)
PREV.mkdir(parents=True, exist_ok=True)

W, H = 320, 180
rng = random.Random(7)
nrng = np.random.default_rng(7)

BAYER = np.array([[0, 8, 2, 10], [12, 4, 14, 6], [3, 11, 1, 9], [15, 7, 13, 5]], dtype=float)

# board rectangle (inside the frame) and frame
FR = (3, 3, 316, 164)   # outer frame
BD = (11, 10, 308, 157)  # slate
TRAY_TOP, TRAY_BOT = 157, 171


def noise(w, h, scale, seed):
    """Smooth value noise in 0..1, scale = feature size in px."""
    r = np.random.default_rng(seed)
    gw, gh = int(w / scale) + 3, int(h / scale) + 3
    g = r.random((gh, gw))
    im = Image.fromarray((g * 255).astype(np.uint8)).resize((gw * scale, gh * scale), Image.BICUBIC)
    a = np.asarray(im, dtype=float)[:h, :w] / 255.0
    return a


def paint_wood(img, box, horizontal, base, light, dark, seed):
    d = ImageDraw.Draw(img)
    x0, y0, x1, y1 = box
    r = random.Random(seed)
    d.rectangle(box, fill=base)
    if horizontal:
        for y in range(y0, y1 + 1):
            if r.random() < 0.55:
                c = light if r.random() < 0.4 else dark
                xa = x0 + r.randint(0, (x1 - x0) // 3)
                xb = min(x1, xa + r.randint(20, 140))
                d.line([(xa, y), (xb, y)], fill=c)
    else:
        for x in range(x0, x1 + 1):
            if r.random() < 0.55:
                c = light if r.random() < 0.4 else dark
                ya = y0 + r.randint(0, (y1 - y0) // 3)
                yb = min(y1, ya + r.randint(10, 90))
                d.line([(x, ya), (x, yb)], fill=c)


def build_albedo():
    img = Image.new("RGB", (W, H), (12, 14, 16))
    d = ImageDraw.Draw(img)

    # wall behind: slightly lighter paint, a few cracks
    wall = noise(W, H, 6, 1)
    wa = np.asarray(img, dtype=float)
    wa += (wall[..., None] - 0.5) * 10
    img = Image.fromarray(np.clip(wa, 0, 255).astype(np.uint8))
    d = ImageDraw.Draw(img)

    # wooden frame (worn): top/bottom horizontal grain, sides vertical
    WOOD, WL, WD = (112, 78, 52), (156, 112, 76), (58, 38, 28)
    paint_wood(img, (FR[0], FR[1], FR[2], BD[1] - 1), True, WOOD, WL, WD, 11)
    paint_wood(img, (FR[0], BD[1] - 1, BD[0] - 1, FR[3]), False, WOOD, WL, WD, 12)
    paint_wood(img, (BD[2] + 1, BD[1] - 1, FR[2], FR[3]), False, WOOD, WL, WD, 13)
    d.line([(FR[0], FR[1]), (FR[2], FR[1])], fill=WL)            # top bevel
    d.line([(BD[0] - 1, BD[1] - 1), (BD[2] + 1, BD[1] - 1)], fill=WD)  # inner shadow
    d.line([(BD[0] - 1, BD[1] - 1), (BD[0] - 1, BD[3] + 1)], fill=WD)
    # worn spots: chipped paint / bare wood along the frame
    for _ in range(40):
        x = rng.randint(FR[0], FR[2])
        y = rng.choice([rng.randint(FR[1], BD[1] - 2), rng.randint(BD[3] + 1, FR[3])])
        d.point((x, y), fill=(150, 118, 86))

    # slate
    sl = noise(W, H, 3, 2) * 0.5 + noise(W, H, 11, 3) * 0.5
    base = np.array([24, 42, 33], dtype=float)
    sa = np.zeros((H, W, 3))
    sa[:] = base
    sa += (sl[..., None] - 0.5) * np.array([10, 12, 11])
    # long horizontal wipe streaks from years of cleaning
    for _ in range(14):
        y = rng.randint(BD[1] + 3, BD[3] - 8)
        x0 = rng.randint(BD[0], BD[0] + 60)
        x1 = rng.randint(BD[2] - 120, BD[2])
        hgt = rng.randint(5, 14)
        m = Image.new("L", (W, H), 0)
        md = ImageDraw.Draw(m)
        # arc-shaped sweep: an eraser stroke bends with the arm
        pts = []
        for i in range(0, 41):
            t = i / 40
            pts.append((x0 + (x1 - x0) * t, y + math.sin(t * 2.2) * rng.uniform(1, 5)))
        md.line(pts, fill=255, width=hgt)
        m = m.filter(ImageFilter.GaussianBlur(2.5))
        ma = np.asarray(m, dtype=float) / 255.0
        grain = noise(W, H, 2, rng.randint(0, 9999))
        ma *= (0.35 + 0.65 * grain)
        sa += ma[..., None] * np.array([24, 28, 26]) * rng.uniform(0.35, 0.8)
    # chalk haze gathering low on the board (dust that settled)
    ys = np.arange(H, dtype=float)[:, None]
    low = np.clip((ys - (BD[3] - 40)) / 40.0, 0, 1)
    sa += (low * (0.4 + 0.6 * noise(W, H, 5, 4)))[..., None] * np.array([20, 22, 21])
    # clip to board interior
    mask = np.zeros((H, W), dtype=bool)
    mask[BD[1]:BD[3] + 1, BD[0]:BD[2] + 1] = True
    arr = np.asarray(img, dtype=float)
    arr[mask] = sa[mask]
    img = Image.fromarray(np.clip(arr, 0, 255).astype(np.uint8))
    d = ImageDraw.Draw(img)

    # ghosts of half-erased writing: faint, small, smeared horizontally
    ghost = Image.new("L", (W, H), 0)
    gd = ImageDraw.Draw(ghost)
    gf = ImageFont.truetype(FONT, 9)
    gf2 = ImageFont.truetype(FONT, 12)
    gd.fontmode = "1"
    samples = [
        (18, 20, "Chapter 4 - quiz Friday", gf, 120),
        (20, 33, "x  + 3x - 10 = 0", gf, 90),
        (150, 22, "p. 42  1-9", gf, 80),
        (214, 126, "see me after class", gf, 110),
        (16, 130, "no talking", gf2, 70),
        (120, 134, "homework?", gf, 90),
    ]
    for x, y, t, f, a in samples:
        gd.text((x, y), t, font=f, fill=a)
    # smear: a horizontal blur + gaps so it reads as wiped
    ghost = ghost.filter(ImageFilter.GaussianBlur(0.25))
    ga = np.asarray(ghost, dtype=float) / 255.0
    ga *= (0.5 + 0.5 * noise(W, H, 2, 8)) * (noise(W, H, 9, 9) > 0.4)
    # smear sideways
    sm = np.zeros_like(ga)
    for k in range(0, 3):
        sm[:, k:] += ga[:, :W - k] * (1 - k / 3)
    sm = np.clip(sm / 1.3, 0, 1)
    sm = (np.round(sm * 4) / 4.0)
    arr = np.asarray(img, dtype=float)
    chalk = np.array([205, 214, 204])
    m3 = (sm * mask)[..., None] * 0.7
    arr = arr * (1 - m3) + chalk * m3
    img = Image.fromarray(np.clip(arr, 0, 255).astype(np.uint8))
    d = ImageDraw.Draw(img)

    # half-erased hangman in the top-right: gallows and a head only, lines broken by a wipe
    hang = Image.new("L", (W, H), 0)
    hd = ImageDraw.Draw(hang)
    gx, gy = 272, 22
    hd.line([(gx - 14, gy + 46), (gx + 14, gy + 46)], fill=255)  # ground
    hd.line([(gx - 8, gy + 46), (gx - 8, gy)], fill=255)          # post
    hd.line([(gx - 8, gy), (gx + 14, gy)], fill=255)              # beam
    hd.line([(gx - 8, gy + 7), (gx - 1, gy)], fill=255)           # brace
    # head
    hd.line([(gx + 14, gy), (gx + 14, gy + 5)], fill=255)
    hd.ellipse([gx + 9, gy + 5, gx + 19, gy + 15], outline=255)
    # blank dashes under it (the word)
    for i in range(4):
        hd.line([(gx - 14 + i * 8, gy + 54), (gx - 8 + i * 8, gy + 54)], fill=255)
    ha = np.asarray(hang, dtype=float) / 255.0
    # wipe: remove chunks and fade the lower part
    wipe = noise(W, H, 4, 21) > 0.36
    fade = np.clip(1.15 - (np.arange(H)[:, None] - 20) / 55.0, 0.25, 1)
    ha = ha * wipe * fade * (0.55 + 0.35 * noise(W, H, 2, 22))
    arr = np.asarray(img, dtype=float)
    m3 = (ha * mask)[..., None] * 0.95
    arr = arr * (1 - m3) + chalk * m3
    # a smear of eraser across the top-right where it was wiped
    sm2 = Image.new("L", (W, H), 0)
    ImageDraw.Draw(sm2).line([(246, 36), (300, 44)], fill=255, width=11)
    sm2 = sm2.filter(ImageFilter.GaussianBlur(3.5))
    s2 = np.asarray(sm2, dtype=float) / 255.0 * (0.4 + 0.6 * noise(W, H, 2, 23)) * mask
    arr += s2[..., None] * np.array([18, 20, 19]) * 0.6
    img = Image.fromarray(np.clip(arr, 0, 255).astype(np.uint8))
    d = ImageDraw.Draw(img)

    # ----- tray (in front of the board): top surface and front face
    TT = (96, 70, 50)
    d.rectangle([FR[0] - 1, TRAY_TOP, FR[2] + 1, TRAY_TOP + 4], fill=TT)
    d.rectangle([FR[0] - 1, TRAY_TOP + 5, FR[2] + 1, TRAY_BOT], fill=(60, 42, 32))
    d.line([(FR[0] - 1, TRAY_TOP), (FR[2] + 1, TRAY_TOP)], fill=(40, 28, 22))  # shadow line against the slate
    d.line([(FR[0] - 1, TRAY_TOP + 1), (FR[2] + 1, TRAY_TOP + 1)], fill=(128, 96, 70))
    d.line([(FR[0] - 1, TRAY_TOP + 5), (FR[2] + 1, TRAY_TOP + 5)], fill=(116, 86, 62))
    for x in range(FR[0], FR[2], 3):
        if rng.random() < 0.6:
            d.point((x, TRAY_TOP + rng.randint(6, 10)), fill=(44, 30, 24))
    # chalk dust along the tray: soft powder
    dust = Image.new("L", (W, H), 0)
    dd = ImageDraw.Draw(dust)
    for _ in range(420):
        x = rng.randint(FR[0], FR[2])
        y = TRAY_TOP + rng.randint(0, 4)
        dd.point((x, y), fill=rng.randint(60, 190))
    dust = dust.filter(ImageFilter.GaussianBlur(0.6))
    da = np.asarray(dust, dtype=float) / 255.0
    arr = np.asarray(img, dtype=float)
    arr = arr * (1 - da[..., None] * 0.7) + chalk * (da[..., None] * 0.7)
    # a drift of dust that fell onto the lower frame
    img = Image.fromarray(np.clip(arr, 0, 255).astype(np.uint8))
    d = ImageDraw.Draw(img)

    # chalk stubs lying on the tray
    d.rectangle([220, TRAY_TOP - 1, 232, TRAY_TOP + 2], fill=(236, 238, 230))
    d.line([(220, TRAY_TOP - 1), (232, TRAY_TOP - 1)], fill=(255, 255, 250))
    d.line([(220, TRAY_TOP + 2), (232, TRAY_TOP + 2)], fill=(150, 156, 148))
    d.rectangle([240, TRAY_TOP, 245, TRAY_TOP + 2], fill=(220, 222, 212))
    d.point((234, TRAY_TOP + 2), fill=(220, 222, 214))
    d.point((236, TRAY_TOP + 1), fill=(220, 222, 214))
    # felt eraser: wooden back with a grey felt pad, chalky
    d.rectangle([54, TRAY_TOP - 6, 80, TRAY_TOP + 3], fill=(130, 94, 64))
    d.rectangle([54, TRAY_TOP - 6, 80, TRAY_TOP - 5], fill=(170, 128, 90))
    d.rectangle([54, TRAY_TOP - 2, 80, TRAY_TOP + 3], fill=(86, 88, 82))
    d.line([(54, TRAY_TOP + 3), (80, TRAY_TOP + 3)], fill=(40, 42, 40))
    for x in range(55, 80):
        if rng.random() < 0.55:
            d.point((x, TRAY_TOP + rng.randint(-2, 2)), fill=(205, 210, 200))
    # board-corner screws and a hanging hook on the frame
    for p in [(FR[0] + 2, FR[1] + 2), (FR[2] - 2, FR[1] + 2)]:
        d.rectangle([p[0] - 1, p[1] - 1, p[0] + 1, p[1] + 1], fill=(150, 150, 150))
        d.point((p[0], p[1]), fill=(60, 60, 60))

    return img


def build_light():
    """Light multiplier: dim cold moonlight from a window on the left (two panes, slanted), falling across the board,
    plus a vignette."""
    ys, xs = np.mgrid[0:H, 0:W].astype(float)
    L = 0.5 + 0.7 * np.exp(-xs / 110.0)
    # window panes: slanted bright patches on the board, soft edged
    u = xs * 0.9 + ys * 0.6
    v = ys * 0.9 - xs * 0.6
    pane = (1 / (1 + np.exp(-(np.cos(u / 40.0) - 0.15) * 9))) * (1 / (1 + np.exp(-(np.cos(v / 34.0 + 1.0) - 0.05) * 9)))
    L += 1.1 * pane * np.exp(-xs / 230.0)
    L *= (1.1 - 0.25 * ys / H)
    cx, cy = W * 0.46, H * 0.46
    r = np.sqrt(((xs - cx) / (W * 0.66)) ** 2 + ((ys - cy) / (H * 0.7)) ** 2)
    vig = np.clip(1.12 - r * r * 0.8, 0.12, 1)
    return L * vig


def render():
    albedo = np.asarray(build_albedo(), dtype=float)
    L = build_light()[..., None]
    cold = np.array([0.62, 0.78, 1.0])  # moonlight tint
    amb = np.array([0.55, 0.65, 0.9])
    lit = albedo * (L * cold * 1.35 + 0.08 * amb)
    # chalk stays slightly self-bright? no: it is just paint, lit like everything else
    # ordered dither + quantise (retro banding)
    step = 7.0
    b = (np.tile(BAYER, (H // 4 + 1, W // 4 + 1))[:H, :W] - 7.5) / 16.0
    q = np.round((lit + b[..., None] * step) / step) * step
    q = np.clip(q, 0, 255).astype(np.uint8)
    return Image.fromarray(q)


if __name__ == "__main__":
    im = render()
    im.save(OUT / "board.png")
    im.resize((W * 4, H * 4), Image.NEAREST).save(PREV / "warning_board_4x.png")
    print("ok")
