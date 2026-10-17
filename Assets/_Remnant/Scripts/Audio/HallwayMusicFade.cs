using Remnant.Core;
using Remnant.Player;
using UnityEngine;

namespace Remnant.Audio
{
    /// <summary>
    /// Fades the storage hallway's music out by distance: it starts to fade this many units before the reveal point and is
    /// completely silent at the silent distance, which is chosen so that it is still quiet for a while of slow walking
    /// before the beam can reach her legs. The values are Inspector values; mind StorageWalkPacer, which slows the walk.
    /// </summary>
    public class HallwayMusicFade : MonoBehaviour
    {
        [SerializeField] private Transform revealPoint;
        [Tooltip("The music starts to fade this far (units) before the reveal point.")]
        [SerializeField, Min(0.1f)] private float fadeStartDistance = 5f;
        [Tooltip("It is completely silent from this far before the reveal point on.")]
        [SerializeField, Min(0f)] private float silentDistance = 2.5f;
        [Tooltip("No fade in the daylight ending (that walk has the other music).")]
        [SerializeField] private string offWhenFlag = "ending_light";

        private PlayerController player;
        private float lastFactor = 1f;

        /// <summary>1 = full music, 0 = silent.</summary>
        public float CurrentFactor => lastFactor;

        private void OnDestroy()
        {
            if (MusicManager.Instance != null) MusicManager.Instance.SetScale(1f);
        }

        private void Update()
        {
            MusicManager manager = MusicManager.Instance;
            GameState state = GameState.Instance;
            if (manager == null || revealPoint == null) return;
            if (state != null && state.HasAnyFlag(offWhenFlag)) return;

            if (player == null) player = FindFirstObjectByType<PlayerController>();
            if (player == null) return;

            float distance = revealPoint.position.x - player.transform.position.x;
            float t = Mathf.Clamp01(Mathf.InverseLerp(fadeStartDistance, silentDistance, distance));
            lastFactor = Mathf.SmoothStep(1f, 0f, t);
            manager.SetScale(lastFactor);
        }
    }
}
