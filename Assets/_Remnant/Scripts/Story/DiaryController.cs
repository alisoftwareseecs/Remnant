using System.Collections;
using Remnant.Core;
using Remnant.Dialogue;
using TMPro;
using UnityEngine;

namespace Remnant.Story
{
    /// <summary>
    /// Keeps the "Pages: N/6" counter and, once the last page has been read and closed, lets the Shadow speak
    /// and sets the completion flag (which unlocks the back door through StoryInteractable variants).
    /// </summary>
    public class DiaryController : MonoBehaviour
    {
        [SerializeField] private DiaryPanel panel;
        [SerializeField] private TMP_Text counterText;
        [SerializeField] private DialogueRunner dialogueRunner;
        [SerializeField] private DialogueData completionDialogue;
        [SerializeField, Min(1)] private int totalPages = 6;
        [SerializeField] private string completionFlag = "diary_complete";

        private GameState state;
        private bool completing;

        private void Start()
        {
            state = GameState.Instance;
            if (state != null) state.FlagChanged += OnFlagChanged;
            if (panel != null) panel.Closed += OnPanelClosed;
            UpdateCounter();
        }

        private void OnDestroy()
        {
            if (state != null) state.FlagChanged -= OnFlagChanged;
            if (panel != null) panel.Closed -= OnPanelClosed;
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
            if (counterText != null) counterText.text = "Pages: " + CountPages() + "/" + totalPages;
        }

        private void OnPanelClosed()
        {
            if (completing || state == null || state.HasFlag(completionFlag)) return;
            if (CountPages() < totalPages) return;

            completing = true;
            StartCoroutine(Complete());
        }

        private IEnumerator Complete()
        {
            // Let the page panel finish closing before the Shadow speaks.
            yield return null;

            bool spoken = false;
            if (dialogueRunner != null) dialogueRunner.Play(completionDialogue, () => spoken = true);
            else spoken = true;
            while (!spoken) yield return null;

            state.SetFlag(completionFlag);
            state.RefreshCheckpointFlags();
            completing = false;
        }
    }
}
