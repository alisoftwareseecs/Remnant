using Remnant.Core;
using Remnant.Dialogue;
using UnityEngine;

namespace Remnant.Interaction
{
    public class ExamineInteractable : Interactable
    {
        [SerializeField] private DialogueRunner dialogueRunner;
        [SerializeField] private DialogueData dialogue;
        [Tooltip("Optional. Story flag set when the dialogue finishes.")]
        [SerializeField] private string setFlagOnComplete;

        protected override void OnInteract()
        {
            if (dialogueRunner == null)
            {
                Debug.LogWarning("ExamineInteractable: no DialogueRunner assigned.", this);
                return;
            }

            dialogueRunner.Play(dialogue, OnFinished);
        }

        private void OnFinished()
        {
            if (GameState.Instance != null) GameState.Instance.SetFlag(setFlagOnComplete);
        }
    }
}
