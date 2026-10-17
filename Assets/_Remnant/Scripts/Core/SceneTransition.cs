using System.Collections;
using System.Collections.Generic;
using Remnant.Audio;
using Remnant.Player;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Remnant.Core
{
    public enum TransitionStyle { Fade, Distort, Flash }

    /// <summary>
    /// Persistent like GameState. Fades to black (or flickers red/black), optionally shows a title card, loads a
    /// scene, puts the player on a named SpawnPoint and fades back in. The loaded scene's SceneSettings applies
    /// itself in Awake. Player movement is locked for the whole transition.
    /// </summary>
    [DefaultExecutionOrder(-950)]
    public class SceneTransition : MonoBehaviour
    {
        public static SceneTransition Instance { get; private set; }

        /// <summary>True once any transition ran in this play session. Used to tell "Play pressed in this scene" apart.</summary>
        public static bool HasTransitioned { get; private set; }

        [Header("UI")]
        [SerializeField] private CanvasGroup overlayGroup;
        [SerializeField] private Image overlayImage;
        [SerializeField] private CanvasGroup titleGroup;
        [SerializeField] private TMP_Text titleText;

        [Header("Timing (seconds)")]
        [SerializeField, Min(0f)] private float fadeOutSeconds = 0.8f;
        [SerializeField, Min(0f)] private float titleFadeSeconds = 0.6f;
        [SerializeField, Min(0f)] private float titleHoldSeconds = 2f;
        [SerializeField, Min(0f)] private float fadeInSeconds = 0.9f;

        [Header("Distort style")]
        [SerializeField, Min(0f)] private float distortSeconds = 0.9f;
        [Tooltip("Length of one red flash (random between the two).")]
        [SerializeField] private Vector2 redFlashSeconds = new Vector2(0.04f, 0.1f);
        [Tooltip("Length of one black frame between two red flashes (random between the two).")]
        [SerializeField] private Vector2 blackFrameSeconds = new Vector2(0.03f, 0.07f);
        [Tooltip("How long the last, solid red lasts before the screen goes black.")]
        [SerializeField, Min(0f)] private float finalRedSeconds = 0.08f;
        [SerializeField] private Color distortColor = new Color(0.45f, 0f, 0f, 1f);

        [Header("Flash style")]
        [Tooltip("The white flash that opens the transition.")]
        [SerializeField, Min(0.01f)] private float flashSeconds = 0.25f;

        public bool IsTransitioning { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
            HasTransitioned = false;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);

            SetOverlay(0f, Color.black);
            SetTitle(0f);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <param name="sceneName">Scene to load; it must be in Build Settings. May be the current scene.</param>
        /// <param name="spawnId">Id of the SpawnPoint to place the player on. Empty uses "start".</param>
        /// <param name="title">Optional centered title card shown on black.</param>
        /// <param name="flagToSet">Optional story flag set while the screen is black, before the load.</param>
        /// <param name="worldPosition">Optional exact position (e.g. a checkpoint). Used instead of the spawn point.</param>
        /// <param name="fadeOutOverride">Optional length of the fade to black, for a slower fade. Negative uses the default.</param>
        public void GoTo(string sceneName, string spawnId = null, string title = null,
            TransitionStyle style = TransitionStyle.Fade, string flagToSet = null, Vector2? worldPosition = null,
            float fadeOutOverride = -1f, bool flashMusic = false)
        {
            if (IsTransitioning) return;

            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError("SceneTransition: scene '" + sceneName + "' is not in Build Settings.", this);
                return;
            }

            StartCoroutine(Run(sceneName, spawnId, title, style, flagToSet, worldPosition, fadeOutOverride, flashMusic));
        }

        private IEnumerator Run(string sceneName, string spawnId, string title, TransitionStyle style,
            string flagToSet, Vector2? worldPosition, float fadeOutOverride, bool flashMusic)
        {
            IsTransitioning = true;
            HasTransitioned = true;
            SetPlayerLocked(true);

            // A flash goes to white and comes back from white; everything else uses black.
            Color fadeColor = style == TransitionStyle.Flash ? Color.white : Color.black;

            if (style == TransitionStyle.Distort) yield return Distort(flashMusic);
            else if (style == TransitionStyle.Flash) yield return FadeOverlay(0f, 1f, fadeOutOverride >= 0f ? fadeOutOverride : flashSeconds, fadeColor);
            else yield return FadeOverlay(0f, 1f, fadeOutOverride >= 0f ? fadeOutOverride : fadeOutSeconds, fadeColor);

            SetOverlay(1f, fadeColor);

            bool hasTitle = !string.IsNullOrEmpty(title);
            if (hasTitle)
            {
                titleText.text = title;
                yield return FadeTitle(0f, 1f, titleFadeSeconds);
                yield return new WaitForSecondsRealtime(titleHoldSeconds);
            }

            if (!string.IsNullOrEmpty(flagToSet) && GameState.Instance != null) GameState.Instance.SetFlag(flagToSet);

            AsyncOperation load = SceneManager.LoadSceneAsync(sceneName);
            while (!load.isDone) yield return null;

            // One extra frame so every Start() in the new scene has run (camera targets, perspective, flags).
            yield return null;
            PlacePlayer(spawnId, worldPosition);
            SetPlayerLocked(true);
            SaveProgress(sceneName);
            yield return null;

            if (hasTitle) StartCoroutine(FadeTitle(1f, 0f, titleFadeSeconds));
            yield return FadeOverlay(1f, 0f, fadeInSeconds, fadeColor);

            SetPlayerLocked(false);
            IsTransitioning = false;
        }

        // The flashing is planned first (alternating red flashes and black frames), so the music can be sliced to the
        // very same flashes: each red flash plays the next slice of the clip, each black frame is silence.
        private IEnumerator Distort(bool flashMusic)
        {
            List<float> reds = new List<float>();
            List<float> blacks = new List<float>();
            float planned = 0f;
            while (planned < distortSeconds)
            {
                float red = Random.Range(redFlashSeconds.x, redFlashSeconds.y);
                float black = Random.Range(blackFrameSeconds.x, blackFrameSeconds.y);
                reds.Add(red);
                blacks.Add(black);
                planned += red + black;
            }

            FlashSliceMusic slicer = flashMusic ? FlashSliceMusic.Instance : null;
            if (slicer != null) slicer.Begin();

            float elapsed = 0f;
            for (int i = 0; i < reds.Count; i++)
            {
                float t = Mathf.Clamp01(elapsed / distortSeconds);
                SetOverlay(Random.Range(0.35f, 0.6f + 0.4f * t), distortColor);
                if (slicer != null) slicer.Red(reds[i]);
                yield return WaitUnscaled(reds[i]);
                elapsed += reds[i];

                SetOverlay(Random.Range(0.15f, 0.35f + 0.65f * t), Color.black);
                if (slicer != null) slicer.Black();
                yield return WaitUnscaled(blacks[i]);
                elapsed += blacks[i];
            }

            SetOverlay(1f, distortColor);
            if (slicer != null) slicer.Finish();
            yield return WaitUnscaled(finalRedSeconds);
            SetOverlay(1f, Color.black);
        }

        private static IEnumerator WaitUnscaled(float seconds)
        {
            float elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        private IEnumerator FadeOverlay(float from, float to, float seconds, Color color)
        {
            float elapsed = 0f;
            while (elapsed < seconds)
            {
                SetOverlay(Mathf.Lerp(from, to, elapsed / seconds), color);
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            SetOverlay(to, color);
        }

        private IEnumerator FadeTitle(float from, float to, float seconds)
        {
            float elapsed = 0f;
            while (elapsed < seconds)
            {
                SetTitle(Mathf.Lerp(from, to, elapsed / seconds));
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            SetTitle(to);
        }

        private void SetOverlay(float alpha, Color color)
        {
            if (overlayImage != null) overlayImage.color = color;
            if (overlayGroup != null)
            {
                overlayGroup.alpha = alpha;
                overlayGroup.blocksRaycasts = alpha > 0.001f;
            }
        }

        private void SetTitle(float alpha)
        {
            if (titleGroup != null) titleGroup.alpha = alpha;
        }

        /// <summary>Every scene with a player is a save point: scene, position and flags go to the save slot.</summary>
        private static void SaveProgress(string sceneName)
        {
            PlayerController player = FindFirstObjectByType<PlayerController>();
            GameState state = GameState.Instance;
            if (player == null || state == null) return;

            SaveSystem.Save(sceneName, player.transform.position, state.Flags);
        }

        private static void SetPlayerLocked(bool locked)
        {
            PlayerController player = FindFirstObjectByType<PlayerController>();
            if (player != null) player.SetMovementLocked(locked);
        }

        private static void PlacePlayer(string spawnId, Vector2? worldPosition)
        {
            PlayerController player = FindFirstObjectByType<PlayerController>();
            // Scenes without a player (the credits) are fine.
            if (player == null) return;

            Vector3 target;
            if (worldPosition.HasValue)
            {
                target = worldPosition.Value;
            }
            else
            {
                SpawnPoint spawn = SpawnPoint.Find(spawnId);
                if (spawn == null)
                {
                    Debug.LogWarning("SceneTransition: no SpawnPoint '" + spawnId + "'. The player stays where the scene put them.");
                    return;
                }

                target = spawn.transform.position;
            }

            Vector3 delta = target - player.transform.position;
            delta.z = 0f;

            player.transform.position += delta;
            Rigidbody2D body = player.GetComponent<Rigidbody2D>();
            if (body != null)
            {
                body.position = player.transform.position;
                body.linearVelocity = Vector2.zero;
            }

            Physics2D.SyncTransforms();

            // Tell Cinemachine the target jumped so the camera snaps instead of panning across the room.
            foreach (CinemachineCamera cam in FindObjectsByType<CinemachineCamera>(FindObjectsSortMode.None))
            {
                cam.OnTargetObjectWarped(player.transform, delta);
            }
        }
    }
}
