using System;
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
    }
}
