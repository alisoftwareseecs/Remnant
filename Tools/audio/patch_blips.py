"""One-off patch: dialogue blips follow the typing (per-speaker setting, per-line override)."""
from pathlib import Path

ROOT = Path(r"D:/Unity/Remnant/Assets/_Remnant/Scripts")


def patch(rel, pairs):
    p = ROOT / rel
    raw = p.read_bytes().decode("utf-8")
    crlf = "\r\n" in raw
    s = raw.replace("\r\n", "\n")
    for old, new, marker in pairs:
        if marker in s:
            print("skip (already)", rel, marker[:40])
            continue
        assert old in s, (rel, old[:70])
        s = s.replace(old, new, 1)
    if crlf:
        s = s.replace("\n", "\r\n")
    p.write_bytes(s.encode("utf-8"))
    print("patched", rel)


patch("Dialogue/DialogueData.cs", [
    ("    public enum DialogueMode { Normal, Tense, Auto }\n",
     "    public enum DialogueMode { Normal, Tense, Auto }\n\n"
     "    /// <summary>Per-line override of the typing blips. Default = the speaker's setting (and the parenthesis rule).</summary>\n"
     "    public enum BlipOverride { Default, On, Off }\n",
     "enum BlipOverride"),
    ("        public bool fadeLabel;\n    }\n",
     "        public bool fadeLabel;\n\n"
     "        [Tooltip(\"Default: the speaker's blip setting decides (and a protagonist line in parentheses is a thought: no blips). On / Off force it for this line. Auto lines never blip.\")]\n"
     "        public BlipOverride blips;\n    }\n",
     "public BlipOverride blips;"),
])

patch("Dialogue/DialogueRunner.cs", [
    ("        [Header(\"Typing sounds (clips are optional)\")]\n"
     "        [SerializeField] private AudioSource blipSource;\n"
     "        [SerializeField] private AudioClip defaultBlip;\n"
     "        [SerializeField] private AudioClip shadowBlip;\n"
     "        [SerializeField, Min(1)] private int blipEveryCharacters = 2;\n"
     "        [SerializeField] private Vector2 blipPitchRange = new Vector2(0.95f, 1.05f);\n",
     "        [Serializable]\n"
     "        private struct SpeakerBlip\n"
     "        {\n"
     "            public Speaker speaker;\n"
     "            [Tooltip(\"Typing blips for this speaker. A line can override it (DialogueLine.blips).\")]\n"
     "            public bool blips;\n"
     "        }\n\n"
     "        [Header(\"Typing blips (clips are optional)\")]\n"
     "        [Tooltip(\"Goes through the SFX mixer group.\")]\n"
     "        [SerializeField] private AudioSource blipSource;\n"
     "        [SerializeField] private AudioClip defaultBlip;\n"
     "        [SerializeField] private AudioClip shadowBlip;\n"
     "        [SerializeField, Range(0f, 1f)] private float defaultBlipVolume = 0.125f;\n"
     "        [SerializeField, Range(0f, 1f)] private float shadowBlipVolume = 0.1f;\n"
     "        [Tooltip(\"At least this long between two blips, so fast typing does not turn into a buzz.\")]\n"
     "        [SerializeField, Min(0f)] private float minBlipGapSeconds = 0.055f;\n"
     "        [SerializeField] private Vector2 blipPitchRange = new Vector2(0.95f, 1.05f);\n"
     "        [Tooltip(\"Which speakers blip. The Narration never does; the protagonist only when he speaks aloud (not in parentheses).\")]\n"
     "        [SerializeField] private SpeakerBlip[] speakerBlips = new SpeakerBlip[]\n"
     "        {\n"
     "            new SpeakerBlip { speaker = Speaker.Protagonist, blips = true },\n"
     "            new SpeakerBlip { speaker = Speaker.Shadow, blips = true },\n"
     "            new SpeakerBlip { speaker = Speaker.Narration, blips = false },\n"
     "            new SpeakerBlip { speaker = Speaker.Aiko, blips = true },\n"
     "            new SpeakerBlip { speaker = Speaker.Reina, blips = true },\n"
     "            new SpeakerBlip { speaker = Speaker.Mika, blips = true },\n"
     "            new SpeakerBlip { speaker = Speaker.Saki, blips = true },\n"
     "        };\n",
     "private struct SpeakerBlip"),
    ("            AudioClip blip = line.mode == DialogueMode.Auto ? null : (shadow ? shadowBlip : defaultBlip);\n",
     "            AudioClip blip = BlipsFor(line) ? (shadow ? shadowBlip : defaultBlip) : null;\n            float blipVolume = shadow ? shadowBlipVolume : defaultBlipVolume;\n            float lastBlipAt = -10f;\n",
     "BlipsFor(line)"),
    ("            int shown = 0;\n            int blipCounter = 0;\n",
     "            int shown = 0;\n",
     "int shown = 0;\n            int blipCounter"),
    ("                    if (blip != null && !char.IsWhiteSpace(c) && !char.IsPunctuation(c))\n"
     "                    {\n"
     "                        if (blipCounter % Mathf.Max(1, blipEveryCharacters) == 0) PlayBlip(blip);\n"
     "                        blipCounter++;\n"
     "                    }\n",
     "                    // one blip per typed letter or digit (never spaces or punctuation), at most one per minimum gap\n"
     "                    if (blip != null && !char.IsWhiteSpace(c) && !char.IsPunctuation(c) && Time.unscaledTime - lastBlipAt >= minBlipGapSeconds)\n"
     "                    {\n"
     "                        lastBlipAt = Time.unscaledTime;\n"
     "                        PlayBlip(blip, blipVolume);\n"
     "                    }\n",
     "lastBlipAt = Time.unscaledTime;"),
    ("            bodyText.maxVisibleCharacters = int.MaxValue;\n            if (line.fadeColor)\n            {\n                bodyText.color = Opaque(line.fadeTo);",
     "            // typing is over (finished or skipped): the blip stops at once\n            if (blipSource != null) blipSource.Stop();\n\n            bodyText.maxVisibleCharacters = int.MaxValue;\n            if (line.fadeColor)\n            {\n                bodyText.color = Opaque(line.fadeTo);",
     "typing is over (finished or skipped)"),
    ("        private void PlayBlip(AudioClip clip)\n        {\n            if (blipSource == null || clip == null) return;\n            blipSource.pitch = UnityEngine.Random.Range(blipPitchRange.x, blipPitchRange.y);\n            blipSource.PlayOneShot(clip);\n",
     "        /// <summary>\n"
     "        /// Does this line blip while it types? Auto never; a per-line override wins; otherwise the speaker's setting, except\n"
     "        /// that a protagonist line in parentheses is a thought and stays silent.\n"
     "        /// </summary>\n"
     "        private bool BlipsFor(DialogueLine line)\n"
     "        {\n"
     "            if (line.mode == DialogueMode.Auto) return false;\n"
     "            if (line.blips == BlipOverride.On) return true;\n"
     "            if (line.blips == BlipOverride.Off) return false;\n\n"
     "            bool speakerBlips = false;\n"
     "            for (int i = 0; i < this.speakerBlips.Length; i++)\n"
     "            {\n"
     "                if (this.speakerBlips[i].speaker == line.speaker) speakerBlips = this.speakerBlips[i].blips;\n"
     "            }\n\n"
     "            if (!speakerBlips) return false;\n"
     "            if (line.speaker == Speaker.Protagonist)\n"
     "            {\n"
     "                string text = (line.text ?? string.Empty).Trim();\n"
     "                if (text.StartsWith(\"(\")) return false;\n"
     "            }\n\n"
     "            return true;\n"
     "        }\n\n"
     "        private void PlayBlip(AudioClip clip, float volume)\n        {\n            if (blipSource == null || clip == null) return;\n            blipSource.pitch = UnityEngine.Random.Range(blipPitchRange.x, blipPitchRange.y);\n            blipSource.PlayOneShot(clip, volume);\n",
     "private bool BlipsFor(DialogueLine line)"),
    ("            IsOpen = false;\n            CurrentDialogue = null;\n            indicatorActive = false;\n",
     "            IsOpen = false;\n            CurrentDialogue = null;\n            indicatorActive = false;\n            if (blipSource != null) blipSource.Stop();\n",
     "if (blipSource != null) blipSource.Stop();\n            endFrame"),
])
