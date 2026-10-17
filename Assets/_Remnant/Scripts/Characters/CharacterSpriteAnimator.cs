using Remnant.Core;
using Remnant.Player;
using UnityEngine;

namespace Remnant.Characters
{
    public enum FacingDirection { Down, Left, Right, Up }

    /// <summary>
    /// Animates RPG Maker style sheets: 4 directions x 3 frames. Walk cycle 0-1-2-1, idle = frame 1.
    /// With a PlayerController assigned it follows the player's movement; without one it shows a fixed facing (NPCs).
    /// In SideView the player uses the same protagonist art (the left and right rows, imported as a separate sheet so
    /// it can have its own scale): the same 0-1-2-1 walk cycle, idle = the standing frame, no mirroring. The walk
    /// cycle runs at the speed he really moves, so when the story slows him down his steps slow down with him.
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

        [Header("Side view (player only)")]
        [Tooltip("6 sprites ordered Left 0-2, Right 0-2 (the protagonist's side-view sheet).")]
        [SerializeField] private Sprite[] sideFrames = new Sprite[6];
        [Tooltip("Walk cycle frames per second at normal walking speed; it scales with his real speed.")]
        [SerializeField, Min(1f)] private float sideWalkFramesPerSecond = 8f;
        [Tooltip("The slowest the cycle runs while he is still trying to move (a shuffle instead of standing frozen mid-step).")]
        [SerializeField, Range(0.05f, 1f)] private float sideMinSpeedShare = 0.15f;

        private SpriteRenderer spriteRenderer;
        private float animationTime;
        private float sideTime;
        private bool wasSideView;

        public FacingDirection Facing => facing;

        private bool HasSideSprites => sideFrames != null && sideFrames.Length >= 6;

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

            spriteRenderer.flipX = false;

            bool moving = playerSource.IsMoving;
            int column = 1;   // standing frame
            if (moving)
            {
                float reference = Mathf.Max(0.01f, playerSource.EffectiveWalkSpeed);
                float share = Mathf.Clamp(playerSource.ActualHorizontalSpeed / reference, sideMinSpeedShare, runSpeedMultiplier * 1.3f);
                sideTime += Time.deltaTime * sideWalkFramesPerSecond * share;
                column = WalkCycle[Mathf.FloorToInt(sideTime) % WalkCycle.Length];
            }
            else
            {
                sideTime = 0f;
            }

            int row = playerSource.FacingDirection < 0 ? 0 : 1;
            Sprite sprite = sideFrames[row * FramesPerDirection + column];
            if (sprite != null) spriteRenderer.sprite = sprite;
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
