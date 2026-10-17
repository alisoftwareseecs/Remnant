using Remnant.Enemies;
using Remnant.Player;
using UnityEngine;

namespace Remnant.Audio
{
    /// <summary>
    /// A loop that follows how close something (the ghost, the monster) is to the player: louder the closer it gets
    /// (inaudible beyond the max distance, full at the full distance), optionally faster in pitch and panned to the side
    /// of the screen it is on. Used for the ghost's breathing (the monster in the corridor and Aiko's ghost in the
    /// Back-to-Day-1 encounter) and her heartbeat. The loop starts the first time the thing is present; with "keep after
    /// absence" it runs on while the thing is away (the heartbeat) and is only silent by distance.
    /// </summary>
    public class ProximityLoop : MonoBehaviour
    {
        public enum LoopKind { Breathing, Heartbeat }

        [Tooltip("Which sound from the audio library.")]
        [SerializeField] private LoopKind kind = LoopKind.Breathing;
        [SerializeField] private string loopId = "ghost_breath";
        [SerializeField] private AudioChannel channel = AudioChannel.Sfx;

        [Header("What it follows (one or none; none = always present)")]
        [SerializeField] private AikoGhostController ghost;
        [SerializeField] private MonsterController monster;

        [Header("Distance (units)")]
        [Tooltip("Inaudible from here on.")]
        [SerializeField, Min(0.1f)] private float maxDistance = 10f;
        [Tooltip("Full volume at this distance (touching range).")]
        [SerializeField, Min(0f)] private float fullDistance = 0.6f;
        [Tooltip("1 = linear, higher = stays quieter until she is near.")]
        [SerializeField, Range(0.5f, 4f)] private float curvePower = 1.6f;
        [SerializeField, Range(0f, 1f)] private float minVolume;

        [Header("Pitch (playback speed)")]
        [SerializeField] private float pitchFar = 1f;
        [SerializeField] private float pitchNear = 1f;

        [Header("Stereo")]
        [Tooltip("How far left/right the sound pans when it is at the edge of the screen (0 = no panning).")]
        [SerializeField, Range(0f, 1f)] private float panAmount = 0.8f;

        [Header("When it ends")]
        [Tooltip("Keep running (only quiet by distance) while the thing is away, instead of stopping.")]
        [SerializeField] private bool keepAfterAbsence;
        [Tooltip("Cut dead on the frame the flashlight is lowered (the Back-to-Day-1 encounter).")]
        [SerializeField] private bool cutWhenLightLowered;
        [SerializeField, Min(0.05f)] private float fadeSeconds = 0.8f;

        private PlayerController player;
        private Camera cam;
        private bool started;
        private bool cut;

        private void OnDisable()
        {
            AudioLoops.Stop(loopId, 0.2f);
            started = false;
        }

        private bool Present
        {
            get
            {
                if (ghost != null) return ghost.IsVisible;
                if (monster != null) return monster.IsApproaching;
                return true;
            }
        }

        private void Update()
        {
            if (cut) return;

            // the game over cut silenced every loop; this one must not start again behind it (nor while the retry transition runs)
            if (Core.GameOverController.IsActive || (Core.SceneTransition.Instance != null && Core.SceneTransition.Instance.IsTransitioning))
            {
                started = false;
                return;
            }

            if (cutWhenLightLowered && Flashlight.FlashlightController.Active != null && Flashlight.FlashlightController.Active.IsLowered)
            {
                AudioLoops.HardCut(loopId);
                cut = true;
                started = false;
                return;
            }

            AudioLibrary cues = MusicManager.Cues;
            if (cues == null) return;

            bool present = Present;
            if (!present && !(started && keepAfterAbsence))
            {
                if (started)
                {
                    AudioLoops.Stop(loopId, fadeSeconds);
                    started = false;
                }

                return;
            }

            if (player == null) player = FindFirstObjectByType<PlayerController>();
            if (player == null) return;

            if (!started)
            {
                AudioCue cue = kind == LoopKind.Heartbeat ? cues.Heartbeat : cues.GhostBreathing;
                AudioLoops.Play(loopId, cue, channel, fadeSeconds);
                started = true;
            }

            Vector2 delta = transform.position - player.transform.position;
            float t = Mathf.InverseLerp(maxDistance, fullDistance, delta.magnitude);
            float volume = Mathf.Max(minVolume, Mathf.Pow(t, curvePower));
            float pitch = Mathf.Lerp(pitchFar, pitchNear, t);

            float pan = 0f;
            if (panAmount > 0f)
            {
                if (cam == null) cam = Camera.main;
                float half = cam != null ? Mathf.Max(0.1f, cam.orthographicSize * cam.aspect) : 8f;
                pan = Mathf.Clamp(delta.x / half, -1f, 1f) * panAmount;
            }

            AudioLoops.Set(loopId, volume, pitch, pan);
        }
    }
}
