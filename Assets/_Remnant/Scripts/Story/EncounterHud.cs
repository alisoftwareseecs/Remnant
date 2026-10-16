using System.Collections;
using System.Collections.Generic;
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
        [SerializeField] private Color shadowColor = new Color(0.62f, 0.30f, 0.30f);
        [SerializeField, Min(8f)] private float captionSize = 30f;
        [SerializeField, Min(0.1f)] private float captionFadeSeconds = 0.25f;
        [SerializeField, Min(0.2f)] private float captionHoldSeconds = 2.2f;

        [Header("Prompt")]
        [SerializeField, Min(8f)] private float promptSize = 22f;
        [Range(0.05f, 1f)] [SerializeField] private float promptMaxAlpha = 0.45f;
        [SerializeField, Min(0.1f)] private float promptFadeSeconds = 2.5f;

        private readonly Queue<string> queue = new Queue<string>();
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

            caption = CreateText(canvas.transform, "ShadowLine", captionSize, FontStyles.Italic,
                new Vector2(0.5f, 0f), new Vector2(0f, 190f), TextAlignmentOptions.Center);
            caption.color = Transparent(shadowColor);

            prompt = CreateText(canvas.transform, "Prompt", promptSize, FontStyles.Normal,
                new Vector2(0.5f, 0f), new Vector2(0f, 110f), TextAlignmentOptions.Center);
            prompt.color = new Color(1f, 1f, 1f, 0f);
        }

        /// <summary>Shows a Shadow line; lines queue up so none is lost when several come close together.</summary>
        public void ShadowSays(string text)
        {
            queue.Enqueue(text);
            if (captionRoutine == null) captionRoutine = StartCoroutine(RunCaptions());
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

        private IEnumerator RunCaptions()
        {
            while (queue.Count > 0)
            {
                caption.text = queue.Dequeue();
                yield return FadeCaption(0f, 1f, captionFadeSeconds);
                yield return new WaitForSeconds(captionHoldSeconds);
                yield return FadeCaption(1f, 0f, captionFadeSeconds);
            }

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

        private static TextMeshProUGUI CreateText(Transform parent, string objectName, float size, FontStyles style,
            Vector2 anchor, Vector2 anchoredPosition, TextAlignmentOptions alignment)
        {
            GameObject textObject = new GameObject(objectName);
            textObject.transform.SetParent(parent, false);

            TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
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
