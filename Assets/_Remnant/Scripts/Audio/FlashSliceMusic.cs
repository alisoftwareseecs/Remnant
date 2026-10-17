using UnityEngine;

namespace Remnant.Audio
{
    /// <summary>
    /// Makes a music clip stutter forward in step with a flashing screen. Every red flash plays the next consecutive
    /// slice of the clip (as long as the flash lasts), every black frame cuts to silence, and the last red flash plays
    /// whatever is left with a short fade. Reusable: anything that flashes calls Begin, Red, Black and Finish. The
    /// slices go through the MusicManager, so music still has only one door.
    /// </summary>
    public class FlashSliceMusic : MonoBehaviour
    {
        public static FlashSliceMusic Instance { get; private set; }

        [Tooltip("The remainder of the clip fades out over this long at its end.")]
        [SerializeField, Min(0f)] private float remainderFadeSeconds = 0.4f;

        private AudioCue cue;
        private float position;
        private int slices;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
        }

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Starts a flashing sequence with the transition clip from the audio library (or the given cue).</summary>
        public void Begin(AudioCue overrideCue = null)
        {
            MusicManager manager = MusicManager.Instance;
            cue = overrideCue != null ? overrideCue : (manager != null && manager.Library != null ? manager.Library.Transition : null);
            position = 0f;
            slices = 0;
            if (cue != null) AudioLog.Add("Flash BEGIN " + cue.name + " (" + cue.Clip.length.ToString("0.00") + "s)");
        }

        /// <summary>A red flash that lasts this long: plays the next slice.</summary>
        public void Red(float seconds)
        {
            MusicManager manager = MusicManager.Instance;
            if (cue == null || manager == null || seconds <= 0f) return;

            float length = Mathf.Min(seconds, cue.Clip.length - position);
            if (length <= 0f) return;

            slices++;
            AudioLog.Add("Flash RED #" + slices + " slice " + position.ToString("0.000") + "-" + (position + length).ToString("0.000") + "s (" + length.ToString("0.000") + "s)");
            manager.PlaySlice(cue, position, length);
            position += seconds;
        }

        /// <summary>A black frame: silence.</summary>
        public void Black()
        {
            MusicManager manager = MusicManager.Instance;
            if (cue == null || manager == null) return;

            manager.CutSlice();
        }

        /// <summary>The last red flash: the rest of the clip, with a short fade at its end.</summary>
        public void Finish()
        {
            MusicManager manager = MusicManager.Instance;
            if (cue == null || manager == null) return;

            float left = cue.Clip.length - position;
            if (left > 0.02f)
            {
                slices++;
                AudioLog.Add("Flash RED #" + slices + " (last) remainder " + position.ToString("0.000") + "-" + cue.Clip.length.ToString("0.000") + "s (" + left.ToString("0.000") + "s, fade " + remainderFadeSeconds.ToString("0.0") + "s)");
                manager.PlaySlice(cue, position, left, remainderFadeSeconds);
            }

            AudioLog.Add("Flash END after " + slices + " red flashes");
            cue = null;
        }
    }
}
