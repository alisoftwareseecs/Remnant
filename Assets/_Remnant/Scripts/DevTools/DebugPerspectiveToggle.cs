using Remnant.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Remnant.DevTools
{
    // Testing helper: flips perspective with a key. Only active in the Editor and development builds.
    public class DebugPerspectiveToggle : MonoBehaviour
    {
        [SerializeField] private Key toggleKey = Key.F9;

        private void Update()
        {
            if (!Debug.isDebugBuild) return;

            Keyboard keyboard = Keyboard.current;
            GameState state = GameState.Instance;
            if (keyboard == null || state == null) return;

            if (keyboard[toggleKey].wasPressedThisFrame)
            {
                Perspective next = state.CurrentPerspective == Perspective.SideView
                    ? Perspective.TopDown
                    : Perspective.SideView;
                state.SetPerspective(next);
                Debug.Log("[DevTools] Perspective: " + next);
            }
        }
    }
}
