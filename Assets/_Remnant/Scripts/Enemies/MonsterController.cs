using Remnant.Core;
using Remnant.Flashlight;
using Remnant.Player;
using UnityEngine;

namespace Remnant.Enemies
{
    /// <summary>
    /// Aiko's ghost in the SideView Future. Appears off-screen after a random delay (usually behind the player),
    /// creeps closer and speeds up. Held in the flashlight cone for a short time it is banished: it dissolves,
    /// fades out and comes back after a random delay. Touching the player is game over.
    /// Its white eyes are drawn on a separate unlit renderer so they show in the dark.
    /// </summary>
    public class MonsterController : MonoBehaviour
    {
        private enum Phase { Waiting, Approaching, Dissolving }

        [Header("Rendering")]
        [SerializeField] private SpriteRenderer bodyRenderer;
        [SerializeField] private SpriteRenderer eyesRenderer;
        [Tooltip("monster_0 to monster_11. 0-2 idle, 3-11 collapsing.")]
        [SerializeField] private Sprite[] bodyFrames = new Sprite[12];
        [Tooltip("The white eye pixels of each body frame, same order, drawn unlit.")]
        [SerializeField] private Sprite[] eyeFrames = new Sprite[12];
        [SerializeField] private Color bodyTint = new Color(0.62f, 0.74f, 0.95f, 1f);

        [Header("Story gating (flags)")]
        [Tooltip("The monster stays away while any of these flags is set (comma separated). Editor testing: add no_monster to the debug start flags.")]
        [SerializeField] private string disabledFlag = "no_monster,ending_light";
        [Tooltip("The monster only starts once this flag is set (so it does not interrupt the opening dialogue).")]
        [SerializeField] private string startFlag = "corridor_intro";

        [Header("Spawning")]
        [SerializeField] private Vector2 firstDelaySeconds = new Vector2(0.5f, 0.5f);
        [SerializeField] private Vector2 respawnDelaySeconds = new Vector2(0.2f, 0.6f);
        [Tooltip("How far past the screen edge it appears.")]
        [SerializeField, Min(0f)] private float spawnMargin = 3f;
        [Range(0f, 1f)] [SerializeField] private float behindChance = 0.7f;
        [Tooltip("+1 when the level progresses to the right. 'Behind' is the opposite side.")]
        [SerializeField] private int forwardDirection = 1;
        [SerializeField] private float minX = 1f;
        [SerializeField] private float maxX = 59f;

        [Header("Chase")]
        [Tooltip("The player walks at 2.5 and runs at 4.5. Above 4.5 the monster outruns the player, so only the flashlight helps.")]
        [SerializeField, Min(0f)] private float startSpeed = 6f;
        [SerializeField, Min(0f)] private float topSpeed = 9f;
        [SerializeField, Min(0.1f)] private float rampSeconds = 1.5f;
        [SerializeField, Min(0.5f)] private float idleFramesPerSecond = 5f;

        [Header("Banishing")]
        [Tooltip("Seconds in the beam before it is banished.")]
        [SerializeField, Min(0.05f)] private float banishSeconds = 0.6f;
        [Tooltip("Exposure lost per second while out of the beam.")]
        [SerializeField, Min(0f)] private float exposureDecayPerSecond = 1.5f;
        [SerializeField, Min(1f)] private float dissolveFramesPerSecond = 10f;
        [SerializeField, Min(0.05f)] private float fadeOutSeconds = 0.5f;
        [Tooltip("Height of the point the beam has to hit, above the monster's feet.")]
        [SerializeField, Min(0f)] private float bodyCenterHeight = 1f;

        [Header("Touch (game over)")]
        [SerializeField] private Vector2 touchHalfSize = new Vector2(0.45f, 1f);

        private static readonly int[] IdleLoop = { 0, 0, 2, 0, 0, 2, 1 };
        private const int FirstDissolveFrame = 3;

        /// <summary>Raised once per session, the first time the monster is on screen (in the camera's view).</summary>
        public event System.Action Sighted;

        /// <summary>Raised each time the monster has been driven back and has faded out completely.</summary>
        public event System.Action Banished;

        private bool sighted;
        private Phase phase = Phase.Waiting;
        private PlayerController player;
        private float timer;
        private float chaseTime;
        private float exposure;
        private float dissolveTime;
        private float fadeTime;
        private float animationTime;
        private int facing = 1;

        private void Start()
        {
            player = FindFirstObjectByType<PlayerController>();
            SetVisible(false);
            timer = Random.Range(firstDelaySeconds.x, firstDelaySeconds.y);
            if (bodyRenderer != null) bodyRenderer.color = bodyTint;
        }

        private bool IsActive()
        {
            GameState state = GameState.Instance;
            if (state == null || player == null) return false;
            if (state.CurrentPerspective != Perspective.SideView || state.CurrentTimePeriod != TimePeriod.Future) return false;
            if (state.HasAnyFlag(disabledFlag)) return false;
            if (!string.IsNullOrEmpty(startFlag) && !state.HasFlag(startFlag)) return false;
            return true;
        }

        private bool IsFrozen()
        {
            if (player.IsMovementLocked) return true;
            if (GameOverController.IsActive) return true;
            SceneTransition transition = SceneTransition.Instance;
            return transition != null && transition.IsTransitioning;
        }

        private void Update()
        {
            if (!IsActive())
            {
                if (phase != Phase.Waiting)
                {
                    phase = Phase.Waiting;
                    SetVisible(false);
                    timer = Random.Range(respawnDelaySeconds.x, respawnDelaySeconds.y);
                }

                return;
            }

            if (IsFrozen()) return;

            switch (phase)
            {
                case Phase.Waiting: UpdateWaiting(); break;
                case Phase.Approaching: UpdateApproaching(); break;
                case Phase.Dissolving: UpdateDissolving(); break;
            }
        }

        private void UpdateWaiting()
        {
            timer -= Time.deltaTime;
            if (timer > 0f) return;

            if (!TrySpawn()) timer = 1f;
        }

        private bool TrySpawn()
        {
            Camera cam = Camera.main;
            if (cam == null) return false;

            float halfWidth = cam.orthographicSize * cam.aspect;
            float cameraX = cam.transform.position.x;
            int side = Random.value < behindChance ? -forwardDirection : forwardDirection;

            // Just past the screen edge on the chosen side. At a corridor end that side may be out of the level,
            // then it comes from the other side instead.
            for (int attempt = 0; attempt < 2; attempt++)
            {
                float x = Mathf.Clamp(cameraX + side * (halfWidth + spawnMargin), minX, maxX);
                bool offScreen = Mathf.Abs(x - cameraX) > halfWidth + 0.5f;
                if (offScreen)
                {
                    Place(x);
                    return true;
                }

                side = -side;
            }

            return false;
        }

        private void Place(float x)
        {
            Vector2 feet = new Vector2(x, player.transform.position.y);
            RaycastHit2D ground = Physics2D.Raycast(feet + Vector2.up * 2f, Vector2.down, 6f);
            if (ground.collider != null && !ground.collider.isTrigger) feet.y = ground.point.y;

            transform.position = new Vector3(feet.x, feet.y, player.transform.position.z);
            chaseTime = 0f;
            exposure = 0f;
            animationTime = 0f;
            phase = Phase.Approaching;
            SetVisible(true);
            SetAlpha(1f);
            SetFrame(0);
            FaceTowardsPlayer();
        }

        private void UpdateApproaching()
        {
            chaseTime += Time.deltaTime;
            float speed = Mathf.Lerp(startSpeed, topSpeed, Mathf.Clamp01(chaseTime / rampSeconds));

            float dx = player.transform.position.x - transform.position.x;
            if (Mathf.Abs(dx) > 0.05f)
            {
                Vector3 position = transform.position;
                position.x += Mathf.Sign(dx) * Mathf.Min(speed * Time.deltaTime, Mathf.Abs(dx));
                transform.position = position;
            }

            FaceTowardsPlayer();
            animationTime += Time.deltaTime * idleFramesPerSecond;
            SetFrame(IdleLoop[Mathf.FloorToInt(animationTime) % IdleLoop.Length]);

            if (!sighted && IsOnScreen())
            {
                sighted = true;
                Sighted?.Invoke();
            }

            if (IsTouchingPlayer())
            {
                GameOverController controller = GameOverController.Instance;
                if (controller != null) controller.Trigger();
                return;
            }

            if (IsInBeam()) exposure += Time.deltaTime;
            else exposure = Mathf.Max(0f, exposure - exposureDecayPerSecond * Time.deltaTime);

            if (exposure >= banishSeconds) StartDissolving();
        }

        private void StartDissolving()
        {
            phase = Phase.Dissolving;
            dissolveTime = 0f;
            fadeTime = 0f;
        }

        private void UpdateDissolving()
        {
            dissolveTime += Time.deltaTime * dissolveFramesPerSecond;
            int last = bodyFrames.Length - 1;
            int frame = Mathf.Min(FirstDissolveFrame + Mathf.FloorToInt(dissolveTime), last);
            SetFrame(frame);

            // Start fading once the last collapse frame is showing.
            if (FirstDissolveFrame + dissolveTime >= last)
            {
                fadeTime += Time.deltaTime;
                SetAlpha(1f - Mathf.Clamp01(fadeTime / fadeOutSeconds));

                if (fadeTime >= fadeOutSeconds)
                {
                    phase = Phase.Waiting;
                    SetVisible(false);
                    timer = Random.Range(respawnDelaySeconds.x, respawnDelaySeconds.y);
                    Banished?.Invoke();
                }
            }
        }

        private bool IsOnScreen()
        {
            Camera cam = Camera.main;
            if (cam == null) return false;

            float halfWidth = cam.orthographicSize * cam.aspect;
            return Mathf.Abs(transform.position.x - cam.transform.position.x) < halfWidth;
        }

        private bool IsTouchingPlayer()
        {
            Vector2 delta = player.transform.position - transform.position;
            return Mathf.Abs(delta.x) < touchHalfSize.x && Mathf.Abs(delta.y) < touchHalfSize.y;
        }

        private bool IsInBeam()
        {
            FlashlightController flashlight = FlashlightController.Active;
            if (flashlight == null) return false;

            Vector2 target = (Vector2)transform.position + Vector2.up * bodyCenterHeight;
            return flashlight.Illuminates(target, transform);
        }

        private void FaceTowardsPlayer()
        {
            float dx = player.transform.position.x - transform.position.x;
            if (Mathf.Abs(dx) > 0.05f) facing = dx > 0f ? 1 : -1;
            ApplyFlip();
        }

        private void ApplyFlip()
        {
            bool flip = facing < 0;
            if (bodyRenderer != null) bodyRenderer.flipX = flip;
            if (eyesRenderer != null) eyesRenderer.flipX = flip;
        }

        private void SetFrame(int index)
        {
            if (bodyRenderer != null && index < bodyFrames.Length) bodyRenderer.sprite = bodyFrames[index];
            if (eyesRenderer != null && index < eyeFrames.Length) eyesRenderer.sprite = eyeFrames[index];
        }

        private void SetVisible(bool visible)
        {
            if (bodyRenderer != null) bodyRenderer.enabled = visible;
            if (eyesRenderer != null) eyesRenderer.enabled = visible;
        }

        private void SetAlpha(float alpha)
        {
            if (bodyRenderer != null)
            {
                Color color = bodyTint;
                color.a = alpha;
                bodyRenderer.color = color;
            }

            if (eyesRenderer != null)
            {
                Color color = Color.white;
                color.a = alpha;
                eyesRenderer.color = color;
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(transform.position + Vector3.up * bodyCenterHeight, touchHalfSize * 2f);
        }
    }
}
