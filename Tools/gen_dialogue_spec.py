"""Turns Docs/SCRIPT.md (via parse_script.py) into the spec file that the Unity-side importer reads.
Spec format, one record per line:
  ASSET|<asset name>|<lockPlayer 1/0>
  L|<speaker index>|<mode 0/1/2>|<fade 0/1>|<fadeLabel 0/1>|<text>
  PAGE|<n>|<date>|<body with \\n escapes>
Speaker indices follow the Speaker enum: Protagonist 0, Shadow 1, Narration 2, Aiko 3, Reina 4, Mika 5, Saki 6."""
import sys
sys.path.insert(0, r"D:/Unity/Remnant/Tools")
from parse_script import parse

SPK = {"Protagonist": 0, "Shadow": 1, "Narration": 2, "Aiko": 3, "Reina": 4, "Mika": 5, "Saki": 6}
MODE = {"NORMAL": 0, "TENSE": 1, "AUTO": 2}
blocks, pages = parse()


def block(heading_start, title_start):
    found = [b for b in blocks if b["heading"].startswith(heading_start) and b["title"].startswith(title_start) and b["lines"]]
    assert len(found) == 1, (heading_start, title_start, len(found))
    return found[0]["lines"]


D1, D2, FC, C2B, SH, A4, EP = "DAY 1", "DAY 2", "FUTURE CORRIDOR", "CLASSROOM 2-B", "STORAGE HALLWAY", "BACK TO DAY 1", "EPILOGUE"

assets = []   # (name, lockPlayer, lines, fadeLastLine)


def add(name, lines, lock=True, fade_last=False):
    assets.append((name, lock, list(lines), fade_last))


bully = block(D1, "[Bullying trigger")
replay_end = next(i for i, (sp, tx, md) in enumerate(bully) if sp == "Reina" and tx.startswith('"Please."'))

add("Day1_Intro", block(D1, "[Scene start"))
add("Act1_Chalkboard_Examine", block(D1, "Chalkboard:"))
add("Day1_Hangman", block(D1, "Hangman:"))
add("Day1_Bullying", bully)
add("Day1_Freeze", block(D1, "[Control returns"))
add("Day1_Door_Leave", block(D1, "[Exit trigger"))

add("Day2_Intro", block(D2, "[Scene start"))
add("Day2_Girls", block(D2, "[Entering, the girls"))
add("Day2_AikoDesk_Examine", block(D2, "[Desk interaction]"))
add("Chalkboard_Day2", block(D2, "[Hangman interaction"))
add("Day2_Awakening", block(D2, "[Awakening"))
add("Day2_Shadow_Awaken_Returning", block(D2, "Shadow awakening — REPLAY"))
add("Day2_Shadow_Awaken", block(D2, "Shadow awakening — ALWAYS"))

add("Future_Corridor_Intro", block(FC, "[Arrival]"))
add("Future_Corridor_Monster", block(FC, "[First sight"))
add("Future_Corridor_DriveBack", block(FC, "[After the player drives"))

add("Classroom2B_Intro", block(C2B, "[Entering]"))
add("Chalkboard_Future2B", block(C2B, "[Hangman — finished]"))
add("Mirror_Examine", block(C2B, "[Mirror]"))
add("Classroom2B_Desk", block(C2B, "[Her desk"))
add("Diary_Complete_Shadow", block(C2B, "[After page 8 closes]"))
for n in (1, 4, 5, 7):
    add("Page%d_Reaction" % n, pages[n]["reaction"])

add("Storage_Arrival", block(SH, "[Entering]"))
add("Storage_Walk_Shadow", block(SH, "[During the slow walk]"), lock=False)
add("Storage_Reveal_Shadow", block(SH, "[The reveal plays"))

add("Act4_Intro", block(A4, "[Scene start"))
add("Act4_Bullying", bully[:replay_end + 1])
walk = block(A4, "[As the player walks")
assert walk[-1][1] == "The lights die."
add("Act4_Shadow_WalkPast", walk[:-1])
add("Act4_LightsDie", walk[-1:])
add("Act4_Reunion_Talk", block(A4, "[When she reaches you]"))
add("Act4_Reunion_Shadow", block(A4, "[The Shadow fades]"), fade_last=True)
add("Act4_Confront", block(A4, "[Lights return"))
add("Act4_Aiko_Talk", block(A4, "[The girls leave]"))

ep_start = block(EP, "(EPILOGUE start)")
assert ep_start[0][1] == "The next morning."
add("Epilogue_Intro", ep_start[:1])
add("Epilogue_Aiko_Morning", ep_start[1:])
add("Epilogue_Chalkboard", block(EP, "[Chalkboard]"))
add("Epilogue_AikoDesk", block(EP, "[Her desk]"))
add("Epilogue_Storage", block(EP, "[Daylight walk"))

out = []
count = 0
for name, lock, lines, fade_last in assets:
    out.append("ASSET|%s|%d" % (name, 1 if lock else 0))
    for i, (sp, tx, md) in enumerate(lines):
        fade = 1 if (fade_last and i == len(lines) - 1) else 0
        assert "|" not in tx
        out.append("L|%d|%d|%d|%d|%s" % (SPK[sp], MODE[md], fade, fade, tx))
        count += 1
for n, p in sorted(pages.items()):
    out.append("PAGE|%d|%s|%s" % (n, p["date"], "\\n".join(p["body"])))

open(r"D:/Unity/RemnantArt/_work/dialogue_spec.txt", "w", encoding="utf8").write("\n".join(out) + "\n")
print("assets", len(assets), "lines", count, "pages", len(pages))
