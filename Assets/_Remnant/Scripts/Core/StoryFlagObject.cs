using UnityEngine;

namespace Remnant.Core
{
    /// <summary>
    /// Shows or hides a GameObject depending on a story flag, so one scene can serve several story states.
    /// Put it on a parent (or another always-active object) and assign Target if the object must be able to
    /// reappear later; with no Target it controls its own GameObject and can only hide it.
    /// </summary>
    public class StoryFlagObject : MonoBehaviour
    {
        public enum Mode { ShowIfFlagSet, HideIfFlagSet }

        [Tooltip("One flag, or several separated by commas (any of them counts as set).")]
        [SerializeField] private string flag;
        [SerializeField] private Mode mode = Mode.ShowIfFlagSet;
        [Tooltip("Optional. While any of these flags (comma separated) is set the object is hidden, whatever the first flag says.")]
        [SerializeField] private string alsoHiddenByFlags;
        [Tooltip("Optional. Defaults to this GameObject.")]
        [SerializeField] private GameObject target;

        private GameState subscribedState;

        private GameObject Target => target != null ? target : gameObject;

        private void Start()
        {
            subscribedState = GameState.Instance;
            if (subscribedState != null) subscribedState.FlagChanged += OnFlagChanged;
            Apply();
        }

        private void OnDestroy()
        {
            if (subscribedState != null) subscribedState.FlagChanged -= OnFlagChanged;
        }

        private void OnFlagChanged(string changedFlag, bool value)
        {
            if ((flag != null && flag.Contains(changedFlag)) || (alsoHiddenByFlags != null && alsoHiddenByFlags.Contains(changedFlag))) Apply();
        }

        private void Apply()
        {
            bool isSet = subscribedState != null && subscribedState.HasAnyFlag(flag);
            bool visible = mode == Mode.ShowIfFlagSet ? isSet : !isSet;
            if (visible && subscribedState != null && subscribedState.HasAnyFlag(alsoHiddenByFlags)) visible = false;
            Target.SetActive(visible);
        }
    }
}
