using System.Collections;
using Remnant.Core;
using Remnant.Dialogue;
using Remnant.Flashlight;
using Remnant.Player;
using UnityEngine;

namespace Remnant.Story
{
    /// <summary>
    /// The reveal in the storage room. The moment the flashlight beam first touches the hanging legs (same cone and
    /// line-of-sight test the monster uses) the player's movement and the beam's aim lock on them. The beam brightens,
    /// then the view cross-fades to a close-up still that stays up through the Shadow's lines. After that a slow fade takes
    /// the player back to the Present with the Act 4 flag.
    /// </summary>
    public class StorageRevealSequence : MonoBehaviour
    {
        [Header("What must be lit")]
        [Tooltip("A point on the legs. The beam has to reach it.")]
        [SerializeField] private Transform lookTarget;

        [Header("Dialogue")]
        [SerializeField] private DialogueRunner dialogueRunner;
        [Tooltip("Played after the still is up (the protagonist's thoughts and the Shadow). The reveal itself has no dialogue.")]
        [SerializeField] private DialogueData shadowDialogue;
        [Tooltip("How long the beam is held on the legs before the Shadow speaks.")]
        [SerializeField, Min(0f)] private float holdSeconds = 4f;
        [Tooltip("How much brighter the beam gets while it is held on them (1 = no change).")]
        [SerializeField, Min(1f)] private float beamBoost = 2.2f;
        [SerializeField, Min(0.1f)] private float boostRampSeconds = 1.5f;

        [Header("Close-up still")]
        [SerializeField] private RevealStill still;
        [Tooltip("When, counted from the start of the hold, the still starts to fade in.")]
        [SerializeField, Min(0f)] private float stillStartSeconds = 2f;
        [SerializeField, Min(0.1f)] private float stillFadeSeconds = 1.5f;

        [Header("After")]
        [Tooltip("Cleared so the Present looks like Day 1 again (Aiko and the bullies present).")]
        [SerializeField] private string flagToClear = "day2";
        [SerializeField] private string actFlag = "act4";
        [SerializeField] private string nextScene = "Present_Classroom";
        [SerializeField] private string nextSpawnId = SpawnPoint.DefaultId;
        [SerializeField] private string titleCard = "Back.";
        [SerializeField, Min(0f)] private float fadeOutSeconds = 3f;

        private bool started;

        /// <summary>True from the moment the legs are lit until the scene changes (no pause menu then).</summary>
        public static bool IsRunning { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            IsRunning = false;
        }

        private void OnDestroy()
        {
            IsRunning = false;
        }

        private void Update()
        {
            if (started || lookTarget == null) return;

            // the Shadow's lines during the walk finish first; the reveal waits for them
            if (dialogueRunner != null && dialogueRunner.BlocksInteraction) return;

            FlashlightController flashlight = FlashlightController.Active;
            if (flashlight == null || !flashlight.Illuminates(lookTarget.position, lookTarget)) return;

            started = true;
            IsRunning = true;
            StartCoroutine(Reveal(flashlight));
        }

        private IEnumerator Reveal(FlashlightController flashlight)
        {
            PlayerController player = FindFirstObjectByType<PlayerController>();
            if (player != null) player.SetMovementLocked(true);

            flashlight.LockAim((Vector2)lookTarget.position - flashlight.Origin);

            // The beam steadies and grows brighter on the legs, then the view cross-fades to the still.
            float elapsed = 0f;
            bool stillStarted = false;
            while (elapsed < holdSeconds)
            {
                flashlight.SetIntensityBoost(Mathf.Lerp(1f, beamBoost, Mathf.Clamp01(elapsed / boostRampSeconds)));

                if (!stillStarted && still != null && elapsed >= stillStartSeconds)
                {
                    stillStarted = true;
                    StartCoroutine(still.FadeIn(stillFadeSeconds));
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            // The still stays up while the Shadow speaks.
            if (still != null && !stillStarted) StartCoroutine(still.FadeIn(stillFadeSeconds));

            bool spoken = false;
            if (dialogueRunner != null) dialogueRunner.Play(shadowDialogue, () => spoken = true);
            else spoken = true;
            while (!spoken) yield return null;

            GameState state = GameState.Instance;
            if (state != null) state.ClearFlag(flagToClear);

            if (SceneTransition.Instance != null)
            {
                SceneTransition.Instance.GoTo(nextScene, nextSpawnId, titleCard, TransitionStyle.Fade, actFlag, null, fadeOutSeconds);
            }
            else
            {
                Debug.LogWarning("StorageRevealSequence: no SceneTransition in the scene.", this);
                if (player != null) player.SetMovementLocked(false);
            }
        }
    }
}
