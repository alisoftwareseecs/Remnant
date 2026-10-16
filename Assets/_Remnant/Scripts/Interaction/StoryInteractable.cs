using System;
using Remnant.Core;
using Remnant.Dialogue;
using Remnant.Story;
using UnityEngine;

namespace Remnant.Interaction
{
    /// <summary>
    /// An interactable whose behaviour depends on story flags. The first variant whose conditions match is used:
    /// it plays its dialogue, sets a flag, then optionally starts a StorySequence or changes scene.
    /// A matching variant with nothing to do makes the object non-interactable.
    /// </summary>
    public class StoryInteractable : Interactable
    {
        [Serializable]
        public class Variant
        {
            [Header("Conditions (both optional, comma separated lists)")]
            [Tooltip("Every listed flag must be set.")]
            [SerializeField] private string requiresFlag;
            [Tooltip("None of the listed flags may be set.")]
            [SerializeField] private string forbidsFlag;

            [Header("Result")]
            [SerializeField] private DialogueData dialogue;
            [Tooltip("Set when the dialogue finishes.")]
            [SerializeField] private string setFlag;
            [SerializeField] private StorySequence sequence;

            [Header("Scene change after the dialogue")]
            [SerializeField] private bool changeScene;
            [SerializeField] private string sceneName;
            [SerializeField] private string spawnId;
            [SerializeField] private string titleCard;
            [Tooltip("Set while the screen is black, before the new scene loads.")]
            [SerializeField] private string flagDuringTransition;
            [SerializeField] private TransitionStyle style;

            public DialogueData Dialogue => dialogue;
            public string SetFlag => setFlag;
            public StorySequence Sequence => sequence;
            public bool ChangeScene => changeScene;
            public string SceneName => sceneName;
            public string SpawnId => spawnId;
            public string TitleCard => titleCard;
            public string FlagDuringTransition => flagDuringTransition;
            public TransitionStyle Style => style;

            public bool HasEffect => dialogue != null || sequence != null || changeScene || !string.IsNullOrEmpty(setFlag);

            public bool Matches(GameState state)
            {
                if (state == null) return string.IsNullOrWhiteSpace(requiresFlag);

                foreach (string flag in Split(requiresFlag))
                {
                    if (!state.HasFlag(flag)) return false;
                }

                foreach (string flag in Split(forbidsFlag))
                {
                    if (state.HasFlag(flag)) return false;
                }

                return true;
            }

            private static string[] Split(string list)
            {
                if (string.IsNullOrWhiteSpace(list)) return System.Array.Empty<string>();

                string[] parts = list.Split(',');
                for (int i = 0; i < parts.Length; i++) parts[i] = parts[i].Trim();
                return parts;
            }
        }

        [SerializeField] private DialogueRunner dialogueRunner;
        [SerializeField] private Variant[] variants = new Variant[0];

        public override bool CanInteract
        {
            get
            {
                if (!base.CanInteract) return false;
                Variant variant = SelectVariant();
                return variant != null && variant.HasEffect;
            }
        }

        private Variant SelectVariant()
        {
            GameState state = GameState.Instance;
            foreach (Variant variant in variants)
            {
                if (variant.Matches(state)) return variant;
            }

            return null;
        }

        protected override void OnInteract()
        {
            Variant variant = SelectVariant();
            if (variant == null) return;

            if (variant.Dialogue != null && dialogueRunner != null)
            {
                dialogueRunner.Play(variant.Dialogue, () => Finish(variant));
            }
            else
            {
                Finish(variant);
            }
        }

        private void Finish(Variant variant)
        {
            if (GameState.Instance != null) GameState.Instance.SetFlag(variant.SetFlag);

            if (variant.Sequence != null) variant.Sequence.Run();

            if (variant.ChangeScene)
            {
                if (SceneTransition.Instance == null)
                {
                    Debug.LogWarning("StoryInteractable: no SceneTransition in the scene.", this);
                    return;
                }

                SceneTransition.Instance.GoTo(variant.SceneName, variant.SpawnId, variant.TitleCard,
                    variant.Style, variant.FlagDuringTransition);
            }
        }
    }
}
