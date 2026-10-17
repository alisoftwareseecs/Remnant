using Remnant.Player;
using UnityEngine;

namespace Remnant.Audio
{
    /// <summary>
    /// Makes the rain come in through the walls of the classroom instead of from nowhere: the loop (light or heavy, whatever
    /// is playing under the id) is split into a left and a right source, each panned to its side, and each side's volume
    /// follows how close the player is to that wall, horizontally: full right next to the wall, fading smoothly, silent
    /// from the fade distance on (about the middle of the room). Walking left and right brings the rain in and out of each
    /// ear. All the start, cut and fade rules of the rain stay with SceneAmbience and the story scripts; this only steers
    /// the two sides. The wall positions, fade distance and maximum volume are Inspector values.
    /// </summary>
    public class WindowRain : MonoBehaviour
    {
        [SerializeField] private string loopId = "rain";

        [Header("The window walls (world x)")]
        [SerializeField] private float leftWallX = 2f;
        [SerializeField] private float rightWallX = 14f;

        [Header("Volume")]
        [Tooltip("Silent from this horizontal distance to a wall on (about the middle of the room).")]
        [SerializeField, Min(0.1f)] private float fadeDistance = 6f;
        [Tooltip("Volume of a side right next to its wall (0-1).")]
        [SerializeField, Range(0f, 1f)] private float maxVolume = 1f;
        [Tooltip("1 = linear, higher = the rain stays quiet until you are closer to the wall.")]
        [SerializeField, Range(0.5f, 4f)] private float curvePower = 1.3f;
        [Tooltip("How far each side is panned (1 = hard left / hard right).")]
        [SerializeField, Range(0f, 1f)] private float panAmount = 0.9f;

        private PlayerController player;

        /// <summary>The volume of the left and right side right now (for tests).</summary>
        public Vector2 CurrentVolumes { get; private set; }

        private void OnDisable()
        {
            AudioLoops.ClearSides(loopId);
        }

        private void Update()
        {
            if (player == null) player = FindFirstObjectByType<PlayerController>();
            if (player == null) return;

            float x = player.transform.position.x;
            float left = SideVolume(Mathf.Abs(x - leftWallX));
            float right = SideVolume(Mathf.Abs(x - rightWallX));
            CurrentVolumes = new Vector2(left, right);
            AudioLoops.SetSides(loopId, left, right, panAmount);
        }

        private float SideVolume(float distance)
        {
            float t = 1f - Mathf.Clamp01(distance / fadeDistance);
            return maxVolume * Mathf.Pow(Mathf.SmoothStep(0f, 1f, t), curvePower);
        }
    }
}
