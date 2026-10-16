using Remnant.Core;
using Remnant.Dialogue;
using UnityEngine;

namespace Remnant.Story
{
    /// <summary>
    /// The new diary page Aiko shows in the epilogue. Opens the diary panel with one extra page (it is not one of the
    /// counted pages), and when the player closes it plays a closing dialogue and sets a flag.
    /// </summary>
    public class EpilogueDiarySequence : StorySequence
    {
        [SerializeField] private DiaryPanel panel;
        [SerializeField] private string pageTitle = "A new page";
        [TextArea(2, 6)] [SerializeField] private string pageText;
        [SerializeField] private DialogueRunner dialogueRunner;
        [Tooltip("Played after the page is closed.")]
        [SerializeField] private DialogueData afterDialogue;
        [SerializeField] private string setFlagAfter = "epilogue_talked";

        private bool running;

        public override void Run()
        {
            if (running || panel == null) return;
            running = true;
            panel.Show(pageTitle, pageText, OnPageClosed);
        }

        private void OnPageClosed()
        {
            if (dialogueRunner != null) dialogueRunner.Play(afterDialogue, Finish);
            else Finish();
        }

        private void Finish()
        {
            if (GameState.Instance != null)
            {
                GameState.Instance.SetFlag(setFlagAfter);
                GameState.Instance.RefreshCheckpointFlags();
            }

            running = false;
        }
    }
}
