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
    }
}
