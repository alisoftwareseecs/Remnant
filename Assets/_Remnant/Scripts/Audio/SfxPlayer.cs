using UnityEngine;
using UnityEngine.Audio;

namespace Remnant.Audio
{
    /// <summary>Plays one-shot sound effects through the SFX mixer group. Lives on the AudioSystem prefab next to the MusicManager.</summary>
    public class SfxPlayer : MonoBehaviour
    {
        public static SfxPlayer Instance { get; private set; }

        [SerializeField] private AudioMixerGroup sfxGroup;
        [SerializeField, Range(1, 16)] private int voiceCount = 6;

        private AudioSource[] sources;
        private float[] baseVolume;
        private float[] fadeRate;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) return;

            Instance = this;
            sources = new AudioSource[voiceCount];
            baseVolume = new float[voiceCount];
            fadeRate = new float[voiceCount];
            for (int i = 0; i < voiceCount; i++)
            {
                GameObject child = new GameObject("SfxVoice" + i);
                child.transform.SetParent(transform, false);
                AudioSource source = child.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f;
                source.outputAudioMixerGroup = sfxGroup;
                sources[i] = source;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Plays the cue once (a looping cue loops until StopAll). Does nothing if the audio system is missing.</summary>
        public static void Play(AudioCue cue, float volumeScale = 1f)
        {
            if (cue == null || cue.Clip == null) return;

            if (Instance == null) MusicManager.EnsureExists();
            if (Instance == null) return;

            Instance.PlayInternal(cue, volumeScale);
        }

        /// <summary>Stops every effect, with a short fade (zero cuts at once).</summary>
        public static void StopAll(float fadeSeconds = 0.3f)
        {
            if (Instance == null) return;
            Instance.StopInternal(fadeSeconds);
        }

        private void PlayInternal(AudioCue cue, float volumeScale)
        {
            int index = -1;
            for (int i = 0; i < sources.Length; i++)
            {
                if (!sources[i].isPlaying) { index = i; break; }
            }

            // all busy: the first effect gives way
            if (index < 0) index = 0;

            AudioSource source = sources[index];
            source.Stop();
            source.clip = cue.Clip;
            source.loop = cue.Loop;
            baseVolume[index] = cue.Volume(0f) * volumeScale;
            fadeRate[index] = 0f;
            source.volume = baseVolume[index];
            source.Play();
            AudioLog.Add("Sfx PLAY " + cue.name);
        }

        private void StopInternal(float fadeSeconds)
        {
            for (int i = 0; i < sources.Length; i++)
            {
                if (!sources[i].isPlaying) continue;

                if (fadeSeconds <= 0f) sources[i].Stop();
                else fadeRate[i] = 1f / fadeSeconds;
                AudioLog.Add("Sfx STOP " + sources[i].clip.name);
            }
        }

        private void Update()
        {
            for (int i = 0; i < sources.Length; i++)
            {
                if (fadeRate[i] <= 0f || !sources[i].isPlaying) continue;

                sources[i].volume = Mathf.MoveTowards(sources[i].volume, 0f, baseVolume[i] * fadeRate[i] * Time.unscaledDeltaTime);
                if (sources[i].volume <= 0.0001f)
                {
                    sources[i].Stop();
                    fadeRate[i] = 0f;
                }
            }
        }
    }
}
