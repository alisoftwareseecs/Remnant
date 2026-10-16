using System;
using System.Collections;
using Remnant.Core;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Remnant.Story
{
    /// <summary>
    /// The credits (a slow scroll on black) followed by the closing screen: a few white lines, one at a time, with fades
    /// and pauses. At the end it records that the game was completed (PlayerPrefs "remnant_completed" = 1) and goes to
    /// the next scene (the future title screen).
    /// </summary>
    public class CreditsSequence : MonoBehaviour
    {
        [Serializable]
        private struct ClosingLine
        {
            [TextArea(1, 3)] public string text;
            [Min(0f)] public float holdSeconds;
            [Tooltip("Silence after the line has faded out.")]
            [Min(0f)] public float pauseSeconds;
        }

        [Header("Credits")]
        [Tooltip("The whole credits text. Its top edge scrolls up from below the screen.")]
        [SerializeField] private RectTransform creditsContent;
        [SerializeField] private TMP_Text creditsText;
        [SerializeField, Min(5f)] private float scrollSpeed = 55f;
        [SerializeField, Min(0f)] private float startDelaySeconds = 1.5f;
        [Tooltip("Reference screen height of the canvas (the scaler's reference resolution).")]
        [SerializeField, Min(100f)] private float screenHeight = 1080f;

        [Header("Closing screen")]
        [SerializeField] private CanvasGroup closingGroup;
        [SerializeField] private TMP_Text closingText;
        [SerializeField, Min(0.1f)] private float lineFadeSeconds = 1.5f;
        [SerializeField, Min(0f)] private float pauseBeforeClosing = 2f;
        [SerializeField] private ClosingLine[] lines =
        {
            new ClosingLine { text = "If you're struggling, you're not alone.", holdSeconds = 4f, pauseSeconds = 1.5f },
            new ClosingLine { text = "Please reach out to someone you trust, or find a helpline near you at findahelpline.com", holdSeconds = 6f, pauseSeconds = 2.5f },
            new ClosingLine { text = "And if someone around you is struggling...", holdSeconds = 4f, pauseSeconds = 4.5f },
            new ClosingLine { text = "look.", holdSeconds = 3f, pauseSeconds = 2.5f },
            new ClosingLine { text = "Don't look away.", holdSeconds = 5f, pauseSeconds = 1.5f },
        };

        [Header("After")]
        [SerializeField] private string completedKey = "remnant_completed";
        [Tooltip("Becomes the title screen later.")]
        [SerializeField] private string nextScene = "Present_Classroom";

        private IEnumerator Start()
        {
            if (closingGroup != null) closingGroup.alpha = 0f;

            // Wait for the fade in from white.
            while (SceneTransition.Instance != null && SceneTransition.Instance.IsTransitioning) yield return null;
            yield return new WaitForSeconds(startDelaySeconds);

            yield return Scroll();
            yield return new WaitForSeconds(pauseBeforeClosing);
            yield return Closing();

            PlayerPrefs.SetInt(completedKey, 1);
            PlayerPrefs.Save();

            if (SceneTransition.Instance != null) SceneTransition.Instance.GoTo(nextScene);
            else SceneManager.LoadScene(nextScene);
        }

        private IEnumerator Scroll()
        {
            if (creditsContent == null) yield break;

            if (creditsText != null) creditsText.ForceMeshUpdate();
            float height = creditsText != null ? creditsText.preferredHeight : creditsContent.rect.height;

            // The content's top edge starts at the bottom of the screen and ends above the top.
            float start = -screenHeight * 0.5f;
            float end = screenHeight * 0.5f + height;
            float y = start;
            while (y < end)
            {
                y += scrollSpeed * Time.deltaTime;
                creditsContent.anchoredPosition = new Vector2(0f, y);
                yield return null;
            }
        }

        private IEnumerator Closing()
        {
            if (closingGroup == null || closingText == null) yield break;

            for (int i = 0; i < lines.Length; i++)
            {
                closingText.text = lines[i].text;
                yield return Fade(0f, 1f);
                yield return new WaitForSeconds(lines[i].holdSeconds);
                yield return Fade(1f, 0f);
                yield return new WaitForSeconds(lines[i].pauseSeconds);
            }
        }

        private IEnumerator Fade(float from, float to)
        {
            float elapsed = 0f;
            while (elapsed < lineFadeSeconds)
            {
                closingGroup.alpha = Mathf.Lerp(from, to, elapsed / lineFadeSeconds);
                elapsed += Time.deltaTime;
                yield return null;
            }

            closingGroup.alpha = to;
        }
    }
}
