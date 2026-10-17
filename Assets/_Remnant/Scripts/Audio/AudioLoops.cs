using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace Remnant.Audio
{
    public enum AudioChannel { Sfx, Ambience }

    /// <summary>
    /// Named looping sounds on the SFX or Ambience mixer group: the rain, the heartbeat, the ghost's breathing, the
    /// distant memory of the pleasant music. One loop per id; asking for the cue that already loops under an id keeps it,
    /// another cue crossfades. A loop can be steered while it plays (volume scale, pitch scale, stereo pan), which is how
    /// the ghost's heartbeat and breathing follow her distance. Everything is logged in <see cref="AudioLog"/>.
    /// Lives on the AudioSystem prefab; like the music, loops are only started and stopped through this class.
    /// </summary>
    public class AudioLoops : MonoBehaviour
    {
        public static AudioLoops Instance { get; private set; }

        [SerializeField] private AudioMixer mixer;
        [Tooltip("SFX loops play through this group.")]
        [SerializeField] private AudioMixerGroup sfxGroup;
        [Tooltip("Ambience loops play through this group (Ambience Trim, a child of Ambience).")]
        [SerializeField] private AudioMixerGroup ambienceGroup;
        [Tooltip("Baseline boost of the Ambience Trim group in dB; ambience cue volumes are lowered by the same amount.")]
        [SerializeField, Range(0f, 20f)] private float ambienceBoostDb = 10f;

        private class Loop
        {
            public string id;
            public AudioChannel channel;
            public AudioSource source;
            public AudioCue cue;
            public float level;
            public float target;
            public float ratePerSecond;
            public bool stopWhenSilent;
            public float volumeScale = 1f;
            public float pitchScale = 1f;
            public float pan;
            public AudioSource second;   // the right-hand source when the loop is split into two sides
        }

        private struct Sides
        {
            public float left;
            public float right;
            public float pan;
        }

        private readonly Dictionary<string, Loop> current = new Dictionary<string, Loop>();
        private readonly Dictionary<string, Sides> sides = new Dictionary<string, Sides>();
        private readonly List<Loop> fadingOut = new List<Loop>();
        private readonly Stack<AudioSource> freeSources = new Stack<AudioSource>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) return;

            Instance = this;
            if (mixer != null) mixer.SetFloat("AmbienceTrim", ambienceBoostDb);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ------------------------------------------------------------------ public API (all null-safe)

        /// <summary>Starts a loop. Empty cues are silent. The same cue already looping under the id is kept; another one crossfades.</summary>
        public static void Play(string id, AudioCue cue, AudioChannel channel, float fadeInSeconds = 0.8f)
        {
            if (cue == null || !cue.HasClip) return;
            if (Instance == null) MusicManager.EnsureExists();
            if (Instance == null) return;

            Instance.PlayInternal(id, cue, channel, fadeInSeconds);
        }

        /// <summary>Fades the loop out and stops it (zero or less cuts at once).</summary>
        public static void Stop(string id, float fadeOutSeconds = 0.8f)
        {
            if (Instance == null) return;
            Instance.StopInternal(id, fadeOutSeconds, false);
        }

        /// <summary>Silence this frame.</summary>
        public static void HardCut(string id)
        {
            if (Instance == null) return;
            Instance.StopInternal(id, 0f, true);
        }

        /// <summary>Silence this frame, for every loop (the game over cut).</summary>
        public static void HardCutAll()
        {
            if (Instance == null) return;

            foreach (string id in new List<string>(Instance.current.Keys)) Instance.StopInternal(id, 0f, true);
            foreach (Loop loop in Instance.fadingOut.ToArray()) Instance.Release(loop);
            Instance.fadingOut.Clear();
        }

        /// <summary>Steers a running loop: volume scale (0-1), pitch scale and stereo pan (-1 left to 1 right).</summary>
        public static void Set(string id, float volumeScale, float pitchScale = 1f, float pan = 0f)
        {
            if (Instance == null) return;
            if (!Instance.current.TryGetValue(id, out Loop loop)) return;

            loop.volumeScale = Mathf.Clamp01(volumeScale);
            loop.pitchScale = pitchScale;
            loop.pan = Mathf.Clamp(pan, -1f, 1f);
        }

        /// <summary>
        /// Splits a loop into a left and a right source (each panned to its side, the right one started at another point of
        /// the clip so the two ears are not identical), with a volume (0-1) for each side. Call every frame; the sides are
        /// remembered per id, so a loop that starts later (a crossfade to the heavy rain) starts already split.
        /// </summary>
        public static void SetSides(string id, float left, float right, float pan = 0.9f)
        {
            if (Instance == null) return;
            Instance.sides[id] = new Sides { left = Mathf.Clamp01(left), right = Mathf.Clamp01(right), pan = Mathf.Clamp01(pan) };
        }

        /// <summary>Back to a single plain source for this id.</summary>
        public static void ClearSides(string id)
        {
            if (Instance == null) return;
            Instance.sides.Remove(id);
        }

        public static bool IsPlaying(string id)
        {
            return Instance != null && Instance.current.TryGetValue(id, out Loop loop) && loop.target > 0f;
        }

        // ------------------------------------------------------------------ internals

        private void PlayInternal(string id, AudioCue cue, AudioChannel channel, float fadeIn)
        {
            if (current.TryGetValue(id, out Loop existing))
            {
                if (existing.cue == cue && existing.target > 0f)
                {
                    AudioLog.Add("Loop KEEP " + id + " (" + cue.name + " already playing)");
                    return;
                }

                AudioLog.Add("Loop CROSSFADE " + id + " " + existing.cue.name + " -> " + cue.name + " " + fadeIn.ToString("0.0") + "s");
                existing.target = 0f;
                existing.ratePerSecond = 1f / Mathf.Max(0.01f, fadeIn);
                existing.stopWhenSilent = true;
                fadingOut.Add(existing);
                current.Remove(id);
            }
            else
            {
                AudioLog.Add("Loop PLAY " + id + " " + cue.name + " fadeIn=" + fadeIn.ToString("0.0") + "s");
            }

            Loop loop = new Loop { id = id, channel = channel, cue = cue, source = TakeSource(cue, channel) };
            if (sides.ContainsKey(id)) Split(loop);
            loop.level = fadeIn > 0f ? 0f : 1f;
            loop.target = 1f;
            loop.ratePerSecond = fadeIn > 0f ? 1f / fadeIn : 1000f;
            Apply(loop);
            loop.source.Play();
            current[id] = loop;
        }

        private void StopInternal(string id, float fade, bool hard)
        {
            if (!current.TryGetValue(id, out Loop loop)) return;

            current.Remove(id);
            if (hard || fade <= 0f)
            {
                AudioLog.Add("Loop " + (hard ? "HARDCUT " : "STOP ") + id + " " + loop.cue.name);
                Release(loop);
                return;
            }

            AudioLog.Add("Loop STOP " + id + " " + loop.cue.name + " fadeOut=" + fade.ToString("0.0") + "s");
            loop.target = 0f;
            loop.ratePerSecond = 1f / fade;
            loop.stopWhenSilent = true;
            fadingOut.Add(loop);
        }

        private AudioSource TakeSource(AudioCue cue, AudioChannel channel)
        {
            AudioSource source;
            if (freeSources.Count > 0)
            {
                source = freeSources.Pop();
                source.gameObject.SetActive(true);
            }
            else
            {
                GameObject child = new GameObject("Loop");
                child.transform.SetParent(transform, false);
                source = child.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f;
            }

            source.outputAudioMixerGroup = channel == AudioChannel.Ambience ? ambienceGroup : sfxGroup;
            source.clip = cue.Clip;
            source.loop = true;
            source.timeSamples = 0;
            source.panStereo = 0f;

            // the character of a loop (a distant, broken memory): a low-pass filter and a heavy reverb
            AudioLowPassFilter lowPass = source.GetComponent<AudioLowPassFilter>();
            if (cue.LowPassHz > 0f)
            {
                if (lowPass == null) lowPass = source.gameObject.AddComponent<AudioLowPassFilter>();
                lowPass.enabled = true;
                lowPass.cutoffFrequency = cue.LowPassHz;
            }
            else if (lowPass != null)
            {
                lowPass.enabled = false;
            }

            AudioReverbFilter reverb = source.GetComponent<AudioReverbFilter>();
            if (cue.HeavyReverb)
            {
                if (reverb == null) reverb = source.gameObject.AddComponent<AudioReverbFilter>();
                reverb.enabled = true;
                reverb.reverbPreset = AudioReverbPreset.User;
                reverb.dryLevel = -1200f;
                reverb.room = -300f;
                reverb.decayTime = 8f;
                reverb.decayHFRatio = 0.4f;
                reverb.reflectionsLevel = -400f;
                reverb.reverbLevel = 600f;
                reverb.diffusion = 100f;
                reverb.density = 100f;
            }
            else if (reverb != null)
            {
                reverb.enabled = false;
            }

            return source;
        }

        private void Release(Loop loop)
        {
            Unsplit(loop);
            loop.source.Stop();
            loop.source.clip = null;
            loop.source.gameObject.SetActive(false);
            freeSources.Push(loop.source);
        }

        private void Split(Loop loop)
        {
            if (loop.second != null) return;

            loop.second = TakeSource(loop.cue, loop.channel);
            loop.second.timeSamples = (int)(loop.cue.Clip.samples * 0.37f);
            loop.second.volume = 0f;
            loop.second.Play();
        }

        private void Unsplit(Loop loop)
        {
            if (loop.second == null) return;

            loop.second.Stop();
            loop.second.clip = null;
            loop.second.gameObject.SetActive(false);
            freeSources.Push(loop.second);
            loop.second = null;
        }

        private float BaselineDb(Loop loop)
        {
            return loop.channel == AudioChannel.Ambience ? ambienceBoostDb : 0f;
        }

        private void Apply(Loop loop)
        {
            float volume = loop.cue.Volume(BaselineDb(loop)) * loop.level * loop.volumeScale;
            float pitch = loop.cue.Pitch * loop.pitchScale;

            if (loop.second != null && sides.TryGetValue(loop.id, out Sides s))
            {
                loop.source.volume = volume * s.left;
                loop.source.panStereo = -s.pan;
                loop.second.volume = volume * s.right;
                loop.second.panStereo = s.pan;
                loop.second.pitch = pitch;
            }
            else
            {
                loop.source.volume = volume;
                loop.source.panStereo = loop.pan;
            }

            loop.source.pitch = pitch;
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;

            foreach (Loop loop in current.Values)
            {
                // split or not follows whether sides are set for this id (the room has window walls)
                if (sides.ContainsKey(loop.id)) Split(loop);
                else Unsplit(loop);

                loop.level = Mathf.MoveTowards(loop.level, loop.target, loop.ratePerSecond * dt);
                Apply(loop);
            }

            for (int i = fadingOut.Count - 1; i >= 0; i--)
            {
                Loop loop = fadingOut[i];
                loop.level = Mathf.MoveTowards(loop.level, 0f, loop.ratePerSecond * dt);
                Apply(loop);
                if (loop.level > 0.0001f) continue;

                AudioLog.Add("Loop stopped " + loop.id + " " + loop.cue.name + " (fade finished)");
                Release(loop);
                fadingOut.RemoveAt(i);
            }
        }
    }
}
