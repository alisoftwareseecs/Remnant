using Remnant.Core;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Remnant.Story
{
    /// <summary>
    /// Gives a light different settings while a story flag is set (for example the bright warm daylight of the ending).
    /// Applied when the scene starts and whenever the flag changes.
    /// </summary>
    public class StoryLightState : MonoBehaviour
    {
        [SerializeField] private Light2D targetLight;
        [SerializeField] private string flag = "ending_light";
        [SerializeField, Min(0f)] private float intensity = 1f;
        [SerializeField] private Color color = new Color(1f, 0.93f, 0.78f);

        private GameState state;

        private void Start()
        {
            state = GameState.Instance;
            if (state != null) state.FlagChanged += OnFlagChanged;
            Apply();
        }

        private void OnDestroy()
        {
            if (state != null) state.FlagChanged -= OnFlagChanged;
        }

        private void OnFlagChanged(string changed, bool value)
        {
            if (changed == flag) Apply();
        }

        private void Apply()
        {
            if (targetLight == null || state == null || !state.HasFlag(flag)) return;

            targetLight.intensity = intensity;
            targetLight.color = color;
        }
    }
}
