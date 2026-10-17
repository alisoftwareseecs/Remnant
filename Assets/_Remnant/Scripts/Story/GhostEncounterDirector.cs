using System.Collections;
using Remnant.Core;
using Remnant.Enemies;
using Remnant.Flashlight;
using Remnant.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Remnant.Story
{
    /// <summary>
    /// Runs the dark classroom scene (Act 4). The Shadow shouts for the light, Aiko's ghost starts to come, the
    /// Shadow comments on every time she is driven back, and after the first time the player is offered the way out:
    /// lowering the light for good. When the ghost then reaches the player, a white flash leads back to the
    /// classroom. There is no fail state in this scene.
    /// </summary>
    public class GhostEncounterDirector : MonoBehaviour
    {
        [SerializeField] private AikoGhostController ghost;
        [SerializeField] private EncounterHud hud;

        [Header("Input (Player/LowerLight from InputSystem_Actions)")]
        [SerializeField] private InputActionReference lowerLightAction;

        [Header("Opening")]
        [Tooltip("How long after the scene starts the ghost first appears.")]
        [SerializeField, Min(0f)] private float ghostFirstDelaySeconds = 2f;

        [Header("The Shadow's captions (non-blocking, see Docs/SCRIPT.md)")]
        [Tooltip("When the ghost appears.")]
        [SerializeField] private string appearLine = "Light! Keep it on her!";
        [Tooltip("When the light first hits her and she flinches.")]
        [SerializeField] private string flinchLine = "Look at her flinch. Just like in class.";
        [Tooltip("When she comes back for the second time.")]
        [SerializeField] private string comingLine = "Don't let her near you. She ruins everyone who gets close.";
        [Tooltip("When the [F] Lower the light prompt appears.")]
        [SerializeField] private string dontLine = "Don't you dare.";
        [Tooltip("If the light is not lowered within Hesitation Seconds of the prompt.")]
        [SerializeField] private string hesitationLine = "Put it back on her! PUT IT BACK—";
        [SerializeField, Min(0.5f)] private float hesitationSeconds = 4f;
        [SerializeField] private string promptText = "[F] Lower the light";
        [SerializeField, Min(0f)] private float flickerSeconds = 1.4f;
        [Tooltip("After her second appearance, how long until the prompt appears.")]
        [SerializeField, Min(0f)] private float promptDelaySeconds = 1.6f;

        [Header("When she reaches you")]
        [SerializeField] private string nextScene = "Present_Classroom";
        [SerializeField] private string nextSpawnId = "reunion";
        [SerializeField] private string reunionFlag = "act4_reunion";

        private bool promptAvailable;
        private bool reached;

        private void OnEnable()
        {
            if (lowerLightAction != null) lowerLightAction.action.Enable();
            if (ghost != null)
            {
                ghost.Appeared += OnAppeared;
                ghost.Banished += OnBanished;
                ghost.ReachedPlayer += OnReachedPlayer;
            }
        }

        private void OnDisable()
        {
            if (ghost != null)
            {
                ghost.Appeared -= OnAppeared;
                ghost.Banished -= OnBanished;
                ghost.ReachedPlayer -= OnReachedPlayer;
            }
        }

        private IEnumerator Start()
        {
            // Wait for the distort transition to finish fading in, then the Shadow shouts at once.
            while (SceneTransition.Instance != null && SceneTransition.Instance.IsTransitioning) yield return null;

            if (ghost != null) ghost.Begin(ghostFirstDelaySeconds);
        }

        private void Update()
        {
            if (!promptAvailable || reached || lowerLightAction == null) return;
            if (!lowerLightAction.action.WasPressedThisFrame()) return;

            FlashlightController flashlight = FlashlightController.Active;
            if (flashlight == null || flashlight.IsLowered) return;

            flashlight.LowerBeam();
            promptAvailable = false;
            if (hud != null)
            {
                // whatever the Shadow is shouting stops dead, mid-word. Silence.
                hud.CutOff();
                hud.HidePrompt();
            }
        }

        private void OnAppeared(int count)
        {
            if (hud == null) return;

            if (count == 1)
            {
                hud.ShadowSays(appearLine);
            }
            else if (count == 2)
            {
                hud.ShadowSays(comingLine);
                StartCoroutine(OfferLowering());
            }
        }

        private void OnBanished(int count)
        {
            if (count != 1) return;

            if (hud != null) hud.ShadowSays(flinchLine);
            FlashlightController flashlight = FlashlightController.Active;
            if (flashlight != null) flashlight.Flicker(flickerSeconds);
        }

        private IEnumerator OfferLowering()
        {
            yield return new WaitForSeconds(promptDelaySeconds);

            promptAvailable = true;
            if (hud != null)
            {
                hud.ShowPrompt(promptText);
                hud.ShadowSays(dontLine);
            }

            // Hesitating: the Shadow gets desperate.
            yield return new WaitForSeconds(hesitationSeconds);
            if (promptAvailable && !reached && hud != null) hud.ShadowSays(hesitationLine);
        }

        private void OnReachedPlayer()
        {
            if (reached) return;
            reached = true;

            PlayerController player = FindFirstObjectByType<PlayerController>();
            if (player != null) player.SetMovementLocked(true);

            if (hud != null) hud.HidePrompt();

            if (SceneTransition.Instance != null)
            {
                SceneTransition.Instance.GoTo(nextScene, nextSpawnId, null, TransitionStyle.Flash, reunionFlag);
            }
            else
            {
                Debug.LogWarning("GhostEncounterDirector: no SceneTransition in the scene.", this);
            }
        }
    }
}
