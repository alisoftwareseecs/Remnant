using Remnant.Core;
using TMPro;
using UnityEngine;

namespace Remnant.Story
{
    /// <summary>
    /// Keeps the "Pages N/8" counter (top-right, pixel font): it is shown from the moment the classroom is entered
    /// until the last page has been read, closed and answered. The last page also sets the completion flag, which
    /// unlocks the back door through StoryInteractable variants. The Shadow's lines after the last page belong to that
    /// page (its reaction dialogue).
    /// </summary>
    public class DiaryController : MonoBehaviour
    {
        [SerializeField] private TMP_Text counterText;
        [SerializeField, Min(1)] private int totalPages = 8;
        [SerializeField] private string completionFlag = "diary_complete";

        private GameState state;

        private void Start()
        {
            state = GameState.Instance;
            if (state != null) state.FlagChanged += OnFlagChanged;
            DiaryPageInteractable.PageFinished += OnPageFinished;

            bool done = state != null && state.HasFlag(completionFlag);
            if (counterText != null) counterText.gameObject.SetActive(!done);
            UpdateCounter();
        }

        private void OnDestroy()
        {
            if (state != null) state.FlagChanged -= OnFlagChanged;
            DiaryPageInteractable.PageFinished -= OnPageFinished;
        }

        private int CountPages()
        {
            int found = 0;
            if (state == null) return 0;

            for (int i = 1; i <= totalPages; i++)
            {
                if (state.HasFlag(DiaryPageInteractable.FlagPrefix + i)) found++;
            }

            return found;
        }

        private void OnFlagChanged(string flag, bool value)
        {
            if (flag.StartsWith(DiaryPageInteractable.FlagPrefix)) UpdateCounter();
        }

        private void UpdateCounter()
        {
            if (counterText != null) counterText.text = "Pages " + CountPages() + "/" + totalPages;
        }

        private void OnPageFinished(int page)
        {
            if (page != totalPages || state == null) return;

            state.SetFlag(completionFlag);
            state.RefreshCheckpointFlags();
            if (counterText != null) counterText.gameObject.SetActive(false);
        }
    }
}
