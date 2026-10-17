"""One-off patch: hooks the audio system into the story scripts. Safe to re-run (each patch checks its marker)."""
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


patch("Story/ShadowAwakeningSequence.cs", [
    ("using System.Collections;\nusing Remnant.Core;",
     "using System.Collections;\nusing Remnant.Audio;\nusing Remnant.Core;",
     "using Remnant.Audio;"),
    ("        [SerializeField] private string awakeFlag = \"shadow_awake\";\n",
     "        [SerializeField] private string awakeFlag = \"shadow_awake\";\n\n"
     "        [Header(\"Sound\")]\n"
     "        [Tooltip(\"The music stops when the lights start to flicker, over this long.\")]\n"
     "        [SerializeField, Min(0f)] private float musicFadeOutSeconds = 0.3f;\n"
     "        [Tooltip(\"The Shadow separates (and its sound plays) when this line of the intro starts: \\\"You move. Your shadow doesn't.\\\"\")]\n"
     "        [SerializeField, Min(0)] private int shadowSeparatesOnLine = 1;\n",
     "musicFadeOutSeconds"),
    ("        private bool running;\n",
     "        private bool running;\n        private bool awakened;\n",
     "private bool awakened;"),
    ("            if (GameState.Instance != null) GameState.Instance.SetFlag(awakeFlag);\n            yield return Flicker(introDelaySeconds);\n\n            yield return Speak(introDialogue);\n",
     "            // The lights flicker and the music stops. The Shadow separates on the line \"You move. Your shadow doesn't.\"\n"
     "            awakened = GameState.Instance != null && GameState.Instance.HasFlag(awakeFlag);\n"
     "            if (MusicManager.Instance != null) MusicManager.Instance.Stop(musicFadeOutSeconds);\n"
     "            yield return Flicker(introDelaySeconds);\n\n"
     "            if (dialogueRunner != null) dialogueRunner.LineStarted += OnLineStarted;\n"
     "            yield return Speak(introDialogue);\n"
     "            if (dialogueRunner != null) dialogueRunner.LineStarted -= OnLineStarted;\n"
     "            Awaken();\n",
     "OnLineStarted;"),
    ("        public override void Run()\n",
     "        private void OnLineStarted(DialogueData data, int index)\n"
     "        {\n"
     "            if (data == introDialogue && index >= shadowSeparatesOnLine) Awaken();\n"
     "        }\n\n"
     "        // The Shadow wakes (it fades in and separates) and its sound plays, once.\n"
     "        private void Awaken()\n"
     "        {\n"
     "            if (awakened) return;\n\n"
     "            awakened = true;\n"
     "            if (GameState.Instance != null) GameState.Instance.SetFlag(awakeFlag);\n"
     "            MusicManager music = MusicManager.Instance;\n"
     "            if (music != null && music.Library != null) SfxPlayer.Play(music.Library.ShadowAppear);\n"
     "        }\n\n"
     "        public override void Run()\n",
     "private void Awaken()"),
    ("SceneTransition.Instance.GoTo(nextScene, nextSpawnId, null, style);",
     "SceneTransition.Instance.GoTo(nextScene, nextSpawnId, null, style, null, null, -1f, true);",
     "null, style, null, null, -1f, true"),
])

patch("Story/StorageRevealSequence.cs", [
    ("using System.Collections;\nusing Remnant.Core;",
     "using System.Collections;\nusing Remnant.Audio;\nusing Remnant.Core;",
     "using Remnant.Audio;"),
    ("            PlayerController player = FindFirstObjectByType<PlayerController>();\n            if (player != null) player.SetMovementLocked(true);\n\n            flashlight.LockAim(",
     "            // The reveal, the still and the lines after it are in complete silence (the hallway music has already faded out by distance).\n"
     "            if (MusicManager.Instance != null) MusicManager.Instance.HardCut();\n\n"
     "            PlayerController player = FindFirstObjectByType<PlayerController>();\n            if (player != null) player.SetMovementLocked(true);\n\n            flashlight.LockAim(",
     "complete silence"),
    ("SceneTransition.Instance.GoTo(nextScene, nextSpawnId, titleCard, TransitionStyle.Fade, actFlag, null, fadeOutSeconds);",
     "// The Shadow sends him back with the same flashing pull as the one into the Future.\n"
     "                SceneTransition.Instance.GoTo(nextScene, nextSpawnId, titleCard, TransitionStyle.Distort, actFlag, null, -1f, true);",
     "TransitionStyle.Distort, actFlag"),
])

patch("Story/Act4Director.cs", [
    ("using System.Collections;\nusing Remnant.Characters;",
     "using System.Collections;\nusing Remnant.Audio;\nusing Remnant.Characters;",
     "using Remnant.Audio;"),
    ("            yield return Speak(lightsDieDialogue);\n",
     "            // \"The lights die.\": the music cuts dead.\n"
     "            if (MusicManager.Instance != null) MusicManager.Instance.HardCut();\n"
     "            yield return Speak(lightsDieDialogue);\n",
     "The lights die.\": the music cuts dead"),
    ("            state.SetFlag(stepInFlag);\n            state.RefreshCheckpointFlags();\n",
     "            state.SetFlag(stepInFlag);\n            state.RefreshCheckpointFlags();\n"
     "            // The lights come back and the pleasant music fades in.\n"
     "            MusicManager music = MusicManager.Instance;\n"
     "            if (music != null && music.Library != null) music.Play(music.Library.Pleasant, lightsBackMusicFadeSeconds);\n",
     "lightsBackMusicFadeSeconds);"),
    ("        [SerializeField] private string stepInFlag = \"act4_stepin\";\n",
     "        [SerializeField] private string stepInFlag = \"act4_stepin\";\n"
     "        [Tooltip(\"When the lights come back the pleasant music fades in over this long.\")]\n"
     "        [SerializeField, Min(0f)] private float lightsBackMusicFadeSeconds = 2.5f;\n",
     "private float lightsBackMusicFadeSeconds"),
])

patch("Story/GhostEncounterDirector.cs", [
    ("using System.Collections;\n",
     "using System.Collections;\nusing Remnant.Audio;\n",
     "using Remnant.Audio;"),
    ("            flashlight.LowerBeam();\n            promptAvailable = false;\n",
     "            flashlight.LowerBeam();\n            promptAvailable = false;\n"
     "            // the music cuts on the same frame as the Shadow's caption\n"
     "            if (MusicManager.Instance != null) MusicManager.Instance.HardCut();\n",
     "the music cuts on the same frame"),
])

patch("Core/GameOverController.cs", [
    ("using System.Collections;\nusing Remnant.Player;",
     "using System.Collections;\nusing Remnant.Audio;\nusing Remnant.Player;",
     "using Remnant.Audio;"),
    ("            if (IsActive) return;\n            IsActive = true;\n            StartCoroutine(Sequence());",
     "            if (IsActive) return;\n            IsActive = true;\n\n"
     "            // The ghost caught him: the music cuts dead, the game over sound plays once, and the screen is silent after it.\n"
     "            MusicManager music = MusicManager.Instance;\n"
     "            if (music != null)\n"
     "            {\n"
     "                music.HardCut();\n"
     "                if (music.Library != null) SfxPlayer.Play(music.Library.GameOver);\n"
     "            }\n\n"
     "            StartCoroutine(Sequence());",
     "the music cuts dead, the game over sound"),
    ("            IsActive = false;\n\n            if (SceneTransition.Instance != null)\n            {\n                SceneTransition.Instance.GoTo(sceneName, SpawnPoint.DefaultId",
     "            IsActive = false;\n            SfxPlayer.StopAll(0.3f);\n\n            if (SceneTransition.Instance != null)\n            {\n                SceneTransition.Instance.GoTo(sceneName, SpawnPoint.DefaultId",
     "SfxPlayer.StopAll"),
])
