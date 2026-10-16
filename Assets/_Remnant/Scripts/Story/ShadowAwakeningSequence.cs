using System.Collections;
using Remnant.Core;
using Remnant.Dialogue;
using Remnant.Player;
using UnityEngine;

namespace Remnant.Story
{
    /// <summary>
    /// The Shadow wakes, speaks, and pulls the player into the Future: sets the awake flag (the Shadow fades in),
    /// waits for it to separate, plays the Shadow's lines, then runs a distorting transition to the next scene.
    /// </summary>
    public class ShadowAwakeningSequence : StorySequence
    {
        [SerializeField] private DialogueRunner dialogueRunner;
        [SerializeField] private DialogueData shadowDialogue;
        [SerializeField] private string awakeFlag = "shadow_awake";

        [Tooltip("Time for the Shadow to fade in and separate before it speaks.")]
        [SerializeField, Min(0f)] private float appearSeconds = 4f;

        [Header("Shift to the Future")]
        [SerializeField] private string nextScene = "Future_Corridor";
        [SerializeField] private string nextSpawnId = SpawnPoint.DefaultId;
        [SerializeField] private TransitionStyle style = TransitionStyle.Distort;

        private bool running;

        public override void Run()
        {
            if (running) return;
            running = true;
            StartCoroutine(Sequence());
        }

        private IEnumerator Sequence()
        {
            PlayerController player = FindFirstObjectByType<PlayerController>();
            if (player != null) player.SetMovementLocked(true);

            if (GameState.Instance != null) GameState.Instance.SetFlag(awakeFlag);
            yield return new WaitForSecondsRealtime(appearSeconds);

            bool spoken = false;
            if (dialogueRunner != null) dialogueRunner.Play(shadowDialogue, () => spoken = true);
            else spoken = true;
            while (!spoken) yield return null;

            if (SceneTransition.Instance != null)
            {
                SceneTransition.Instance.GoTo(nextScene, nextSpawnId, null, style);
            }
            else
            {
                Debug.LogWarning("ShadowAwakeningSequence: no SceneTransition in the scene.", this);
                if (player != null) player.SetMovementLocked(false);
            }

            running = false;
        }
    }
}
