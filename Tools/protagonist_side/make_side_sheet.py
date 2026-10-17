"""Builds the side-view protagonist sheet from the top-down protagonist's own left/right frames (Charas105-112b.png,
character #5). Nothing is redrawn: the pixels are copied 1:1. Output: 192x192, row 0 = left (frames 0-2), row 1 = right.
Run:  python make_side_sheet.py   (then let Unity import it; slicing and import settings are applied by an editor call)."""
from pathlib import Path
from PIL import Image

ROOT = Path(r"D:/Unity/Remnant/Assets/_Remnant/Art/Characters")
sheet = Image.open(ROOT / "TopDown/Charas105-112b.png").convert("RGBA")
X0, Y0 = 0, 384            # character #5: column 0, row 1 of the sheet
out = Image.new("RGBA", (192, 192), (0, 0, 0, 0))
for row, src_row in enumerate((1, 2)):   # source rows: 0 down, 1 left, 2 right, 3 up
    strip = sheet.crop((X0, Y0 + src_row * 96, X0 + 192, Y0 + src_row * 96 + 96))
    out.paste(strip, (0, row * 96))
out.save(ROOT / "SideView/protagonist_side.png")
print("saved protagonist_side.png")
