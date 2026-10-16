using Remnant.Flashlight;
using UnityEngine;

namespace Remnant.Story
{
    /// <summary>
    /// Keeps a cast shadow on the wall behind a hanging object. The shadow is thrown away from the flashlight:
    /// the further the light is to one side, the further the shadow lies to the other side. Put a HangingSway on the
    /// shadow object too, with different values from the body, so the two sway out of sync.
    /// </summary>
    public class CastShadowFollower : MonoBehaviour
    {
        [SerializeField] private Transform source;
        [Tooltip("Where the shadow lies when no light is on it.")]
        [SerializeField] private Vector2 restOffset = new Vector2(0.45f, -0.04f);
        [Tooltip("Sideways offset per unit between the light and the source.")]
        [SerializeField, Min(0f)] private float offsetPerUnit = 0.1f;
        [SerializeField, Min(0f)] private float maxSideOffset = 1.2f;
        [Tooltip("How much lower the shadow falls for every unit of sideways offset.")]
        [SerializeField, Min(0f)] private float dropPerOffset = 0.1f;
        [SerializeField, Min(0.1f)] private float smoothing = 6f;
        [SerializeField] private float depthBehindSource = 0.05f;

        private Vector2 current;
        private bool initialised;

        private void LateUpdate()
        {
            if (source == null) return;

            Vector2 target = restOffset;
            FlashlightController light = FlashlightController.Active;
            if (light != null)
            {
                float side = Mathf.Clamp((source.position.x - light.Origin.x) * offsetPerUnit, -maxSideOffset, maxSideOffset);
                target = new Vector2(side, -Mathf.Abs(side) * dropPerOffset);
            }

            if (!initialised)
            {
                current = target;
                initialised = true;
            }
            else
            {
                current = Vector2.Lerp(current, target, 1f - Mathf.Exp(-smoothing * Time.deltaTime));
            }

            transform.position = new Vector3(source.position.x + current.x, source.position.y + current.y,
                source.position.z + depthBehindSource);
        }
    }
}
