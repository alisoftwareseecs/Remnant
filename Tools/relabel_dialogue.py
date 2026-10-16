"""One-off: the rewritten Day 1 / Day 2 NPC lines use named speakers instead of quoted narration."""
import re

D = r"D:\Unity\Remnant\Assets\_Remnant\Data\Dialogue" + "\\"

# file, speaker value, new text (as YAML scalar)
changes = [
    ("Act1_Aiko_Talk.asset", 3, "...You should go. They're right about me.", "...You should go. They''re right about me."),
    ("Act1_Bully1_Talk.asset", 4, "Don't stand so close. She's dangerous.", None),
    ("Act1_Bully2_Talk.asset", 5, "Ask her what happened to her dad.", None),
    ("Act1_Bully3_Talk.asset", 6, "Mind your own business. Unless you want to be next.", None),
    ("Day2_Bully1_Talk.asset", 4, "Killers don't come to class anyway.", None),
    ("Day2_Bully2_Talk.asset", 5, "Good riddance.", None),
    ("Day2_Bully3_Talk.asset", 6, "What, you miss her?", None),
]

for name, speaker, text, _ in changes:
    s = open(D + name, encoding="utf-8").read()
    lines = s.split("\n")
    out = []
    i = 0
    done = False
    while i < len(lines):
        line = lines[i]
        if not done and line.strip().startswith("text:") and '"' in line and "\\" not in line.replace('\\"', '') or \
           (not done and line.strip().startswith("text: '\"") ):
            # replace the quoted NPC line and the speaker line before it
            out[-1] = "  - speaker: %d" % speaker
            scalar = text.replace("'", "''")
            out.append("    text: '%s'" % scalar if ("'" in text or ":" in text or text.startswith(".")) else "    text: %s" % text)
            done = True
        else:
            out.append(line)
        i += 1
    assert done, name
    open(D + name, "w", encoding="utf-8", newline="").write("\n".join(out))
    print(name, "ok")
