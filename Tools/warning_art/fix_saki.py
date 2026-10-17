"""Repaints the front-facing frames (row 0) of Saki (bully 3, Charas193-200b.png, character #6) so she reads as a
schoolgirl in uniform: the open jacket gets a white shirt and a red tie behind it, and the grey shorts become a navy
pleated skirt. Side and back frames are already jacketed and are left alone. Run once:  python fix_saki.py
The untouched original is kept in D:/Unity/RemnantArt/_work/saki_original.png."""
import shutil
from pathlib import Path
from PIL import Image

SHEET = Path(r"D:/Unity/Remnant/Assets/_Remnant/Art/Characters/TopDown/Charas193-200b.png")
BACKUP = Path(r"D:/Unity/RemnantArt/_work/saki_original.png")
if not BACKUP.exists():
    shutil.copy(SHEET, BACKUP)
im = Image.open(BACKUP).convert("RGBA")   # always repaint from the original
px = im.load()
X0, Y0 = 192, 384   # character #6: column 1, row 1 of the sheet


def is_skin(c):
    r, g, b, a = c
    return a > 200 and r > 190 and g > 120 and b > 70 and r - b > 45


def is_grey(c):
    r, g, b, a = c
    return a > 200 and abs(r - g) < 22 and abs(g - b) < 26 and 70 < r < 215


SHIRT = [(246, 246, 252), (232, 232, 242), (212, 212, 228)]
TIE = [(186, 44, 58), (150, 30, 44)]
SKIRT = [(72, 78, 112), (58, 62, 94), (88, 94, 130)]

for c in range(3):
    fx = X0 + c * 64          # frame origin in the sheet
    fy = Y0                   # row 0 = facing down
    # ---- shirt: skin in the torso box becomes white; a red tie runs down the middle
    for y in range(62, 77):
        for x in range(22, 43):
            p = px[fx + x, fy + y]
            if is_skin(p) or (p[3] > 200 and p[0] > 150 and p[1] < 110 and p[2] < 110):
                shade = 0 if (y + x) % 5 else 1
                px[fx + x, fy + y] = SHIRT[shade] + (255,)
    # tie: find the torso centre from the face above and draw it
    xs = [x for x in range(20, 46) for y in range(50, 60) if is_skin(px[fx + x, fy + y])]
    cx = (min(xs) + max(xs)) // 2 if xs else 32
    for y in range(63, 73):
        for dx in (0, 1):
            px[fx + cx + dx, fy + y] = TIE[dx] + (255,)
    for dx in (-1, 0, 1, 2):
        px[fx + cx + dx - (0 if dx in (0, 1) else 0), fy + 63] = TIE[1] + (255,)
    # ---- leftover orange/brown waist pixels inside the body become shirt too
    for y in range(70, 78):
        for x in range(24, 41):
            p = px[fx + x, fy + y]
            if p[3] > 200 and p[0] > p[1] > p[2] and p[0] - p[2] > 40 and p[0] > 120:
                px[fx + x, fy + y] = SHIRT[1] + (255,)
    # ---- skirt: the grey shorts (and the top of the legs) become a navy pleated skirt
    for y in range(76, 86):
        for x in range(21, 44):
            p = px[fx + x, fy + y]
            body = is_grey(p) or (y >= 82 and is_skin(p) and y <= 84)
            if body:
                pleat = 2 if (x % 4 == 0) else (1 if y >= 83 else 0)
                px[fx + x, fy + y] = SKIRT[pleat] + (255,)

im.save(SHEET)
print("Saki repainted:", SHEET)
