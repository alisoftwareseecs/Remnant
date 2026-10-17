using UnityEngine;

namespace Remnant.Audio
{
    /// <summary>
    /// One piece of audio (a music track, a sound effect or an ambience loop) with its loudness correction. The gain is
    /// measured, not guessed: it brings the file to the project's target loudness (Tools/audio/measure.py). A cue with no
    /// clip is an empty slot: everything that plays cues treats it as silence, so files can be dropped in later.
    /// </summary>
    [CreateAssetMenu(menuName = "Remnant/Audio Cue", fileName = "AudioCue")]
    public class AudioCue : ScriptableObject
    {
        [SerializeField] private AudioClip clip;
        [Tooltip("Gain in dB that brings the file to the target loudness (negative = quieter). Music and ambience are played through a Trim group that carries a baseline boost, so the quietest files can be lifted too.")]
        [SerializeField] private float gainDb;
        [SerializeField] private bool loop = true;

        [Header("Character (loops only)")]
        [Tooltip("Playback pitch (1 = normal). Below 1 is slower and deeper.")]
        [SerializeField, Range(0.5f, 2f)] private float pitch = 1f;
        [Tooltip("Low-pass filter cutoff in Hz (0 = off).")]
        [SerializeField, Min(0f)] private float lowPassHz;
        [Tooltip("Adds a heavy, washed-out reverb.")]
        [SerializeField] private bool heavyReverb;

        public AudioClip Clip => clip;
        public float GainDb => gainDb;
        public bool Loop => loop;
        public float Pitch => pitch;
        public float LowPassHz => lowPassHz;
        public bool HeavyReverb => heavyReverb;

        /// <summary>True when the slot has a clip (empty slots are silent).</summary>
        public bool HasClip => clip != null;

        /// <summary>The AudioSource volume (0-1) for this cue, given a baseline boost already applied by the mixer.</summary>
        public float Volume(float baselineBoostDb)
        {
            return Mathf.Clamp01(Mathf.Pow(10f, (gainDb - baselineBoostDb) / 20f));
        }
    }
}
