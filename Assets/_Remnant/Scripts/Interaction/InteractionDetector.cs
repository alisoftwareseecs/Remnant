using System.Collections.Generic;
using Remnant.Dialogue;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Remnant.Interaction
{
    /// <summary>
    /// Lives on a child of the Player with a trigger Collider2D. Targets the closest available
    /// Interactable in range (works the same in SideView and TopDown) and fires it on a single press.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class InteractionDetector : MonoBehaviour
    {
        [Header("Input (Player/Interact from InputSystem_Actions)")]
        [Tooltip("The asset's Interact action has a Hold interaction; this reads the raw press instead.")]
        [SerializeField] private InputActionReference interactAction;

        [Header("References")]
        [SerializeField] private DialogueRunner dialogueRunner;
        [SerializeField] private InteractionPrompt prompt;

        // Overlap count per interactable, so objects with several colliders are handled.
        private readonly Dictionary<Interactable, int> inRange = new Dictionary<Interactable, int>();
        private readonly List<Interactable> stale = new List<Interactable>();
        private Interactable target;

        public Interactable Target => target;

        private void OnEnable()
        {
            if (interactAction != null) interactAction.action.Enable();
        }

        private void OnDisable()
        {
            inRange.Clear();
            target = null;
            if (prompt != null) prompt.Hide();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            Interactable interactable = other.GetComponentInParent<Interactable>();
            if (interactable == null) return;

            inRange.TryGetValue(interactable, out int count);
            inRange[interactable] = count + 1;
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            Interactable interactable = other.GetComponentInParent<Interactable>();
            if (interactable == null || !inRange.TryGetValue(interactable, out int count)) return;

            if (count <= 1) inRange.Remove(interactable);
            else inRange[interactable] = count - 1;
        }

        private void Update()
        {
            if (dialogueRunner != null && dialogueRunner.BlocksInteraction)
            {
                SetTarget(null);
                return;
            }

            SetTarget(FindClosest());

            if (target != null && interactAction != null && interactAction.action.WasPressedThisFrame())
            {
                target.Interact();
            }
        }

        private Interactable FindClosest()
        {
            Interactable best = null;
            float bestDistance = float.MaxValue;
            Vector2 origin = transform.position;

            stale.Clear();
            foreach (Interactable candidate in inRange.Keys)
            {
                // Destroyed or hidden objects don't always raise an exit event.
                if (candidate == null || !candidate.isActiveAndEnabled)
                {
                    stale.Add(candidate);
                    continue;
                }

                if (!candidate.CanInteract) continue;

                float distance = ((Vector2)candidate.transform.position - origin).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = candidate;
                }
            }

            for (int i = 0; i < stale.Count; i++) inRange.Remove(stale[i]);
            return best;
        }

        private void SetTarget(Interactable newTarget)
        {
            target = newTarget;
            if (prompt == null) return;

            if (target != null) prompt.Show(target.PromptPosition);
            else prompt.Hide();
        }
    }
}
