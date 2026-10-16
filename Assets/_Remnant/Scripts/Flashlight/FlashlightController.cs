using Remnant.Core;
using Remnant.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

namespace Remnant.Flashlight
{
    /// <summary>
    /// The player's flashlight: a URP 2D spot light at chest height that aims at the mouse (world space) or the
    /// right stick, and turns the player to face the side it points to. On only in SideView in the Future.
    /// Other systems (the monster) read Origin, Direction, Range and the effective cone from here.
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
        private bool useStick;
        private bool lit;
        private float noiseSeed;

        public bool IsOn => lit;
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
            bool shouldBeOn = state != null &&
                              state.CurrentPerspective == Perspective.SideView &&
                              state.CurrentTimePeriod == TimePeriod.Future;

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

            if (player != null && Mathf.Abs(aimDirection.x) > 0.05f)
            {
                player.SetFacingOverride(aimDirection.x > 0f ? 1 : -1);
            }
        }

        private void UpdateAim()
        {
            Vector2 target = aimDirection;

            if (lookAction != null)
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
            if (mouse != null && mouse.delta.ReadValue().sqrMagnitude > 0.25f) useStick = false;

            if (!useStick && mouse != null && mainCamera != null)
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
            beam.intensity = baseIntensity * (1f + noise * 2f * flickerAmount);
        }
    }
}
