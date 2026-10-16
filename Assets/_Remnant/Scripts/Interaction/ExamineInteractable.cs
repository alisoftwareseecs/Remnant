using Remnant.Dialogue;
using UnityEngine;

namespace Remnant.Interaction
{
    public class ExamineInteractable : Interactable
    {
        [SerializeField] private DialogueRunner dialogueRunner;
        [SerializeField] private DialogueData dialogue;

        protected override void OnInteract()
        {
            if (dialogueRunner == null)
            {
                Debug.LogWarning("ExamineInteractable: no DialogueRunner assigned.", this);
                return;
            }

            dialogueRunner.Play(dialogue);
        }
    }
}
