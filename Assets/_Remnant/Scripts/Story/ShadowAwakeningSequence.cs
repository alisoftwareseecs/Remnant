using System.Collections;
using Remnant.Audio;
using Remnant.Core;
using Remnant.Dialogue;
using Remnant.Player;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Remnant.Story
{
    /// <summary>
    /// The Shadow wakes, speaks, and pulls the player into the Future: sets the awake flag (the Shadow fades in),
    /// waits for it to separate, plays the Shadow's lines, then runs a distorting transition to the next scene.
    /// </summary>
    public class ShadowAwakeningSequence : StorySequence
    {
        [SerializeField] private DialogueRunner dialogueRunner;
        [Tooltip("Narration while the Shadow separates (\"The lights flicker...\").")]
        [SerializeField] private DialogueData introDialogue;
        [Tooltip("Always played, after the intro.")]
        [SerializeField] private DialogueData shadowDialogue;
        [Tooltip("Played before the Shadow's lines when the game has been completed before: the Shadow remembers.")]
        [SerializeField] private DialogueData returningDialogue;
        [SerializeField] private string awakeFlag = "shadow_awake";

        [Header("Sound")]
        [Tooltip("The music stops when the lights start to flicker, over this long.")]
        [SerializeField, Min(0f)] private float musicFadeOutSeconds = 0.3f;
        [Tooltip("The Shadow separates (and its sound plays) when this line of the intro starts: \"You move. Your shadow doesn't.\"")]
        [SerializeField, Min(0)] private int shadowSeparatesOnLine = 1;

        [Tooltip("The intro narration starts this long after the Shadow starts to wake; the lights flicker meanwhile.")]
        [SerializeField, Min(0f)] private float introDelaySeconds = 1.6f;
        [Tooltip("The classroom's light, which flickers when the Shadow wakes.")]
        [SerializeField] private Light2D globalLight;

        [Header("Shift to the Future")]
        [SerializeField] private string nextScene = "Future_Corridor";
        [SerializeField] private string nextSpawnId = SpawnPoint.DefaultId;
        [SerializeField] private TransitionStyle style = TransitionStyle.Distort;

        private bool running;
        private bool awakened;

        private IEnumerator Speak(DialogueData dialogue)
        {
            if (dialogue == null || dialogueRunner == null) yield break;

            bool spoken = false;
            dialogueRunner.Play(dialogue, () => spoken = true);
            while (!spoken) yield return null;

            // the runner unlocks the player when it closes; the sequence keeps control until the scene changes
            PlayerController player = FindFirstObjectByType<PlayerController>();
            if (player != null) player.SetMovementLocked(true);
        }

        private IEnumerator Flicker(float seconds)
        {
            float normal = globalLight != null ? globalLight.intensity : 1f;
            float elapsed = 0f;
            while (elapsed < seconds)
            {
                if (globalLight != null) globalLight.intensity = Random.value < 0.5f ? Random.Range(0.05f, 0.3f) * normal : normal;
                float wait = Random.Range(0.05f, 0.16f);
                elapsed += wait;
                yield return new WaitForSecondsRealtime(wait);
            }

            if (globalLight != null) globalLight.intensity = normal;
        }

        private void OnLineStarted(DialogueData data, int index)
        {
            if (data == introDialogue && index >= shadowSeparatesOnLine) Awaken();
        }

        // The Shadow wakes (it fades in and separates) and its sound plays, once.
        private void Awaken()
        {
            if (awakened) return;

            awakened = true;
            if (GameState.Instance != null) GameState.Instance.SetFlag(awakeFlag);
            MusicManager music = MusicManager.Instance;
            if (music != null && music.Library != null) SfxPlayer.Play(music.Library.ShadowAppear);
        }

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

            // The lights flicker and the music stops. The Shadow separates on the line "You move. Your shadow doesn't."
            awakened = GameState.Instance != null && GameState.Instance.HasFlag(awakeFlag);
            if (MusicManager.Instance != null) MusicManager.Instance.Stop(musicFadeOutSeconds);
            yield return Flicker(introDelaySeconds);

            if (dialogueRunner != null) dialogueRunner.LineStarted += OnLineStarted;
            yield return Speak(introDialogue);
            if (dialogueRunner != null) dialogueRunner.LineStarted -= OnLineStarted;
            Awaken();
            yield return new WaitForSecondsRealtime(0.8f);

            if (SaveSystem.Completed && returningDialogue != null) yield return Speak(returningDialogue);
            yield return Speak(shadowDialogue);

            if (SceneTransition.Instance != null)
            {
                SceneTransition.Instance.GoTo(nextScene, nextSpawnId, null, style, null, null, -1f, true);
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
