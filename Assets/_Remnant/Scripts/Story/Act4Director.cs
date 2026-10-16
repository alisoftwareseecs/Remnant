using System.Collections;
using Remnant.Characters;
using Remnant.Core;
using Remnant.Dialogue;
using Remnant.Player;
using Remnant.Shadow;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Remnant.Story
{
    /// <summary>
    /// Directs the Present classroom during Act 4 (flag "act4"), which passes through these states:
    /// 1. Arrival: Day 1 again with the bullies around Aiko. Getting close to them makes the lights flicker and die and
    ///    sends the player to the dark encounter (flag "act4_dark").
    /// 2. Reunion ("act4_reunion", set by the encounter): dim light, no bullies, Aiko stands in front of the player. They speak,
    ///    the Shadow says what it is and becomes an ordinary shadow ("shadow_normal"). Then "act4_stepin": bright light,
    ///    the bullies are back around Aiko.
    /// 3. Confrontation ("act4_confronted", set when the player talks to a bully): the bullies walk out through the door.
    /// 4. The talk with Aiko ("act4_done") is a StoryInteractable on Aiko.
    /// </summary>
    public class Act4Director : MonoBehaviour
    {
        [SerializeField] private DialogueRunner dialogueRunner;
        [SerializeField] private Light2D globalLight;

        [Header("The dark")]
        [Tooltip("Getting this close to the middle of the group starts the dark.")]
        [SerializeField] private Transform bullyGroupCenter;
        [SerializeField, Min(0.5f)] private float darkDistance = 3f;
        [SerializeField, Min(0.2f)] private float flickerSeconds = 1.5f;
        [Range(0f, 0.3f)] [SerializeField] private float deadLightIntensity = 0.05f;
        [SerializeField] private string encounterScene = "Act4_Encounter";
        [SerializeField] private string encounterSpawnId = "start";

        [Header("Cast (present in the scene)")]
        [SerializeField] private Transform aiko;
        [SerializeField] private Transform[] bullies = new Transform[3];
        [Tooltip("Where the bullies walk out. The door.")]
        [SerializeField] private Transform exitPoint;

        [Header("Reunion")]
        [SerializeField] private DialogueData reunionDialogue;
        [SerializeField] private DialogueData shadowReunionDialogue;
        [Tooltip("The spawn point the encounter sends the player to. Aiko stands just in front of it.")]
        [SerializeField] private string reunionSpawnId = "reunion";
        [Tooltip("Distance in front of the player at which Aiko stands.")]
        [SerializeField, Min(0.5f)] private float aikoDistance = 1.1f;
        [Range(0.05f, 1f)] [SerializeField] private float dimIntensity = 0.3f;
        [SerializeField, Min(0.5f)] private float shrinkSeconds = 5f;
        [SerializeField, Min(0.1f)] private float lightsReturnSeconds = 1.6f;
        [Tooltip("Where the bullies stand again, relative to Aiko (bully 1, 2, 3).")]
        [SerializeField] private Vector2[] bullyOffsets =
        {
            new Vector2(-1.5f, 0.5f), new Vector2(1.5f, 0.5f), new Vector2(0f, 1.4f)
        };

        [Header("Leaving")]
        [SerializeField, Min(0.5f)] private float bullyWalkSpeed = 2.4f;
        [SerializeField, Min(0f)] private float bullyStagger = 0.5f;

        [Header("Flags")]
        [SerializeField] private string actFlag = "act4";
        [SerializeField] private string darkFlag = "act4_dark";
        [SerializeField] private string reunionFlag = "act4_reunion";
        [SerializeField] private string stepInFlag = "act4_stepin";
        [SerializeField] private string confrontedFlag = "act4_confronted";

        private GameState state;
        private PlayerController player;
        private float brightIntensity = 1f;
        private bool busy;

        private void Start()
        {
            state = GameState.Instance;
            if (state == null) return;

            if (globalLight != null) brightIntensity = globalLight.intensity;
            state.FlagChanged += OnFlagChanged;

            if (!state.HasFlag(actFlag)) return;

            if (state.HasFlag(confrontedFlag)) StartCoroutine(AfterFrame(HideBullies));
            else if (state.HasFlag(stepInFlag)) StartCoroutine(AfterFrame(PlaceBullies));
            else if (state.HasFlag(reunionFlag)) StartCoroutine(Reunion());
        }

        private void OnDestroy()
        {
            if (state != null) state.FlagChanged -= OnFlagChanged;
        }

        private void OnFlagChanged(string flag, bool value)
        {
            if (flag == confrontedFlag && value) StartCoroutine(BulliesLeave());
        }

        private void Update()
        {
            if (busy || state == null || !state.HasFlag(actFlag)) return;
            if (state.HasFlag(darkFlag) || state.HasFlag(reunionFlag) || bullyGroupCenter == null) return;
            if (dialogueRunner != null && dialogueRunner.BlocksInteraction) return;
            if (SceneTransition.Instance != null && SceneTransition.Instance.IsTransitioning) return;

            if (player == null) player = FindFirstObjectByType<PlayerController>();
            if (player == null) return;

            if (Vector2.Distance(player.transform.position, bullyGroupCenter.position) > darkDistance) return;

            busy = true;
            StartCoroutine(TheDark());
        }

        private IEnumerator AfterFrame(System.Action action)
        {
            // Scene objects that follow story flags settle on the first frame; change them after that.
            yield return null;
            action();
        }

        // ---- 1. The dark ----

        private IEnumerator TheDark()
        {
            player.SetMovementLocked(true);

            float elapsed = 0f;
            while (elapsed < flickerSeconds)
            {
                float remaining = 1f - elapsed / flickerSeconds;
                if (globalLight != null)
                {
                    // Stutters between nearly out and the old brightness, a little weaker each time.
                    globalLight.intensity = Random.value < 0.5f
                        ? Random.Range(0f, 0.2f) * brightIntensity
                        : Random.Range(0.4f, 1f) * brightIntensity * remaining;
                }

                float wait = Random.Range(0.04f, 0.14f);
                elapsed += wait;
                yield return new WaitForSeconds(wait);
            }

            if (globalLight != null) globalLight.intensity = deadLightIntensity;
            yield return new WaitForSeconds(0.35f);

            if (SceneTransition.Instance != null)
            {
                SceneTransition.Instance.GoTo(encounterScene, encounterSpawnId, null, TransitionStyle.Distort, darkFlag);
            }
            else
            {
                Debug.LogWarning("Act4Director: no SceneTransition in the scene.", this);
                player.SetMovementLocked(false);
                busy = false;
            }
        }

        // ---- 2. The reunion ----

        private IEnumerator Reunion()
        {
            yield return null;

            busy = true;
            player = FindFirstObjectByType<PlayerController>();
            if (player == null) yield break;
            player.SetMovementLocked(true);

            if (globalLight != null) globalLight.intensity = dimIntensity;
            HideBullies();

            // The transition puts the player on the spawn point only after the scene has loaded, so use the spawn point itself.
            SpawnPoint spawn = SpawnPoint.Find(reunionSpawnId);
            Vector3 anchor = spawn != null ? spawn.transform.position : player.transform.position;
            if (spawn != null && !SceneTransition.HasTransitioned)
            {
                // Started directly in this scene (editor testing): move the player there ourselves.
                player.transform.position = new Vector3(anchor.x, anchor.y, player.transform.position.z);
            }

            // Aiko stands right in front of the player, and they look at each other.
            if (aiko != null)
            {
                aiko.gameObject.SetActive(true);
                Vector3 position = anchor + Vector3.up * aikoDistance;
                position.z = aiko.position.z;
                aiko.position = position;
                Face(aiko, Vector2.down);
            }

            player.SetLastMoveDirection(Vector2.up);

            while (SceneTransition.Instance != null && SceneTransition.Instance.IsTransitioning) yield return null;
            yield return new WaitForSeconds(0.8f);

            yield return Speak(reunionDialogue);

            // The Shadow says what it is while it shrinks to the player's feet, and stays an ordinary shadow from then on.
            ShadowCompanion shadow = FindFirstObjectByType<ShadowCompanion>();
            bool shrunk = shadow == null;
            if (shadow != null) StartCoroutine(Shrink(shadow, () => shrunk = true));

            yield return Speak(shadowReunionDialogue);
            while (!shrunk) yield return null;

            // Time resumes: the light is back and so are the bullies.
            state.SetFlag(stepInFlag);
            state.RefreshCheckpointFlags();
            PlaceBullies();

            float elapsed = 0f;
            while (elapsed < lightsReturnSeconds)
            {
                if (globalLight != null) globalLight.intensity = Mathf.Lerp(dimIntensity, brightIntensity, elapsed / lightsReturnSeconds);
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (globalLight != null) globalLight.intensity = brightIntensity;
            player.SetMovementLocked(false);
            busy = false;
        }

        private IEnumerator Shrink(ShadowCompanion shadow, System.Action done)
        {
            yield return shadow.ShrinkToFeet(shrinkSeconds);
            done();
        }

        private IEnumerator Speak(DialogueData dialogue)
        {
            bool spoken = false;
            if (dialogueRunner != null) dialogueRunner.Play(dialogue, () => spoken = true);
            else spoken = true;
            while (!spoken) yield return null;

            // The runner unlocks the player when it closes; the scene stays under control until the end.
            if (player != null) player.SetMovementLocked(true);
        }

        // ---- 3. The bullies ----

        private void PlaceBullies()
        {
            if (globalLight != null && state.HasFlag(stepInFlag)) globalLight.intensity = brightIntensity;

            for (int i = 0; i < bullies.Length; i++)
            {
                if (bullies[i] == null) continue;

                bullies[i].gameObject.SetActive(true);
                if (aiko == null || i >= bullyOffsets.Length) continue;

                Vector3 position = aiko.position + (Vector3)bullyOffsets[i];
                position.z = bullies[i].position.z;
                bullies[i].position = position;
                Face(bullies[i], (Vector2)aiko.position - (Vector2)position);
            }
        }

        private void HideBullies()
        {
            foreach (Transform bully in bullies)
            {
                if (bully != null) bully.gameObject.SetActive(false);
            }
        }

        private IEnumerator BulliesLeave()
        {
            bool[] gone = new bool[bullies.Length];
            for (int i = 0; i < bullies.Length; i++) StartCoroutine(WalkOut(i, gone));

            bool all = false;
            while (!all)
            {
                all = true;
                foreach (bool g in gone)
                {
                    if (!g) all = false;
                }

                yield return null;
            }
        }

        private IEnumerator WalkOut(int index, bool[] gone)
        {
            Transform bully = bullies[index];
            if (bully == null || exitPoint == null)
            {
                gone[index] = true;
                yield break;
            }

            yield return new WaitForSeconds(bullyStagger * index);

            Vector3 target = exitPoint.position;
            target.z = bully.position.z;

            while (Vector2.Distance(bully.position, target) > 0.1f)
            {
                Vector3 next = Vector3.MoveTowards(bully.position, target, bullyWalkSpeed * Time.deltaTime);
                Face(bully, next - bully.position);
                bully.position = next;
                yield return null;
            }

            bully.gameObject.SetActive(false);
            gone[index] = true;
        }

        private static void Face(Transform character, Vector2 direction)
        {
            CharacterSpriteAnimator animator = character.GetComponent<CharacterSpriteAnimator>();
            if (animator != null) animator.SetFacing(CharacterSpriteAnimator.DirectionFromVector(direction));
        }
    }
}
