using System;
using System.Collections.Generic;
using UnityEngine;

namespace Remnant.Core
{
    /// <summary>
    /// One save slot in PlayerPrefs (JSON): the scene, the player's position and the story flags. Also knows the
    /// "game completed" mark that makes the game remember a finished playthrough.
    /// </summary>
    public static class SaveSystem
    {
        public const string SaveKey = "remnant_save";
        public const string CompletedKey = "remnant_completed";

        public const string ContentWarningScene = "ContentWarning";
        public const string TitleScene = "Title";
        public const string CreditsScene = "Credits";

        [Serializable]
        private class SaveData
        {
            public string scene;
            public float x;
            public float y;
            public string[] flags;
        }

        public static bool HasSave => PlayerPrefs.HasKey(SaveKey);

        /// <summary>True once the credits have been seen (PlayerPrefs "remnant_completed").</summary>
        public static bool Completed => PlayerPrefs.GetInt(CompletedKey, 0) == 1;

        /// <summary>The menu, warning and credits scenes are never saved and have no pause menu.</summary>
        public static bool IsGameplayScene(string sceneName)
        {
            return sceneName != ContentWarningScene && sceneName != TitleScene && sceneName != CreditsScene;
        }

        public static void Save(string sceneName, Vector2 position, IEnumerable<string> flags)
        {
            if (!IsGameplayScene(sceneName)) return;

            SaveData data = new SaveData
            {
                scene = sceneName,
                x = position.x,
                y = position.y,
                flags = new List<string>(flags).ToArray()
            };

            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }

        public static bool TryLoad(out string sceneName, out Vector2 position, out string[] flags)
        {
            sceneName = null;
            position = Vector2.zero;
            flags = Array.Empty<string>();

            if (!HasSave) return false;

            SaveData data = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(SaveKey));
            if (data == null || string.IsNullOrEmpty(data.scene)) return false;

            sceneName = data.scene;
            position = new Vector2(data.x, data.y);
            flags = data.flags ?? Array.Empty<string>();
            return true;
        }

        public static void Clear()
        {
            PlayerPrefs.DeleteKey(SaveKey);
            PlayerPrefs.Save();
        }

        /// <summary>Loads the save: restores the flags, then loads the saved scene at the saved position.</summary>
        public static bool Continue()
        {
            GameState state = GameState.Instance;
            SceneTransition transition = SceneTransition.Instance;
            if (state == null || transition == null) return false;
            if (!TryLoad(out string sceneName, out Vector2 position, out string[] flags)) return false;

            state.ReplaceFlags(flags);
            transition.GoTo(sceneName, null, null, TransitionStyle.Fade, null, position);
            return true;
        }
    }
}
