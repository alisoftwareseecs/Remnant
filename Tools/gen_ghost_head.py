"""Builds ghost_head.png: for every frame of the monster sheet, the head region (top part of the figure) as white
pixels. The game colours it faint red (Aiko's hair) at runtime. Same layout as monster_eyes.png (12 frames of 32x32)."""
from PIL import Image

SRC = r"D:\Unity\Remnant\Assets\_Remnant\Art\Characters\SideView\monster_sighting-sheet.png"
OUT = r"D:\Unity\Remnant\Assets\_Remnant\Art\Characters\SideView\ghost_head.png"
FRAME = 32
HEAD_SHARE = 0.38  # share of the figure's height, counted from its top, that counts as the head

sheet = Image.open(SRC).convert("RGBA")
out = Image.new("RGBA", sheet.size, (0, 0, 0, 0))
frames = sheet.size[0] // FRAME

for i in range(frames):
    frame = sheet.crop((i * FRAME, 0, (i + 1) * FRAME, FRAME))
    px = frame.load()
    rows = [y for y in range(FRAME) if any(px[x, y][3] > 20 for x in range(FRAME))]
    if not rows:
        continue
    top, bottom = min(rows), max(rows)
    limit = top + max(2, int((bottom - top + 1) * HEAD_SHARE))
    result = Image.new("RGBA", (FRAME, FRAME), (0, 0, 0, 0))
    rp = result.load()
    for y in range(top, min(limit, FRAME)):
        for x in range(FRAME):
            if px[x, y][3] > 20:
                rp[x, y] = (255, 255, 255, 255)
    out.paste(result, (i * FRAME, 0))

out.save(OUT)
print("saved", OUT, frames, "frames")
