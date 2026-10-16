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
        [SerializeField] private string openingLine = "Light! NOW! Before it reaches you!";
        [Tooltip("How long after the opening line the ghost first appears.")]
        [SerializeField, Min(0f)] private float ghostFirstDelaySeconds = 2f;

        [Header("After each banish")]
        [SerializeField] private string[] banishLines = { "Good.", "Again.", "Keep it on her.", "Just like them." };
        [Tooltip("Used for every banish after the list is used up.")]
        [SerializeField] private string repeatLine = "Just like them.";
        [SerializeField] private string dontLine = "Don't you dare.";
        [SerializeField] private string promptText = "[F] Lower the light";
        [SerializeField, Min(0f)] private float flickerSeconds = 1.4f;
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
                ghost.Banished += OnBanished;
                ghost.ReachedPlayer += OnReachedPlayer;
            }
        }

        private void OnDisable()
        {
            if (ghost != null)
            {
                ghost.Banished -= OnBanished;
                ghost.ReachedPlayer -= OnReachedPlayer;
            }
        }

        private IEnumerator Start()
        {
            // Wait for the distort transition to finish fading in, then the Shadow shouts at once.
            while (SceneTransition.Instance != null && SceneTransition.Instance.IsTransitioning) yield return null;

            if (hud != null) hud.ShadowSays(openingLine);
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
            if (hud != null) hud.HidePrompt();
        }

        private void OnBanished(int count)
        {
            if (hud != null)
            {
                string line = count <= banishLines.Length ? banishLines[count - 1] : repeatLine;
                hud.ShadowSays(line);
            }

            if (count == 1) StartCoroutine(FirstBanish());
        }

        private IEnumerator FirstBanish()
        {
            FlashlightController flashlight = FlashlightController.Active;
            if (flashlight != null) flashlight.Flicker(flickerSeconds);

            yield return new WaitForSeconds(promptDelaySeconds);

            promptAvailable = true;
            if (hud != null)
            {
                hud.ShowPrompt(promptText);
                hud.ShadowSays(dontLine);
            }
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
