"""One-off patch: sound effect hooks in the existing scripts. Safe to re-run (markers)."""
from pathlib import Path

ROOT = Path(r"D:/Unity/Remnant/Assets/_Remnant/Scripts")


def patch(rel, pairs):
    p = ROOT / rel
    raw = p.read_bytes().decode("utf-8")
    crlf = "\r\n" in raw
    s = raw.replace("\r\n", "\n")
    for old, new, marker in pairs:
        if marker in s:
            print("skip (already)", rel, marker[:50])
            continue
        assert old in s, (rel, old[:80])
        s = s.replace(old, new, 1)
    if crlf:
        s = s.replace("\n", "\r\n")
    p.write_bytes(s.encode("utf-8"))
    print("patched", rel)


# ---- doors: a sound per story variant (played when the door is used, i.e. when its line appears / the scene change starts)
patch("Interaction/StoryInteractable.cs", [
    ("using System;\nusing Remnant.Core;",
     "using System;\nusing Remnant.Audio;\nusing Remnant.Core;",
     "using Remnant.Audio;"),
    ("            [SerializeField] private StorySequence sequence;\n",
     "            [SerializeField] private StorySequence sequence;\n"
     "            [Tooltip(\"Played the moment this variant is used (a door opening, a locked door rattling). Empty = silent.\")]\n"
     "            [SerializeField] private AudioCue sound;\n",
     "private AudioCue sound;"),
    ("            public StorySequence Sequence => sequence;\n",
     "            public StorySequence Sequence => sequence;\n            public AudioCue Sound => sound;\n",
     "public AudioCue Sound =>"),
    ("            Variant variant = SelectVariant();\n            if (variant == null) return;\n\n            if (variant.Dialogue != null && dialogueRunner != null)",
     "            Variant variant = SelectVariant();\n            if (variant == null) return;\n\n            SfxPlayer.Play(variant.Sound);\n\n            if (variant.Dialogue != null && dialogueRunner != null)",
     "SfxPlayer.Play(variant.Sound)"),
])

# ---- diary pages: paper turn
patch("Story/DiaryPanel.cs", [
    ("using System;\nusing Remnant.Core;" if False else "using Remnant.Core;\nusing System;",
     "using Remnant.Audio;\nusing Remnant.Core;\nusing System;",
     "using Remnant.Audio;"),
    ("            InteractionLock.Acquire();\n            if (player != null) player.SetMovementLocked(true);\n            if (panelRoot != null) panelRoot.SetActive(true);",
     "            InteractionLock.Acquire();\n            if (player != null) player.SetMovementLocked(true);\n            if (panelRoot != null) panelRoot.SetActive(true);\n            SfxPlayer.Play(MusicManager.Cues?.PaperTurn);",
     "MusicManager.Cues?.PaperTurn"),
])

# ---- menus
patch("UI/TitleMenu.cs", [
    ("using System.Collections;\nusing Remnant.Core;",
     "using System.Collections;\nusing Remnant.Audio;\nusing Remnant.Core;",
     "using Remnant.Audio;"),
    ("        private void NewGame()\n        {\n",
     "        private void NewGame()\n        {\n            SfxPlayer.Play(MusicManager.Cues?.MenuSelect);\n",
     "MusicManager.Cues?.MenuSelect);\n            GameState state = GameState.Instance;\n            if (state != null) state.ResetAll"),
    ("        private void Quit()\n        {\n",
     "        private void Quit()\n        {\n            SfxPlayer.Play(MusicManager.Cues?.MenuSelect);\n",
     "Quit()\n        {\n            SfxPlayer"),
])
patch("UI/PauseMenu.cs", [
    ("            if (resumeButton != null) resumeButton.onClick.AddListener(Resume);\n            if (quitButton != null) quitButton.onClick.AddListener(QuitToTitle);",
     "            if (resumeButton != null) resumeButton.onClick.AddListener(() => { MenuSound(); Resume(); });\n            if (quitButton != null) quitButton.onClick.AddListener(() => { MenuSound(); QuitToTitle(); });",
     "MenuSound(); Resume();"),
    ("        public void Resume()\n        {\n            Close();\n        }\n",
     "        public void Resume()\n        {\n            Close();\n        }\n\n        private static void MenuSound()\n        {\n            Remnant.Audio.SfxPlayer.Play(Remnant.Audio.MusicManager.Cues?.MenuSelect);\n        }\n",
     "private static void MenuSound()"),
])

# ---- game over: every loop cut dead with the music
patch("Core/GameOverController.cs", [
    ("                music.HardCut();\n                if (music.Library != null) SfxPlayer.Play(music.Library.GameOver);\n            }\n",
     "                music.HardCut();\n                if (music.Library != null) SfxPlayer.Play(music.Library.GameOver);\n            }\n\n            // the breathing and the memory of the pleasant music stop with the music\n            AudioLoops.HardCutAll();\n",
     "AudioLoops.HardCutAll();"),
])

# ---- Day 2: the lights flicker (empty slot), the rain goes on, the transition cuts it
patch("Story/ShadowAwakeningSequence.cs", [
    ("            if (MusicManager.Instance != null) MusicManager.Instance.Stop(musicFadeOutSeconds);\n",
     "            if (MusicManager.Instance != null) MusicManager.Instance.Stop(musicFadeOutSeconds);\n            SfxPlayer.Play(MusicManager.Cues?.LightsFlicker);\n",
     "Cues?.LightsFlicker"),
    ("            if (SceneTransition.Instance != null)\n            {\n                SceneTransition.Instance.GoTo(nextScene, nextSpawnId, null, style, null, null, -1f, true);",
     "            if (SceneTransition.Instance != null)\n            {\n                // the rain was the only sound left; the pull into the Future cuts it\n                AudioLoops.HardCut(\"rain\");\n                SceneTransition.Instance.GoTo(nextScene, nextSpawnId, null, style, null, null, -1f, true);",
     "AudioLoops.HardCut(\"rain\");"),
])

# ---- Act 4: light returns (empty slot + the rain comes back with the music)
patch("Story/Act4Director.cs", [
    ("            if (music != null && music.Library != null) music.Play(music.Library.Pleasant, lightsBackMusicFadeSeconds);\n",
     "            if (music != null && music.Library != null)\n            {\n                music.Play(music.Library.Pleasant, lightsBackMusicFadeSeconds);\n                AudioLoops.Play(\"rain\", music.Library.RainLight, AudioChannel.Ambience, lightsBackMusicFadeSeconds);\n                SfxPlayer.Play(music.Library.LightsReturn);\n            }\n",
     "AudioLoops.Play(\"rain\", music.Library.RainLight"),
])

# ---- the encounter: pressing F, the ghost flinching
patch("Story/GhostEncounterDirector.cs", [
    ("            if (MusicManager.Instance != null) MusicManager.Instance.HardCut();\n            if (hud != null)\n            {\n                // whatever the Shadow is shouting",
     "            if (MusicManager.Instance != null) MusicManager.Instance.HardCut();\n\n"
     "            // the heartbeat, her breathing and the rain are cut on the same frame (total silence for the reunion)\n"
     "            AudioLoops.HardCut(\"ghost_heartbeat\");\n"
     "            AudioLoops.HardCut(\"ghost_breath\");\n"
     "            AudioLoops.HardCut(\"rain\");\n"
     "            SfxPlayer.Play(MusicManager.Cues?.LightLower);\n"
     "            if (hud != null)\n            {\n                // whatever the Shadow is shouting",
     "AudioLoops.HardCut(\"ghost_heartbeat\")"),
    ("        private void OnBanished(int count)\n        {\n            if (count != 1) return;\n",
     "        private void OnBanished(int count)\n        {\n            SfxPlayer.Play(MusicManager.Cues?.GhostFlinch);\n            if (count != 1) return;\n",
     "Cues?.GhostFlinch"),
])

# ---- the ending door
patch("Story/EndingSequence.cs", [
    ("            started = true;\n            player.SetMovementLocked(true);\n",
     "            started = true;\n            player.SetMovementLocked(true);\n            Remnant.Audio.SfxPlayer.Play(Remnant.Audio.MusicManager.Cues?.DoorOpen);\n",
     "Cues?.DoorOpen"),
])
