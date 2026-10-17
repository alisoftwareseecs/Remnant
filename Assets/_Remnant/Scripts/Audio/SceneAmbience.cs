using System;
using System.Collections.Generic;
using Remnant.Core;
using UnityEngine;

namespace Remnant.Audio
{
    /// <summary>
    /// What a scene does with the looping ambiences (the rain, the memory of the pleasant music) when it starts, like
    /// SceneMusic does for the music. For each loop name the first rule whose flags match wins, so one scene can steer
    /// several loops. A rule can start a loop, fade it out, cut it, or do nothing (so the loop that is already running
    /// keeps going).
    /// </summary>
    public class SceneAmbience : MonoBehaviour
    {
        public enum Action { Play, Stop, HardCut, Nothing }

        /// <summary>Which cue of the audio library a rule plays.</summary>
        public enum Sound { None, RainLight, RainHeavy, PleasantMemory }

        [Serializable]
        private struct Rule
        {
            [Tooltip("Only for the notes in the Inspector.")]
            public string label;
            [Tooltip("The loop's name: rain, memory.")]
            public string loopId;
            [Tooltip("Comma separated; ALL of these flags must be set. Empty = always.")]
            public string whenFlags;
            [Tooltip("Comma separated; the rule is skipped when ANY of these is set.")]
            public string unlessFlags;
            public Action action;
            public Sound sound;
            [Min(0f)] public float fadeSeconds;
        }

        [SerializeField] private Rule[] rules = new Rule[0];

        private void Start()
        {
            MusicManager manager = MusicManager.EnsureExists();
            if (manager == null) return;

            GameState state = GameState.Instance;
            HashSet<string> handled = new HashSet<string>();
            foreach (Rule rule in rules)
            {
                if (handled.Contains(rule.loopId)) continue;

                if (state != null)
                {
                    if (!string.IsNullOrEmpty(rule.whenFlags) && !state.HasAllFlags(rule.whenFlags)) continue;
                    if (!string.IsNullOrEmpty(rule.unlessFlags) && state.HasAnyFlag(rule.unlessFlags)) continue;
                }

                handled.Add(rule.loopId);
                AudioLog.Add("Scene " + gameObject.scene.name + " ambience rule '" + rule.label + "' -> " + rule.action);
                Apply(manager.Library, rule);
            }
        }

        private static void Apply(AudioLibrary library, Rule rule)
        {
            switch (rule.action)
            {
                case Action.Play:
                    if (library != null) AudioLoops.Play(rule.loopId, Pick(library, rule.sound), AudioChannel.Ambience, rule.fadeSeconds);
                    break;
                case Action.Stop: AudioLoops.Stop(rule.loopId, rule.fadeSeconds); break;
                case Action.HardCut: AudioLoops.HardCut(rule.loopId); break;
            }
        }

        private static AudioCue Pick(AudioLibrary library, Sound sound)
        {
            switch (sound)
            {
                case Sound.RainLight: return library.RainLight;
                case Sound.RainHeavy: return library.RainHeavy;
                case Sound.PleasantMemory: return library.PleasantMemory;
                default: return null;
            }
        }
    }
}
