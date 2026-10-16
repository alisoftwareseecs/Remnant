using Remnant.Player;
using UnityEngine;

namespace Remnant.Characters
{
    public enum FacingDirection { Down, Left, Right, Up }

    /// <summary>
    /// Animates RPG Maker style sheets: 4 directions x 3 frames. Walk cycle 0-1-2-1, idle = frame 1.
    /// With a PlayerController assigned it follows the player's movement; without one it shows a fixed facing (NPCs).
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class CharacterSpriteAnimator : MonoBehaviour
    {
        private static readonly int[] WalkCycle = { 0, 1, 2, 1 };
        private const int FramesPerDirection = 3;

        [Tooltip("12 sprites ordered Down 0-2, Left 0-2, Right 0-2, Up 0-2.")]
        [SerializeField] private Sprite[] frames = new Sprite[12];

        [Tooltip("Leave empty for NPCs.")]
        [SerializeField] private PlayerController playerSource;

        [Tooltip("Fixed facing when no player source is assigned.")]
        [SerializeField] private FacingDirection facing = FacingDirection.Down;

        [SerializeField, Min(1f)] private float framesPerSecond = 8f;
        [SerializeField, Min(1f)] private float runSpeedMultiplier = 1.4f;

        private SpriteRenderer spriteRenderer;
        private float animationTime;

        public FacingDirection Facing => facing;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void OnEnable()
        {
            animationTime = 0f;
            Apply(false);
        }

        public void SetFacing(FacingDirection direction)
        {
            facing = direction;
            if (playerSource == null && spriteRenderer != null) Apply(false);
        }

        private void LateUpdate()
        {
            bool moving = false;

            if (playerSource != null)
            {
                moving = playerSource.IsMoving;
                facing = DirectionFromVector(playerSource.LastMoveDirection);
            }

            if (moving)
            {
                float speed = playerSource.IsRunning ? runSpeedMultiplier : 1f;
                animationTime += Time.deltaTime * framesPerSecond * speed;
            }
            else
            {
                animationTime = 0f;
            }

            Apply(moving);
        }

        private void Apply(bool moving)
        {
            if (frames == null || frames.Length < FramesPerDirection * 4) return;

            int column = moving ? WalkCycle[Mathf.FloorToInt(animationTime) % WalkCycle.Length] : 1;
            Sprite sprite = frames[(int)facing * FramesPerDirection + column];
            if (sprite != null) spriteRenderer.sprite = sprite;
        }

        public static FacingDirection DirectionFromVector(Vector2 v)
        {
            if (v.sqrMagnitude < 0.0001f) return FacingDirection.Down;
            if (Mathf.Abs(v.x) > Mathf.Abs(v.y)) return v.x > 0f ? FacingDirection.Right : FacingDirection.Left;
            return v.y > 0f ? FacingDirection.Up : FacingDirection.Down;
        }
    }
}
