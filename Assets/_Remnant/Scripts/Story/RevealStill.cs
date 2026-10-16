using System.Collections;
using UnityEngine;

namespace Remnant.Story
{
    /// <summary>A full-screen still that fades in over the game (a CanvasGroup under a black letterbox background).</summary>
    public class RevealStill : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;

        public bool IsShown => group != null && group.alpha > 0.99f;

        private void Awake()
        {
            if (group != null)
            {
                group.alpha = 0f;
                group.blocksRaycasts = false;
                group.interactable = false;
            }
        }

        public IEnumerator FadeIn(float seconds)
        {
            if (group == null) yield break;

            float elapsed = 0f;
            while (elapsed < seconds)
            {
                group.alpha = Mathf.SmoothStep(0f, 1f, elapsed / seconds);
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            group.alpha = 1f;
        }

        public void Hide()
        {
            if (group != null) group.alpha = 0f;
        }
    }
}
