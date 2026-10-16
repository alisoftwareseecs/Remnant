using Remnant.Core;
using Remnant.Player;
using UnityEngine;

namespace Remnant.Characters
{
    public enum FacingDirection { Down, Left, Right, Up }

    /// <summary>
    /// Animates RPG Maker style sheets: 4 directions x 3 frames. Walk cycle 0-1-2-1, idle = frame 1.
    /// With a PlayerController assigned it follows the player's movement; without one it shows a fixed facing (NPCs).
    /// When the player is in SideView and the side-view sprite sets are filled in, it switches to those
    /// (idle and walk loops, flipped horizontally for left/right).
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

        [Header("Side view (optional, player only)")]
        [Tooltip("Idle loop used in SideView. Drawn facing right; flipped for left.")]
        [SerializeField] private Sprite[] sideIdleFrames = new Sprite[0];
        [Tooltip("Walk loop used in SideView. Drawn facing right; flipped for left.")]
        [SerializeField] private Sprite[] sideWalkFrames = new Sprite[0];
        [SerializeField, Min(0.1f)] private float sideIdleFramesPerSecond = 3f;
        [SerializeField, Min(0.1f)] private float sideWalkFramesPerSecond = 10f;

        private SpriteRenderer spriteRenderer;
        private float animationTime;
        private float sideTime;
        private bool wasSideView;

        public FacingDirection Facing => facing;

        private bool HasSideSprites =>
            sideIdleFrames != null && sideIdleFrames.Length > 0 && sideWalkFrames != null && sideWalkFrames.Length > 0;

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
            bool sideView = playerSource != null && HasSideSprites &&
                            playerSource.CurrentPerspective == Perspective.SideView;

            if (sideView)
            {
                ApplySideView();
                return;
            }

            if (wasSideView)
            {
                wasSideView = false;
                spriteRenderer.flipX = false;
            }

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

        private void ApplySideView()
        {
            if (!wasSideView)
            {
                wasSideView = true;
                sideTime = 0f;
            }

            bool moving = playerSource.IsMoving;
            Sprite[] set = moving ? sideWalkFrames : sideIdleFrames;
            float fps = moving ? sideWalkFramesPerSecond * (playerSource.IsRunning ? runSpeedMultiplier : 1f)
                               : sideIdleFramesPerSecond;

            sideTime += Time.deltaTime * fps;
            Sprite sprite = set[Mathf.FloorToInt(sideTime) % set.Length];
            if (sprite != null) spriteRenderer.sprite = sprite;

            spriteRenderer.flipX = playerSource.FacingDirection < 0;
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
