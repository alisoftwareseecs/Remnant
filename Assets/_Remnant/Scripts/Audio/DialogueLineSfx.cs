using System;
using Remnant.Dialogue;
using UnityEngine;

namespace Remnant.Audio
{
    /// <summary>
    /// Puts a sound on a moment of a dialogue without touching the dialogue: when a line appears, when the dialogue starts
    /// or when it has finished. A sound can be a one-shot, the start of a loop, or the end of one. Used for the heartbeat
    /// on Day 1 and in 2-B, the girls laughing, the lights dying. One per scene; it finds the scene's DialogueRunner.
    /// </summary>
    public class DialogueLineSfx : MonoBehaviour
    {
        public enum Moment { LineStarts, DialogueStarts, DialogueFinishes }
        public enum Effect { OneShot, StartLoop, StopLoop }

        /// <summary>The cues a dialogue moment can play.</summary>
        public enum Sound { None, Heartbeat, GirlsLaugh, LightsDie, LightsFlicker, LightsReturn }

        [Serializable]
        private struct Entry
        {
            [Tooltip("Only for the notes in the Inspector.")]
            public string label;
            public DialogueData dialogue;
            public Moment moment;
            [Tooltip("For 'Line Starts': the index of the line (0 = the first).")]
            [Min(0)] public int lineIndex;
            public Effect effect;
            public Sound sound;
            [Tooltip("The loop's name (loops only).")]
            public string loopId;
            [Tooltip("Fade in (StartLoop) or out (StopLoop) in seconds.")]
            [Min(0f)] public float fadeSeconds;
        }

        [SerializeField] private DialogueRunner dialogueRunner;
        [SerializeField] private Entry[] entries = new Entry[0];

        private void OnEnable()
        {
            if (dialogueRunner == null) dialogueRunner = FindFirstObjectByType<DialogueRunner>();
            if (dialogueRunner == null) return;

            dialogueRunner.LineStarted += OnLineStarted;
            dialogueRunner.DialogueStarted += OnDialogueStarted;
            dialogueRunner.DialogueFinished += OnDialogueFinished;
        }

        private void OnDisable()
        {
            if (dialogueRunner == null) return;

            dialogueRunner.LineStarted -= OnLineStarted;
            dialogueRunner.DialogueStarted -= OnDialogueStarted;
            dialogueRunner.DialogueFinished -= OnDialogueFinished;
        }

        private void OnLineStarted(DialogueData data, int index)
        {
            foreach (Entry entry in entries)
            {
                if (entry.moment == Moment.LineStarts && entry.dialogue == data && entry.lineIndex == index) Run(entry);
            }
        }

        private void OnDialogueStarted(DialogueData data)
        {
            foreach (Entry entry in entries)
            {
                if (entry.moment == Moment.DialogueStarts && entry.dialogue == data) Run(entry);
            }
        }

        private void OnDialogueFinished(DialogueData data)
        {
            foreach (Entry entry in entries)
            {
                if (entry.moment == Moment.DialogueFinishes && entry.dialogue == data) Run(entry);
            }
        }

        private static void Run(Entry entry)
        {
            AudioLibrary cues = MusicManager.Cues;
            AudioCue cue = null;
            if (cues != null)
            {
                switch (entry.sound)
                {
                    case Sound.Heartbeat: cue = cues.Heartbeat; break;
                    case Sound.GirlsLaugh: cue = cues.GirlsLaugh; break;
                    case Sound.LightsDie: cue = cues.LightsDie; break;
                    case Sound.LightsFlicker: cue = cues.LightsFlicker; break;
                    case Sound.LightsReturn: cue = cues.LightsReturn; break;
                }
            }

            switch (entry.effect)
            {
                case Effect.OneShot: SfxPlayer.Play(cue); break;
                case Effect.StartLoop: AudioLoops.Play(entry.loopId, cue, AudioChannel.Sfx, entry.fadeSeconds); break;
                case Effect.StopLoop: AudioLoops.Stop(entry.loopId, entry.fadeSeconds); break;
            }
        }
    }
}
