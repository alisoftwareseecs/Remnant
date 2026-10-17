using UnityEngine;
using UnityEngine.Audio;

namespace Remnant.Audio
{
    /// <summary>
    /// The only thing that starts, stops or fades music. Persistent (it lives on the Resources/AudioSystem prefab, which is
    /// created before the first scene). Two voices let one track fade out while the next fades in. Asking for the track
    /// that is already playing keeps it playing: it is never restarted. Everything is logged in <see cref="AudioLog"/>.
    /// </summary>
    public class MusicManager : MonoBehaviour
    {
        private const string ResourcePath = "AudioSystem";

        public static MusicManager Instance { get; private set; }

        [Header("Mixer")]
        [SerializeField] private AudioMixer mixer;
        [Tooltip("The group the music voices play through (Music Trim, a child of Music).")]
        [SerializeField] private AudioMixerGroup musicGroup;
        [SerializeField] private AudioLibrary library;
        [Tooltip("Baseline boost of the Music Trim group in dB. Cue volumes are lowered by the same amount, so the quietest track can still reach the target loudness without clipping.")]
        [SerializeField, Range(0f, 20f)] private float baselineBoostDb = 10f;

        [Header("Fades")]
        [SerializeField, Min(0f)] private float defaultFadeInSeconds = 1.5f;
        [SerializeField, Min(0f)] private float defaultFadeOutSeconds = 1f;

        private class Voice
        {
            public AudioSource source;
            public AudioCue cue;
            public float level;
            public float target;
            public float ratePerSecond;
            public bool stopWhenSilent;
        }

        private Voice[] voices;
        private AudioSource sliceSource;
        private double sliceFadeStart;
        private double sliceEnd;
        private float sliceFadeSeconds;
        private float sliceVolume;
        private float scale = 1f;

        public AudioLibrary Library => library;

        /// <summary>The cue library of the audio system, or null before it exists. Null-safe shortcut for one-shot hooks.</summary>
        public static AudioLibrary Cues => Instance != null ? Instance.library : null;
        public float BaselineBoostDb => baselineBoostDb;

        /// <summary>The cue playing (or fading in) right now, null when nothing is.</summary>
        public AudioCue CurrentCue
        {
            get
            {
                Voice voice = CurrentVoice();
                return voice != null ? voice.cue : null;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            EnsureExists();
        }

        /// <summary>Creates the persistent audio system if it is not there yet. Returns it (null when the prefab is missing).</summary>
        public static MusicManager EnsureExists()
        {
            if (Instance != null) return Instance;

            GameObject prefab = Resources.Load<GameObject>(ResourcePath);
            if (prefab == null)
            {
                Debug.LogWarning("MusicManager: Resources/" + ResourcePath + " is missing.");
                return null;
            }

            Instantiate(prefab);
            return Instance;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);

            voices = new Voice[] { CreateVoice("MusicVoiceA"), CreateVoice("MusicVoiceB") };
            sliceSource = CreateSource("MusicSlice");
            if (mixer != null) mixer.SetFloat("MusicTrim", baselineBoostDb);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private Voice CreateVoice(string voiceName)
        {
            return new Voice { source = CreateSource(voiceName) };
        }

        private AudioSource CreateSource(string sourceName)
        {
            GameObject child = new GameObject(sourceName);
            child.transform.SetParent(transform, false);
            AudioSource source = child.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.outputAudioMixerGroup = musicGroup;
            return source;
        }

        // ------------------------------------------------------------------ public API

        /// <summary>Starts a track. If it is already playing nothing happens; if another track plays it is crossfaded.</summary>
        public void Play(AudioCue cue, float fadeInSeconds = -1f)
        {
            if (!IsUsable(cue)) return;
            float fade = fadeInSeconds >= 0f ? fadeInSeconds : defaultFadeInSeconds;

            Voice current = CurrentVoice();
            if (current != null && current.cue == cue)
            {
                AudioLog.Add("Music KEEP " + cue.name + " (already playing)");
                return;
            }

            if (current != null)
            {
                Crossfade(cue, fade);
                return;
            }

            AudioLog.Add("Music PLAY " + cue.name + " fadeIn=" + fade.ToString("0.0") + "s");
            StartVoice(cue, fade, null);
        }

        /// <summary>Fades the current track out (and stops it). Zero or less is the same as <see cref="HardCut"/>.</summary>
        public void Stop(float fadeOutSeconds = -1f)
        {
            float fade = fadeOutSeconds >= 0f ? fadeOutSeconds : defaultFadeOutSeconds;
            if (fade <= 0f)
            {
                HardCut();
                return;
            }

            Voice current = CurrentVoice();
            if (current == null) return;

            AudioLog.Add("Music STOP " + current.cue.name + " fadeOut=" + fade.ToString("0.0") + "s");
            FadeOut(current, fade);
        }

        /// <summary>Silence this very frame: every voice and the transition slices.</summary>
        public void HardCut()
        {
            bool any = false;
            foreach (Voice voice in voices)
            {
                if (voice.cue == null) continue;
                any = true;
                AudioLog.Add("Music HARDCUT " + voice.cue.name);
                Silence(voice);
            }

            if (sliceSource.isPlaying)
            {
                any = true;
                sliceSource.Stop();
            }

            if (!any) AudioLog.Add("Music HARDCUT (nothing was playing)");
        }

        /// <summary>The current track fades out while this one fades in, over the same time.</summary>
        public void Crossfade(AudioCue cue, float durationSeconds)
        {
            if (!IsUsable(cue)) return;

            Voice current = CurrentVoice();
            if (current != null && current.cue == cue)
            {
                AudioLog.Add("Music KEEP " + cue.name + " (already playing)");
                return;
            }

            float duration = Mathf.Max(0.01f, durationSeconds);
            AudioLog.Add("Music CROSSFADE " + (current != null ? current.cue.name : "(silence)") + " -> " + cue.name + " " + duration.ToString("0.0") + "s");
            if (current != null) FadeOut(current, duration);
            StartVoice(cue, duration, current);
        }

        public bool IsPlaying(AudioCue cue)
        {
            Voice current = CurrentVoice();
            return current != null && current.cue == cue;
        }

        /// <summary>Scales all music (0-1) on top of the fades. The storage hallway uses it to fade the track out by distance.</summary>
        public void SetScale(float value)
        {
            scale = Mathf.Clamp01(value);
        }

        // ------------------------------------------------------------------ transition slices

        /// <summary>Plays a piece of a clip from startSeconds for lengthSeconds, sample-accurate (scheduled on the audio clock).</summary>
        public void PlaySlice(AudioCue cue, float startSeconds, float lengthSeconds, float fadeOutSeconds = 0f)
        {
            if (!IsUsable(cue) || lengthSeconds <= 0f) return;

            AudioClip clip = cue.Clip;
            sliceSource.Stop();
            sliceSource.clip = clip;
            sliceSource.loop = false;
            sliceSource.timeSamples = Mathf.Clamp(Mathf.RoundToInt(startSeconds * clip.frequency), 0, clip.samples - 1);
            sliceVolume = cue.Volume(baselineBoostDb);
            sliceSource.volume = sliceVolume;

            double start = AudioSettings.dspTime + 0.01;
            sliceEnd = start + lengthSeconds;
            sliceFadeSeconds = Mathf.Min(fadeOutSeconds, lengthSeconds);
            sliceFadeStart = sliceEnd - sliceFadeSeconds;
            sliceSource.PlayScheduled(start);
            sliceSource.SetScheduledEndTime(sliceEnd);
        }

        /// <summary>Stops the current slice at once (the black frame between two red flashes).</summary>
        public void CutSlice()
        {
            if (sliceSource.isPlaying) sliceSource.Stop();
            sliceFadeSeconds = 0f;
        }

        // ------------------------------------------------------------------ internals

        private bool IsUsable(AudioCue cue)
        {
            if (cue != null && cue.Clip != null) return true;
            Debug.LogWarning("MusicManager: asked to play an empty cue.", this);
            return false;
        }

        private Voice CurrentVoice()
        {
            if (voices == null) return null;
            foreach (Voice voice in voices) if (voice.cue != null && voice.target > 0f) return voice;
            return null;
        }

        private void StartVoice(AudioCue cue, float fadeSeconds, Voice avoid)
        {
            Voice voice = voices[0] == avoid ? voices[1] : voices[0];
            if (voice.cue != null) Silence(voice);   // an older fade-out still running is cut

            voice.cue = cue;
            voice.source.clip = cue.Clip;
            voice.source.loop = cue.Loop;
            voice.source.timeSamples = 0;
            voice.level = fadeSeconds > 0f ? 0f : 1f;
            voice.target = 1f;
            voice.ratePerSecond = fadeSeconds > 0f ? 1f / fadeSeconds : 1000f;
            voice.stopWhenSilent = false;
            voice.source.volume = cue.Volume(baselineBoostDb) * voice.level * scale;
            voice.source.Play();
        }

        private static void FadeOut(Voice voice, float seconds)
        {
            voice.target = 0f;
            voice.ratePerSecond = 1f / Mathf.Max(0.01f, seconds);
            voice.stopWhenSilent = true;
        }

        private static void Silence(Voice voice)
        {
            voice.source.Stop();
            voice.source.clip = null;
            voice.cue = null;
            voice.level = 0f;
            voice.target = 0f;
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;

            foreach (Voice voice in voices)
            {
                if (voice.cue == null) continue;

                voice.level = Mathf.MoveTowards(voice.level, voice.target, voice.ratePerSecond * dt);
                voice.source.volume = voice.cue.Volume(baselineBoostDb) * voice.level * scale;

                bool finishedFading = voice.stopWhenSilent && voice.level <= 0.0001f;
                bool endedByItself = !voice.cue.Loop && !voice.source.isPlaying && voice.level > 0f;
                if (finishedFading || endedByItself)
                {
                    if (finishedFading) AudioLog.Add("Music stopped " + voice.cue.name + " (fade finished)");
                    Silence(voice);
                }
            }

            if (sliceFadeSeconds > 0f && sliceSource.isPlaying)
            {
                double now = AudioSettings.dspTime;
                if (now > sliceFadeStart)
                {
                    float left = Mathf.Clamp01((float)((sliceEnd - now) / sliceFadeSeconds));
                    sliceSource.volume = sliceVolume * left;
                }
            }
        }
    }
}
