"""Checks that the DialogueData assets and the diary pages in the scenes match Docs/SCRIPT.md exactly. Run: python Tools/verify_script.py"""
import sys, re
sys.path.insert(0, r"D:/Unity/Remnant/Tools")
from parse_script import parse
import importlib.util

spec = importlib.util.spec_from_file_location("tbl", r"D:/Unity/Remnant/Tools/gen_dialogue_table.py")
src = open(r"D:/Unity/Remnant/Tools/gen_dialogue_table.py", encoding="utf8").read().split("rows = []")[0]
ns = {}
exec(compile(src, "tbl", "exec"), ns)
lines_of, ORDER, UNUSED = ns["lines_of"], ns["ORDER"], ns["UNUSED"]

blocks, pages = parse()
script = set()
for b in blocks:
    for sp, tx, md in b["lines"]:
        script.add((sp, tx, md))
for n, p in pages.items():
    for sp, tx, md in p["reaction"]:
        script.add((sp, tx, md))

assets = set()
for name, scene, when in ORDER:
    if name == "Future_Door_Locked":
        continue   # "Locked." stays as it is
    for sp, tx, md in lines_of(name):
        md = md.split(" +")[0]
        assets.add((sp, tx, md))

missing = sorted(script - assets)
extra = sorted(assets - script)
print("script lines:", len(script), "| asset lines:", len(assets))
print("in the script but not in an asset:", missing)
print("in an asset but not in the script:", extra)

# diary pages in the scene files vs the script
scene2b = open(r"D:/Unity/Remnant/Assets/_Remnant/Scenes/Future_Classroom2B.unity", encoding="utf8").read()
ok = True
for blk in re.split(r"\n(?=--- !u!)", scene2b):
    m = re.search(r"pageNumber: (\d)", blk)
    if m and "dateHeader" in blk:
        n = int(m.group(1))
        date = re.search(r"dateHeader: (.*)", blk).group(1).strip()
        body = ns["yaml_text"](re.search(r"pageText: (.*?)\n  panel:", blk, re.S).group(1))
        want = " / ".join(pages[n]["body"])
        if date != pages[n]["date"] or body != want:
            ok = False
            print("PAGE MISMATCH", n, date, "|", body, "|", want)
print("diary pages match the script:", ok)
sys.exit(0 if (not missing and not extra and ok) else 1)
