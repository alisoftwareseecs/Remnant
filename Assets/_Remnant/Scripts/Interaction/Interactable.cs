using Remnant.Core;
using UnityEngine;

namespace Remnant.Interaction
{
    /// <summary>
    /// Base for anything the player can interact with. Needs a Collider2D (trigger or solid)
    /// so the player's InteractionDetector can find it.
    /// </summary>
    public abstract class Interactable : MonoBehaviour
    {
        [Header("Availability")]
        [SerializeField] private bool restrictPerspective;
        [SerializeField] private Perspective onlyInPerspective = Perspective.SideView;
        [SerializeField] private bool restrictTimePeriod;
        [SerializeField] private TimePeriod onlyInTimePeriod = TimePeriod.Present;
        [SerializeField] private bool onceOnly;

        [Header("Prompt")]
        [SerializeField] private Vector3 promptOffset = new Vector3(0f, 0.9f, 0f);

        private bool used;

        public Vector3 PromptPosition => transform.position + promptOffset;

        public virtual bool CanInteract
        {
            get
            {
                if (onceOnly && used) return false;

                GameState state = GameState.Instance;
                if (state != null)
                {
                    if (restrictPerspective && state.CurrentPerspective != onlyInPerspective) return false;
                    if (restrictTimePeriod && state.CurrentTimePeriod != onlyInTimePeriod) return false;
                }

                return true;
            }
        }

        public void Interact()
        {
            if (!CanInteract) return;
            used = true;
            OnInteract();
        }

        protected abstract void OnInteract();
    }
}
