using Remnant.Core;
using Remnant.Interaction;
using UnityEngine;

namespace Remnant.Story
{
    /// <summary>
    /// One of Aiko's diary pages. Picking it up shows the text on the DiaryPanel and sets the flag "page_N".
    /// Pair it with a StoryFlagObject (hide when "page_N" is set) so a collected page stays gone.
    /// </summary>
    public class DiaryPageInteractable : Interactable
    {
        public const string FlagPrefix = "page_";

        [SerializeField, Range(1, 8)] private int pageNumber = 1;
        [TextArea(2, 6)] [SerializeField] private string pageText;
        [SerializeField] private DiaryPanel panel;

        public int PageNumber => pageNumber;
        public string FlagName => FlagPrefix + pageNumber;

        public override bool CanInteract
        {
            get
            {
                if (!base.CanInteract) return false;
                GameState state = GameState.Instance;
                return state == null || !state.HasFlag(FlagName);
            }
        }

        protected override void OnInteract()
        {
            if (panel == null)
            {
                Debug.LogWarning("DiaryPageInteractable: no DiaryPanel assigned.", this);
                return;
            }

            if (GameState.Instance != null) GameState.Instance.SetFlag(FlagName);
            panel.Show("Page " + pageNumber, pageText);
        }
    }
}
