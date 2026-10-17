"""Regenerates D:/Unity/RemnantArt/_previews/dialogue_lines.md from the DialogueData assets (what the game really plays),
the diary pages in the scene files and the Shadow's encounter captions. Run:  python Tools/gen_dialogue_table.py"""
import re, glob, os

SPK = ["Protagonist", "Shadow", "Narration", "Aiko", "Reina", "Mika", "Saki"]
MODE = ["NORMAL", "TENSE", "AUTO"]
D = r"D:/Unity/Remnant/Assets/_Remnant/Data/Dialogue/"

# asset -> (scene, trigger), in story order
ORDER = [
    ("Day1_Intro", "Present_Classroom, Day 1", "Scene start"),
    ("Act1_Chalkboard_Examine", "Present_Classroom, Day 1", "Chalkboard (optional, before the bullying)"),
    ("Day1_Hangman", "Present_Classroom, Day 1", "Hangman corner (optional, before the bullying)"),
    ("Day1_Bullying", "Present_Classroom, Day 1", "Bullying trigger (a few steps into the room)"),
    ("Day1_Freeze", "Present_Classroom, Day 1", "Freeze trigger (before her desk); the invisible wall appears"),
    ("Day1_Door_Leave", "Present_Classroom, Day 1", "Door, after the freeze; leads to Day 2"),
    ("Day2_Intro", "Present_Classroom, Day 2", "Scene start"),
    ("Day2_Girls", "Present_Classroom, Day 2", "Girls trigger (entering)"),
    ("Chalkboard_Day2", "Present_Classroom, Day 2", "Hangman corner (optional)"),
    ("Day2_AikoDesk_Examine", "Present_Classroom, Day 2", "Her desk"),
    ("Day2_Awakening", "Present_Classroom, Day 2", "Right after the desk lines (the lights flicker)"),
    ("Day2_Shadow_Awaken_Returning", "Present_Classroom, Day 2", "REPLAY ONLY (game completed before), before the Shadow's lines"),
    ("Day2_Shadow_Awaken", "Present_Classroom, Day 2", "The Shadow awakens, always; then the shift to the Future"),
    ("Future_Corridor_Intro", "Future_Corridor", "Arrival trigger"),
    ("Future_Corridor_Monster", "Future_Corridor", "The first time the monster is on screen"),
    ("Future_Corridor_DriveBack", "Future_Corridor", "The first time the monster has been driven back"),
    ("Future_Door_Locked", "Future_Corridor / 2-B / Storage_Hallway", "Locked doors"),
    ("Classroom2B_Intro", "Future_Classroom2B", "Entering"),
    ("Page1_Reaction", "Future_Classroom2B", "After diary page 1 closes"),
    ("Page4_Reaction", "Future_Classroom2B", "After diary page 4 closes"),
    ("Page5_Reaction", "Future_Classroom2B", "After diary page 5 closes"),
    ("Page7_Reaction", "Future_Classroom2B", "After diary page 7 closes"),
    ("Chalkboard_Future2B", "Future_Classroom2B", "Chalkboard (the finished hangman)"),
    ("Mirror_Examine", "Future_Classroom2B", "Cracked mirror"),
    ("Classroom2B_Desk", "Future_Classroom2B", "Her desk, only after pages 1-7 (then page 8 opens)"),
    ("Diary_Complete_Shadow", "Future_Classroom2B", "After page 8 closes; unlocks the back door"),
    ("Storage_Arrival", "Storage_Hallway", "Entering"),
    ("Storage_Walk_Shadow", "Storage_Hallway", "During the slow walk (the player keeps walking)"),
    ("Storage_Reveal_Shadow", "Storage_Hallway", "After the reveal still (the reveal itself has no dialogue)"),
    ("Act4_Intro", "Present_Classroom, Back to Day 1", "Scene start"),
    ("Act4_Bullying", "Present_Classroom, Back to Day 1", "Bullying trigger (replay of the Day 1 lines)"),
    ("Act4_Shadow_WalkPast", "Present_Classroom, Back to Day 1", "Walking toward her desk"),
    ("Act4_LightsDie", "Present_Classroom, Back to Day 1", "The lights die (then Act4_Encounter)"),
    ("Act4_Reunion_Talk", "Present_Classroom, reunion", "When she reaches you"),
    ("Act4_Reunion_Shadow", "Present_Classroom, reunion", "The Shadow fades"),
    ("Act4_Confront", "Present_Classroom, step in", "Lights return, automatic"),
    ("Act4_Aiko_Talk", "Present_Classroom, step in", "After the girls leave, automatic"),
    ("Epilogue_Intro", "Present_Classroom, epilogue", "Scene start"),
    ("Epilogue_Aiko_Morning", "Present_Classroom, epilogue", "Talking to Aiko (then her diary page)"),
    ("Epilogue_Chalkboard", "Present_Classroom, epilogue", "Chalkboard"),
    ("Epilogue_AikoDesk", "Present_Classroom, epilogue", "Her desk"),
    ("Epilogue_Storage", "Storage_Hallway, daylight", "The open door at the end (then the credits)"),
]
UNUSED = ["Future_Door_2B", "Desk_Examine", "Key_Pickup"]


def yaml_text(raw):
    """Unity's YAML scalar -> text: single quotes stripped, '' unescaped, a blank line is a real line break, a folded newline a space."""
    raw = raw.strip()
    if raw.startswith("'") and raw.endswith("'"):
        raw = raw[1:-1].replace("''", "'")
    elif raw.startswith('"') and raw.endswith('"'):
        raw = raw[1:-1].replace('\\"', '"')
    parts = re.split(r"\n[ \t]*\n", raw)
    return " / ".join(re.sub(r"\s*\n\s*", " ", x).strip() for x in parts)


def lines_of(name):
    t = open(D + name + ".asset", encoding="utf8").read().split("lines:", 1)[1]
    parts = re.split(r"\n  - speaker: ", "\n" + t)
    out = []
    for p in parts[1:]:
        m = re.match(r"(\d+)\s+text: (.*?)\s+mode: (\d+)(?:\s+fadeColor: (\d))?(?:.*?fadeLabel: (\d))?", p, re.S)
        txt = re.sub(r"\s*\n\s+", " ", m.group(2)).strip()
        if txt.startswith("'") and txt.endswith("'"):
            txt = txt[1:-1].replace("''", "'")
        elif txt.startswith('"') and txt.endswith('"'):
            txt = txt[1:-1].replace('\\"', '"')
        note = ""
        if m.group(4) == "1":
            note = " + text colour fade crimson to white" + (" (label too)" if m.group(5) == "1" else "")
        out.append((SPK[int(m.group(1))], txt, MODE[int(m.group(3))] + note))
    return out


rows = []
n = 0
out = ["# Remnant - every line the game plays", "",
       "Generated from the DialogueData assets by Tools/gen_dialogue_table.py. Source of truth for the wording: Docs/SCRIPT.md.", "",
       "## Dialogue box lines", "",
       "| # | Scene | When | Asset | Speaker | Text | Mode |", "|---|---|---|---|---|---|---|"]
for name, scene, when in ORDER:
    for sp, tx, md in lines_of(name):
        n += 1
        out.append("| %d | %s | %s | %s | %s | %s | %s |" % (n, scene, when, name, sp, tx.replace("|", "\\|"), md))

out += ["", "## Diary pages (page panel, Future_Classroom2B and the epilogue)", "",
        "| Page | Date header | Text | After closing |", "|---|---|---|---|"]
scene2b = open(r"D:/Unity/Remnant/Assets/_Remnant/Scenes/Future_Classroom2B.unity", encoding="utf8").read()
pages = {}
for blk in re.split(r"\n(?=--- !u!)", scene2b):
    m = re.search(r"pageNumber: (\d)", blk)
    if m and "dateHeader" in blk:
        date = re.search(r"dateHeader: (.*)", blk).group(1).strip()
        body = yaml_text(re.search(r'pageText: (.*?)\n  panel:', blk, re.S).group(1))
        pages[int(m.group(1))] = (date, body)
after = {1: "Page1_Reaction", 4: "Page4_Reaction", 5: "Page5_Reaction", 7: "Page7_Reaction", 8: "Diary_Complete_Shadow"}
for k in sorted(pages):
    out.append("| %d | %s | %s | %s |" % (k, pages[k][0], pages[k][1].replace("|", "\\|"), after.get(k, "-")))
pres = open(r"D:/Unity/Remnant/Assets/_Remnant/Scenes/Present_Classroom.unity", encoding="utf8").read()
m = re.search(r"pageTitle: (.*?)\n\s+pageText: (.*?)\n\s+dialogueRunner", pres, re.S)
if m:
    out.append("| epilogue | %s | %s | - |" % (m.group(1).strip(), yaml_text(m.group(2))))

out += ["", "## The Shadow's captions in the dark classroom (non-blocking, crimson, typed out with jitter)", "",
        "| When | Text |", "|---|---|",
        "| The ghost appears | Light! Keep it on her! |",
        "| The light first hits her | Look at her flinch. Just like in class. |",
        "| She keeps coming (second appearance) | Don't let her near you. She ruins everyone who gets close. |",
        "| The [F] Lower the light prompt appears | Don't you dare. |",
        "| 4 s after the prompt, light not lowered | Put it back on her! PUT IT BACK\u2014 |",
        "| The light is lowered | (the caption cuts off instantly, mid-word; silence) |",
        "", "## Not played in the build", "",
        "Sandbox scene and unused assets, left alone: " + ", ".join(UNUSED) + "."]
open(r"D:/Unity/RemnantArt/_previews/dialogue_lines.md", "w", encoding="utf8").write("\n".join(out) + "\n")

# every asset in the folder must be accounted for
known = set(x[0] for x in ORDER) | set(UNUSED)
folder = set(os.path.basename(f)[:-6] for f in glob.glob(D + "*.asset"))
print("lines:", n, "| assets not in the table:", sorted(folder - known), "| table entries without asset:", sorted(known - folder))
