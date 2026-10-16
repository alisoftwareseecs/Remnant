using TMPro;
using UnityEngine;

namespace Remnant.Interaction
{
    /// <summary>World-space "E" hint. A single instance is moved above the current target.</summary>
    public class InteractionPrompt : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;

        private void Awake()
        {
            Hide();
        }

        public void Show(Vector3 worldPosition)
        {
            transform.position = worldPosition;
            if (label != null) label.enabled = true;
        }

        public void Hide()
        {
            if (label != null) label.enabled = false;
        }
    }
}
