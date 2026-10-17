using Remnant.Core;
using Remnant.Dialogue;
using Remnant.Enemies;
using UnityEngine;

namespace Remnant.Story
{
    /// <summary>
    /// The two spoken beats tied to the corridor monster: the Shadow explains it the first time it is on screen, and the
    /// protagonist thinks of classroom 2-B the first time it has been driven back. Each plays once (flags are stored in
    /// the checkpoint, so dying and retrying does not repeat them). The monster stands still while a dialogue is open.
    /// </summary>
    public class CorridorBeats : MonoBehaviour
    {
        [SerializeField] private MonsterController monster;
        [SerializeField] private DialogueRunner dialogueRunner;
        [SerializeField] private DialogueData sightDialogue;
        [SerializeField] private DialogueData driveBackDialogue;
        [SerializeField] private string sightFlag = "corridor_monster_seen";
        [SerializeField] private string driveBackFlag = "corridor_monster_driven_back";

        private void OnEnable()
        {
            if (monster == null) return;
            monster.Sighted += OnSighted;
            monster.Banished += OnBanished;
        }

        private void OnDisable()
        {
            if (monster == null) return;
            monster.Sighted -= OnSighted;
            monster.Banished -= OnBanished;
        }

        private void OnSighted()
        {
            Play(sightDialogue, sightFlag);
        }

        private void OnBanished()
        {
            Play(driveBackDialogue, driveBackFlag);
        }

        private void Play(DialogueData dialogue, string flag)
        {
            GameState state = GameState.Instance;
            if (state != null && state.HasFlag(flag)) return;

            // set at once so nothing can play it twice; the checkpoint keeps it
            if (state != null)
            {
                state.SetFlag(flag);
                state.RefreshCheckpointFlags();
            }

            if (dialogueRunner != null) dialogueRunner.Play(dialogue);
        }
    }
}
