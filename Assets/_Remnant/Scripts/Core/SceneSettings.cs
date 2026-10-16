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

        private void Awake()
        {
            GameState state = GameState.Instance;
            if (state == null)
            {
                Debug.LogWarning("SceneSettings: no GameState in the scene.", this);
                return;
            }

            state.SetPerspective(perspective);
            state.SetTimePeriod(timePeriod);
        }
    }
}
