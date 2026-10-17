"""Prints name, parent and local position/scale of every GameObject whose name matches a pattern. Usage: python scene_pos.py Scene regex"""
import re, sys
scene, pat = sys.argv[1], sys.argv[2]
t = open(rf"D:/Unity/Remnant/Assets/_Remnant/Scenes/{scene}.unity", encoding="utf8").read()
docs = re.split(r"\n(?=--- !u!)", t)
names = {}; trans = {}; go_of = {}
for d in docs:
    m = re.match(r"--- !u!(\d+) &(\d+)", d)
    if not m: continue
    typ, fid = m.group(1), m.group(2)
    if typ == "1": names[fid] = re.search(r"m_Name: (.*)", d).group(1)
    if typ in ("4", "224"):
        go = re.search(r"m_GameObject: \{fileID: (\d+)\}", d).group(1)
        pos = re.search(r"m_LocalPosition: \{x: ([-\d.e]+), y: ([-\d.e]+), z: ([-\d.e]+)\}", d)
        sc = re.search(r"m_LocalScale: \{x: ([-\d.e]+), y: ([-\d.e]+)", d)
        fa = re.search(r"m_Father: \{fileID: (\d+)\}", d).group(1)
        trans[fid] = (go, pos.groups() if pos else None, sc.groups() if sc else None, fa)
        go_of[go] = fid
for fid, (go, pos, sc, fa) in trans.items():
    n = names.get(go, "?")
    if re.search(pat, n):
        pn = names.get(trans[fa][0], "root") if fa in trans else "root"
        print(f"{n:28s} parent={pn:16s} pos={tuple(round(float(v),2) for v in pos) if pos else None} scale={tuple(round(float(v),2) for v in sc) if sc else None}")
