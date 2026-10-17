using System;
using System.Collections.Generic;
using UnityEngine;

namespace Remnant.Core
{
    public enum Perspective { SideView, TopDown }
    public enum TimePeriod { Present, Future }

    [DefaultExecutionOrder(-1000)]
    public class GameState : MonoBehaviour
    {
        public static GameState Instance { get; private set; }

        [SerializeField] private Perspective startingPerspective = Perspective.SideView;
        [SerializeField] private TimePeriod startingTimePeriod = TimePeriod.Present;

        public Perspective CurrentPerspective { get; private set; }
        public TimePeriod CurrentTimePeriod { get; private set; }

        public event Action<Perspective> PerspectiveChanged;
        public event Action<TimePeriod> TimePeriodChanged;
        /// <summary>Raised when a story flag is set (true) or cleared (false).</summary>
        public event Action<string, bool> FlagChanged;

        private readonly HashSet<string> flags = new HashSet<string>();
        private readonly HashSet<string> checkpointFlags = new HashSet<string>();
        private readonly List<string> flagScratch = new List<string>();

        /// <summary>All story flags that are set right now.</summary>
        public IEnumerable<string> Flags => flags;

        public bool HasCheckpoint { get; private set; }
        public string CheckpointScene { get; private set; }
        public Vector2 CheckpointPosition { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);

            CurrentPerspective = startingPerspective;
            CurrentTimePeriod = startingTimePeriod;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void SetPerspective(Perspective perspective)
        {
            if (perspective == CurrentPerspective) return;
            CurrentPerspective = perspective;
            PerspectiveChanged?.Invoke(perspective);
        }

        public void SetTimePeriod(TimePeriod timePeriod)
        {
            if (timePeriod == CurrentTimePeriod) return;
            CurrentTimePeriod = timePeriod;
            TimePeriodChanged?.Invoke(timePeriod);
        }

        public bool HasFlag(string flag)
        {
            return !string.IsNullOrEmpty(flag) && flags.Contains(flag);
        }

        /// <summary>True when any flag in a comma separated list is set (a single flag name works too).</summary>
        public bool HasAnyFlag(string commaSeparatedFlags)
        {
            if (string.IsNullOrWhiteSpace(commaSeparatedFlags)) return false;

            foreach (string flag in commaSeparatedFlags.Split(','))
            {
                if (flags.Contains(flag.Trim())) return true;
            }

            return false;
        }

        /// <summary>True when every flag in a comma separated list is set (an empty list counts as true).</summary>
        public bool HasAllFlags(string commaSeparatedFlags)
        {
            if (string.IsNullOrWhiteSpace(commaSeparatedFlags)) return true;

            foreach (string flag in commaSeparatedFlags.Split(','))
            {
                string trimmed = flag.Trim();
                if (trimmed.Length > 0 && !flags.Contains(trimmed)) return false;
            }

            return true;
        }

        public void SetFlag(string flag)
        {
            if (string.IsNullOrEmpty(flag)) return;
            if (flags.Add(flag)) FlagChanged?.Invoke(flag, true);
        }

        public void ClearFlag(string flag)
        {
            if (string.IsNullOrEmpty(flag)) return;
            if (flags.Remove(flag)) FlagChanged?.Invoke(flag, false);
        }

        /// <summary>Clears every flag and the checkpoint: the start of a new game.</summary>
        public void ResetAll()
        {
            flagScratch.Clear();
            flagScratch.AddRange(flags);
            foreach (string flag in flagScratch) ClearFlag(flag);

            checkpointFlags.Clear();
            HasCheckpoint = false;
            CheckpointScene = null;
        }

        /// <summary>Makes exactly these flags the set flags (loading a save).</summary>
        public void ReplaceFlags(IEnumerable<string> newFlags)
        {
            ResetAll();
            foreach (string flag in newFlags) SetFlag(flag);
        }

        /// <summary>Remembers where to restart (scene + position) together with the flags as they are now.</summary>
        public void SaveCheckpoint(string sceneName, Vector2 position)
        {
            HasCheckpoint = true;
            CheckpointScene = sceneName;
            CheckpointPosition = position;
            RefreshCheckpointFlags();
            SaveSystem.Save(sceneName, position, flags);
        }

        /// <summary>Updates the saved flags to the current ones, keeping the saved scene and position.</summary>
        public void RefreshCheckpointFlags()
        {
            if (!HasCheckpoint) return;
            checkpointFlags.Clear();
            checkpointFlags.UnionWith(flags);
        }

        /// <summary>Puts the flags back to what they were when the checkpoint was saved.</summary>
        public void RestoreCheckpointFlags()
        {
            if (!HasCheckpoint) return;

            flagScratch.Clear();
            foreach (string flag in flags)
            {
                if (!checkpointFlags.Contains(flag)) flagScratch.Add(flag);
            }

            foreach (string flag in flagScratch) ClearFlag(flag);
            foreach (string flag in checkpointFlags) SetFlag(flag);
        }
    }
}
