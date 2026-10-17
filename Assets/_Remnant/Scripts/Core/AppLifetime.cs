using UnityEngine;

namespace Remnant.Core
{
    /// <summary>
    /// Tells whether the application (or the Editor's Play mode) is shutting down. OnDisable handlers that would finish
    /// work and run completion callbacks use it to skip the callbacks while everything is being destroyed, because the
    /// objects those callbacks touch may already be gone.
    /// </summary>
    public static class AppLifetime
    {
        public static bool IsQuitting { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            IsQuitting = false;
            Application.quitting -= OnQuitting;
            Application.quitting += OnQuitting;
        }

        private static void OnQuitting()
        {
            IsQuitting = true;
        }

        /// <summary>True when an object that is being disabled is part of a teardown (quit, Play mode stop, scene unload).</summary>
        public static bool IsTearingDown(GameObject owner)
        {
            return IsQuitting || owner == null || !owner.scene.isLoaded;
        }
    }
}
