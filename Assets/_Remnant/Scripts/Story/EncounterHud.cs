using System.Collections;
using Remnant.Dialogue;
using TMPro;
using UnityEngine;

namespace Remnant.Story
{
    /// <summary>
    /// Small on-screen text for moments where the game must keep running: the Shadow's shouted lines (shown one after
    /// another, without stopping the player) and a faint prompt that fades in. Builds its own overlay canvas at runtime,
    /// so the scene needs only this component.
    /// </summary>
    public class EncounterHud : MonoBehaviour
    {
        [Header("Shadow lines")]
        [Tooltip("The pixel font of the dialogue box.")]
        [SerializeField] private TMP_FontAsset font;
        [Tooltip("Same crimson as the Shadow in the dialogue box.")]
        [SerializeField] private Color shadowColor = new Color(0.690f, 0.063f, 0.165f);
        [SerializeField, Min(8f)] private float captionSize = 48f;
        [Tooltip("Letter jitter of the Shadow's shouted lines (tense delivery).")]
        [SerializeField, Min(0f)] private float captionJitter = 1.5f;
        [SerializeField, Min(0.1f)] private float captionFadeSeconds = 0.25f;
        [Tooltip("The caption types out; a line that is cut off shows only what was typed so far.")]
        [SerializeField, Min(1f)] private float captionCharsPerSecond = 34f;
        [SerializeField, Min(0.2f)] private float captionHoldSeconds = 2.2f;

        [Header("Prompt")]
        [SerializeField, Min(8f)] private float promptSize = 32f;
        [Range(0.05f, 1f)] [SerializeField] private float promptMaxAlpha = 0.45f;
        [SerializeField, Min(0.1f)] private float promptFadeSeconds = 2.5f;

        private TextMeshProUGUI caption;
        private TextMeshProUGUI prompt;
        private Coroutine captionRoutine;
        private Coroutine promptRoutine;

        private void Awake()
        {
            GameObject canvasObject = new GameObject("EncounterHudCanvas");
            canvasObject.transform.SetParent(transform, false);
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            // Below the dialogue panel (50) and the scene transition overlay.
            canvas.sortingOrder = 40;

            caption = CreateText(canvas.transform, "ShadowLine", captionSize, FontStyles.Normal,
                new Vector2(0.5f, 0f), new Vector2(0f, 190f), TextAlignmentOptions.Center);
            caption.color = Transparent(shadowColor);
            caption.gameObject.AddComponent<JitterText>().BaseAmplitude = captionJitter;

            prompt = CreateText(canvas.transform, "Prompt", promptSize, FontStyles.Normal,
                new Vector2(0.5f, 0f), new Vector2(0f, 110f), TextAlignmentOptions.Center);
            prompt.color = new Color(1f, 1f, 1f, 0f);
        }

        /// <summary>
        /// Stops the Shadow's caption at once, mid-word if it is still typing, and drops any queued lines.
        /// </summary>
        public void CutOff()
        {
            if (captionRoutine != null)
            {
                StopCoroutine(captionRoutine);
                captionRoutine = null;
            }

            caption.text = string.Empty;
            caption.color = Transparent(shadowColor);
        }

        /// <summary>
        /// Shows a Shadow line. Captions do not queue: a new one replaces the one on screen at once, so every line
        /// appears at the moment it is meant to.
        /// </summary>
        public void ShadowSays(string text)
        {
            if (captionRoutine != null) StopCoroutine(captionRoutine);
            captionRoutine = StartCoroutine(RunCaption(text));
        }

        public void ShowPrompt(string text)
        {
            prompt.text = text;
            if (promptRoutine != null) StopCoroutine(promptRoutine);
            promptRoutine = StartCoroutine(FadePrompt(promptMaxAlpha, promptFadeSeconds));
        }

        public void HidePrompt()
        {
            if (promptRoutine != null) StopCoroutine(promptRoutine);
            promptRoutine = StartCoroutine(FadePrompt(0f, 0.4f));
        }

        private IEnumerator RunCaption(string text)
        {
            caption.text = text;
            caption.maxVisibleCharacters = 0;
            caption.color = new Color(shadowColor.r, shadowColor.g, shadowColor.b, 1f);
            caption.ForceMeshUpdate();

            int total = caption.textInfo.characterCount;
            float typed = 0f;
            while (typed < total)
            {
                typed += captionCharsPerSecond * Time.deltaTime;
                caption.maxVisibleCharacters = Mathf.Min(total, Mathf.FloorToInt(typed));
                yield return null;
            }

            caption.maxVisibleCharacters = int.MaxValue;
            yield return new WaitForSeconds(captionHoldSeconds);
            yield return FadeCaption(1f, 0f, captionFadeSeconds);

            captionRoutine = null;
        }

        private IEnumerator FadeCaption(float from, float to, float seconds)
        {
            float elapsed = 0f;
            while (elapsed < seconds)
            {
                caption.color = new Color(shadowColor.r, shadowColor.g, shadowColor.b, Mathf.Lerp(from, to, elapsed / seconds));
                elapsed += Time.deltaTime;
                yield return null;
            }

            caption.color = new Color(shadowColor.r, shadowColor.g, shadowColor.b, to);
        }

        private IEnumerator FadePrompt(float target, float seconds)
        {
            float start = prompt.color.a;
            float elapsed = 0f;
            while (elapsed < seconds)
            {
                prompt.color = new Color(1f, 1f, 1f, Mathf.Lerp(start, target, elapsed / seconds));
                elapsed += Time.deltaTime;
                yield return null;
            }

            prompt.color = new Color(1f, 1f, 1f, target);
            promptRoutine = null;
        }

        private static Color Transparent(Color color)
        {
            color.a = 0f;
            return color;
        }

        private TextMeshProUGUI CreateText(Transform parent, string objectName, float size, FontStyles style,
            Vector2 anchor, Vector2 anchoredPosition, TextAlignmentOptions alignment)
        {
            GameObject textObject = new GameObject(objectName);
            textObject.transform.SetParent(parent, false);

            TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
            if (font != null) text.font = font;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = alignment;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;

            RectTransform rect = text.rectTransform;
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(1200f, size * 1.6f);
            return text;
        }
    }
}
