using System.Collections.Generic;
using UnityEngine;

namespace Remnant.Audio
{
    /// <summary>Every music and sound-effect event goes here with a timestamp, so a run can be checked against the plan.</summary>
    public static class AudioLog
    {
        private const int MaxEntries = 500;
        private static readonly List<string> Entries = new List<string>();

        public static bool ToConsole { get; set; }
        public static IReadOnlyList<string> All => Entries;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            Entries.Clear();
            ToConsole = false;
        }

        public static void Add(string message)
        {
            string line = "[" + Time.unscaledTime.ToString("0.00") + "s] " + message;
            if (Entries.Count >= MaxEntries) Entries.RemoveAt(0);
            Entries.Add(line);
            if (ToConsole) Debug.Log("[Audio] " + line);
        }

        public static void Clear()
        {
            Entries.Clear();
        }
    }
}
