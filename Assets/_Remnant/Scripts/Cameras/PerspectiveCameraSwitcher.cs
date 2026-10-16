using Remnant.Core;
using UnityEngine;

namespace Remnant.Cameras
{
    public class PerspectiveCameraSwitcher : MonoBehaviour
    {
        [Tooltip("The Cinemachine camera object used in side view.")]
        [SerializeField] private GameObject sideViewCamera;

        [Tooltip("The Cinemachine camera object used in top-down view.")]
        [SerializeField] private GameObject topDownCamera;

        private GameState subscribedState;

        private void Start()
        {
            subscribedState = GameState.Instance;
            if (subscribedState != null)
            {
                subscribedState.PerspectiveChanged += ApplyPerspective;
                ApplyPerspective(subscribedState.CurrentPerspective);
            }
            else
            {
                Debug.LogWarning("PerspectiveCameraSwitcher: no GameState in the scene. Defaulting to SideView.", this);
                ApplyPerspective(Perspective.SideView);
            }
        }

        private void OnDestroy()
        {
            if (subscribedState != null)
            {
                subscribedState.PerspectiveChanged -= ApplyPerspective;
            }
        }

        private void ApplyPerspective(Perspective perspective)
        {
            if (sideViewCamera != null) sideViewCamera.SetActive(perspective == Perspective.SideView);
            if (topDownCamera != null) topDownCamera.SetActive(perspective == Perspective.TopDown);
        }
    }
}
