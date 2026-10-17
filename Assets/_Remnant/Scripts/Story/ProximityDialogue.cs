using System.Collections;
using Remnant.Core;
using Remnant.Dialogue;
using Remnant.Player;
using UnityEngine;

namespace Remnant.Story
{
    /// <summary>
    /// Plays a dialogue once, when its conditions hold. With a Target the player has to come within Radius of it;
    /// with no Target it plays as soon as the scene has finished its transition (an arrival line). Then it sets a flag
    /// so the beat never repeats.
    /// </summary>
    public class ProximityDialogue : MonoBehaviour
    {
        [SerializeField] private DialogueRunner dialogueRunner;
        [SerializeField] private DialogueData dialogue;

        [Header("Where")]
        [Tooltip("Leave empty for an arrival line (plays once the scene has faded in).")]
        [SerializeField] private Transform target;
        [SerializeField, Min(0.1f)] private float radius = 5f;

        [Header("Conditions")]
        [Tooltip("Every listed flag must be set (comma separated). Empty = no requirement.")]
        [SerializeField] private string requiresFlag = "act4";
        [Tooltip("Normally the same as Set Flag, so the beat plays only once. Several flags (comma separated): any of them blocks it.")]
        [SerializeField] private string forbidsFlag;
        [Tooltip("Also forbidden while this other flag is set (for example the dark encounter).")]
        [SerializeField] private string alsoForbidsFlag;

        [Header("After")]
        [SerializeField] private string setFlag;
        [SerializeField] private bool refreshCheckpointFlags = true;

        private PlayerController player;
        private bool running;

        private void Update()
        {
            if (running) return;

            GameState state = GameState.Instance;
            if (!string.IsNullOrWhiteSpace(requiresFlag) && (state == null || !state.HasAllFlags(requiresFlag))) return;
            if (state != null && state.HasAnyFlag(forbidsFlag)) return;
            if (state != null && state.HasAnyFlag(alsoForbidsFlag)) return;
            if (SceneTransition.Instance != null && SceneTransition.Instance.IsTransitioning) return;

            if (player == null) player = FindFirstObjectByType<PlayerController>();
            if (player == null) return;

            if (target != null && Vector2.Distance(player.transform.position, target.position) > radius) return;

            running = true;
            StartCoroutine(Play());
        }

        private IEnumerator Play()
        {
            // Never talk over another conversation.
            while (dialogueRunner != null && dialogueRunner.BlocksInteraction) yield return null;

            if (dialogueRunner != null) dialogueRunner.Play(dialogue, Finish);
            else Finish();
        }

        private void Finish()
        {
            GameState state = GameState.Instance;
            if (state != null && !string.IsNullOrEmpty(setFlag))
            {
                state.SetFlag(setFlag);
                if (refreshCheckpointFlags) state.RefreshCheckpointFlags();
            }
        }
    }
}
