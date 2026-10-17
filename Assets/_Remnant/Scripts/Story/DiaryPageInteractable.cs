using System;
using Remnant.Core;
using Remnant.Dialogue;
using Remnant.Interaction;
using UnityEngine;

namespace Remnant.Story
{
    /// <summary>
    /// One of Aiko's diary pages. Interacting with it (optionally after a dialogue, and only once the pages it requires
    /// have been found) opens the text on the DiaryPanel with the date as the header and sets the flag "page_N". When the
    /// page is closed its reaction dialogue plays, and then <see cref="PageFinished"/> is raised. Pair it with a
    /// StoryFlagObject (hide when "page_N" is set) so a collected page stays gone.
    /// </summary>
    public class DiaryPageInteractable : Interactable
    {
        public const string FlagPrefix = "page_";

        [SerializeField, Range(1, 8)] private int pageNumber = 1;
        [Tooltip("The header of the page: the date.")]
        [SerializeField] private string dateHeader;
        [TextArea(2, 8)] [SerializeField] private string pageText;
        [SerializeField] private DiaryPanel panel;
        [SerializeField] private DialogueRunner dialogueRunner;

        [Header("Order")]
        [Tooltip("Flags that must all be set before this page can be taken (comma separated), e.g. page_1,page_2 ... for the last page.")]
        [SerializeField] private string requiresFlags;
        [Tooltip("Played before the page opens (e.g. the blood on her desk).")]
        [SerializeField] private DialogueData beforeDialogue;
        [Tooltip("Played after the page was closed. Leave empty for pages that only get a counter update.")]
        [SerializeField] private DialogueData reactionDialogue;

        /// <summary>Raised when a page was read, closed, and its reaction dialogue (if any) has finished. The argument is the page number.</summary>
        public static event Action<int> PageFinished;

        public int PageNumber => pageNumber;
        public string FlagName => FlagPrefix + pageNumber;

        public override bool CanInteract
        {
            get
            {
                if (!base.CanInteract) return false;
                GameState state = GameState.Instance;
                if (state == null) return true;
                return !state.HasFlag(FlagName) && state.HasAllFlags(requiresFlags);
            }
        }

        protected override void OnInteract()
        {
            if (panel == null)
            {
                Debug.LogWarning("DiaryPageInteractable: no DiaryPanel assigned.", this);
                return;
            }

            if (beforeDialogue != null && dialogueRunner != null) dialogueRunner.Play(beforeDialogue, OpenPage);
            else OpenPage();
        }

        private void OpenPage()
        {
            if (GameState.Instance != null) GameState.Instance.SetFlag(FlagName);
            panel.Show(dateHeader, pageText, OnPageClosed);
        }

        private void OnPageClosed()
        {
            if (reactionDialogue != null && dialogueRunner != null) dialogueRunner.Play(reactionDialogue, Finish);
            else Finish();
        }

        private void Finish()
        {
            PageFinished?.Invoke(pageNumber);
        }
    }
}
