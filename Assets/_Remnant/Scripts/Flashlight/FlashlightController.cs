using Remnant.Core;
using Remnant.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

namespace Remnant.Flashlight
{
    /// <summary>
    /// The player's flashlight: a URP 2D spot light at chest height that aims at the mouse (world space) or the
    /// right stick. On in the Future, in both perspectives: in SideView the player turns to face the side it points to,
    /// in TopDown it aims all the way round. Other systems (the monster, the reveal) ask Illuminates().
    /// </summary>
    public class FlashlightController : MonoBehaviour
    {
        public static FlashlightController Active { get; private set; }

        [Header("Input (Player/Look from InputSystem_Actions)")]
        [Tooltip("Used for the gamepad right stick. The mouse is read by position.")]
        [SerializeField] private InputActionReference lookAction;

        [Header("References")]
        [SerializeField] private Light2D beam;
        [SerializeField] private PlayerController player;

        [Header("When it works")]
        [Tooltip("Normally the beam is on only in the Future. Turn this on for a Present scene that is dark on purpose (the Act 4 encounter).")]
        [SerializeField] private bool alwaysOn;
        [Tooltip("The beam stays off while this flag is set (the daylight ending).")]
        [SerializeField] private string offWhenFlag = "ending_light";

        [Header("Beam")]
        [SerializeField, Min(0.1f)] private float baseIntensity = 1.3f;
        [Range(0f, 0.3f)] [SerializeField] private float flickerAmount = 0.05f;
        [SerializeField, Min(0.1f)] private float flickerSpeed = 7f;
        [Tooltip("Share of the visible cone that counts as 'lit' for gameplay (monster banishing).")]
        [Range(0.5f, 1f)] [SerializeField] private float effectiveConeShare = 0.85f;

        [Header("Aiming")]
        [Range(0.1f, 0.9f)] [SerializeField] private float stickDeadzone = 0.3f;
        [SerializeField, Min(1f)] private float turnSpeedDegrees = 720f;

        private Camera mainCamera;
        private GameState state;
        private Vector2 aimDirection = Vector2.right;
        private Vector2 lockedDirection;
        private bool aimLocked;
        private float intensityBoost = 1f;
        private bool useStick;
        private float flickerUntil;
        private bool lit;
        private float noiseSeed;

        public bool IsOn => lit;
        /// <summary>True once the beam has been pointed at the floor for good.</summary>
        public bool IsLowered { get; private set; }
        public Vector2 Origin => transform.position;
        public Vector2 Direction => aimDirection;
        public float Range => beam != null ? beam.pointLightOuterRadius : 0f;
        public float EffectiveHalfAngle => beam != null ? beam.pointLightOuterAngle * 0.5f * effectiveConeShare : 0f;

        private void Awake()
        {
            noiseSeed = Random.value * 50f;
            if (player == null) player = GetComponentInParent<PlayerController>();
        }

        private void OnEnable()
        {
            if (lookAction != null) lookAction.action.Enable();
        }

        private void Start()
        {
            state = GameState.Instance;
            if (state != null)
            {
                state.PerspectiveChanged += OnPerspectiveChanged;
                state.TimePeriodChanged += OnTimePeriodChanged;
            }

            Refresh();
        }

        private void OnDestroy()
        {
            if (state != null)
            {
                state.PerspectiveChanged -= OnPerspectiveChanged;
                state.TimePeriodChanged -= OnTimePeriodChanged;
            }

            if (Active == this) Active = null;
        }

        private void OnDisable()
        {
            if (Active == this) Active = null;
        }

        private void OnPerspectiveChanged(Perspective perspective) => Refresh();
        private void OnTimePeriodChanged(TimePeriod timePeriod) => Refresh();

        private void Refresh()
        {
            bool shouldBeOn = alwaysOn || (state != null && state.CurrentTimePeriod == TimePeriod.Future);
            if (state != null && state.HasFlag(offWhenFlag)) shouldBeOn = false;

            lit = shouldBeOn;
            if (beam != null) beam.enabled = shouldBeOn;

            if (shouldBeOn) Active = this;
            else
            {
                if (Active == this) Active = null;
                if (player != null) player.SetFacingOverride(0);
            }
        }

        private void Update()
        {
            if (!lit || beam == null) return;

            if (mainCamera == null) mainCamera = Camera.main;
            UpdateAim();
            UpdateFlicker();

            if (player == null) return;

            // In SideView the player faces the side the light points to. In TopDown movement decides the facing.
            bool sideView = state != null && state.CurrentPerspective == Perspective.SideView;
            if (sideView && Mathf.Abs(aimDirection.x) > 0.05f) player.SetFacingOverride(aimDirection.x > 0f ? 1 : -1);
            else player.SetFacingOverride(0);
        }

        /// <summary>Makes the beam turn to this direction and ignore the mouse and stick until UnlockAim().</summary>
        public void LockAim(Vector2 direction)
        {
            if (direction.sqrMagnitude < 0.0001f) return;
            lockedDirection = direction.normalized;
            aimLocked = true;
        }

        /// <summary>Multiplies the beam brightness (1 = normal). Used to make the beam stronger in a story moment.</summary>
        public void SetIntensityBoost(float boost)
        {
            intensityBoost = Mathf.Max(0f, boost);
        }

        public void UnlockAim()
        {
            aimLocked = false;
        }

        /// <summary>Points the beam at the floor, permanently. The player can no longer aim it.</summary>
        public void LowerBeam()
        {
            IsLowered = true;
            LockAim(Vector2.down);
        }

        /// <summary>Makes the beam stutter and dim for a moment, as if the light itself were afraid.</summary>
        public void Flicker(float seconds)
        {
            flickerUntil = Time.time + seconds;
        }

        /// <summary>
        /// True when the point is inside the effective cone and range, and no solid collider is in the way.
        /// The player and anything under ignoreRoot do not block the line of sight.
        /// </summary>
        public bool Illuminates(Vector2 point, Transform ignoreRoot = null)
        {
            if (!lit || beam == null) return false;

            Vector2 toPoint = point - Origin;
            if (toPoint.magnitude > Range) return false;
            if (Vector2.Angle(aimDirection, toPoint) > EffectiveHalfAngle) return false;

            foreach (RaycastHit2D hit in Physics2D.LinecastAll(Origin, point))
            {
                Collider2D collider = hit.collider;
                if (collider == null || collider.isTrigger) continue;
                if (collider.GetComponentInParent<PlayerController>() != null) continue;
                if (ignoreRoot != null && collider.transform.IsChildOf(ignoreRoot)) continue;
                return false;
            }

            return true;
        }

        private void UpdateAim()
        {
            Vector2 target = aimDirection;

            if (aimLocked)
            {
                target = lockedDirection;
            }
            else if (lookAction != null)
            {
                Vector2 stick = lookAction.action.ReadValue<Vector2>();
                InputControl control = lookAction.action.activeControl;
                bool fromGamepad = control != null && control.device is Gamepad;
                if (fromGamepad && stick.magnitude > stickDeadzone)
                {
                    useStick = true;
                    target = stick.normalized;
                }
            }

            Mouse mouse = Mouse.current;
            if (!aimLocked && mouse != null && mouse.delta.ReadValue().sqrMagnitude > 0.25f) useStick = false;

            if (!aimLocked && !useStick && mouse != null && mainCamera != null)
            {
                Vector2 screen = mouse.position.ReadValue();
                Vector3 world = mainCamera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -mainCamera.transform.position.z));
                Vector2 toMouse = (Vector2)world - Origin;
                if (toMouse.sqrMagnitude > 0.04f) target = toMouse.normalized;
            }

            float current = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
            float wanted = Mathf.Atan2(target.y, target.x) * Mathf.Rad2Deg;
            float next = Mathf.MoveTowardsAngle(current, wanted, turnSpeedDegrees * Time.deltaTime);
            aimDirection = new Vector2(Mathf.Cos(next * Mathf.Deg2Rad), Mathf.Sin(next * Mathf.Deg2Rad));

            // A URP 2D spot light points along its local up axis.
            transform.rotation = Quaternion.Euler(0f, 0f, next - 90f);
        }

        private void UpdateFlicker()
        {
            float t = Time.time * flickerSpeed + noiseSeed;
            float noise = Mathf.PerlinNoise(t, 0.37f) - 0.5f;
            float strobe = 1f;
            if (Time.time < flickerUntil) strobe = Random.value < 0.45f ? Random.Range(0f, 0.25f) : Random.Range(0.6f, 1.1f);

            beam.intensity = baseIntensity * intensityBoost * strobe * (1f + noise * 2f * flickerAmount);
        }
    }
}
