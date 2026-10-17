"""One-off patch: hooks the audio system into the existing scripts. Safe to re-run (each patch checks its marker)."""
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
        assert old in s, (rel, old[:60])
        s = s.replace(old, new, 1)
    if crlf:
        s = s.replace("\n", "\r\n")
    p.write_bytes(s.encode("utf-8"))
    print("patched", rel)


# ---- DialogueRunner: per-line event
patch("Dialogue/DialogueRunner.cs", [
    ("        public event Action Ended;\n",
     "        public event Action Ended;\n        /// <summary>Raised when a line starts (the dialogue and the index of the line in it).</summary>\n        public event Action<DialogueData, int> LineStarted;\n",
     "LineStarted"),
    ("                currentLineStartedAt = Time.unscaledTime;\n",
     "                currentLineStartedAt = Time.unscaledTime;\n                LineStarted?.Invoke(data, i);\n",
     "LineStarted?.Invoke"),
])

# ---- SceneTransition: flashing distort with synced music slices
patch("Core/SceneTransition.cs", [
    ("using System.Collections;\nusing Remnant.Player;",
     "using System.Collections;\nusing System.Collections.Generic;\nusing Remnant.Audio;\nusing Remnant.Player;",
     "using Remnant.Audio;"),
    ("        [Header(\"Distort style\")]\n        [SerializeField, Min(0f)] private float distortSeconds = 0.9f;\n",
     "        [Header(\"Distort style\")]\n        [SerializeField, Min(0f)] private float distortSeconds = 0.9f;\n        [Tooltip(\"Length of one red flash (random between the two).\")]\n        [SerializeField] private Vector2 redFlashSeconds = new Vector2(0.04f, 0.1f);\n        [Tooltip(\"Length of one black frame between two red flashes (random between the two).\")]\n        [SerializeField] private Vector2 blackFrameSeconds = new Vector2(0.03f, 0.07f);\n        [Tooltip(\"How long the last, solid red lasts before the screen goes black.\")]\n        [SerializeField, Min(0f)] private float finalRedSeconds = 0.08f;\n",
     "redFlashSeconds"),
    ("            float fadeOutOverride = -1f)\n        {\n            if (IsTransitioning) return;",
     "            float fadeOutOverride = -1f, bool flashMusic = false)\n        {\n            if (IsTransitioning) return;",
     "bool flashMusic = false"),
    ("StartCoroutine(Run(sceneName, spawnId, title, style, flagToSet, worldPosition, fadeOutOverride));",
     "StartCoroutine(Run(sceneName, spawnId, title, style, flagToSet, worldPosition, fadeOutOverride, flashMusic));",
     "fadeOutOverride, flashMusic));"),
    ("            string flagToSet, Vector2? worldPosition, float fadeOutOverride)\n        {",
     "            string flagToSet, Vector2? worldPosition, float fadeOutOverride, bool flashMusic)\n        {",
     "float fadeOutOverride, bool flashMusic)"),
    ("            if (style == TransitionStyle.Distort) yield return Distort();",
     "            if (style == TransitionStyle.Distort) yield return Distort(flashMusic);",
     "Distort(flashMusic)"),
])


def replace_between(rel, start_marker, end_marker, new_block, done_marker):
    p = ROOT / rel
    raw = p.read_bytes().decode("utf-8")
    crlf = "\r\n" in raw
    s = raw.replace("\r\n", "\n")
    if done_marker in s:
        print("skip (already)", rel, done_marker[:40])
        return
    a = s.index(start_marker)
    b = s.index(end_marker)
    s = s[:a] + new_block + s[b:]
    if crlf:
        s = s.replace("\n", "\r\n")
    p.write_bytes(s.encode("utf-8"))
    print("replaced block in", rel)


DISTORT = '''        // The flashing is planned first (alternating red flashes and black frames), so the music can be sliced to the
        // very same flashes: each red flash plays the next slice of the clip, each black frame is silence.
        private IEnumerator Distort(bool flashMusic)
        {
            List<float> reds = new List<float>();
            List<float> blacks = new List<float>();
            float planned = 0f;
            while (planned < distortSeconds)
            {
                float red = Random.Range(redFlashSeconds.x, redFlashSeconds.y);
                float black = Random.Range(blackFrameSeconds.x, blackFrameSeconds.y);
                reds.Add(red);
                blacks.Add(black);
                planned += red + black;
            }

            FlashSliceMusic slicer = flashMusic ? FlashSliceMusic.Instance : null;
            if (slicer != null) slicer.Begin();

            float elapsed = 0f;
            for (int i = 0; i < reds.Count; i++)
            {
                float t = Mathf.Clamp01(elapsed / distortSeconds);
                SetOverlay(Random.Range(0.35f, 0.6f + 0.4f * t), distortColor);
                if (slicer != null) slicer.Red(reds[i]);
                yield return WaitUnscaled(reds[i]);
                elapsed += reds[i];

                SetOverlay(Random.Range(0.15f, 0.35f + 0.65f * t), Color.black);
                if (slicer != null) slicer.Black();
                yield return WaitUnscaled(blacks[i]);
                elapsed += blacks[i];
            }

            SetOverlay(1f, distortColor);
            if (slicer != null) slicer.Finish();
            yield return WaitUnscaled(finalRedSeconds);
            SetOverlay(1f, Color.black);
        }

        private static IEnumerator WaitUnscaled(float seconds)
        {
            float elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

'''
replace_between("Core/SceneTransition.cs", "        private IEnumerator Distort()", "        private IEnumerator FadeOverlay", DISTORT, "Distort(bool flashMusic)")
