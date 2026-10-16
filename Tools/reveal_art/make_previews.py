"""Writes the 4x previews to D:/Unity/RemnantArt/_previews/reveal_*.png (nothing here goes into the Unity project)."""
from PIL import Image

from gen_reveal import OUT, PREV
from gen_reveal2 import make_cast_shadow, make_fallen_shoe, make_legs, make_still

S = 4
legs = make_legs()
shoe = make_fallen_shoe()
shadow = make_cast_shadow(legs)
still = make_still()

# sources for later wiring (still outside the Unity project)
legs.save(OUT / "hanging_legs.png")
shoe.save(OUT / "fallen_shoe.png")
shadow.save(OUT / "cast_shadow.png")
still.convert("RGB").save(OUT / "reveal_still.png")


def up(img, k=S):
    return img.resize((img.width * k, img.height * k), Image.NEAREST)


def panel(size, color):
    return Image.new("RGBA", size, color + (255,))


# --- legs: in the dark, and on a lit wall tone, with the floor and the fallen shoe below
dark, wall = (14, 16, 24), (96, 100, 118)
pw, ph = legs.width * S + 80, legs.height * S + 40 + 140
sheet = Image.new("RGBA", (pw * 2 + 30, ph), (8, 8, 12, 255))
for i, bg in enumerate((dark, wall)):
    p = panel((pw, ph), bg)
    p.alpha_composite(up(legs), (40, 10))
    p.paste((70, 54, 44, 255), (0, ph - 70, pw, ph))
    p.alpha_composite(up(shoe), (pw // 2 - shoe.width * S // 2 + 60, ph - 70 - shoe.height * S + 14))
    sheet.alpha_composite(p, (i * (pw + 30), 0))
sheet.convert("RGB").save(PREV / "reveal_legs.png")

# --- cast shadow: on its own, and behind the legs on a lit wall
sw, sh = shadow.width * S + 40, shadow.height * S + 40
cw = legs.width * S + 40 + 40 + 20 * S
sheet = Image.new("RGBA", (sw + cw + 30, max(sh, legs.height * S + 40)), (8, 8, 12, 255))
p1 = panel((sw, sheet.height), (150, 144, 152))
p1.alpha_composite(up(shadow), (20, 20))
sheet.alpha_composite(p1, (0, 0))
p2 = panel((cw, sheet.height), (150, 144, 152))
p2.alpha_composite(up(shadow), (-shadow.width * S // 5 + 20 * S + 20, 14))
p2.alpha_composite(up(legs), (20, 20))
sheet.alpha_composite(p2, (sw + 30, 0))
sheet.convert("RGB").save(PREV / "reveal_cast_shadow.png")

# --- the close-up still at 4x (1280 x 720)
still.resize((320 * S, 180 * S), Image.NEAREST).convert("RGB").save(PREV / "reveal_still.png")
still.resize((960, 540), Image.NEAREST).convert("RGB").save(OUT / "_still_3x.png")
up(legs, 8).save(OUT / "_legs_8x.png")
print("previews written; legs", legs.size, "shadow", shadow.size)
