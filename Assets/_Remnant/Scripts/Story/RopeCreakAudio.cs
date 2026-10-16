using Remnant.Player;
using UnityEngine;

namespace Remnant.Story
{
    /// <summary>
    /// The rope's creak, tied to the sway. A looping creak gets louder as the player comes closer and swells with
    /// the strain at the ends of each swing. Optionally a one-shot creak plays every time the swing turns round.
    /// Leave the clips empty until audio exists; nothing plays then.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class RopeCreakAudio : MonoBehaviour
    {
        [Header("Clips (all optional)")]
        [Tooltip("A slow rope creak, ideally seamless so it can loop.")]
        [SerializeField] private AudioClip creakLoop;
        [Tooltip("One of these plays (at random) each time the swing turns round.")]
        [SerializeField] private AudioClip[] swingCreaks = new AudioClip[0];

        [Header("Hearing")]
        [SerializeField, Min(1f)] private float hearingDistance = 12f;
        [Range(0f, 1f)] [SerializeField] private float maxVolume = 0.8f;
        [Tooltip("1 = volume follows distance linearly, higher = stays quiet until the player is close.")]
        [SerializeField, Min(0.5f)] private float distanceCurve = 2f;

        [Header("Tied to the sway")]
        [Tooltip("Found on this object when empty.")]
        [SerializeField] private HangingSway sway;
        [Tooltip("Loop volume at the middle of the swing, compared with the ends (1).")]
        [Range(0f, 1f)] [SerializeField] private float volumeAtCentre = 0.35f;
        [SerializeField, Min(0f)] private float pitchRange = 0.06f;

        private AudioSource source;
        private PlayerController player;
        private float closeness;

        private void Awake()
        {
            source = GetComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f;
            source.clip = creakLoop;
            source.volume = 0f;
            if (sway == null) sway = GetComponent<HangingSway>();
        }

        private void OnEnable()
        {
            if (sway != null) sway.SwingReversed += OnSwingReversed;
        }

        private void OnDisable()
        {
            if (sway != null) sway.SwingReversed -= OnSwingReversed;
        }

        private void Update()
        {
            if (player == null) player = FindFirstObjectByType<PlayerController>();
            if (player == null) return;

            float distance = Vector2.Distance(player.transform.position, transform.position);
            closeness = Mathf.Pow(1f - Mathf.Clamp01(distance / hearingDistance), distanceCurve);

            if (creakLoop == null) return;

            float strain = 1f;
            if (sway != null && sway.MaxDegrees > 0.001f)
            {
                float t = Mathf.Clamp01(Mathf.Abs(sway.Angle) / sway.MaxDegrees);
                strain = Mathf.Lerp(volumeAtCentre, 1f, t);
                source.pitch = 1f + Mathf.Sign(sway.Angle) * pitchRange * t;
            }

            source.volume = maxVolume * closeness * strain;
            if (!source.isPlaying) source.Play();
        }

        private void OnSwingReversed()
        {
            if (swingCreaks == null || swingCreaks.Length == 0 || closeness < 0.02f) return;

            AudioClip clip = swingCreaks[Random.Range(0, swingCreaks.Length)];
            if (clip != null) source.PlayOneShot(clip, maxVolume * closeness);
        }
    }
}
