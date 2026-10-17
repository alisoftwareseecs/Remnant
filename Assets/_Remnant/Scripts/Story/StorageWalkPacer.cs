using Remnant.Core;
using Remnant.Dialogue;
using Remnant.Player;
using UnityEngine;

namespace Remnant.Story
{
    /// <summary>
    /// Paces the walk down the storage hallway to the Shadow's Auto lines. Over the last third of the way the player's
    /// walk speed eases down, scaled by how much of the walk dialogue is still left to play, so that he reaches the
    /// reveal point at about the moment the last line has faded. Once the lines are done the walk is back to the normal
    /// slow speed. He can never pass the reveal point before the lines are done: there is no wall, the speed has simply
    /// run out, as if his legs were giving out.
    /// </summary>
    public class StorageWalkPacer : MonoBehaviour
    {
        [SerializeField] private DialogueRunner dialogueRunner;
        [Tooltip("The Shadow's lines during the walk (the ones that set the pace).")]
        [SerializeField] private DialogueData walkDialogue;
        [Tooltip("Where the walk starts (the spawn point).")]
        [SerializeField] private Transform walkStart;
        [Tooltip("The reveal point: the farthest the player gets while the lines are still playing.")]
        [SerializeField] private Transform revealPoint;
        [Tooltip("The easing starts this share of the way before the reveal point.")]
        [SerializeField, Range(0.1f, 0.6f)] private float slowZoneShare = 1f / 3f;
        [Tooltip("He should reach the reveal point this long after the last line has faded.")]
        [SerializeField, Min(0f)] private float arriveAfterLinesSeconds = 1f;
        [Tooltip("The lowest speed share while the lines are still playing (his legs are nearly gone).")]
        [SerializeField, Range(0.01f, 0.3f)] private float minimumSpeedShare = 0.05f;
        [Tooltip("How fast the speed share may change per second, so it eases instead of jerking.")]
        [SerializeField, Min(0.1f)] private float easeRatePerSecond = 0.6f;
        [Tooltip("Set when the walk lines have started (StoryTrigger on the walk).")]
        [SerializeField] private string walkStartedFlag = "storage_walk";
        [Tooltip("The pacing is off in the daylight ending.")]
        [SerializeField] private string offWhenFlag = "ending_light";

        private PlayerController player;
        private float factor = 1f;

        private void OnDisable()
        {
            if (player != null) player.SetSpeedFactor(1f);
        }

        private void Update()
        {
            GameState state = GameState.Instance;
            if (state == null || revealPoint == null || walkStart == null || state.HasAnyFlag(offWhenFlag)) return;

            if (player == null) player = FindFirstObjectByType<PlayerController>();
            if (player == null) return;

            float revealX = revealPoint.position.x;
            float length = Mathf.Max(1f, revealX - walkStart.position.x);
            float zoneStartX = revealX - length * slowZoneShare;
            float x = player.transform.position.x;

            float remaining = SecondsLeft(state);
            float target = 1f;

            if (remaining > 0f && x > zoneStartX)
            {
                // arrive at the reveal point as the last line fades: speed = distance left / time left
                float baseSpeed = Mathf.Max(0.01f, player.EffectiveWalkSpeed);
                float wanted = Mathf.Max(0f, revealX - x) / (remaining + arriveAfterLinesSeconds);
                target = Mathf.Clamp(wanted / baseSpeed, minimumSpeedShare, 1f);
            }

            factor = Mathf.MoveTowards(factor, target, easeRatePerSecond * Time.deltaTime);
            player.SetSpeedFactor(factor);
        }

        private void LateUpdate()
        {
            GameState state = GameState.Instance;
            if (state == null || revealPoint == null || player == null || state.HasAnyFlag(offWhenFlag)) return;
            if (SecondsLeft(state) <= 0f) return;

            // The lines are not done: he cannot go past the reveal point. By now his speed is almost nothing.
            float limit = revealPoint.position.x;
            Vector3 position = player.transform.position;
            if (position.x <= limit) return;

            position.x = limit;
            player.transform.position = position;
            Rigidbody2D body = player.GetComponent<Rigidbody2D>();
            if (body != null) body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
        }

        /// <summary>Seconds the walk lines still need (0 when they are finished).</summary>
        private float SecondsLeft(GameState state)
        {
            if (dialogueRunner == null) return 0f;

            float left = 0f;
            if (dialogueRunner.IsOpen && dialogueRunner.CurrentDialogue == walkDialogue) left = dialogueRunner.EstimatedRemainingSeconds;
            else if (!state.HasFlag(walkStartedFlag)) left = dialogueRunner.EstimateDurationSeconds(walkDialogue);

            return left;
        }
    }
}
