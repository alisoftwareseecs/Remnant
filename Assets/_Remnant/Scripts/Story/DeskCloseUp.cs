using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Remnant.Story
{
    /// <summary>
    /// A full-screen close-up of a surface that is too small to read at the game's scale (Aiko's desk). It fades in behind
    /// the dialogue box while a dialogue about it plays and fades out when it ends. The scene needs only this component;
    /// it builds its own overlay canvas (sort order below the dialogue box).
    /// </summary>
    public class DeskCloseUp : MonoBehaviour
    {
        [Tooltip("Below the dialogue box (100) and above the game.")]
        [SerializeField] private int sortingOrder = 60;
        [SerializeField, Min(0.05f)] private float fadeSeconds = 0.5f;

        private CanvasGroup group;
        private Image picture;
        private Coroutine fade;

        /// <summary>True while a close-up is showing or fading in.</summary>
        public bool IsShown { get; private set; }

        private void Awake()
        {
            GameObject canvasObject = new GameObject("DeskCloseUpCanvas");
            canvasObject.transform.SetParent(transform, false);
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            group = canvasObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;

            // a dark backdrop, then the picture on top of it (the picture is the whole screen, 16:9)
            Image backdrop = CreateImage(canvasObject.transform, "Backdrop");
            backdrop.color = new Color(0f, 0f, 0f, 1f);
            picture = CreateImage(canvasObject.transform, "Picture");
            picture.preserveAspect = true;
            picture.color = Color.white;
        }

        private static Image CreateImage(Transform parent, string name)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Image image = go.AddComponent<Image>();
            image.raycastTarget = false;
            return image;
        }

        /// <summary>Fades the close-up in. Does nothing for a null picture.</summary>
        public void Show(Sprite sprite)
        {
            if (sprite == null || picture == null) return;

            picture.sprite = sprite;
            IsShown = true;
            StartFade(1f);
        }

        /// <summary>Fades the close-up out.</summary>
        public void Hide()
        {
            if (!IsShown) return;

            IsShown = false;
            StartFade(0f);
        }

        private void StartFade(float target)
        {
            if (fade != null) StopCoroutine(fade);
            fade = StartCoroutine(Fade(target));
        }

        private IEnumerator Fade(float target)
        {
            float start = group.alpha;
            float elapsed = 0f;
            while (elapsed < fadeSeconds)
            {
                group.alpha = Mathf.Lerp(start, target, elapsed / fadeSeconds);
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            group.alpha = target;
            fade = null;
        }
    }
}
