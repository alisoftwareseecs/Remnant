using System.Collections;
using Remnant.Core;
using Remnant.Dialogue;
using Remnant.Player;
using UnityEngine;

namespace Remnant.Story
{
    /// <summary>
    /// A trigger volume that plays a dialogue once when the player enters (after any scene transition
    /// has finished), then sets a flag.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class StoryTrigger : MonoBehaviour
    {
        [SerializeField] private DialogueRunner dialogueRunner;
        [SerializeField] private DialogueData dialogue;

        [Header("Conditions (both optional)")]
        [Tooltip("Every listed flag must be set (comma separated).")]
        [SerializeField] private string requiresFlag;
        [Tooltip("Normally the same as Set Flag, so the beat plays only once. Several flags may be listed, separated by commas.")]
        [SerializeField] private string forbidsFlag;

        [Header("After the dialogue")]
        [SerializeField] private string setFlag;
        [Tooltip("Also store the new flag in the saved checkpoint, so retrying does not replay this beat.")]
        [SerializeField] private bool refreshCheckpointFlags = true;

        [Header("Detection")]
        [Tooltip("Only the player's body counts, not its wide interaction trigger. Use this for triggers that must fire at an exact distance.")]
        [SerializeField] private bool playerBodyOnly;

        private bool running;

        private void Reset()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (running || other.GetComponentInParent<PlayerController>() == null) return;
            if (playerBodyOnly && other.isTrigger) return;

            GameState state = GameState.Instance;
            if (!string.IsNullOrWhiteSpace(requiresFlag) && (state == null || !state.HasAllFlags(requiresFlag))) return;
            if (state != null && state.HasAnyFlag(forbidsFlag)) return;

            running = true;
            StartCoroutine(Play());
        }

        private IEnumerator Play()
        {
            // The transition fade-in keeps the player locked and unlocks at its end; wait so the two never fight.
            while (SceneTransition.Instance != null && SceneTransition.Instance.IsTransitioning) yield return null;

            if (dialogueRunner != null) dialogueRunner.Play(dialogue, Finish);
            else Finish();
        }

        private void Finish()
        {
            GameState state = GameState.Instance;
            if (state != null)
            {
                state.SetFlag(setFlag);
                if (refreshCheckpointFlags) state.RefreshCheckpointFlags();
            }

            running = false;
        }
    }
}
