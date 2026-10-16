using Remnant.Core;
using Remnant.Dialogue;
using UnityEngine;

namespace Remnant.Interaction
{
    /// <summary>Plays an optional dialogue, sets the flag "item_&lt;id&gt;", then hides itself.</summary>
    public class PickupInteractable : Interactable
    {
        [SerializeField] private string itemId = "item";
        [SerializeField] private DialogueRunner dialogueRunner;
        [Tooltip("Optional.")]
        [SerializeField] private DialogueData dialogue;

        public string FlagName => "item_" + itemId;

        private void Start()
        {
            // Already collected earlier (e.g. after returning to this scene): stay hidden.
            GameState state = GameState.Instance;
            if (state != null && state.HasFlag(FlagName)) gameObject.SetActive(false);
        }

        protected override void OnInteract()
        {
            if (dialogueRunner != null && dialogue != null)
            {
                dialogueRunner.Play(dialogue, Collect);
            }
            else
            {
                Collect();
            }
        }

        private void Collect()
        {
            GameState state = GameState.Instance;
            if (state != null) state.SetFlag(FlagName);
            gameObject.SetActive(false);
        }
    }
}
