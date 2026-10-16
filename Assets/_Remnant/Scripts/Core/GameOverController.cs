using System.Collections;
using Remnant.Player;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Remnant.Core
{
    /// <summary>
    /// One per scene that can end in a game over. Trigger() plays a sharp red/black flash, goes to black with
    /// "You looked away." and "Press E to try again", then reloads at the last checkpoint with its saved flags.
    /// </summary>
    public class GameOverController : MonoBehaviour
    {
        public static GameOverController Instance { get; private set; }
        public static bool IsActive { get; private set; }

        [Header("Input (Player/Interact from InputSystem_Actions)")]
        [SerializeField] private InputActionReference interactAction;

        [Header("UI")]
        [SerializeField] private GameObject root;
        [SerializeField] private Image flashImage;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text promptText;

        [Header("Text")]
        [SerializeField] private string title = "You looked away.";
        [SerializeField] private string prompt = "Press E to try again";

        [Header("Timing (seconds)")]
        [SerializeField, Min(0.05f)] private float flashSeconds = 0.3f;
        [SerializeField, Min(0f)] private float blackSeconds = 0.7f;
        [SerializeField, Min(0.05f)] private float titleFadeSeconds = 1.2f;
        [SerializeField, Min(0f)] private float promptDelaySeconds = 1.2f;
        [SerializeField, Min(0.05f)] private float promptFadeSeconds = 0.6f;
        [SerializeField] private Color flashColor = new Color(0.75f, 0f, 0f, 1f);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
            IsActive = false;
        }

        private void Awake()
        {
            Instance = this;
            IsActive = false;
            if (root != null) root.SetActive(false);
        }

        private void OnEnable()
        {
            if (interactAction != null) interactAction.action.Enable();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
                IsActive = false;
            }
        }

        public void Trigger()
        {
            if (IsActive) return;
            IsActive = true;
            StartCoroutine(Sequence());
        }

        private IEnumerator Sequence()
        {
            PlayerController player = FindFirstObjectByType<PlayerController>();
            if (player != null) player.SetMovementLocked(true);

            root.SetActive(true);
            titleText.text = title;
            promptText.text = prompt;
            SetTextAlpha(titleText, 0f);
            SetTextAlpha(promptText, 0f);

            // Sharp red/black flicker, then solid black.
            float elapsed = 0f;
            bool red = true;
            float next = 0f;
            while (elapsed < flashSeconds)
            {
                if (elapsed >= next)
                {
                    flashImage.color = red ? flashColor : Color.black;
                    red = !red;
                    next = elapsed + 0.05f;
                }

                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            flashImage.color = Color.black;
            yield return new WaitForSecondsRealtime(blackSeconds);
            yield return FadeText(titleText, titleFadeSeconds);
            yield return new WaitForSecondsRealtime(promptDelaySeconds);
            yield return FadeText(promptText, promptFadeSeconds);

            // Wait for a fresh press so a button held during the death does not skip the screen.
            while (interactAction == null || !interactAction.action.WasPressedThisFrame()) yield return null;

            Retry();
        }

        private void Retry()
        {
            GameState state = GameState.Instance;
            string sceneName = SceneManager.GetActiveScene().name;
            Vector2? position = null;

            if (state != null && state.HasCheckpoint)
            {
                state.RestoreCheckpointFlags();
                sceneName = state.CheckpointScene;
                position = state.CheckpointPosition;
            }

            IsActive = false;

            if (SceneTransition.Instance != null)
            {
                SceneTransition.Instance.GoTo(sceneName, SpawnPoint.DefaultId, null, TransitionStyle.Fade, null, position);
            }
            else
            {
                SceneManager.LoadScene(sceneName);
            }
        }

        private static IEnumerator FadeText(TMP_Text text, float seconds)
        {
            float elapsed = 0f;
            while (elapsed < seconds)
            {
                SetTextAlpha(text, elapsed / seconds);
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            SetTextAlpha(text, 1f);
        }

        private static void SetTextAlpha(TMP_Text text, float alpha)
        {
            Color color = text.color;
            color.a = alpha;
            text.color = color;
        }
    }
}
