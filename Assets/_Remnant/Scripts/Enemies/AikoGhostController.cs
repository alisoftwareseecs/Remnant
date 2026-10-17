using System;
using Remnant.Core;
using Remnant.Flashlight;
using Remnant.Player;
using UnityEngine;

namespace Remnant.Enemies
{
    /// <summary>
    /// Aiko's ghost in the dark classroom (Act 4). It uses the monster's sprites but is slow and hesitant, and it can never
    /// hurt the player. Held in the beam it does not dissolve: it flinches, hides its eyes, shakes, shrinks back and retreats
    /// off-screen crying, then returns a few seconds later. It always comes back. While the light is up it stops a short
    /// way off and waits; once the beam has been lowered it walks all the way to the player, and its touch raises
    /// ReachedPlayer. There is no game over here.
    /// </summary>
    public class AikoGhostController : MonoBehaviour
    {
        private enum Phase { Inactive, Hidden, Approaching, Flinching, Retreating, Reached }

        [Header("Rendering")]
        [SerializeField] private SpriteRenderer bodyRenderer;
        [SerializeField] private SpriteRenderer eyesRenderer;
        [Tooltip("Unlit overlay with the head area only. Tinted a very faint red (Aiko's hair), a little more every return.")]
        [SerializeField] private SpriteRenderer headRenderer;
        [Tooltip("monster_0 to monster_11. 0-2 are the idle frames.")]
        [SerializeField] private Sprite[] bodyFrames = new Sprite[12];
        [SerializeField] private Sprite[] eyeFrames = new Sprite[12];
        [SerializeField] private Sprite[] headFrames = new Sprite[12];
        [SerializeField] private Color bodyTint = new Color(0.62f, 0.74f, 0.95f, 1f);

        [Header("Red tint (head)")]
        [Range(0f, 1f)] [SerializeField] private float headAlphaFirst = 0.12f;
        [Range(0f, 0.5f)] [SerializeField] private float headAlphaGrowth = 0.06f;
        [Range(0f, 1f)] [SerializeField] private float headAlphaMax = 0.5f;
        [SerializeField] private Color headColor = new Color(0.8f, 0.1f, 0.1f, 1f);

        [Header("Walking")]
        [Tooltip("Slow and hesitant. The player walks at 2.5.")]
        [SerializeField, Min(0.1f)] private float walkSpeed = 1.2f;
        [SerializeField] private Vector2 walkSecondsBeforePause = new Vector2(1f, 2.2f);
        [SerializeField] private Vector2 pauseSeconds = new Vector2(0.4f, 1f);
        [Tooltip("While the light is up she stops this far from the player and waits.")]
        [SerializeField, Min(0.3f)] private float waitDistance = 1f;
        [Tooltip("With the light lowered, her touch is this close (horizontally).")]
        [SerializeField, Min(0.1f)] private float touchDistance = 0.45f;
        [SerializeField, Min(0.5f)] private float idleFramesPerSecond = 4f;

        [Header("Spawning")]
        [SerializeField, Min(0f)] private float spawnMargin = 2f;
        [SerializeField] private float minX = 1f;
        [SerializeField] private float maxX = 19f;
        [SerializeField, Min(0f)] private float returnDelaySeconds = 3f;

        [Header("Beam")]
        [Tooltip("Seconds in the beam before she flinches.")]
        [SerializeField, Min(0.05f)] private float flinchAfterSeconds = 0.6f;
        [SerializeField, Min(0f)] private float exposureDecayPerSecond = 1.5f;
        [Tooltip("Height of the point the beam has to hit, above her feet.")]
        [SerializeField, Min(0f)] private float bodyCenterHeight = 1f;

        [Header("Flinch and retreat")]
        [SerializeField, Min(0.05f)] private float flinchSeconds = 0.4f;
        [SerializeField, Min(0f)] private float shakeDistance = 0.09f;
        [Range(0.3f, 1f)] [SerializeField] private float shrunkScale = 0.82f;
        [SerializeField, Min(0.1f)] private float retreatSpeed = 2.4f;

        [Header("Crying")]
        [Tooltip("Optional. Leave empty until the sound exists; nothing plays then.")]
        [SerializeField] private AudioSource cryingSource;
        [SerializeField] private AudioClip cryingClip;

        private static readonly int[] IdleLoop = { 0, 0, 2, 0, 0, 2, 1 };

        private Phase phase = Phase.Inactive;
        private PlayerController player;
        private float timer;
        private float exposure;
        private float animationTime;
        private float phaseTime;
        private float walkClock;
        private float nextPauseAt;
        private float pausedUntil;
        private int facing = 1;
        private int banishCount;
        private int appearCount;
        private Vector3 restPosition;
        private Vector3 baseScale = Vector3.one;

        /// <summary>Raised each time she flinches away from the beam. The argument is how many times so far.</summary>
        public event Action<int> Banished;

        /// <summary>Raised each time she appears on screen. The argument is how many times so far (1 = the first time).</summary>
        public event Action<int> Appeared;

        /// <summary>Raised once, when her touch reaches the player with the light lowered.</summary>
        public event Action ReachedPlayer;

        public int BanishCount => banishCount;

        /// <summary>True while she is on screen (not hidden or retreating out of view).</summary>
        public bool IsVisible => phase == Phase.Approaching || phase == Phase.Flinching || phase == Phase.Retreating;

        private void Awake()
        {
            baseScale = transform.localScale;
            SetVisible(false);
        }

        private void Start()
        {
            player = FindFirstObjectByType<PlayerController>();
        }

        /// <summary>Starts the haunting: she appears after the given delay.</summary>
        public void Begin(float firstDelaySeconds)
        {
            if (phase != Phase.Inactive) return;
            phase = Phase.Hidden;
            timer = firstDelaySeconds;
        }

        private bool LightLowered
        {
            get
            {
                FlashlightController flashlight = FlashlightController.Active;
                return flashlight != null && flashlight.IsLowered;
            }
        }

        private void Update()
        {
            if (phase == Phase.Inactive || phase == Phase.Reached) return;
            if (player == null)
            {
                player = FindFirstObjectByType<PlayerController>();
                if (player == null) return;
            }

            SceneTransition transition = SceneTransition.Instance;
            if (transition != null && transition.IsTransitioning) return;

            switch (phase)
            {
                case Phase.Hidden: UpdateHidden(); break;
                case Phase.Approaching: UpdateApproaching(); break;
                case Phase.Flinching: UpdateFlinching(); break;
                case Phase.Retreating: UpdateRetreating(); break;
            }
        }

        private void UpdateHidden()
        {
            timer -= Time.deltaTime;
            if (timer > 0f) return;

            if (!TrySpawn()) timer = 0.5f;
        }

        private bool TrySpawn()
        {
            Camera cam = Camera.main;
            if (cam == null) return false;

            float halfWidth = cam.orthographicSize * cam.aspect;
            float cameraX = cam.transform.position.x;
            int side = UnityEngine.Random.value < 0.5f ? -1 : 1;

            for (int attempt = 0; attempt < 2; attempt++)
            {
                float x = Mathf.Clamp(cameraX + side * (halfWidth + spawnMargin), minX, maxX);
                if (Mathf.Abs(x - cameraX) > halfWidth + 0.5f)
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
            transform.localScale = baseScale;
            exposure = 0f;
            animationTime = 0f;
            walkClock = 0f;
            nextPauseAt = UnityEngine.Random.Range(walkSecondsBeforePause.x, walkSecondsBeforePause.y);
            pausedUntil = 0f;
            phase = Phase.Approaching;

            if (cryingSource != null) cryingSource.Stop();

            SetVisible(true);
            SetEyesVisible(true);
            ApplyHeadTint();
            SetFrame(0);
            FaceTowardsPlayer();

            appearCount++;
            Appeared?.Invoke(appearCount);
        }

        private void UpdateApproaching()
        {
            bool lowered = LightLowered;
            float dx = player.transform.position.x - transform.position.x;
            float stopAt = lowered ? touchDistance : waitDistance;

            if (Mathf.Abs(dx) > stopAt) Walk(Mathf.Sign(dx));
            else if (lowered) { Reach(); return; }

            FaceTowardsPlayer();
            Animate();

            if (lowered)
            {
                exposure = 0f;
                return;
            }

            if (IsInBeam()) exposure += Time.deltaTime;
            else exposure = Mathf.Max(0f, exposure - exposureDecayPerSecond * Time.deltaTime);

            if (exposure >= flinchAfterSeconds) StartFlinching();
        }

        private void Walk(float direction)
        {
            if (Time.time < pausedUntil) return;

            walkClock += Time.deltaTime;
            if (walkClock >= nextPauseAt)
            {
                // A hesitant stop, as if she is not sure she should come closer.
                walkClock = 0f;
                nextPauseAt = UnityEngine.Random.Range(walkSecondsBeforePause.x, walkSecondsBeforePause.y);
                pausedUntil = Time.time + UnityEngine.Random.Range(pauseSeconds.x, pauseSeconds.y);
                return;
            }

            Vector3 position = transform.position;
            position.x += direction * walkSpeed * Time.deltaTime;
            position.x = Mathf.Clamp(position.x, minX, maxX);
            transform.position = position;
        }

        private void Reach()
        {
            phase = Phase.Reached;
            ReachedPlayer?.Invoke();
        }

        private void StartFlinching()
        {
            phase = Phase.Flinching;
            phaseTime = 0f;
            restPosition = transform.position;
            exposure = 0f;
            SetEyesVisible(false);

            banishCount++;
            Banished?.Invoke(banishCount);
        }

        private void UpdateFlinching()
        {
            phaseTime += Time.deltaTime;
            float t = Mathf.Clamp01(phaseTime / flinchSeconds);

            float shake = (UnityEngine.Random.value * 2f - 1f) * shakeDistance * (1f - t);
            transform.position = restPosition + new Vector3(shake, 0f, 0f);
            transform.localScale = baseScale * Mathf.Lerp(1f, shrunkScale, t);

            if (phaseTime < flinchSeconds) return;

            transform.position = restPosition;
            phase = Phase.Retreating;
            if (cryingSource != null && cryingClip != null)
            {
                cryingSource.clip = cryingClip;
                cryingSource.loop = true;
                cryingSource.Play();
            }
        }

        private void UpdateRetreating()
        {
            // She backs away from the player, still facing them, until she is out of view.
            float away = transform.position.x >= player.transform.position.x ? 1f : -1f;
            Vector3 position = transform.position;
            position.x += away * retreatSpeed * Time.deltaTime;
            transform.position = position;

            FaceTowardsPlayer();
            Animate();

            Camera cam = Camera.main;
            bool offScreen = cam == null ||
                             Mathf.Abs(position.x - cam.transform.position.x) > cam.orthographicSize * cam.aspect + 1f ||
                             position.x <= minX || position.x >= maxX;
            if (!offScreen) return;

            phase = Phase.Hidden;
            timer = returnDelaySeconds;
            SetVisible(false);
            if (cryingSource != null) cryingSource.Stop();
        }

        private bool IsInBeam()
        {
            FlashlightController flashlight = FlashlightController.Active;
            if (flashlight == null) return false;

            return flashlight.Illuminates((Vector2)transform.position + Vector2.up * bodyCenterHeight, transform);
        }

        private void Animate()
        {
            animationTime += Time.deltaTime * idleFramesPerSecond;
            SetFrame(IdleLoop[Mathf.FloorToInt(animationTime) % IdleLoop.Length]);
        }

        private void FaceTowardsPlayer()
        {
            float dx = player.transform.position.x - transform.position.x;
            if (Mathf.Abs(dx) > 0.05f) facing = dx > 0f ? 1 : -1;

            bool flip = facing < 0;
            if (bodyRenderer != null) bodyRenderer.flipX = flip;
            if (eyesRenderer != null) eyesRenderer.flipX = flip;
            if (headRenderer != null) headRenderer.flipX = flip;
        }

        private void ApplyHeadTint()
        {
            if (headRenderer == null) return;

            Color color = headColor;
            color.a = Mathf.Min(headAlphaMax, headAlphaFirst + headAlphaGrowth * banishCount);
            headRenderer.color = color;
        }

        private void SetFrame(int index)
        {
            if (bodyRenderer != null && index < bodyFrames.Length) bodyRenderer.sprite = bodyFrames[index];
            if (eyesRenderer != null && index < eyeFrames.Length) eyesRenderer.sprite = eyeFrames[index];
            if (headRenderer != null && index < headFrames.Length) headRenderer.sprite = headFrames[index];
        }

        private void SetVisible(bool visible)
        {
            if (bodyRenderer != null)
            {
                bodyRenderer.enabled = visible;
                bodyRenderer.color = bodyTint;
            }

            if (eyesRenderer != null) eyesRenderer.enabled = visible;
            if (headRenderer != null) headRenderer.enabled = visible;
        }

        private void SetEyesVisible(bool visible)
        {
            if (eyesRenderer != null) eyesRenderer.enabled = visible;
        }
    }
}
