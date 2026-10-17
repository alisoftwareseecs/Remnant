using Remnant.Audio;
using Remnant.Characters;
using Remnant.Core;
using UnityEngine;

namespace Remnant.Player
{
    /// <summary>
    /// Plays a footstep each time a foot lands in the player's walk animation, so the steps follow the animation: they
    /// slow down with him in the storage hallway, and stop when he stops. Top-down uses the classroom footstep slot,
    /// side view the corridor one (both are cues in the audio library; an empty slot is silent). Running steps are a
    /// little louder and higher.
    /// </summary>
    public class FootstepAudio : MonoBehaviour
    {
        [SerializeField] private PlayerController player;
        [SerializeField] private CharacterSpriteAnimator animator;
        [SerializeField, Range(0f, 1f)] private float volume = 1f;
        [SerializeField, Range(0f, 0.3f)] private float pitchVariation = 0.06f;

        private void Awake()
        {
            if (player == null) player = GetComponentInParent<PlayerController>();
            if (animator == null && player != null) animator = player.GetComponent<CharacterSpriteAnimator>();
        }

        private void OnEnable()
        {
            if (animator != null) animator.Stepped += OnStepped;
        }

        private void OnDisable()
        {
            if (animator != null) animator.Stepped -= OnStepped;
        }

        private void OnStepped()
        {
            AudioLibrary cues = MusicManager.Cues;
            if (cues == null || player == null) return;

            AudioCue cue = player.CurrentPerspective == Perspective.TopDown ? cues.FootstepClassroom : cues.FootstepCorridor;
            float pitch = 1f + Random.Range(-pitchVariation, pitchVariation) + (player.IsRunning ? 0.05f : 0f);
            SfxPlayer.Play(cue, volume, pitch);
        }
    }
}
