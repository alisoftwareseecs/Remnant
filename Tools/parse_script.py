"""Parses Docs/SCRIPT.md into dialogue blocks with exact text and the delivery mode of every line.
Rules (from the script header): mode is NORMAL unless a [TENSE]/[AUTO]/[NORMAL] marker applies; a marker holds until the next
marker or ## heading. Lines in the diary pages are separate (pages)."""
import re, json, sys
SPEAKERS = ["Narration", "Protagonist", "Aiko", "Reina", "Mika", "Saki", "Shadow"]
LINE = re.compile(r"^\s*(?:→ after closing:\s*)?(Narration|Protagonist|Aiko|Reina|Mika|Saki|Shadow):\s?(.*)$")
FLASH_KEY = None   # the script now carries the literal hint text

def parse(path=r"D:/Unity/Remnant/Docs/SCRIPT.md"):
    blocks = []      # each: dict(title, heading, lines=[(speaker,text,mode)])
    pages = {}       # n -> dict(date, body, reaction=[(speaker,text,mode)])
    mode = "NORMAL"; heading = ""; cur = None; page = None
    for raw in open(path, encoding="utf8").read().split("\n"):
        s = raw.rstrip()
        if s.startswith("## "):
            heading = s[3:]; mode = "NORMAL"; page = None
            cur = {"title": "(" + heading + " start)", "heading": heading, "lines": []}; blocks.append(cur); continue
        m = re.match(r"^\[(TENSE|AUTO|NORMAL)\]\s*$", s)
        if m:
            mode = m.group(1); continue
        pm = re.match(r"^(PAGE (\d) — (.+)|EPILOGUE PAGE — (.+))$", s)
        if pm:
            n = int(pm.group(2)) if pm.group(2) else 9
            date = pm.group(3) or pm.group(4)
            page = pages.setdefault(n, {"date": date, "body": [], "reaction": []}); cur = None; continue
        lm = LINE.match(s)
        if page is not None:
            if lm and ("→ after closing:" in s or raw.startswith("      ")):
                text = lm.group(2)
                page["reaction"].append((lm.group(1), text, mode))
            elif s.strip() and not s.startswith("["):
                page["body"].append(s.strip())
            elif s.startswith("["):
                page = None
            if page is not None: continue
        if s.startswith("[") or re.match(r"^(Chalkboard|Hangman):$", s) or s.startswith("Shadow awakening") or s.startswith("When ") or s.startswith("If F not"):
            cur = {"title": s, "heading": heading, "lines": []}; blocks.append(cur)
            if s.startswith("When ") or s.startswith("If F not"):
                cur["caption"] = True
            continue
        if lm and cur is not None and not cur.get("caption"):
            text = lm.group(2)
            if "←" in text: text = text.split("←")[0].rstrip()
            pass
            cur["lines"].append((lm.group(1), text, mode))
    return blocks, pages

if __name__ == "__main__":
    blocks, pages = parse()
    for i, b in enumerate(blocks):
        if not b["lines"]: continue
        print(f"#{i} [{b['heading'][:22]}] {b['title'][:70]}")
        for sp, tx, md in b["lines"]:
            print(f"     {md:6s} {sp:11s} {tx}")
    for n, p in sorted(pages.items()):
        print(f"PAGE {n}: {p['date']} | body lines={len(p['body'])} | reaction={[(s,t,m) for s,t,m in p['reaction']]}")
