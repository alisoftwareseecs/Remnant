using UnityEngine;

namespace Remnant.Audio
{
    /// <summary>
    /// One piece of audio (a music track or a sound effect) with its loudness correction. The gain is measured, not
    /// guessed: it brings the file to the project's target loudness (Tools/audio/measure.py).
    /// </summary>
    [CreateAssetMenu(menuName = "Remnant/Audio Cue", fileName = "AudioCue")]
    public class AudioCue : ScriptableObject
    {
        [SerializeField] private AudioClip clip;
        [Tooltip("Gain in dB that brings the file to the target loudness (negative = quieter). Music is played through the Music Trim group, which carries the project's baseline boost, so the quietest tracks can be lifted too.")]
        [SerializeField] private float gainDb;
        [SerializeField] private bool loop = true;

        public AudioClip Clip => clip;
        public float GainDb => gainDb;
        public bool Loop => loop;

        /// <summary>The AudioSource volume (0-1) for this cue, given a baseline boost already applied by the mixer.</summary>
        public float Volume(float baselineBoostDb)
        {
            return Mathf.Clamp01(Mathf.Pow(10f, (gainDb - baselineBoostDb) / 20f));
        }
    }
}
