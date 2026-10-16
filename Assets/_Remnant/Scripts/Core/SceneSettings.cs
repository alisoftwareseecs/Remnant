using Remnant.Player;
using UnityEngine;

namespace Remnant.Core
{
    /// <summary>
    /// One per scene. Declares the scene's starting perspective and time period and applies them to the
    /// GameState on load, including when the persistent GameState came from an earlier scene.
    /// </summary>
    [DefaultExecutionOrder(-900)]
    public class SceneSettings : MonoBehaviour
    {
        [SerializeField] private Perspective perspective = Perspective.SideView;
        [SerializeField] private TimePeriod timePeriod = TimePeriod.Present;

        [Header("Movement limits")]
        [Tooltip("Turn off to stop the player from running in this scene.")]
        [SerializeField] private bool allowRunning = true;
        [Tooltip("Scales the player's walk and run speed in this scene (1 = normal).")]
        [Range(0.2f, 1f)] [SerializeField] private float walkSpeedMultiplier = 1f;

#if UNITY_EDITOR
        [Header("Editor testing only")]
        [Tooltip("Story flags set when Play is pressed directly in this scene (not after a scene transition). Stripped from builds. " +
                 "Useful ones: day2, talked_aiko, shadow_awake, corridor_intro (skips the opening Shadow lines), no_monster (the monster never appears), " +
                 "classroom2b_intro, page_1 to page_6 and diary_complete (diary done, back door open), mirror_seen, storage_revealed.")]
        [SerializeField] private string[] debugStartFlags = new string[0];
#endif

        private void Awake()
        {
            GameState state = GameState.Instance;
            if (state == null)
            {
                Debug.LogWarning("SceneSettings: no GameState in the scene.", this);
                return;
            }

#if UNITY_EDITOR
            if (!SceneTransition.HasTransitioned)
            {
                foreach (string flag in debugStartFlags) state.SetFlag(flag);
                if (debugStartFlags.Length > 0)
                {
                    Debug.Log("[SceneSettings] Debug start flags applied: " + string.Join(", ", debugStartFlags), this);
                }
            }
#endif

            state.SetPerspective(perspective);
            state.SetTimePeriod(timePeriod);
        }

        private void Start()
        {
            PlayerController player = FindFirstObjectByType<PlayerController>();
            if (player != null) player.SetMovementLimits(allowRunning, walkSpeedMultiplier);
        }
    }
}
