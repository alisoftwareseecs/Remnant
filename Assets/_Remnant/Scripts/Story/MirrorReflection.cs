using Remnant.Player;
using UnityEngine;

namespace Remnant.Story
{
    /// <summary>
    /// The reflection in the cracked mirror. It only ever shows the protagonist: a small front-facing figure
    /// drawn inside the glass (the reflection renderer must use "Visible Inside Mask" with a SpriteMask on the glass).
    /// The Shadow is deliberately never drawn here, because it is not part of him and does not appear in mirrors.
    /// </summary>
    public class MirrorReflection : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer reflection;
        [Tooltip("What the mirror shows: the protagonist seen from the front.")]
        [SerializeField] private Sprite frontSprite;
        [SerializeField] private Color tint = new Color(0.55f, 0.6f, 0.72f, 0.8f);

        [Header("Glass (local space of this object, whose origin is the bottom centre of the mirror)")]
        [SerializeField] private float glassBottom = 0.15f;
        [SerializeField, Min(0.1f)] private float glassHalfWidth = 0.3f;

        [Header("When it shows")]
        [Tooltip("Horizontal distance from the mirror at which the reflection appears.")]
        [SerializeField, Min(0.5f)] private float visibleDistanceX = 3.5f;
        [Tooltip("How far below the mirror (in front of it) the player can be for the reflection to appear.")]
        [SerializeField, Min(0.5f)] private float visibleDistanceY = 4.5f;

        private PlayerController player;

        private void Awake()
        {
            if (reflection != null)
            {
                reflection.sprite = frontSprite;
                reflection.color = tint;
                reflection.enabled = false;
            }
        }

        private void LateUpdate()
        {
            if (reflection == null) return;

            if (player == null)
            {
                player = FindFirstObjectByType<PlayerController>();
                if (player == null) return;
            }

            Vector3 offset = player.transform.position - transform.position;
            bool near = Mathf.Abs(offset.x) < visibleDistanceX && offset.y < 0.3f && offset.y > -visibleDistanceY;
            reflection.enabled = near;
            if (!near) return;

            float closeness = 1f - Mathf.Clamp01(-offset.y / visibleDistanceY);
            float scale = Mathf.Lerp(0.35f, 0.68f, closeness);
            float x = Mathf.Clamp(offset.x * 0.35f, -glassHalfWidth, glassHalfWidth);

            reflection.transform.localPosition = new Vector3(x, glassBottom, 0f);
            reflection.transform.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
