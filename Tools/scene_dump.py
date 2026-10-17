"""Lists the story-related objects of a scene: GameObject name, project components, and their dialogue/flag/scene fields.
Usage: python scene_dump.py Present_Classroom"""
import re, sys, glob, os
root = r"D:/Unity/Remnant/Assets"
guid2cls = {}
for f in glob.glob(root + "/_Remnant/Scripts/**/*.cs", recursive=True):
    m = open(f + ".meta", encoding="utf8").read()
    g = re.search(r"guid: (\w+)", m).group(1)
    guid2cls[g] = os.path.basename(f)[:-3]
guid2asset = {}
for f in glob.glob(root + "/_Remnant/Data/Dialogue/*.asset"):
    g = re.search(r"guid: (\w+)", open(f + ".meta", encoding="utf8").read()).group(1)
    guid2asset[g] = os.path.basename(f)[:-6]
scene = sys.argv[1]
t = open(root + f"/_Remnant/Scenes/{scene}.unity", encoding="utf8").read()
docs = re.split(r"\n(?=--- !u!)", t)
objs = {}   # fileID -> dict
comps = {}  # goID -> list
for d in docs:
    m = re.match(r"--- !u!(\d+) &(\d+)", d)
    if not m: continue
    typ, fid = m.group(1), m.group(2)
    if typ == "1":
        name = re.search(r"m_Name: (.*)", d).group(1)
        objs[fid] = name
    elif typ == "114":
        go = re.search(r"m_GameObject: \{fileID: (\d+)\}", d)
        sg = re.search(r"m_Script: \{fileID: \d+, guid: (\w+)", d)
        if not go or not sg: continue
        cls = guid2cls.get(sg.group(1))
        if not cls: continue
        info = []
        for line in d.split("\n"):
            lm = re.match(r"\s+(\w+): (.*)", line)
            if lm and lm.group(1) not in ("m_ObjectHideFlags", "m_CorrespondingSourceObject", "m_PrefabInstance", "m_PrefabAsset", "m_GameObject", "m_Enabled", "m_EditorHideFlags", "m_Script", "m_Name", "m_EditorClassIdentifier"):
                v = lm.group(2).strip()
                gm = re.search(r"guid: (\w+)", v)
                if gm and gm.group(1) in guid2asset: v = "ASSET:" + guid2asset[gm.group(1)]
                elif "fileID: 0}" in v: continue
                if v in ("", "0", "[]") : continue
                if re.match(r"\{fileID: \d+\}$", v):
                    v = "->" + objs.get(re.search(r"\d+", v).group(0), "obj" + re.search(r"\d+", v).group(0)) if False else "->ref"
                info.append(f"{lm.group(1)}={v}")
        comps.setdefault(go.group(1), []).append((cls, info))
for go, lst in comps.items():
    print(f"== {objs.get(go, go)}")
    for cls, info in lst:
        print(f"   [{cls}] " + "; ".join(info))
