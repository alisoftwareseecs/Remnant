using System;
using Remnant.Core;
using UnityEngine;

namespace Remnant.Audio
{
    public enum SceneMusicAction { Play, Stop, HardCut, Nothing }

    /// <summary>
    /// What a scene does with the music when it starts. The first rule whose flags match wins, so one scene can start
    /// different music (or none) depending on story progress. Asking for the track that already plays keeps it playing.
    /// Mid-scene changes are made by the story scripts, always through the MusicManager.
    /// </summary>
    public class SceneMusic : MonoBehaviour
    {
        [Serializable]
        private struct Rule
        {
            [Tooltip("Only for the notes in the Inspector.")]
            public string label;
            [Tooltip("Comma separated; ALL of these flags must be set. Empty = always.")]
            public string whenFlags;
            [Tooltip("Comma separated; the rule is skipped when ANY of these is set.")]
            public string unlessFlags;
            public SceneMusicAction action;
            public AudioCue cue;
            [Min(0f)] public float fadeSeconds;
        }

        [SerializeField] private Rule[] rules = new Rule[0];

        private void Start()
        {
            MusicManager manager = MusicManager.EnsureExists();
            if (manager == null) return;

            GameState state = GameState.Instance;
            foreach (Rule rule in rules)
            {
                if (state != null)
                {
                    if (!string.IsNullOrEmpty(rule.whenFlags) && !state.HasAllFlags(rule.whenFlags)) continue;
                    if (!string.IsNullOrEmpty(rule.unlessFlags) && state.HasAnyFlag(rule.unlessFlags)) continue;
                }

                AudioLog.Add("Scene " + gameObject.scene.name + " rule '" + rule.label + "' -> " + rule.action);
                Apply(manager, rule);
                return;
            }

            AudioLog.Add("Scene " + gameObject.scene.name + ": no music rule matched");
        }

        private static void Apply(MusicManager manager, Rule rule)
        {
            switch (rule.action)
            {
                case SceneMusicAction.Play: manager.Play(rule.cue, rule.fadeSeconds); break;
                case SceneMusicAction.Stop: manager.Stop(rule.fadeSeconds); break;
                case SceneMusicAction.HardCut: manager.HardCut(); break;
            }
        }
    }
}
