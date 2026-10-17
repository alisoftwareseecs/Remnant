using Remnant.Core;
using Remnant.Dialogue;
using Remnant.Player;
using UnityEngine;

namespace Remnant.Story
{
    /// <summary>
    /// The end of the final walk. When the player walks into this trigger (the sunlight at the open door) the screen
    /// slowly fades to white and the credits start.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class EndingSequence : MonoBehaviour
    {
        [SerializeField] private string requiresFlag = "ending_light";
        [SerializeField] private DialogueRunner dialogueRunner;
        [Tooltip("Spoken at the open storage room door before the screen fades to white.")]
        [SerializeField] private DialogueData dialogue;
        [SerializeField] private string creditsScene = "Credits";
        [SerializeField, Min(0.5f)] private float fadeToWhiteSeconds = 4f;

        private bool started;

        private void Reset()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (started) return;

            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player == null) return;

            GameState state = GameState.Instance;
            if (!string.IsNullOrEmpty(requiresFlag) && (state == null || !state.HasFlag(requiresFlag))) return;
            if (SceneTransition.Instance == null)
            {
                Debug.LogWarning("EndingSequence: no SceneTransition in the scene.", this);
                return;
            }

            started = true;
            player.SetMovementLocked(true);
            Remnant.Audio.SfxPlayer.Play(Remnant.Audio.MusicManager.Cues?.DoorOpen);

            if (dialogueRunner != null && dialogue != null) dialogueRunner.Play(dialogue, Leave);
            else Leave();
        }

        private void Leave()
        {
            PlayerController player = FindFirstObjectByType<PlayerController>();
            if (player != null) player.SetMovementLocked(true);
            SceneTransition.Instance.GoTo(creditsScene, null, null, TransitionStyle.Flash, null, null, fadeToWhiteSeconds);
        }
    }
}
