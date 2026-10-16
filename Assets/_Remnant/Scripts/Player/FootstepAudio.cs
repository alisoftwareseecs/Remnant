using UnityEngine;

namespace Remnant.Player
{
    /// <summary>
    /// Plays a footstep clip at a steady rhythm while the player moves. Leave the clip list empty until audio
    /// exists; nothing plays then.
    /// </summary>
    public class FootstepAudio : MonoBehaviour
    {
        [SerializeField] private PlayerController player;
        [SerializeField] private AudioSource source;
        [Tooltip("One is picked at random for each step.")]
        [SerializeField] private AudioClip[] footstepClips = new AudioClip[0];
        [SerializeField, Min(0.05f)] private float walkInterval = 0.5f;
        [SerializeField, Min(0.05f)] private float runInterval = 0.3f;
        [Range(0f, 1f)] [SerializeField] private float volume = 0.6f;
        [SerializeField, Range(0f, 0.3f)] private float pitchVariation = 0.08f;

        private float timer;

        private void Awake()
        {
            if (player == null) player = GetComponentInParent<PlayerController>();
        }

        private void Update()
        {
            if (player == null || source == null || footstepClips == null || footstepClips.Length == 0) return;

            if (!player.IsMoving)
            {
                timer = 0f;
                return;
            }

            timer -= Time.deltaTime;
            if (timer > 0f) return;

            timer = player.IsRunning ? runInterval : walkInterval;
            AudioClip clip = footstepClips[Random.Range(0, footstepClips.Length)];
            if (clip == null) return;

            source.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
            source.PlayOneShot(clip, volume);
        }
    }
}
