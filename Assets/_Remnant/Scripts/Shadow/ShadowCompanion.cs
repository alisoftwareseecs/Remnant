using System.Collections.Generic;
using Remnant.Core;
using Remnant.Player;
using UnityEngine;

namespace Remnant.Shadow
{
    /// <summary>
    /// The protagonist's own shadow. Copies the player's sprite (whatever sprite set is current), draws it as a
    /// flat translucent black silhouette just behind the player and replays the player's recorded movement after
    /// a short delay. Now and then it falls further behind or faces the wrong way. Hidden until the awake flag is set.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class ShadowCompanion : MonoBehaviour
    {
        private struct Sample
        {
            public float time;
            public Vector3 position;
            public Vector3 scale;
            public Sprite sprite;
            public bool flipX;
        }

        [Header("Story")]
        [SerializeField] private string awakeFlag = "shadow_awake";
        [Tooltip("Set when the Shadow first appears, so it only fades in slowly once.")]
        [SerializeField] private string seenFlag = "shadow_seen";

        [Tooltip("Once set, the Shadow is an ordinary shadow for the rest of the game: in sync, no delay, no wobble, softer grey.")]
        [SerializeField] private string normalFlag = "shadow_normal";

        [Header("Look")]
        [Range(0f, 1f)] [SerializeField] private float maxAlpha = 0.65f;
        [SerializeField, Min(0.1f)] private float fadeInSeconds = 3f;
        [Tooltip("Where the Shadow rests relative to the player once separated from the feet.")]
        [SerializeField] private Vector2 restOffset = new Vector2(0.45f, -0.05f);

        [Header("Normal mode (after the Shadow has said who it is)")]
        [SerializeField] private Color normalColor = new Color(0.22f, 0.22f, 0.24f, 0.4f);
        [Tooltip("Where an ordinary shadow lies relative to the player's feet.")]
        [SerializeField] private Vector2 normalOffset = new Vector2(0.12f, -0.06f);

        [Header("Following")]
        [SerializeField, Min(0f)] private float followDelay = 0.4f;

        [Header("Out of sync")]
        [SerializeField] private Vector2 desyncIntervalSeconds = new Vector2(4f, 9f);
        [SerializeField] private Vector2 extraLagRange = new Vector2(0.5f, 0.9f);
        [SerializeField] private Vector2 desyncDurationSeconds = new Vector2(1.2f, 2.4f);
        [SerializeField, Min(0.1f)] private float lagChangeSpeed = 1.2f;

        [Header("Wobble")]
        [SerializeField, Min(0f)] private float wobbleScale = 0.04f;
        [SerializeField, Min(0f)] private float wobblePosition = 0.02f;
        [SerializeField, Min(0f)] private float wobbleRotationDegrees = 3f;
        [SerializeField, Min(0.1f)] private float wobbleSpeed = 2.3f;

        private readonly List<Sample> samples = new List<Sample>();
        private SpriteRenderer spriteRenderer;
        private SpriteRenderer playerRenderer;
        private PlayerController player;
        private GameState state;

        private bool awake;
        private bool normal;
        private float vanish;
        private float appearProgress;
        private float extraLag;
        private float extraLagTarget;
        private bool wrongFacing;
        private float desyncEndsAt;
        private float nextDesyncAt;
        private float noiseSeed;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            spriteRenderer.enabled = false;
            noiseSeed = Random.value * 100f;
        }

        private void Start()
        {
            state = GameState.Instance;
            if (state == null)
            {
                Debug.LogWarning("ShadowCompanion: no GameState in the scene.", this);
                return;
            }

            state.FlagChanged += OnFlagChanged;
            normal = state.HasFlag(normalFlag);
            if (state.HasFlag(awakeFlag)) Wake(!state.HasFlag(seenFlag));
        }

        private void OnDestroy()
        {
            if (state != null) state.FlagChanged -= OnFlagChanged;
        }

        private void OnFlagChanged(string flag, bool value)
        {
            if (flag == awakeFlag && value && !awake) Wake(!state.HasFlag(seenFlag));
            if (flag == normalFlag) normal = value;
        }

        /// <summary>
        /// The Shadow shrinks towards the player's feet while fading, then settles as an ordinary shadow
        /// (sets the normal flag, so it stays that way). The returned coroutine ends when it has settled.
        /// </summary>
        public System.Collections.IEnumerator ShrinkToFeet(float seconds)
        {
            float elapsed = 0f;
            while (elapsed < seconds)
            {
                vanish = Mathf.SmoothStep(0f, 1f, elapsed / seconds);
                elapsed += Time.deltaTime;
                yield return null;
            }

            vanish = 1f;
            if (state != null) state.SetFlag(normalFlag);
            normal = true;

            // It comes back as a plain shadow.
            elapsed = 0f;
            while (elapsed < 1f)
            {
                vanish = 1f - Mathf.SmoothStep(0f, 1f, elapsed);
                elapsed += Time.deltaTime;
                yield return null;
            }

            vanish = 0f;
        }

        private void Wake(bool slowAppearance)
        {
            awake = true;
            appearProgress = slowAppearance ? 0f : 1f;
            samples.Clear();
            nextDesyncAt = Time.time + Random.Range(desyncIntervalSeconds.x, desyncIntervalSeconds.y);
            if (slowAppearance && state != null) state.SetFlag(seenFlag);
        }

        private void LateUpdate()
        {
            if (!awake) return;

            if (player == null)
            {
                player = FindFirstObjectByType<PlayerController>();
                if (player == null) return;
                playerRenderer = player.GetComponent<SpriteRenderer>();
            }

            if (playerRenderer == null || playerRenderer.sprite == null) return;

            appearProgress = Mathf.MoveTowards(appearProgress, 1f, Time.deltaTime / fadeInSeconds);
            if (!normal) UpdateDesync();
            Record();
            Render();
        }

        private void UpdateDesync()
        {
            if (extraLagTarget > 0f || wrongFacing)
            {
                if (Time.time >= desyncEndsAt)
                {
                    extraLagTarget = 0f;
                    wrongFacing = false;
                    nextDesyncAt = Time.time + Random.Range(desyncIntervalSeconds.x, desyncIntervalSeconds.y);
                }
            }
            else if (Time.time >= nextDesyncAt && appearProgress >= 1f)
            {
                desyncEndsAt = Time.time + Random.Range(desyncDurationSeconds.x, desyncDurationSeconds.y);
                if (Random.value < 0.5f) extraLagTarget = Random.Range(extraLagRange.x, extraLagRange.y);
                else wrongFacing = true;
            }

            extraLag = Mathf.MoveTowards(extraLag, extraLagTarget, lagChangeSpeed * Time.deltaTime);
        }

        private void Record()
        {
            samples.Add(new Sample
            {
                time = Time.time,
                position = player.transform.position,
                scale = player.transform.lossyScale,
                sprite = playerRenderer.sprite,
                flipX = playerRenderer.flipX
            });
        }

        private void Render()
        {
            // At the start the Shadow sits exactly on the player's feet; separation widens the delay and the offset.
            float separation = Mathf.SmoothStep(0f, 1f, appearProgress);
            float delay = normal ? 0f : (followDelay + extraLag) * separation;
            float renderTime = Time.time - delay;

            int index = 0;
            while (index + 1 < samples.Count && samples[index + 1].time <= renderTime) index++;
            if (index > 0) samples.RemoveRange(0, index);

            Sample from = samples[0];
            Vector3 position = from.position;
            if (samples.Count > 1)
            {
                Sample to = samples[1];
                float span = to.time - from.time;
                float t = span > 0.0001f ? Mathf.Clamp01((renderTime - from.time) / span) : 1f;
                position = Vector3.Lerp(from.position, to.position, t);
            }

            float time = Time.time * wobbleSpeed + noiseSeed;
            float nx = Mathf.PerlinNoise(time, 0.3f) - 0.5f;
            float ny = Mathf.PerlinNoise(0.7f, time) - 0.5f;
            float nr = Mathf.PerlinNoise(time * 0.8f, 5.1f) - 0.5f;

            // While it shrinks, the Shadow slides in to the player's feet.
            Vector2 offset = normal ? normalOffset : restOffset;
            position += (Vector3)(offset * separation * (1f - vanish));
            if (!normal) position += new Vector3(nx, ny, 0f) * (2f * wobblePosition);
            position.z = player.transform.position.z;

            transform.position = position;
            transform.rotation = normal ? Quaternion.identity : Quaternion.Euler(0f, 0f, nr * 2f * wobbleRotationDegrees);
            float wobbleX = normal ? 0f : nx * 2f * wobbleScale;
            float wobbleY = normal ? 0f : ny * 2f * wobbleScale;
            float shrink = Mathf.Lerp(1f, 0.25f, vanish);
            transform.localScale = new Vector3(
                from.scale.x * (1f + wobbleX) * shrink,
                from.scale.y * (1f + wobbleY) * shrink,
                from.scale.z);

            spriteRenderer.sprite = from.sprite;
            spriteRenderer.flipX = from.flipX != (wrongFacing && !normal);
            spriteRenderer.sortingLayerID = playerRenderer.sortingLayerID;
            spriteRenderer.sortingOrder = playerRenderer.sortingOrder - 1;
            Color color = normal ? normalColor : new Color(0f, 0f, 0f, maxAlpha);
            color.a *= appearProgress * (1f - vanish);
            spriteRenderer.color = color;
            spriteRenderer.enabled = true;
        }
    }
}
