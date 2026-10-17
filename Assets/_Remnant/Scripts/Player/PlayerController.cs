using Remnant.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Remnant.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Input (from InputSystem_Actions)")]
        [SerializeField] private InputActionReference moveAction;
        [SerializeField] private InputActionReference sprintAction;

        [Header("Speed")]
        [SerializeField, Min(0f)] private float walkSpeed = 2.5f;
        [SerializeField, Min(0f)] private float runSpeed = 4.5f;

        [Header("Side view")]
        [Tooltip("Gravity keeps the player on the floor in side view. Top-down always uses 0.")]
        [SerializeField, Min(0f)] private float sideViewGravityScale = 3f;

        private Rigidbody2D body;
        private Perspective perspective = Perspective.SideView;
        private Vector2 moveInput;
        private bool isRunning;
        private bool movementLocked;
        private int facingOverride;
        private bool runAllowed = true;
        private float speedMultiplier = 1f;
        private float speedFactor = 1f;
        private GameState subscribedState;

        public Perspective CurrentPerspective => perspective;
        public Vector2 MoveInput => moveInput;
        public bool IsRunning => isRunning;
        public bool IsMoving => moveInput.sqrMagnitude > 0.0001f;
        public bool IsMovementLocked => movementLocked;
        public int FacingDirection { get; private set; } = 1;
        /// <summary>How fast he is really moving along the floor right now, in units per second (0 when blocked or standing).</summary>
        public float ActualHorizontalSpeed => body != null ? Mathf.Abs(body.linearVelocity.x) : 0f;
        /// <summary>Last non-zero movement direction (unit vector). Starts facing down. Used for sprite facing.</summary>
        public Vector2 LastMoveDirection { get; private set; } = Vector2.down;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        private void OnEnable()
        {
            if (moveAction != null) moveAction.action.Enable();
            if (sprintAction != null) sprintAction.action.Enable();
        }

        private void Start()
        {
            subscribedState = GameState.Instance;
            if (subscribedState != null)
            {
                subscribedState.PerspectiveChanged += ApplyPerspective;
                ApplyPerspective(subscribedState.CurrentPerspective);
            }
            else
            {
                Debug.LogWarning("PlayerController: no GameState in the scene. Defaulting to SideView.", this);
                ApplyPerspective(Perspective.SideView);
            }
        }

        private void OnDestroy()
        {
            if (subscribedState != null)
            {
                subscribedState.PerspectiveChanged -= ApplyPerspective;
            }
        }

        private void Update()
        {
            if (movementLocked || moveAction == null)
            {
                moveInput = Vector2.zero;
                isRunning = false;
                return;
            }

            Vector2 raw = moveAction.action.ReadValue<Vector2>();
            moveInput = perspective == Perspective.SideView
                ? new Vector2(raw.x, 0f)
                : Vector2.ClampMagnitude(raw, 1f);

            isRunning = runAllowed && sprintAction != null && sprintAction.action.IsPressed() && IsMoving;

            if (IsMoving) LastMoveDirection = moveInput.normalized;

            if (moveInput.x > 0.01f) FacingDirection = 1;
            else if (moveInput.x < -0.01f) FacingDirection = -1;

            if (facingOverride != 0) FacingDirection = facingOverride;
        }

        private void FixedUpdate()
        {
            float speed = (isRunning ? runSpeed : walkSpeed) * speedMultiplier * speedFactor;

            if (perspective == Perspective.SideView)
            {
                body.linearVelocity = new Vector2(moveInput.x * speed, body.linearVelocity.y);
            }
            else
            {
                body.linearVelocity = moveInput * speed;
            }
        }

        /// <summary>The walk speed in this scene (after the scene's limits, before any story speed factor).</summary>
        public float EffectiveWalkSpeed => walkSpeed * speedMultiplier;

        /// <summary>
        /// A temporary story-driven speed scale on top of the scene limits (1 = none). Used where the player's legs
        /// should give out slowly, such as the storage hallway.
        /// </summary>
        public void SetSpeedFactor(float factor)
        {
            speedFactor = Mathf.Clamp01(factor);
        }

        /// <summary>Per-scene movement limits (set by SceneSettings): disable running and/or scale all speeds.</summary>
        public void SetMovementLimits(bool allowRun, float speedScale)
        {
            runAllowed = allowRun;
            speedMultiplier = Mathf.Max(0f, speedScale);
            if (!runAllowed) isRunning = false;
        }

        /// <summary>Forces the side-view facing (-1 left, +1 right), e.g. towards a flashlight beam. 0 gives control back to movement.</summary>
        public void SetFacingOverride(int direction)
        {
            facingOverride = direction == 0 ? 0 : (direction > 0 ? 1 : -1);
            if (facingOverride != 0) FacingDirection = facingOverride;
        }

        /// <summary>Turns the TopDown sprite to look along this direction without moving (e.g. at the start of a scene).</summary>
        public void SetLastMoveDirection(Vector2 direction)
        {
            if (direction.sqrMagnitude > 0.0001f) LastMoveDirection = direction.normalized;
        }

        public void SetMovementLocked(bool locked)
        {
            movementLocked = locked;
            if (locked)
            {
                moveInput = Vector2.zero;
                isRunning = false;
            }
        }

        private void ApplyPerspective(Perspective newPerspective)
        {
            perspective = newPerspective;
            body.gravityScale = newPerspective == Perspective.SideView ? sideViewGravityScale : 0f;
            body.linearVelocity = Vector2.zero;
        }
    }
}
