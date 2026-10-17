using System.Collections;
using Remnant.Audio;
using Remnant.Characters;
using Remnant.Core;
using Remnant.Dialogue;
using Remnant.Player;
using Remnant.Shadow;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Serialization;

namespace Remnant.Story
{
    /// <summary>
    /// Directs the Present classroom during Act 4 (flag "act4"), which passes through these states:
    /// 1. Arrival: Day 1 again with the girls around Aiko. Getting close to them makes the lights flicker and die and
    ///    sends the player to the dark encounter (flag "act4_dark").
    /// 2. Reunion ("act4_reunion", set by the encounter): dim light, no girls, Aiko stands in front of the player. They speak,
    ///    the Shadow says what it is and becomes an ordinary shadow ("shadow_normal"). Then "act4_stepin": bright light,
    ///    the girls are back around Aiko.
    /// 3. Confrontation ("act4_confronted"): plays by itself once the lights are back (Docs/SCRIPT.md); the girls walk out
    ///    through the door.
    /// 4. The talk with Aiko plays by itself, sets "act4_done" and leads to the next morning (the epilogue).
    /// </summary>
    public class Act4Director : MonoBehaviour
    {
        [SerializeField] private DialogueRunner dialogueRunner;
        [SerializeField] private Light2D globalLight;

        [Header("The dark")]
        [Tooltip("The dark only starts after this flag is set (the Shadow's lines as the player walks toward her desk).")]
        [SerializeField] private string requiredBeforeDark = "act4_shadow_walkpast";
        [Tooltip("Narration when the light is dead.")]
        [SerializeField] private DialogueData lightsDieDialogue;
        [Tooltip("Getting this close to this point (Aiko's desk) starts the dark.")]
        [FormerlySerializedAs("bullyGroupCenter")]
        [SerializeField] private Transform groupCenter;
        [SerializeField, Min(0.5f)] private float darkDistance = 3f;
        [SerializeField, Min(0.2f)] private float flickerSeconds = 1.5f;
        [Range(0f, 0.3f)] [SerializeField] private float deadLightIntensity = 0.05f;
        [SerializeField] private string encounterScene = "Act4_Encounter";
        [SerializeField] private string encounterSpawnId = "start";

        [Header("Cast (present in the scene)")]
        [SerializeField] private Transform aiko;
        [FormerlySerializedAs("bullies")]
        [SerializeField] private Transform[] girls = new Transform[3];
        [Tooltip("Where the girls walk out. The door.")]
        [SerializeField] private Transform exitPoint;

        [Header("Reunion")]
        [Tooltip("Her hands are cold... (Auto mode)")]
        [SerializeField] private DialogueData reunionDialogue;
        [Tooltip("The Shadow fades: ...Fine. ... Your remnant.")]
        [SerializeField] private DialogueData shadowReunionDialogue;
        [Tooltip("The protagonist stands up to the girls.")]
        [SerializeField] private DialogueData confrontDialogue;
        [Tooltip("After the girls left: Why did you do that?")]
        [SerializeField] private DialogueData aikoTalkDialogue;
        [Tooltip("The spawn point the encounter sends the player to. Aiko stands just in front of it.")]
        [SerializeField] private string reunionSpawnId = "reunion";
        [Tooltip("Distance in front of the player at which Aiko stands.")]
        [SerializeField, Min(0.5f)] private float aikoDistance = 1.1f;
        [Range(0.05f, 1f)] [SerializeField] private float dimIntensity = 0.3f;
        [SerializeField, Min(0.5f)] private float shrinkSeconds = 5f;
        [SerializeField, Min(0.1f)] private float lightsReturnSeconds = 1.6f;
        [Tooltip("Where the girls stand again, relative to Aiko (girl 1, 2, 3).")]
        [FormerlySerializedAs("bullyOffsets")]
        [SerializeField] private Vector2[] girlOffsets =
        {
            new Vector2(-1.5f, 0.5f), new Vector2(1.5f, 0.5f), new Vector2(0f, 1.4f)
        };

        [Header("Epilogue (the next day)")]
        [Tooltip("Where Aiko sits in the epilogue: her chair.")]
        [SerializeField] private Transform aikoSeat;

        [Header("The next morning")]
        [SerializeField] private string nextDayScene = "Present_Classroom";
        [SerializeField] private string nextDaySpawnId = "start";

        [Header("Leaving")]
        [FormerlySerializedAs("bullyWalkSpeed")]
        [SerializeField, Min(0.5f)] private float girlWalkSpeed = 2.4f;
        [FormerlySerializedAs("bullyStagger")]
        [SerializeField, Min(0f)] private float girlStagger = 0.5f;

        [Header("Flags")]
        [SerializeField] private string actFlag = "act4";
        [SerializeField] private string darkFlag = "act4_dark";
        [SerializeField] private string reunionFlag = "act4_reunion";
        [SerializeField] private string stepInFlag = "act4_stepin";
        [Tooltip("When the lights come back the pleasant music fades in over this long.")]
        [SerializeField, Min(0f)] private float lightsBackMusicFadeSeconds = 2.5f;
        [SerializeField] private string confrontedFlag = "act4_confronted";
        [SerializeField] private string doneFlag = "act4_done";
        [SerializeField] private string epilogueFlag = "epilogue";

        private GameState state;
        private PlayerController player;
        private float brightIntensity = 1f;
        private bool busy;

        private void Start()
        {
            state = GameState.Instance;
            if (state == null) return;

            if (globalLight != null) brightIntensity = globalLight.intensity;

            if (!state.HasFlag(actFlag)) return;

            // The checkpoint can bring the player back in the middle of the scene: pick the story up where it was.
            if (state.HasFlag(epilogueFlag)) StartCoroutine(AfterFrame(Epilogue));
            else if (state.HasFlag(confrontedFlag)) StartCoroutine(AfterFrame(AikoTalkOnLoad));
            else if (state.HasFlag(stepInFlag)) StartCoroutine(AfterFrame(ConfrontOnLoad));
            else if (state.HasFlag(reunionFlag)) StartCoroutine(Reunion());
        }

        private void ConfrontOnLoad()
        {
            PlaceGirls();
            StartCoroutine(Confrontation());
        }

        private void AikoTalkOnLoad()
        {
            HideGirls();
            StartCoroutine(AikoTalk());
        }

        private void Update()
        {
            if (busy || state == null || !state.HasFlag(actFlag)) return;
            if (state.HasFlag(darkFlag) || state.HasFlag(reunionFlag) || state.HasFlag(epilogueFlag) || groupCenter == null) return;
            if (!string.IsNullOrEmpty(requiredBeforeDark) && !state.HasAllFlags(requiredBeforeDark)) return;
            if (dialogueRunner != null && dialogueRunner.BlocksInteraction) return;
            if (SceneTransition.Instance != null && SceneTransition.Instance.IsTransitioning) return;

            if (player == null) player = FindFirstObjectByType<PlayerController>();
            if (player == null) return;

            if (Vector2.Distance(player.transform.position, groupCenter.position) > darkDistance) return;

            busy = true;
            StartCoroutine(TheDark());
        }

        // ---- Epilogue ----

        private void Epilogue()
        {
            // A normal day: the girls are not in the room and Aiko sits at her desk.
            HideGirls();
            if (aiko == null || aikoSeat == null) return;

            aiko.gameObject.SetActive(true);
            Vector3 position = aikoSeat.position;
            position.z = aiko.position.z;
            aiko.position = position;
            Face(aiko, Vector2.up);
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

            // "The lights die.": the music cuts dead.
            if (MusicManager.Instance != null) MusicManager.Instance.HardCut();
            yield return Speak(lightsDieDialogue);

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
            HideGirls();

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

            // Time resumes: the light is back and so are the girls.
            state.SetFlag(stepInFlag);
            state.RefreshCheckpointFlags();
            // The lights come back and the pleasant music fades in.
            MusicManager music = MusicManager.Instance;
            if (music != null && music.Library != null) music.Play(music.Library.Pleasant, lightsBackMusicFadeSeconds);
            PlaceGirls();

            float elapsed = 0f;
            while (elapsed < lightsReturnSeconds)
            {
                if (globalLight != null) globalLight.intensity = Mathf.Lerp(dimIntensity, brightIntensity, elapsed / lightsReturnSeconds);
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (globalLight != null) globalLight.intensity = brightIntensity;
            busy = false;
            yield return Confrontation();
        }

        private IEnumerator Shrink(ShadowCompanion shadow, System.Action done)
        {
            yield return shadow.ShrinkToFeet(shrinkSeconds);
            done();
        }

        private IEnumerator Speak(DialogueData dialogue)
        {
            bool spoken = false;
            if (dialogueRunner != null && dialogue != null) dialogueRunner.Play(dialogue, () => spoken = true);
            else spoken = true;
            while (!spoken) yield return null;

            // The runner unlocks the player when it closes; the scene stays under control until the end.
            if (player != null) player.SetMovementLocked(true);
        }

        // ---- 3. The confrontation and the talk ----

        private IEnumerator Confrontation()
        {
            busy = true;
            if (player == null) player = FindFirstObjectByType<PlayerController>();
            if (player != null) player.SetMovementLocked(true);

            while (SceneTransition.Instance != null && SceneTransition.Instance.IsTransitioning) yield return null;
            yield return new WaitForSeconds(0.4f);

            yield return Speak(confrontDialogue);

            state.SetFlag(confrontedFlag);
            state.RefreshCheckpointFlags();
            yield return GirlsLeave();

            yield return AikoTalk();
        }

        private IEnumerator AikoTalk()
        {
            busy = true;
            if (player == null) player = FindFirstObjectByType<PlayerController>();
            if (player != null) player.SetMovementLocked(true);

            while (SceneTransition.Instance != null && SceneTransition.Instance.IsTransitioning) yield return null;
            yield return new WaitForSeconds(0.4f);

            yield return Speak(aikoTalkDialogue);

            state.SetFlag(doneFlag);
            state.RefreshCheckpointFlags();

            // The next morning: the epilogue state is set while the screen is black.
            if (SceneTransition.Instance != null)
            {
                SceneTransition.Instance.GoTo(nextDayScene, nextDaySpawnId, null, TransitionStyle.Fade, epilogueFlag);
            }
            else
            {
                Debug.LogWarning("Act4Director: no SceneTransition in the scene.", this);
                if (player != null) player.SetMovementLocked(false);
                busy = false;
            }
        }

        // ---- 4. The girls ----

        private void PlaceGirls()
        {
            if (globalLight != null && state.HasFlag(stepInFlag)) globalLight.intensity = brightIntensity;

            for (int i = 0; i < girls.Length; i++)
            {
                if (girls[i] == null) continue;

                girls[i].gameObject.SetActive(true);
                if (aiko == null || i >= girlOffsets.Length) continue;

                Vector3 position = aiko.position + (Vector3)girlOffsets[i];
                position.z = girls[i].position.z;
                girls[i].position = position;
                Face(girls[i], (Vector2)aiko.position - (Vector2)position);
            }
        }

        private void HideGirls()
        {
            foreach (Transform girl in girls)
            {
                if (girl != null) girl.gameObject.SetActive(false);
            }
        }

        private IEnumerator GirlsLeave()
        {
            bool[] gone = new bool[girls.Length];
            for (int i = 0; i < girls.Length; i++) StartCoroutine(WalkOut(i, gone));

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
            Transform girl = girls[index];
            if (girl == null || exitPoint == null)
            {
                gone[index] = true;
                yield break;
            }

            yield return new WaitForSeconds(girlStagger * index);

            Vector3 target = exitPoint.position;
            target.z = girl.position.z;

            while (Vector2.Distance(girl.position, target) > 0.1f)
            {
                Vector3 next = Vector3.MoveTowards(girl.position, target, girlWalkSpeed * Time.deltaTime);
                Face(girl, next - girl.position);
                girl.position = next;
                yield return null;
            }

            girl.gameObject.SetActive(false);
            gone[index] = true;
        }

        private static void Face(Transform character, Vector2 direction)
        {
            CharacterSpriteAnimator animator = character.GetComponent<CharacterSpriteAnimator>();
            if (animator != null) animator.SetFacing(CharacterSpriteAnimator.DirectionFromVector(direction));
        }
    }
}
