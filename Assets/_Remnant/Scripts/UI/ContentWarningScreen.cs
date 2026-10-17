using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.SceneManagement;

namespace Remnant.UI
{
    /// <summary>
    /// The first screen of the game: the content warning. After a short wait "Press any key to continue" fades in;
    /// any key, mouse button or gamepad button then fades everything out and loads the title screen.
    /// </summary>
    public class ContentWarningScreen : MonoBehaviour
    {
        [SerializeField] private CanvasGroup warningGroup;
        [SerializeField] private CanvasGroup promptGroup;
        [SerializeField, Min(0f)] private float promptDelaySeconds = 2f;
        [SerializeField, Min(0.1f)] private float fadeSeconds = 1f;
        [SerializeField] private string nextScene = "Title";

        private bool ready;
        private bool leaving;
        private System.IDisposable listener;

        private IEnumerator Start()
        {
            if (promptGroup != null) promptGroup.alpha = 0f;
            if (warningGroup != null) warningGroup.alpha = 0f;

            yield return Fade(warningGroup, 0f, 1f);
            yield return new WaitForSeconds(promptDelaySeconds);
            yield return Fade(promptGroup, 0f, 1f);

            ready = true;
            listener = InputSystem.onAnyButtonPress.CallOnce(_ => Continue());
        }

        private void OnDestroy()
        {
            listener?.Dispose();
        }

        private void Continue()
        {
            if (!ready || leaving) return;
            leaving = true;
            StartCoroutine(Leave());
        }

        private IEnumerator Leave()
        {
            StartCoroutine(Fade(promptGroup, promptGroup != null ? promptGroup.alpha : 0f, 0f));
            yield return Fade(warningGroup, 1f, 0f);
            SceneManager.LoadScene(nextScene);
        }

        private IEnumerator Fade(CanvasGroup group, float from, float to)
        {
            if (group == null) yield break;

            float elapsed = 0f;
            while (elapsed < fadeSeconds)
            {
                group.alpha = Mathf.Lerp(from, to, elapsed / fadeSeconds);
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            group.alpha = to;
        }
    }
}
