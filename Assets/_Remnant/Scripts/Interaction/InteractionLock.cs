using UnityEngine;

namespace Remnant.Interaction
{
    /// <summary>
    /// Lets UI that is not the DialogueRunner (for example the diary page) stop the player from interacting while
    /// it is open, and on the frame it closes so the closing key press is not reused.
    /// </summary>
    public static class InteractionLock
    {
        private static int holders;
        private static int releaseFrame = -1;

        public static bool IsBlocked => holders > 0 || Time.frameCount == releaseFrame;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            holders = 0;
            releaseFrame = -1;
        }

        public static void Acquire()
        {
            holders++;
        }

        public static void Release()
        {
            holders = Mathf.Max(0, holders - 1);
            releaseFrame = Time.frameCount;
        }
    }
}
